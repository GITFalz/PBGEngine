using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.LoaderConfig;
using PBG.UI;

public class GroupOutputNode : NodeBase
{
    private Action _regenerateOutputs = () => { };

    public GroupOutputNode(NodeCollection nodeCollection, Vector2 pos, Dictionary<string, string> outputs) : this(null, nodeCollection, pos, outputs) { }
    public GroupOutputNode(NodeCollection nodeCollection, GroupsConfig config) : this(config.GUID, nodeCollection, new Vector2(config.OutputPosition[0], config.OutputPosition[1]), config.Outputs) { }
    public GroupOutputNode(string? guid, NodeCollection nodeCollection, Vector2 pos, Dictionary<string, string> outputs) : base(guid, nodeCollection)
    {
        Vector2 position = Mathf.Round(pos / 10f) * 10f;
        Position = position;
        GridPosition = position;
        _position = position;
        Color = (0.6f, 0.5f, 0.8f);
        foreach (var (name, _) in outputs)
        {
            var field = new NodeInputField(new(), this, new() { Name = name, Type = "float" });
            InputFields.Add(name, field);
        }
        var ui = new GroupOutputUI(Mathf.FloorToInt(Position), Color, this);
        _regenerateOutputs = ui.RegenerateOutputs;
        Collection = ui;
    }

    public override string GetName() => "GroupOutputNode " + ID;

    public override string GetLine(LineContext context)
    {
        string line = "";
        if (context.GetCurrentValue)
        {
            foreach (var (name, input) in InputFields)
            {
                line += $"{name} = {input.GetVariable(context.GetCurrentValue)};";
            }
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
        var field = new NodeInputField(new(), this, new() { Name = name, Type = "float" });
        InputFields.Add(name, field);
        if (field.Input != null) NodeManager.NodeCollection.Inputs.Add(field.Input);
        _regenerateOutputs();
    }

    public void RemoveValue(string name)
    {
        if (InputFields.Remove(name, out var field))
        {
            if (field.Input != null) NodeManager.NodeCollection.Inputs.Remove(field.Input);
            _regenerateOutputs();
            if (field.Input?.IsConnected ?? false)
            {
                field.Input.Disconnect();
                NodeManager.GenerateLines();
            }
        }
    }

    private string GetUniqueName(string name, int index = 1)
    {
        if (!InputFields.ContainsKey(name)) return name;
        if (!InputFields.ContainsKey(name + index)) return name + index;
        return GetUniqueName(name, index + 1);
    }

    public override void DeleteCore()
    {
        foreach (var output in GetInputs())
        {
            output.Disconnect();
        }
        InputFields = [];
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
