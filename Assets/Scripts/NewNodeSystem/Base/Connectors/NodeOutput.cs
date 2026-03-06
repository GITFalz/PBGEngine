using PBG.UI;

public class NodeOutput : NodeConnector
{
    public List<NodeInput> Inputs = new List<NodeInput>();
    public List<int> Indices = new List<int>();
    public NodeOutputField OutputField;
    public int Index = -1;
    public string VariableName = "output";

    public uint DefaultType => OutputField.DefaultType;
    public uint CurrentType => OutputField.CurrentType;
    public uint OverloadFlags => OutputField.OverloadFlags;

    public NodeOutput(UIButton button, NodeBase node, NodeOutputField outputField) : base(button, node)
    {
        Name = "Output";
        OutputField = outputField;
    }

    public void Connect(NodeInput input)
    {
        if (Inputs.Contains(input))
            return;

        Inputs.Add(input);
        Indices.Add(-1);
        input.Output = this;

        IsConnected = true;
        input.IsConnected = true;
    }

    public override void Disconnect()
    {
        foreach (var input in Inputs)
        {
            input.Output = null;
            input.IsConnected = false;
        }

        IsConnected = false;
        Inputs = [];
        Indices = [];
    }

    public void SetIndex(NodeInput input, int index)
    {
        if (Inputs.Contains(input))
        {
            Indices[Inputs.IndexOf(input)] = index;
        }
    }

    public void Disconnect(NodeInput input)
    {
        if (Inputs.Remove(input))
        {
            Indices.Remove(Inputs.IndexOf(input));
            input.Output = null;
            input.IsConnected = false;
        }

        if (Inputs.Count == 0)
        {
            IsConnected = false;
        }
    }
    
    public bool IsConnectedTo(NodeInput input)
    {
        return Inputs.Contains(input);
    }
}