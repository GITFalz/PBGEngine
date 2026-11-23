namespace PBG.Nodes;

using System.Linq.Expressions;
using PBG.MathLibrary;
using SType = NodeExpressionType;

public class NodeExpressionSimpleBinary : NodeExpression
{
    public readonly NodeExpressionSimpleBinaryType BinaryType;
    public readonly NodeExpression Left;
    public readonly NodeExpression Right;
    public readonly string Operator;
    public readonly bool IsFunction;

    public NodeExpressionSimpleBinary(Type type, NodeExpressionSimpleBinaryType binaryType, NodeExpression left, NodeExpression right, bool isFunction = false) : 
    base(GetCorrectValueType(binaryType, type), SType.Readable | SType.Value | SType.Constant | SType.Isolated)
    {
        BinaryType = binaryType;
        Left = left;
        Right = right;
        IsFunction = isFunction;
    }

    public override Expression GetExpression()
    {
        if (IsFunction)
        {
            Type type = typeof(Mathf);
            string name = _expressionFunctionConversions[BinaryType];

            var method = type.GetMethod(name, [Left.ValueType, Right.ValueType]) ?? throw new MissingMethodException(type.FullName, name);
            return Expression.Call(method, Left.GetExpression(), Right.GetExpression());
        }
        else
        {
            return Expression.MakeBinary(_expressionTypeConversions[BinaryType], Left.GetExpression(), Right.GetExpression());
        }
    }

    public override void Compile(NodeExpressionCompileContext context)
    {
        (bool isFunction, string op) = context.CompileType switch
        {
            NodeExpressionCompileType.GLSL => GetGLSLOperator(),
            NodeExpressionCompileType.Scalar => GetScalarOperator(),
            NodeExpressionCompileType.Avx2 => GetAvx2Operator(),
            _ => throw new Exception()
        };

        if (isFunction)
        {
            context.Write($"{op}(");
            Left.CompileIsolated(context);
            context.Write($", ");
            Right.CompileIsolated(context);
            context.Write($")");
        }
        else
        {
            Left.CompileIsolated(context);
            context.Write($" {op} ");
            Right.CompileIsolated(context);
        }
    }

    private (bool isFunction, string op) GetGLSLOperator()
    {
        return BinaryType switch
        {
            NodeExpressionSimpleBinaryType.Add                => (false, "+"),
            NodeExpressionSimpleBinaryType.Subtract           => (false, "-"),
            NodeExpressionSimpleBinaryType.Multiply           => (false, "*"),
            NodeExpressionSimpleBinaryType.Divide             => (false, "/"),
            NodeExpressionSimpleBinaryType.Modulo             => (true, "mod"),

            NodeExpressionSimpleBinaryType.Max                => (true, "max"),
            NodeExpressionSimpleBinaryType.Min                => (true, "min"),
            NodeExpressionSimpleBinaryType.Power              => (true, "pow"),
            NodeExpressionSimpleBinaryType.Atan2              => (true, "atan2"),

            NodeExpressionSimpleBinaryType.Equal              => (false, "=="),
            NodeExpressionSimpleBinaryType.NotEqual           => (false, "!="),
            NodeExpressionSimpleBinaryType.LessThan           => (false, "<"),
            NodeExpressionSimpleBinaryType.LessThanOrEqual    => (false, "<="),
            NodeExpressionSimpleBinaryType.GreaterThan        => (false, ">"),
            NodeExpressionSimpleBinaryType.GreaterThanOrEqual => (false, ">="),

            NodeExpressionSimpleBinaryType.AndAlso            => (false, "&&"),
            NodeExpressionSimpleBinaryType.OrElse             => (false, "||"),

            _ => throw new InvalidOperationException(
                $"Unsupported binary expression type: {BinaryType}")
        };
    }

    private (bool isFunction, string op) GetScalarOperator()
    {
        return BinaryType switch
        {
            NodeExpressionSimpleBinaryType.Add                => (false, "+"),
            NodeExpressionSimpleBinaryType.Subtract           => (false, "-"),
            NodeExpressionSimpleBinaryType.Multiply           => (false, "*"),
            NodeExpressionSimpleBinaryType.Divide             => (false, "/"),
            NodeExpressionSimpleBinaryType.Modulo             => (true, "MathF.Mod"),

            NodeExpressionSimpleBinaryType.Max                => (true, "max"),
            NodeExpressionSimpleBinaryType.Min                => (true, "min"),
            NodeExpressionSimpleBinaryType.Power              => (true, "pow"),
            NodeExpressionSimpleBinaryType.Atan2              => (true, "atan2"),

            NodeExpressionSimpleBinaryType.Equal              => (false, "=="),
            NodeExpressionSimpleBinaryType.NotEqual           => (false, "!="),
            NodeExpressionSimpleBinaryType.LessThan           => (false, "<"),
            NodeExpressionSimpleBinaryType.LessThanOrEqual    => (false, "<="),
            NodeExpressionSimpleBinaryType.GreaterThan        => (false, ">"),
            NodeExpressionSimpleBinaryType.GreaterThanOrEqual => (false, ">="),

            NodeExpressionSimpleBinaryType.AndAlso            => (false, "&&"),
            NodeExpressionSimpleBinaryType.OrElse             => (false, "||"),

            _ => throw new InvalidOperationException(
                $"Unsupported binary expression type: {BinaryType}")
        };
    }

