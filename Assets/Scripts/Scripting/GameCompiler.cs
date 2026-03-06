
using System.Text;
using PBG.Compiler.Lines;
using PBG.MathLibrary;

namespace PBG.Compiler;

public class GameCompiler
{    
    public CompileData CompileData = new();
    public int LineIndex = 0;
    public List<Line> Lines = [];
    public Dictionary<string, Variable> Variables = [];
    private HashSet<If> Ifs = [];

    public bool Compile(List<string> lines)
    {
        Parent? currentParent = null;

        for (int i = 0; i < lines.Count; i++)
        {
            LineIndex = i;
            var line = lines[i];
            int trailingSpaces = line.TakeWhile(char.IsWhiteSpace).Count();
            line = line.Trim();

            if (string.IsNullOrEmpty(line))
                continue;

            var newLine = SpaceTokens(line);
            newLine = newLine.Replace("(", "( ");
            newLine = newLine.Replace("-", " -");
            newLine = CompilerFormater.MyRegex().Replace(newLine, " ");
            
            // Split the line into multiple parts
            var Parts = newLine.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (Parts.Length == 0)
                continue;

            List<Token> tokens = [];

            var index = 0;
            for (int j = 0; j < Parts.Length; j++)
            {
                var p = Parts[j].Trim();
                if (p.Length == 0)
                    continue;

                while (index < line.Length && line[index] != p[0])
                {
                    index++;
                }

                tokens.Add(new Token()
                {
                    Line = p,
                    IndexStart = index + trailingSpaces,
                    Count = p.Length
                });

                index+=p.Length;
            }

            bool soeLength(int count) => tokens.Count >= count;
            bool isLength(int count) => tokens.Count == count;

            bool isType = false;
            string variableType = "var";

            var first = tokens[0];

            if (first == "float")
            {
                isType = true;
                variableType = "float";
            }

            if (first == "bool")
            {
                isType = true;
                variableType = "bool";
            }

            if (first == "int")
            {
                isType = true;
                variableType = "int";
            }

            if (isType || first == "var") // This is the start of the declaration of a variable
            {
                if (!soeLength(2)) // The length has to be at least 2
                    return LineError("'var' must be followed by an identifier", tokens[0]);
                
                if (StartsWithNumber(tokens[1]))
                    return LineError("an identifier cannot start with a number", tokens[1]);
                
                if (!StartsWithLetter(tokens[1]))
                    return LineError($"'{tokens[1]}' is not a valid identifier", tokens[1]);
                
                if (isLength(2) && variableType == "var")
                    return LineError($"cannot infer type for var", first);

                if (isLength(3))
                {
                    if (tokens[2] == "=")
                        return LineError("expected value after '='", tokens[2]);
                    return LineError("expected '=' after variable name", tokens[2]);
                }   
                
                if (tokens.Count <= 2)
                    return LineError("expected '=' after variable name", tokens[1]);

                if (tokens[2] != "=")
                    return LineError("expected '=' after variable name", tokens[2]);

                if (Variables.ContainsKey(tokens[1]))
                    return LineError($"Variable '{tokens[1]}' cannot be defined twice", tokens[1]);

                var fullLineToken = GetCombinedToken(tokens, 0, tokens.Count);
                var variable = new Variable(this, fullLineToken, tokens[1]);

                if (currentParent == null)
                    Variables[tokens[1]] = variable;
                else    
                    currentParent.Variables[tokens[1]] = variable;
                    
                variable.Parent = currentParent;
                variable.Index = Variables.Count;
                variable.Type = variableType;

                StringList steps = new(this, tokens[1], tokens[1]);
                if (!CompileExpression(variable, tokens, steps, 3, tokens.Count))
                    return false;

                if (!VariableTypeCheck(variable, steps.Type, tokens[1]))
                    return false;

                steps.GetEvaluatorList(variable.Evaluators, out variable.Result, true);
                if (currentParent == null)
                    Lines.Add(variable);
                else    
                    currentParent.Lines.Add(variable);
                
            }
            else if (first == "if")
            {
                if (tokens[^1].Line.Trim() != "then")
                    return LineError("An if statement must end with 'then'", first);

                List<Token> variableCopy = [..tokens];
                variableCopy.RemoveAt(0);
                variableCopy.RemoveAt(variableCopy.Count - 1);

                if (variableCopy.Count == 0)
                    return LineError("Expected statement inside if but none found", first);

                var fullLineToken = GetCombinedToken(tokens, 0, tokens.Count);
                var variable = new Variable(this, fullLineToken, "if");

                variable.Parent = currentParent;
                variable.Type = "bool";

                Token token = GetCombinedToken(variableCopy, 0, variableCopy.Count);

                StringList steps = new(this, "if", token);
                if (!CompileExpression(variable, variableCopy, steps, 0, variableCopy.Count))
                    return false;

                if (!VariableTypeCheck(variable, steps.Type, token))
                    return false;

                steps.GetEvaluatorList(variable.Evaluators, out variable.Result, true);

                If ifclass = new(this, token, variable);
                ifclass.Parent = currentParent;

                Ifs.Add(ifclass);
                if (currentParent == null)
                    Lines.Add(ifclass);
                else    
                    currentParent.Lines.Add(ifclass);

                currentParent = ifclass;
            }
            else if (first == "end")
            {
                if (tokens.Count > 1)
                {
                    var token = GetCombinedToken(tokens, 1, tokens.Count);
                    LineWarning($"'{token.Line}' is unknown and will not be compiled", token);
                }

                if (currentParent == null)
                    return LineError($"Trying to close unknown if", first);

                currentParent = currentParent.Parent;
            }
            else if (first == "print(")
            {
                var token = GetCombinedToken(tokens, 0, tokens.Count);
                var variable = new Variable(this, token, "print");

                StringList steps = new(this, "print", token);
                if (!CompileExpression(variable, tokens, steps, 0, tokens.Count))
                    return false;

                steps.GetEvaluatorList(variable.Evaluators, out variable.Result, true);

                if (variable.Result is not FunctionValue fv)
                    throw new Exception($"Something went wrong compiling print, variable result is '{variable.Result.GetType().Name}'");

                Print print = new(token, variable, fv);
                print.Parent = currentParent;

                if (currentParent == null)
                    Lines.Add(print);
                else    
                    currentParent.Lines.Add(print);
            }
            else if (first == "return")
            {
                if (tokens.Count > 1)
                {
                    var token = GetCombinedToken(tokens, 1, tokens.Count);
                    LineWarning($"'{token.Line}' is unknown and will not be compiled", token);
                }

                if (currentParent == null)
                    Lines.Add(new Return());
                else    
                    currentParent.Lines.Add(new Return());
            }
            else if ((currentParent != null && currentParent.TryGetVariable(first, out var v)) || Variables.TryGetValue(first, out v))
            {
                if (isLength(1))
                    return LineError("expected '=' after variable name", first);

                if (isLength(2))
                {
                    if (tokens[1] == "=")
                        return LineError("expected value after '='", tokens[1]);
                    return LineError("expected '=' after variable name", tokens[1]);
                }   

                if (tokens[1] != "=")
                    return LineError("expected '=' after variable name", tokens[1]);

                var fullLineToken = GetCombinedToken(tokens, 0, tokens.Count);
                var variable = new Variable(this, fullLineToken, tokens[1]);

                variable.Parent = currentParent;
                variable.Index = Variables.Count;
                variable.Type = v.Type;

                StringList steps = new(this, tokens[0], tokens[0]);
                if (!CompileExpression(variable, tokens, steps, 2, tokens.Count))
                    return false;

                if (!VariableTypeCheck(variable, steps.Type, tokens[1]))
                    return false;

                steps.GetEvaluatorList(variable.Evaluators, out Value a, true);
                variable.Result = v.Result;
                variable.Evaluators.Add(new Setter(a, variable.Result));
                
                if (currentParent == null)
                    Lines.Add(variable);
                else    
                    currentParent.Lines.Add(variable);
            }
            else
            {
                var token = GetCombinedToken(tokens, 0, tokens.Count);
                LineWarning($"'{token.Line}' is unknown and will not be compiled", token);
            }
        } 

        if (currentParent != null)
            return LineError($"If does not have a closing statement", currentParent.Token);

        return true;
    }

