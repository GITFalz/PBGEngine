using PBG.Nodes;
using PBG.Core;
using PBG.UI;
using PBG.NewVoxel;
using PBG.MathLibrary;
using PBG.Physics;

public class NodeTestScene : Scene
{
    public NodeTestScene() : base("NodeTest") { }

    public override void Load()
    {
        var mainNode = NewInternalNode("Root");
        PBGNodes.NewNodeWindow(mainNode, new(240, 240, 0, 0));

        var editorNode = mainNode.AddChild("Editor");

        var editorUI = new UIController();
        var editor = new WorldNodeEditor();

        editorNode.AddComponent(editorUI, editor);

        var worldNode = mainNode.AddChild("World");

        var skybox = new Skybox();
        skybox.Day = new Vector3(0.41f, 0.62f, 0.78f);
        skybox.Night = new Vector3(0.02f, 0.03f, 0.1f);
        var renderer = new VoxelRenderer();
        var uicontroller = new UIController();

        worldNode.AddComponent(uicontroller, skybox, renderer);

        // Player
        var playerNode = mainNode.AddChild("Player");

        var physicsBody = new PhysicsBody();
        var playerController = new PlayerController();

        playerNode.AddComponent(physicsBody, playerController);
        playerNode.Position = (0, 300, 0);
    }
}


