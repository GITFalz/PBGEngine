using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using PBG.Core;
using PBG.MathLibrary;

namespace PBG.NewVoxel;

using VH = VoxelHelper;

[InternalSystemInit(InitPriority.Data)]
[InternalSystemCleanup]
public unsafe static class GreedyMesher7YByte
{
    private static int THREAD_COUNT => VoxelRenderer.RenderingThreads;

    public static NewMeshData[] MeshDatas = [];

    public static void Init()
    {
        MeshDatas = new NewMeshData[THREAD_COUNT];

        for (int i = 0; i < THREAD_COUNT; i++)
        {
            MeshDatas[i] = new();
        }
    }

    public static void Cleanup()
    {
        for (int i = 0; i < THREAD_COUNT; i++)
        {
            MeshDatas[i].Dispose();
        }
    }

    public readonly static V256u StateMaskV256 = V256U.New(Block.STATE_MASK);
    public readonly static V256u SolidMaskV256 = V256U.New(1 << Block.STATE_SHIFT);

    public static void GetBitMaps(VoxelChunk chunk, ref NeighbourChunks neighbours, int workerId)
    {
        var meshData =  MeshDatas[workerId];

        ulong* bitMap = meshData.BitMap;
        
        uint* aoTypeMap = meshData.AOTypeMap;
        uint* yaoTypeMap = meshData.YAOTypeMap;
        uint* yBitMap = meshData.YBitMap;

        uint* typeMap = meshData.TypeMap;
        uint* zTypeMap = meshData.ZTypeMap;
        uint* nonSolidMap = meshData.NonSolidMap;

        ulong* topBits = meshData.TopBits;
        ulong* bottomBits = meshData.BottomBits;


        uint* topMask = meshData.RightMask;
        uint* bottomMask = meshData.LeftMask;

        //uint* blockPtr = (uint*)chunk.Blocks;

        int maxYlayerIndex = 31744;

        V256u nzRow1 = V256U.Zero;
        V256u nzRow2 = V256U.Zero;
        V256u nzRow3 = V256U.Zero;
        V256u nzRow4 = V256U.Zero;

        V256u pzRow1 = V256U.Zero;
        V256u pzRow2 = V256U.Zero;
        V256u pzRow3 = V256U.Zero;
        V256u pzRow4 = V256U.Zero;

        for (int z = 0; z < 32; z++)
        {
            int blockIndex = z * 1024;
            int mapIndex = (z + 1) * 34 + 1;

            V256u mem1 = V256U.Zero;
            V256u mem2 = V256U.Zero;
            V256u mem3 = V256U.Zero;
            V256u mem4 = V256U.Zero;

            V256u yRow1 = V256U.Zero;
            V256u yRow2 = V256U.Zero;
            V256u yRow3 = V256U.Zero;
            V256u yRow4 = V256U.Zero;

            uint rowBottom = 0;
            uint rowTop = 0;

            V256u zTypeRow1 = V256U.Zero;
            V256u zTypeRow2 = V256U.Zero;
            V256u zTypeRow3 = V256U.Zero;
            V256u zTypeRow4 = V256U.Zero;

            uint* typePtr     = typeMap;
            ulong* bitPtr     = bitMap + mapIndex;
            uint* aoPtr       = aoTypeMap + mapIndex;

            //uint* blockPtrLocal = blockPtr + z * 1024;

            for (int x = 0; x < 32; x++)
            {
                V256b blockRow = V256B.Load(chunk.ByteBlocks + x * 32 + z * 1024);

                /*
                V256u row1 = V256U.Load(blockPtrLocal);
                V256u row2 = V256U.Load(blockPtrLocal + 8);
                V256u row3 = V256U.Load(blockPtrLocal + 16);
                V256u row4 = V256U.Load(blockPtrLocal + 24);
                */

                var (row1, row2, row3, row4) = VH.WidenToV256u(blockRow);

                /*
                var state1 = row1 & StateMaskV256;
                var state2 = row2 & StateMaskV256;
                var state3 = row3 & StateMaskV256;
                var state4 = row4 & StateMaskV256;

                var isSolid1 = state1.CompareEqual(SolidMaskV256);
                var isSolid2 = state2.CompareEqual(SolidMaskV256);
                var isSolid3 = state3.CompareEqual(SolidMaskV256);
                var isSolid4 = state4.CompareEqual(SolidMaskV256);

                var solidRow1 = row1 & isSolid1;
                var solidRow2 = row2 & isSolid2;
                var solidRow3 = row3 & isSolid3;
                var solidRow4 = row4 & isSolid4;
                */

                var isAir1 = row1.CompareEqual(V256U.Zero);
                var isAir2 = row2.CompareEqual(V256U.Zero);
                var isAir3 = row3.CompareEqual(V256U.Zero);
                var isAir4 = row4.CompareEqual(V256U.Zero);

                var isSolid1 = isAir1 ^ V256U.MaxValue;
                var isSolid2 = isAir2 ^ V256U.MaxValue;
                var isSolid3 = isAir3 ^ V256U.MaxValue;
                var isSolid4 = isAir4 ^ V256U.MaxValue;

                
                //ulong row = VH.GetV256usNonAirBitRowX64(solidRow1, solidRow2, solidRow3, solidRow4); //VH.GetRowSolidBitsV256X64(solidRow1, solidRow2, solidRow3, solidRow4);

                ulong row = VH.GetRowSolidBitsV256X64(isSolid1, isSolid2, isSolid3, isSolid4);
                uint bitTop    = neighbours.Blocks22[blockIndex] != 0 ?     1U : 0;
                uint bitBottom = neighbours.Blocks4[blockIndex + 31] != 0 ? 1U : 0;

                row <<= 1;
                row |= (ulong)bitTop << 33;
                row |= (ulong)bitBottom;
                

                *bitPtr = row;
                *aoPtr = (uint)(VH.GetAOTypeMap(row) >> 1);


                V256b rowOffset = V256B.Load(chunk.ByteBlocks + x * 32 + z * 1024 - 1);

                /*
                // get the rows but shifted by one block
                V256u rowOffset1 = V256U.Load(blockPtrLocal - 1);
                V256u rowOffset2 = V256U.Load(blockPtrLocal + 7);
                V256u rowOffset3 = V256U.Load(blockPtrLocal + 15);
                V256u rowOffset4 = V256U.Load(blockPtrLocal + 23);
                */

                var (rowOffset1, rowOffset2, rowOffset3, rowOffset4) = VH.WidenToV256u(blockRow);

                // compare the normal rows with the shifted rows
                // if a block and a shifted block are the same, the result will be equal to true else false, now we know when a block changes
                V256u equal1 = row1.CompareEqual(rowOffset1);
                V256u equal2 = row2.CompareEqual(rowOffset2);
                V256u equal3 = row3.CompareEqual(rowOffset3);
                V256u equal4 = row4.CompareEqual(rowOffset4);   

                /*
                uint mask1 = (uint)~Avx2.MoveMask(equal1.AsByte()) & 0x11111111u;
                uint mask2 = (uint)~Avx2.MoveMask(equal2.AsByte()) & 0x11111111u;
                uint mask3 = (uint)~Avx2.MoveMask(equal3.AsByte()) & 0x11111111u;
                uint mask4 = (uint)~Avx2.MoveMask(equal4.AsByte()) & 0x11111111u;

                ulong compact1 = Bmi2.X64.ParallelBitExtract(mask1 | ((ulong)(mask2) << 32), 0x1111111111111111ul);
                ulong compact2 = Bmi2.X64.ParallelBitExtract(mask3 | ((ulong)(mask4) << 32), 0x1111111111111111ul);

                // v1
                uint typeCheck = (uint)(compact1 | (compact2 << 16)) & 0xFFFFFFFEu;
                */
                

                // v2
                uint typeCheck = (~VH.GetRowSolidBitsV256X64(equal1, equal2, equal3, equal4)) & 0xFFFFFFFEu;
                
                *typePtr = typeCheck;



                V256u ybit1 = isSolid1 & V256U.One;
                V256u ybit2 = isSolid2 & V256U.One;
                V256u ybit3 = isSolid3 & V256U.One;
                V256u ybit4 = isSolid4 & V256U.One;

                // shift the bits to their place in the map
                V256u yshift1 = ybit1 << (byte)x;
                V256u yshift2 = ybit2 << (byte)x;
                V256u yshift3 = ybit3 << (byte)x;
                V256u yshift4 = ybit4 << (byte)x;

                yRow1 |= yshift1;
                yRow2 |= yshift2;
                yRow3 |= yshift3;
                yRow4 |= yshift4;


                rowTop    |= bitTop    << x;
                rowBottom |= bitBottom << x;


                // handle the type checking for the y axis
                if (x > 0)
                {   
                    // check if block are unequal
                    V256u zequal1 = row1.CompareEqual(mem1);
                    V256u zequal2 = row2.CompareEqual(mem2);
                    V256u zequal3 = row3.CompareEqual(mem3);
                    V256u zequal4 = row4.CompareEqual(mem4);

                    // invert the equality
                    V256u znot1 = zequal1 ^ V256U.MaxValue;
                    V256u znot2 = zequal2 ^ V256U.MaxValue;
                    V256u znot3 = zequal3 ^ V256U.MaxValue;
                    V256u znot4 = zequal4 ^ V256U.MaxValue;

                    // make it so the values are either 0 or 1
                    V256u zbit1 = znot1 & V256U.One;
                    V256u zbit2 = znot2 & V256U.One;
                    V256u zbit3 = znot3 & V256U.One;
                    V256u zbit4 = znot4 & V256U.One;

                    // shift the bits to their place in the map
                    V256u zshift1 = zbit1 << (byte)x;
                    V256u zshift2 = zbit2 << (byte)x;
                    V256u zshift3 = zbit3 << (byte)x;
                    V256u zshift4 = zbit4 << (byte)x;

                    zTypeRow1 |= zshift1;
                    zTypeRow2 |= zshift2;
                    zTypeRow3 |= zshift3;
                    zTypeRow4 |= zshift4;
                }

                mem1 = row1;
                mem2 = row2;
                mem3 = row3;
                mem4 = row4;

                /*
                // get the non solid blocks
                var isNotSolid1 = isSolid1 ^ V256U.MaxValue;
                var isNotSolid2 = isSolid2 ^ V256U.MaxValue;
                var isNotSolid3 = isSolid3 ^ V256U.MaxValue;
                var isNotSolid4 = isSolid4 ^ V256U.MaxValue;

                var nonSolidRow1 = row1 & isNotSolid1;
                var nonSolidRow2 = row2 & isNotSolid2;
                var nonSolidRow3 = row3 & isNotSolid3;
                var nonSolidRow4 = row4 & isNotSolid4;

                uint nonSolidRow = (uint)VH.GetV256usNonAirBitRowX64(nonSolidRow1, nonSolidRow2, nonSolidRow3, nonSolidRow4);

                nonSolidMap[index] = nonSolidRow;


                
                */

                mapIndex++;
                blockIndex += 32;

                bitPtr++;
                aoPtr++;
                typePtr++;  
                //blockPtrLocal += 32;
            }


            int yIndex = z * 1024;
            int zIndexY = z * 32;

            int rowZindex = (z + 1) * 34;
            


            Avx.Store(zTypeMap + zIndexY,      zTypeRow1);
            Avx.Store(zTypeMap + zIndexY + 8,  zTypeRow2);
            Avx.Store(zTypeMap + zIndexY + 16, zTypeRow3);
            Avx.Store(zTypeMap + zIndexY + 24, zTypeRow4);



            yBitMap[z + 1123]  = 0;//rowTop;
            yBitMap[z + 1]     = 0;//rowBottom;

            yBitMap[rowZindex] = rowBottom;//uint.MaxValue;
            yBitMap[rowZindex + 33] = rowTop;//uint.MaxValue;

            yaoTypeMap[z + 1123]  = 0;//VH.GetAOTypeMap(rowTop);
            yaoTypeMap[z + 1]     = 0;//VH.GetAOTypeMap(rowBottom);

            yaoTypeMap[rowZindex] = (uint)VH.GetAOTypeMap(rowBottom);//uint.MaxValue;
            yaoTypeMap[rowZindex + 33] = (uint)VH.GetAOTypeMap(rowTop);//uint.MaxValue;

            Avx.Store(yBitMap + rowZindex + 1, yRow1);
            Avx.Store(yBitMap + rowZindex + 9, yRow2);
            Avx.Store(yBitMap + rowZindex + 17, yRow3);
            Avx.Store(yBitMap + rowZindex + 25, yRow4);

            Avx.Store(yaoTypeMap + rowZindex + 1, VH.GetAOTypeMap(yRow1));
            Avx.Store(yaoTypeMap + rowZindex + 9, VH.GetAOTypeMap(yRow2));
            Avx.Store(yaoTypeMap + rowZindex + 17, VH.GetAOTypeMap(yRow3));
            Avx.Store(yaoTypeMap + rowZindex + 25, VH.GetAOTypeMap(yRow4));
            


            int indexNx = 992 + yIndex;
            int indexPx = yIndex;

            int indexNz = zIndexY + maxYlayerIndex;
            int indexPz = zIndexY;

            byte* ptr12 = neighbours.Blocks12 + indexNx;
            byte* ptr14 = neighbours.Blocks14 + indexPx;
            byte* ptr10 = neighbours.Blocks10 + indexNz;
            byte* ptr16 = neighbours.Blocks16 + indexPz;

            // rows in the direction of y, on the plane P (+) / N (-), on the axis x / z

            // handle chunk that is in -x at index 12 and i need the last block
            ulong rowNx = GetSolidRow(ptr12);

            // handle chunk that is in +x at index 14 and i need the first block
            ulong rowPx = GetSolidRow(ptr14);

            // handle chunk that is in -z at index 10 and i need the last block
            ulong rowNz = GetSolidRow(ptr10, z, ref nzRow1, ref nzRow2, ref nzRow3, ref nzRow4);

            // handle chunk that is in +z at index 16 and i need the first block
            ulong rowPz = GetSolidRow(ptr16, z, ref pzRow1, ref pzRow2, ref pzRow3, ref pzRow4);

            bitMap[rowZindex] = rowNx;
            bitMap[33 + rowZindex] = rowPx;

            bitMap[z + 1] = rowNz;
            bitMap[1123 + z] = rowPz;


            aoTypeMap[rowZindex] = (uint)(VH.GetAOTypeMap(rowNx) >> 1);
            aoTypeMap[33 + rowZindex] = (uint)(VH.GetAOTypeMap(rowPx) >> 1);

            aoTypeMap[z + 1] = (uint)(VH.GetAOTypeMap(rowNz) >> 1);
            aoTypeMap[1123 + z] = (uint)(VH.GetAOTypeMap(rowPz) >> 1);
        }

        // store the shifted rows for the z axis
        Avx.Store(yBitMap + 1, nzRow1);
        Avx.Store(yBitMap + 9, nzRow2);
        Avx.Store(yBitMap + 17, nzRow3);
        Avx.Store(yBitMap + 25, nzRow4);

        Avx.Store(yBitMap + 1123, pzRow1);
        Avx.Store(yBitMap + 1131, pzRow2);
        Avx.Store(yBitMap + 1139, pzRow3);
        Avx.Store(yBitMap + 1147, pzRow4);

        // store the ao types
        Avx.Store(yaoTypeMap + 1, VH.GetAOTypeMap(nzRow1));
        Avx.Store(yaoTypeMap + 9, VH.GetAOTypeMap(nzRow2));
        Avx.Store(yaoTypeMap + 17, VH.GetAOTypeMap(nzRow3));
        Avx.Store(yaoTypeMap + 25, VH.GetAOTypeMap(nzRow4));

        Avx.Store(yaoTypeMap + 1123, VH.GetAOTypeMap(pzRow1));
        Avx.Store(yaoTypeMap + 1131, VH.GetAOTypeMap(pzRow2));
        Avx.Store(yaoTypeMap + 1139, VH.GetAOTypeMap(pzRow3));
        Avx.Store(yaoTypeMap + 1147, VH.GetAOTypeMap(pzRow4));
    }

