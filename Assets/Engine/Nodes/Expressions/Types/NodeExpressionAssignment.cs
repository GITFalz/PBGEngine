namespace PBG.Nodes;

using System.Linq.Expressions;
using SType = NodeExpressionType;

public class NodeExpressionAssignment(NodeExpression left, NodeExpression right) : NodeExpression(typeof(void), SType.Statement)
{
    public override Expression GetExpression() => Expression.Assign(left.GetExpression(), right.GetExpression());
    public override void Compile(NodeExpressionCompileContext context)
    {
        if (left.IsType(SType.Writeable) && right.IsType(SType.Readable))
        {
            left.Compile(context);
            context.Write(" = ");
            Convert(right, left.ValueType).Compile(context);
        }
        else
        {
            throw new InvalidOperationException(
                $"Invalid assignment: " +
                $"Target ({left.GetType().Name}, {left.ValueType.Name}, {left.ExpressionType}) is not writeable " + 
                $"or source ({right.GetType().Name}, {right.ValueType.Name}, {right.ExpressionType}) is not readable."
            );
        }
    }
}
