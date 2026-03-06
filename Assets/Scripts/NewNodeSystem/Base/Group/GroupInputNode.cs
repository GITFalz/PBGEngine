using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.LoaderConfig;
using PBG.UI;

public class GroupInputNode : NodeBase
{
    private Action _regenerateInputs = () => { };

    public GroupInputNode(NodeCollection nodeCollection, GroupsConfig config) : this(config.GUID, nodeCollection, new Vector2(config.InputPosition[0], config.InputPosition[1]), config.Inputs, config.Values) { }
    public GroupInputNode(string? guid, NodeCollection nodeCollection, Vector2 pos, Dictionary<string, string> inputs, List<float[]> values) : base(guid, nodeCollection)
    {
        Vector2 position = Mathf.Round(pos / 10f) * 10f;
        Position = position;
        GridPosition = position;
        _position = position;
        Color = (0.6f, 0.5f, 0.8f);
        int i = 0;
        foreach (var (name, type) in inputs)
        {
            var input = new NodeOutputParam(name, type);
            var field = new NodeOutputField(new(), this, input);
            for (int k = 0; k < values[i].Length; k++)
            {
                var value = values[i][k];
                field.Value.SetValue(value, k);
            }
            OutputFields.Add(input.Name, field);
            i++;
        }
        var ui = new GroupInputUI(Mathf.FloorToInt(Position), Color, this);
        _regenerateInputs = ui.RegenerateInputs;
        Collection = ui;
    }

    public override string GetName() => "GroupInputNode " + ID;

    public override string GetLine(LineContext context)
    {
        string line = "";
        foreach (var (name, output) in OutputFields)
        {
            line += context.GetCurrentValue ? $"{output.Value.GetGLSLType()} {output.Output.VariableName} = {name};" : $"{output.Value.GetGLSLType()} {output.Output.VariableName} = {output.Value.GetVariable(context.GetCurrentValue)}; ";
        }
        return line;
    }

    public override NoiseNode? BuildExpression(Dictionary<NodeBase, Dictionary<string, SetterValue>> connections, List<Expression> expressions)
    {
        
        return null;
    }

    public void AddValue(string name)
    {
        name = GetUniqueName(name);
        var field = new NodeOutputField(new(), this, new() { Name = name, Type = "float" });
        OutputFields.Add(name, field);
        NodeManager.NodeCollection.Outputs.Add(field.Output);
        _regenerateInputs();
    }

    public void RemoveValue(string name)
    {
        if (OutputFields.Remove(name, out var field))
        {
            NodeManager.NodeCollection.Outputs.Remove(field.Output);
            _regenerateInputs();
            if (field.Output.IsConnected)
            {
                field.Output.Disconnect();
                NodeManager.GenerateLines();
            }
        }
    }

    private string GetUniqueName(string name, int index = 1)
    {
        if (!OutputFields.ContainsKey(name)) return name;
        if (!OutputFields.ContainsKey(name + index)) return name + index;
        return GetUniqueName(name, index + 1);
    }

    public override bool WritesLines() => true;

    public override void ResetValueReferences()
    {
        foreach (var field in OutputFields.Values)
        {
            field.Value.ResetValueReferences();
        }
    }

    public override void SetValueReferences(List<float> values, ref int index)
    {
        foreach (var field in OutputFields.Values)
        {
            field.Value.SetValueReferences(values, ref index);
        }
    }

    public override void DeleteCore()
    {
        foreach (var input in GetOutputs())
        {
            input.Disconnect();
        }
        OutputFields = [];
        BlockIconCollections = [];
    }

    public override void DeleteNode()
    {
        base.DeleteNode();
        DeleteCore();
        Collection?.Delete();
        NodeManager.RemoveNode(this);
    }
}
