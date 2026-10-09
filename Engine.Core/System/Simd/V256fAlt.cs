using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace PBG;

public static class V256FAlternative
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f CompareE(this V256f left, V256f right) => Avx.CompareEqual(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f CompareG(this V256f left, V256f right) => Avx.CompareGreaterThan(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f CompareGE(this V256f left, V256f right) => Avx.CompareGreaterThanOrEqual(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f CompareLE(this V256f left, V256f right) => Avx.CompareLessThanOrEqual(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f CompareL(this V256f left, V256f right) => Avx.CompareLessThan(left, right);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f IsEqualTo(this V256f left, V256f right) => Avx.CompareEqual(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f IsGreaterThan(this V256f left, V256f right) => Avx.CompareGreaterThan(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f IsGreaterOrEqualThan(this V256f left, V256f right) => Avx.CompareGreaterThanOrEqual(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f IsLessOrEqualThan(this V256f left, V256f right) => Avx.CompareLessThanOrEqual(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f IsLessThan(this V256f left, V256f right) => Avx.CompareLessThan(left, right);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f IfThenElse(this V256f condition, V256f ifTrue, V256f ifFalse) => Avx.BlendVariable(ifFalse, ifTrue, condition);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Or(this V256f left, V256f right) => Avx.Or(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f And(this V256f left, V256f right) => Avx.And(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f AndNot(this V256f left, V256f right) => Avx.AndNot(right, left); // left & ~right

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f SetIf(this ref V256f vector, V256f condition, V256f ifTrue) => vector = Avx.BlendVariable(vector, ifTrue, condition);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i ToInt(this V256f vector) => Avx.ConvertToVector256Int32(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256i FloorToInt(this V256f vector) => Avx.ConvertToVector256Int32(Avx.RoundToNegativeInfinity(vector));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Floor(this V256f vector) => Avx.Floor(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Mod(this V256f vector, V256f mod) => vector - (mod * Floor(vector / mod));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Fraction(this V256f vector) => Avx.Subtract(vector, Avx.Floor(vector));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256f Clamp01(this V256f value) => Avx.Min(Avx.Max(value, V256f.Zero), V256f.One);
}