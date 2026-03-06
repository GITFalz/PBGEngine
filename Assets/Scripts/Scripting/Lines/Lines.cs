using System.Diagnostics.CodeAnalysis;
using PBG.MathLibrary;

namespace PBG.Compiler.Lines
{
    public class Executor
    {
        public List<Line> Lines = [];
        
        public void Run()
        {
            for (int i = 0; i < Lines.Count; i++)
            {
                var line = Lines[i];
                if (!line.Run())
                    return;
            }
        }
    }

    public abstract class Line(Token token)
    {
        public Parent? Parent = null;
        public Token Token = token;

        public abstract bool Run();
        public abstract void Clear();
    }

    public abstract class Parent(GameCompiler compiler, Token token) : Line(token)
    {
        public List<Line> Lines = [];
        public Dictionary<string, Variable> Variables = [];

        public bool TryGetVariable(string name, [NotNullWhen(true)] out Variable? variable)
        {
            if (Variables.TryGetValue(name, out variable))
                return true;
            if (Parent == null)
                return compiler.Variables.TryGetValue(name, out variable);
            return Parent.TryGetVariable(name, out variable);
        }

        public void ParentClear()
        {
            for (int i = 0; i < Lines.Count; i++)
            {
                Lines[i].Clear();
            }

            foreach (var (_, variable) in Variables)
            {
                variable.Clear();
            }
        }
    }

    public class Variable(GameCompiler compiler, Token token, string name) : Line(token)
    {
        public string Name = name;
        public int Index = 0;
        public string Type = "var";

        public List<Expression> Evaluators = [];
        public Value Result = new FloatValue();

        public bool TryGetVariable(string name, [NotNullWhen(true)] out Variable? variable)
        {
            if (Parent == null)
                return compiler.Variables.TryGetValue(name, out variable);
            return Parent.TryGetVariable(name, out variable);
        }

        public override bool Run()
        {
            for (int i = 0; i < Evaluators.Count; i++)
            {
                var line = Evaluators[i];
                line.Evaluate();
            }
            return true;
        }

        public override void Clear()
        {
            Evaluators = [];
        }
    }

    public class If : Parent
    {
        public Variable TestVariable;

        public If(GameCompiler compiler, Token token, Variable variable) : base(compiler, token)
        {
            TestVariable = variable;
        }

        public override bool Run()
        {   
            TestVariable.Run();
            if (!TestVariable.Result.GetBool())
                return true;

            for (int i = 0; i < Lines.Count; i++)
            {
                var line = Lines[i];
                if (!line.Run())
                    return false;
            }
            return true;
        }

        public override void Clear()
        {
            ParentClear();
            TestVariable.Clear();
        }
    }

    public class Print(Token token, Variable variable, FunctionValue value) : Line(token)
    {
        public Variable PrintVariable = variable;
        public FunctionValue Value = value;

        public override bool Run()
        {
            PrintVariable.Run();
            Value.Action.Invoke();
            return true;
        }

        public override void Clear() => PrintVariable.Clear();
    }

    public class Return : Line
    {
        public Return() : base(new Token()) {}
        public override bool Run() => false;
        public override void Clear() {}
    }


    public abstract class Value
    {
        public bool Invert = false;
        public Expression? Parent = null;

        public abstract float GetFloat();
        public abstract void SetFloat(float value);

        public abstract int GetInt();
        public abstract void SetInt(int value);

        public abstract bool GetBool();
        public abstract void SetBool(bool value);
        public abstract void SetValue(Value value);
    }

    public class FloatValue : Value
    {
        public float Value = 0;

        public FloatValue() {}
        public FloatValue(float value) => Value = value;

        public override float GetFloat() => Value;
        public override void SetFloat(float value) => Value = value;

        public override int GetInt() => Value.Fti();
        public override void SetInt(int value) => Value = value;

        public override bool GetBool() { return false; }
        public override void SetBool(bool value) {}
        public override void SetValue(Value value) => Value = value.GetFloat();

        public override string ToString() => ""+Value;
    }

    public class IntValue : Value
    {
        public int Value = 0;

        public IntValue() {}
        public IntValue(int value) => Value = value;

        public override float GetFloat() => Value;
        public override void SetFloat(float value) => Value = value.Fti();

        public override int GetInt() => Value;
        public override void SetInt(int value) => Value = value;

        public override bool GetBool() { return false; }
        public override void SetBool(bool value) {}
        public override void SetValue(Value value) => Value = value.GetFloat().Fti();

        public override string ToString() => ""+Value;
    }

    public class BoolValue : Value
    {
        public bool Value = false;

        public BoolValue() {}
        public BoolValue(bool value) => Value = value;
        public BoolValue(bool inverted, bool value) { Invert = inverted; Value = value; }

