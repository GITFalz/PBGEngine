using PBG.Compiler.Lines;

namespace PBG.Compiler;

public class StringList(GameCompiler compiler, string name, Token token) : BaseString(token)
{
    public Token? ListToken = null;
    public string Name = name;
    public bool Inverted = false;
    public List<BaseString> Parts = [];

    public int Count => Parts.Count;

    public void Add(BaseString s)
    {
        s.Parent = this;
        Parts.Add(s);
    }

    public BaseString this[int index]
    {
        get => Parts[index];
        set => Parts[index] = value;
    }

    public bool IsNotFunction() => Name == "enclosed" || Name == "parameter";

    public override Token LargestToken() => ListToken ?? Token;

    // Step 3: Checking the order of the parts (value, token, value, token, ... , value)
    public bool CheckOrdering(Variable variable, bool skipIfFunction = false)
    {
        if (!skipIfFunction || IsNotFunction())
        {
            for (int i = 0; i < Parts.Count; i++)
            {
                var part = Parts[i];
                if (part is CompilerString s)
                {
                    if (i % 2 == 0) // expects value type (int, float, variable, etc...)
                    {
                        if (s.IsToken()) return compiler.LineError($"Expected a value but found '{s.Token.Line}'", s.Token);
                    }
                    else
                    {
                        if (!s.IsToken()) return compiler.LineError($"Expected a token but found '{s.Token.Line}'", s.Token);
                    }
                }
                else if (part is StringList sl)
                {
                    if (!sl.CheckOrdering(variable, true))
                        return false;
                }
            }

            // If it has all been processed but still nothing wrong, check if the count is even or not, it should be odd
            if (int.IsEvenInteger(Count))
                return Count == 0 ? 
                compiler.LineError("Expected an expression", LargestToken()) : 
                compiler.LineError($"Expected an expression after '{Parts[^1].LargestToken().Line}', but reached end of input", Parts[^1].LargestToken());
        }
        else
        {
            for (int i = 0; i < Parts.Count; i++)
            {
                var part = Parts[i];
                if (part is StringList sl)
                {
                    if (!sl.CheckOrdering(variable, true))
                        return false;
                }
            }
        }      
        return true;
    }

    public void Separate(Variable variable, Func<string, bool> check)
    {
        if (Count == 0)
            return;
        
        if (Count == 1)
        {
            if (Parts[0] is StringList sl)
                sl.Separate(variable, check);
            return;
        }

        Token previousToken = new();
        StringList previous = new(compiler, "enclosed", previousToken);
        CompilerString? sep = null;
        Token nextToken = new();
        StringList next = new(compiler, "enclosed", nextToken);
        List<StringList> lists = [];

        for (int i = 0; i < Parts.Count; i++)
        {
            var part = Parts[i];
            if (part is CompilerString s && sep == null && (i & 1) == 1 && check(s.Token))
            {
                sep = s;
                continue;
            }

            if (part is StringList sl)
            {
                lists.Add(sl);
            }

            if (sep == null)
            {
                previous.Add(part);
                if (previous.Count == 1)
                {
                    previousToken.IndexStart = part.Token.IndexStart;
                    previousToken.Count = part.Token.Count;
                }
                else
                {
                    previousToken.Count = (part.Token.IndexStart + part.Token.Count + 1) - previousToken.IndexStart;
                }
                previousToken.Line += part.Token.Line + " ";
            }
            else
            {
                next.Add(part);
                if (next.Count == 1)
                {
                    nextToken.IndexStart = part.Token.IndexStart;
                    nextToken.Count = part.Token.Count;
                }
                else
                {
                    nextToken.Count = (part.Token.IndexStart + part.Token.Count + 1) - nextToken.IndexStart;
                }
                nextToken.Line += part.Token.Line + " ";
            }
        }

        previousToken.Line = previousToken.Line.Trim();
        nextToken.Line = nextToken.Line.Trim();

        if (sep == null)
        {
            for (int i = 0; i < lists.Count; i++)
            {
                lists[i].Separate(variable, check);
            }
        }
        else
        {
            Parts = [previous, sep, next];
            previous.Separate(variable, check);
            next.Separate(variable, check);
        }
    }

