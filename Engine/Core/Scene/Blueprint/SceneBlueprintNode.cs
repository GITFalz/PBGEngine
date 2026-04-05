using PBG.MathLibrary;

namespace PBG.Core;

public class SceneBlueprintNode : SceneDefinitionNode
{
    public List<ScriptBlueprint> Scripts = [];

    public override int ScriptCount => Scripts.Count;

    public SceneBlueprintNode(Node parentNode, string name) : base(parentNode, name) {}

    public override ScriptDefinition[] GetScripts()
    {
        ScriptDefinition[] scripts = new ScriptDefinition[Scripts.Count];
        for (int i = 0; i < Scripts.Count; i++)
            scripts[i] = Scripts[i];
        return scripts;
    }

    public SceneRuntimeNode Copy(Scene scene, Node parent)
    {
        var runtimeNode = new SceneRuntimeNode(parent, Name);

        runtimeNode.Transform.Position = Transform.Position;
        runtimeNode.Transform.Scale = Transform.Scale;
        runtimeNode.Transform.Rotation = Transform.Rotation;

        for (int i = 0; i < Scripts.Count; i++)
        {
            var component = Scripts[i];
            var instance = component.Copy();
            if (instance == null)
                continue;

            var node = new ScriptRuntime(runtimeNode, component.Name)
            {
                ScriptingNode = instance
            };
            runtimeNode.Scripts.Add(node);
            runtimeNode.Transform.AddComponent(instance);
        }

        for (int i = 0; i < ChildrenNodes.Count; i++)
        {
            var childNode = ChildrenNodes[i];
            if (childNode is SceneBlueprintNode blueprintNode)
            {
                var node = blueprintNode.Copy(scene, runtimeNode.Transform);
                runtimeNode.ChildrenNodes.Add(node);
                runtimeNode.ChildrenNodeMap.Add(node.Name, node);
            }
        }

        return runtimeNode;
    }

    public override void AddScript(ScriptDefinition scriptDefinition)
    {
        if (scriptDefinition is ScriptBlueprint script)
            Scripts.Add(script);
    }

    public override void OrderScripts()
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

    public override bool RemoveScript(ScriptDefinition script)
    {
        if (script is ScriptBlueprint sc && Scripts.Remove(sc) && script.ScriptingNode != null)
        {
            Transform.RemoveScript(script.ScriptingNode);
            return true;
        }
        return false;
    }

    public override void RefreshScripts()
    {
        for (int i = 0; i < Scripts.Count; i++)
            Scripts[i].Refresh();
    }

    public override void Clear()
    {
        for (int i = 0; i < ChildrenNodes.Count; i++)
            ChildrenNodes[i].Clear();

        for (int i = 0; i < Scripts.Count; i++)
            RemoveScript(Scripts[i]);

        ChildrenNodes.Clear();
        ChildrenNodeMap.Clear();
        Scripts.Clear();
    }
}
