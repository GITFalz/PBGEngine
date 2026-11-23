using System.Linq.Expressions;
using PBG.MathLibrary;

namespace PBG.Nodes;

using SType = NodeExpressionType;

public class NodeExpressionConvert : NodeExpression
{
    public readonly NodeExpression Expression;

    public readonly int SourceCount;
    public readonly ValueSubType SourceSubType; 

    public readonly int TargetCount;
    public readonly ValueSubType TargetSubType; 

    public readonly bool SameType;

    public readonly bool Constructor;
    public readonly bool ToInt;
    public readonly bool ToFloat;

    public NodeExpressionConvert(NodeExpression expression, Type targetType) : base(targetType, SType.Readable | SType.Value | SType.Constant)
    {
        Expression = expression;

        SourceCount     = GetFieldCount(expression);
        SourceSubType   = GetSubType(expression);

        TargetCount     = GetFieldCount(this);
        TargetSubType   = GetSubType(this);

        SameType = Expression.ValueType == ValueType;

        Constructor = TargetCount > 1 && Expression.ValueType != ValueType;

        ToInt = SourceSubType == ValueSubType.Float && TargetSubType == ValueSubType.Int;
        ToFloat = SourceSubType == ValueSubType.Int && TargetSubType == ValueSubType.Float;
    }

    private static string ComponentName(int index) =>       index switch { 0 => "X", 1 => "Y", 2 => "Z", 3 => "W", _ => "X" };
    private static string ShaderComponentName(int index) => index switch { 0 => "x", 1 => "y", 2 => "z", 3 => "w", _ => "x" };
    private static string Avx2ComponentName(int index) => index switch { 0 => "x", 1 => "y", 2 => "z", 3 => "w", _ => "x" };

    public override Expression GetExpression()
    {
        /*
        if (expression.IsType(SType.Readable))
        {
            var sourceInfo = _typeInfo[expression.ValueType];
            var targetInfo = _typeInfo[ValueType];

            Console.WriteLine("Trying to convert from " + expression.ValueType + " to " + ValueType);

            bool toInt = targetInfo.integer && !sourceInfo.integer;
            var expr = expression.GetExpression();

            if (sourceInfo.count == 1 && targetInfo.count == 1)
            {
                return toInt ? Expression.Convert(expr, typeof(int)) : expr;
            }

            if (targetInfo.count == 1)
            {
                var field = Expression.Field(expr, ComponentName(0));
                return toInt ? Expression.Convert(field, typeof(int)) : field;
            }

            Type[] constructorTypes = new Type[targetInfo.count];
            Expression[] expressions = new Expression[targetInfo.count];
            
            for (int i = 0; i < targetInfo.count; i++)
            {
                constructorTypes[i] = targetInfo.integer ? typeof(int) : typeof(float);
            }
            
            if (sourceInfo.count == 1)
            {
                var field = toInt ? Expression.Convert(expr, typeof(int)) : expr;  
                for (int i = 0; i < targetInfo.count; i++)
                {
                    expressions[i] = field;
                }
                return Expression.New(ValueType.GetConstructor(constructorTypes)!, expressions);
            }
            else
            {
                int min = Mathf.Min(targetInfo.count, sourceInfo.count);
                for (int i = 0; i < min; i++)
                {
                    Expression field = Expression.Field(expr, ComponentName(i));
                    Expression field2 = toInt ? Expression.Convert(field, typeof(int)) : field;  
                    expressions[i] = field2;
                }
                for (int i = min; i < targetInfo.count; i++)
                {
                    expressions[i] = Expression.Constant(0);
                }
                return Expression.New(ValueType.GetConstructor(constructorTypes)!, expressions);
            }
        }
        else
        {
            throw new InvalidOperationException($"Value is not readable so it cannot be converted to type '{ValueType}'");
        }
        */
        throw new NotImplementedException();
    }

