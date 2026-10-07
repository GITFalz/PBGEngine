using PBG.MathLibrary;
using PBG.UI;

namespace PBG.Nodes;

public struct NodeExecute
{
    public string Name;
    public int Index;
    public UIElementBase icon;

    public readonly Vector2 Position => icon.Center;
    public readonly Vector4 Color => icon.Color;

    public NodeExecute(string name, int index, UIElementBase Icon)
    {
        Name = name;
        Index = index;
        icon = Icon;
    }
}