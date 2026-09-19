using System.Linq.Expressions;
using PBG.MathLibrary;

namespace PBG.Nodes;

using SType = NodeExpressionType;

public class NodeExpressionConstant(object value) : NodeExpression(value.GetType(), SType.Readable | SType.Value | SType.Constant)
{
    public override Expression GetExpression()
    {
        return value switch
        {
            float v => Expression.Constant(v),
            int v => Expression.Constant(v),

            Vector2 v => GetVectorConstructor<Vector2, float>(v),
            Vector2i v => GetVectorConstructor<Vector2i, int>(v),
            
            Vector3 v => GetVectorConstructor<Vector3, float>(v),
            Vector3i v => GetVectorConstructor<Vector3i, int>(v),
            
            Vector4 v => GetVectorConstructor<Vector4, float>(v),
            Vector4i v => GetVectorConstructor<Vector4i, int>(v),

            _ => throw new InvalidOperationException($"Type '{ValueType}' is not supported when creating a constant")
        };
    }

    private static NewExpression GetVectorConstructor<T1, T2>(IVector<T2> vector) where T1 : IVector<T2>
    {
        Type[] constructorTypes = new Type[vector.ElementCount]; 
        Expression[] expressions = new Expression[vector.ElementCount];
        for (int i = 0; i < vector.ElementCount; i++)
        {
            constructorTypes[i] = typeof(T2);
            expressions[i] = Expression.Constant(vector[i]);
        }
        return Expression.New(typeof(T1).GetConstructor(constructorTypes)!, expressions);
    }

    public override void Compile(NodeExpressionCompileContext context)
    {
        if (IsType([typeof(float), typeof(int), typeof(Vector2), typeof(Vector2i), typeof(Vector3), typeof(Vector3i), typeof(Vector4), typeof(Vector4i)]))
        {
            context.Write(ObjectToGlslString(value));
        }
        else
        {
            throw new InvalidOperationException(
                $"Invalid type: Constant cannot be of type {ValueType.Name}"
            );
        }
    }
}
