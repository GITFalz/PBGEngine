using PBG.MathLibrary;
using PBG.UI;

namespace PBG.Nodes;

public struct NodeBlock
{
    public string Name;
    public int Index;
    public UICol Collection;

    public NodeBlock(string name, int index, UICol collection)
    {
        Name = name;
        Index = index;
        Collection = collection;
    }
}