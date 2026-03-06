using PBG.MathLibrary;
using PBG.UI;

public abstract class NodeConnector(UIButton button, NodeBase node)
{
    public string Name = "Connector";
    public bool IsConnected = false;
    public bool IsSelected = false;

    public UIButton Button = button;
    public NodeBase Node = node;
    public Vector3 Color = node.Color;

    public Vector3 Position => Mathf.Vec3(Button?.Center ?? Vector2.Zero, 0);

    public abstract void Disconnect();

    public bool Connected()
    {
        return IsConnected;
    }

    public void Select()
    {
        IsSelected = true;
        Button.Color = new Vector4(Color * 1.5f, 1);
        Button.UpdateColor();
    }

    public void Deselect()
    {
        IsSelected = false;
        Button.Color = new Vector4(Color, 1);
        Button.UpdateColor();
    }
}