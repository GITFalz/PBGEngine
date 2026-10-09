using System.Text;

namespace PBG.Nodes;

public class NodeExpressionCompileContext
{
    private StringBuilder _builder = new();
    private int _count = 0;
    private uint _padding = 0;

    private HashSet<NodeExpressionVariable> Variables = [];

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

    public void Declare(NodeExpressionVariable variable)
    {
        if (Variables.Add(variable))
        {
            variable.Declare(this);
            Write($"{variable.TypeString} {variable.Name};");
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