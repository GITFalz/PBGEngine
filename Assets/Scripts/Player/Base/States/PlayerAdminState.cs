using PBG;
using PBG.Data;
using PBG.Rendering;


public class PlayerAdminState : PlayerBaseState
{
    public PlayerAdminState(PlayerController player) : base(player) {}


    public override void Start()
    {
        Console.WriteLine("Enter Player Admin State");
    }

    public override void Update()
    {
        if (Input.IsKeyPressed(Key.E))
        {
            if (Game.GetCursorState() == CursorMode.Disabled)
            {
                Game.SetCursorState(CursorMode.Normal);
                Camera.SetCameraMode(CameraMode.Fixed);
            }
            else if (Game.GetCursorState() == CursorMode.Normal)
            {
                Game.SetCursorState(CursorMode.Disabled);
                Camera.SetCameraMode(CameraMode.Free);
            }
        }
        
        if (Input.IsKeyPressed(Key.G))
        {
            Camera.SetCameraMode(PBG.Rendering.CameraMode.Follow);
            Player.SwitchState(Player.GameState);
        }

        Player.Transform.Position = Camera.Position;
    }

    public override void FixedUpdate()
    {
        
    }

    public override void Exit()
    {

    }   
}