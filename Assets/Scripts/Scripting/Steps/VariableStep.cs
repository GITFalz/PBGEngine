using PBG.Compiler.Lines;
namespace PBG.Compiler;

public class VariableStep(GameCompiler compiler) : BaseStep(InstructionStepType.Variable)
{ 
    public string Name = ""; 
    public string type = "";
    public bool Invert = false;

    public Variable? Variable;

    public override bool Parse(Variable currentVariable, string part)
    {
        Invert = false;
        if (part.StartsWith('!'))
        {
            Invert = true;
            part = part[1..];
        }
        
        if (!currentVariable.TryGetVariable(part, out var variable))
            return compiler.LineAnalysis($"Variable '{part}' does not exist");

        Name = variable.Name;
        type = variable.Type;
        Variable = variable;
        return true;
    }

    public override string getType() => type;
    public override Value GetValue() => Variable?.Result ?? throw new Exception("Variable is null for some reason");
}