    private static ulong GetSolidRow(byte* ptr)
    {
        V256b blockRow = V256B.Load(ptr);

        var (row1, row2, row3, row4) = VH.WidenToV256u(blockRow);

        /*
        var state1 = row1 & StateMaskV256;
        var state2 = row2 & StateMaskV256;
        var state3 = row3 & StateMaskV256;
        var state4 = row4 & StateMaskV256;

        var isSolid1 = state1.CompareEqual(SolidMaskV256);
        var isSolid2 = state2.CompareEqual(SolidMaskV256);
        var isSolid3 = state3.CompareEqual(SolidMaskV256);
        var isSolid4 = state4.CompareEqual(SolidMaskV256);
        */

        var isAir1 = row1.CompareEqual(V256U.Zero);
        var isAir2 = row2.CompareEqual(V256U.Zero);
        var isAir3 = row3.CompareEqual(V256U.Zero);
        var isAir4 = row4.CompareEqual(V256U.Zero);

        var isSolid1 = isAir1 ^ V256U.MaxValue;
        var isSolid2 = isAir2 ^ V256U.MaxValue;
        var isSolid3 = isAir3 ^ V256U.MaxValue;
        var isSolid4 = isAir4 ^ V256U.MaxValue;



        return (ulong)VH.GetRowSolidBitsV256X64(isSolid1, isSolid2, isSolid3, isSolid4) << 1;
    }

