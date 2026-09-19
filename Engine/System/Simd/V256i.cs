using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using PBG.MathLibrary;

namespace PBG;

public static class V256I
{
    public static readonly V256i ByteMask = Vector256.Create(255);

    public static readonly V256i Zero = Vector256.Create(0);
    public static readonly V256i One = Vector256.Create(1);
    public static readonly V256i Two = Vector256.Create(2);
    public static readonly V256i Three = Vector256.Create(3);
    public static readonly V256i Four = Vector256.Create(4);
    public static readonly V256i Five = Vector256.Create(5);
    public static readonly V256i Six = Vector256.Create(6);
    public static readonly V256i Seven = Vector256.Create(7);
    public static readonly V256i Eight = Vector256.Create(8);
    public static readonly V256i Nine = Vector256.Create(9);
    public static readonly V256i Ten = Vector256.Create(10);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i New(int v) => Vector256.Create(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i Stride(int start, int gap) => Vector256.Create(
        start, 
        start + gap, 
        start + gap * 2, 
        start + gap * 3, 
        start + gap * 4, 
        start + gap * 5, 
        start + gap * 6, 
        start + gap * 7
    );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i Add(V256i a, V256i b) => Avx2.Add(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i Subtract(V256i a, V256i b) => Avx2.Subtract(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<long> Multiply(V256i a, V256i b) => Avx2.Multiply(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i Min(V256i a, V256i b) => Avx2.Min(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i Max(V256i a, V256i b) => Avx2.Max(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i Clamp(V256i value, V256i min, V256i max)
    {
        return Avx2.Min(Avx2.Max(value, min), max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i Clamp01(V256i value)
    {
        return Avx2.Min(Avx2.Max(value, Zero), One);
    }

    private static readonly V256i AbsMask = Vector256.Create(0x7FFFFFFF);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i Abs(V256i x) => Avx2.And(x, AbsMask);

    private static readonly V256i _max = V256U.New(0xFFFFFFFF).AsInt32();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i FloorToInt(V256f v)
    {
        return Avx.ConvertToVector256Int32(Avx.RoundToNegativeInfinity(v));
    }

    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i IfElseFull(V256i condition, V256i ifTrue, V256i ifFalse) => Avx2.BlendVariable(ifFalse, ifTrue, condition);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i IfThenElse(this V256i condition, V256i ifTrue, V256i ifFalse) => Avx2.BlendVariable(ifFalse, ifTrue, condition);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AllTrue(V256i mask)
    {
        var inverse = Avx2.Xor(mask, One);
        return Avx.TestZ(inverse, inverse);
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i CompareE(this V256i left, V256i right) => Avx2.CompareEqual(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i CompareG(this V256i left, V256i right) => Avx2.CompareGreaterThan(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i CompareGE(this V256i left, V256i right) => Avx2.CompareGreaterThan(left, right) | Avx2.CompareEqual(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i CompareL(this V256i left, V256i right) => Avx2.CompareGreaterThan(right, left);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i CompareLE(this V256i left, V256i right) => Avx2.CompareGreaterThan(right, left) | Avx2.CompareEqual(left, right);



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i CompareLessThanOrEqual(V256i a, V256i b)
    {
        return Avx2.Or(Avx2.CompareGreaterThan(b, a), Avx2.CompareEqual(a, b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i CompareGreaterThanOrEqual(V256i a, V256i b)
    {
        return Avx2.Or(Avx2.CompareGreaterThan(a, b), Avx2.CompareEqual(a, b));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMSB(this V256i value) => Avx2.MoveMask(value.AsByte());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetExtractedMSB(this V256i value) => Bit.Extract((uint)Avx2.MoveMask(value.AsByte()), 0x11111111u);


}