using System.Reflection;
using PBG.MathLibrary;
using PBG.Modeling;
using PBG.Rendering;
using Silk.NET.Input;

namespace PBG.Core;

public class SceneBlueprintNode(Node parentNode, string name)
{
    public string Name = name;

    public TransformNode Transform = parentNode.AddChild(name);
    public TransformNode? RuntimeNode = null;

    public List<ScriptBlueprint> Scripts = [];
    public List<SceneBlueprintNode> ChildrenNodes = [];  
    public static Dictionary<string, SceneBlueprintNode> ChildrenNodeMap = [];  

    public void Copy(Scene scene, Node parent)
    {
        var newNode = parent.AddChild(Name);

        newNode.Position = Transform.Position;
        newNode.Scale = Transform.Scale;
        newNode.Rotation = Transform.Rotation;

        for (int i = 0; i < Scripts.Count; i++)
        {
            var component = Scripts[i];
            var instance = component.Copy();
            if (instance == null)
                continue;

            newNode.AddComponent(instance);
        }

        for (int i = 0; i < ChildrenNodes.Count; i++)
        {
            var childNode = ChildrenNodes[i];
            childNode.Copy(scene, newNode);
        }

        RuntimeNode = newNode;
    }
    
    public SceneBlueprintNode AddNode(string name)
    {
        string newName = name;
        int count = 1;
        while (ChildrenNodeMap.ContainsKey(newName)) 
        { 
            newName = name + "_" + count; 
            count++; 
        }
        SceneBlueprintNode blueprintNode = new(Transform, newName);
        ChildrenNodes.Add(blueprintNode);
        ChildrenNodeMap.Add(newName, blueprintNode);
        return blueprintNode;
    }

    public bool AddScript(ScriptingNode scriptingNode)
    {   
        var script = new InternalScriptBlueprint(this, scriptingNode);
        Scripts.Add(script);
        Transform.AddComponent(scriptingNode);
        return true;
    }

    public bool AddScript(SceneScriptJson json)
    {   
        ScriptBlueprint script;
        if (_internalScriptParsers.TryGetValue(json.Name, out var action))
        {
            var parser = action.Invoke().Parse(json);;
            var scriptingNode = parser.GetScript();
            script = new InternalScriptBlueprint(this, scriptingNode);
        }
        else
        {
            script = new CustomScriptBlueprint(this, json.Name);
            script.Refresh();
            json.ParseFields(script);
        }
        
        Scripts.Add(script);
        if (script.ScriptingNode == null)
            return false;

        Transform.AddComponent(script.ScriptingNode);
        OrderScripts();
        return true;
    }

    public bool AddScript(string name) => AddScript(name, out _);
    public bool AddScript(string name, out ScriptBlueprint script)
    {   
        if (_internalTypes.TryGetValue(name, out var action))
        {
            script = new InternalScriptBlueprint(this, action.Invoke());
        }
        else
        {
            script = new CustomScriptBlueprint(this, name);
            script.Refresh();
        }
        
        Scripts.Add(script);
        if (script.ScriptingNode == null)
            return false;

        Transform.AddComponent(script.ScriptingNode);
        return true;
    }

    public void RefreshScripts()
    {
        for (int i = 0; i < Scripts.Count; i++)
            Scripts[i].Refresh();
    }

    public void OrderScripts()
    {
        List<ScriptBlueprint> scripts = [];

        for (int i = 0; i < Scripts.Count; i++)
        {
            var script = Scripts[i];
            if (script.Status == ScriptStatus.InternalScript) 
                scripts.Add(script);
        }

        for (int i = 0; i < Scripts.Count; i++)
        {
            var script = Scripts[i];
            if (script.Status != ScriptStatus.InternalScript) 
                scripts.Add(script);
        }

        Scripts = scripts;
    }

    public bool RemoveScript(ScriptBlueprint script)
    {
        if (Scripts.Remove(script) && script.ScriptingNode != null)
        {
            Transform.RemoveScript(script.ScriptingNode);
            return true;
        }
        return false;
    }

    public SceneNodeJson GetJson()
    {
        var json = new SceneNodeJson
        {
            Name = Name,
        };

        if (Transform.Position != Vector3.Zero)         json.Position = [Transform.Position.X, Transform.Position.Y, Transform.Position.Z];
        if (Transform.Rotation != Quaternion.Identity)  json.Rotation = [Transform.Rotation.X, Transform.Rotation.Y, Transform.Rotation.Z, Transform.Rotation.W];
        if (Transform.Scale != Vector3.One)             json.Scale = [Transform.Scale.X, Transform.Scale.Y, Transform.Scale.Z];

        json.Scripts ??= [];
        for (int i = 0; i < Scripts.Count; i++)
            json.Scripts.Add(Scripts[i].GetJson());

        json.Nodes ??= [];
        for (int i = 0; i < ChildrenNodes.Count; i++)
            json.Nodes.Add(ChildrenNodes[i].GetJson());

        return json;
    }

    public void Clear()
    {
        for (int i = 0; i < ChildrenNodes.Count; i++)
            ChildrenNodes[i].Clear();

        ChildrenNodes.Clear();
        ChildrenNodeMap.Clear();
        Scripts.Clear();
        RuntimeNode = null;
    }

    private static readonly Dictionary<string, Func<ScriptingNode>> _internalTypes = new()
    {
        {  "MeshRenderer", () => new MeshRenderer() }
    };

    private static readonly Dictionary<string, Func<InternalScriptParser>> _internalScriptParsers = new()
    {
        {  "MeshRenderer", () => new MeshRendererParser() }
    };

    public abstract class InternalScriptParser
    {
        public abstract InternalScriptParser Parse(SceneScriptJson json);
        public abstract ScriptingNode GetScript();
    }

    public class MeshRendererParser : InternalScriptParser
    {
        private MeshRenderer _renderer = new();

        public override InternalScriptParser Parse(SceneScriptJson json)
        {
            if (json.Fields != null)
            for (int i = 0; i < json.Fields.Count; i++)
            {
                var field = json.Fields[i];
                if (field.Type == "Mesh" && field.Value != null)
                {
                    var path = Game.CurrentProjectPath / field.Value;
                    if (File.Exists(path))
                    {
                        ObjLoader.LoadMesh(path, _renderer);
                    }
                    break;
                }
            }
            return this;
        }
        public override ScriptingNode GetScript() => _renderer;
    }
}
