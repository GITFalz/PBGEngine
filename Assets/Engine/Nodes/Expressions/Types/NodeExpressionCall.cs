using System.Linq.Expressions;
using System.Reflection;

namespace PBG.Nodes;

using SType = NodeExpressionType;

public class NodeExpressionCall(Type type, string name, Type[] parameterTypes, NodeExpression[] expressions) : NodeExpression(typeof(void), SType.Statement)
{
    public override Expression GetExpression()
    {
        Expression[] newExpressions = new Expression[expressions.Length];
        for (int i = 0; i < expressions.Length; i++) newExpressions[i] = expressions[i].GetExpression();

        var method = type.GetMethod(name, parameterTypes) ?? throw new MissingMethodException(type.FullName, name);
        return Expression.Call(method, newExpressions);
    }

    public override void Compile(NodeExpressionCompileContext context)
    {
        context.Write(name + "(");
        for (int i = 0; i < expressions.Length; i++)
        {
            Type type = i <= parameterTypes.Length - 1 ? parameterTypes[i] : typeof(float);
            if (i != 0)
                context.Write(", ");
            
            Convert(expressions[i], type).Compile(context);
        }
        context.Write(")");
    }
}
