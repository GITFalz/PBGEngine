using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.Parse;

namespace PBG.Nodes;

public static class NodeExpressionHelper
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
    
    private static string ComponentName(int index)
    {
        return index switch
        {
            0 => "X",
            1 => "Y",
            2 => "Z",
            3 => "W",
            _ => "X"
        };
    }

    public static (NodeExpression, NodeExpression) ResolveBinary(NodeExpression a, NodeExpression b, bool intPriority = false)
    {
        Dictionary<Type, (int order, Type type)> typeOrders = intPriority ? _intTypeOrders : _floatTypeOrders;
        
        Type target = GetCommonType(typeOrders, a.ValueType, b.ValueType);

        a = ConvertIfNeeded(a, target);
        b = ConvertIfNeeded(b, target);

        return (a, b);
    }

    public static Type GetCommonFloatType(Type a, Type b) => GetCommonType(_floatTypeOrders, a, b);
    public static Type GetCommonIntType(Type a, Type b) => GetCommonType(_intTypeOrders, a, b);
    private static Type GetCommonType(Dictionary<Type, (int order, Type type)> typeOrders, Type a, Type b, bool enforceType = false)
    {
        if (a == b) 
            return a;

        GetTypeOrder(typeOrders, a, out var orderA, out var targetA);
        GetTypeOrder(typeOrders, b, out var orderB, out var targetB);

        if (targetA == targetB) return targetA;

        // only one of them is int type
        if (((orderA + orderB) & 1) == 1)
        {
            // return the largest common float based type
            return orderA > orderB ? targetA : targetB;
        }

        // if enforeType is true, result must be of type order
        if (enforceType)
        {
            a = targetA;
            b = targetB;
        }

        // both are either int or float based, so direct conversion is possible
        return orderA > orderB ? a : b;
    }

    private static Dictionary<Type, (int order, Type type)> _floatTypeOrders = new()
    {
        { typeof(float),    (0, typeof(float)) },
        { typeof(int),      (1, typeof(float)) },
        { typeof(Vector2),  (2, typeof(Vector2)) },
        { typeof(Vector2i), (3, typeof(Vector2)) },
        { typeof(Vector3),  (4, typeof(Vector3)) },
        { typeof(Vector3i), (5, typeof(Vector3)) },
        { typeof(Vector4),  (6, typeof(Vector4)) },
        { typeof(Vector4i), (7, typeof(Vector4)) }
    };

    private static Dictionary<Type, (int order, Type type)> _intTypeOrders = new()
    {
        { typeof(float),    (0, typeof(int)) },
        { typeof(int),      (1, typeof(int)) },
        { typeof(Vector2),  (2, typeof(Vector2i)) },
        { typeof(Vector2i), (3, typeof(Vector2i)) },
        { typeof(Vector3),  (4, typeof(Vector3i)) },
        { typeof(Vector3i), (5, typeof(Vector3i)) },
        { typeof(Vector4),  (6, typeof(Vector4i)) },
        { typeof(Vector4i), (7, typeof(Vector4i)) }
    };
    
    public static void GetFloatTypeOrder(Type type, out int order, out Type target) => GetTypeOrder(_floatTypeOrders, type, out order, out target);
    public static void GetIntTypeOrder(Type type, out int order, out Type target) => GetTypeOrder(_intTypeOrders, type, out order, out target);
    private static void GetTypeOrder(Dictionary<Type, (int order, Type type)> typeOrders, Type type, out int order, out Type target)
    {
        if (typeOrders.TryGetValue(type, out var data))
        {
            order = data.order;
            target = data.type;
        }
        else
        {
            throw new InvalidOperationException($"Type '{type}' is not supported");
        }
    }

    public static NodeExpression ConvertIfNeeded(NodeExpression expression, Type target)
    {
        if (expression.ValueType == target)
            return expression;

        return NodeExpression.Convert(expression, target); 
    }
}