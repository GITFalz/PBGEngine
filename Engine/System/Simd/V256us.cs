using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace PBG;

public static class V256US
{
    public static readonly V256us Zero = New(0);
    public static readonly V256us One = New(1);
    public static readonly V256us Two = New(2);
    public static readonly V256us Three = New(3);
    public static readonly V256us Four = New(4);
    public static readonly V256us Five = New(5);
    public static readonly V256us Six = New(6);
    public static readonly V256us Seven = New(7);
    public static readonly V256us Eight = New(8);
    public static readonly V256us Nine = New(9);
    public static readonly V256us Ten = New(10);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256us New(ushort v) => V256.Create(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256us Add(V256us a, V256us b) => Avx2.Add(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256us Subtract(V256us a, V256us b) => Avx2.Subtract(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256us Min(V256us a, V256us b) => Avx2.Min(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256us Max(V256us a, V256us b) => Avx2.Max(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256us Clamp(V256us value, V256us min, V256us max)
    {
        return Avx2.Min(Avx2.Max(value, min), max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256us Clamp01(V256us value)
    {
        return Avx2.Min(Avx2.Max(value, Zero), One);
    }

    private static readonly V256us AbsMask = New(0x7FFF);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256us Abs(V256us x) => Avx2.And(x, AbsMask);
}