using PBG.UI;
using static PBG.UI.Styles;
using PBG.MathLibrary;
using PBG.Graphics;
using PBG;
using PBG.Core;
using PBG.Data;
using PBG.Rendering;
using PBG.Threads;
using Silk.NET.Input;

public class StructureNodeManager : ScriptingNode
{
    public static StructureNodeManager Instance = null!;

    public static int NodePanelWidth = Game.Width - 500;
    public static int NodePanelHeight = Game.Height - 60;

    /*
    public static ShaderProgram NoiseGridShader = new ShaderProgram("Noise/noiseGrid.vert", "Noise/noiseGrid.frag");
    public static ShaderProgram NodeSystemShader = new ShaderProgram("Utils/Rectangle.vert", "Utils/Image.frag");

    public static VAO NoiseVao = new();

    private static int _noiseModelLocation = -1;
    private static int _noiseViewLocation = -1;
    private static int _noiseProjectionLocation = -1;
    private static int _noiseSizeLocation = -1;
    private static int _noisePositionLocation = -1;
    private static int _noiseScaleLocation = -1;
    */

    public static bool UpdateIconsAfterUpdate = false;

    // Navbar panel
    public UICol TreeButton = null!;
    public UICol NoiseButton = null!;
    public UICol StructureButton = null!;

    public UICol LeftPanelCollection = null!; // The base collection for the left panel

    public string NodeType = "Basic";


    public string currentType = "Tree";

    
    public static NodeAbstractField? GroupInputField = null!;
    public static Action<string> GroupRemoveField = _ => {};
    public static Action<UIField> SetGroupFieldName = (field) => {};
    public static Action<NodeValue> SetGroupFieldType = (value) => {};

    public NodeSelector NodeSelector = null!;

    

    public float OldTreeGenerationTime = 0f;
    public bool TreeSettingsChanged = true;

    public bool TreeUpdateAnalyser = false;
    public float TreeAnalyserProgress = 0f;

    public static Vector2i NodeWindowPosition { get; private set; } = new Vector2i(240, 60);
    public static Vector2i InternalNodeWindowPosition { get; private set; } = new Vector2i(0, 200);
    public static float NoiseSize = 1f;
    public static Vector2 Offset = new Vector2(0, 0);

    public static Vector2 DisplayPosition = new Vector2(100, 100);

