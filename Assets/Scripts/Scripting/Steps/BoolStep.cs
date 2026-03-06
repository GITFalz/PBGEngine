using PBG.Compiler.Lines;

namespace PBG.Compiler;

public class BoolStep(GameCompiler compiler) : BaseStep(InstructionStepType.Bool)
{ 
    public bool State;

    public override bool Parse(Variable variable, string part)
    {
        if (part == "true")
        {
            State = true;
            return true;
        }
        if (part == "false")
        {
            State = false;
            return true;
        }
        if (part == "!true")
        {
            State = false;
            return true;
        }
        if (part == "!false")
        {
            State = true;
            return true;
        }
        return compiler.LineAnalysis($"'{part}' is not a valid bool");
    }

    public override string getType() => "bool";
    public override Value GetValue() => new BoolValue() { Value = State };
}