        public override float GetFloat() => Value ? 1 : 0;
        public override void SetFloat(float value) => Value = value == 1;

        public override int GetInt() => Value ? 1 : 0;
        public override void SetInt(int value) => Value = value == 1;

        public override bool GetBool() => Invert ? !Value : Value;
        public override void SetBool(bool value) => Value = value;
        public override void SetValue(Value value) => Value = value.GetBool();

        public override string ToString() => ""+Value;
    }

    public class FunctionValue : Value
    {
        public Func<Value> Action = () => new FloatValue();

        public override float GetFloat() => Action.Invoke().GetFloat();
        public override void SetFloat(float value) {}

        public override int GetInt() => Action.Invoke().GetInt();
        public override void SetInt(int value) {}

        public override bool GetBool() => Action.Invoke().GetBool();
        public override void SetBool(bool value) {}
        public override void SetValue(Value value) => throw new Exception("HUH? can't set the value of a function");

        public override string ToString() => ""+Action.Invoke();

        public void SetAction(Func<Value> action)
        {
            Action = action;
        }

        public void SetPrint(List<Value> parameters)
        {
            Action = () =>
            {
                string print = "(";
                for (int i = 0; i < parameters.Count; i++)
                {
                    if (i != 0)
                        print += ", ";
                    print += parameters[i];
                }
                Console.WriteLine(print + ")");
                return new FloatValue();
            };
        }

        public void SetAction(string name, Value[] parameters)
        {
            Action = name switch
            {
                "abs"      => () => new FloatValue(Abs(parameters[0].GetFloat())),
                "floor"    => () => new FloatValue(Floor(parameters[0].GetFloat())),
                "ceil"     => () => new FloatValue(Ceil(parameters[0].GetFloat())),
                "round"    => () => new FloatValue(Round(parameters[0].GetFloat())),
                "sqrt"     => () => new FloatValue(Sqrt(parameters[0].GetFloat())),
                "pow"      => () => new FloatValue(Pow(parameters[0].GetFloat(), parameters[1].GetFloat())),
                "log"      => () => new FloatValue(Log(parameters[0].GetFloat(), parameters[1].GetFloat())),
                "log2"     => () => new FloatValue(Log2(parameters[0].GetFloat())),
                "exp"      => () => new FloatValue(Exp(parameters[0].GetFloat())),
                "min"      => () => new FloatValue(Min(parameters[0].GetFloat(), parameters[1].GetFloat())),
                "max"      => () => new FloatValue(Max(parameters[0].GetFloat(), parameters[1].GetFloat())),
                "clamp"    => () => new FloatValue(Clamp(parameters[0].GetFloat(), parameters[1].GetFloat(), parameters[2].GetFloat())),
                "sin"      => () => new FloatValue(Sin(parameters[0].GetFloat())),
                "cos"      => () => new FloatValue(Cos(parameters[0].GetFloat())),
                "tan"      => () => new FloatValue(Tan(parameters[0].GetFloat())),
                "asin"     => () => new FloatValue(Asin(parameters[0].GetFloat())),
                "acos"     => () => new FloatValue(Acos(parameters[0].GetFloat())),
                "atan"     => () => new FloatValue(Atan(parameters[0].GetFloat())),
                "atan2"    => () => new FloatValue(Atan2(parameters[0].GetFloat(), parameters[1].GetFloat())),
                "radians"  => () => new FloatValue(Radians(parameters[0].GetFloat())),
                "degrees"  => () => new FloatValue(Degrees(parameters[0].GetFloat())),
                "lerp"     => () => new FloatValue(Lerp(parameters[0].GetFloat(), parameters[1].GetFloat(), parameters[2].GetFloat())),
                "sign"     => () => new FloatValue(Sign(parameters[0].GetFloat())),
                "fract"    => () => new FloatValue(Fract(parameters[0].GetFloat())),
                "mod"      => () => new FloatValue(Mod(parameters[0].GetFloat(), parameters[1].GetFloat())),
                "distance" => () => new FloatValue(Distance(parameters[0].GetFloat(), parameters[1].GetFloat())),
                _          => () => new FloatValue()
            };
        }

