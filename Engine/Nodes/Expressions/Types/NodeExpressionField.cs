using System.Linq.Expressions;
using System.Reflection;

namespace PBG.Nodes;

using SType = NodeExpressionType;

public class NodeExpressionField(NodeExpression expression, Type fieldType, string fieldName) : NodeExpression(fieldType, SType.Value | SType.Readable)
{
    public override Expression GetExpression()
    {
        return Expression.Field(expression.GetExpression(), ValueType, fieldName);
    }

    public override void Compile(NodeExpressionCompileContext context)
    {
        expression.Compile(context);
        context.Write("." + fieldName);
    }
}
