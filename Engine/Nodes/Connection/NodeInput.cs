using PBG.MathLibrary;
using PBG.UI;

namespace PBG.Nodes;

public struct NodeInput
{
    public string Name;
    public int Index;
    public UIElementBase? icon;
    public NodeDataType Type;
    public NodeConnectionType ConnectionType;
    public object? Value = null;

    public readonly Vector2 Position => icon!.Center;
    public readonly Vector4 Color => icon!.Color;

    public NodeInput(string name, int index, UIElementBase? Icon, NodeDataType type, NodeConnectionType connectionType)
    {
        Name = name;
        Index = index;
        icon = Icon;
        Type = type;
        ConnectionType = connectionType;
    }
}