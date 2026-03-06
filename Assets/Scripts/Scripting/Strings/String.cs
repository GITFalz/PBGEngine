using PBG.Compiler.Lines;

namespace PBG.Compiler;

public class CompilerString(GameCompiler compiler, Token token) : BaseString(token)
{
    public BaseStep? Step;

    public bool IsToken() => GameCompiler.IsToken(Token);
    public bool IsOperator() => GameCompiler.IsOperator(Token);
    public bool IsCheck() => GameCompiler.IsCheck(Token);
    public bool IsLogical() => GameCompiler.IsLogical(Token);

    public override bool TypeChecking(Variable variable, bool ignore = true)
    {
        if (Parse(variable, new FloatStep(compiler))) return true;
        if (Parse(variable, new IntStep(compiler))) return true;
        if (Parse(variable, new BoolStep(compiler))) return true;
        if (Parse(variable, new VariableStep(compiler))) return true;
        return compiler.LineError($"Was not able to parse '{Token.Line}', this should not happen", Token);
    }

    public override Token LargestToken() => Token;

    private bool Parse(Variable variable, BaseStep step)
    {
        if (!step.Parse(variable, Token))
            return false;

        Step = step;
        Type = step.getType();
        return true;
    }

    public Value GetValue()
    {
        if (Step == null)
            throw new Exception("Step is null for unknown reason");

        return Step.GetValue();
    }

    public static implicit operator string(CompilerString s) => s.Token;

    public override string ToString(int indent)
    {
        return new string(' ', indent) + $"String: \"{Token.Line}\"";
    }
}