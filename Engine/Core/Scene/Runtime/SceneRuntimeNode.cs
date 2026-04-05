namespace PBG.Core;

public class SceneRuntimeNode : SceneDefinitionNode
{
    public List<ScriptRuntime> Scripts = [];
    public override int ScriptCount => Scripts.Count;

    public SceneRuntimeNode(Node parentNode, string name) : base(parentNode, name) {}
    public SceneRuntimeNode(TransformNode node) : base(node) {}

    public override ScriptDefinition[] GetScripts()
    {
        ScriptDefinition[] scripts = new ScriptDefinition[Scripts.Count];
        for (int i = 0; i < Scripts.Count; i++)
            scripts[i] = Scripts[i];
        return scripts;
    }

    public void UpdateActiveNodes(TransformNode transform)
    {
        var childrenNodeMap = ChildrenNodeMap;

        ChildrenNodeMap = [];
        ChildrenNodes = [];

        for (int i = 0; i < transform.Children.Count; i++)
        {
            var child = transform.Children[i];
            if (childrenNodeMap.TryGetValue(child.Name, out var node))
            {
                ChildrenNodeMap.Add(child.Name, node);
                ChildrenNodes.Add(node);
                childrenNodeMap.Remove(child.Name);
            }
            else
            {
                node = new SceneRuntimeNode(child);
                ChildrenNodes.Add(node);
                ChildrenNodeMap.Add(node.Name, node);
            }

            for (int j = 0; j < transform.Components.Count; j++)
            {
                
            }
        }

        foreach (var (_, child) in childrenNodeMap)
            child.Clear();
    }

    public override void AddScript(ScriptDefinition scriptDefinition)
    {
        if (scriptDefinition is ScriptRuntime script)
            Scripts.Add(script);
    }

    public override void OrderScripts()
    {
        List<ScriptRuntime> scripts = [];

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
        if (script is ScriptRuntime sc && Scripts.Remove(sc) && script.ScriptingNode != null)
        {
            Transform.RemoveScript(script.ScriptingNode);
            return true;
        }
        return false;
    }

    public override void RefreshScripts() {}
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