using PBG.Data;
using PBG.MathLibrary;
using Silk.NET.Input;

public class PlayerIdleState(PlayerGameState gameState, PlayerController player) : PlayerGameBase(gameState, player)
{
    public override void Start()
    {
        GameState.PlayerPace = 0f;
    }

    public override void Update()
    {
        if (Input.MovementInput != Vector2.Zero)
        {
            if (Input.IsKeyDown(Key.ControlLeft))
                GameState.SwitchState(GameState.RunningState);
            else
                GameState.SwitchState(GameState.WalkingState);
            return;
        }

        if (Input.IsMousePressed(MouseButton.Left) && !Player.IsMining)
        {
            GameState.SwitchState(GameState.AttackState);
            return;
        }

        if (Input.IsKeyDown(Key.Space))
        {
            GameState.SwitchState(GameState.JumpingState);
            return;
        }

        if (!Player.PhysicsBody.IsGrounded)
        {
            Player.PhysicsBody.Gravity = 40;
            Player.PhysicsBody.DecelerationFactor = 2.5f;
            GameState.SwitchState(GameState.FallingState);
            return;
        }
    }
    
    public override void FixedUpdate()
    {

    }
    
    public override void Exit()
    {

    }
}