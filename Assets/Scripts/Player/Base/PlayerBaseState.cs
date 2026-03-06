using PBG.Rendering;
using PBG.Voxel;

public abstract class PlayerBaseState
{
    public PlayerController Player;
    public Camera Camera => Player.Scene.DefaultCamera;
    public VoxelRenderer World => Player.World;

    public PlayerBaseState(PlayerController player)
    {
        Player = player;
    }

    public abstract void Start();
    public abstract void Update();
    public abstract void FixedUpdate();
    public abstract void Exit();
}