    private static ulong GetSolidRow(byte* ptr, int z, ref V256u sRow1, ref V256u sRow2, ref V256u sRow3, ref V256u sRow4)
    {
        V256b blockRow = V256B.Load(ptr);

        var (row1, row2, row3, row4) = VH.WidenToV256u(blockRow);

       /*
        var state1 = row1 & StateMaskV256;
        var state2 = row2 & StateMaskV256;
        var state3 = row3 & StateMaskV256;
        var state4 = row4 & StateMaskV256;

        var isSolid1 = state1.CompareEqual(SolidMaskV256);
        var isSolid2 = state2.CompareEqual(SolidMaskV256);
        var isSolid3 = state3.CompareEqual(SolidMaskV256);
        var isSolid4 = state4.CompareEqual(SolidMaskV256);
        */

        var isAir1 = row1.CompareEqual(V256U.Zero);
        var isAir2 = row2.CompareEqual(V256U.Zero);
        var isAir3 = row3.CompareEqual(V256U.Zero);
        var isAir4 = row4.CompareEqual(V256U.Zero);

        var isSolid1 = isAir1 ^ V256U.MaxValue;
        var isSolid2 = isAir2 ^ V256U.MaxValue;
        var isSolid3 = isAir3 ^ V256U.MaxValue;
        var isSolid4 = isAir4 ^ V256U.MaxValue;

        // row shift
        V256u ybit1 = isSolid1 & V256U.One;
        V256u ybit2 = isSolid2 & V256U.One;
        V256u ybit3 = isSolid3 & V256U.One;
        V256u ybit4 = isSolid4 & V256U.One;

        // shift the bits to their place in the map
        V256u yshift1 = ybit1 << (byte)z;
        V256u yshift2 = ybit2 << (byte)z;
        V256u yshift3 = ybit3 << (byte)z;
        V256u yshift4 = ybit4 << (byte)z;

        sRow1 |= yshift1;
        sRow2 |= yshift2;
        sRow3 |= yshift3;
        sRow4 |= yshift4;

        return (ulong)VH.GetRowSolidBitsV256X64(isSolid1, isSolid2, isSolid3, isSolid4) << 1;
    }

