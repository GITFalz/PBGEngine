using PBG.Core;
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
        var voxelSettings = new VoxelRendererSettings()
        {
            GenerationType = VoxelRendererGenerationType.Cube,
            EnableTerrainGeneration = false,
            Viewport = (240, 240, 0, 60)
        };

        var mainNode = NewInternalNode("Root");

        var worldNode = mainNode.AddChild("World");

        var skybox = new Skybox();

        worldNode.AddComponent(skybox);

        var structureNode = mainNode.AddChild("Structure");

        var voxelRenderer = new VoxelRenderer(voxelSettings)
        {
            AmbientOcclusion = false,
            RealtimeShadows = false,
            NeedsNeighborsToRender = false,
            Name = "StructureRenderer"
        };

        var structureEngineManager = new StructureEngineManager();
        var structureNodeManager = new StructureNodeManager(structureEngineManager);
        var nodeManager = new NodeManager();

        var nodeUINode = structureNode.AddChild("Nodes");

        var nodeController = new UIController();
        nodeController.Alignment.Left = 240;
        nodeController.Alignment.Right = 240;
        nodeController.Alignment.Top = 60;

        nodeUINode.AddComponent(nodeController, nodeManager);

        var groupNodeDisplayNode = structureNode.AddChild("Groups");

        var groupController = new UIController();
        groupController.Alignment.Left = 240;
        groupController.Alignment.Right = 240;
        groupController.Alignment.Top = 60;
        groupController.DisableInputHandling = true;

        var groupDisplay = new GroupDisplay();

        var selectorNode = structureNode.AddChild("Selector");

        selectorNode.AddComponent(new UIController(), new NodeSelector());

        var editorNode = structureNode.AddChild("Editor");

        editorNode.AddComponent(new UIController(), voxelRenderer, structureEngineManager, structureNodeManager);

        groupNodeDisplayNode.AddComponent(groupController, groupDisplay);
    }
}