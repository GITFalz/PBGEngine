using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using PBG.Core;
using PBG.MathLibrary;

namespace PBG.NewVoxel;

[InternalSystemInit(InitPriority.Data)]
[InternalSystemCleanup]
public unsafe static class VoxelHelper
{
    public static uint GetV256Row(uint* ptr, int i)
    {
        V256u v = Avx.LoadVector256(ptr + i * 8);
        V256u isAir = Avx2.CompareEqual(v, V256U.Zero);

        uint byteMask = (uint)Avx2.MoveMask(isAir.AsByte());
        byteMask &= 0x11111111u;

        uint compact = Bmi2.ParallelBitExtract(byteMask, 0x11111111u); // one bit per lane, bits 0..7
        uint notAir = (~compact) & 0xFFu; // CompareEqual gave "is air", invert

        return notAir << (i * 8);
    }

    

    public static uint GetV256NonAirBits(V256u blocks)
    {
        V256u isAir1 = Avx2.CompareEqual(blocks, V256U.Zero);
        return (uint)Avx2.MoveMask(isAir1.AsByte()) & 0x11111111u;
    }

    public static ulong GetV256PackedNonAirBits(V256u blocks)
    {
        V256u isAir = Avx2.CompareEqual(blocks, V256U.Zero);
        uint mask = (uint)Avx2.MoveMask(isAir.AsByte());

        return Bmi2.X64.ParallelBitExtract(mask, 0x11111111u);
    }

