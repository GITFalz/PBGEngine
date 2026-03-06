using System.Globalization;
using PBG.Compiler.Lines;

namespace PBG.Compiler;

public class IntStep(GameCompiler compiler) : BaseStep(InstructionStepType.Int)
{ 
    public int Value;

    public override bool Parse(Variable variable, string part)
    {
        if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            return compiler.LineAnalysis($"'{part}' is not a valid int");

        Value = result;
        return true;
    }

    public override string getType() => "int";
    public override Value GetValue() => new IntValue() { Value = Value };
}