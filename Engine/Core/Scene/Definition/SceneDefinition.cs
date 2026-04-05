using System.Diagnostics.CodeAnalysis;
using PBG.Editor;

namespace PBG.Core;

public abstract class SceneDefinition
{
    public static Scene BlueprintScene = null!;
    public static SceneBlueprint Blueprint = null!;

    public static Scene? ActiveScene = null;
    public static SceneRuntime? Active = null!;

    public static string Name = "";
    public static string CurrentPath = "";

    public List<SceneDefinitionNode> ChildrenNodes = [];  
    public Dictionary<string, SceneDefinitionNode> ChildrenNodeMap = [];  

    public static void Init()
    {
        BlueprintScene ??= new();
        Blueprint ??= new ();
    }

    public static Scene CreateScene()
    {
        Active = new();
        Console.WriteLine($"Creating scene '{Name}'");
        if (Scene.Scenes.TryGetValue(Name, out var oldScene))
        {
            oldScene.SetGameCamera();
            oldScene.Exit();
            oldScene.Dispose();
            Scene.Scenes.Remove(Name);
        }
        
        Scene scene = new(Name);
        scene.SetCamera(EditorManager.Instance.Camera);

        for (int i = 0; i < Blueprint.ChildrenNodes.Count; i++)
        {
            var blueprintNode = Blueprint.ChildrenNodes[i];
            if (blueprintNode is SceneBlueprintNode blueprintNode1)
            {
                var node = blueprintNode1.Copy(scene, scene.RootNode);
                Active.ChildrenNodes.Add(node);
                Active.ChildrenNodeMap.Add(node.Name, node);
            }
        }

        ActiveScene = scene;
        return scene;
    }   

    public void RefreshScripts()
    {
        for (int i = 0; i < ChildrenNodes.Count; i++)
            ChildrenNodes[i].RefreshScripts();
    }

    public abstract SceneDefinitionNode AddNode(string name);

    public bool RemoveNode(string name, [NotNullWhen(true)] out SceneDefinitionNode? node)
    {
        node = null;
        if (!ChildrenNodeMap.TryGetValue(name, out var n))
            return false;
            
        ChildrenNodeMap.Remove(name);
        ChildrenNodes.Remove(n);
        node = n;
        return true;
    }

    public SceneDefinitionNode AddOrGetNode(string name)
    {
        if (ChildrenNodeMap.TryGetValue(name, out var node))
            return node;
        return AddNode(name);
    }

    public SceneBaseJson GetJson()
    {
        var json = new SceneBaseJson()
        {
            Name = Name,
        };

        foreach (var child in ChildrenNodes)
            json.Nodes.Add(child.GetJson());

        return json;
    }

    public void Clear()
    {
        for (int i = 0; i < ChildrenNodes.Count; i++)
            ChildrenNodes[i].Clear();

        ChildrenNodes = [];
        ChildrenNodeMap = [];
    } 
}