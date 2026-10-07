using System.Diagnostics.CodeAnalysis;
using PBG.Core;
using PBG.MathLibrary;
using PBG.UI;

namespace PBG.Nodes;

public class NodeTemplate
{
    public static Dictionary<string, NodeTemplate> NodeTemplates = [];

    public string Name;
    public Color Color = NodeTypeColors.Default;
    public NodeDefinitionType DefinitionType = NodeDefinitionType.Default;

    public List<string> IncludeExternalCodePaths = [];
    public List<NodePinTemplate> Inputs { get; } = [];
    public List<NodePinTemplate> Outputs { get; } = [];
    public List<string> Blocks { get; } = [];
    public List<string> Selections = [];

    public HashSet<int> InputDependencies = [];

    public Func<NodeExecutionContext, NodeExpression?>? CPUExecute = null;
    public Func<NodeExecutionContext, NodeExpression?>? GPUExecute = null;

    private NodeTemplate(string name, NodeDefinitionType definitionType)
    {
        Name = name;
        DefinitionType = definitionType;
    }

    public static NodeTemplate New(string name) => New(name, NodeDefinitionType.Default);
    public static NodeTemplate New(string name, NodeDefinitionType definitionType)
    {
        if (NodeTemplates.TryGetValue(name, out var template))
            return template;
        
        template = new NodeTemplate(name, definitionType);
        NodeTemplates.Add(name, template);
        return template;
    }

    public static bool TryGetTemplate(string name, [NotNullWhen(true)] out NodeTemplate? template)
    {
        return NodeTemplates.TryGetValue(name, out template);
    }

    public NodeTemplate Include(string path)
    {
        IncludeExternalCodePaths.Add(path);
        return this;
    }

    public NodeTemplate AddSelection(string name)
    {
        Selections.Add(name);
        return this;
    }

    public NodeTemplate Input(string name, NodeDataType type, object? defaultValue = null)
    {
        Inputs.Add(new NodePinTemplate(name, type, defaultValue));
        return this;
    }

    public NodeTemplate Output(string name, NodeDataType type)
    {
        Outputs.Add(new NodePinTemplate(name, type));
        return this;
    }

    public NodeTemplate Block(string name)
    {
        Blocks.Add(name);
        return this;
    }

    public NodeTemplate AddInputDependency(int index)
    {
        InputDependencies.Add(index);
        return this;
    }

    public NodeTemplate SetColor(Color color)
    {
        Color = color;
        return this;
    }

    public NodeTemplate SetExecute(Func<NodeExecutionContext, NodeExpression> execution)
    {
        CPUExecute = execution;
        GPUExecute = execution;
        return this;
    }

    public NodeTemplate SetExecute(Action<NodeExecutionContext> execution)
    {
        CPUExecute = c => { execution(c); return null; };
        GPUExecute = c => { execution(c); return null; };
        return this;
    }

    public NodeTemplate SetCPUExecute(Func<NodeExecutionContext, NodeExpression> execution)
    {
        CPUExecute = execution;
        return this;
    }

    public NodeTemplate SetGPUExecute(Func<NodeExecutionContext, NodeExpression> execution)
    {
        GPUExecute = execution;
        return this;
    }

    public NodeTemplate SetCPUExecute(Action<NodeExecutionContext> execution)
    {
        CPUExecute = c => { execution(c); return null; };
        return this;
    }

    public NodeTemplate SetGPUExecute(Action<NodeExecutionContext> execution)
    {
        GPUExecute = c => { execution(c); return null; };
        return this;
    }

    public NodeExpression? RunGPUExecute(NodeExecutionContext context) => GPUExecute?.Invoke(context);
}

public class NodePinTemplate
{
    public string Name { get; }
    public NodeDataType Type { get; }

    public bool HasInput = true;
    public Type? ValueType = null; // null when multiple
    public object? DefaultValue = null;

