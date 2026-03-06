using System.Linq.Expressions;
using PBG.MathLibrary;

public static class ExpressionHelper
{
    public static Expression Const(int value) => Expression.Constant(value);
    public static Expression Const(float value) => Expression.Constant(value);

    public static Expression FTI(Expression expression) => Expression.Convert(expression, typeof(int));
    public static Expression ITF(Expression expression) => Expression.Convert(expression, typeof(float));

    public static Expression Vector2(Expression x, Expression y) => Expression.New(typeof(Vector2).GetConstructor([typeof(float), typeof(float)])!, x, y);
    public static Expression Vector2i(Expression x, Expression y) => Expression.New(typeof(Vector2i).GetConstructor([typeof(int), typeof(int)])!, x, y);
    public static Expression Vector3(Expression x, Expression y, Expression z) => Expression.New(typeof(Vector3).GetConstructor([typeof(float), typeof(float), typeof(float)])!, x, y, z);
    public static Expression Vector3i(Expression x, Expression y, Expression z) => Expression.New(typeof(Vector3i).GetConstructor([typeof(int), typeof(int), typeof(int)])!, x, y, z);


    public static Expression Vector2ITF(Expression x, Expression y) => Expression.New(typeof(Vector2).GetConstructor([typeof(float), typeof(float)])!, ITF(x), ITF(y));
    public static Expression Vector2iFTI(Expression x, Expression y) => Expression.New(typeof(Vector2i).GetConstructor([typeof(int), typeof(int)])!, FTI(x), FTI(y));
    public static Expression Vector3ITF(Expression x, Expression y, Expression z) => Expression.New(typeof(Vector3).GetConstructor([typeof(float), typeof(float), typeof(float)])!, ITF(x), ITF(y), ITF(z));
    public static Expression Vector3iFTI(Expression x, Expression y, Expression z) => Expression.New(typeof(Vector3i).GetConstructor([typeof(int), typeof(int), typeof(int)])!, FTI(x), FTI(y), FTI(z));

    public static Expression ConvertTo(ValueType from, ValueType to, Expression expression)
    {
        if (from == to)
            return expression;

        return from switch
        {
            ValueType.Float => ConvertFloatTo(to, expression),
            ValueType.Int => ConvertIntTo(to, expression),
            ValueType.Vector2 => ConvertVector2To(to, expression),
            ValueType.Vector2i => ConvertVector2iTo(to, expression),
            ValueType.Vector3 => ConvertVector3To(to, expression),
            ValueType.Vector3i => ConvertVector3iTo(to, expression),
            ValueType.Block => ConvertBlockTo(to, expression),
            _ => throw new ArgumentOutOfRangeException(nameof(from), from, null),
        };
    }

    public static Expression ConvertFloatTo(ValueType to, Expression expr)
    {
        return to switch
        {
            ValueType.Float => expr,
            ValueType.Int => Expression.Convert(expr, typeof(int)),
            ValueType.Vector2 => Vector2(expr, expr),
            ValueType.Vector2i => Vector2iFTI(expr, expr),
            ValueType.Vector3 => Vector3(expr, expr, expr),
            ValueType.Vector3i => Vector3iFTI(expr, expr, expr),
            _ => throw new ArgumentOutOfRangeException(nameof(to), to, null),
        };
    }

    public static Expression ConvertIntTo(ValueType to, Expression expr)
    {
        return to switch
        {
            ValueType.Int => expr,
            ValueType.Float => Expression.Convert(expr, typeof(float)),
            ValueType.Vector2 => Vector2ITF(expr, expr),
            ValueType.Vector2i => Vector2i(expr, expr),
            ValueType.Vector3 => Vector3ITF(expr, expr, expr),
            ValueType.Vector3i => Vector3i(expr, expr, expr),
            _ => throw new ArgumentOutOfRangeException(nameof(to), to, null),
        };
    }

    public static Expression ConvertVector2To(ValueType to, Expression expr)
    {
        var x = Expression.Field(expr, "X");
        var y = Expression.Field(expr, "Y");

        return to switch
        {
            ValueType.Float => x,
            ValueType.Int => Expression.Convert(x, typeof(int)),
            ValueType.Vector2 => expr,
            ValueType.Vector2i => Vector2iFTI(x, y),
            ValueType.Vector3 => Vector3(x, y, Const(0f)),
            ValueType.Vector3i => Vector3iFTI(x, y, Const(0)),
            _ => throw new ArgumentOutOfRangeException(nameof(to), to, null),
        };
    }

    public static Expression ConvertVector2iTo(ValueType to, Expression expr)
    {
        var x = Expression.Field(expr, "X");
        var y = Expression.Field(expr, "Y");

        return to switch
        {
            ValueType.Float => Expression.Convert(x, typeof(float)),
            ValueType.Int => x,
            ValueType.Vector2 => Vector2ITF(x, y),
            ValueType.Vector2i => expr,
            ValueType.Vector3 => Vector3ITF(x, y, Const(0)),
            ValueType.Vector3i => Vector3i(x, y, Const(0)),
            _ => throw new ArgumentOutOfRangeException(nameof(to), to, null),
        };
    }

    public static Expression ConvertVector3To(ValueType to, Expression expr)
    {
        var x = Expression.Field(expr, "X");
        var y = Expression.Field(expr, "Y");
        var z = Expression.Field(expr, "Z");

        return to switch
        {
            ValueType.Float => x,
            ValueType.Int => Expression.Convert(x, typeof(int)),
            ValueType.Vector2 => Vector2(x, y),
            ValueType.Vector2i => Vector2iFTI(x, y),
            ValueType.Vector3 => expr,
            ValueType.Vector3i => Vector3iFTI(x, y, z),
            _ => throw new ArgumentOutOfRangeException(nameof(to), to, null),
        };
    }

    public static Expression ConvertVector3iTo(ValueType to, Expression expr)
    {
        var x = Expression.Field(expr, "X");
        var y = Expression.Field(expr, "Y");
        var z = Expression.Field(expr, "Z");

        return to switch
        {
            ValueType.Float => Expression.Convert(x, typeof(float)),
            ValueType.Int => x,
            ValueType.Vector2 => Vector2ITF(x, y),
            ValueType.Vector2i => Vector2i(x, y),
            ValueType.Vector3 => Vector3ITF(x, y, z),
            ValueType.Vector3i => expr,
            _ => throw new ArgumentOutOfRangeException(nameof(to), to, null),
        };
    }

    public static Expression ConvertBlockTo(ValueType to, Expression expr)
    {
        return to switch
        {
            ValueType.Int => expr,
            _ => throw new ArgumentOutOfRangeException(nameof(to), to, null),
        };
    }
}