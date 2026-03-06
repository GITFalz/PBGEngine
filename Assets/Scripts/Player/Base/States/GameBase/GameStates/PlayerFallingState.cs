using PBG.Data;
using PBG.MathLibrary;
using Silk.NET.Input;

public class PlayerFallingState(PlayerGameState gameState, PlayerController player) : PlayerGameBase(gameState, player)
{
    private Vector2 _input = Vector2.Zero;

    public override void Start()
    {
    }

    public override void Update()
    {
        _input = Input.MovementInput;
    
        if (Player.PhysicsBody.IsGrounded)
        {
            if (Input.IsKeyDown(Key.ControlLeft))
                GameState.SwitchState(GameState.RunningState);
            else
                GameState.SwitchState(GameState.WalkingState);
            return;
        }
    }
    
    public override void FixedUpdate()
    {
        Player.PhysicsBody.Gravity = Mathf.Lerp(Player.PhysicsBody.Gravity, 120, 2f * (float)GameTime.FixedDeltaTime);
        Player.PhysicsBody.DecelerationFactor = Mathf.Lerp(Player.PhysicsBody.DecelerationFactor, 1, 2f * (float)GameTime.FixedDeltaTime);

        if (_input != Vector2.Zero)
        {   
            Vector2 input = Vector2.Normalize(_input);
            Vector3 right = Player.Scene.DefaultCamera.RightYto0() * -input.X * 0.5f;
            Vector3 front = Player.Scene.DefaultCamera.FrontYto0() * input.Y;
            Player.PhysicsBody.AddForce(right + front, 30f);
        }  
    }
    
    public override void Exit()
    {
        Player.PhysicsBody.Gravity = 120;
        Player.PhysicsBody.DecelerationFactor = 1;
    }
}