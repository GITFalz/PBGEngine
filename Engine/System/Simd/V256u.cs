using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace PBG;

public unsafe static class V256U
{
    public static readonly V256u MaxValue = Vector256.Create(0xFFFFFFFFu);

    public static readonly V256u Zero = Vector256.Create(0u);
    public static readonly V256u One = Vector256.Create(1u);
    public static readonly V256u Two = Vector256.Create(2u);
    public static readonly V256u Three = Vector256.Create(3u);
    public static readonly V256u Four = Vector256.Create(4u);
    public static readonly V256u Five = Vector256.Create(5u);
    public static readonly V256u Six = Vector256.Create(6u);
    public static readonly V256u Seven = Vector256.Create(7u);
    public static readonly V256u Eight = Vector256.Create(8u);
    public static readonly V256u Nine = Vector256.Create(9u);
    public static readonly V256u Ten = Vector256.Create(10u);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256u New(uint v) => Vector256.Create(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256u Add(V256u a, V256u b) => Avx2.Add(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256u Subtract(V256u a, V256u b) => Avx2.Subtract(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<ulong> Multiply(V256u a, V256u b) => Avx2.Multiply(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256u Min(V256u a, V256u b) => Avx2.Min(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256u Max(V256u a, V256u b) => Avx2.Max(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256u Clamp(V256u value, V256u min, V256u max)
    {
        return Avx2.Min(Avx2.Max(value, min), max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256u Clamp01(V256u value)
    {
        return Avx2.Min(Avx2.Max(value, Zero), One);
    }

    private static readonly V256u AbsMask = Vector256.Create(0x7FFFFFFFu);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256u Abs(V256u x) => Avx2.And(x, AbsMask);



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256u Load(uint* ptr) => Avx.LoadVector256(ptr);



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256u CompareEqual(this V256u left, V256u right) => Avx2.CompareEqual(left, right);
}