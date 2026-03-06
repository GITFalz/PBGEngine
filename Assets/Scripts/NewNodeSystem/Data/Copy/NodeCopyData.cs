using PBG.MathLibrary;

public class NodeCopyData
{
    public List<NodeBaseCopyData> Nodes = [];

    public void Clear()
    {
        Nodes = [];
    }
}

public abstract class NodeBaseCopyData
{
    
}

public class CustomNodeCopyData
{
    public string Name = "";
    public Vector2 Position = Vector2.Zero;
    public string Type = "";
}

public class GroupNodeCopyData
{
    public string GroupName = "";
}

public class IfElseNodeCopyData
{
    
}