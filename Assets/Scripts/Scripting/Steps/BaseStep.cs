using PBG.Compiler.Lines;
namespace PBG.Compiler;

public abstract class BaseStep(InstructionStepType type)
{
    public InstructionStepType Type = type;

    public abstract bool Parse(Variable variable, string part);
    public abstract string getType();
    public abstract Value GetValue();
}

public enum InstructionStepType
{
    Variable = 1, // simple variable name
    Float = 2, // float (duh)
    Int = 4, // int (duh)
    Bool = 8, // bool (duh)
    Connection = 16,
    Ruleset = 32, // ruleset saved in the structure data written as 'rulesetname'.'variablename'
}