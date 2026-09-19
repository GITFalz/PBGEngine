namespace PBG.NewVoxel;

using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using PBG.Core;
using VH = VoxelHelper;

[InternalSystemInit(InitPriority.Data)]
[InternalSystemCleanup]
public unsafe static class VoxelChunkMesher
{
    private static int THREAD_COUNT => VoxelRenderer.RenderingThreads;
    private static MesherData[] _meshData = [];

    public static MeshData[] MeshDatas = [];

    public static MesherData GetMeshData(int workerId) => _meshData[workerId];

    public static void Init()
    {
        _meshData = new MesherData[THREAD_COUNT];
        MeshDatas = new MeshData[THREAD_COUNT];

        for (int i = 0; i < THREAD_COUNT; i++)
        {
            _meshData[i] = new();
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

    public static void GetVector256BitMap2(Block* blocks, ref NeighbourChunks neighbours, int workerId)
    {
        MesherData meshData = _meshData[workerId];

        var xBitMap = meshData.XBitMap;
        var zBitMap = meshData.ZBitMap;

        V256i zOffsets = V256I.Stride(0, 32);

        V256u[] frontSliceData  = meshData.FrontSlice.Slice;
        V256u[] rightSliceData  = meshData.RightSlice.Slice;
        V256u[] topSliceData    = meshData.TopSlice.Slice;
        V256u[] leftSliceData   = meshData.LeftSlice.Slice;
        V256u[] bottomSliceData = meshData.BottomSlice.Slice;
        V256u[] backSliceData   = meshData.BackSlice.Slice;

        int index = 0;
        for (int a = 0; a < 32; a++)
        {
            // the index progress in the regular block array
            int ayIndex = a * 1024;
            int azIndex = a * 32;
            int asi     = a * 4;

            int blockIndex = a * 1024;
            int mapIndex = (a + 1) * 34 + 1;

            V256u[] xSliceData = meshData.XIDSlices[a].Slice;
            V256u[] zSliceData = meshData.ZIDSlices[a].Slice;

            for (int b = 0; b < 32; b++)
            {
                /*
                ulong row = 0;

                uint* ptr = (uint*)blocks + blockIndex;

                row |= VH.GetV256RowX64(ptr, 0) | VH.GetV256RowX64(ptr, 2);

                row <<= 1;

                row |= neighbours.Blocks12[blockIndex + 31].blockData != 0 ? 1UL : 0;
                row |= neighbours.Blocks14[blockIndex].blockData != 0 ? 0x200000000UL : 0;

                bitMap[mapIndex] = row;

                blockIndex += 32;
                mapIndex++;
                */
                int bzIndex = b * 32;
                int bs      = b * 4;


                // X Axis aligned data
                uint* xPtr = (uint*)blocks + bzIndex + ayIndex;
                V256u xBlockRow1 = Avx.LoadVector256(xPtr);
                V256u xBlockRow2 = Avx.LoadVector256(xPtr + 8);
                V256u xBlockRow3 = Avx.LoadVector256(xPtr + 16);
                V256u xBlockRow4 = Avx.LoadVector256(xPtr + 24);

                xSliceData[bs]     = xBlockRow1;
                xSliceData[bs + 1] = xBlockRow2;
                xSliceData[bs + 2] = xBlockRow3;
                xSliceData[bs + 3] = xBlockRow4;

                ulong rowX = VH.GetV256PackedNonAirBits(xBlockRow1, xBlockRow2) | (VH.GetV256PackedNonAirBits(xBlockRow3, xBlockRow4) << 16);

                rowX <<= 1;

                rowX |= neighbours.Blocks12[blockIndex + 31].blockData != 0 ? 1UL : 0;
                rowX |= neighbours.Blocks14[blockIndex].blockData != 0 ? 0x200000000UL : 0;

                xBitMap[mapIndex] = rowX;


                // Z Axis aligned data
                uint* zPtr = (uint*)blocks + b + ayIndex;
                V256u zBlockRow1 = Avx2.GatherVector256(zPtr,       zOffsets, 4);
                V256u zBlockRow2 = Avx2.GatherVector256(zPtr + 256, zOffsets, 4);
                V256u zBlockRow3 = Avx2.GatherVector256(zPtr + 512, zOffsets, 4);
                V256u zBlockRow4 = Avx2.GatherVector256(zPtr + 768, zOffsets, 4);

                zSliceData[bs]     = zBlockRow1;
                zSliceData[bs + 1] = zBlockRow2;
                zSliceData[bs + 2] = zBlockRow3;
                zSliceData[bs + 3] = zBlockRow4;

                ulong rowZ = VH.GetV256PackedNonAirBits(zBlockRow1, zBlockRow2) | (VH.GetV256PackedNonAirBits(zBlockRow3, zBlockRow4) << 16);

                rowZ <<= 1;

                rowZ |= neighbours.Blocks10[ayIndex + b + 992].blockData != 0 ? 1UL : 0;
                rowZ |= neighbours.Blocks16[ayIndex + b].blockData != 0 ? 0x200000000UL : 0;

                zBitMap[mapIndex] = rowZ;
                


                mapIndex++;
            }
            
            mapIndex = (a + 1) * 34;

            // -x axis slice
            {
                uint* ptr = (uint*)neighbours.Blocks12 + 31 + ayIndex;
                V256u row1 = Avx2.GatherVector256(ptr,       zOffsets, 4);
                V256u row2 = Avx2.GatherVector256(ptr + 256, zOffsets, 4);
                V256u row3 = Avx2.GatherVector256(ptr + 512, zOffsets, 4);
                V256u row4 = Avx2.GatherVector256(ptr + 768, zOffsets, 4);

                frontSliceData[asi]     = row1;
                frontSliceData[asi + 1] = row2;
                frontSliceData[asi + 2] = row3;
                frontSliceData[asi + 3] = row4;

                ulong row = VH.GetV256PackedNonAirBits(row1, row2) | (VH.GetV256PackedNonAirBits(row3, row4) << 16);

                row <<= 1;

                /*
                rowX |= neighbours.Blocks12[blockIndex + 31].blockData != 0 ? 1UL : 0;
                rowX |= neighbours.Blocks14[blockIndex].blockData != 0 ? 0x200000000UL : 0;
                */

                zBitMap[mapIndex] = row;
            }


            // +x axis slice
            {
                uint* ptr = (uint*)neighbours.Blocks14 + ayIndex;
                V256u row1 = Avx2.GatherVector256(ptr,       zOffsets, 4);
                V256u row2 = Avx2.GatherVector256(ptr + 256, zOffsets, 4);
                V256u row3 = Avx2.GatherVector256(ptr + 512, zOffsets, 4);
                V256u row4 = Avx2.GatherVector256(ptr + 768, zOffsets, 4);

                backSliceData[asi]     = row1;
                backSliceData[asi + 1] = row2;
                backSliceData[asi + 2] = row3;
                backSliceData[asi + 3] = row4;

                ulong row = VH.GetV256PackedNonAirBits(row1, row2) | (VH.GetV256PackedNonAirBits(row3, row4) << 16);

                row <<= 1;

                /*
                rowX |= neighbours.Blocks12[blockIndex + 31].blockData != 0 ? 1UL : 0;
                rowX |= neighbours.Blocks14[blockIndex].blockData != 0 ? 0x200000000UL : 0;
                */

                zBitMap[33 + mapIndex] = row;
            }


            // -y axis slice
            {
                uint* ptr = (uint*)neighbours.Blocks4 + azIndex + 31744;
                V256u row1 = Avx.LoadVector256(ptr);
                V256u row2 = Avx.LoadVector256(ptr + 8);
                V256u row3 = Avx.LoadVector256(ptr + 16);
                V256u row4 = Avx.LoadVector256(ptr + 24);

                bottomSliceData[asi]     = row1;
                bottomSliceData[asi + 1] = row2;
                bottomSliceData[asi + 2] = row3;
                bottomSliceData[asi + 3] = row4;

                ulong row = VH.GetV256PackedNonAirBits(row1, row2) | (VH.GetV256PackedNonAirBits(row3, row4) << 16);

                row <<= 1;

                /*
                rowX |= neighbours.Blocks12[blockIndex + 31].blockData != 0 ? 1UL : 0;
                rowX |= neighbours.Blocks14[blockIndex].blockData != 0 ? 0x200000000UL : 0;
                */

                xBitMap[1 + a] = row;
            }


            // +y axis slice
            {
                uint* ptr = (uint*)neighbours.Blocks22 + azIndex;
                V256u row1 = Avx.LoadVector256(ptr);
                V256u row2 = Avx.LoadVector256(ptr + 8);
                V256u row3 = Avx.LoadVector256(ptr + 16);
                V256u row4 = Avx.LoadVector256(ptr + 24);

                topSliceData[asi]     = row1;
                topSliceData[asi + 1] = row2;
                topSliceData[asi + 2] = row3;
                topSliceData[asi + 3] = row4;

                ulong row = VH.GetV256PackedNonAirBits(row1, row2) | (VH.GetV256PackedNonAirBits(row3, row4) << 16);

                row <<= 1;

                /*
                rowX |= neighbours.Blocks12[blockIndex + 31].blockData != 0 ? 1UL : 0;
                rowX |= neighbours.Blocks14[blockIndex].blockData != 0 ? 0x200000000UL : 0;
                */

                xBitMap[1123 + a] = row;
            }


            // -z axis slice
            {
                uint* ptr = (uint*)neighbours.Blocks10 + 992 + ayIndex;
                V256u row1 = Avx.LoadVector256(ptr);
                V256u row2 = Avx.LoadVector256(ptr + 8);
                V256u row3 = Avx.LoadVector256(ptr + 16);
                V256u row4 = Avx.LoadVector256(ptr + 24);

                leftSliceData[asi]     = row1;
                leftSliceData[asi + 1] = row2;
                leftSliceData[asi + 2] = row3;
                leftSliceData[asi + 3] = row4;

                ulong row = VH.GetV256PackedNonAirBits(row1, row2) | (VH.GetV256PackedNonAirBits(row3, row4) << 16);

                row <<= 1;

                /*
                rowX |= neighbours.Blocks12[blockIndex + 31].blockData != 0 ? 1UL : 0;
                rowX |= neighbours.Blocks14[blockIndex].blockData != 0 ? 0x200000000UL : 0;
                */

                xBitMap[mapIndex] = row;
            }


            // +z axis slice
            {
                uint* ptr = (uint*)neighbours.Blocks16 + ayIndex;
                V256u row1 = Avx.LoadVector256(ptr);
                V256u row2 = Avx.LoadVector256(ptr + 8);
                V256u row3 = Avx.LoadVector256(ptr + 16);
                V256u row4 = Avx.LoadVector256(ptr + 24);

                rightSliceData[asi]     = row1;
                rightSliceData[asi + 1] = row2;
                rightSliceData[asi + 2] = row3;
                rightSliceData[asi + 3] = row4;

                ulong row = VH.GetV256PackedNonAirBits(row1, row2) | (VH.GetV256PackedNonAirBits(row3, row4) << 16);

                row <<= 1;

                /*
                rowX |= neighbours.Blocks12[blockIndex + 31].blockData != 0 ? 1UL : 0;
                rowX |= neighbours.Blocks14[blockIndex].blockData != 0 ? 0x200000000UL : 0;
                */

                xBitMap[33 + mapIndex] = row;
            }
        }
    }

    public static void GetVector256BitMap3(Block* blocks, ref NeighbourChunks neighbours, int workerId)
    {
        MeshData meshData = MeshDatas[workerId];

        ulong* bitMap = meshData.BitMap;
        uint* typeMap = meshData.TypeMap;
        uint* zTypeMap = meshData.ZTypeMap;

        bitMap[0] = 0;
        bitMap[33] = 0;
        bitMap[1122] = 0;
        bitMap[1155] = 0;

        int maxYlayerIndex = 31744;

        int index = 0;
        for (int y = 0; y < 32; y++)
        {
            // the index progress in the regular block array
            int blockIndex = y * 1024;
            int mapIndex = (y + 1) * 34 + 1;

            for (int z = 0; z < 32; z++)
            {
                uint* ptr = (uint*)blocks + blockIndex;

                // get normal block rows
                V256u row1 = Avx.LoadVector256(ptr);
                V256u row2 = Avx.LoadVector256(ptr + 8);
                V256u row3 = Avx.LoadVector256(ptr + 16);
                V256u row4 = Avx.LoadVector256(ptr + 24);

                // get the rows but shifted by one block
                V256u rowOffset1 = Avx.LoadVector256(ptr - 1);
                V256u rowOffset2 = Avx.LoadVector256(ptr + 7);
                V256u rowOffset3 = Avx.LoadVector256(ptr + 15);
                V256u rowOffset4 = Avx.LoadVector256(ptr + 23);

                // compare the normal rows with the shifted rows
                // if a block and a shifted block are the same, the result will be equal to true else false, now we know when a block changes
                V256u equal1 = Avx2.CompareEqual(row1, rowOffset1);
                V256u equal2 = Avx2.CompareEqual(row2, rowOffset2);
                V256u equal3 = Avx2.CompareEqual(row3, rowOffset3);
                V256u equal4 = Avx2.CompareEqual(row4, rowOffset4);

                uint mask1 = (uint)~Avx2.MoveMask(equal1.AsByte()) & 0x11111111u;
                uint mask2 = (uint)~Avx2.MoveMask(equal2.AsByte()) & 0x11111111u;
                uint mask3 = (uint)~Avx2.MoveMask(equal3.AsByte()) & 0x11111111u;
                uint mask4 = (uint)~Avx2.MoveMask(equal4.AsByte()) & 0x11111111u;

                ulong compact1 = Bmi2.X64.ParallelBitExtract(mask1 | ((ulong)(mask2) << 32), 0x1111111111111111ul);
                ulong compact2 = Bmi2.X64.ParallelBitExtract(mask3 | ((ulong)(mask4) << 32), 0x1111111111111111ul);

                uint typeCheck = (uint)(compact1 | (compact2 << 16)) & 0xFFFFFFFEu;
                
                typeMap[index] = typeCheck;

                index++;

                ulong row = VH.GetV256usNonAirBitRowX64(row1, row2, row3, row4);

                row <<= 1;

                row |= neighbours.Blocks12[blockIndex + 31].blockData != 0 ? 1UL : 0;
                row |= neighbours.Blocks14[blockIndex].blockData != 0 ? 0x200000000UL : 0;

                bitMap[mapIndex] = row;

                blockIndex += 32;
                mapIndex++;
            }

            // the index progress in the regular block array
            int yIndex = y * 1024;
            int zIndexY = y * 32; // simulation of z using y

            

            int indexNz = 992 + yIndex; // represent the block index in the blocks of the chunk next to this one in the direction of -z
            int indexPz = yIndex;

            int indexNy = zIndexY + maxYlayerIndex; // simulate z progression using y at the top y layer
            int indexPy = zIndexY; // simulate z progression using y at the bottom y layer

            uint* ptr10 = (uint*)neighbours.Blocks10 + indexNz;
            uint* ptr16 = (uint*)neighbours.Blocks16 + indexPz;
            uint* ptr4  = (uint*)neighbours.Blocks4  + indexNy;
            uint* ptr22 = (uint*)neighbours.Blocks22 + indexPy;

            // rows in the direction of x, on the plane P (+) / N (-), on the axis z / y
            // handle chunk that is in -z at index 10 and i need the last block
            ulong rowNz = VH.GetV256usNonAirBitRowX64(ptr10);
            
            // handle chunk that is in +z at index 16 and i need the first block
            ulong rowPz = VH.GetV256usNonAirBitRowX64(ptr16);

            // handle chunk that is in -y at index 4 and i need the last block
            ulong rowNy = VH.GetV256usNonAirBitRowX64(ptr4);

            // handle chunk that is in +y at index 22 and i need the first block
            ulong rowPy = VH.GetV256usNonAirBitRowX64(ptr22);

            int rowZindex = (y + 1) * 34;

            bitMap[rowZindex] = rowNz << 1;
            bitMap[33 + rowZindex] = rowPz << 1;

            bitMap[y + 1] = rowNy << 1; // on the z axis of the bit map (y simulates z)
            bitMap[1123 + y] = rowPy << 1; // on the z axis of the bit map but at the last row (y simulates z) (1123 is 33 x 34 = 1122 and +1 like the line before (y + 1))
        }
    }



    public static void GetVector256BitMap4(Block* blocks, ref NeighbourChunks neighbours, int workerId)
    {
        MeshData meshData = MeshDatas[workerId];

        ulong* bitMap = meshData.BitMap;
        ulong* aoTypeMap = meshData.AOTypeMap;

        uint* typeMap = meshData.TypeMap;
        uint* zTypeMap = meshData.ZTypeMap;
        uint* nonSolidMap = meshData.NonSolidMap;

        bitMap[0] = 0;
        bitMap[33] = 0;
        bitMap[1122] = 0;
        bitMap[1155] = 0;

        aoTypeMap[0] = 0;
        aoTypeMap[33] = 0;
        aoTypeMap[1122] = 0;
        aoTypeMap[1155] = 0;

        int maxYlayerIndex = 31744;

        int index = 0;
        for (int y = 0; y < 32; y++)
        {
            // the index progress in the regular block array
            int blockIndex = y * 1024;
            int mapIndex = (y + 1) * 34 + 1;

            V256u mem1 = V256U.Zero;
            V256u mem2 = V256U.Zero;
            V256u mem3 = V256U.Zero;
            V256u mem4 = V256U.Zero;

            V256u zrow1 = V256U.Zero;
            V256u zrow2 = V256U.Zero;
            V256u zrow3 = V256U.Zero;
            V256u zrow4 = V256U.Zero;

            for (int z = 0; z < 32; z++)
            {
                uint* ptr = (uint*)blocks + blockIndex;

                // get normal block rows
                V256u row1 = Avx.LoadVector256(ptr);
                V256u row2 = Avx.LoadVector256(ptr + 8);
                V256u row3 = Avx.LoadVector256(ptr + 16);
                V256u row4 = Avx.LoadVector256(ptr + 24);

                // get the rows but shifted by one block
                V256u rowOffset1 = Avx.LoadVector256(ptr - 1);
                V256u rowOffset2 = Avx.LoadVector256(ptr + 7);
                V256u rowOffset3 = Avx.LoadVector256(ptr + 15);
                V256u rowOffset4 = Avx.LoadVector256(ptr + 23);

                // compare the normal rows with the shifted rows
                // if a block and a shifted block are the same, the result will be equal to true else false, now we know when a block changes
                V256u equal1 = Avx2.CompareEqual(row1, rowOffset1);
                V256u equal2 = Avx2.CompareEqual(row2, rowOffset2);
                V256u equal3 = Avx2.CompareEqual(row3, rowOffset3);
                V256u equal4 = Avx2.CompareEqual(row4, rowOffset4);

                uint mask1 = (uint)~Avx2.MoveMask(equal1.AsByte()) & 0x11111111u;
                uint mask2 = (uint)~Avx2.MoveMask(equal2.AsByte()) & 0x11111111u;
                uint mask3 = (uint)~Avx2.MoveMask(equal3.AsByte()) & 0x11111111u;
                uint mask4 = (uint)~Avx2.MoveMask(equal4.AsByte()) & 0x11111111u;

                ulong compact1 = Bmi2.X64.ParallelBitExtract(mask1 | ((ulong)(mask2) << 32), 0x1111111111111111ul);
                ulong compact2 = Bmi2.X64.ParallelBitExtract(mask3 | ((ulong)(mask4) << 32), 0x1111111111111111ul);

                uint typeCheck = (uint)(compact1 | (compact2 << 16)) & 0xFFFFFFFEu;
                
                typeMap[index] = typeCheck;

                index++;


                // handle the type checking for the z axis
                if (z > 0)
                {   
                    // check if block are unequal
                    V256u zequal1 = Avx2.CompareEqual(row1, mem1);
                    V256u zequal2 = Avx2.CompareEqual(row2, mem2);
                    V256u zequal3 = Avx2.CompareEqual(row3, mem3);
                    V256u zequal4 = Avx2.CompareEqual(row4, mem4);

                    // invert the equality
                    V256u znot1 = Avx2.Xor(zequal1, V256U.MaxValue);
                    V256u znot2 = Avx2.Xor(zequal2, V256U.MaxValue);
                    V256u znot3 = Avx2.Xor(zequal3, V256U.MaxValue);
                    V256u znot4 = Avx2.Xor(zequal4, V256U.MaxValue);

                    // make it so the values are either 0 or 1
                    V256u zbit1 = Avx2.And(znot1, V256U.One);
                    V256u zbit2 = Avx2.And(znot2, V256U.One);
                    V256u zbit3 = Avx2.And(znot3, V256U.One);
                    V256u zbit4 = Avx2.And(znot4, V256U.One);

                    // shift the bits to their place in the map
                    V256u zshift1 = Avx2.ShiftLeftLogical(zbit1, (byte)z);
                    V256u zshift2 = Avx2.ShiftLeftLogical(zbit2, (byte)z);
                    V256u zshift3 = Avx2.ShiftLeftLogical(zbit3, (byte)z);
                    V256u zshift4 = Avx2.ShiftLeftLogical(zbit4, (byte)z);

                    zrow1 = Avx2.Or(zrow1, zshift1);
                    zrow2 = Avx2.Or(zrow2, zshift2);
                    zrow3 = Avx2.Or(zrow3, zshift3);
                    zrow4 = Avx2.Or(zrow4, zshift4);
                }

                mem1 = row1;
                mem2 = row2;
                mem3 = row3;
                mem4 = row4;


                ulong row = VH.GetV256usNonAirBitRowX64(row1, row2, row3, row4);

                row <<= 1;

                row |= neighbours.Blocks12[blockIndex + 31].blockData != 0 ? 1UL : 0;
                row |= neighbours.Blocks14[blockIndex].blockData != 0 ? 0x200000000UL : 0;

                bitMap[mapIndex] = row;
                aoTypeMap[mapIndex] = VH.GetAOTypeMap(row) >> 1;

                blockIndex += 32;
                mapIndex++;
            }

            // the index progress in the regular block array
            int yIndex = y * 1024;
            int zIndexY = y * 32; // simulation of z using y

            Avx.Store(zTypeMap + zIndexY,      zrow1);
            Avx.Store(zTypeMap + zIndexY + 8,  zrow2);
            Avx.Store(zTypeMap + zIndexY + 16, zrow3);
            Avx.Store(zTypeMap + zIndexY + 24, zrow4);
            

            int indexNz = 992 + yIndex; // represent the block index in the blocks of the chunk next to this one in the direction of -z
            int indexPz = yIndex;

            int indexNy = zIndexY + maxYlayerIndex; // simulate z progression using y at the top y layer
            int indexPy = zIndexY; // simulate z progression using y at the bottom y layer

            uint* ptr10 = (uint*)neighbours.Blocks10 + indexNz;
            uint* ptr16 = (uint*)neighbours.Blocks16 + indexPz;
            uint* ptr4  = (uint*)neighbours.Blocks4  + indexNy;
            uint* ptr22 = (uint*)neighbours.Blocks22 + indexPy;

            // rows in the direction of x, on the plane P (+) / N (-), on the axis z / y
            // handle chunk that is in -z at index 10 and i need the last block
            ulong rowNz = VH.GetV256usNonAirBitRowX64(ptr10) << 1;
            
            // handle chunk that is in +z at index 16 and i need the first block
            ulong rowPz = VH.GetV256usNonAirBitRowX64(ptr16) << 1;

            // handle chunk that is in -y at index 4 and i need the last block
            ulong rowNy = VH.GetV256usNonAirBitRowX64(ptr4) << 1;

            // handle chunk that is in +y at index 22 and i need the first block
            ulong rowPy = VH.GetV256usNonAirBitRowX64(ptr22) << 1;

            int rowZindex = (y + 1) * 34;

            bitMap[rowZindex] = rowNz;
            bitMap[33 + rowZindex] = rowPz;

            bitMap[y + 1] = rowNy; // on the z axis of the bit map (y simulates z)
            bitMap[1123 + y] = rowPy; // on the z axis of the bit map but at the last row (y simulates z) (1123 is 33 x 34 = 1122 and +1 like the line before (y + 1))


            aoTypeMap[rowZindex] = VH.GetAOTypeMap(rowNz) >> 1;
            aoTypeMap[33 + rowZindex] = VH.GetAOTypeMap(rowPz) >> 1;

            aoTypeMap[y + 1] = VH.GetAOTypeMap(rowNy) >> 1; // on the z axis of the bit map (y simulates z)
            aoTypeMap[1123 + y] = VH.GetAOTypeMap(rowPy) >> 1; // on the z axis of the bit map but at the last row (y simulates z) (1123 is 33 x 34 = 1122 and +1 like the line before (y + 1))
        }
    }





    public static void GetVector256BitMap4Test(Block* blocks, ref NeighbourChunks neighbours, int workerId)
    {
        MeshData meshData = MeshDatas[workerId];

        ulong* bitMap = meshData.BitMap;
        ulong* aoTypeMap = meshData.AOTypeMap;

        uint* typeMap = meshData.TypeMap;
        uint* zTypeMap = meshData.ZTypeMap;
        uint* nonSolidMap = meshData.NonSolidMap;

        bitMap[0] = 0;
        bitMap[33] = 0;
        bitMap[1122] = 0;
        bitMap[1155] = 0;

        aoTypeMap[0] = 0;
        aoTypeMap[33] = 0;
        aoTypeMap[1122] = 0;
        aoTypeMap[1155] = 0;

        int maxYlayerIndex = 31744;

        int index = 0;
        for (int y = 0; y < 32; y++)
        {
            // the index progress in the regular block array
            int blockIndex = y * 1024;
            int mapIndex = (y + 1) * 34 + 1;

            V256u mem1 = V256U.Zero;
            V256u mem2 = V256U.Zero;
            V256u mem3 = V256U.Zero;
            V256u mem4 = V256U.Zero;

            V256u zrow1 = V256U.Zero;
            V256u zrow2 = V256U.Zero;
            V256u zrow3 = V256U.Zero;
            V256u zrow4 = V256U.Zero;

            {
                ulong row = 0;

                uint* ptr = (uint*)blocks + blockIndex;

                // get normal block rows
                V256u row1 = Avx.LoadVector256(ptr);
                V256u row2 = Avx.LoadVector256(ptr + 8);
                V256u row3 = Avx.LoadVector256(ptr + 16);
                V256u row4 = Avx.LoadVector256(ptr + 24);

                // get the rows but shifted by one block
                V256u rowOffset1 = Avx.LoadVector256(ptr - 1);
                V256u rowOffset2 = Avx.LoadVector256(ptr + 7);
                V256u rowOffset3 = Avx.LoadVector256(ptr + 15);
                V256u rowOffset4 = Avx.LoadVector256(ptr + 23);

                // compare the normal rows with the shifted rows
                // if a block and a shifted block are the same, the result will be equal to true else false, now we know when a block changes
                V256u equal1 = Avx2.CompareEqual(row1, rowOffset1);
                V256u equal2 = Avx2.CompareEqual(row2, rowOffset2);
                V256u equal3 = Avx2.CompareEqual(row3, rowOffset3);
                V256u equal4 = Avx2.CompareEqual(row4, rowOffset4);

                uint mask1 = (uint)~Avx2.MoveMask(equal1.AsByte()) & 0x11111111u;
                uint mask2 = (uint)~Avx2.MoveMask(equal2.AsByte()) & 0x11111111u;
                uint mask3 = (uint)~Avx2.MoveMask(equal3.AsByte()) & 0x11111111u;
                uint mask4 = (uint)~Avx2.MoveMask(equal4.AsByte()) & 0x11111111u;

                ulong compact1 = Bmi2.X64.ParallelBitExtract(mask1 | ((ulong)(mask2) << 32), 0x1111111111111111ul);
                ulong compact2 = Bmi2.X64.ParallelBitExtract(mask3 | ((ulong)(mask4) << 32), 0x1111111111111111ul);

                uint typeCheck = (uint)(compact1 | (compact2 << 16)) & 0xFFFFFFFEu;
                
                typeMap[index] = typeCheck;

                row = VH.GetV256usNonAirBitRowX64(row1, row2, row3, row4) << 1; 

                row |= neighbours.Blocks12[blockIndex + 31].blockData != 0 ? 1UL : 0;
                row |= neighbours.Blocks14[blockIndex].blockData != 0 ? 0x200000000UL : 0;

                bitMap[mapIndex] = row;
                aoTypeMap[mapIndex] = VH.GetAOTypeMap(row) >> 1;

                mem1 = row1;
                mem2 = row2;
                mem3 = row3;
                mem4 = row4;

                index++;

                blockIndex += 32;
                mapIndex++;
            }

            for (int z = 1; z < 32; z++)
            {
                ulong row = 0;

                uint* ptr = (uint*)blocks + blockIndex;

                // get normal block rows
                V256u row1 = Avx.LoadVector256(ptr);
                V256u row2 = Avx.LoadVector256(ptr + 8);
                V256u row3 = Avx.LoadVector256(ptr + 16);
                V256u row4 = Avx.LoadVector256(ptr + 24);

                // get the rows but shifted by one block
                V256u rowOffset1 = Avx.LoadVector256(ptr - 1);
                V256u rowOffset2 = Avx.LoadVector256(ptr + 7);
                V256u rowOffset3 = Avx.LoadVector256(ptr + 15);
                V256u rowOffset4 = Avx.LoadVector256(ptr + 23);

                // compare the normal rows with the shifted rows
                // if a block and a shifted block are the same, the result will be equal to true else false, now we know when a block changes
                V256u equal1 = Avx2.CompareEqual(row1, rowOffset1);
                V256u equal2 = Avx2.CompareEqual(row2, rowOffset2);
                V256u equal3 = Avx2.CompareEqual(row3, rowOffset3);
                V256u equal4 = Avx2.CompareEqual(row4, rowOffset4);



                // check if block are unequal
                V256u zequal1 = Avx2.CompareEqual(row1, mem1);
                V256u zequal2 = Avx2.CompareEqual(row2, mem2);
                V256u zequal3 = Avx2.CompareEqual(row3, mem3);
                V256u zequal4 = Avx2.CompareEqual(row4, mem4);

                // invert the equality
                V256u znot1 = Avx2.Xor(zequal1, V256U.MaxValue);
                V256u znot2 = Avx2.Xor(zequal2, V256U.MaxValue);
                V256u znot3 = Avx2.Xor(zequal3, V256U.MaxValue);
                V256u znot4 = Avx2.Xor(zequal4, V256U.MaxValue);

                // make it so the values are either 0 or 1
                V256u zbit1 = Avx2.And(znot1, V256U.One);
                V256u zbit2 = Avx2.And(znot2, V256U.One);
                V256u zbit3 = Avx2.And(znot3, V256U.One);
                V256u zbit4 = Avx2.And(znot4, V256U.One);

                // shift the bits to their place in the map
                V256u zshift1 = Avx2.ShiftLeftLogical(zbit1, (byte)z);
                V256u zshift2 = Avx2.ShiftLeftLogical(zbit2, (byte)z);
                V256u zshift3 = Avx2.ShiftLeftLogical(zbit3, (byte)z);
                V256u zshift4 = Avx2.ShiftLeftLogical(zbit4, (byte)z);

                zrow1 = Avx2.Or(zrow1, zshift1);
                zrow2 = Avx2.Or(zrow2, zshift2);
                zrow3 = Avx2.Or(zrow3, zshift3);
                zrow4 = Avx2.Or(zrow4, zshift4);



                uint mask1 = (uint)~Avx2.MoveMask(equal1.AsByte()) & 0x11111111u;
                uint mask2 = (uint)~Avx2.MoveMask(equal2.AsByte()) & 0x11111111u;
                uint mask3 = (uint)~Avx2.MoveMask(equal3.AsByte()) & 0x11111111u;
                uint mask4 = (uint)~Avx2.MoveMask(equal4.AsByte()) & 0x11111111u;

                ulong compact1 = Bmi2.X64.ParallelBitExtract(mask1 | ((ulong)(mask2) << 32), 0x1111111111111111ul);
                ulong compact2 = Bmi2.X64.ParallelBitExtract(mask3 | ((ulong)(mask4) << 32), 0x1111111111111111ul);

                uint typeCheck = (uint)(compact1 | (compact2 << 16)) & 0xFFFFFFFEu;
                
                typeMap[index] = typeCheck;

                row |= VH.GetV256usNonAirBitRowX64(row1, row2, row3, row4) << 1;  

                row |= neighbours.Blocks12[blockIndex + 31].blockData != 0 ? 1UL : 0;
                row |= neighbours.Blocks14[blockIndex].blockData != 0 ? 0x200000000UL : 0;

                bitMap[mapIndex] = row;
                aoTypeMap[mapIndex] = VH.GetAOTypeMap(row) >> 1;

                mem1 = row1;
                mem2 = row2;
                mem3 = row3;
                mem4 = row4;

                index++;

                blockIndex += 32;
                mapIndex++;
            }

            // the index progress in the regular block array
            int yIndex = y * 1024;
            int zIndexY = y * 32; // simulation of z using y

            Avx.Store(zTypeMap + zIndexY,      zrow1);
            Avx.Store(zTypeMap + zIndexY + 8,  zrow2);
            Avx.Store(zTypeMap + zIndexY + 16, zrow3);
            Avx.Store(zTypeMap + zIndexY + 24, zrow4);
            

            int indexNz = 992 + yIndex; // represent the block index in the blocks of the chunk next to this one in the direction of -z
            int indexPz = yIndex;

            int indexNy = zIndexY + maxYlayerIndex; // simulate z progression using y at the top y layer
            int indexPy = zIndexY; // simulate z progression using y at the bottom y layer

            uint* ptr10 = (uint*)neighbours.Blocks10 + indexNz;
            uint* ptr16 = (uint*)neighbours.Blocks16 + indexPz;
            uint* ptr4  = (uint*)neighbours.Blocks4  + indexNy;
            uint* ptr22 = (uint*)neighbours.Blocks22 + indexPy;

            // rows in the direction of x, on the plane P (+) / N (-), on the axis z / y
            // handle chunk that is in -z at index 10 and i need the last block
            ulong rowNz = VH.GetV256usNonAirBitRowX64(ptr10) << 1;
            
            // handle chunk that is in +z at index 16 and i need the first block
            ulong rowPz = VH.GetV256usNonAirBitRowX64(ptr16) << 1;

            // handle chunk that is in -y at index 4 and i need the last block
            ulong rowNy = VH.GetV256usNonAirBitRowX64(ptr4) << 1;

            // handle chunk that is in +y at index 22 and i need the first block
            ulong rowPy = VH.GetV256usNonAirBitRowX64(ptr22) << 1;

            int rowZindex = (y + 1) * 34;

            bitMap[rowZindex] = rowNz;
            bitMap[33 + rowZindex] = rowPz;

            bitMap[y + 1] = rowNy; // on the z axis of the bit map (y simulates z)
            bitMap[1123 + y] = rowPy; // on the z axis of the bit map but at the last row (y simulates z) (1123 is 33 x 34 = 1122 and +1 like the line before (y + 1))


            aoTypeMap[rowZindex] = VH.GetAOTypeMap(rowNz) >> 1;
            aoTypeMap[33 + rowZindex] = VH.GetAOTypeMap(rowPz) >> 1;

            aoTypeMap[y + 1] = VH.GetAOTypeMap(rowNy) >> 1; // on the z axis of the bit map (y simulates z)
            aoTypeMap[1123 + y] = VH.GetAOTypeMap(rowPy) >> 1; // on the z axis of the bit map but at the last row (y simulates z) (1123 is 33 x 34 = 1122 and +1 like the line before (y + 1))
        }
    }






    public static void GetVector256BitMap5(Block* blocks, ref NeighbourChunks neighbours, int workerId)
    {
        MeshData meshData = MeshDatas[workerId];

        ulong* bitMap = meshData.BitMap;
        uint* typeMap = meshData.TypeMap;
        uint* zTypeMap = meshData.ZTypeMap;

        bitMap[0] = 0;
        bitMap[33] = 0;
        bitMap[1122] = 0;
        bitMap[1155] = 0;

        int maxYlayerIndex = 31744;

        int index = 0;
        for (int y = 0; y < 32; y++)
        {
            // the index progress in the regular block array
            int blockIndex = y * 1024;
            int mapIndex = (y + 1) * 34 + 1;

            V256u mem1 = V256U.Zero;
            V256u mem2 = V256U.Zero;
            V256u mem3 = V256U.Zero;
            V256u mem4 = V256U.Zero;

            V256u zrow1 = V256U.Zero;
            V256u zrow2 = V256U.Zero;
            V256u zrow3 = V256U.Zero;
            V256u zrow4 = V256U.Zero;

            for (int z = 0; z < 32; z++)
            {
                uint* ptr = (uint*)blocks + blockIndex;

                // get normal block rows
                V256u row1 = Avx.LoadVector256(ptr);
                V256u row2 = Avx.LoadVector256(ptr + 8);
                V256u row3 = Avx.LoadVector256(ptr + 16);
                V256u row4 = Avx.LoadVector256(ptr + 24);

                // get the rows but shifted by one block
                V256u rowOffset1 = Avx.LoadVector256(ptr - 1);
                V256u rowOffset2 = Avx.LoadVector256(ptr + 7);
                V256u rowOffset3 = Avx.LoadVector256(ptr + 15);
                V256u rowOffset4 = Avx.LoadVector256(ptr + 23);

                // compare the normal rows with the shifted rows
                // if a block and a shifted block are the same, the result will be equal to true else false, now we know when a block changes
                V256u equal1 = Avx2.CompareEqual(row1, rowOffset1);
                V256u equal2 = Avx2.CompareEqual(row2, rowOffset2);
                V256u equal3 = Avx2.CompareEqual(row3, rowOffset3);
                V256u equal4 = Avx2.CompareEqual(row4, rowOffset4);

                uint mask1 = (uint)~Avx2.MoveMask(equal1.AsByte()) & 0x11111111u;
                uint mask2 = (uint)~Avx2.MoveMask(equal2.AsByte()) & 0x11111111u;
                uint mask3 = (uint)~Avx2.MoveMask(equal3.AsByte()) & 0x11111111u;
                uint mask4 = (uint)~Avx2.MoveMask(equal4.AsByte()) & 0x11111111u;

                ulong compact1 = Bmi2.X64.ParallelBitExtract(mask1 | ((ulong)(mask2) << 32), 0x1111111111111111ul);
                ulong compact2 = Bmi2.X64.ParallelBitExtract(mask3 | ((ulong)(mask4) << 32), 0x1111111111111111ul);

                uint typeCheck = (uint)(compact1 | (compact2 << 16)) & 0xFFFFFFFEu;
                
                typeMap[index] = typeCheck;

                index++;


                // handle the type checking for the z axis
                if (z > 0)
                {   
                    // check if block are unequal
                    V256u zequal1 = Avx2.CompareEqual(row1, mem1);
                    V256u zequal2 = Avx2.CompareEqual(row2, mem2);
                    V256u zequal3 = Avx2.CompareEqual(row3, mem3);
                    V256u zequal4 = Avx2.CompareEqual(row4, mem4);

                    // invert the equality
                    V256u znot1 = Avx2.Xor(zequal1, V256U.MaxValue);
                    V256u znot2 = Avx2.Xor(zequal2, V256U.MaxValue);
                    V256u znot3 = Avx2.Xor(zequal3, V256U.MaxValue);
                    V256u znot4 = Avx2.Xor(zequal4, V256U.MaxValue);

                    // make it so the values are either 0 or 1
                    V256u zbit1 = Avx2.And(znot1, V256U.One);
                    V256u zbit2 = Avx2.And(znot2, V256U.One);
                    V256u zbit3 = Avx2.And(znot3, V256U.One);
                    V256u zbit4 = Avx2.And(znot4, V256U.One);

                    // shift the bits to their place in the map
                    V256u zshift1 = Avx2.ShiftLeftLogical(zbit1, (byte)z);
                    V256u zshift2 = Avx2.ShiftLeftLogical(zbit2, (byte)z);
                    V256u zshift3 = Avx2.ShiftLeftLogical(zbit3, (byte)z);
                    V256u zshift4 = Avx2.ShiftLeftLogical(zbit4, (byte)z);

                    zrow1 = Avx2.Or(zrow1, zshift1);
                    zrow2 = Avx2.Or(zrow2, zshift2);
                    zrow3 = Avx2.Or(zrow3, zshift3);
                    zrow4 = Avx2.Or(zrow4, zshift4);
                }

                mem1 = row1;
                mem2 = row2;
                mem3 = row3;
                mem4 = row4;


                ulong row = VH.GetV256usNonAirBitRowX64(row1, row2, row3, row4);

                row <<= 1;

                row |= neighbours.Blocks12[blockIndex + 31].blockData != 0 ? 1UL : 0;
                row |= neighbours.Blocks14[blockIndex].blockData != 0 ? 0x200000000UL : 0;

                bitMap[mapIndex] = row;

                blockIndex += 32;
                mapIndex++;
            }

            // the index progress in the regular block array
            int yIndex = y * 1024;
            int zIndexY = y * 32; // simulation of z using y

            Avx.Store(zTypeMap + zIndexY,      zrow1);
            Avx.Store(zTypeMap + zIndexY + 8,  zrow2);
            Avx.Store(zTypeMap + zIndexY + 16, zrow3);
            Avx.Store(zTypeMap + zIndexY + 24, zrow4);
            

            int indexNz = 992 + yIndex; // represent the block index in the blocks of the chunk next to this one in the direction of -z
            int indexPz = yIndex;

            int indexNy = zIndexY + maxYlayerIndex; // simulate z progression using y at the top y layer
            int indexPy = zIndexY; // simulate z progression using y at the bottom y layer

            uint* ptr10 = (uint*)neighbours.Blocks10 + indexNz;
            uint* ptr16 = (uint*)neighbours.Blocks16 + indexPz;
            uint* ptr4  = (uint*)neighbours.Blocks4  + indexNy;
            uint* ptr22 = (uint*)neighbours.Blocks22 + indexPy;

            // rows in the direction of x, on the plane P (+) / N (-), on the axis z / y
            // handle chunk that is in -z at index 10 and i need the last block
            ulong rowNz = VH.GetV256usNonAirBitRowX64(ptr10);
            
            // handle chunk that is in +z at index 16 and i need the first block
            ulong rowPz = VH.GetV256usNonAirBitRowX64(ptr16);

            // handle chunk that is in -y at index 4 and i need the last block
            ulong rowNy = VH.GetV256usNonAirBitRowX64(ptr4);

            // handle chunk that is in +y at index 22 and i need the first block
            ulong rowPy = VH.GetV256usNonAirBitRowX64(ptr22);

            int rowZindex = (y + 1) * 34;

            bitMap[rowZindex] = rowNz << 1;
            bitMap[33 + rowZindex] = rowPz << 1;

            bitMap[y + 1] = rowNy << 1; // on the z axis of the bit map (y simulates z)
            bitMap[1123 + y] = rowPy << 1; // on the z axis of the bit map but at the last row (y simulates z) (1123 is 33 x 34 = 1122 and +1 like the line before (y + 1))
        }
    }
}

public unsafe sealed class MeshData
{
    // maps 34
    public ulong* BitMap;
    public ulong* AOTypeMap;

    // maps 32
    public uint* TypeMap;
    public uint* ZTypeMap;
    public uint* LocalBitMap;
    public uint* NonSolidMap;

    // maps 32 x 34
    public uint* YBitMap;


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


    // ulongs 34
    public ulong* RightMaskLong;
    public ulong* MiddleMaskLong;
    public ulong* LeftMaskLong;

    public ulong* RightAOType;
    public ulong* MiddleAOType;
    public ulong* LeftAOType;


    // base pointers
    private ulong* _maps34;
    private ulong* _ulongs34;

    private uint* _maps32;
    private uint* _uints32;

    private uint* _maps32_34;

    public MeshData()
    {
        const int MAP_34_SIZE = 34 * 34;
        const int MAP_32_SIZE = 32 * 32;
        const int MAP_32_34_SIZE = 32 * 32;

        // maps 34
        _maps34 = MemoryHelper.AllocClear<ulong>(MAP_34_SIZE * 2);

        BitMap          = _maps34 + MAP_34_SIZE * 0;
        AOTypeMap       = _maps34 + MAP_34_SIZE * 1;

        // maps 32
        _maps32 = MemoryHelper.AllocClear<uint>(MAP_32_SIZE * 4);

        TypeMap         = _maps32 + MAP_32_SIZE * 0;
        ZTypeMap        = _maps32 + MAP_32_SIZE * 1;
        LocalBitMap     = _maps32 + MAP_32_SIZE * 2;
        NonSolidMap     = _maps32 + MAP_32_SIZE * 3;

        // maps 32 34
        _maps32_34 = MemoryHelper.AllocClear<uint>(MAP_32_34_SIZE * 1);

        YBitMap         = _maps32_34 + MAP_32_34_SIZE * 0;

        // uints 32
        _uints32 = MemoryHelper.AllocClear<uint>(32 * 10); 

        FrontSlice      = _uints32 + 32 * 0;
        BackSlice       = _uints32 + 32 * 1;

        TopSlice        = _uints32 + 32 * 2;
        BottomSlice     = _uints32 + 32 * 3;

        RightSlice      = _uints32 + 32 * 4;
        MiddleSlice     = _uints32 + 32 * 5;
        LeftSlice       = _uints32 + 32 * 6;

        RightMask       = _uints32 + 32 * 7;
        XSlice          = _uints32 + 32 * 8;
        LeftMask        = _uints32 + 32 * 9;

        // ulongs 34
        _ulongs34 = MemoryHelper.AllocClear<ulong>(34 * 6); 
        
        RightMaskLong   = _ulongs34 + 34 * 0;
        MiddleMaskLong  = _ulongs34 + 34 * 1;
        LeftMaskLong    = _ulongs34 + 34 * 2;

        RightAOType     = _ulongs34 + 34 * 3;
        MiddleAOType    = _ulongs34 + 34 * 4;
        LeftAOType      = _ulongs34 + 34 * 5;
    }

    public void Dispose()
    {
        MemoryHelper.Free(_maps34);
        MemoryHelper.Free(_ulongs34);

        MemoryHelper.Free(_maps32);
        MemoryHelper.Free(_uints32);

        MemoryHelper.Free(_maps32_34);
    }
}

public unsafe sealed class MesherData
{
    public ulong[] XBitMap;
    public ulong[] ZBitMap;

    public IDSliceData[] XIDSlices;
    public IDSliceData[] ZIDSlices;

    public IDSliceData FrontSlice = new();
    public IDSliceData RightSlice = new();
    public IDSliceData TopSlice = new();
    public IDSliceData LeftSlice = new();
    public IDSliceData BottomSlice = new();
    public IDSliceData BackSlice = new();

    public MesherData()
    {
        // 34 because it takes neighbouring chunks into account
        XBitMap = new ulong[34 * 34];
        ZBitMap = new ulong[34 * 34];

        XIDSlices = new IDSliceData[32];
        ZIDSlices = new IDSliceData[32];

        for (int i = 0; i < 32; i++)
        {
            XIDSlices[i] = new();
            ZIDSlices[i] = new();
        }
    }
}   

public struct IDSliceData
{
    public V256u[] Slice;

    public IDSliceData()
    {
        Slice = new V256u[32 * 4];
    }
}