using PBG.UI;

public class  NodeInput : NodeConnector
{
    public NodeOutput? Output = null;
    public NodeInputField InputField;

    public uint Type => InputField.DefaultType;
    public uint OverloadFlags => InputField.OverloadFlags;

    public NodeInput(UIButton button, NodeBase node, NodeInputField inputField) : base(button, node)
    {
        Name = "Input";
        InputField = inputField;
    }

    public void Connect(NodeOutput output)
    {
        output.Connect(this); // list logic is handled in NodeOutput
    }

    public override void Disconnect()
    {
        
        if (Output != null && (Output.Node.ParentCacheNode == Node || Output.Node.ParentCacheNode != null && Node.ParentCacheNode != Output.Node.ParentCacheNode))
        {
            Output.Node.DisconnectFromCache();
        }
        Output?.Disconnect(this);
    }
}