    private static void BuildMesh(VoxelChunk chunk, ref MeshMapping mapping, int workerId)
    {
        var meshData = MeshDatas[workerId];
        
        ulong* bitMap = meshData.BitMap;
        
        uint* aoTypeMap = meshData.AOTypeMap;
        uint* yaoTypeMap = meshData.YAOTypeMap;
        uint* yBitMap = meshData.YBitMap;

        uint* typeMap = meshData.TypeMap;
        uint* zTypeMap = meshData.ZTypeMap;
        uint* nonSolidMap = meshData.NonSolidMap;

        ulong* topBits = meshData.TopBits;
        ulong* bottomBits = meshData.BottomBits;
        

        uint* frontSlice = meshData.FrontSlice;
        uint* backSlice = meshData.BackSlice;

        uint* topSlice = meshData.TopSlice;
        uint* bottomSlice = meshData.BottomSlice;

        uint* rightSlice = meshData.RightSlice;
        uint* leftSlice = meshData.LeftSlice;

        for (int i = 0; i < 32; i++)
        {  
            int iy = (i + 1) * 32;
            int iz = (i + 1) * 34;

            for (int s = 0; s < 32; s++)
            {
                int sz = (s + 1) * 34;
                int si = i + 1 + sz;

                frontSlice[s]   = (uint)(bitMap[iz + s + 1] >> 1);
                backSlice[s]    = (uint)(bitMap[iz + s + 1] >> 1);

                rightSlice[s]   = (uint)(bitMap[si] >> 1);
                leftSlice[s]    = (uint)(bitMap[si] >> 1);

                topSlice[s]     = yBitMap[si];
                bottomSlice[s]  = yBitMap[si];
            }

            HandleGreedyFrontAndBack(chunk, ref mapping, bitMap, aoTypeMap, typeMap, frontSlice, backSlice, i);
            HandleGreedyRightAndLeft(chunk, ref mapping, bitMap, aoTypeMap, typeMap, rightSlice, leftSlice, i);
            HandleGreedyTopAndBottom(chunk, ref mapping, yBitMap, yaoTypeMap, zTypeMap, topSlice, bottomSlice, i);
        }
    }

    
    public static bool GenerateMesh(VoxelChunk chunk, ref MeshMapping mapping, int workerId)
    {
        NeighbourChunks neighbours = new(chunk.Renderer, chunk.RelativePosition);

        GetBitMaps(chunk, ref neighbours, workerId);
        BuildMesh(chunk, ref mapping, workerId);   

        return true;
    }

