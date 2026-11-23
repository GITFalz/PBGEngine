using System.Text;

namespace PBG.Nodes;

public class NodeExpressionCompileContext
{
    public NodeExpressionCompileType CompileType { get; private set; }
    public INodeCodeFormatter Formatter;
    private StringBuilder _builder = new();
    private int _count = 0;
    private uint _padding = 0;

    private HashSet<NodeExpressionVariable> Variables = [];

    public NodeExpressionCompileContext() : this(NodeExpressionCompileType.GLSL) {}
    public NodeExpressionCompileContext(NodeExpressionCompileType compileType)
    {
        CompileType = compileType;
        Formatter = compileType switch
        {
            NodeExpressionCompileType.GLSL      => new GLSLFormatter(),
            NodeExpressionCompileType.Scalar    => new ScalarFormatter(),
            NodeExpressionCompileType.Avx2      => new Avx2Formatter(),
            _ => throw new Exception($"Unsupported compile type '{compileType}'")
        };
    }

    public string GetNextVariableName()
    {
        string variable = $"var{_count}";
        _count++;
        return variable;
    }

    public void Write(string line)
    {
        _builder.Append(line);
    }

    public void NewLine()
    {
        _builder.Append('\n' + new string(' ', (int)_padding*4));
    }

    public void AddPadding() => _padding++;
    public void RemovePadding() => _padding--;

    public void Declare(NodeExpressionCompileContext context, NodeExpressionVariable variable)
    {
        if (Variables.Add(variable))
        {
            variable.Declare(this);

            string typeString = Formatter.TypeToString(variable.ValueType);
                
            Write($"{typeString} {variable.Name};");
        }
    }

    public override string ToString()
    {
        return _builder.ToString();
    }

    public void Clear()
    {
        _builder.Clear();
        Variables = [];
    }
}

public enum NodeExpressionCompileType
{
    GLSL,
    Scalar,
    Avx2
}