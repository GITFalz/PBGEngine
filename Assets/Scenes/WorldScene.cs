using PBG.Core;
using PBG.Data;
using PBG.MathLibrary;
using PBG.Physics;
using PBG.Rendering;
using PBG.UI;
using PBG.Voxel;

public class WorldScene : Scene
{
    public WorldScene() : base("World") {}

    public override void Preload()
    {
        _ = new NodeDefinitionLoader();
        _ = new GLSLManager();
    }

    public override void Load()
    {
        var mainNode = NewInternalNode("Root");

        // World
        var worldNode = mainNode.AddChild("World");
        
        var worldManager = new WorldManager();
        var skybox = new Skybox();
        skybox.Color = new Vector3(0.39f, 0.58f, 0.93f);
        var voxelRenderer = new VoxelRenderer();
        voxelRenderer.ChunkGenerator = new WorldGenerator();
        var LODVoxelRenderer = new LODVoxelRenderer();

        worldNode.AddComponent(worldManager, skybox, voxelRenderer, LODVoxelRenderer);

        // UI
        var uiNode = mainNode.AddChild("UI");

        var info = new Info();

        uiNode.AddComponent(new UIController(), info);

        // Player
        var playerNode = mainNode.AddChild("Player");

        var physicsBody = new PhysicsBody();
        var playerController = new PlayerController();

        playerNode.AddComponent(physicsBody, playerController);
        playerNode.Position = (0, 300, 0);

        // Entity
        var entityNode = mainNode.AddChild("Entity");

        var entityManager = new EntityManager();

        entityNode.AddComponent(entityManager);

        DefaultCamera.SetCameraMode(CameraMode.Free);
    }
}