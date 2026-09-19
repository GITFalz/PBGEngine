using System.Linq.Expressions;
using PBG.MathLibrary;

namespace PBG.Nodes;

using SType = NodeExpressionType;

public class NodeExpressionConvert(NodeExpression expression, Type targetType) : NodeExpression(targetType, SType.Readable | SType.Value | SType.Constant)
{
    private static Dictionary<Type, (int count, bool integer)> _typeInfo = new()
    {
        { typeof(float), (1, false) },
        { typeof(int),   (1, true) },

        { typeof(Vector2), (2, false) },
        { typeof(Vector2i), (2, true) },

        { typeof(Vector3), (3, false) },
        { typeof(Vector3i), (3, true) },

        { typeof(Vector4), (4, false) },
        { typeof(Vector4i), (4, true) },
    };

    private static string ComponentName(int index) =>       index switch { 0 => "X", 1 => "Y", 2 => "Z", 3 => "W", _ => "X" };
    private static string ShaderComponentName(int index) => index switch { 0 => "x", 1 => "y", 2 => "z", 3 => "w", _ => "x" };

    public override Expression GetExpression()
    {
        if (expression.IsType(SType.Readable))
        {
            var sourceInfo = _typeInfo[expression.ValueType];
            var targetInfo = _typeInfo[ValueType];

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
    }
    public override void Compile(NodeExpressionCompileContext context)
    {
        if (expression.IsType(SType.Readable))
        {
            var sourceInfo = _typeInfo[expression.ValueType];
            var targetInfo = _typeInfo[ValueType];

            Console.WriteLine(sourceInfo.count + " " + sourceInfo.integer + " / " + targetInfo.count + " " + targetInfo.integer);

            bool toInt = targetInfo.integer && !sourceInfo.integer;

            bool constructor = targetInfo.count > 1 && expression.ValueType != ValueType;
            if (constructor)
            {
                context.Write(TypeToGlslString());
                context.Write("(");
            }

            Console.WriteLine(toInt + " " + constructor);

            if (sourceInfo.count == 1)
            {
                context.Write(toInt ? "int(" : "");
                expression.Compile(context);
                context.Write(toInt ? ")" : "");
            }
            else if (expression.ValueType == ValueType)
            {
                expression.Compile(context);
            }
            else
            {
                int min = Mathf.Min(targetInfo.count, sourceInfo.count);
                for (int i = 0; i < min; i++)
                {
                    if (i > 0)
                        context.Write(", ");

                    context.Write(toInt ? "int(" : "");
                    expression.Compile(context);
                    context.Write($".{ShaderComponentName(i)}" + (toInt ? ")" : ""));
                }

                for (int i = min; i < targetInfo.count; i++)
                    context.Write($", 0");
            }
            
            if (constructor)
                context.Write(")");
        }
        else
        {
            throw new InvalidOperationException($"Value is not readable so it cannot be converted to type '{ValueType}'");
        }
    }
}
