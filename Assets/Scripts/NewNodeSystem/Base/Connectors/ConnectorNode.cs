using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.LoaderConfig;

public class ConnectorNode : NodeBase
{
    /* Node logic */
    public NodeInputField InputField = null!;
    public NodeOutputField OutputField = null!;

    public ConnectorNode(NodeCollection nodeCollection) : this(nodeCollection, Vector2.Zero) {}
    public ConnectorNode(NodeCollection nodeCollection, Vector2 position) : this(null, nodeCollection, position) {}
    
    public ConnectorNode(string? guid, NodeCollection nodeCollection) : this(guid, nodeCollection, Vector2.Zero) { }
    public ConnectorNode(string? guid, NodeCollection nodeCollection, Vector2 position) : base(guid, nodeCollection)
    {
        Color = new Vector3(0.478f, 0.549f, 0.600f);
        SetPosition(position);
    }

    public void SetPosition(Vector2 position)
    {
        Position = position;
        GridPosition = Mathf.Round(position / 10f) * 10f;
        _position = position;
    }

    /* Initialization */
    public ConnectorNode InitBlankUI()
    {
        InputField = new(new(), this, new() { Name = "Input", Type = "float", CanOverload = true });
        OutputField = new(new(), this, new() { Name = "Output", Type = "float" });
        return this;
    }

    public ConnectorNode InitUI()
    {
        Collection = new ConnectorNodeUI(this, "Basic", Mathf.FloorToInt(Position), Color);
        return this;
    }


    public override string GetName() => "ConnectorNode " + ID;

    public override string GetLine(LineContext context)
    {
        string line = $"{OutputField.GetVariable(false)} = {InputField.GetVariable(context.GetCurrentValue)};";
        return line;
    }

    public override NoiseNode? BuildExpression(Dictionary<NodeBase, Dictionary<string, SetterValue>> connections, List<Expression> expressions)
    {
        return null;
    }

    public void SetType(string type) => SetType(NodeValue.GetDefaultType(type), []);
    public void SetType(uint type, HashSet<NodeBase> nodes)
    {
        InputField.SetValueType(type);
        OutputField.SetValueType(type);
        for (int i = 0; i < OutputField.Output.Inputs.Count; i++)
        {
            var input = OutputField.Output.Inputs[i];
            input.Node.InitNodeTypes(nodes);
        }
    }

    public override bool WritesLines() => true;

    public override void ResetValueReferences()
    {
        InputField.Value.ResetValueReferences();
    }

    public override void SetValueReferences(List<float> values, ref int index)
    {
        InputField.Value.SetValueReferences(values, ref index);
    }

    public override void DeleteCore()
    {
        InputField.Input?.Disconnect();
        OutputField.Output.Disconnect();
    }

    public override void DeleteNode()
    {
        base.DeleteNode();
        DeleteCore();
        Collection?.Delete();
        NodeManager.RemoveNode(this);
    }

    public override NodeConfig Save()
    {
        var config = new ConnectorNodeConfig()
        {
            Position = [Position.X, Position.Y],
            GUID = ID.ToString()
        };
        config.Outputs.Add(OutputField.GetUniqueName());
        if (InputField.Input != null)
        {
            config.Inputs.Add(InputField.GetUniqueName(), (InputField.Input.IsConnected && InputField.Input.Output != null) ? InputField.Input.Output.OutputField.GetUniqueName() : null);
        }
        return config;
    }
}