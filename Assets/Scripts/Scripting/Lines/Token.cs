using Compiler;

namespace PBG.Compiler;

public class Token
{
    public string Line = "";
    public int IndexStart = 0;
    public int Count = 0;

    public static implicit operator string(Token t) => t.Line;
    public Token Copy() => new()
    {
        Line = Line,
        IndexStart = IndexStart,
        Count = Count
    };

    public override string ToString() => Line;
}