        private static float Abs(double x) => (float)Math.Abs(x);
        private static float Floor(double x) => (float)Math.Floor(x);
        private static float Ceil(double x) => (float)Math.Ceiling(x);
        private static float Round(double x) => (float)Math.Round(x);
        private static float Sqrt(double x) => (float)Math.Sqrt(x);
        private static float Pow(double a, double b) => (float)Math.Pow(a, b);
        private static float Log(double @base, double x) => (float)Math.Log(x, @base);
        private static float Log2(double x) => (float)Math.Log2(x);
        private static float Exp(double x) => (float)Math.Exp(x);
        private static float Min(double a, double b) => (float)Math.Min(a, b);
        private static float Max(double a, double b) => (float)Math.Max(a, b);
        private static float Clamp(double x, double min, double max) => (float)Math.Clamp(x, min, max);
        private static float Sin(double x) => (float)Math.Sin(x);
        private static float Cos(double x) => (float)Math.Cos(x);
        private static float Tan(double x) => (float)Math.Tan(x);
        private static float Asin(double x) => (float)Math.Asin(x);
        private static float Acos(double x) => (float)Math.Acos(x);
        private static float Atan(double x) => (float)Math.Atan(x);
        private static float Atan2(double y, double x) => (float)Math.Atan2(y, x);
        private static float Radians(double deg) => (float)(deg * (Math.PI / 180.0));
        private static float Degrees(double rad) => (float)(rad * (180.0 / Math.PI));
        private static float Lerp(double a, double b, double t) => (float)(a + (b - a) * t);
        private static float Sign(double x) => (float)Math.Sign(x);
        private static float Fract(double x) => (float)(x - Math.Floor(x));
        private static float Mod(double a, double b)
        {
            double m = a % b;
            return (float)((m < 0) ? m + Math.Abs(b) : m);
        }
        private static float Distance(double a, double b) => (float)Math.Abs(a - b);
    }

    public abstract class Expression
    {
        public abstract void Evaluate();
    }

    public class FunctionExpression : Expression
    {
        public FunctionValue Function;
        public Value Result;

        public FunctionExpression(FunctionValue function, Value result)
        {
            function.Parent = this;
            result.Parent = this;

            Function = function;
            Result = result;
        }

        public override void Evaluate()
        {
            Result.SetValue(Function);
        }
    }

    public abstract class Evaluator : Expression
    {
        public Value A;
        public Value B;
        public Value Result;

        public Evaluator(Value a, Value b, Value result)
        {
            a.Parent = this;
            b.Parent = this;
            result.Parent = this;

            A = a;
            B = b;
            Result = result;
        }
    }

    public class Operation : Evaluator
    {
        public Func<float, float, float> Action;

        public Operation(string operation, Value a, Value b, Value result) : base(a, b, result)
        {
            Action = operation switch
            {
                "+" => Add,
                "-" => Min,
                "*" => Times,
                "/" => Divide,
                "%" => Mod,
                _ => throw new InvalidOperationException($"Unknown operation '{operation}'")
            };
        }

        public override void Evaluate()
        {
            Result.SetFloat(Action.Invoke(A.GetFloat(), B.GetFloat()));
        }

        //["+", "-", "*", "/", "%"];
        private static float Add(float a, float b) => a + b;
        private static float Min(float a, float b) => a - b;
        private static float Times(float a, float b) => a * b;
        private static float Divide(float a, float b) => a / b;
        private static float Mod(float a, float b) => a % b;
    }

    public class Check : Evaluator
    {
        public Func<float, float, bool> Action;

        public Check(string check, Value a, Value b, Value result) : base(a, b, result)
        {
            Action = check switch
            {
                "==" => Equal,
                "<" => Inferior,
                ">" => Superior,
                "<=" => InferiorOrEqual,
                ">=" => SuperiorOrEqual,
                "!=" => NotEqual,
                _ => throw new InvalidOperationException($"Unknown check '{check}'")
            };
        }

        public override void Evaluate()
        {
            Result.SetBool(Action.Invoke(A.GetFloat(), B.GetFloat()));
        }

        //["==", "<", ">", "<=", ">=", "!="]
        private static bool Equal(float a, float b) => a == b;
        private static bool Inferior(float a, float b) => a < b;
        private static bool Superior(float a, float b) => a > b;
        private static bool InferiorOrEqual(float a, float b) => a <= b;
        private static bool SuperiorOrEqual(float a, float b) => a >= b;
        private static bool NotEqual(float a, float b) => a != b;
    }

    public class LogicalOperator : Evaluator
    {
        public Func<bool, bool, bool> Action;

        public LogicalOperator(string logical, Value a, Value b, Value result) : base(a, b, result)
        {
            Action = logical switch
            {
                "&&" => And,
                "||" => Or,
                _ => throw new InvalidOperationException($"Unknown logical operator '{logical}'")
            };
        }

        public override void Evaluate()
        {
            Result.SetBool(Action.Invoke(A.GetBool(), B.GetBool()));
        }

        //["&&", "||"]
        private static bool And(bool a, bool b) => a && b;
        private static bool Or(bool a, bool b) => a || b;
    }

    public class Setter : Evaluator
    {
        public Setter(Value a, Value result) : base(a, new IntValue(), result) { }

        public override void Evaluate()
        {
            Result.SetValue(A);
        }
    }
}