    public override void Compile(NodeExpressionCompileContext context)
    {
        if (Expression.IsType(SType.Readable))
        {
            if (SameType)
            {
                Expression.Compile(context);
                return;
            }

            if (Constructor)
                context.Write(context.Formatter.GetConstructor(ValueType));

            if (SourceCount == 1)
            {
                if (ToInt)
                {
                    context.Write(context.Formatter.CastIntStart());
                    Expression.Compile(context);
                    context.Write(context.Formatter.CastIntEnd());
                }
                else if (ToFloat)
                {
                    context.Write(context.Formatter.CastFloatStart());
                    Expression.Compile(context);
                    context.Write(context.Formatter.CastFloatEnd());
                }
                else
                {
                    Expression.Compile(context);
                }
            }
            else
            {
                for (int i = 0; i < Math.Min(TargetCount, SourceCount); i++)
                {
                    if (i > 0)
                        context.Write(", ");

                    if (ToInt)
                    {
                        context.Write(context.Formatter.CastIntStart());
                        Expression.Compile(context);
                        context.Write(context.Formatter.GetVectorFieldName(i));
                        context.Write(context.Formatter.CastIntEnd());
                    }
                    else if (ToFloat)
                    {
                        context.Write(context.Formatter.CastFloatStart());
                        Expression.Compile(context);
                        context.Write(context.Formatter.GetVectorFieldName(i));
                        context.Write(context.Formatter.CastFloatEnd());
                    }
                    else
                    {
                        Expression.Compile(context);
                        context.Write(context.Formatter.GetVectorFieldName(i));
                    }
                }

                if (TargetCount > SourceCount)
                {
                    for (int i = SourceCount; i < TargetCount; i++)
                    {
                        if (i > 0)
                            context.Write(", ");

                        Constant(TargetSubType == ValueSubType.Int ? 0 : 0f).Compile(context);
                    }
                }
            }

            if (Constructor)
                context.Write(")");
        }
        else
        {
            throw new InvalidOperationException($"Expression '{Expression.GetType()}' is not readable so it cannot be converted to type '{ValueType}'");
        }
    }

    private string GetComponent(NodeExpressionCompileContext context, int i)
    {
        if (context.CompileType == NodeExpressionCompileType.GLSL)
        {
            return $".{ShaderComponentName(i)}";
        }
        else if (context.CompileType == NodeExpressionCompileType.Avx2)
        {
            return $".{Avx2ComponentName(i)}";
        }
        return "";
    }



    private static ValueSubType GetSubType(NodeExpression expression)
    {
        return expression.ValueType switch
        {
            _ when expression.ValueType == typeof(float)   => ValueSubType.Float,
            _ when expression.ValueType == typeof(int)     => ValueSubType.Int,
            _ when expression.ValueType == typeof(Vector2) => ValueSubType.Float,
            _ when expression.ValueType == typeof(Vector2i)=> ValueSubType.Int,
            _ when expression.ValueType == typeof(Vector3) => ValueSubType.Float,
            _ when expression.ValueType == typeof(Vector3i)=> ValueSubType.Int,
            _ when expression.ValueType == typeof(Vector4) => ValueSubType.Float,
            _ when expression.ValueType == typeof(Vector4i)=> ValueSubType.Int,
            _ => throw new Exception($"Cannot get sub type of type: {expression.ValueType}"),
        };
    }

    private static int GetFieldCount(NodeExpression expression)
    {
        return expression.ValueType switch
        {
            _ when expression.ValueType == typeof(float)   => 1,
            _ when expression.ValueType == typeof(int)     => 2,
            _ when expression.ValueType == typeof(Vector2) => 2,
            _ when expression.ValueType == typeof(Vector2i)=> 2,
            _ when expression.ValueType == typeof(Vector3) => 3,
            _ when expression.ValueType == typeof(Vector3i)=> 3,
            _ when expression.ValueType == typeof(Vector4) => 4,
            _ when expression.ValueType == typeof(Vector4i)=> 4,
            _ => throw new Exception($"Cannot get field count of type: {expression.ValueType}"),
        };
    }
}

public enum ValueSubType
{
    Int,
    Float
}