namespace PBG.Core;

public class SceneBlueprint : SceneDefinition
{
    public static SceneBlueprintNode? SelectedNode = null;

    public override SceneDefinitionNode AddNode(string name)
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
}
