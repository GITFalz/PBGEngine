using PBG.MathLibrary;
using PBG.Modeling;
using PBG.Rendering;

namespace PBG.Core;

public abstract class SceneDefinitionNode
{
    public string Name;
    public TransformNode Transform;

    public List<SceneDefinitionNode> ChildrenNodes = [];  
    public Dictionary<string, SceneDefinitionNode> ChildrenNodeMap = [];

    public abstract int ScriptCount { get; }

    public SceneDefinitionNode(Node parentNode, string name)
    {
        Name = name;
        Transform = parentNode.AddChild(name);
    }

    public SceneDefinitionNode(TransformNode node)
    {
        Name = node.Name;
        Transform = node;
    }

    public abstract ScriptDefinition[] GetScripts();
    public abstract void AddScript(ScriptDefinition scriptDefinition);

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
        AddScript(script);
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
        
        AddScript(script);
        if (script.ScriptingNode == null)
            return false;

        Transform.AddComponent(script.ScriptingNode);
        OrderScripts();
        Console.WriteLine("There are " + ScriptCount + " Scripts");
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
        
        AddScript(script);
        if (script.ScriptingNode == null)
            return false;

        Transform.AddComponent(script.ScriptingNode);
        return true;
    }


    public abstract void OrderScripts();
    public abstract bool RemoveScript(ScriptDefinition script);
    public abstract void RefreshScripts();
    public abstract void Clear();

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
        foreach (var script in GetScripts())
            json.Scripts.Add(script.GetJson());

        json.Nodes ??= [];
        foreach (var child in ChildrenNodes)
            if (child is SceneBlueprintNode blueprintNode)
                json.Nodes.Add(blueprintNode.GetJson());

        return json;
    }

    protected static readonly Dictionary<string, Func<ScriptingNode>> _internalTypes = new()
    {
        {  "MeshRenderer", () => new MeshRenderer() }
    };

    protected static readonly Dictionary<string, Func<InternalScriptParser>> _internalScriptParsers = new()
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