using PBG.Core;
using PBG.Physics;

public class EntityManager : ScriptingNode
{
    public static EntityManager Instance = null!;

    public static List<Entity> Entities = [];

    public EntityManager()
    {
        Instance = this;
    }

    public static void AddEntity(Entity entity)
    {
        Entities.Add(entity);
        
        var physicsBody = new PhysicsBody();
        var transform = Instance.Transform.AddChild("Entity");
        transform.AddComponent(physicsBody, entity);
    }
}