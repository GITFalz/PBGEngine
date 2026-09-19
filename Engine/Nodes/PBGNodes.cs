using PBG.Core;
using PBG.Data;
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.UI;

namespace PBG.Nodes;

public class PBGNodes : ScriptingNode
{
    private UIController _nodeUI;
    private NodeModule _nodeModule;
    private NodeManager _nodeManager;

    private UIController _selectionUI;
    private NodeSelection _nodeSelection;

    private bool _disabled = false;

    public PBGNodes(UIController nodeUI, NodeModule nodeModule, NodeManager nodeManager, UIController selectionUI, NodeSelection nodeSelection)
    {
        _nodeUI = nodeUI;
        _nodeModule = nodeModule;
        _nodeManager = nodeManager;

        _selectionUI = selectionUI;
        _nodeSelection = nodeSelection;
    }

    public string GetGLSLCode()
    {
        NodeCompilation nodeCompilation = new(_nodeModule);
        nodeCompilation.Sort();
        nodeCompilation.Compile();
        return nodeCompilation.GetCode();
    }

    public void Disable()
    {
        _disabled = true;

        _nodeUI.Transform.Disabled = true;
        _selectionUI.Transform.Disabled = true;

        _nodeModule.Transform.Disabled = true;
        _nodeManager.Transform.Disabled = true;
        _nodeSelection.Transform.Disabled = true;
    }

    public void Enable()
    {
        _disabled = false;

        _nodeUI.Transform.Disabled = false;
        _selectionUI.Transform.Disabled = false;

        _nodeModule.Transform.Disabled = false;
        _nodeManager.Transform.Disabled = false;
        _nodeSelection.Transform.Disabled = false;
    }


    void Update()
    {
        if (_disabled)
            return;

        if (Input.IsKeyDown(Key.ControlLeft))
        {
            ScaleSelectionWindow();
            if (Input.IsMouseDown(MouseButton.Left))
            {
                MoveNodeWindow();
            }
        }
    }

    public void ScaleSelectionWindow()
    {
        float delta = Input.GetMouseScrollDelta().Y;
        if (delta == 0 || !Input.IsKeyDown(Key.ControlLeft))
            return;

        float scale = Mathf.Clampy(_nodeUI.Scale + delta * _nodeUI.Scale * 0.1f, 0.2f, 10f);
        _nodeUI.SetScale(scale, (0, 0, 0));
    }

    public void MoveNodeWindow()
    {
        Vector2 mouseDelta = Input.GetMouseDelta();
        if (mouseDelta != Vector2.Zero && Input.IsKeyDown(Key.ControlLeft))
        {
            Vector3 newMouseDelta = new Vector3(mouseDelta.X, mouseDelta.Y, 0f);
            Vector3 newPosition = _nodeUI.Position + newMouseDelta;
            _nodeUI.SetPosition(newPosition);
        }
    }

    public static PBGNodes NewNodeWindow(TransformNode mainNode, UIAlignment viewport)
    {
        var controller = new UIController(viewport);
        var nodeModule = new NodeModule();
        var nodeManager = new NodeManager();

        var nodesNode = mainNode.AddChild("Nodes");
        nodesNode.AddComponent(nodeModule, controller, nodeManager);

        var selectionController = new UIController(viewport, "selection");
        var nodeSelection = new NodeSelection(nodeModule);

        var selectionNode = mainNode.AddChild("Selection");
        selectionNode.AddComponent(selectionController, nodeSelection);

        var nodes = new PBGNodes(controller, nodeModule, nodeManager, selectionController, nodeSelection);

        mainNode.AddComponent(nodes);

        return nodes;
    }
}