using PBG;
using PBG.Core;
using PBG.MathLibrary;
using PBG.Physics;

public class Entity(Vector3 position) : ScriptingNode
{
    public Vector3 Size = (0.7f, 1.8f, 0.7f);
    public Vector3 Position = position;

    public SimpleModel? Model;

    public PhysicsBody PhysicsBody = null!;

    void Start()
    {
        PhysicsBody = Transform.GetComponent<PhysicsBody>();
        PhysicsBody.SetPosition(Position);

        string modelPath = Path.Combine(Game.ModelPath, "player2.model");
        if (File.Exists(modelPath))
        {
            Model = new(modelPath);
        }
    }

    void Update()
    {
        if (Model != null)
        {
            Model.Position = Transform.Position;
            Model.Update();
        }
    }

    void Render()
    {
        Model?.Render();
    }

    void Dispose()
    {
        Model?.Delete();
    }
}