    private (bool isFunction, string op) GetAvx2Operator()
    {
        return BinaryType switch
        {
            NodeExpressionSimpleBinaryType.Add                => (true, "Avx2Math.Add"),
            NodeExpressionSimpleBinaryType.Subtract           => (true, "Avx2Math.Subtract"),
            NodeExpressionSimpleBinaryType.Multiply           => (true, "Avx2Math.Multiply"),
            NodeExpressionSimpleBinaryType.Divide             => (true, "Avx2Math.Divide"),
            NodeExpressionSimpleBinaryType.Modulo             => (true, "Avx2Math.Modulo"),

            NodeExpressionSimpleBinaryType.Max                => (true, "Avx2Math.Max"),
            NodeExpressionSimpleBinaryType.Min                => (true, "Avx2Math.Min"),
            NodeExpressionSimpleBinaryType.Atan2              => (true, "Avx2Math.Atan2"),

            NodeExpressionSimpleBinaryType.Equal              => (true, "Avx2Math.EqualAll"),
            NodeExpressionSimpleBinaryType.NotEqual           => (true, "Avx2Math.NotEqualAll"),
            NodeExpressionSimpleBinaryType.LessThan           => (true, "Avx2Math.LessThanAll"),
            NodeExpressionSimpleBinaryType.LessThanOrEqual    => (true, "Avx2Math.LessThanOrEqualAll"),
            NodeExpressionSimpleBinaryType.GreaterThan        => (true, "Avx2Math.GreaterThanAll"),
            NodeExpressionSimpleBinaryType.GreaterThanOrEqual => (true, "Avx2Math.GreaterThanOrEqualAll"),

            NodeExpressionSimpleBinaryType.AndAlso            => (true, "&&"),
            NodeExpressionSimpleBinaryType.OrElse             => (true, "||"),

            _ => throw new InvalidOperationException(
                $"Unsupported binary expression type: {BinaryType}")
        };
    }

    private static Type GetCorrectValueType(NodeExpressionSimpleBinaryType binaryType, Type type)
    {
        return binaryType switch
        {
            NodeExpressionSimpleBinaryType.Equal              => typeof(bool),
            NodeExpressionSimpleBinaryType.NotEqual           => typeof(bool),
            NodeExpressionSimpleBinaryType.LessThan           => typeof(bool),
            NodeExpressionSimpleBinaryType.LessThanOrEqual    => typeof(bool),
            NodeExpressionSimpleBinaryType.GreaterThan        => typeof(bool),
            NodeExpressionSimpleBinaryType.GreaterThanOrEqual => typeof(bool),

            NodeExpressionSimpleBinaryType.AndAlso            => typeof(bool),
            NodeExpressionSimpleBinaryType.OrElse             => typeof(bool),

            _ => type
        };
    }

    private static readonly Dictionary<NodeExpressionSimpleBinaryType, ExpressionType> _expressionTypeConversions = new()
    {
        { NodeExpressionSimpleBinaryType.Add,                 System.Linq.Expressions.ExpressionType.Add },
        { NodeExpressionSimpleBinaryType.Subtract,            System.Linq.Expressions.ExpressionType.Subtract },
        { NodeExpressionSimpleBinaryType.Multiply,            System.Linq.Expressions.ExpressionType.Multiply },
        { NodeExpressionSimpleBinaryType.Divide,              System.Linq.Expressions.ExpressionType.Divide },

        { NodeExpressionSimpleBinaryType.Equal,               System.Linq.Expressions.ExpressionType.Equal },
        { NodeExpressionSimpleBinaryType.NotEqual,            System.Linq.Expressions.ExpressionType.NotEqual },
        { NodeExpressionSimpleBinaryType.LessThan,            System.Linq.Expressions.ExpressionType.LessThan },
        { NodeExpressionSimpleBinaryType.LessThanOrEqual,     System.Linq.Expressions.ExpressionType.LessThanOrEqual },
        { NodeExpressionSimpleBinaryType.GreaterThan,         System.Linq.Expressions.ExpressionType.GreaterThan },
        { NodeExpressionSimpleBinaryType.GreaterThanOrEqual,  System.Linq.Expressions.ExpressionType.GreaterThanOrEqual },

        { NodeExpressionSimpleBinaryType.AndAlso,             System.Linq.Expressions.ExpressionType.AndAlso },
        { NodeExpressionSimpleBinaryType.OrElse,              System.Linq.Expressions.ExpressionType.OrElse },
/* Not used yet
        { NodeExpressionSimpleBinaryType.And,                 ExpressionType.And },
        { NodeExpressionSimpleBinaryType.Or,                  ExpressionType.Or }
*/
    };

    private static readonly Dictionary<NodeExpressionSimpleBinaryType, string> _expressionFunctionConversions = new()
    {
        { NodeExpressionSimpleBinaryType.Modulo, "Mod"},
        { NodeExpressionSimpleBinaryType.Max, "Max"},
        { NodeExpressionSimpleBinaryType.Min, "Min"},
        { NodeExpressionSimpleBinaryType.Power, "Power"},
        { NodeExpressionSimpleBinaryType.Atan2, "Atan2"},
/* Not used yet
        { NodeExpressionSimpleBinaryType.And,                 ExpressionType.And },
        { NodeExpressionSimpleBinaryType.Or,                  ExpressionType.Or }
*/
    };
}