    public NodePinTemplate(string name, NodeDataType type, object? defaultValue = null)
    {
        Name = name;
        Type = type;

        if (type.HasFlag(NodeDataType.FieldOnly))
        {
            HasInput = false;
            type &= ~NodeDataType.FieldOnly;
        }

        ValueType = type switch {
            NodeDataType.Bool       => typeof(bool),

            NodeDataType.Float      => typeof(float),
            NodeDataType.Int        => typeof(int),

            NodeDataType.Vector2    => typeof(Vector2),
            NodeDataType.Vector2i   => typeof(Vector2i),

            NodeDataType.Vector3    => typeof(Vector3),
            NodeDataType.Vector3i   => typeof(Vector3i),

            NodeDataType.Vector4    => typeof(Vector4),
            NodeDataType.Vector4i   => typeof(Vector4i),
            _ => null
        };

        DefaultValue = defaultValue;
    }

    private record FieldTypeInfo(uint Count, bool IsFloat, Func<object?, int, object, object> SetComponent);

    private static readonly Dictionary<Type, FieldTypeInfo> _typeFieldGenerators = new()
    {
        { typeof(float), new(1, true,  (_, _, v) => v) },
        { typeof(int),   new(1, false, (_, _, v) => (int)v) },

        { typeof(Vector2),  Vector(Vector2.Zero,  isFloat: true) },
        { typeof(Vector2i), Vector(Vector2i.Zero, isFloat: false) },
        { typeof(Vector3),  Vector(Vector3.Zero,  isFloat: true) },
        { typeof(Vector3i), Vector(Vector3i.Zero, isFloat: false) },
        { typeof(Vector4),  Vector(Vector4.Zero,  isFloat: true) },
        { typeof(Vector4i), Vector(Vector4i.Zero, isFloat: false) },
    };

    private static FieldTypeInfo Vector<T>(IVector<T> zero, bool isFloat) => new(zero.ElementCount, isFloat, (old, i, v) => VectorAction(zero, old, i, v));

    public static UIElementBase? GenerateTypeFields(NodeBase node, int index, NodePinTemplate nodePin)
    {
        if (nodePin.ValueType == null || !_typeFieldGenerators.TryGetValue(nodePin.ValueType, out var info))
            return null;

        return new UIVCol(grow_children, spacing_[5])[
            new Forloop(0, info.Count, i =>
            {
                var v = GetValueString(nodePin.DefaultValue, (int)i);
                return new UICol(left_[10], w_[50], h_[20], blank_sharp, rgba_v4_[NodeBase.Background])[
                    new UIField(v, mc_[10], fs_[1.2f], middle_left, left_[5],
                            info.IsFloat ? text_type_decimal : text_type_numeric)
                        .OnTextChange(field => OnFieldChange(node, index, field, (int)i, info))
                ];
            })
        ];
    }

    private static string GetValueString(object? value, int index)
    {
        if (value == null) 
            return "0";

        if (value is IVector<float> v) 
            return v[index].ToString();
        if (value is IVector<int> v2) 
            return v2[index].ToString();

        return value.ToString() ?? "0";
    }

    private static void OnFieldChange(NodeBase node, int index, UIField field, int vectorIndex, FieldTypeInfo info)
    {
        
        var input = node.Inputs[index];
        object value;
            if (info.IsFloat)
                value = field.GetFloat();
            else
                value = field.GetInt();

        input.Value = info.SetComponent(input.Value, vectorIndex, value);
        node.Inputs[index] = input;
    }

    private static IVector<T> VectorAction<T>(IVector<T> @default, object? old, int index, object value)
    {
        if (old is not IVector<T> vector || value is not T v)
            return @default;
        vector[index] = v;
        return vector;
    }
}

[Flags]
public enum NodeDataType
{
    Execute     = 1 << 0,
    Bool        = 1 << 1,
    Float       = 1 << 2,
    Int         = 1 << 3,
    Vector2     = 1 << 4,
    Vector2i    = 1 << 5,
    Vector3     = 1 << 6,
    Vector3i    = 1 << 7,
    Vector4     = 1 << 8,
    Vector4i    = 1 << 9,
    Numeric     = Float | Int | Vector2 | Vector2i | Vector3 | Vector3i | Vector4 | Vector4i,
    Any         = 1 << 11,
    FieldOnly   = 1 << 12,
}

[Flags]
public enum NodeDefinitionType
{
    Default     = 0,
    IsOutput    = 1 << 0,
    Priority    = 1 << 1,
    Dependency  = 1 << 2,
    Block       = 1 << 3,
    GPU         = 1 << 4,
    CPU         = 1 << 5
}