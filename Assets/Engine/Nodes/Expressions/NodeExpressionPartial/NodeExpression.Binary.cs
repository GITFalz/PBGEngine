namespace PBG.Nodes;

public abstract partial class NodeExpression
{
    public static NodeExpressionSimpleBinary Add(NodeExpression left, NodeExpression right)
    {
        (var l, var r) = NodeExpressionHelper.ResolveBinary(left, right);
        return MakeBinary(NodeExpressionSimpleBinaryType.Add, l, r);
    }

    public static NodeExpressionSimpleBinary Subtract(NodeExpression left, NodeExpression right)
    {
        (var l, var r) = NodeExpressionHelper.ResolveBinary(left, right);
        return MakeBinary(NodeExpressionSimpleBinaryType.Subtract, l, r);
    }

    public static NodeExpressionSimpleBinary Multiply(NodeExpression left, NodeExpression right)
    {
        (var l, var r) = NodeExpressionHelper.ResolveBinary(left, right);
        return MakeBinary(NodeExpressionSimpleBinaryType.Multiply, l, r);
    }

    public static NodeExpressionSimpleBinary Divide(NodeExpression left, NodeExpression right)
    {
        (var l, var r) = NodeExpressionHelper.ResolveBinary(left, right);
        return MakeBinary(NodeExpressionSimpleBinaryType.Divide, l, r);
    }

    public static NodeExpressionSimpleBinary Modulo(NodeExpression left, NodeExpression right)
    {
        (var l, var r) = NodeExpressionHelper.ResolveBinary(left, right);
        return MakeBinary(NodeExpressionSimpleBinaryType.Modulo, l, r, true);
    }

    public static NodeExpressionSimpleBinary Max(NodeExpression left, NodeExpression right)
    {
        (var l, var r) = NodeExpressionHelper.ResolveBinary(left, right);
        return MakeBinary(NodeExpressionSimpleBinaryType.Max, l, r, true);
    }

    public static NodeExpressionSimpleBinary Min(NodeExpression left, NodeExpression right)
    {
        (var l, var r) = NodeExpressionHelper.ResolveBinary(left, right);
        return MakeBinary(NodeExpressionSimpleBinaryType.Min, l, r, true);
    }

    public static NodeExpressionSimpleBinary Power(NodeExpression left, NodeExpression right)
    {
        (var l, var r) = NodeExpressionHelper.ResolveBinary(left, right);
        return MakeBinary(NodeExpressionSimpleBinaryType.Power, l, r, true);
    }

    public static NodeExpressionSimpleBinary Atan2(NodeExpression left, NodeExpression right)
    {
        (var l, var r) = NodeExpressionHelper.ResolveBinary(left, right);
        return MakeBinary(NodeExpressionSimpleBinaryType.Atan2, l, r, true);
    }

    public static NodeExpressionSimpleBinary Equal(NodeExpression left, NodeExpression right)
    {
        (var l, var r) = NodeExpressionHelper.ResolveBinary(left, right);
        return MakeBinary(NodeExpressionSimpleBinaryType.Equal, l, r);
    }

    public static NodeExpressionSimpleBinary NotEqual(NodeExpression left, NodeExpression right)
    {
        (var l, var r) = NodeExpressionHelper.ResolveBinary(left, right);
        return MakeBinary(NodeExpressionSimpleBinaryType.NotEqual, l, r);
    }

    public static NodeExpressionSimpleBinary LessThan(NodeExpression left, NodeExpression right)
    {
        (var l, var r) = NodeExpressionHelper.ResolveBinary(left, right);
        return MakeBinary(NodeExpressionSimpleBinaryType.LessThan, l, r);
    }

    public static NodeExpressionSimpleBinary LessThanOrEqual(NodeExpression left, NodeExpression right)
    {
        (var l, var r) = NodeExpressionHelper.ResolveBinary(left, right);
        return MakeBinary(NodeExpressionSimpleBinaryType.LessThanOrEqual, l, r);
    }

    public static NodeExpressionSimpleBinary GreaterThan(NodeExpression left, NodeExpression right)
    {
        (var l, var r) = NodeExpressionHelper.ResolveBinary(left, right);
        return MakeBinary(NodeExpressionSimpleBinaryType.GreaterThan, l, r);
    }

    public static NodeExpressionSimpleBinary GreaterThanOrEqual(NodeExpression left, NodeExpression right)
    {
        (var l, var r) = NodeExpressionHelper.ResolveBinary(left, right);
        return MakeBinary(NodeExpressionSimpleBinaryType.GreaterThanOrEqual, l, r);
    }

    public static NodeExpressionSimpleBinary AndAlso(NodeExpression left, NodeExpression right)
    {
        (var l, var r) = NodeExpressionHelper.ResolveBinary(left, right);
        return MakeBinary(NodeExpressionSimpleBinaryType.AndAlso, l, r);
    }

    public static NodeExpressionSimpleBinary OrAlso(NodeExpression left, NodeExpression right)
    {
        (var l, var r) = NodeExpressionHelper.ResolveBinary(left, right);
        return MakeBinary(NodeExpressionSimpleBinaryType.OrElse, l, r);
    }
    
    public static NodeExpressionSimpleBinary MakeBinary(NodeExpressionSimpleBinaryType binaryType, NodeExpression left, NodeExpression right, bool isFunction = false)
    {
        return new NodeExpressionSimpleBinary(left.ValueType, binaryType, left, right, isFunction);
    }
}