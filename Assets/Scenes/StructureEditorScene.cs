using PBG.Core;
using PBG.Graphics;
using PBG.Rendering;
using PBG.UI;
using PBG.Voxel;

public class StructureEditorScene : Scene
{
    public StructureEditorScene() : base("StructureEditor") { }

    public override void Preload()
    {
        _ = new NodeDefinitionLoader();
        _ = new GLSLManager();
    }

    public override void Load()
    {
        var mainNode = NewInternalNode("Root");

        var worldNode = mainNode.AddChild("World");
        var skybox = new Skybox();
        worldNode.AddComponent(skybox);

        var structureNode = mainNode.AddChild("Structure");


        // Nodes
        var nodeUINode = structureNode.AddChild("Nodes");

        var nodeController = new UIController();
        nodeController.Alignment.Left = 240;
        nodeController.Alignment.Right = 240;
        nodeController.Alignment.Top = 60;

        var nodeManager = new NodeManager();

        nodeUINode.AddComponent(nodeController, nodeManager);


        // Group
        var groupNodeDisplayNode = structureNode.AddChild("Groups");

        var groupController = new UIController();
        groupController.Alignment.Left = 240;
        groupController.Alignment.Right = 240;
        groupController.Alignment.Top = 60;
        groupController.DisableInputHandling = true;

        var groupDisplay = new GroupDisplay();

        groupNodeDisplayNode.AddComponent(groupController, groupDisplay);


        // Selector
        var selectorNode = structureNode.AddChild("Selector");
        selectorNode.AddComponent(new UIController(), new NodeSelector());


        // UI
        structureNode.AddChild("WorldUI").AddComponent(new UIController()); 
        structureNode.AddChild("StructureUI").AddComponent(new UIController()); 
        

        var buildBoundingBox = new BoundingBoxRenderer();
        var blockBoundingBox = new BoundingBoxRenderer();


        // Editor
        var editorNode = structureNode.AddChild("Editor");

        var bitMaskDebugger = new BitMaskDebugger();
        var bitMaskController = new UIController();

        var voxelSettings = new VoxelRendererSettings()
        {
            GenerationType = VoxelRendererGenerationType.Cube,
            EnableTerrainGeneration = false,
            Viewport = (240, 240, 0, 60)
        };

        var voxelRenderer = new VoxelRenderer(voxelSettings)
        {
            RealtimeShadows = false,
            NeedsNeighborsToRender = false,
            Name = "StructureRenderer"
        }; 

        var structureEngineManager = new StructureEngineManager();
        var structureNodeManager = new StructureNodeManager(structureEngineManager)
        {
            BuildBoundingBox = buildBoundingBox,
            BlockBoundingBox = blockBoundingBox
        };

        editorNode.AddComponent(voxelRenderer, structureEngineManager, structureNodeManager, bitMaskDebugger, bitMaskController);

        // Build bounding box
        var buildNode = structureNode.AddChild("Build Bounding Box");
        buildNode.AddComponent(new Viewport(240, 240, 0, 60), buildBoundingBox);

        // Block bounding box
        var blockNode = structureNode.AddChild("Block Bounding Box");
        blockNode.AddComponent(blockBoundingBox);
    }
}