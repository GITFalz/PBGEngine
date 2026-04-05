namespace PBG.Core;

public class SceneRuntime : SceneDefinition
{
    public override SceneDefinitionNode AddNode(string name)
    {
        string newName = name;
        int count = 1;
        while (ChildrenNodeMap.ContainsKey(newName)) 
        { 
            newName = name + "_" + count; 
            count++; 
        }
        SceneRuntimeNode blueprintNode = new(ActiveScene!.RootNode, newName);
        ChildrenNodes.Add(blueprintNode);
        ChildrenNodeMap.Add(newName, blueprintNode);
        return blueprintNode;
    }

    public void UpdateActiveNodes()
    {
        if (ActiveScene == null || Active == null)
            return;

        var childrenNodeMap = Active.ChildrenNodeMap;

        Active.ChildrenNodeMap = [];
        Active.ChildrenNodes = [];

        for (int i = 0; i < ActiveScene.RootNode.Children.Count; i++)
        {
            var child = ActiveScene.RootNode.Children[i];
            if (childrenNodeMap.TryGetValue(child.Name, out var node))
            {
                Active.ChildrenNodeMap.Add(child.Name, node);
                Active.ChildrenNodes.Add(node);
                childrenNodeMap.Remove(child.Name);
            }
            else
            {
                node = new SceneRuntimeNode(child);
                ChildrenNodes.Add(node);
                ChildrenNodeMap.Add(node.Name, node);
            }
        }

        foreach (var (_, child) in childrenNodeMap)
            child.Clear();
    }
}
