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
    base(type, SType.Readable | SType.Value | SType.Constant | SType.Isolated)
    {
        BinaryType = binaryType;
        Left = left;
        Right = right;
        Operator = GetOperator();
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
        if (IsFunction)
        {
            context.Write($"{Operator}(");
            Left.CompileIsolated(context);
            context.Write($", ");
            Right.CompileIsolated(context);
            context.Write($")");
        }
        else
        {
            Left.CompileIsolated(context);
            context.Write($" {Operator} ");
            Right.CompileIsolated(context);
        }
    }

    private string GetOperator()
    {
        return BinaryType switch
        {
            NodeExpressionSimpleBinaryType.Add                => "+",
            NodeExpressionSimpleBinaryType.Subtract           => "-",
            NodeExpressionSimpleBinaryType.Multiply           => "*",
            NodeExpressionSimpleBinaryType.Divide             => "/",
            NodeExpressionSimpleBinaryType.Modulo             => "mod",

            NodeExpressionSimpleBinaryType.Max                => "max",
            NodeExpressionSimpleBinaryType.Min                => "min",
            NodeExpressionSimpleBinaryType.Power              => "pow",
            NodeExpressionSimpleBinaryType.Atan2              => "atan2",

            NodeExpressionSimpleBinaryType.Equal              => "==",
            NodeExpressionSimpleBinaryType.NotEqual           => "!=",
            NodeExpressionSimpleBinaryType.LessThan           => "<",
            NodeExpressionSimpleBinaryType.LessThanOrEqual    => "<=",
            NodeExpressionSimpleBinaryType.GreaterThan        => ">",
            NodeExpressionSimpleBinaryType.GreaterThanOrEqual => ">=",

            NodeExpressionSimpleBinaryType.AndAlso            => "&&",
            NodeExpressionSimpleBinaryType.OrElse             => "||",

            _ => throw new InvalidOperationException(
                $"Unsupported binary expression type: {BinaryType}")
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