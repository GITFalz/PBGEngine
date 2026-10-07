using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace PBG.MathLibrary;

public static class Bit
{
    public const uint ONE_MASK = 1u;
    public const uint INVERTED_ONE_MASK = ~1u;
    public const uint MAX_UINT_MASK = 0xFFFFFFFFu;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong TrailingOnes(int n) => n >= 64 ? ulong.MaxValue : (1UL << n) - 1;



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TrailingOnes(uint n) => BitOperations.TrailingZeroCount(~n);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TrailingOnes(ulong n) => BitOperations.TrailingZeroCount(~n);



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TrailingZeros(uint n) => BitOperations.TrailingZeroCount(n);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TrailingZeros(ulong n) => BitOperations.TrailingZeroCount(n);



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(uint n) => BitOperations.PopCount(n);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(int n) => BitOperations.PopCount((uint)n);

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToString(ulong n) => Convert.ToString((long)n, 2).PadLeft(64, '0');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToString(ulong n, int length) => Convert.ToString((long)n, 2).PadLeft(length, '0');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToString(ulong n, int start, int length) => Convert.ToString((long)(n >> start), 2).PadLeft(length, '0');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToString(uint n) => Convert.ToString((long)n, 2).PadLeft(32, '0');

    /// <summary>
    /// Returns a mask with all bits set except bit <paramref name="n"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Invert(int n) => ~(1u << n);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Extract(uint value, uint mask) => Bmi2.ParallelBitExtract(value, mask);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Extract(ulong value, ulong mask) => Bmi2.X64.ParallelBitExtract(value, mask);
}