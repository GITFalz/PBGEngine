using System.Globalization;
using PBG.Compiler.Lines;

namespace PBG.Compiler;

public class FloatStep(GameCompiler compiler) : BaseStep(InstructionStepType.Float)
{ 
    public float Value;

    public override bool Parse(Variable variable, string part)
    {
        if (!float.TryParse(part, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float result))
        {
            return compiler.LineAnalysis($"'{part}' is not a valid float");
        }

        Value = result;
        return true;
    }

    public override string getType() => "float";
    public override Value GetValue() => new FloatValue() { Value = Value };
}