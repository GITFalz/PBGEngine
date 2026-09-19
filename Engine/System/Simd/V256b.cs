using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace PBG;

public unsafe static class V256B
{
    public static readonly V256b MaxValue = Vector256.Create((byte)0xFFu);

    public static readonly V256b Zero = Vector256.Create((byte)0u);
    public static readonly V256b One = Vector256.Create((byte)1u);
    public static readonly V256b Two = Vector256.Create((byte)2u);
    public static readonly V256b Three = Vector256.Create((byte)3u);
    public static readonly V256b Four = Vector256.Create((byte)4u);
    public static readonly V256b Five = Vector256.Create((byte)5u);
    public static readonly V256b Six = Vector256.Create((byte)6u);
    public static readonly V256b Seven = Vector256.Create((byte)7u);
    public static readonly V256b Eight = Vector256.Create((byte)8u);
    public static readonly V256b Nine = Vector256.Create((byte)9u);
    public static readonly V256b Ten = Vector256.Create((byte)10u);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256b New(byte v) => Vector256.Create(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256b Add(V256b a, V256b b) => Avx2.Add(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256b Subtract(V256b a, V256b b) => Avx2.Subtract(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256b Min(V256b a, V256b b) => Avx2.Min(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256b Max(V256b a, V256b b) => Avx2.Max(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256b Clamp(V256b value, V256b min, V256b max)
    {
        return Avx2.Min(Avx2.Max(value, min), max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256b Clamp01(V256b value)
    {
        return Avx2.Min(Avx2.Max(value, Zero), One);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256b Load(byte* ptr) => Avx.LoadVector256(ptr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256b CompareEqual(this V256b left, V256b right) => Avx2.CompareEqual(left, right);
}