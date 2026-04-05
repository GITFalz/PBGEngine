namespace PBG.Core;

public class ScriptRuntime(SceneDefinitionNode parent, string name) : ScriptDefinition(parent, name)
{
    public override bool IsScriptValid => true;
}
