namespace PBG.Core;

public class InternalScriptBlueprint : ScriptBlueprint
{
    public override bool IsScriptValid => true;

    public InternalScriptBlueprint(SceneDefinitionNode parent, ScriptingNode script) : base(parent, script.GetType().Name)
    {
        ScriptingNode = script;
        Status = ScriptStatus.InternalScript;
    }

    public override ScriptingNode? CreateInstance()
    {
        if (ScriptingNode == null)
            throw new Exception("[Error] : Scripting node should not be null if it is set as Internal, contact dev");

        var instance = (ScriptingNode)Activator.CreateInstance(ScriptingNode.GetType())!;
        return instance;
    }

    public override void Refresh() {}
}
