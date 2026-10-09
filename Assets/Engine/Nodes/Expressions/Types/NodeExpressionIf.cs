using System.Linq.Expressions;
using System.Reflection;

namespace PBG.Nodes;

using SType = NodeExpressionType;

public class NodeExpressionIf(NodeExpression expression, NodeExpressionBlock ifTrue) : NodeExpression(typeof(void), SType.Statement)
{
    public override Expression GetExpression()
    {
        return Expression.IfThen(expression.GetExpression(), ifTrue.GetExpression());
    }

    public override void Compile(NodeExpressionCompileContext context)
    {
        context.Write("if (");
        expression.Compile(context);
        context.Write(")");
        ifTrue.Compile(context);
    }
}
