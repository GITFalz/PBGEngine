using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.Assets.Scripts.NoiseNodes.Nodes;
using PBG.Data;
using PBG.LoaderConfig;
using PBG.UI;

public class CacheNode : NodeBase
{
    public static int IDCounter = 0;
    public int CacheID = 0;

    /* Node logic */
    
    //public NodeInputField InputField = null!;
    //public NodeOutputField OutputField = null!;

    public CacheNode(NodeCollection nodeCollection) : this(nodeCollection, Vector2.Zero) {}
    public CacheNode(NodeCollection nodeCollection, Vector2 position) : this(null, nodeCollection) {}

    public CacheNode(string? guid, NodeCollection nodeCollection) : this(guid, nodeCollection, Vector2.Zero) { }
    public CacheNode(string? guid, NodeCollection nodeCollection, Vector2 position) : base(guid, nodeCollection)
    {
        Color = new Vector3(0.298f, 0.373f, 0.478f);
        SetPosition(position);
    }

    public void SetPosition(Vector2 position)
    {
        Position = position;
        GridPosition = Mathf.Round(position / 10f) * 10f;
        _position = position;
    }

    public override string GetLine(LineContext context)
    {
        var input = InputFields["Input"];
        var output = OutputFields["Output"];

        if (context.GetCurrentValue)
        {
            string line = $"imageStore(heightMap, ivec2(x + z * 32, {CacheID}), vec4(";
            if (input.Input?.Output == null)
                line += GLSLHelper.GLSLConvertTo(input.Value.GetValueType(), ValueType.Vector3, input.Value.GetVariable(true));
            else
                line += GLSLHelper.GLSLConvertTo(input.Input.Output.OutputField.Value.GetValueType(), ValueType.Vector3, input.Input.Output.VariableName);

            return line + $", 1));";
            
        }
        else
        {
            return $"{output.GetVariable(context.GetCurrentValue)} = {input.GetVariable(context.GetCurrentValue)};";
        }
    }

    /* Initialization */
    public CacheNode InitBlankUI()
    {
        InputFields.Add("Input", new(new(), this, new() { Name = "Input", Type = "vec3" }));
        OutputFields.Add("Output", new(new(), this, new() { Name = "Output", Type = "vec3" }));
        return this;
    }

    public CacheNode InitUI()
    {
        Collection = new CacheNodeUI(this, "Cache", Mathf.FloorToInt(Position), Color);
        return this;
    }

    public override string GetName() => "CacheNode " + ID;

    public override bool WritesLines() => true;

    public override void ResetValueReferences()
    {
        InputFields["Input"].Value.ResetValueReferences();
    }

    public override void SetValueReferences(List<float> values, ref int index)
    {
        InputFields["Input"].Value.SetValueReferences(values, ref index);
    }

    public CacheNoiseNode GenerateNoiseNode(NoiseNodeManager manager, ref int index) => GenerateNoiseNode([], ref index);
    public CacheNoiseNode GenerateNoiseNode(NoiseValue[] variables, ref int index, int indexOffset = 0)
    {
        //Debug.Print($"[Node] : Generating noise node for node '{GetType().Name}'");

        List<GetterValue> getters = [];
        List<SetterValue> setters = [];

        /*
        GetterValue getter;

        var input = InputFields["Input"];
        var output = OutputFields["Output"];

        if (input.Input == null || !input.Input.IsConnected || input.Input.Output == null)
        {
            getter = new OLD_GetterValue(variables, index + indexOffset);
            Debug.Print($"[Node] : Getter value is constant with index: {index}");
            variables[index] = input.Value.GetNoiseValue();
            index++;
        }
        else
        {
            getter = new OLD_GetterValue(variables, input.Input.Output.Index + indexOffset);
            Debug.Print($"[Node] : Getter value is variable with index: {input.Input.Output.Index + indexOffset}");

        }
        getters.Add(getter);
        

        output.Output.Index = index;
        index++;
        

        OLD_SetterValue setter = new OLD_SetterValue(variables, output.Output.Index + indexOffset);
        Debug.Print($"[Node] : Setter value is variable with index: {output.Output.Index + indexOffset}");

        setters.Add(setter);

        var noiseNode = new CacheNoiseNode([.. getters], [.. setters], "");
        noiseNode.Id = CacheID;
        return noiseNode;
        */
        return null;
    }

    public override NoiseNode? BuildExpression(Dictionary<NodeBase, Dictionary<string, SetterValue>> connections, List<Expression> expressions)
    {
        if (connections.ContainsKey(this))
            return null;
            
        Dictionary<string, SetterValue> setterValues = [];

        List<SetterValue> setters = [];

        foreach (var (name, output) in OutputFields)
        {
            SetterValue setter = new SetterValue(output);
            setterValues.Add(name, setter);
            setters.Add(setter);
        }

        connections.Add(this, setterValues);

        var cacheNode = new CacheNoiseNode([], [.. setters], "")
        {
            Id = CacheID
        };
        return cacheNode;
    }

    public override void DeleteCore()
    {
        InputFields["Input"].Input?.Disconnect();
        OutputFields["Output"].Output.Disconnect();
    }

    public override void DeleteNode()
    {
        DisconnectFromCache();
        base.DeleteNode();
        DeleteCore();
        Collection?.Delete();
        NodeManager.RemoveNode(this);
    }

    public override NodeConfig Save()
    {
        var config = new CacheNodeConfig()
        {
            Position = [Position.X, Position.Y],
            GUID = ID.ToString()
        };
        var input = InputFields["Input"];
        config.Outputs.Add(OutputFields["Output"].GetUniqueName());
        if (input.Input != null)
        {
            config.Inputs.Add(input.GetUniqueName(), (input.Input.IsConnected && input.Input.Output != null) ? input.Input.Output.OutputField.GetUniqueName() : null);
        }
        return config;
    }
}