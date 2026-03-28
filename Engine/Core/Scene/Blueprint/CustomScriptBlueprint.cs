using PBG.Editor;

namespace PBG.Core;

public class CustomScriptBlueprint : ScriptBlueprint
{
    public override bool IsScriptValid => Status == ScriptStatus.Instanced;

    public CustomScriptBlueprint(SceneBlueprintNode parent, string name) : base(parent, name) {}

    public override ScriptingNode? CreateInstance()
    {
        if (HotReloadManager.Get(Name, out var reloader))
        {
            Console.WriteLine($"Found script '{Name}'");
            var script = reloader.CreateInstance();
            Status = ScriptStatus.Instanced;
            return script;
        }
        else if (HotReloadManager.IsOld(Name, out _))
        {
            Console.WriteLine($"Found old script '{Name}'");
            Status = ScriptStatus.IsOld;
            return null;
        }
        else
        {
            Console.WriteLine($"Didn't find script '{Name}'");
            Status = ScriptStatus.NotFound;
            return null;
        }
    }

    public override void Refresh() => ScriptingNode = CreateInstance();
}