    public void Clear()
    {
        CompileData.Clear();
        LineIndex = 0;
        Lines = [];
        Variables = [];
        Ifs = [];
    }

    private static readonly HashSet<string> _tokens = [")", ",", "=", "+", "-", "*", "/", "%", "==", "<", ">", "<=", ">=", "!=", "&&", "||"];
    private static readonly HashSet<string> _operator = ["+", "-", "*", "/", "%"];
    private static readonly HashSet<string> _additive = ["+", "-"];
    private static readonly HashSet<string> _multiplicative = ["*", "/", "%"];
    private static readonly HashSet<string> _checks = ["==", "<", ">", "<=", ">=", "!="];
    private static readonly HashSet<string> _logical = ["&&", "||"];

    public static bool IsToken(string s) => _tokens.Contains(s);
    public static bool IsOperator(string s) => _operator.Contains(s);
    public static bool IsAdditive(string s) => _additive.Contains(s);
    public static bool IsMultiplicative(string s) => _multiplicative.Contains(s);
    public static bool IsCheck(string s) => _checks.Contains(s);
    public static bool IsLogical(string s) => _logical.Contains(s);

    public static string SpaceTokens(string input)
    {
        var sorted = _tokens.OrderByDescending(t => t.Length).ToList();
        sorted.Remove("-");
        var result = new StringBuilder();
        int i = 0;
        
        while (i < input.Length)
        {
            bool matched = false;
            
            foreach (var token in sorted)
            {
                if (i + token.Length <= input.Length && 
                    input.Substring(i, token.Length) == token)
                {
                    result.Append($" {token} ");
                    i += token.Length;
                    matched = true;
                    break;
                }
            }
            
            if (!matched)
            {
                result.Append(input[i]);
                i++;
            }
        }
        
        return string.Join(' ', result.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public static Token GetFunctionContextToken(List<Token> tokens, Token baseToken, int start)
    {
        int openParam = 0;
        int k;
        Token t;
        Token token = baseToken.Copy();
        
        for (k = start; k < tokens.Count; k++)
        {
            t = tokens[k];
            token.Line += t.Line + " ";
            if (t.Line.Contains('('))
            {
                openParam++;
            }
            else if (t.Line == ")")
            {
                if (openParam == 0)
                    break;
                openParam--;
            }
        }

        k = Mathf.Min(k, tokens.Count - 1);
        t = tokens[k];
        token.Count = (t.IndexStart + t.Count) - token.IndexStart;
        token.Line = token.Line.Trim();
        return token;
    }

    public static Token GetCombinedToken(List<Token> tokens, int start, int count)
    {
        Token token = new();
        start.ClampSety(0, tokens.Count);
        count.ClampSety(0, tokens.Count);
        if (start >= count)
            return token;

        Token last = tokens[start];
        token = last.Copy();

        for (int i = start+1; i < count; i++)
        {
            last = tokens[i];
            token.Line += last.Line;
            token.IndexStart.MinSet(last.IndexStart);
        }

        token.Count = (last.IndexStart + last.Count) - token.IndexStart;
        return token;
    }

    public bool CompileExpression(Variable variable, List<Token> tokens, StringList steps, int start, int count)
    {
        var currentList = steps;
        start.ClampSety(0, tokens.Count);
        count.ClampSety(0, tokens.Count);

        for (int p = start; p < count; p++)
        {
            var token = tokens[p];
            var part = token.Line;
            if (part.EndsWith('(')) // If the part is the start of a function
            {
                if (p + 1 == tokens.Count)
                    return LineError($"function isn't closed properly", token);

                bool inverted = false;
                if (part.StartsWith('!'))
                {
                    inverted = true;
                    part = part[1..];
                }
                
                string name = (part.Length == 1) ? "enclosed" : part.Split('(')[0];
                var functionToken = GetFunctionContextToken(tokens, token, p+1);
                var list = new StringList(this, name, token)
                {
                    ListToken = functionToken,
                    Inverted = inverted
                };

                token.Count = Mathf.Max(1, token.Count - 1);
                currentList.Add(list);
                currentList = list;
                
                var next = tokens[p+1].Line;
                if (next == ")")
                {
                    if (currentList.Parent == null)
                        return LineError($"there are too many closing parentheses", tokens[p+1]);
                }
                else if (next == ",")
                {
                    if (name == "enclosed")
                        return LineError($"cannot have ',' outside of a function", tokens[p+1]);

                    return LineError($"a parameter is missing in function", tokens[p+1]);
                }
                else if (name != "enclosed")
                {
                    Token parameterToken = GetFunctionContextToken(tokens, tokens[p+1], p+1);
                    var parameterList = new StringList(this, "parameter", parameterToken);
                    currentList.Add(parameterList);
                    currentList = parameterList;
                }
                continue;
            }

            if (part == ",") // if it is a parameter seperator
            {
                if (p + 1 == tokens.Count)
                    return LineError($"missing code in function", token);
                
                var next = tokens[p+1];
                if (next == ")" || next == ",")
                    return LineError($"a parameter is missing in function", tokens[p+1]);
            
                if (currentList.Parent == null || currentList.Name == "enclosed")
                    return LineError($"cannot have ',' outside of a function", tokens[p+1]);

                Token parameterToken = GetFunctionContextToken(tokens, tokens[p+1], p+1);
                currentList = currentList.Parent;
                var parameterList = new StringList(this, "parameter", parameterToken);
                currentList.Add(parameterList);
                currentList = parameterList;
                continue;
            }

            if (part == ")") // if part is a closing function
            {
                if (currentList.Name == "parameter")
                {
                    if (currentList.Parent == null)
                        return LineError($"there are too many closing parentheses", token);

                    currentList = currentList.Parent;
                }

                if (currentList.Parent == null)
                    return LineError($"there are too many closing parentheses", token);

                currentList = currentList.Parent;
                continue;
            }

            currentList.Add(new CompilerString(this, token));
        }
        
        if (steps != currentList)
            return LineError($"function isn't closed properly", tokens[^1]);

        if (!steps.CheckOrdering(variable))
            return false;

        steps.Separate(variable, s => s == "||");
        steps.Separate(variable, s => s == "&&");
        steps.Separate(variable, IsCheck);
        steps.Separate(variable, IsAdditive);
        steps.Separate(variable, IsMultiplicative);

        if (!steps.TypeChecking(variable))
            return false;
        return true;
    }

    public bool VariableTypeCheck(Variable variable, string type, Token errorToken)
    {
        if (variable.Type == "var")
        {
            variable.Type = type;
        }
        else if (variable.Type == "bool" && type != "bool")
        {
            return LineError($"Type mismatch: expected 'bool' but got '{type}'", errorToken);
        }
        else if (variable.Type == "int")
        {
            if (type == "float")
                return LineError($"Type mismatch: cannot assign 'float' to 'int'", errorToken);
            if (type == "bool")
                return LineError($"Type mismatch: expected 'int' but got 'bool'", errorToken);
        }
        else if (variable.Type == "float" && type == "bool")
        {
            return LineError($"Type mismatch: expected 'float' but got 'bool'", errorToken);
        }
        return true;
    }

    public static readonly Dictionary<string, int> Functions = new()
    {
        { "abs", 1 },
        { "floor", 1 },
        { "ceil", 1 },
        { "round", 1 },
        { "sqrt", 1 },
        { "pow", 2 },
        { "log", 2 },      // Natural log (1 arg). If you want log(base, x) → change to 2.
        { "log2", 1 },      // Natural log (1 arg). If you want log(base, x) → change to 2.
        { "exp", 1 },
        { "min", 2 },
        { "max", 2 },
        { "clamp", 3 },
        { "sin", 1 },
        { "cos", 1 },
        { "tan", 1 },
        { "asin", 1 },
        { "acos", 1 },
        { "atan", 1 },     // atan2 is the 2-arg version
        { "atan2", 2 },
        { "radians", 1 },
        { "degrees", 1 },
        { "lerp", 3 },     // a, b, t
        { "sign", 1 },
        { "fract", 1 },
        { "mod", 2 },
        { "distance", 2 }  // distance(a, b)
    };

    
    public bool LineAnalysis(string line) 
    { 
        CompileData.AnalysisLog.Add(new("error", line, new(), LineIndex)); 
        return false; 
    }
    public bool LineError(string line, Token token)
    {
        CompileData.ErrorLog.Add(new("error", line, token, LineIndex)); 
        return false; 
    }
    public void LineWarning(string line, Token token) => CompileData.WarningLog.Add(new("warning", line, token, LineIndex)); 

    public static bool StartsWithNumber(string s) => !string.IsNullOrEmpty(s) && char.IsDigit(s[0]);
    public static bool StartsWithLetter(string s) => !string.IsNullOrEmpty(s) && char.IsLetter(s[0]);
    public static bool IsOnlyNumbers(string s) => !string.IsNullOrEmpty(s) && s.All(char.IsDigit);
}

public struct CompileData
{
    public List<CompilerLog> AnalysisLog = [];
    public List<CompilerLog> ErrorLog = [];
    public List<CompilerLog> WarningLog = [];

    public CompileData() {}
    public void Clear()
    {
        AnalysisLog = [];
        ErrorLog = [];
        WarningLog = [];
    }
    public void Print()
    {
        Console.WriteLine("\n--- Compiler Log ---");
        foreach (var line in AnalysisLog)
        {
            Console.WriteLine(line.Line);
        }
        foreach (var line in ErrorLog)
        {
            Console.WriteLine(line.Line);
        }
        Console.WriteLine();
    }
}

public struct CompilerLog
{
    public string Type;
    public string Line;
    public Token Token;
    public int Index;

    public Vector4 Color;

    public CompilerLog(string type, string line, Token token, int index)
    {
        Type = type;
        Line = line;
        Token = token;
        Index = index;
        Color = _colors.TryGetValue(type, out var c) ? c : new(0, 1, 0, 0.3f);
    }

    private static readonly Dictionary<string, Vector4> _colors = new()
    {
        { "error" , new(1, 0, 0, 0.3f) },
        { "warning" , new(1, 1, 0, 0.3f) }
    };
}
