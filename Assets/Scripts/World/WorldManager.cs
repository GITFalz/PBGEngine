using PBG.MathLibrary;
using PBG.Core;

public class WorldManager : ScriptingNode
{
    public static WorldManager Instance = null!;

    public WorldManager()
    {
        Instance = this;
    }

    public static void SpawnEntity(Vector3 position)
    {
        //var entity = new Entity(position);
        //EntityManager.AddEntity(entity);
    }
}