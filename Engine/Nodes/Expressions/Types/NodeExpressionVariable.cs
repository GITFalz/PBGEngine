using System.Linq.Expressions;

namespace PBG.Nodes;

using SType = NodeExpressionType;

public class NodeExpressionVariable : NodeExpression
{
    private static int _counter = 0;

    public string Name { get; private set; } = $"variable{_counter++}";
    public readonly string TypeString;

    public NodeExpressionVariable(Type valueType, string? name = null) : base(valueType, SType.Readable | SType.Writeable | SType.Value)
    {
        TypeString = TypeToGlslString();
        if (name != null)
            Name = name;
    }

    public void Declare(NodeExpressionCompileContext context)
    {
        Name = context.GetNextVariableName();
    }
    
    public override ParameterExpression GetExpression() => Expression.Variable(ValueType);
    public override void Compile(NodeExpressionCompileContext context)
    {
        context.Write(Name);
    }
}
