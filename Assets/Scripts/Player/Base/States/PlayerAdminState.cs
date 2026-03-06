using PBG.Data;
using Silk.NET.Input;

public class PlayerAdminState : PlayerBaseState
{
    public PlayerAdminState(PlayerController player) : base(player) {}


    public override void Start()
    {
        Console.WriteLine("Enter Player Admin State");
    }

    public override void Update()
    {
        if (Input.IsKeyPressed(Key.G))
        {
            Camera.SetCameraMode(PBG.Rendering.CameraMode.Follow);
            Player.SwitchState(Player.GameState);
        }
    }

    public override void FixedUpdate()
    {
        
    }

    public override void Exit()
    {
        
    }   
}