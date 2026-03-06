using PBG.Rendering;

public abstract class PlayerGameBase(PlayerGameState gameState, PlayerController player)
{
    public PlayerController Player = player;
    public PlayerGameState GameState = gameState;
    public Camera Camera => Player.Scene.DefaultCamera;

    protected float _yaw
    {
        get => Player.Yaw;
        set => Player.Yaw = value;
    }

    public abstract void Start();
    public abstract void Update();
    public abstract void FixedUpdate();
    public abstract void Exit();
}