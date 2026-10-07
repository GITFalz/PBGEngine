using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace PBG;

public static class V256F
{
    public static readonly V256f Zero = Vector256.Create(0f);
    public static readonly V256f Half = Vector256.Create(0.5f);
    public static readonly V256f One = Vector256.Create(1f);
    public static readonly V256f Two = Vector256.Create(2f);
    public static readonly V256f Three = Vector256.Create(3f);
    public static readonly V256f Four = Vector256.Create(4f);
    public static readonly V256f Five = Vector256.Create(5f);
    public static readonly V256f Six = Vector256.Create(6f);
    public static readonly V256f Seven = Vector256.Create(7f);
    public static readonly V256f Eight = Vector256.Create(8f);
    public static readonly V256f Nine = Vector256.Create(9f);
    public static readonly V256f Ten = Vector256.Create(10f);

    private static readonly V256f AbsMask = Vector256.Create(0x7FFFFFFF).AsSingle();

    public static readonly V256f PI = Vector256.Create(3.141592653589793f);
    public static readonly V256f PI_OVER_2 = Vector256.Create(1.5707963267948966f);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f New(float f) => Vector256.Create(f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Add(V256f a, V256f b) => Avx.Add(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Subtract(V256f a, V256f b) => Avx.Subtract(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Multiply(V256f a, V256f b) => Avx.Multiply(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Divide(V256f a, V256f b) => Avx.Divide(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Min(V256f a, V256f b) => Avx.Min(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Max(V256f a, V256f b) => Avx.Max(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Clamp(V256f value, V256f min, V256f max)
    {
        return Avx.Min(Avx.Max(value, min), max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Clamp01(V256f value)
    {
        return Avx.Min(Avx.Max(value, V256f.Zero), V256f.One);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Lerp(V256f a, V256f b, V256f t) => a + (b - a) * t;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Abs(V256f x) => Avx.And(x, AbsMask);

    private static readonly V256f _atan2_1 = New(0.999866f);
    private static readonly V256f _atan2_2 = New(0.330299f);
    private static readonly V256f _atan2_3 = New(0.180141f);
    private static readonly V256f _atan2_4 = New(0.055156f);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Atan2(V256f y, V256f x)
    {
        V256f xcomp = x.CompareE(Zero);
        V256f ycomp = y.CompareE(Zero);
        V256f zcomp = xcomp & ycomp;

        V256f absX = Abs(x);
        V256f absY = Abs(y);
        
        V256f swap = absY.CompareG(absX);
        
        V256f num = swap.IfThenElse(absX, absY);
        V256f den = swap.IfThenElse(absY, absX);

        V256f denSafe = zcomp.IfThenElse(One, den); // to be safe so no / 0
        V256f t = num / denSafe;
        V256f t2 = t * t;
        
        V256f angle = t * (_atan2_1 - t2 * (_atan2_2 - t2 * (_atan2_3 - t2 * _atan2_4)));

        angle.SetIf(swap, PI_OVER_2 - angle);
        angle.SetIf(x.CompareL(Zero), PI - angle);
        angle.SetIf(y.CompareL(Zero), -angle);

        return zcomp.IfThenElse(Zero, angle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (V256f, V256f) Normalize(V256f x, V256f y)
    {
        var scale = One / Length(x, y);
        return (x * scale, y * scale);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Length(V256f x, V256f y)
    {
        return Avx.Sqrt(x * x + y * y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f LengthSquared(V256f x, V256f y)
    {
        return x * x + y * y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Distance((V256f x, V256f y) a, (V256f x, V256f y) b)
    {
        var x = b.x - a.x;
        var y = b.y - a.y;
        return Length(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f DistanceSquared((V256f x, V256f y) a, (V256f x, V256f y) b)
    {
        var x = b.x - a.x;
        var y = b.y - a.y;
        return LengthSquared(x, y);
    }
}