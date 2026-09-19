namespace PBG.Nodes;

using System.Linq.Expressions;
using SType = NodeExpressionType;

public class NodeExpressionBlock(NodeExpressionVariable[]? variables, params NodeExpression[] expressions) : NodeExpression(typeof(void), SType.Statement)
{
    public override Expression GetExpression()
    {
        ParameterExpression[]? newVariables = null;
        if (variables != null)
        {
            newVariables = new ParameterExpression[variables.Length];
            for (int i = 0; i < variables.Length; i++) newVariables[i] = variables[i].GetExpression();
        }

        Expression[] newExpressions = new Expression[expressions.Length];
        for (int i = 0; i < expressions.Length; i++) newExpressions[i] = expressions[i].GetExpression();

        return Expression.Block(newVariables, newExpressions);
    }
    
    public override void Compile(NodeExpressionCompileContext context)
    {
        context.NewLine();
        context.Write("{");

        context.AddPadding();

        if (variables != null)
        {
            foreach (var variable in variables)
            {
                context.NewLine();
                context.Declare(variable);
            }
        }

        foreach (var expression in expressions)
        {
            context.NewLine();
            expression.Compile(context);
            context.Write(";");
        }

        context.RemovePadding();

        context.NewLine();
        context.Write("}");
    }
}