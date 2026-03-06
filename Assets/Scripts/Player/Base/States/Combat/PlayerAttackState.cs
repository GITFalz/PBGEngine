using PBG.Data;
using PBG.MathLibrary;
using PBG.Rendering;
using Silk.NET.Input;

public class PlayerAttackState(PlayerGameState gameState, PlayerController player) : PlayerGameBase(gameState, player)
{
    private float _timer = 0f;

    public override void Start()
    {
        Player.PhysicsBody.DecelerationFactor = 0.1f;

        Player.PhysicsBody.AddForce(Camera.FrontYto0(), 25 * GameState.PlayerPace);

        for (int i = 0; i < EntityManager.Entities.Count; i++)
        {
            var entity = EntityManager.Entities[i];
            var direction = entity.Transform.Position - Player.Transform.Position;
            var dot = Vector3.Dot(direction.Normalized(), Camera.FrontYto0());
            if (dot < 0 || direction.Length > 3.5f * (dot * 0.7f + 0.3f))
            {
                continue;
            }

            if (direction.Length == 0)
            {
                direction = Camera.FrontYto0();
            }
            else
            {
                direction /= direction.Length;
            }

            direction.Y = 0.8f;

            entity.PhysicsBody.AddForce(direction, 1200);
        }

        _timer = 0f;

        Player.WeaponModel?.PlayAnimation("Swing1");
    }

    public override void Update()
    {
        var _input = Input.MovementInput;
        _timer += GameTime.DeltaTime;

        if (_timer >= 0.5f)
        {
            if (_input == Vector2i.Zero)
            {
                Player.WeaponModel?.PlayAnimation("Reset1");
                Player.WeaponModel?.QueueLoopAnimation("Idle");
                GameState.SwitchState(GameState.IdleState);
                return;
            }
            
        }

        if (_timer >= 0.3f)
        {
            if (_input != Vector2i.Zero)
            {
                if (Input.IsKeyDown(Key.ControlLeft))
                {
                    Player.WeaponModel?.PlayAnimation("Reset1");
                    Player.WeaponModel?.QueueLoopAnimation("Idle");
                    GameState.SwitchState(GameState.RunningState);
                }
                else
                {
                    Player.WeaponModel?.PlayAnimation("Reset1");
                    Player.WeaponModel?.QueueLoopAnimation("Idle");
                    GameState.SwitchState(GameState.WalkingState);
                }
                return;
            }
        }
    }
    
    public override void FixedUpdate()
    {

    }
    
    public override void Exit()
    {
        Player.PhysicsBody.DecelerationFactor = 1.0f;
    }
}