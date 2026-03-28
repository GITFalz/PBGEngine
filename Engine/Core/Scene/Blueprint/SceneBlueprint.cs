using PBG.Editor;
using PBG.MathLibrary;
using Silk.NET.SPIRV.Cross;

namespace PBG.Core;

public static class SceneBlueprint
{
    public static string Name = "";
    public static string CurrentPath = "";
    public static Scene BlueprintScene = null!;
    public static Scene? ActiveScene = null;
    public static List<SceneBlueprintNode> ChildrenNodes = [];  
    public static Dictionary<string, SceneBlueprintNode> ChildrenNodeMap = [];  

    public static void Init()
    {
        BlueprintScene ??= new();
    }

    public static Scene CreateScene()
    {
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

        for (int i = 0; i < ChildrenNodes.Count; i++)
            ChildrenNodes[i].Copy(scene, scene.RootNode);

        ActiveScene = scene;
        return scene;
    }   

    public static void RefreshScripts()
    {
        for (int i = 0; i < ChildrenNodes.Count; i++)
            ChildrenNodes[i].RefreshScripts();
    }

    public static SceneBlueprintNode AddNode(string name)
    {
        string newName = name;
        int count = 1;
        while (ChildrenNodeMap.ContainsKey(newName)) 
        { 
            newName = name + "_" + count; 
            count++; 
        }
        SceneBlueprintNode blueprintNode = new(BlueprintScene.RootNode, newName);
        ChildrenNodes.Add(blueprintNode);
        ChildrenNodeMap.Add(newName, blueprintNode);
        return blueprintNode;
    }

    public static SceneBlueprintNode AddOrGetNode(string name)
    {
        if (ChildrenNodeMap.TryGetValue(name, out var node))
            return node;
        return AddNode(name);
    }

    public static SceneBaseJson GetJson()
    {
        var json = new SceneBaseJson()
        {
            Name = Name,
        };

        for (int i = 0; i < ChildrenNodes.Count; i++)
            json.Nodes.Add(ChildrenNodes[i].GetJson());

        return json;
    }

    public static void Clear()
    {
        for (int i = 0; i < ChildrenNodes.Count; i++)
            ChildrenNodes[i].Clear();

        ChildrenNodes = [];
        ChildrenNodeMap = [];
    } 
}
