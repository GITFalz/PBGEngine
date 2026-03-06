using PBG.Compiler.Lines;

namespace PBG.Compiler;

public abstract class BaseString(Token token)
{
    public StringList? Parent;
    public Token Token = token;
    public string Type = "";
    public Value? Result = null;

    public abstract bool TypeChecking(Variable variable, bool ignore = true);
    public abstract Token LargestToken();

    public override string ToString() => ToString(0);
    public virtual string ToString(int indent)
    {
        return new string(' ', indent) + "(BaseString)";
    }
}