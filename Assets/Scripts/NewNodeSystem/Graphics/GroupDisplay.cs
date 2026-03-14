using PBG.Core;

public class GroupDisplay : ScriptingNode
{
    public static ConnectionRenderer? connectionRenderer;
    private static NodeCollection? _nodeCollection;

    public static void UpdateDisplay(NodeCollection nodeCollection)
    {
        _nodeCollection = nodeCollection;
    }

    void Update()
    {
        if (_nodeCollection != null)
        {
            connectionRenderer?.GenerateLines(_nodeCollection);
            _nodeCollection = null;
        }
    }

    void Render()
    {
        connectionRenderer?.RenderLines(NodeManager.GroupDisplayController);
    }
}