    public static void HandleGreedyFrontAndBack(VoxelChunk chunk, ref MeshMapping mapping, ulong* bitMap, uint* aoTypeMap, uint* typeMap, uint* frontData, uint* backData, int z)
    {
        int frontZindex = z * 34;
        int backZindex = (z + 2) * 34;

        ulong* frontBitMapPtr = bitMap + frontZindex;
        uint *frontAoTypeMapPtr = aoTypeMap + frontZindex;
        uint *frontTypeMapPtr = typeMap + z * 32;

        uint frontAoType1 = frontAoTypeMapPtr[0];
        uint frontAoType2 = frontAoTypeMapPtr[1];

        ulong frontFront1 = frontBitMapPtr[0];
        ulong frontFront2 = frontBitMapPtr[1];

        ulong* backBitMapPtr = bitMap + backZindex;
        uint* backAoTypeMapPtr = aoTypeMap + backZindex;
        uint* backTypeMapPtr = typeMap + z * 32 + 2;

        uint backAoType1 = backAoTypeMapPtr[0];
        uint backAoType2 = backAoTypeMapPtr[1];

        ulong backFront1 = backBitMapPtr[0];
        ulong backFront2 = backBitMapPtr[1];

        byte* blockPtr = chunk.ByteBlocks + z * 1024;

        for (int i = 0; i < 32; i++)
        {
            // Front
            {
                uint aoType3 = frontAoTypeMapPtr[i + 2];
                ulong front3 = frontBitMapPtr[i + 2];

                uint type = frontTypeMapPtr[i] | frontAoType1 | frontAoType2 | aoType3;
                uint row = frontData[i] & (uint)((~frontFront2) >> 1);

                while (row != 0)
                {
                    int trailingZeros = Bit.TrailingZeros(row);

                    uint newRow = row >> trailingZeros;
                    uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                    int h = Bit.TrailingZeros((~newRow) | newType);

                    uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                    row &= ~mask;
                    type &= ~mask;

                    int w = 1;

                    byte block = blockPtr[trailingZeros + i * 32];
                                    
                    int ao = VH.GetAO(frontFront1, frontFront2, front3, trailingZeros);

                    uint sAoType1 = frontAoType2;
                    uint sAoType2 = aoType3;

                    ulong sFront1 = frontFront2;
                    ulong sFront2 = front3;

                    while (w < 32 - i)
                    {
                        int iw = i + w; // base iw is i + 1

                        uint sAoType3 = frontAoTypeMapPtr[iw + 2];
                        ulong sFront3 = frontBitMapPtr[iw + 2];

                        uint sType = frontTypeMapPtr[iw] | sAoType1 | sAoType2 | sAoType3;
                        uint sRow = frontData[iw] & (uint)((~sFront2) >> 1);

                        sRow &= ~(sType & Bit.Invert(trailingZeros));

                        int sAo = VH.GetAO(sFront1, sFront2, sFront3, trailingZeros);
                        byte sBlock = blockPtr[trailingZeros + iw * 32];

                        if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                            break;

                        frontData[iw] &= ~mask;
                        w++;

                        sAoType1 = sAoType2;
                        sAoType2 = sAoType3;

                        sFront1 = sFront2;
                        sFront2 = sFront3;
                    }

                    w--;
                    h--;

                    int packedAo = VH.GetPackedAO2(ao);

                    var geometryIndex = new Block(block).GetSolidGeometryIndex(0);
                    mapping.AddFace(new(geometryIndex, i | (trailingZeros << 5) | (z << 10), packedAo, w | (h << 5)));
                }

                frontAoType1 = frontAoType2;
                frontAoType2 = aoType3;

                frontFront1 = frontFront2;
                frontFront2 = front3;
            }

            // Back
            {
                uint aoType3 = backAoTypeMapPtr[i + 2];
                ulong front3 = backBitMapPtr[i + 2];

                uint type = backTypeMapPtr[i] | backAoType1 | backAoType2 | aoType3;
                uint row = backData[i] & (uint)((~backFront2) >> 1);

                while (row != 0)
                {
                    int trailingZeros = Bit.TrailingZeros(row);

                    uint newRow = row >> trailingZeros;
                    uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                    int h = Bit.TrailingZeros((~newRow) | newType);

                    uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                    row &= ~mask;
                    type &= ~mask;

                    int w = 1;

                    byte block = blockPtr[trailingZeros + i * 32];
                                    
                    int ao = VH.GetAO(front3, backFront2, backFront1, trailingZeros);

                    uint sAoType1 = backAoType2;
                    uint sAoType2 = aoType3;

                    ulong sFront1 = backFront2;
                    ulong sFront2 = front3;

                    while (w < 32 - i)
                    {
                        int iw = i + w;

                        uint sAoType3 = backAoTypeMapPtr[iw + 2];
                        ulong sFront3 = backBitMapPtr[iw + 2];

                        uint sType = backTypeMapPtr[iw] | sAoType1 | sAoType2 | sAoType3;
                        uint sRow = backData[iw] & (uint)((~sFront2) >> 1);

                        sRow &= ~(sType & Bit.Invert(trailingZeros));

                        int sAo = VH.GetAO(sFront3, sFront2, sFront1, trailingZeros);
                        byte sBlock = blockPtr[trailingZeros + iw * 32];

                        if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                            break;

                        backData[iw] &= ~mask;
                        w++;

                        sAoType1 = sAoType2;
                        sAoType2 = sAoType3;

                        sFront1 = sFront2;
                        sFront2 = sFront3;
                    }

                    w--;
                    h--;

                    int packedAo = VH.GetPackedAO2(ao);

                    var geometryIndex = new Block(block).GetSolidGeometryIndex(5);
                    mapping.AddFace(new(geometryIndex, i | (trailingZeros << 5) | (z << 10), packedAo, w | (h << 5)));
                }

                backAoType1 = backAoType2;
                backAoType2 = aoType3;

                backFront1 = backFront2;
                backFront2 = front3;
            }
        }
    }


