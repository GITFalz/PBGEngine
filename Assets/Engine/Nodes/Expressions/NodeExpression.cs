using System.Linq.Expressions;
using System.Reflection;
using PBG.MathLibrary;
using PBG.Parse;

namespace PBG.Nodes;

public abstract partial class NodeExpression
{
    public readonly Type ValueType = typeof(void);
    public NodeExpressionType ExpressionType;

    public NodeExpression(NodeExpressionType expressionType)
    {
        ExpressionType = expressionType;
    }

    public NodeExpression(Type valueType, NodeExpressionType expressionType)
    {
        ValueType = valueType;
        ExpressionType = expressionType;
    }

    public abstract Expression GetExpression();
    public abstract void Compile(NodeExpressionCompileContext context);

    // If code needs to be isolated for ordering reasons
    // example: a * (b + c)
    internal void CompileIsolated(NodeExpressionCompileContext context)
    {
        bool needsIsolation = IsType(NodeExpressionType.Isolated);
        if (needsIsolation) context.Write("(");
        Compile(context);
        if (needsIsolation) context.Write(")");
    }

    public bool IsType(NodeExpressionType NodeExpressionType)
    {
        return (ExpressionType & NodeExpressionType) == NodeExpressionType;
    }

    public static NodeExpressionVariable Variable(Type type)
    {
        return new NodeExpressionVariable(type);
    }

    public static NodeExpressionVariable Variable(Type type, string name)
    {
        return new NodeExpressionVariable(type, name);
    }

    public static NodeExpressionConstant Constant(object value)
    {
        return new NodeExpressionConstant(value);
    }

    public static NodeExpressionField Field(NodeExpression expression, Type fieldType, string fieldName)
    {
        return new NodeExpressionField(expression, fieldType, fieldName);
    }

    public static NodeExpressionNew New(ConstructorInfo constructor, params NodeExpression[] arguments)
    {
        return new NodeExpressionNew(constructor, arguments);
    }

    public static NodeExpressionAssignment Assign(NodeExpression left, NodeExpression right)
    {
        return new NodeExpressionAssignment(left, right);
    }

    public static NodeExpressionBlock Block(params NodeExpression[] expressions)
    {
        return new NodeExpressionBlock(null, expressions);
    }

    public static NodeExpressionBlock Block(NodeExpressionVariable[]? variables, params NodeExpression[] expressions)
    {
        return new NodeExpressionBlock(variables, expressions);
    }

    public static NodeExpressionConvert Convert(NodeExpression expression, Type targetType)
    {
        return new NodeExpressionConvert(expression, targetType);
    }

    public static NodeExpressionIf If(NodeExpression condition, NodeExpressionBlock ifTrue)
    {
        return new NodeExpressionIf(condition, ifTrue);
    }

    public static NodeExpressionIfElse IfElse(NodeExpression condition, NodeExpressionBlock ifTrue, NodeExpressionBlock ifFalse)
    {
        return new NodeExpressionIfElse(condition, ifTrue, ifFalse);
    }

    public static NodeExpressionCall Call(string name, Type[] parameterTypes, params NodeExpression[] expressions)
    {
        return new NodeExpressionCall(typeof(void), name, parameterTypes, expressions);
    }

    public static NodeExpressionCall Call(Type type, string name, Type[] parameterTypes, params NodeExpression[] expressions)
    {
        return new NodeExpressionCall(type, name, parameterTypes, expressions);
    }

    public bool IsType(params Type[] types)
    {
        for (int i = 0; i < types.Length; i++)
        {
            if (ValueType == types[i])
                return true;
        }

        return false;
    }

    public Type GetSubType()
    {
        return ValueType switch
        {
            _ when ValueType == typeof(float)   => typeof(float),
            _ when ValueType == typeof(int)     => typeof(int),
            _ when ValueType == typeof(Vector2) => typeof(float),
            _ when ValueType == typeof(Vector2i)=> typeof(int),
            _ when ValueType == typeof(Vector3) => typeof(float),
            _ when ValueType == typeof(Vector3i)=> typeof(int),
            _ when ValueType == typeof(Vector4) => typeof(float),
            _ when ValueType == typeof(Vector4i)=> typeof(int),
            _ => ValueType
        };
    }

    public int DataSize()
    {
        return ValueType switch
        {
            _ when ValueType == typeof(float)   => 1,
            _ when ValueType == typeof(int)     => 1,
            _ when ValueType == typeof(Vector2) => 2,
            _ when ValueType == typeof(Vector2i)=> 2,
            _ when ValueType == typeof(Vector3) => 3,
            _ when ValueType == typeof(Vector3i)=> 3,
            _ when ValueType == typeof(Vector4) => 4,
            _ when ValueType == typeof(Vector4i)=> 4,
            _ => 1
        };
    }

    public string TypeToGlslString()
    {
        return ValueType switch
        {
            _ when ValueType == typeof(float)   => "float",
            _ when ValueType == typeof(int)     => "int",
            _ when ValueType == typeof(Vector2) => "vec2",
            _ when ValueType == typeof(Vector2i)=> "ivec2",
            _ when ValueType == typeof(Vector3) => "vec3",
            _ when ValueType == typeof(Vector3i)=> "ivec3",
            _ when ValueType == typeof(Vector4) => "vec4",
            _ when ValueType == typeof(Vector4i)=> "ivec4",
            _ => throw new Exception($"Cannot convert type '{ValueType}' to GLSL type"),
        };
    }

    public static string ObjectToGlslString(object value)
    {
        return value switch
        {
            float v    => Float.Str(v),
            int v      => v.ToString(),
            Vector2 v  => $"vec2({Float.Str(v.X)}, {Float.Str(v.Y)})",
            Vector2i v => $"ivec2({v.X}, {v.Y})",
            Vector3 v  => $"vec3({Float.Str(v.X)}, {Float.Str(v.Y)}, {Float.Str(v.Z)})",
            Vector3i v => $"ivec3({v.X}, {v.Y}, {v.Z})",
            Vector4 v  => $"vec4({Float.Str(v.X)}, {Float.Str(v.Y)}, {Float.Str(v.Z)}, {Float.Str(v.W)})",
            Vector4i v => $"ivec4({v.X}, {v.Y}, {v.Z}, {v.W})",
            _          => throw new Exception($"Unsupported type: {value.GetType()}")
        };
    }
}
