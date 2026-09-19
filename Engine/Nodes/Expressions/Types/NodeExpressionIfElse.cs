using System.Linq.Expressions;
using System.Reflection;

namespace PBG.Nodes;

using SType = NodeExpressionType;

public class NodeExpressionIfElse(NodeExpression expression, NodeExpressionBlock ifTrue, NodeExpressionBlock ifFalse) : NodeExpression(typeof(void), SType.Statement)
{
    public override Expression GetExpression()
    {
        return Expression.IfThenElse(expression.GetExpression(), ifTrue.GetExpression(), ifFalse.GetExpression());
    }

    public override void Compile(NodeExpressionCompileContext context)
    {
        context.Write("if (");
        expression.Compile(context);
        context.Write(")");
        ifTrue.Compile(context);
        context.NewLine();
        context.Write("else");
        ifFalse.Compile(context);
    }
}