    public override bool TypeChecking(Variable variable, bool ignore = true)
    {
        if (Name == "enclosed" || Name == "parameter" || ignore)
        {
            if (Count == 1)
            {
                if (!Parts[0].TypeChecking(variable, false))
                    return false;

                Type = Parts[0].Type;
                return true;
            }

            if (Count == 3)
            {
                if (Parts[0] is not StringList part1)
                    return compiler.LineError($"The first element in the '{Name}' list should be a list too but isn't, this should not happen", Parts[0].Token);
                
                if (Parts[1] is not CompilerString sup)
                    return compiler.LineError($"The second element in the '{Name}' list should be a string but isn't, this should not happen", Parts[1].Token);

                if (Parts[2] is not StringList part2)
                    return compiler.LineError($"The third element in the '{Name}' list should be a list too but isn't, this should not happen", Parts[2].Token);

                if (!part1.TypeChecking(variable, false) || !part2.TypeChecking(variable, false))
                    return false;

                if (GameCompiler.IsOperator(sup))
                {
                    if (part1.Type != "float" && part1.Type != "int")
                        return compiler.LineError($"Expected number but found '{part1.Type}' before operator", part1.Token);

                    if (part2.Type != "float" && part2.Type != "int")
                        return compiler.LineError($"Expected number but found '{part2.Type}' after operator", part2.Token);

                    Type = (part1.Type == "float" || part2.Type == "float") ? "float" : "int";
                    Result = Type == "float" ? new FloatValue() : new IntValue();
                    return true;
                }
                if (GameCompiler.IsCheck(sup))
                {
                    if (part1.Type != "float" && part1.Type != "int")
                        return compiler.LineError($"Expected number but found '{part1.Type}' before check", part1.Token);

                    if (part2.Type != "float" && part2.Type != "int")
                        return compiler.LineError($"Expected number but found '{part2.Type}' after check", part2.Token);

                    Type = "bool";
                    Result = new BoolValue();
                    return true;
                }
                if (GameCompiler.IsLogical(sup))
                {
                    if (part1.Type != "bool")
                        return compiler.LineError($"Expected bool but found '{part1.Type}' before logical operator", part1.Token);

                    if (part2.Type != "bool")
                        return compiler.LineError($"Expected bool but found '{part2.Type}' after logical operator", part2.Token);

                    Type = "bool";
                    Result = new BoolValue();
                    return true;
                }
            }

            return compiler.LineError($"There are {Count} elements in the '{Name}' list, this should not happen", Token);
        }

        if (Name == "print")
        {
            for (int i = 0; i < Parts.Count; i++)
            {
                var part = Parts[i];
                if (!part.TypeChecking(variable, false))
                    return false;
            }
            
            return true;
        }

        if (!GameCompiler.Functions.TryGetValue(Name, out int parameterCount))
            return compiler.LineError($"Function '{Name}' does not exist", Token);

        if (Count != parameterCount)
            return compiler.LineError($"Expected '{parameterCount}' parameter{(parameterCount == 1 ? "" : 's')} but found '{Count}'", Token);

        for (int i = 0; i < Parts.Count; i++)
        {
            var part = Parts[i];
            if (!part.TypeChecking(variable, false))
                return false;
            
            if (part.Type != "float" && part.Type != "int") // for now functions only expect floats/ints as parameters
                return compiler.LineError($"Functions only take numbers as parameters but found '{part.Type}'", part.Token);
        }

        Type = "float";
        return true;
    }

    public void GetEvaluatorList(List<Expression> evaluators, out Value value, bool ignore = false)
    {
        if (Name == "enclosed" || Name == "parameter" || ignore)
        {
            if (Count == 1)
            {
                var part = Parts[0];
                if (part is CompilerString s)
                {
                    value = s.GetValue();
                    return;
                }
                if (part is StringList sl)
                {
                    sl.GetEvaluatorList(evaluators, out value);
                    return;
                }
            }

            if (Count == 3)
            {
                if (Result == null)
                    throw new InvalidCastException("Result is null for some reason");

                if (Parts[0] is not StringList part1)
                    throw new InvalidCastException("Part 0 is not a list for some reason");
                
                if (Parts[1] is not CompilerString sup)
                    throw new InvalidCastException("Part 1 is not a list for some reason");

                if (Parts[2] is not StringList part2)
                    throw new InvalidCastException("Part 2 is not a list for some reason");

                part1.GetEvaluatorList(evaluators, out var a);
                part2.GetEvaluatorList(evaluators, out var b);

                value = Result;

                if (GameCompiler.IsOperator(sup))
                {
                    var operation = new Operation(sup, a, b, value);
                    evaluators.Add(operation);
                    return;
                }
                if (GameCompiler.IsCheck(sup))
                {
                    var check = new Check(sup, a, b, value);
                    evaluators.Add(check);
                    return;
                }
                if (GameCompiler.IsLogical(sup))
                {
                    var logic = new LogicalOperator(sup, a, b, value);
                    evaluators.Add(logic);
                    return;
                }

                throw new Exception($"Unknown operator '{sup}'");
            }

            throw new Exception("Some failed when trying to get evaluator list");
        }

        if (Name == "print")
        {
            List<Value> parameters = [];

            for (int i = 0; i < Parts.Count; i++)
            {
                var part = Parts[i];
                if (part is CompilerString s)
                {
                    parameters.Add(s.GetValue());
                }
                else if (part is StringList sl)
                {
                    sl.GetEvaluatorList(evaluators, out var v);
                    parameters.Add(v);
                }
            }

            var function = new FunctionValue();
            function.SetPrint(parameters);
            value = function;
        }
        else
        {
            if (!GameCompiler.Functions.TryGetValue(Name, out int parameterCount))
                throw new Exception($"Couldn't find function {Name}");

            if (Count != parameterCount)
                throw new Exception($"Expected '{parameterCount}' parameter{(parameterCount == 1 ? "" : 's')} but found '{Count}'");

            Value[] parameters = new Value[parameterCount];

            for (int i = 0; i < Parts.Count; i++)
            {
                var part = Parts[i];
                if (part is CompilerString s)
                {
                    parameters[i] = s.GetValue();
                }
                else if (part is StringList sl)
                {
                    sl.GetEvaluatorList(evaluators, out var v);
                    parameters[i] = v;
                }
            }

            if (!ignore)
            {
                var function = new FunctionValue();
                function.SetAction(Name, parameters);
                value = function;
            }
            else
            {
                var result = new FloatValue();
                var function = new FunctionValue();
                function.SetAction(Name, parameters);
                var expression = new FunctionExpression(function, result);
                evaluators.Add(expression);
                value = result;
            }
        }
    }

    public override string ToString(int indent)
    {
        var pad = new string(' ', indent);

        var result = $"{pad}List: {Name}\n";

        foreach (var item in Parts)
        {
            result += item.ToString(indent + 2) + "\n";
        }

        return result.TrimEnd();
    }
}