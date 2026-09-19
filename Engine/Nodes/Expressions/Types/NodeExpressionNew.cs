using System.Linq.Expressions;
using System.Reflection;

namespace PBG.Nodes;

using SType = NodeExpressionType;

public class NodeExpressionNew(ConstructorInfo constructor, params NodeExpression[] arguments) : NodeExpression(constructor.DeclaringType!, SType.Readable | SType.Value | SType.Constant)
{
    public override Expression GetExpression()
    {
        Expression[] newArguments = new Expression[arguments.Length];
        for (int i = 0; i < arguments.Length; i++) newArguments[i] = arguments[i].GetExpression();
        return Expression.New(constructor, newArguments);
    }

    public override void Compile(NodeExpressionCompileContext context)
    {
        context.Write(TypeToGlslString());
        context.Write("(");

        for (int i = 0; i < arguments.Length; i++)
        {
            if (i > 0)
                context.Write(", ");

            arguments[i].Compile(context);
        }

        context.Write(")");
    }
}
