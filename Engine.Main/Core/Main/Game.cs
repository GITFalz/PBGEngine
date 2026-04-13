namespace PBG.Core;

public abstract class Game(string name)
{
    public readonly string Name = name;
    public abstract List<ScriptingNode> Initialize();
}