    public static Matrix4 DisplayProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0, Game.Width, Game.Height, 0, -4, 0);

    public StructureEngineManager Parent = null!;

    // Editors
    public StructureEditor StructureEditor = null!;
    

    // UI
    public StructureNodeUI structureNodeUI = null!;
    public DragBlockUI dragBlockUI = null!;


    public TransformNode GroupsNode = null!;

    public StructureNodeManager(StructureEngineManager parent) : base()
    {
        Instance = this;

        Parent = parent;

        /*
        _noiseModelLocation = NoiseGridShader.GetLocation("model");
        _noiseViewLocation = NoiseGridShader.GetLocation("view");
        _noiseProjectionLocation = NoiseGridShader.GetLocation("projection");
        _noiseSizeLocation = NoiseGridShader.GetLocation("size");
        _noisePositionLocation = NoiseGridShader.GetLocation("position");
        _noiseScaleLocation = NoiseGridShader.GetLocation("scale");
        */
    }

    void Start()
    {
        StructureEditor = new(this);
        
        dragBlockUI = new(this);
        structureNodeUI = new(this);

        var controller = Transform.GetComponent<UIController>();

        controller.AddElement(dragBlockUI);
        controller.AddElement(structureNodeUI);

        GroupsNode = Transform.ParentNode!.GetNode("Groups");

        NodeSelector = Transform.ParentNode.GetNode("Selector").GetComponent<NodeSelector>();
    }

    

    public static void GroupRemoveFieldCall(string name) => GroupRemoveField(name);
    public static void SetGroupFieldNameCall(UIField field) => SetGroupFieldName(field);
    public static void SetGroupFieldTypeCall(NodeValue value) => SetGroupFieldType(value);

    public Vector3 LoadingBarColor(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        if (t < 0.5f)
        {
            float k = t / 0.5f;
            return new Vector3(1f, k, 0f);
        }
        else
        {
            float k = (t - 0.5f) / 0.5f;
            return new Vector3(1f - k, 1f, 0f);
        }
    }

    public void SwitchTree()
    {
        if (currentType == "Tree") return;
        ToggleType(currentType, false);
        ToggleType("Tree", true);
        currentType = "Tree";

        TreeButton.UpdateColor((0.3f, 0.3f, 0.3f, 1f));
        NoiseButton.UpdateColor((0.2f, 0.2f, 0.2f, 1f));
        StructureButton.UpdateColor((0.2f, 0.2f, 0.2f, 1f));

        Parent.Editor = EditorType.Tree;

        Parent.MeshRenderer.Renderer.Run = true;
    }

    public void SwitchNoise()
    {
        if (currentType == "Noise") return;
        ToggleType(currentType, false);
        ToggleType("Noise", true);
        currentType = "Noise";

        TreeButton.UpdateColor((0.2f, 0.2f, 0.2f, 1f));
        NoiseButton.UpdateColor((0.3f, 0.3f, 0.3f, 1f));
        StructureButton.UpdateColor((0.2f, 0.2f, 0.2f, 1f));

        Parent.Editor = EditorType.Noise;

        Parent.MeshRenderer.Renderer.Run = false;
    }

    public void SwitchStructure()
    {
        if (currentType == "Structure") return;
        ToggleType(currentType, false);
        ToggleType("Structure", true);
        currentType = "Structure";

        TreeButton.UpdateColor((0.2f, 0.2f, 0.2f, 1f));
        NoiseButton.UpdateColor((0.2f, 0.2f, 0.2f, 1f));
        StructureButton.UpdateColor((0.3f, 0.3f, 0.3f, 1f));

        Parent.Editor = EditorType.Structure;

        StructureEditor.Start();
        
        Parent.MeshRenderer.Renderer.Run = true;
    }

    public void ToggleType(string type, bool state)
    {
        switch (type)
        {
            case "Tree":
                foreach (var element in structureNodeUI.TreeElements)
                {
                    element.SetVisible(state);
                }
                break;
            case "Noise":
                foreach (var element in structureNodeUI.NoiseElements)
                {
                    element.SetVisible(state);
                }
                break;
            case "Structure":
                foreach (var element in structureNodeUI.StructureElements)
                {
                    element.SetVisible(state);
                }
                break;
        }
    }

    public void LoadFile(string fileName, string filePath)
    {
        NodeManager.SetName(fileName);
        bool succes = NodeManager.Load();
        if (succes)
        {
            Console.WriteLine("Loaded file: " + filePath);
        }
        else
        {
            Console.WriteLine($"The file {fileName} failed to load");
        }
    }

    public void DeleteFile(string filePath)
    {
        if (!NodeManager.DeleteFile(filePath))
            return;
            
        switch (NodeType)
        {
            case "Basic": structureNodeUI.RegenerateNodeList(); break;
            case "Group": structureNodeUI.RegenerateGroupList(); NodeSelector.RegenerateGroupList(); break;
        }
    }

    public void CenterOnClick(UICol _)
    {
        if (StructureEngineManager.CanRotate)
            return;

        if (Parent.Editor == EditorType.Structure && StructureEditor.ShowScript && !Parent.MeshRenderer.Rendering)
            return;
        
        if (StructureEditor.RightUIPanel.BlockCollection.Hovering)
            return;

        StructureEngineManager.CanRotate = true;
        Game.SetCursorState(CursorMode.Disabled);
        Camera.SetCameraMode(PBG.Rendering.CameraMode.Free);
        Camera.Unlock();
    }

    public static void UpdateScalingAndParent(NodeBase node)
    {
        node.Collection.ApplyChanges(UIChange.Scale);
        if (node.ParentIfElseNode != null)
        {
            UpdateScalingAndParent(node.ParentIfElseNode);
        }
    }

    public static void MoveNodes()
    {
        Vector2 min = new Vector2(float.MaxValue);
        Vector2 max = new Vector2(float.MinValue);
        bool hasParentNode = false;
        foreach (var node in NodeManager.SelectedNodes)
        {
            node.MoveNode();
            min = Mathf.Min(min, node.Collection.Point1);
            max = Mathf.Max(max, node.Collection.Point2);
            if (node.ParentIfElseNode != null)
                hasParentNode = true;
        }
        Vector2 center = (min + max) * 0.5f;

        // Check if trying to put nodes inside of a parent node
        var highestIfElseNode = GetHighestParentIfElseNode(IfElseNode.AllIfElseNodes, center);        
        if (highestIfElseNode != null)
        {
            foreach (var node in NodeManager.SelectedNodes)
            {
                if (node != highestIfElseNode && node.ParentIfElseNode != highestIfElseNode && !highestIfElseNode.SubCollection.Has(node.Collection))
                {
                    if (!node.Collection.RemoveFromParent())
                        NodeManager.NodeUIController.AbsoluteElements.Remove(node.Collection);

                    highestIfElseNode.SubCollection.AddElement(node.Collection);
                    node.ParentIfElseNode = highestIfElseNode;

                    node.Collection.ApplyChanges(UIChange.Transform);
                }
            }
            UpdateScalingAndParent(highestIfElseNode);
        }
        else if (hasParentNode)
        {
            foreach (var node in NodeManager.SelectedNodes)
            {
                var parentNode = node.ParentIfElseNode;
                if (parentNode == null)
                    continue;

                node.Collection.RemoveFromParent();
                NodeManager.NodeUIController.AbsoluteElements.Add(node.Collection);
                node.ParentIfElseNode = null;
                UpdateScalingAndParent(parentNode);
            }
        }

        NodeManager.UpdateLines();
    }

    public void MoveNodeWindow()
    {
        Vector2 mouseDelta = Input.GetMouseDelta();
        if (mouseDelta != Vector2.Zero && Input.IsKeyDown(Key.ControlLeft))
        {
            Vector3 newMouseDelta = new Vector3(mouseDelta.X, mouseDelta.Y, 0f);
            Vector3 newPosition = NodeManager.NodeUIController.Position + newMouseDelta;
            NodeManager.NodeUIController.SetPosition(newPosition);
        }
    }

    public void ScaleSelectionWindow()
    {
        float delta = Input.GetMouseScrollDelta().Y;
        if (delta == 0 || !Input.IsKeyDown(Key.ControlLeft))
            return;

        float scale = Mathf.Clampy(NodeManager.NodeUIController.Scale + delta * NodeManager.NodeUIController.Scale * 0.1f, 0.2f, 10f);
        NodeManager.NodeUIController.SetScale(scale, (NodeWindowPosition.X, NodeWindowPosition.Y, 0));
    }

    public void ResizeNodeWindow()
    {
        NodePanelWidth = Game.Width - 480;
        NodePanelHeight = Game.Height - 60;

        InternalNodeWindowPosition = new Vector2i(0, Game.Height - NodePanelHeight);

        DisplayProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0, Game.Width, Game.Height, 0, -4, 0);
    }

    public void Awake()
    {

    }

    public void Resize()
    {
        StructureEditor.Rezise();
        ResizeNodeWindow();

        DisplayPosition = new Vector2(Game.Width - 235, Game.Height - 235);
    }

    public void RegenerateTree()
    {
        var process = new StructureTreeGenerationProcess(Parent.MeshRenderer.Renderer, (0, 0, 0), structureNodeUI.GetCurrentTreeInfo());
        TaskPool.QueueAction(process);
    }

    public void Update()
    {
        structureNodeUI.Update();

        if (Parent.Editor == EditorType.Tree)
        {
            if (TreeSettingsChanged && OldTreeGenerationTime + 0.05f <= GameTime.TotalTime)
            {
                TreeSettingsChanged = false;
                OldTreeGenerationTime = GameTime.TotalTime;
                RegenerateTree();
            }

            if (TreeUpdateAnalyser)
            {
                TreeUpdateAnalyser = false;
                structureNodeUI.TreeAnalyserLoadingBar.Width = UISize.Percent(TreeAnalyserProgress * 100f);
                structureNodeUI.TreeAnalyserLoadingBar.Color = new Vector4(LoadingBarColor(TreeAnalyserProgress), 1f);
                structureNodeUI.TreeAnalyserLoadingBar.ApplyChanges(UIChange.Scale | UIChange.Color);
            }
        }
        else if (Parent.Editor == EditorType.Structure)
        {
            StructureEditor.Update();
        }
        else if (Parent.Editor == EditorType.Noise)
        {
            if (Input.IsKeyPressed(Key.B))
            {
                NodeManager.PrintLines();
            }

            if (Input.IsMousePressed(MouseButton.Right))
            {
                NodeSelector.UpdatePosition();
            }

            if (Input.IsKeyPressed(Key.Delete))
            {
                NodeManager.DeleteSelectedNode();
            }

            if (Input.IsKeyDown(Key.G))
            {
                MoveNodes();
            }

            if (Input.IsKeyDown(Key.ShiftLeft))
            {
                NoiseSize -= Input.GetMouseScrollDelta().Y * 0.1f * NoiseSize;
            }

            if (Input.IsKeyDown(Key.ControlLeft))
            {
                ScaleSelectionWindow();
                if (Input.IsMouseDown(MouseButton.Left))
                {
                    MoveNodeWindow();
                }

                if (Input.IsKeyPressed(Key.S))
                {
                    NodeManager.Save();
                }

                if (Input.IsKeyPressed(Key.L))
                {
                    NodeManager.Load();
                }

                if (Input.IsKeyPressed(Key.C))
                {
                    NodeManager.SaveCopyNodes();
                }

                if (Input.IsKeyPressed(Key.V))
                {
                    NodeManager.LoadCopyNodes();
                }
            }

            if (Input.IsKeyPressed(Key.U))
            {
                var nodes = NodeManager.NodeCollection.GetConnectedNodeList();
                var nodeTree = NodeManager.GetNodeTree(nodes);
                for (int i = 0; i < nodeTree.Count; i++)
                {
                    var node = nodeTree[i];
                    node.Print();
                }
            }

            if (Input.IsMousePressed(MouseButton.Left) && !Input.IsKeyDown(Key.ShiftLeft))
            {
                NodeManager.UnselectAllNodes();
            }
        }
    }

    public static IfElseNode? GetHighestParentIfElseNode(List<IfElseNode> nodes, Vector2 center) => GetHighestParentIfElseNode(nodes, center, null);
    public static IfElseNode? GetHighestParentIfElseNode(List<IfElseNode> nodes, Vector2 center, NodeBase? ignoreNode)
    {
        if (nodes.Count == 0)
            return null;

        var highestIfElseNode = nodes[0];
        int inbeddingLevel = highestIfElseNode.GetInbeddingLevel();
        for (int i = 1; i < nodes.Count; i++)
        {
            var hoveringNode = nodes[i];
            if (hoveringNode == ignoreNode)
                continue;

            int hoveringInbeddingLevel = hoveringNode.GetInbeddingLevel();
            if (hoveringInbeddingLevel >= inbeddingLevel && hoveringNode.SubCollection.MouseOver(center))
            {
                highestIfElseNode = hoveringNode;
                inbeddingLevel = hoveringInbeddingLevel;
            }
        }
        return (!highestIfElseNode.SubCollection.MouseOver(center) || highestIfElseNode == ignoreNode) ? null : highestIfElseNode;
    }

    public void LateUpdate()
    {
        NodeManager.LateUpdate();
        GroupsNode.Disabled = !Input.IsKeyDown(Key.Tab);
    }

    public void Render()
    {
        /*
        if (Parent.Editor == EditorType.Tree)
        {
            GL.Clear(ClearBufferMask.DepthBufferBit);
        }
        else if (Parent.Editor == EditorType.Structure)
        {
            StructureEditor.Render();
        }
        else if (Parent.Editor == EditorType.Noise)
        {
            GL.Viewport(InternalNodeWindowPosition.X + 240, InternalNodeWindowPosition.Y - 60, NodePanelWidth, NodePanelHeight);

            GL.Disable(EnableCap.DepthTest);

            NoiseGridShader.Bind();

            Matrix4 model = Matrix4.Identity;
            Matrix4 view = Matrix4.Identity;
            Matrix4 projection = NodeManager.NodeUIController.GetProjection();

            GL.UniformMatrix4(_noiseModelLocation, false, ref model);
            GL.UniformMatrix4(_noiseViewLocation, false, ref view);
            GL.UniformMatrix4(_noiseProjectionLocation, false, ref projection);
            GL.Uniform2(_noiseSizeLocation, new Vector2(NodePanelWidth, NodePanelHeight));
            GL.Uniform2(_noisePositionLocation, NodeManager.NodeUIController.Position.Xy);
            GL.Uniform1(_noiseScaleLocation, NodeManager.NodeUIController.Scale);

            NoiseVao.Bind();

            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            Shader.Error("Noise grid shader error: ");

            NoiseVao.Unbind();
            NoiseGridShader.Unbind();

            GL.Enable(EnableCap.DepthTest);

            GL.Viewport(0, 0, Game.Width, Game.Height);

            GLSLManager.Render(DisplayProjectionMatrix, DisplayPosition, (230, 230), NoiseSize, Offset, (1, 1, 1, 1));
        }
        */
    }

    public void Exit()
    {
        
    }

    public void Dispose()
    {
        //NoiseGridShader.DeleteBuffer();
        //NodeSystemShader.DeleteBuffer();
    }
}   