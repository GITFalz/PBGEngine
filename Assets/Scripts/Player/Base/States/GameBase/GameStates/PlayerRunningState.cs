using PBG.Data;
using PBG.MathLibrary;
using Silk.NET.Input;

public class PlayerRunningState(PlayerGameState gameState, PlayerController player) : PlayerGameBase(gameState, player)
{
    private Vector2i _input = Vector2i.Zero;

    public override void Start()
    {
        GameState.PlayerPace = 60f;
    }

    public override void Update()
    {
        _input = Input.MovementInput;

        if (Input.IsKeyDown(Key.Space))
        {
            GameState.SwitchState(GameState.JumpingState);
            return;
        }

        if (Input.IsMousePressed(MouseButton.Left) && !Player.IsMining)
        {
            GameState.SwitchState(GameState.AttackState);
            return;
        }

        if (!Player.PhysicsBody.IsGrounded)
        {
            Player.PhysicsBody.Gravity = 40;
            Player.PhysicsBody.DecelerationFactor = 2.5f;
            GameState.SwitchState(GameState.FallingState);
            return;
        }

        if (Input.IsKeyPressed(Key.ControlLeft))
        {
            GameState.SwitchState(GameState.WalkingState);
            return;
        }

        if (_input == Vector2i.Zero)
        {
            GameState.SwitchState(GameState.IdleState);
            return;
        } 
    }
    
    public override void FixedUpdate()
    {
        if (_input != Vector2i.Zero)
        {   
            Vector2 input = Vector2.Normalize(_input);
            Vector3 right = Player.Scene.DefaultCamera.RightYto0() * -input.X;
            Vector3 front = Player.Scene.DefaultCamera.FrontYto0() * input.Y;
            Player.PhysicsBody.AddForce((right + front) * (float)GameTime.FixedDeltaTime * 3000f, 100f * Mathf.Clampy(Mathf.Abs(input.Y) + Mathf.Abs(input.X * 0.6f), 0, 1));
        }   
    }
    
    public override void Exit()
    {

    }
}