using PBG.Data;
using PBG.MathLibrary;
using PBG.Physics;
using PBG.Voxel;
using Silk.NET.Input;
using CameraMode = PBG.Rendering.CameraMode;

public class PlayerGameState : PlayerBaseState
{
    public PlayerGameBase CurrentState = null!;

    public PlayerIdleState IdleState;
    public PlayerWalkingState WalkingState;
    public PlayerRunningState RunningState;
    public PlayerJumpingState JumpingState;
    public PlayerFallingState FallingState;

    // Combat
    public PlayerAttackState AttackState;

    public float PlayerPace = 60f; // the speed at which the player is moving in a horizontal direction

    private float _blockPlaceTimer = 0;

    public PlayerGameState(PlayerController player) : base(player)
    {
        IdleState = new(this, player);
        
        WalkingState = new(this, player);
        RunningState = new(this, player);
        JumpingState = new(this, player);
        FallingState = new(this, player);

        AttackState = new(this, player);

        CurrentState = WalkingState;
    }

    public void SwitchState(PlayerGameBase state)
    {
        CurrentState.Exit();
        CurrentState = state;
        CurrentState.Start();
    }

    public override void Start()
    {
        Console.WriteLine("Enter Player Game State");
        CurrentState.Start();
        Player.PhysicsBody.Gravity = 90;
        _blockPlaceTimer = 0;
    }

    public override void Update()
    {
        Vector3 eyePosition = Player.Transform.Position + Player.FirstPersonCameraOffset;
        if (Camera.GetCameraMode() == CameraMode.Follow)
        {
            Camera.Position = eyePosition;
        }
        else
        {
            Camera.Center = eyePosition;
        }
        
        if (Input.IsKeyPressed(Key.Q))
        {
            Camera.SetCameraMode(CameraMode.Free);
            Player.SwitchState(Player.AdminState);
        }

        if (Input.IsKeyPressed(Key.F5))
        {
            Camera.SetCameraMode(Camera.GetCameraMode() == CameraMode.Follow ? CameraMode.Orbit : CameraMode.Follow);
            if (Player.Model != null) Player.Model.IsShown = Camera.GetCameraMode() == CameraMode.Orbit;
            if (Player.WeaponModel != null) Player.WeaponModel.IsShown = Camera.GetCameraMode() == CameraMode.Orbit;
        }

        /* 
        -- Block interactions (need to be moved to a separate class later) --

        if (VoxelData.Raycast(World, eyePosition, Camera.front, 4, out Hit hit))
        {
            if (!hit.Block.IsAir() && Input.IsMousePressed(MouseButton.Left))
            {
                World.SetBlock(hit.BlockPosition, Block.Air);
            }
            if (_blockPlaceTimer >= 0.2f && !World.GetBlock(hit.BlockPosition + hit.Normal) && Input.IsMouseDown(MouseButton.Right))
            {
                var block = new Block(BlockState.Solid, 0);
                if (!Player.PhysicsBody.CollidesWidthBlockAt(hit.BlockPosition + hit.Normal, block))
                {
                    World.SetBlock(hit.BlockPosition + hit.Normal, block);
                    _blockPlaceTimer = 0;
                }
            }
        }
        */

        CurrentState.Update();

        float targetYaw = Mathf.Lerp(Player.Yaw, -Player.Scene.DefaultCamera.Yaw, 0.5f);
        float diff = Mathf.DeltaAngle(Player.Yaw, targetYaw);
        Player.Yaw += diff * (8.0f * GameTime.DeltaTime);
        var rotation = Quaternion.FromEuler(new Vector3(0, Mathf.DegreesToRadians(Player.Yaw), 0));

        if (Player.Model != null)
        {
            Player.Model.Rotation = rotation;
        }
        
        if (Player.WeaponModel != null)
        {
            Player.WeaponModel.Rotation = rotation;
        }   

        //_blockPlaceTimer += GameTime.DeltaTime;
    }

    public override void FixedUpdate()
    {
        CurrentState.FixedUpdate();
    }

    public override void Exit()
    {
        CurrentState.Exit();
    }

    public float GetInputAngle(Vector2i input) => _inputAngles[(input.X + 1) | ((input.Y + 1) << 2)];
    private readonly int[] _inputAngles = [225, 180, 135, -1, 270, 360, 90, -1, 315, 0, 45];
}