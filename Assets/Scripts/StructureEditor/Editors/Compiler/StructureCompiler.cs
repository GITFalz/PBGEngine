using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using PBG.MathLibrary;
using PBG.Compiler.Lines;
using PBG.Parse;
using PBG.Compiler;

namespace Compiler;

public static partial class StructureCompiler
{
    public static Variable ScoreVariable;

    static StructureCompiler()
    {
        ScoreVariable = new(Compiler, new(), "SCORE")
        {
            Type = "float",
        };
        ScoreVariable.Result.SetFloat(1);
    }

    public static GameCompiler Compiler = new();
    public static StructureData StructureData = null!;

    public static bool Compile(Variable scoreVariable, GameCompiler compiler, List<string> lines, ref StructureData structureData)
    {
        scoreVariable.Result.SetFloat(1);

        compiler.Clear();
        compiler.Variables.Add("SCORE", scoreVariable);
        void AddVariable(string name, Value result, string type, Func<Value> action)
        {
            var variable = new Variable(compiler, new(), name);
            var function = new FunctionValue();
            function.SetAction(action);
            var expression = new FunctionExpression(function, result);
            variable.Evaluators.Add(expression);
            variable.Type = type;
            variable.Result = result;
            compiler.Variables.Add(name, variable);
            compiler.Lines.Add(variable);
        }
        foreach (var (name, rule) in structureData.RulesetPoints)
        {
            AddVariable($"{name}_HEIGHT", new IntValue(), "int", rule.GetHeight);
            AddVariable($"{name}_X", new IntValue(), "int", rule.GetX);
            AddVariable($"{name}_Y", new IntValue(), "int", rule.GetY);
            AddVariable($"{name}_Z", new IntValue(), "int", rule.GetZ);
        }
        foreach (var (name, connection) in structureData.ConnectionPoints)
        {
            AddVariable($"{name}", new BoolValue(), "bool", connection.GetConnected);
        }
    
        return compiler.Compile(lines);
    }

    /// <summary>
    /// This is the function that is called inside of the structure editor
    /// </summary>
    /// <param name="lines"></param>
    /// <param name="structureData"></param>
    /// <returns></returns>
    public static bool CompileDefault(List<string> lines, ref StructureData structureData)
    {
        return Compile(ScoreVariable, Compiler, lines, ref structureData);
    }

    public static void Run()
    {
        for (int i = 0; i < Compiler.Lines.Count; i++)
        {
            var line = Compiler.Lines[i];
            if (!line.Run())
                return;
        }
    }

    public static void ResetScore() => ScoreVariable.Result.SetFloat(1);
    public static float GetScore() => ScoreVariable.Result.GetFloat();
}
