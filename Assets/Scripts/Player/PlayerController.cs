using PBG;
using PBG.Core;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Physics;
using PBG.Rendering;
using PBG.UI;
using PBG.Voxel;
using Silk.NET.Input;

public class PlayerController : ScriptingNode
{
    public static Vector3 PlacedBlockPos = Vector3.Zero;
    public Vector3 FirstPersonCameraOffset = (0, 1.7f, 0);

    public PlayerBaseState CurrentState;
    public PlayerBaseState AdminState;
    public PlayerBaseState GameState;

    public PhysicsBody PhysicsBody = null!;
    public VoxelRenderer World = null!;
    public Skybox Skybox = null!;
    
    public SimpleModel? Model;
    public SimpleModel? WeaponModel;

    public float Yaw;

    private Vector3 _oldPlayerPosition = Vector3.Zero;
    private Vector3 _oldCameraPosition = Vector3.Zero;

    private CameraMode _oldCameraMode;

    public bool UpdateBlockData = true;

    public Vector3i BlockPlacementPosition
    {
        get => _blockPlacementPosition;
        set {
            if (_blockPlacementPosition != value)
            {
                UpdateBlockData = true;
                _blockPlacementPosition = value;
            }
        }
    }
    public Vector3i _blockPlacementPosition = Vector3i.Zero;
    public int SelectedRegion = -1;
    public int SelectedSide = 0;

    public bool RenderPlacementHelper = false;
    public bool RenderEmptyBlock = false;

    private CursorMode? _oldState = null;

    public bool ShowScript = false;
    public bool ShowBoundingBoxes = true;


    public bool IsMining = false;


    private bool _showSettings = false;

    public PlayerController()
    {
        AdminState = new PlayerAdminState(this);
        GameState = new PlayerGameState(this);

        CurrentState = GameState;
    }

    public void SwitchState(PlayerBaseState state)
    {
        CurrentState.Exit();
        CurrentState = state;
        CurrentState.Start();
    }  

    void Start()
    {
        PhysicsBody = Transform.GetComponent<PhysicsBody>();
        var worldNode = Scene.GetNode("Root/World");
        World = worldNode.GetComponent<VoxelRenderer>();
        Skybox = worldNode.GetComponent<Skybox>();

        Info.SetPlayerPosition(Scene.DefaultCamera.Position);
        _oldPlayerPosition = Transform.Position;
        _oldCameraPosition = Scene.DefaultCamera.Position;

        string modelPath = Path.Combine(Game.ModelPath, "player2.model");
        if (File.Exists(modelPath))
        {
            Model = new(modelPath);
            Model.IsShown = false;
        }

        WeaponData.TryGet("sword", out WeaponModel);

        WorldManager.SpawnEntity((10, 20, 10));
    }

    void Awake()
    {
        CurrentState.Start();

        Scene.DefaultCamera.CanZoom = () => Input.IsKeyDown(Key.ControlLeft);

        
    }

    void Resize()
    {

    }

    void Update()
    {
        IsMining = false;

        SelectedRegion = -1;
        RenderPlacementHelper = false;
        RenderEmptyBlock = false;
        if (_oldState != CursorMode.Normal && Game.IsCursorState(CursorMode.Disabled))
        {
            bool raycast = VoxelData.Raycast(World, Camera.Position, Camera.front, 100, out Hit hit);
            if (raycast)
            {
                Vector3i position = hit.BlockPosition + hit.Normal;
                if (Input.IsMousePressed(MouseButton.Right) && BlockData.GetBlock("grass_block", out Block block))
                {
                    int rotationIndex = 0;
                    
                    block.SetRotation((uint)rotationIndex);
                    World.SetBlock(position, block);
                    IsMining = true;
                }
                BlockPlacementPosition = hit.BlockPosition;
                SelectedRegion = hit.Region;
                RenderPlacementHelper = true;
                SelectedSide = hit.Side;
            }

            if (Input.IsMousePressed(MouseButton.Left))
            {
                if (raycast)
                {
                    Vector3i position = hit.BlockPosition;
                    World.SetBlock(position, Block.Air);
                    IsMining = true;
                }
            }

            if (Input.IsKeyPressed(Key.I))
            {
                var rotation = hit.Block.Rotation();
                var definition = BlockData.BlockDefinitions[hit.Block.ID];
                //BlockPlacement.Key((int)rotation, out var side, out var region, out var facing);
                //Console.WriteLine($"[Info] : Looking at block '{definition.Name}' that is placed on side {side}, region {region} and facing {(BlockFacing)facing}");
            }
        }

        Model?.Update();
        WeaponModel?.Update();
        CurrentState.Update();

        Skybox.LightDirection = World.LightDirection;

        World.Transform.Position.Xz = Transform.Position.Xz;

        if (Input.IsKeyAndControlPressed(Key.P))
        {
            Scene.LoadScene("StructureEditor");
        }

        if (Input.IsKeyPressed(Key.M))
        {
            Console.WriteLine("Generating blocks");
            Info.GenerateBlocks();
        }

        if (Input.IsKeyPressed(Key.F3))
        {
            if (Info.RenderInfo)
                Info.ToggleOff();
            else    
                Info.ToggleOn();
        }
    }

    void LateUpdate()
    {
        if (Input.IsMousePressed(MouseButton.Left) && Game.IsCursorState(CursorMode.Normal))
        {
            Scene.DefaultCamera.SetCameraMode(_oldCameraMode);
            Game.SetCursorState(CursorMode.Disabled);
            _showSettings = false;
        }

        if (Input.IsKeyPressed(Key.Escape))
        {
            _oldCameraMode = Scene.DefaultCamera.GetCameraMode();
            Scene.DefaultCamera.SetCameraMode(CameraMode.Fixed);
            Game.SetCursorState(CursorMode.Normal);
            _showSettings = true;
        }

        if (_oldPlayerPosition != Transform.Position)
        {
            
            _oldPlayerPosition = Transform.Position;
        }

        if (_oldCameraPosition != Scene.DefaultCamera.Position)
        {
            Info.SetPlayerPosition(Scene.DefaultCamera.Position);
            _oldCameraPosition = Scene.DefaultCamera.Position;
        }

        if (Model != null)
        {
            Model.Position = Transform.Position;
        }

        if (WeaponModel != null)
        {
            WeaponModel.Position = Transform.Position;
        }

        if (_showSettings)
        {
            //SettingsManager.SettingsUI.Update();
        }
    }

    void FixedUpdate()
    {
        CurrentState.FixedUpdate();
    }

    void Render()
    {
        Model?.Render();
        WeaponModel?.Render();

        if (RenderPlacementHelper)
        {
            PlacementHelper.Render(Camera, BlockPlacementPosition, SelectedSide, SelectedRegion);
        }

        if (_showSettings)
        {
            //SettingsManager.SettingsUI.RenderNoDepthTest();
        }
    }

    void Exit()
    {
        CurrentState.Exit();

        Scene.DefaultCamera.CanZoom = () => true;
    }

    void Dispose()
    {
        Model?.Delete();
        WeaponModel?.Delete();
    }
}