    public static void HandleGreedyRightAndLeft(VoxelChunk chunk, ref MeshMapping mapping, ulong* bitMap, uint* aoTypeMap, uint* typeMap, uint* rightData, uint* leftData, int x)
    {
        uint rightAoType1 = aoTypeMap[x + 2];
        uint rightAoType2 = aoTypeMap[x + 2 + 34];

        ulong rightFront1 = bitMap[x + 2];
        ulong rightFront2 = bitMap[x + 2 + 34];

        uint leftAoType1 = aoTypeMap[x];
        uint leftAoType2 = aoTypeMap[x + 34];

        ulong leftFront1 = bitMap[x];
        ulong leftFront2 = bitMap[x + 34];

        for (int i = 0; i < 32; i++)
        {
            // Right
            {
                uint aoType3 = aoTypeMap[x + 2 + (i + 2) * 34];
                ulong front3 = bitMap[x + 2 + (i + 2) * 34];

                uint type = typeMap[x + i * 32] | rightAoType1 | rightAoType2 | aoType3;
                uint row = leftData[i] & (uint)((~rightFront2) >> 1);

                while (row != 0)
                {
                    int trailingZeros = Bit.TrailingZeros(row);

                    uint newRow = row >> trailingZeros;
                    uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                    int h = Bit.TrailingZeros((~newRow) | newType);

                    uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                    row &= ~mask;
                    type &= ~mask;

                    int w = 1;

                    Block block = chunk.Get(trailingZeros + x * 32 + i * 1024);

                    int ao = VH.GetAO(rightFront1, rightFront2, front3 ,trailingZeros);

                    uint sAoType1 = rightAoType2;
                    uint sAoType2 = aoType3;

                    ulong sFront1 = rightFront2;
                    ulong sFront2 = front3;
                    
                    while (w < 32 - i)
                    {
                        int iw = i + w;

                        uint sAoType3 = aoTypeMap[x + 2 + (i + 2 + w) * 34];
                        ulong sFront3 = bitMap[x + 2 + (i + 2 + w) * 34];

                        uint sType = typeMap[x + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                        uint sRow = leftData[iw] & (uint)((~sFront2) >> 1);

                        sRow &= ~(sType & Bit.Invert(trailingZeros));

                        int sAo = VH.GetAO(sFront1, sFront2, sFront3, trailingZeros);
                        Block sBlock = chunk.Get(trailingZeros + x * 32 + iw * 1024);

                        if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                            break;

                        leftData[iw] &= ~mask;
                        w++;

                        sAoType1 = sAoType2;
                        sAoType2 = sAoType3;

                        sFront1 = sFront2;
                        sFront2 = sFront3;
                    }

                    w--;
                    h--;

                    int packedAo = VH.GetPackedAO2(ao);

                    var geometryIndex = block.GetSolidGeometryIndex(1);
                    mapping.AddFace(new(geometryIndex, x | (trailingZeros << 5) | (i << 10), packedAo, (h << 5) | (w << 10)));
                }

                rightAoType1 = rightAoType2;
                rightAoType2 = aoType3;

                rightFront1 = rightFront2;
                rightFront2 = front3;
            }

            // Left
            {
                uint aoType3 = (uint)aoTypeMap[x + (i + 2) * 34];
                ulong front3 = bitMap[x + (i + 2) * 34];

                uint type = typeMap[x + i * 32] | leftAoType1 | leftAoType2 | aoType3;
                uint row = rightData[i] & (uint)((~leftFront2) >> 1);

                while (row != 0)
                {
                    int trailingZeros = Bit.TrailingZeros(row);

                    uint newRow = row >> trailingZeros;
                    uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                    int h = Bit.TrailingZeros((~newRow) | newType);

                    uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                    row &= ~mask;
                    type &= ~mask;

                    int w = 1;

                    Block block = chunk.Get(trailingZeros + x * 32 + i * 1024);

                    int ao = VH.GetAO(front3, leftFront2, leftFront1, trailingZeros);

                    uint sAoType1 = leftAoType2;
                    uint sAoType2 = aoType3;

                    ulong sFront1 = leftFront2;
                    ulong sFront2 = front3;

                    while (w < 32 - i)
                    {
                        int iw = i + w;

                        uint sAoType3 = (uint)aoTypeMap[x + (i + 2 + w) * 34];
                        ulong sFront3 = bitMap[x + (i + 2 + w) * 34];

                        uint sType = typeMap[x + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                        uint sRow = rightData[iw] & (uint)((~sFront2) >> 1);

                        sRow &= ~(sType & Bit.Invert(trailingZeros));

                        int sAo = VH.GetAO(sFront3, sFront2, sFront1, trailingZeros);
                        Block sBlock = chunk.Get(trailingZeros + x * 32 + iw * 1024);

                        if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                            break;

                        rightData[iw] &= ~mask;
                        w++;

                        sAoType1 = sAoType2;
                        sAoType2 = sAoType3;

                        sFront1 = sFront2;
                        sFront2 = sFront3;
                    }

                    w--;
                    h--;

                    int packedAo = VH.GetPackedAO2(ao);

                    var geometryIndex = block.GetSolidGeometryIndex(3);
                    mapping.AddFace(new(geometryIndex, x | (trailingZeros << 5) | (i << 10), packedAo, (h << 5) | (w << 10)));
                }

                leftAoType1 = leftAoType2;
                leftAoType2 = aoType3;

                leftFront1 = leftFront2;
                leftFront2 = front3;
            }
        }
    }



    public static void HandleGreedyTopAndBottom(VoxelChunk chunk, ref MeshMapping mapping, uint* bitMap, uint* aoTypeMap, uint* typeMap, uint* topData, uint* bottomData, int y)
    {
        uint topAoType1 = aoTypeMap[y + 2];
        uint topAoType2 = aoTypeMap[y + 2 + 34];

        ulong topFront1 = (ulong)bitMap[y + 2] << 1;
        ulong topFront2 = (ulong)bitMap[y + 2 + 34] << 1;

        uint bottomAoType1 = aoTypeMap[y];
        uint bottomAoType2 = aoTypeMap[y + 34];

        ulong bottomFront1 = (ulong)bitMap[y] << 1;
        ulong bottomFront2 = (ulong)bitMap[y + 34] << 1;

        for (int i = 0; i < 32; i++)
        {
            // Top
            {
                uint aoType3 = aoTypeMap[y + 2 + (i + 2) * 34];
                ulong front3 = (ulong)bitMap[y + 2 + (i + 2) * 34] << 1;

                uint type = typeMap[y + i * 32] | topAoType1 | topAoType2 | aoType3;
                uint row = topData[i] & (uint)((~topFront2) >> 1);

                while (row != 0)
                {
                    int trailingZeros = Bit.TrailingZeros(row);

                    uint newRow = row >> trailingZeros;
                    uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                    int h = Bit.TrailingZeros((~newRow) | newType);

                    uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                    row &= ~mask;
                    type &= ~mask;

                    int w = 1;

                    Block block = chunk.Get(y + trailingZeros * 32 + i * 1024);

                    int ao = VH.GetAO(topFront1, topFront2, front3, trailingZeros);

                    uint sAoType1 = topAoType2;
                    uint sAoType2 = aoType3;

                    ulong sFront1 = topFront2;
                    ulong sFront2 = front3;

                    while (w < 32 - i)
                    {
                        int iw = i + w;

                        uint sAoType3 = aoTypeMap[y + 2 + (iw + 2) * 34];
                        ulong sFront3 = (ulong)bitMap[y + 2 + (iw + 2) * 34] << 1;

                        uint sType = typeMap[y + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                        uint sRow = topData[iw] & (uint)((~sFront2) >> 1);

                        sRow &= ~(sType & Bit.Invert(trailingZeros));

                        int sAo = VH.GetAO(sFront1, sFront2, sFront3, trailingZeros);
                        Block sBlock = chunk.Get(y + trailingZeros * 32 + iw * 1024);

                        if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                            break;

                        topData[iw] &= ~mask;
                        w++;

                        sAoType1 = sAoType2;
                        sAoType2 = sAoType3;

                        sFront1 = sFront2;
                        sFront2 = sFront3;
                    }

                    w--;
                    h--;

                    int packedAo = VH.GetPackedAO(ao);

                    var geometryIndex = block.GetSolidGeometryIndex(2);
                    mapping.AddFace(new(geometryIndex, trailingZeros | (y << 5) | (i << 10), packedAo, h | (w << 10)));
                }

                topAoType1 = topAoType2;
                topAoType2 = aoType3;

                topFront1 = topFront2;
                topFront2 = front3;
            }

            // Bottom
            {
                uint aoType3 = aoTypeMap[y + (i + 2) * 34];
                ulong front3 = (ulong)bitMap[y + (i + 2) * 34] << 1;
                
                uint type = typeMap[y + i * 32] | bottomAoType1 | bottomAoType2 | aoType3;
                uint row = bottomData[i] & (uint)((~bottomFront2) >> 1);

                while (row != 0)
                {
                    int trailingZeros = Bit.TrailingZeros(row);

                    uint newRow = row >> trailingZeros;
                    uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                    int h = Bit.TrailingZeros((~newRow) | newType);

                    uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                    row &= ~mask;
                    type &= ~mask;

                    int w = 1;

                    Block block = chunk.Get(y + trailingZeros * 32 + i * 1024);

                    int ao = VH.GetFlippedAO(bottomFront1, bottomFront2, front3, trailingZeros);

                    uint sAoType1 = bottomAoType2;
                    uint sAoType2 = aoType3;

                    ulong sFront1 = bottomFront2;
                    ulong sFront2 = front3;
                    
                    while (w < 32 - i)
                    {
                        int iw = i + w;

                        uint sAoType3 = aoTypeMap[y + (iw + 2) * 34];
                        ulong sFront3 = (ulong)bitMap[y + (iw + 2) * 34] << 1;

                        uint sType = typeMap[y + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                        uint sRow = bottomData[iw] & (uint)((~sFront2) >> 1);

                        sRow &= ~(sType & Bit.Invert(trailingZeros));

                        int sAo = VH.GetFlippedAO(sFront1, sFront2, sFront3, trailingZeros);
                        Block sBlock = chunk.Get(y + trailingZeros * 32 + iw * 1024);

                        if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                            break;

                        bottomData[iw] &= ~mask;
                        w++;

                        sAoType1 = sAoType2;
                        sAoType2 = sAoType3;

                        sFront1 = sFront2;
                        sFront2 = sFront3;
                    }

                    w--;
                    h--;

                    int packedAo = VH.GetPackedAO(ao);

                    var geometryIndex = block.GetSolidGeometryIndex(4);
                    mapping.AddFace(new(geometryIndex, trailingZeros | (y << 5) | (i << 10), packedAo, h | (w << 10)));
                }

                bottomAoType1 = bottomAoType2;
                bottomAoType2 = aoType3;

                bottomFront1 = bottomFront2;
                bottomFront2 = front3;
            }
        }
    }



    private static ulong ExtractBitMap(ulong* ptr, byte shift) =>
        (ulong)VH.ExtractBitsX64(ptr + 0,  shift)       | 
        (ulong)VH.ExtractBitsX64(ptr + 8,  shift) << 8  | 
        (ulong)VH.ExtractBitsX64(ptr + 16, shift) << 16 | 
        (ulong)VH.ExtractBitsX64(ptr + 24, shift) << 24 |
        (ulong)VH.ExtractBits(   ptr + 32, shift) << 32;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Copy32(uint* src, uint* dst) => Buffer.MemoryCopy(src, dst, 128, 128);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Copy(uint* src, uint* dst, ulong srcByteSize, ulong dstByteSize) => Buffer.MemoryCopy(src, dst, dstByteSize, srcByteSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Copy(ulong* src, ulong* dst, ulong srcByteSize, ulong dstByteSize) => Buffer.MemoryCopy(src, dst, dstByteSize, srcByteSize);


    public unsafe struct NeighbourChunks
    {
        public byte* Blocks0;  // (-1, -1, -1)
        public byte* Blocks1;  // ( 0, -1, -1)
        public byte* Blocks2;  // ( 1, -1, -1)

        public byte* Blocks3;  // (-1, -1,  0)
        public byte* Blocks4;  // ( 0, -1,  0)
        public byte* Blocks5;  // ( 1, -1,  0)

        public byte* Blocks6;  // (-1, -1,  1)
        public byte* Blocks7;  // ( 0, -1,  1)
        public byte* Blocks8;  // ( 1, -1,  1)

        public byte* Blocks9;  // (-1,  0, -1)
        public byte* Blocks10; // ( 0,  0, -1)
        public byte* Blocks11; // ( 1,  0, -1)

        public byte* Blocks12; // (-1,  0,  0)
        // 13 skipped — (0, 0, 0) is the chunk being rendered
        public byte* Blocks14; // ( 1,  0,  0)

        public byte* Blocks15; // (-1,  0,  1)
        public byte* Blocks16; // ( 0,  0,  1)
        public byte* Blocks17; // ( 1,  0,  1)

        public byte* Blocks18; // (-1,  1, -1)
        public byte* Blocks19; // ( 0,  1, -1)
        public byte* Blocks20; // ( 1,  1, -1)

        public byte* Blocks21; // (-1,  1,  0)
        public byte* Blocks22; // ( 0,  1,  0)
        public byte* Blocks23; // ( 1,  1,  0)

        public byte* Blocks24; // (-1,  1,  1)
        public byte* Blocks25; // ( 0,  1,  1)
        public byte* Blocks26; // ( 1,  1,  1)

        public NeighbourChunks(VoxelRenderer renderer, Vector3i relativePosition)
        {
            Blocks0  = GetBlocks(renderer, relativePosition, -1, -1, -1);
            Blocks1  = GetBlocks(renderer, relativePosition,  0, -1, -1);
            Blocks2  = GetBlocks(renderer, relativePosition,  1, -1, -1);

            Blocks3  = GetBlocks(renderer, relativePosition, -1, -1,  0);
            Blocks4  = GetBlocks(renderer, relativePosition,  0, -1,  0);
            Blocks5  = GetBlocks(renderer, relativePosition,  1, -1,  0);

            Blocks6  = GetBlocks(renderer, relativePosition, -1, -1,  1);
            Blocks7  = GetBlocks(renderer, relativePosition,  0, -1,  1);
            Blocks8  = GetBlocks(renderer, relativePosition,  1, -1,  1);

            Blocks9  = GetBlocks(renderer, relativePosition, -1,  0, -1);
            Blocks10 = GetBlocks(renderer, relativePosition,  0,  0, -1);
            Blocks11 = GetBlocks(renderer, relativePosition,  1,  0, -1);

            Blocks12 = GetBlocks(renderer, relativePosition, -1,  0,  0);
            Blocks14 = GetBlocks(renderer, relativePosition,  1,  0,  0);

            Blocks15 = GetBlocks(renderer, relativePosition, -1,  0,  1);
            Blocks16 = GetBlocks(renderer, relativePosition,  0,  0,  1);
            Blocks17 = GetBlocks(renderer, relativePosition,  1,  0,  1);

            Blocks18 = GetBlocks(renderer, relativePosition, -1,  1, -1);
            Blocks19 = GetBlocks(renderer, relativePosition,  0,  1, -1);
            Blocks20 = GetBlocks(renderer, relativePosition,  1,  1, -1);

            Blocks21 = GetBlocks(renderer, relativePosition, -1,  1,  0);
            Blocks22 = GetBlocks(renderer, relativePosition,  0,  1,  0);
            Blocks23 = GetBlocks(renderer, relativePosition,  1,  1,  0);

            Blocks24 = GetBlocks(renderer, relativePosition, -1,  1,  1);
            Blocks25 = GetBlocks(renderer, relativePosition,  0,  1,  1);
            Blocks26 = GetBlocks(renderer, relativePosition,  1,  1,  1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte* GetBlocks(VoxelRenderer renderer, Vector3i relativePosition, int ox, int oy, int oz)
        {
            relativePosition.X += ox;
            relativePosition.Y += oy;
            relativePosition.Z += oz;
            return renderer.GetChunk(relativePosition, out var chunk) ? chunk.ByteBlocks : VoxelChunk.Empty.ByteBlocks;
        }
    }

    public sealed class NewMeshData
    {
        const int MAP_34_SIZE = 34 * 34;
        const int MAP_32_SIZE = 32 * 32;
        
        // ulong maps 34 x 34
        public ulong* BitMap;
        
        // uint maps 34 x 34
        public uint* YBitMap;
        public uint* AOTypeMap;
        public uint* YAOTypeMap;

        // uint maps 32 x 32
        public uint* TypeMap;
        public uint* ZTypeMap;
        public uint* NonSolidMap;

        // ulong 32
        public ulong* TopBits;
        public ulong* BottomBits;
        
        // uints 32
        public uint* FrontSlice;
        public uint* BackSlice;

        public uint* TopSlice;
        public uint* BottomSlice;

        public uint* RightSlice;
        public uint* MiddleSlice;
        public uint* LeftSlice;

        public uint* RightMask;
        public uint* XSlice;
        public uint* LeftMask;


        // base pointers
        private Allocator<ulong> _ulongMaps34_34;
        private Allocator<uint> _uintMaps34_34;
        private Allocator<uint> _uintMaps32_32;
        private Allocator<ulong> _ulong32;
        private Allocator<uint> _uints32;

        public NewMeshData()
        {
            // maps 34
            _ulongMaps34_34 = new(MAP_34_SIZE, 1);

            BitMap          = _ulongMaps34_34.Next();

            // uint maps 34
            _uintMaps34_34  = new(MAP_34_SIZE, 3);

            YBitMap         = _uintMaps34_34.Next();
            AOTypeMap       = _uintMaps34_34.Next();
            YAOTypeMap      = _uintMaps34_34.Next();

            // maps 32
            _uintMaps32_32  = new(MAP_32_SIZE, 3);
            
            TypeMap         = _uintMaps32_32.Next();
            ZTypeMap        = _uintMaps32_32.Next();
            NonSolidMap     = _uintMaps32_32.Next();

            // ulong 32
            _ulong32        = new(32, 2);
            
            TopBits         = _ulong32.Next();
            BottomBits      = _ulong32.Next();

            // uints 32
            _uints32        = new(32, 10); 

            FrontSlice      = _uints32.Next();
            BackSlice       = _uints32.Next();

            TopSlice        = _uints32.Next();
            BottomSlice     = _uints32.Next();

            RightSlice      = _uints32.Next();
            MiddleSlice     = _uints32.Next();
            LeftSlice       = _uints32.Next();

            RightMask       = _uints32.Next();
            XSlice          = _uints32.Next();
            LeftMask        = _uints32.Next();
        }

        public void Dispose()
        {
            _ulongMaps34_34.Free();
            _uintMaps34_34.Free();
            _uintMaps32_32.Free();
            _ulong32.Free();
            _uints32.Free();    
        }

        private struct Allocator<T>(int size, int count) where T : unmanaged
        {
            private readonly T* _ptr = MemoryHelper.AllocClear<T>(size * count);
            private int i = 0;
            public T* Next() => _ptr + size * i++;
            public readonly void Free() => MemoryHelper.Free(_ptr);
        }
    }
}