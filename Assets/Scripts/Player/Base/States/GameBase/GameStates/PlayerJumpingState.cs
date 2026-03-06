using PBG.Data;
using PBG.MathLibrary;
using Silk.NET.Input;

public class PlayerJumpingState(PlayerGameState gameState, PlayerController player) : PlayerGameBase(gameState, player)
{
    private Vector2 _input = Vector2.Zero;
    private float _timer = 0;

    public override void Start()
    {
        Player.PhysicsBody.AddForce((0, 850, 0));
        Player.PhysicsBody.Gravity = 40;
        Player.PhysicsBody.DecelerationFactor = 2.5f;
        _timer = 0.1f;
    }

    public override void Update()
    {
        _input = Input.MovementInput;

        if (_timer > 0)
        {
            _timer -= GameTime.DeltaTime;
        }
        else
        {
            if (Player.PhysicsBody.IsGrounded)
            {
                if (Input.IsKeyDown(Key.ControlLeft))
                    GameState.SwitchState(GameState.RunningState);
                else
                    GameState.SwitchState(GameState.WalkingState);
                return;
            }
        }

        if (!Player.PhysicsBody.IsGrounded && Player.Transform.Position.Y < 0)
        {
            GameState.SwitchState(GameState.FallingState);
            return;
        }
    }
    
    public override void FixedUpdate()
    {
        if (_input != Vector2.Zero)
        {   
            Vector2 input = Vector2.Normalize(_input);
            Vector3 right = Player.Scene.DefaultCamera.RightYto0() * -input.X;
            Vector3 front = Player.Scene.DefaultCamera.FrontYto0() * input.Y;
            Player.PhysicsBody.AddForce(right + front, GameState.PlayerPace * 0.6f * Mathf.Clampy(Mathf.Abs(input.Y) + Mathf.Abs(input.X * 0.6f), 0, 1));
        }  
    }
    
    public override void Exit()
    {
        Player.PhysicsBody.Gravity = 90;
        Player.PhysicsBody.DecelerationFactor = 1;
    }
}