    public static ulong GetV256PackedNonAirBits(V256u blocks1, V256u blocks2)
    {
        V256u isAir1 = Avx2.CompareEqual(blocks1, V256U.Zero);
        V256u isAir2 = Avx2.CompareEqual(blocks2, V256U.Zero);

        ulong mask = (uint)Avx2.MoveMask(isAir1.AsByte()) | ((ulong)(uint)Avx2.MoveMask(isAir2.AsByte()) << 32);

        return (~Bmi2.X64.ParallelBitExtract(mask, 0x1111111111111111ul)) & 0xFFFFul;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetV256RowMask(uint* ptr, int i)
    {
        V256u v1 = Avx.LoadVector256(ptr + i * 8);
        V256u isAir1 = Avx2.CompareEqual(v1, V256U.Zero);
        return (uint)Avx2.MoveMask(isAir1.AsByte()) & 0x11111111u;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256us GetNarrowedV256us(uint* ptr) => V256.Narrow(Avx.LoadVector256(ptr), Avx.LoadVector256(ptr + 8));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetNarrowedV256usMask(uint* ptr)
    {
        V256us isAir = Avx2.CompareEqual(GetNarrowedV256us(ptr), V256US.Zero);
        return (uint)Avx2.MoveMask(isAir.AsByte()) & 0x55555555u;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetNarrowedV256usMask(V256u a, V256u b)
    {
        V256us isAir = Avx2.CompareEqual(V256.Narrow(a, b), V256US.Zero);
        return (uint)Avx2.MoveMask(isAir.AsByte()) & 0x55555555u;
    }

    public static ulong GetV256uRowX64(uint* ptr, int i)
    {
        ulong byteMask1 = GetV256RowMask(ptr, i);
        ulong byteMask2 = GetV256RowMask(ptr, i+1);

        ulong compact = Bmi2.X64.ParallelBitExtract(byteMask1 | (byteMask2 << 32), 0x1111111111111111ul); // one bit per lane, bits 0..7
        ulong notAir = (~compact) & 0xFFFFul; // CompareEqual gave "is air", invert

        return notAir << (i * 8);
    }


    public static ulong GetV256usSolidBitRowX64(uint* ptr)
    {
        ulong byteMask1 = GetNarrowedV256usMask(ptr);
        ulong byteMask2 = GetNarrowedV256usMask(ptr + 16);
        ulong compact = Bmi2.X64.ParallelBitExtract(byteMask1 | (byteMask2 << 32), 0x5555555555555555ul);
        return (~compact) & 0xFFFFFFFFu;
    }


    public static ulong GetV256usNonAirBitRowX64(uint* ptr)
    {
        ulong byteMask1 = GetNarrowedV256usMask(ptr);
        ulong byteMask2 = GetNarrowedV256usMask(ptr + 16);
        ulong compact = Bmi2.X64.ParallelBitExtract(byteMask1 | (byteMask2 << 32), 0x5555555555555555ul);
        return (~compact) & 0xFFFFFFFFu;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetV256usNonAirBitRowX64(V256u a, V256u b, V256u c, V256u d)
    {
        ulong byteMask1 = GetNarrowedV256usMask(a, b);
        ulong byteMask2 = GetNarrowedV256usMask(c, d);
        ulong compact = Bmi2.X64.ParallelBitExtract(byteMask1 | (byteMask2 << 32), 0x5555555555555555ul);
        return (~compact) & 0xFFFFFFFFu;
    }

    


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetRowSolidBitsV256X64(V256u a, V256u b, V256u c, V256u d)
    {
        uint mask1 = (uint)Avx.MoveMask(a.AsSingle());
        uint mask2 = (uint)Avx.MoveMask(b.AsSingle());
        uint mask3 = (uint)Avx.MoveMask(c.AsSingle());
        uint mask4 = (uint)Avx.MoveMask(d.AsSingle());

        return mask1 | (mask2 << 8) | (mask3 << 16) | (mask4 << 24);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetRowSolidBitsV256X64(V256b row)
    {
        return (uint)Avx2.MoveMask(row);
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (V256u, V256u, V256u, V256u) WidenToV256u(V256b vector)
    {
        var (ushortsLo, ushortsHi) = Vector256.Widen(vector);

        var (uints0, uints1) = Vector256.Widen(ushortsLo);
        var (uints2, uints3) = Vector256.Widen(ushortsHi);

        return (uints0, uints1, uints2, uints3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256b ShortenToV256b(V256u v1, V256u v2, V256u v3, V256u v4)
    {
        var ushortsLo = Vector256.Narrow(v1, v2);
        var ushortsHi = Vector256.Narrow(v3, v4);

        return Vector256.Narrow(ushortsLo, ushortsHi);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256b ShortenToV256b(V256i v1, V256i v2, V256i v3, V256i v4)
    {
        var ushortsLo = Vector256.Narrow(v1, v2);
        var ushortsHi = Vector256.Narrow(v3, v4);

        return Vector256.Narrow(ushortsLo, ushortsHi).AsByte();
    }



    public static uint GetV256RowBit(uint* ptr, byte shift)
    {
        V256u v = Avx2.ShiftLeftLogical(Avx.LoadVector256(ptr), shift);

        uint byteMask = (uint)Avx2.MoveMask(v.AsByte()) & 0x11111111u;

        return Bmi2.ParallelBitExtract(byteMask, 0x11111111u); // one bit per lane, bits 0..7
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256ul LoadV256ul(ulong* ptr) => Avx.LoadVector256(ptr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256ul ShiftV256ul(V256ul v, byte shift) => Avx2.ShiftLeftLogical(v, shift);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ExtractFirstBits(V256u v) => Bmi2.ParallelBitExtract((uint)Avx2.MoveMask(v.AsByte()), 0x11111111u);

    public static ulong ExtractBits(ulong* ptr, byte shift)
    {
        V256ul v = Avx2.ShiftLeftLogical(Avx.LoadVector256(ptr), (byte)(63 - shift));
        ulong mask = (uint)Avx2.MoveMask(v.AsByte());
        return Bmi2.X64.ParallelBitExtract(mask, 0x80808080ul);
    }

    public static ulong ExtractBitsX64(ulong* ptr, byte shift)
    {
        V256ul v1 = Avx2.ShiftLeftLogical(Avx.LoadVector256(ptr    ), (byte)(63 - shift));
        V256ul v2 = Avx2.ShiftLeftLogical(Avx.LoadVector256(ptr + 4), (byte)(63 - shift));

        ulong mask1 = (uint)Avx2.MoveMask(v1.AsByte());
        ulong mask2 = (uint)Avx2.MoveMask(v2.AsByte());

        return Bmi2.X64.ParallelBitExtract(mask1 | (mask2 << 32), 0x8080808080808080ul);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetAOTypeMap(ulong row)
    {
        ulong rowXor = row ^ (row >> 1);
        return rowXor | (rowXor << 1) | (rowXor << 2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V256u GetAOTypeMap(V256u row)
    {
        V256u rowXor = row ^ (row >> 1);
        return rowXor | (rowXor << 1) | (rowXor << 2);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetAO(ulong row1, ulong row2, ulong row3, int shift)
    {
        int v1 = (int)((row1 >> shift) & 0b111);
        int v2 = (int)((row2 >> shift) & 0b111);
        int v3 = (int)((row3 >> shift) & 0b111);
        
        int v2_1 = v2 & 1;
        int v2_2 = (v2 & 4) >> 2;

        return v1 | (v2_1 << 3) | (v2_2 << 4) | (v3 << 5);
    }

    private static readonly byte[] _bitSwap = [0, 4, 2, 6, 1, 5, 3, 7];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFlippedAO(ulong row1, ulong row2, ulong row3, int shift)
    {
        int v1 = _bitSwap[(int)(row1 >> shift) & 7];
        int v2 = _bitSwap[(int)(row2 >> shift) & 7];
        int v3 = _bitSwap[(int)(row3 >> shift) & 7];

        int v2_1 = v2 & 1;
        int v2_2 = (v2 & 4) >> 2;

        return v1 | (v2_1 << 3) | (v2_2 << 4) | (v3 << 5);
    }

    private static readonly byte[] _bitSwap2 = [0, 4, 2, 6, 1, 5, 3, 7];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFlippedAO2(ulong row1, ulong row2, ulong row3, int shift)
    {
        int v1 = _bitSwap2[(int)(row1 >> shift) & 7];
        int v2 = _bitSwap2[(int)(row2 >> shift) & 7];
        int v3 = _bitSwap2[(int)(row3 >> shift) & 7];

        int v2_1 = v2 & 1;
        int v2_2 = (v2 & 4) >> 2;

        return v1 | (v2_1 << 3) | (v2_2 << 4) | (v3 << 5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetPackedAO(int ao) => _aoLookup[ao];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetPackedAO2(int ao) => _aoLookup2[ao];


    private static int* _aoLookup = null!;
    private static int* _aoLookup2 = null!;


    public static void Init()
    {
        {
            int Flip2_1(int b) => ((b << 1) | (b >> 1)) & 3;
            int Flip2_2(int b) => ((b << 1) | (b >> 1)) & 6;

            _aoLookup = MemoryHelper.AllocClear<int>(256);

            for (int i = 0; i < 256; i++)
            {
                int ao1 = Flip2_1(i & 3) | ((i >> 1) & 4);
                int ao2 = ((i >> 4) & 6) | ((i >> 3) & 1);
                int ao3 = ((i >> 6) & 3) | ((i >> 2) & 4);
                int ao4 = Flip2_2(i & 6) | ((i >> 4) & 1);

                ao1 = ao1 == 5 ? 3 : Bit.PopCount(ao1);
                ao2 = ao2 == 5 ? 3 : Bit.PopCount(ao2);
                ao3 = ao3 == 5 ? 3 : Bit.PopCount(ao3);
                ao4 = ao4 == 5 ? 3 : Bit.PopCount(ao4);

                int packed = ao1 | (ao2 << 2) | (ao3 << 4) | (ao4 << 6);

                _aoLookup[i] = packed;
            }
        }

        {
            _aoLookup2 = MemoryHelper.AllocClear<int>(256);

            for (int i = 0; i < 256; i++)
            {
                /*
                base bits are

                bit|       |
                ---+-------+-----
                 3 | 0 0 0 |
                 2 | 0 0 0 |
                 1 | 0 0 0 |
                ---+-------+-----
                   | 1 2 3 |front

                the bits that are interesting go from

                          first
                           v
                front 1 00[000]000
                front 2 00[000]000
                front 3 00[000]000
                             ^
                            last

                to

                2_1: first bit of front2
                2_2: last bit of front2

                       1    2_1 2_2    3
                ao: [0|0|0] [0] [0] [0|0|0]
                ----+-+-+-+-+-+-+-+-+-+-+-+-
                    |0|1|2| |3| |4| |5|6|7|


                bits are 0-7

                ao1 bits: 3, 0, 1
                ao2 bits: 1, 2, 4
                ao3 bits: 4, 7, 6
                ao4 bits: 6, 5, 3
                */

                int bit0 = (i >> 0) & 1;
                int bit1 = (i >> 1) & 1;
                int bit2 = (i >> 2) & 1;
                int bit3 = (i >> 3) & 1;
                int bit4 = (i >> 4) & 1;
                int bit5 = (i >> 5) & 1;
                int bit6 = (i >> 6) & 1;
                int bit7 = (i >> 7) & 1;

                int ao1 = (bit3 << 2) | (bit0 << 1) | bit1;
                int ao2 = (bit1 << 2) | (bit2 << 1) | bit4;
                int ao3 = (bit4 << 2) | (bit7 << 1) | bit6;
                int ao4 = (bit6 << 2) | (bit5 << 1) | bit3;

                ao1 = ao1 == 5 ? 3 : Bit.PopCount(ao1);
                ao2 = ao2 == 5 ? 3 : Bit.PopCount(ao2);
                ao3 = ao3 == 5 ? 3 : Bit.PopCount(ao3);
                ao4 = ao4 == 5 ? 3 : Bit.PopCount(ao4);

                int packed = ao1 | (ao2 << 2) | (ao3 << 4) | (ao4 << 6);

                _aoLookup2[i] = packed;
            }
        }
    }

    public static void Cleanup()
    {
        if (_aoLookup != null)
        {
            MemoryHelper.Free(ref _aoLookup);
        }

        if (_aoLookup2 != null)
        {
            MemoryHelper.Free(ref _aoLookup2);
        }
    }
}