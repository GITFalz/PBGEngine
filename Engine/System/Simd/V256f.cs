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
    public static V256f CompareE(this V256f left, V256f right) => Avx.CompareEqual(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f CompareG(this V256f left, V256f right) => Avx.CompareGreaterThan(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f CompareGE(this V256f left, V256f right) => Avx.CompareGreaterThan(left, right) | Avx.CompareEqual(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f CompareL(this V256f left, V256f right) => CompareGE(right, left);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f CompareLE(this V256f left, V256f right) => CompareG(right, left);

    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Abs(V256f x) => Avx.And(x, AbsMask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i ToInt(this V256f vector) => Avx.ConvertToVector256Int32(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i FloorToInt(this V256f vector) => Avx.ConvertToVector256Int32(Avx.RoundToNegativeInfinity(vector));
}