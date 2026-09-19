using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using PBG.MathLibrary;

namespace PBG.NewVoxel;

using VH = VoxelHelper;

public unsafe static class GreedyMesher4
{
    public readonly static V256u StateMaskV256 = V256U.New(Block.STATE_MASK);
    public readonly static V256u SolidMaskV256 = V256U.New(1 << Block.STATE_SHIFT);

    public static void GetVector256BitMap4(Block* blocks, List<Vector4i> vertexData, ref NeighbourChunks neighbours, int workerId)
    {
        MeshData meshData = VoxelChunkMesher.MeshDatas[workerId];

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
                V256u row1 = V256U.Load(ptr);
                V256u row2 = V256U.Load(ptr + 8);
                V256u row3 = V256U.Load(ptr + 16);
                V256u row4 = V256U.Load(ptr + 24);

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


                ulong row = VH.GetV256usNonAirBitRowX64(solidRow1, solidRow2, solidRow3, solidRow4);

                row <<= 1;

                row |= neighbours.Blocks12[blockIndex + 31].blockData != 0 ? 1UL : 0;
                row |= neighbours.Blocks14[blockIndex].blockData != 0 ? 0x200000000UL : 0;

                bitMap[mapIndex] = row;
                aoTypeMap[mapIndex] = VH.GetAOTypeMap(row) >> 1;



                // get the rows but shifted by one block
                V256u rowOffset1 = V256U.Load(ptr - 1);
                V256u rowOffset2 = V256U.Load(ptr + 7);
                V256u rowOffset3 = V256U.Load(ptr + 15);
                V256u rowOffset4 = V256U.Load(ptr + 23);

                // compare the normal rows with the shifted rows
                // if a block and a shifted block are the same, the result will be equal to true else false, now we know when a block changes
                V256u equal1 = solidRow1.CompareEqual(rowOffset1);
                V256u equal2 = solidRow2.CompareEqual(rowOffset2);
                V256u equal3 = solidRow3.CompareEqual(rowOffset3);
                V256u equal4 = solidRow4.CompareEqual(rowOffset4);   

                uint mask1 = (uint)~Avx2.MoveMask(equal1.AsByte()) & 0x11111111u;
                uint mask2 = (uint)~Avx2.MoveMask(equal2.AsByte()) & 0x11111111u;
                uint mask3 = (uint)~Avx2.MoveMask(equal3.AsByte()) & 0x11111111u;
                uint mask4 = (uint)~Avx2.MoveMask(equal4.AsByte()) & 0x11111111u;

                ulong compact1 = Bmi2.X64.ParallelBitExtract(mask1 | ((ulong)(mask2) << 32), 0x1111111111111111ul);
                ulong compact2 = Bmi2.X64.ParallelBitExtract(mask3 | ((ulong)(mask4) << 32), 0x1111111111111111ul);

                uint typeCheck = (uint)(compact1 | (compact2 << 16)) & 0xFFFFFFFEu;
                
                typeMap[index] = typeCheck;


                // handle the type checking for the z axis
                if (z > 0)
                {   
                    // check if block are unequal
                    V256u zequal1 = solidRow1.CompareEqual(mem1);
                    V256u zequal2 = solidRow2.CompareEqual(mem2);
                    V256u zequal3 = solidRow3.CompareEqual(mem3);
                    V256u zequal4 = solidRow4.CompareEqual(mem4);

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
                    V256u zshift1 = zbit1 << (byte)z;
                    V256u zshift2 = zbit2 << (byte)z;
                    V256u zshift3 = zbit3 << (byte)z;
                    V256u zshift4 = zbit4 << (byte)z;

                    zrow1 |= zshift1;
                    zrow2 |= zshift2;
                    zrow3 |= zshift3;
                    zrow4 |= zshift4;
                }

                mem1 = solidRow1;
                mem2 = solidRow2;
                mem3 = solidRow3;
                mem4 = solidRow4;


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
            ulong rowNz = GetSolidRow(ptr10) << 1;
            
            // handle chunk that is in +z at index 16 and i need the first block
            ulong rowPz = GetSolidRow(ptr16) << 1;

            // handle chunk that is in -y at index 4 and i need the last block
            ulong rowNy = GetSolidRow(ptr4) << 1;

            // handle chunk that is in +y at index 22 and i need the first block
            ulong rowPy = GetSolidRow(ptr22) << 1;

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

    private static ulong GetSolidRow(uint* ptr)
    {
        // get normal block rows
        V256u row1 = V256U.Load(ptr);
        V256u row2 = V256U.Load(ptr + 8);
        V256u row3 = V256U.Load(ptr + 16);
        V256u row4 = V256U.Load(ptr + 24);

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

        return VH.GetV256usNonAirBitRowX64(solidRow1, solidRow2, solidRow3, solidRow4);
    }


    
    public static bool GenerateMesh(VoxelChunk chunk, List<Vector4i> vertexData, Vector3i worldPosition, int workerId, out int vertexCount)
    {
        vertexCount = 0;
        int vertCount = 0;

        Block* blocks = chunk.Blocks;

        var renderer = chunk.Renderer;

        NeighbourChunks neighbours = new(renderer, chunk.RelativePosition);

        Stopwatch sw = Stopwatch.StartNew();
        GetVector256BitMap4(blocks, vertexData, ref neighbours, workerId);
        Console.WriteLine(sw.Elapsed.TotalMilliseconds);

        var meshData = VoxelChunkMesher.MeshDatas[workerId];
        
        ulong* bitMap = meshData.BitMap;
        ulong* aoTypeMap = meshData.AOTypeMap;

        uint* typeMap = meshData.TypeMap;
        uint* zTypeMap = meshData.ZTypeMap;
        uint* nonSolidMap = meshData.NonSolidMap;

        uint* localBitMap = meshData.LocalBitMap;

        uint* frontSlice = meshData.FrontSlice;
        uint* backSlice = meshData.BackSlice;

        uint* topSlice = meshData.TopSlice;
        uint* bottomSlice = meshData.BottomSlice;

        uint* rightSlice = meshData.RightSlice;
        uint* middleSlice = meshData.MiddleSlice;
        uint* leftSlice = meshData.LeftSlice;

        ulong* rightMask = meshData.RightMaskLong;
        ulong* middleMask = meshData.MiddleMaskLong;
        ulong* leftMask = meshData.LeftMaskLong;

        ulong* rightType = meshData.RightAOType;
        ulong* middleType = meshData.MiddleAOType;
        ulong* leftType = meshData.LeftAOType;

        int index = 0;

        for (int i = 0; i < 32; i++)
        {  
            int iz = (i + 1) * 34;

            if (i == 0)
            {
                ulong row1 = ExtractBitMap(bitMap,        0);
                ulong row2 = ExtractBitMap(bitMap + 1122, 0);

                ulong row3 = ExtractBitMap(bitMap,        1);
                ulong row4 = ExtractBitMap(bitMap + 1122, 1);

                ulong row5 = ExtractBitMap(bitMap,        2);
                ulong row6 = ExtractBitMap(bitMap + 1122, 2);

                leftMask[0] = row1;
                leftMask[33] = row2;

                leftType[0] = VH.GetAOTypeMap(row1) >> 1;
                leftType[33] = VH.GetAOTypeMap(row2) >> 1;


                middleMask[0] = row3;
                middleMask[33] = row4;

                middleType[0] = VH.GetAOTypeMap(row3) >> 1;
                middleType[33] = VH.GetAOTypeMap(row4) >> 1;


                rightMask[0] = row5;
                rightMask[33] = row6;

                rightType[0] = VH.GetAOTypeMap(row5) >> 1;
                rightType[33] = VH.GetAOTypeMap(row6) >> 1;

                for (int s = 0; s < 32; s++)
                {
                    int sz = (s + 1) * 34;
                    int si = i + 1 + sz;

                    row1 = ExtractBitMap(bitMap + sz, 0);
                    row2 = ExtractBitMap(bitMap + sz, 1);

                    uint sRow2 = (uint)(row2 >> 1);

                    // left
                    leftMask[s+1] = row1;
                    leftType[s+1] = VH.GetAOTypeMap(row1) >> 1;
                    
                    // middle
                    middleMask[s+1] = row2;
                    middleType[s+1] = VH.GetAOTypeMap(row2) >> 1;

                    // slices
                    rightSlice[s] = sRow2;
                    leftSlice[s] = sRow2;   

                    ulong row = ExtractBitMap(bitMap + sz, (byte)(i + 2));

                    rightMask[s+1]  = row;

                    rightType[s+1]  = VH.GetAOTypeMap(row) >> 1;
                    
                    frontSlice[s]   = (uint)(bitMap[si] >> 1);
                    backSlice[s]    = (uint)(bitMap[si] >> 1);

                    topSlice[s]     = (uint)(bitMap[s + 1 + iz] >> 1);
                    bottomSlice[s]  = (uint)(bitMap[s + 1 + iz] >> 1);
                }
            }
            else
            {
                ulong row5 = ExtractBitMap(bitMap,        (byte)(i + 2));
                ulong row6 = ExtractBitMap(bitMap + 1122, (byte)(i + 2));

                rightMask[0] = row5;
                rightMask[33] = row6;

                rightType[0] = VH.GetAOTypeMap(row5) >> 1;
                rightType[33] = VH.GetAOTypeMap(row6) >> 1;

                for (int s = 0; s < 32; s++)
                {
                    int sz = (s + 1) * 34;
                    int si = i + 1 + sz;

                    ulong mask = middleMask[s+1] >> 1;

                    rightSlice[s] = (uint)mask;
                    leftSlice[s] = (uint)mask;

                    ulong row = ExtractBitMap(bitMap + sz, (byte)(i + 2));

                    rightMask[s+1]  = row;

                    rightType[s+1]  = VH.GetAOTypeMap(row) >> 1;
                    
                    frontSlice[s]   = (uint)(bitMap[si] >> 1);
                    backSlice[s]    = (uint)(bitMap[si] >> 1);

                    topSlice[s]     = (uint)(bitMap[s + 1 + iz] >> 1);
                    bottomSlice[s]  = (uint)(bitMap[s + 1 + iz] >> 1);
                }
            }

            // handle non solid blocks
            for (int s = 0; s < 32; s++)
            {
                int y = i;
                int z = s;

                uint nonSolidRow = nonSolidMap[index];

                while (nonSolidRow != 0)
                {
                    var trailingZeros = Bit.TrailingZeros(nonSolidRow);
                    var shifted = nonSolidRow >> trailingZeros;
                    var trailingOnes = Bit.TrailingOnes(shifted);
                    uint mask = (uint)((1UL << trailingOnes) - 1) << trailingZeros;
                    nonSolidRow &= ~mask;
                    
                    int posYz = (y << 5) | (z << 10);
                    int iYz = z * 32 + y * 1024;

                    for (int x = trailingZeros; x < trailingOnes + trailingZeros; x++)
                    {
                        int pos = x | posYz;
                        Block block = blocks[iYz + x];

                        ref var vertexIndices = ref BlockData.VoxelDataIndices[block.GetVariantIndex()];
                        var start = vertexIndices.Start;
                        var end = start + vertexIndices.Count;

                        for (int j = start; j < end; j++)
                        {
                            var geometryIndex = BlockData.VoxelGeometryIndices[j];
                            vertexData.Add(new(geometryIndex, pos, 0, 0));
                        }
                    }
                }

                index++;
            }


            HandleGreedyFrontAndBack( blocks, vertexData, bitMap, aoTypeMap, typeMap, frontSlice, backSlice, i);
            //HandleGreedyFront( blocks, vertexData, bitMap, aoTypeMap, typeMap, frontSlice, i);
            //HandleGreedyBack(  blocks, vertexData, bitMap, aoTypeMap, typeMap, backSlice, i);

            HandleGreedyTopAndBottom(   blocks, vertexData, bitMap, aoTypeMap, typeMap, topSlice, bottomSlice, i);
            //HandleGreedyTop(   blocks, vertexData, bitMap, aoTypeMap, typeMap, topSlice, i);
            //HandleGreedyBottom(blocks, vertexData, bitMap, aoTypeMap, typeMap, bottomSlice, i);

            HandleGreedyRight(blocks, vertexData, rightMask, rightType, zTypeMap, rightSlice, i);
            HandleGreedyLeft(  blocks, vertexData, leftMask, leftType, zTypeMap, leftSlice, i);
            
            /*
            for (int s = 0; s < 32; s++)
            {
                HandleGreedyFront( blocks, vertexData, bitMap, aoTypeMap, typeMap, frontSlice, i, s);
                HandleGreedyBack(  blocks, vertexData, bitMap, aoTypeMap, typeMap, backSlice, i, s);

                HandleGreedyTop(   blocks, vertexData, bitMap, aoTypeMap, typeMap, topSlice, i, s);
                HandleGreedyBottom(blocks, vertexData, bitMap, aoTypeMap, typeMap, bottomSlice, i, s);

                HandleGreedyRight(blocks, vertexData, rightMask, rightType, zTypeMap, rightSlice, i, s);
                HandleGreedyLeft(  blocks, vertexData, leftMask, leftType, zTypeMap, leftSlice, i, s);
            }
            */

            ulong* temp = leftMask;
            leftMask = middleMask;
            middleMask = rightMask;
            rightMask = temp;

            ulong* tempType = leftType;
            leftType = middleType;
            middleType = rightType;
            rightType = tempType;
        }

        vertexCount = vertCount;

        /*
        if (worldPosition == (0, 0, 0))
        {
            string b = "";
            for (int i = 0; i < 34 * 34; i++)
            {
                for (int j = 0; j < 34; j++)
                {
                    b += ((bitMap[i] >> j) & 1) == 1 ? '1' : '0';
                }
            }
            File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "bits.txt"), b);
        }

        /*
        if (worldPosition == (0, 0, 0))
        {
            string b = "";
            for (int i = 0; i < 32 * 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    b += ((typeMap[i] >> j) & 1) == 1 ? '1' : '0';
                }
            }
            File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "types1.txt"), b);
        }

        if (worldPosition == (0, 0, 0))
        {
            string b = "";
            for (int i = 0; i < 32 * 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    b += ((zTypeMap[i] >> j) & 1) == 1 ? '1' : '0';
                }
            }
            File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "types2.txt"), b);
        }
        */

        return true;
    }


    public static void HandleGreedyFront(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int z)
    {
        for (int i = 0; i < 32; i++)
        {
            uint aoType1 = (uint)aoTypeMap[z + (i + 0) * 34];
            uint aoType2 = (uint)aoTypeMap[z + (i + 1) * 34];
            uint aoType3 = (uint)aoTypeMap[z + (i + 2) * 34];

            ulong front1 = bitMap[z + (i + 0) * 34];
            ulong front2 = bitMap[z + (i + 1) * 34];
            ulong front3 = bitMap[z + (i + 2) * 34];

            uint type = typeMap[z + i * 32] | aoType1 | aoType2 | aoType3;
            uint row = data[i] & (uint)((~front2) >> 1);

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

                Block block = blocks[trailingZeros + z * 32 + i * 1024];
                if (block.BlockId() == 0)
                    continue;

                int ao = VH.GetAO(front1, front2, front3, trailingZeros);

                while (w < 32 - i)
                {
                    int iw = i + w;

                    uint sAoType1 = (uint)aoTypeMap[z + (i + 0 + w) * 34];
                    uint sAoType2 = (uint)aoTypeMap[z + (i + 1 + w) * 34];
                    uint sAoType3 = (uint)aoTypeMap[z + (i + 2 + w) * 34];

                    ulong sFront1 = bitMap[z + (i + 0 + w) * 34];
                    ulong sFront2 = bitMap[z + (i + 1 + w) * 34];
                    ulong sFront3 = bitMap[z + (i + 2 + w) * 34];

                    uint sType = typeMap[z + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                    uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    int sAo = VH.GetAO(sFront1, sFront2, sFront3, trailingZeros);
                    Block sBlock = blocks[trailingZeros + z * 32 + iw * 1024];

                    if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                int packedAo = VH.GetPackedAO(ao);

                var geometryIndex = block.GetSolidGeometryIndex(0);
                vertexData.Add(new(geometryIndex, trailingZeros | (i << 5) | (z << 10), packedAo, h | (w << 5)));
            }
        }
    }

    public static void HandleGreedyBack(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int z)
    {
        for (int i = 0; i < 32; i++)
        {
            uint aoType1 = (uint)aoTypeMap[z + 2 + (i + 0) * 34];
            uint aoType2 = (uint)aoTypeMap[z + 2 + (i + 1) * 34];
            uint aoType3 = (uint)aoTypeMap[z + 2 + (i + 2) * 34];

            ulong front1 = bitMap[z + 2 + (i + 0) * 34];
            ulong front2 = bitMap[z + 2 + (i + 1) * 34];
            ulong front3 = bitMap[z + 2 + (i + 2) * 34];

            uint type = typeMap[z + i * 32] | aoType1 | aoType2 | aoType3;
            uint row = data[i] & (uint)((~front2) >> 1);

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

                Block block = blocks[trailingZeros + z * 32 + i * 1024];
                if (block.BlockId() == 0)
                    continue;

                int ao = VH.GetFlippedAO(front1, front2, front3, trailingZeros);
                
                while (w < 32 - i)
                {
                    int iw = i + w;

                    uint sAoType1 = (uint)aoTypeMap[z + 2 + (i + 0 + w) * 34];
                    uint sAoType2 = (uint)aoTypeMap[z + 2 + (i + 1 + w) * 34];
                    uint sAoType3 = (uint)aoTypeMap[z + 2 + (i + 2 + w) * 34];

                    ulong sFront1 = bitMap[z + 2 + (i + 0 + w) * 34];
                    ulong sFront2 = bitMap[z + 2 + (i + 1 + w) * 34];
                    ulong sFront3 = bitMap[z + 2 + (i + 2 + w) * 34];

                    uint sType = typeMap[z + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                    uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    int sAo = VH.GetFlippedAO(sFront1, sFront2, sFront3, trailingZeros);
                    Block sBlock = blocks[trailingZeros + z * 32 + iw * 1024];

                    if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                int packedAo = VH.GetPackedAO(ao);

                var geometryIndex = block.GetSolidGeometryIndex(5);
                vertexData.Add(new(geometryIndex, trailingZeros | (i << 5) | (z << 10), packedAo, h | (w << 5)));
            }
        }
    }



    public static void HandleGreedyRight(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int x)
    {
        for (int i = 0; i < 32; i++)
        {
            uint aoType1 = (uint)aoTypeMap[i];
            uint aoType2 = (uint)aoTypeMap[i + 1];
            uint aoType3 = (uint)aoTypeMap[i + 2];

            ulong front1 = bitMap[i];
            ulong front2 = bitMap[i + 1];
            ulong front3 = bitMap[i + 2];
            
            uint type = typeMap[x + i * 32] | aoType1 | aoType2 | aoType3;
            uint row = data[i] & (uint)((~front2) >> 1);

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

                Block block = blocks[x + trailingZeros * 32 + i * 1024];
                if (block.BlockId() == 0)
                    continue;

                int ao = VH.GetAO(front1, front2, front3, trailingZeros);

                while (w < 32 - i)
                {
                    int iw = i + w;

                    uint sAoType1 = (uint)aoTypeMap[iw];
                    uint sAoType2 = (uint)aoTypeMap[iw + 1];
                    uint sAoType3 = (uint)aoTypeMap[iw + 2];

                    ulong sFront1 = bitMap[iw];
                    ulong sFront2 = bitMap[iw + 1];
                    ulong sFront3 = bitMap[iw + 2];

                    uint sType = typeMap[x + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                    uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    int sAo = VH.GetAO(sFront1, sFront2, sFront3, trailingZeros);
                    Block sBlock = blocks[x + trailingZeros * 32 + iw * 1024];

                    if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                int packedAo = VH.GetPackedAO(ao);

                var geometryIndex = block.GetSolidGeometryIndex(1);
                vertexData.Add(new(geometryIndex, x | (i << 5) | (trailingZeros << 10), packedAo, (w << 5) | (h << 10)));
            }
        }
    }

    public static void HandleGreedyLeft(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int x)
    {
        for (int i = 0; i < 32; i++)
        {
            uint aoType1 = (uint)aoTypeMap[i];
            uint aoType2 = (uint)aoTypeMap[i + 1];
            uint aoType3 = (uint)aoTypeMap[i + 2];

            ulong front1 = bitMap[i];
            ulong front2 = bitMap[i + 1];
            ulong front3 = bitMap[i + 2];
            
            uint type = typeMap[x + i * 32] | aoType1 | aoType2 | aoType3;
            uint row = data[i] & (uint)((~front2) >> 1);

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

                Block block = blocks[x + trailingZeros * 32 + i * 1024];
                if (block.BlockId() == 0)
                    continue;

                int ao = VH.GetFlippedAO(front1, front2, front3, trailingZeros);
                
                while (w < 32 - i)
                {
                    int iw = i + w;

                    uint sAoType1 = (uint)aoTypeMap[iw];
                    uint sAoType2 = (uint)aoTypeMap[iw + 1];
                    uint sAoType3 = (uint)aoTypeMap[iw + 2];

                    ulong sFront1 = bitMap[iw];
                    ulong sFront2 = bitMap[iw + 1];
                    ulong sFront3 = bitMap[iw + 2];

                    uint sType = typeMap[x + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                    uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    int sAo = VH.GetFlippedAO(sFront1, sFront2, sFront3, trailingZeros);
                    Block sBlock = blocks[x + trailingZeros * 32 + iw * 1024];

                    if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                int packedAo = VH.GetPackedAO(ao);

                var geometryIndex = block.GetSolidGeometryIndex(3);
                vertexData.Add(new(geometryIndex, x | (i << 5) | (trailingZeros << 10), packedAo, (w << 5) | (h << 10)));
            }
        }
    }

    public static void HandleGreedyTop(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int y)
    {
        for (int i = 0; i < 32; i++)
        {
            uint aoType1 = (uint)aoTypeMap[i + 0 + (y + 2) * 34];
            uint aoType2 = (uint)aoTypeMap[i + 1 + (y + 2) * 34];
            uint aoType3 = (uint)aoTypeMap[i + 2 + (y + 2) * 34];

            ulong front1 = bitMap[i + 0 + (y + 2) * 34];
            ulong front2 = bitMap[i + 1 + (y + 2) * 34];
            ulong front3 = bitMap[i + 2 + (y + 2) * 34];

            uint type = typeMap[i + y * 32] | aoType1 | aoType2 | aoType3;
            uint row = data[i] & (uint)((~front2) >> 1);

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

                Block block = blocks[trailingZeros + i * 32 + y * 1024];
                if (block.BlockId() == 0)
                    continue;
                                
                int ao = VH.GetAO(front1, front2, front3, trailingZeros);

                while (w < 32 - i)
                {
                    int iw = i + w;

                    uint sAoType1 = (uint)aoTypeMap[iw + 0 + (y + 2) * 34];
                    uint sAoType2 = (uint)aoTypeMap[iw + 1 + (y + 2) * 34];
                    uint sAoType3 = (uint)aoTypeMap[iw + 2 + (y + 2) * 34];

                    ulong sFront1 = bitMap[iw + 0 + (y + 2) * 34];
                    ulong sFront2 = bitMap[iw + 1 + (y + 2) * 34];
                    ulong sFront3 = bitMap[iw + 2 + (y + 2) * 34];

                    uint sType = typeMap[iw + y * 32] | sAoType1 | sAoType2 | sAoType3;
                    uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    int sAo = VH.GetAO(sFront1, sFront2, sFront3, trailingZeros);
                    Block sBlock = blocks[trailingZeros + iw * 32 + y * 1024];

                    if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                int packedAo = VH.GetPackedAO(ao);

                var geometryIndex = block.GetSolidGeometryIndex(2);
                vertexData.Add(new(geometryIndex, trailingZeros | (y << 5) | (i << 10), packedAo, h | (w << 10)));
            }
        }
    }

    public static void HandleGreedyBottom(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int y)
    {
        for (int i = 0; i < 32; i++)
        {
            uint aoType1 = (uint)aoTypeMap[i + 0 + y * 34];
            uint aoType2 = (uint)aoTypeMap[i + 1 + y * 34];
            uint aoType3 = (uint)aoTypeMap[i + 2 + y * 34];

            ulong front1 = bitMap[i + 0 + y * 34];
            ulong front2 = bitMap[i + 1 + y * 34];
            ulong front3 = bitMap[i + 2 + y * 34];

            uint type = typeMap[i + y * 32] | aoType1 | aoType2 | aoType3;
            uint row = data[i] & (uint)((~front2) >> 1);

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

                Block block = blocks[trailingZeros + i * 32 + y * 1024];
                if (block.BlockId() == 0)
                    continue;
                
                int ao = VH.GetFlippedAO(front1, front2, front3, trailingZeros);

                while (w < 32 - i)
                {
                    int iw = i + w;

                    uint sAoType1 = (uint)aoTypeMap[iw + 0 + y * 34];
                    uint sAoType2 = (uint)aoTypeMap[iw + 1 + y * 34];
                    uint sAoType3 = (uint)aoTypeMap[iw + 2 + y * 34];

                    ulong sFront1 = bitMap[iw + 0 + y * 34];
                    ulong sFront2 = bitMap[iw + 1 + y * 34];
                    ulong sFront3 = bitMap[iw + 2 + y * 34];

                    uint sType = typeMap[iw + y * 32] | sAoType1 | sAoType2 | sAoType3;
                    uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    int sAo = VH.GetFlippedAO(sFront1, sFront2, sFront3, trailingZeros);
                    Block sBlock = blocks[trailingZeros + iw * 32 + y * 1024];

                    if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                int packedAo = VH.GetPackedAO(ao);

                var geometryIndex = block.GetSolidGeometryIndex(4);
                vertexData.Add(new(geometryIndex, trailingZeros | (y << 5) | (i << 10), packedAo, h | (w << 10)));
            }
        }
    }



    public static void HandleGreedyFront(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int z, int i)
    {
        uint aoType1 = (uint)aoTypeMap[z + (i + 0) * 34];
        uint aoType2 = (uint)aoTypeMap[z + (i + 1) * 34];
        uint aoType3 = (uint)aoTypeMap[z + (i + 2) * 34];

        ulong front1 = bitMap[z + (i + 0) * 34];
        ulong front2 = bitMap[z + (i + 1) * 34];
        ulong front3 = bitMap[z + (i + 2) * 34];

        uint type = typeMap[z + i * 32] | aoType1 | aoType2 | aoType3;
        uint row = data[i] & (uint)((~front2) >> 1);

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

            Block block = blocks[trailingZeros + z * 32 + i * 1024];
            if (block.BlockId() == 0)
                continue;

            int ao = VH.GetAO(front1, front2, front3, trailingZeros);

            while (w < 32 - i)
            {
                int iw = i + w;

                uint sAoType1 = (uint)aoTypeMap[z + (i + 0 + w) * 34];
                uint sAoType2 = (uint)aoTypeMap[z + (i + 1 + w) * 34];
                uint sAoType3 = (uint)aoTypeMap[z + (i + 2 + w) * 34];

                ulong sFront1 = bitMap[z + (i + 0 + w) * 34];
                ulong sFront2 = bitMap[z + (i + 1 + w) * 34];
                ulong sFront3 = bitMap[z + (i + 2 + w) * 34];

                uint sType = typeMap[z + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                sRow &= ~(sType & Bit.Invert(trailingZeros));

                int sAo = VH.GetAO(sFront1, sFront2, sFront3, trailingZeros);
                Block sBlock = blocks[trailingZeros + z * 32 + iw * 1024];

                if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                    break;

                data[iw] &= ~mask;
                w++;
            }

            w--;
            h--;

            int packedAo = VH.GetPackedAO(ao);

            var geometryIndex = block.GetSolidGeometryIndex(0);
            vertexData.Add(new(geometryIndex, trailingZeros | (i << 5) | (z << 10), packedAo, h | (w << 5)));
        }
    }

    public static void HandleGreedyBack(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int z, int i)
    {
        uint aoType1 = (uint)aoTypeMap[z + 2 + (i + 0) * 34];
        uint aoType2 = (uint)aoTypeMap[z + 2 + (i + 1) * 34];
        uint aoType3 = (uint)aoTypeMap[z + 2 + (i + 2) * 34];

        ulong front1 = bitMap[z + 2 + (i + 0) * 34];
        ulong front2 = bitMap[z + 2 + (i + 1) * 34];
        ulong front3 = bitMap[z + 2 + (i + 2) * 34];

        uint type = typeMap[z + i * 32] | aoType1 | aoType2 | aoType3;
        uint row = data[i] & (uint)((~front2) >> 1);

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

            Block block = blocks[trailingZeros + z * 32 + i * 1024];
            if (block.BlockId() == 0)
                continue;

            int ao = VH.GetFlippedAO(front1, front2, front3, trailingZeros);
            
            while (w < 32 - i)
            {
                int iw = i + w;

                uint sAoType1 = (uint)aoTypeMap[z + 2 + (i + 0 + w) * 34];
                uint sAoType2 = (uint)aoTypeMap[z + 2 + (i + 1 + w) * 34];
                uint sAoType3 = (uint)aoTypeMap[z + 2 + (i + 2 + w) * 34];

                ulong sFront1 = bitMap[z + 2 + (i + 0 + w) * 34];
                ulong sFront2 = bitMap[z + 2 + (i + 1 + w) * 34];
                ulong sFront3 = bitMap[z + 2 + (i + 2 + w) * 34];

                uint sType = typeMap[z + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                sRow &= ~(sType & Bit.Invert(trailingZeros));

                int sAo = VH.GetFlippedAO(sFront1, sFront2, sFront3, trailingZeros);
                Block sBlock = blocks[trailingZeros + z * 32 + iw * 1024];

                if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                    break;

                data[iw] &= ~mask;
                w++;
            }

            w--;
            h--;

            int packedAo = VH.GetPackedAO(ao);

            var geometryIndex = block.GetSolidGeometryIndex(5);
            vertexData.Add(new(geometryIndex, trailingZeros | (i << 5) | (z << 10), packedAo, h | (w << 5)));
        }
    }



    public static void HandleGreedyRight(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int x, int i)
    {
        uint aoType1 = (uint)aoTypeMap[i];
        uint aoType2 = (uint)aoTypeMap[i + 1];
        uint aoType3 = (uint)aoTypeMap[i + 2];

        ulong front1 = bitMap[i];
        ulong front2 = bitMap[i + 1];
        ulong front3 = bitMap[i + 2];
        
        uint type = typeMap[x + i * 32] | aoType1 | aoType2 | aoType3;
        uint row = data[i] & (uint)((~front2) >> 1);

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

            Block block = blocks[x + trailingZeros * 32 + i * 1024];
            if (block.BlockId() == 0)
                continue;

            int ao = VH.GetAO(front1, front2, front3, trailingZeros);

            while (w < 32 - i)
            {
                int iw = i + w;

                uint sAoType1 = (uint)aoTypeMap[iw];
                uint sAoType2 = (uint)aoTypeMap[iw + 1];
                uint sAoType3 = (uint)aoTypeMap[iw + 2];

                ulong sFront1 = bitMap[iw];
                ulong sFront2 = bitMap[iw + 1];
                ulong sFront3 = bitMap[iw + 2];

                uint sType = typeMap[x + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                sRow &= ~(sType & Bit.Invert(trailingZeros));

                int sAo = VH.GetAO(sFront1, sFront2, sFront3, trailingZeros);
                Block sBlock = blocks[x + trailingZeros * 32 + iw * 1024];

                if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                    break;

                data[iw] &= ~mask;
                w++;
            }

            w--;
            h--;

            int packedAo = VH.GetPackedAO(ao);

            var geometryIndex = block.GetSolidGeometryIndex(1);
            vertexData.Add(new(geometryIndex, x | (i << 5) | (trailingZeros << 10), packedAo, (w << 5) | (h << 10)));
        }
    }

    public static void HandleGreedyLeft(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int x, int i)
    {
        uint aoType1 = (uint)aoTypeMap[i];
        uint aoType2 = (uint)aoTypeMap[i + 1];
        uint aoType3 = (uint)aoTypeMap[i + 2];

        ulong front1 = bitMap[i];
        ulong front2 = bitMap[i + 1];
        ulong front3 = bitMap[i + 2];
        
        uint type = typeMap[x + i * 32] | aoType1 | aoType2 | aoType3;
        uint row = data[i] & (uint)((~front2) >> 1);

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

            Block block = blocks[x + trailingZeros * 32 + i * 1024];
            if (block.BlockId() == 0)
                continue;

            int ao = VH.GetFlippedAO(front1, front2, front3, trailingZeros);
            
            while (w < 32 - i)
            {
                int iw = i + w;

                uint sAoType1 = (uint)aoTypeMap[iw];
                uint sAoType2 = (uint)aoTypeMap[iw + 1];
                uint sAoType3 = (uint)aoTypeMap[iw + 2];

                ulong sFront1 = bitMap[iw];
                ulong sFront2 = bitMap[iw + 1];
                ulong sFront3 = bitMap[iw + 2];

                uint sType = typeMap[x + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                sRow &= ~(sType & Bit.Invert(trailingZeros));

                int sAo = VH.GetFlippedAO(sFront1, sFront2, sFront3, trailingZeros);
                Block sBlock = blocks[x + trailingZeros * 32 + iw * 1024];

                if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                    break;

                data[iw] &= ~mask;
                w++;
            }

            w--;
            h--;

            int packedAo = VH.GetPackedAO(ao);

            var geometryIndex = block.GetSolidGeometryIndex(3);
            vertexData.Add(new(geometryIndex, x | (i << 5) | (trailingZeros << 10), packedAo, (w << 5) | (h << 10)));
        }
    }

    public static void HandleGreedyTop(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int y, int i)
    {
        uint aoType1 = (uint)aoTypeMap[i + 0 + (y + 2) * 34];
        uint aoType2 = (uint)aoTypeMap[i + 1 + (y + 2) * 34];
        uint aoType3 = (uint)aoTypeMap[i + 2 + (y + 2) * 34];

        ulong front1 = bitMap[i + 0 + (y + 2) * 34];
        ulong front2 = bitMap[i + 1 + (y + 2) * 34];
        ulong front3 = bitMap[i + 2 + (y + 2) * 34];

        uint type = typeMap[i + y * 32] | aoType1 | aoType2 | aoType3;
        uint row = data[i] & (uint)((~front2) >> 1);

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

            Block block = blocks[trailingZeros + i * 32 + y * 1024];
            if (block.BlockId() == 0)
                continue;
                            
            int ao = VH.GetAO(front1, front2, front3, trailingZeros);

            while (w < 32 - i)
            {
                int iw = i + w;

                uint sAoType1 = (uint)aoTypeMap[iw + 0 + (y + 2) * 34];
                uint sAoType2 = (uint)aoTypeMap[iw + 1 + (y + 2) * 34];
                uint sAoType3 = (uint)aoTypeMap[iw + 2 + (y + 2) * 34];

                ulong sFront1 = bitMap[iw + 0 + (y + 2) * 34];
                ulong sFront2 = bitMap[iw + 1 + (y + 2) * 34];
                ulong sFront3 = bitMap[iw + 2 + (y + 2) * 34];

                uint sType = typeMap[iw + y * 32] | sAoType1 | sAoType2 | sAoType3;
                uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                sRow &= ~(sType & Bit.Invert(trailingZeros));

                int sAo = VH.GetAO(sFront1, sFront2, sFront3, trailingZeros);
                Block sBlock = blocks[trailingZeros + iw * 32 + y * 1024];

                if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                    break;

                data[iw] &= ~mask;
                w++;
            }

            w--;
            h--;

            int packedAo = VH.GetPackedAO(ao);

            var geometryIndex = block.GetSolidGeometryIndex(2);
            vertexData.Add(new(geometryIndex, trailingZeros | (y << 5) | (i << 10), packedAo, h | (w << 10)));
        }
    }

    public static void HandleGreedyBottom(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int y, int i)
    {
        uint aoType1 = (uint)aoTypeMap[i + 0 + y * 34];
        uint aoType2 = (uint)aoTypeMap[i + 1 + y * 34];
        uint aoType3 = (uint)aoTypeMap[i + 2 + y * 34];

        ulong front1 = bitMap[i + 0 + y * 34];
        ulong front2 = bitMap[i + 1 + y * 34];
        ulong front3 = bitMap[i + 2 + y * 34];

        uint type = typeMap[i + y * 32] | aoType1 | aoType2 | aoType3;
        uint row = data[i] & (uint)((~front2) >> 1);

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

            Block block = blocks[trailingZeros + i * 32 + y * 1024];
            if (block.BlockId() == 0)
                continue;
            
            int ao = VH.GetFlippedAO(front1, front2, front3, trailingZeros);

            while (w < 32 - i)
            {
                int iw = i + w;

                uint sAoType1 = (uint)aoTypeMap[iw + 0 + y * 34];
                uint sAoType2 = (uint)aoTypeMap[iw + 1 + y * 34];
                uint sAoType3 = (uint)aoTypeMap[iw + 2 + y * 34];

                ulong sFront1 = bitMap[iw + 0 + y * 34];
                ulong sFront2 = bitMap[iw + 1 + y * 34];
                ulong sFront3 = bitMap[iw + 2 + y * 34];

                uint sType = typeMap[iw + y * 32] | sAoType1 | sAoType2 | sAoType3;
                uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                sRow &= ~(sType & Bit.Invert(trailingZeros));

                int sAo = VH.GetFlippedAO(sFront1, sFront2, sFront3, trailingZeros);
                Block sBlock = blocks[trailingZeros + iw * 32 + y * 1024];

                if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                    break;

                data[iw] &= ~mask;
                w++;
            }

            w--;
            h--;

            int packedAo = VH.GetPackedAO(ao);

            var geometryIndex = block.GetSolidGeometryIndex(4);
            vertexData.Add(new(geometryIndex, trailingZeros | (y << 5) | (i << 10), packedAo, h | (w << 10)));
        }
    }





    public static void HandleGreedyFrontAndBack(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* frontData, uint* backData, int z)
    {
        for (int i = 0; i < 32; i++)
        {
            // Front
            {
                uint aoType1 = (uint)aoTypeMap[z + (i + 0) * 34];
                uint aoType2 = (uint)aoTypeMap[z + (i + 1) * 34];
                uint aoType3 = (uint)aoTypeMap[z + (i + 2) * 34];

                ulong front1 = bitMap[z + (i + 0) * 34];
                ulong front2 = bitMap[z + (i + 1) * 34];
                ulong front3 = bitMap[z + (i + 2) * 34];

                uint type = typeMap[z + i * 32] | aoType1 | aoType2 | aoType3;
                uint row = frontData[i] & (uint)((~front2) >> 1);

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

                    Block block = blocks[trailingZeros + z * 32 + i * 1024];
                    if (block.BlockId() == 0)
                        continue;

                    int ao = VH.GetAO(front1, front2, front3, trailingZeros);

                    while (w < 32 - i)
                    {
                        int iw = i + w;

                        uint sAoType1 = (uint)aoTypeMap[z + (i + 0 + w) * 34];
                        uint sAoType2 = (uint)aoTypeMap[z + (i + 1 + w) * 34];
                        uint sAoType3 = (uint)aoTypeMap[z + (i + 2 + w) * 34];

                        ulong sFront1 = bitMap[z + (i + 0 + w) * 34];
                        ulong sFront2 = bitMap[z + (i + 1 + w) * 34];
                        ulong sFront3 = bitMap[z + (i + 2 + w) * 34];

                        uint sType = typeMap[z + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                        uint sRow = frontData[iw] & (uint)((~sFront2) >> 1);

                        sRow &= ~(sType & Bit.Invert(trailingZeros));

                        int sAo = VH.GetAO(sFront1, sFront2, sFront3, trailingZeros);
                        Block sBlock = blocks[trailingZeros + z * 32 + iw * 1024];

                        if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                            break;

                        frontData[iw] &= ~mask;
                        w++;
                    }

                    w--;
                    h--;

                    int packedAo = VH.GetPackedAO(ao);

                    var geometryIndex = block.GetSolidGeometryIndex(0);
                    vertexData.Add(new(geometryIndex, trailingZeros | (i << 5) | (z << 10), packedAo, h | (w << 5)));
                }
            }

            // Back
            {
                uint aoType1 = (uint)aoTypeMap[z + 2 + (i + 0) * 34];
                uint aoType2 = (uint)aoTypeMap[z + 2 + (i + 1) * 34];
                uint aoType3 = (uint)aoTypeMap[z + 2 + (i + 2) * 34];

                ulong front1 = bitMap[z + 2 + (i + 0) * 34];
                ulong front2 = bitMap[z + 2 + (i + 1) * 34];
                ulong front3 = bitMap[z + 2 + (i + 2) * 34];

                uint type = typeMap[z + i * 32] | aoType1 | aoType2 | aoType3;
                uint row = backData[i] & (uint)((~front2) >> 1);

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

                    Block block = blocks[trailingZeros + z * 32 + i * 1024];
                    if (block.BlockId() == 0)
                        continue;

                    int ao = VH.GetFlippedAO(front1, front2, front3, trailingZeros);
                    
                    while (w < 32 - i)
                    {
                        int iw = i + w;

                        uint sAoType1 = (uint)aoTypeMap[z + 2 + (i + 0 + w) * 34];
                        uint sAoType2 = (uint)aoTypeMap[z + 2 + (i + 1 + w) * 34];
                        uint sAoType3 = (uint)aoTypeMap[z + 2 + (i + 2 + w) * 34];

                        ulong sFront1 = bitMap[z + 2 + (i + 0 + w) * 34];
                        ulong sFront2 = bitMap[z + 2 + (i + 1 + w) * 34];
                        ulong sFront3 = bitMap[z + 2 + (i + 2 + w) * 34];

                        uint sType = typeMap[z + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                        uint sRow = backData[iw] & (uint)((~sFront2) >> 1);

                        sRow &= ~(sType & Bit.Invert(trailingZeros));

                        int sAo = VH.GetFlippedAO(sFront1, sFront2, sFront3, trailingZeros);
                        Block sBlock = blocks[trailingZeros + z * 32 + iw * 1024];

                        if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                            break;

                        backData[iw] &= ~mask;
                        w++;
                    }

                    w--;
                    h--;

                    int packedAo = VH.GetPackedAO(ao);

                    var geometryIndex = block.GetSolidGeometryIndex(5);
                    vertexData.Add(new(geometryIndex, trailingZeros | (i << 5) | (z << 10), packedAo, h | (w << 5)));
                }
            }
        }
    }




    public static void HandleGreedyTopAndBottom(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* topData, uint* bottomData, int y)
    {
        for (int i = 0; i < 32; i++)
        {
            {
                uint aoType1 = (uint)aoTypeMap[i + 0 + (y + 2) * 34];
                uint aoType2 = (uint)aoTypeMap[i + 1 + (y + 2) * 34];
                uint aoType3 = (uint)aoTypeMap[i + 2 + (y + 2) * 34];

                ulong front1 = bitMap[i + 0 + (y + 2) * 34];
                ulong front2 = bitMap[i + 1 + (y + 2) * 34];
                ulong front3 = bitMap[i + 2 + (y + 2) * 34];

                uint type = typeMap[i + y * 32] | aoType1 | aoType2 | aoType3;
                uint row = topData[i] & (uint)((~front2) >> 1);

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

                    Block block = blocks[trailingZeros + i * 32 + y * 1024];
                    if (block.BlockId() == 0)
                        continue;
                                    
                    int ao = VH.GetAO(front1, front2, front3, trailingZeros);

                    while (w < 32 - i)
                    {
                        int iw = i + w;

                        uint sAoType1 = (uint)aoTypeMap[iw + 0 + (y + 2) * 34];
                        uint sAoType2 = (uint)aoTypeMap[iw + 1 + (y + 2) * 34];
                        uint sAoType3 = (uint)aoTypeMap[iw + 2 + (y + 2) * 34];

                        ulong sFront1 = bitMap[iw + 0 + (y + 2) * 34];
                        ulong sFront2 = bitMap[iw + 1 + (y + 2) * 34];
                        ulong sFront3 = bitMap[iw + 2 + (y + 2) * 34];

                        uint sType = typeMap[iw + y * 32] | sAoType1 | sAoType2 | sAoType3;
                        uint sRow = topData[iw] & (uint)((~sFront2) >> 1);

                        sRow &= ~(sType & Bit.Invert(trailingZeros));

                        int sAo = VH.GetAO(sFront1, sFront2, sFront3, trailingZeros);
                        Block sBlock = blocks[trailingZeros + iw * 32 + y * 1024];

                        if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                            break;

                        topData[iw] &= ~mask;
                        w++;
                    }

                    w--;
                    h--;

                    int packedAo = VH.GetPackedAO(ao);

                    var geometryIndex = block.GetSolidGeometryIndex(2);
                    vertexData.Add(new(geometryIndex, trailingZeros | (y << 5) | (i << 10), packedAo, h | (w << 10)));
                }
            }

            // Bottom
            {
                uint aoType1 = (uint)aoTypeMap[i + 0 + y * 34];
                uint aoType2 = (uint)aoTypeMap[i + 1 + y * 34];
                uint aoType3 = (uint)aoTypeMap[i + 2 + y * 34];

                ulong front1 = bitMap[i + 0 + y * 34];
                ulong front2 = bitMap[i + 1 + y * 34];
                ulong front3 = bitMap[i + 2 + y * 34];

                uint type = typeMap[i + y * 32] | aoType1 | aoType2 | aoType3;
                uint row = bottomData[i] & (uint)((~front2) >> 1);

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

                    Block block = blocks[trailingZeros + i * 32 + y * 1024];
                    if (block.BlockId() == 0)
                        continue;
                    
                    int ao = VH.GetFlippedAO(front1, front2, front3, trailingZeros);

                    while (w < 32 - i)
                    {
                        int iw = i + w;

                        uint sAoType1 = (uint)aoTypeMap[iw + 0 + y * 34];
                        uint sAoType2 = (uint)aoTypeMap[iw + 1 + y * 34];
                        uint sAoType3 = (uint)aoTypeMap[iw + 2 + y * 34];

                        ulong sFront1 = bitMap[iw + 0 + y * 34];
                        ulong sFront2 = bitMap[iw + 1 + y * 34];
                        ulong sFront3 = bitMap[iw + 2 + y * 34];

                        uint sType = typeMap[iw + y * 32] | sAoType1 | sAoType2 | sAoType3;
                        uint sRow = bottomData[iw] & (uint)((~sFront2) >> 1);

                        sRow &= ~(sType & Bit.Invert(trailingZeros));

                        int sAo = VH.GetFlippedAO(sFront1, sFront2, sFront3, trailingZeros);
                        Block sBlock = blocks[trailingZeros + iw * 32 + y * 1024];

                        if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                            break;

                        bottomData[iw] &= ~mask;
                        w++;
                    }

                    w--;
                    h--;

                    int packedAo = VH.GetPackedAO(ao);

                    var geometryIndex = block.GetSolidGeometryIndex(4);
                    vertexData.Add(new(geometryIndex, trailingZeros | (y << 5) | (i << 10), packedAo, h | (w << 10)));
                }
            }
        }
    }




    public static bool GenerateMesh2(VoxelChunk chunk, List<Vector4i> vertexData, Vector3i worldPosition, int workerId, out int vertexCount)
    {
        vertexCount = 0;
        int vertCount = 0;

        Block* blocks = chunk.Blocks;

        var renderer = chunk.Renderer;

        NeighbourChunks neighbours = new(renderer, chunk.RelativePosition);

        //Stopwatch sw = Stopwatch.StartNew();
        VoxelChunkMesher.GetVector256BitMap4(blocks, ref neighbours, workerId);
        //Console.WriteLine(sw.Elapsed.TotalMilliseconds);

        var meshData = VoxelChunkMesher.MeshDatas[workerId];
        
        ulong* bitMap = meshData.BitMap;
        ulong* aoTypeMap = meshData.AOTypeMap;

        uint* typeMap = meshData.TypeMap;
        uint* zTypeMap = meshData.ZTypeMap;

        uint* localBitMap = meshData.LocalBitMap;

        uint* frontSlice = meshData.FrontSlice;
        uint* backSlice = meshData.BackSlice;

        uint* topSlice = meshData.TopSlice;
        uint* bottomSlice = meshData.BottomSlice;

        uint* rightSlice = meshData.RightSlice;
        uint* middleSlice = meshData.MiddleSlice;
        uint* leftSlice = meshData.LeftSlice;

        ulong* rightMask = meshData.RightMaskLong;
        ulong* middleMask = meshData.MiddleMaskLong;
        ulong* leftMask = meshData.LeftMaskLong;

        ulong* rightType = meshData.RightAOType;
        ulong* middleType = meshData.MiddleAOType;
        ulong* leftType = meshData.LeftAOType;

        for (int i = 0; i < 32; i++)
        {  
            int iz = (i + 1) * 34;

            if (i == 0)
            {
                // left
                ulong rowL1 = ExtractBitMap(bitMap,      0);
                ulong rowL2 = ExtractBitMap(bitMap + 34, 0);

                ulong aoL1  = VH.GetAOTypeMap(rowL1) >> 1;
                ulong aoL2  = VH.GetAOTypeMap(rowL2) >> 1;

                leftMask[0] = rowL1;
                leftMask[1] = rowL2;

                // middle
                ulong rowM1 = ExtractBitMap(bitMap,      1);
                ulong rowM2 = ExtractBitMap(bitMap + 34, 1);

                ulong aoM1  = VH.GetAOTypeMap(rowM1) >> 1;
                ulong aoM2  = VH.GetAOTypeMap(rowM2) >> 1;

                middleMask[0] = rowM1;
                middleMask[1] = rowM2;

                // right
                ulong row1  = ExtractBitMap(bitMap,      2);
                ulong row2  = ExtractBitMap(bitMap + 34, 2);

                ulong ao1   = VH.GetAOTypeMap(row1) >> 1;
                ulong ao2   = VH.GetAOTypeMap(row2) >> 1;

                rightMask[0] = row1;
                rightMask[1] = row2;


                for (int s = 0; s < 32; s++)
                {
                    int sz = (s + 2) * 34;
                    int si = i + 1 + (s + 1) * 34;

                    // left
                    ulong rowL3 = ExtractBitMap(bitMap + sz, 0);
                    ulong aoL3 = VH.GetAOTypeMap(rowL3) >> 1;

                    leftMask[s+2] = rowL3;
                    leftType[s+1] = aoL1 | aoL2 | aoL3;

                    // middle
                    ulong rowM3 = ExtractBitMap(bitMap + sz, 1);
                    ulong aoM3 = VH.GetAOTypeMap(rowM3) >> 1;

                    middleMask[s+2] = rowM3;
                    middleType[s+1] = aoM1 | aoM2 | aoM3;

                    // right
                    ulong row3 = ExtractBitMap(bitMap + sz, 2);
                    ulong ao3 = VH.GetAOTypeMap(row3) >> 1;

                    rightMask[s+2] = row3;
                    rightType[s+1] = ao1 | ao2 | ao3;
                    
                    // slices
                    rightSlice[s]   = (uint)(rowM2 >> 1);
                    leftSlice[s]    = (uint)(rowM2 >> 1);   

                    frontSlice[s]   = (uint)(bitMap[si] >> 1);
                    backSlice[s]    = (uint)(bitMap[si] >> 1);

                    topSlice[s]     = (uint)(bitMap[s + 1 + iz] >> 1);
                    bottomSlice[s]  = (uint)(bitMap[s + 1 + iz] >> 1);

        
                    rowM2 = rowM3;

                    ao1 = ao2;
                    ao2 = ao3;
                }
            }
            else
            {
                // Get the first 2 rows
                ulong row1 = ExtractBitMap(bitMap,      (byte)(i + 2));
                ulong row2 = ExtractBitMap(bitMap + 34, (byte)(i + 2));

                ulong ao1 = VH.GetAOTypeMap(row1) >> 1;
                ulong ao2 = VH.GetAOTypeMap(row2) >> 1;

                rightMask[0] = row1;
                rightMask[1] = row2;

                for (int s = 0; s < 32; s++)
                {
                    int sz = (s + 2) * 34;
                    int si = i + 1 + (s + 1) * 34;

                    ulong mask = middleMask[s+1] >> 1;

                    // the index starts at 68 which is the 3rd element
                    ulong row3 = ExtractBitMap(bitMap + sz, (byte)(i + 2));
                    ulong ao3 = VH.GetAOTypeMap(row3) >> 1;

                    rightMask[s+2] = row3;
                    rightType[s+1] = ao1 | ao2 | ao3;

                    // slices
                    rightSlice[s] = (uint)mask;
                    leftSlice[s] = (uint)mask;

                    frontSlice[s]  = (uint)(bitMap[si] >> 1);
                    backSlice[s]   = (uint)(bitMap[si] >> 1);

                    topSlice[s]    = (uint)(bitMap[s + 1 + iz] >> 1);
                    bottomSlice[s] = (uint)(bitMap[s + 1 + iz] >> 1);
                                        
                    ao1 = ao2;
                    ao2 = ao3;
                }
            }
            

            HandleGreedyFront2( blocks, vertexData, bitMap, aoTypeMap, typeMap, frontSlice, i);
            HandleGreedyBack2(  blocks, vertexData, bitMap, aoTypeMap, typeMap, backSlice, i);

            HandleGreedyTop2(   blocks, vertexData, bitMap, aoTypeMap, typeMap, topSlice, i);
            HandleGreedyBottom2(blocks, vertexData, bitMap, aoTypeMap, typeMap, bottomSlice, i);

            HandleGreedyRight2(blocks, vertexData, rightMask, rightType, zTypeMap, rightSlice, i);
            HandleGreedyLeft2(  blocks, vertexData, leftMask, leftType, zTypeMap, leftSlice, i);

            ulong* temp = leftMask;
            leftMask = middleMask;
            middleMask = rightMask;
            rightMask = temp;

            ulong* tempType = leftType;
            leftType = middleType;
            middleType = rightType;
            rightType = tempType;
        }

        vertexCount = vertCount;

        /*
        if (worldPosition == (0, 0, 0))
        {
            string b = "";
            for (int i = 0; i < 34 * 34; i++)
            {
                for (int j = 0; j < 34; j++)
                {
                    b += ((bitMap[i] >> j) & 1) == 1 ? '1' : '0';
                }
            }
            File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "bits.txt"), b);
        }

        /*
        if (worldPosition == (0, 0, 0))
        {
            string b = "";
            for (int i = 0; i < 32 * 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    b += ((typeMap[i] >> j) & 1) == 1 ? '1' : '0';
                }
            }
            File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "types1.txt"), b);
        }

        if (worldPosition == (0, 0, 0))
        {
            string b = "";
            for (int i = 0; i < 32 * 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    b += ((zTypeMap[i] >> j) & 1) == 1 ? '1' : '0';
                }
            }
            File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "types2.txt"), b);
        }
        */

        return true;
    }



    public static void HandleGreedyFront2(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int z)
    {
        for (int i = 0; i < 32; i++)
        {
            uint aoType1 = (uint)aoTypeMap[z + (i + 0) * 34];
            uint aoType2 = (uint)aoTypeMap[z + (i + 1) * 34];
            uint aoType3 = (uint)aoTypeMap[z + (i + 2) * 34];

            ulong front1 = bitMap[z + (i + 0) * 34];
            ulong front2 = bitMap[z + (i + 1) * 34];
            ulong front3 = bitMap[z + (i + 2) * 34];

            uint type = typeMap[z + i * 32] | aoType1 | aoType2 | aoType3;
            uint row = data[i] & (uint)((~front2) >> 1);

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

                Block block = blocks[trailingZeros + z * 32 + i * 1024];
                if (block.BlockId() == 0)
                    continue;

                int ao = VH.GetAO(front1, front2, front3, trailingZeros);

                while (w < 32 - i)
                {
                    int iw = i + w;

                    uint sAoType1 = (uint)aoTypeMap[z + (i + 0 + w) * 34];
                    uint sAoType2 = (uint)aoTypeMap[z + (i + 1 + w) * 34];
                    uint sAoType3 = (uint)aoTypeMap[z + (i + 2 + w) * 34];

                    ulong sFront1 = bitMap[z + (i + 0 + w) * 34];
                    ulong sFront2 = bitMap[z + (i + 1 + w) * 34];
                    ulong sFront3 = bitMap[z + (i + 2 + w) * 34];

                    uint sType = typeMap[z + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                    uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    int sAo = VH.GetAO(sFront1, sFront2, sFront3, trailingZeros);
                    Block sBlock = blocks[trailingZeros + z * 32 + iw * 1024];

                    if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                int packedAo = VH.GetPackedAO(ao);

                var definition = block.Definition();
                vertexData.Add(new(definition.GetSolidGeometryIndex(0), trailingZeros | (i << 5) | (z << 10), packedAo, h | (w << 5)));
            }
        }
    }

    public static void HandleGreedyBack2(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int z)
    {
        for (int i = 0; i < 32; i++)
        {
            uint aoType1 = (uint)aoTypeMap[z + 2 + (i + 0) * 34];
            uint aoType2 = (uint)aoTypeMap[z + 2 + (i + 1) * 34];
            uint aoType3 = (uint)aoTypeMap[z + 2 + (i + 2) * 34];

            ulong front1 = bitMap[z + 2 + (i + 0) * 34];
            ulong front2 = bitMap[z + 2 + (i + 1) * 34];
            ulong front3 = bitMap[z + 2 + (i + 2) * 34];

            uint type = typeMap[z + i * 32] | aoType1 | aoType2 | aoType3;
            uint row = data[i] & (uint)((~front2) >> 1);

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

                Block block = blocks[trailingZeros + z * 32 + i * 1024];
                if (block.BlockId() == 0)
                    continue;

                int ao = VH.GetFlippedAO(front1, front2, front3, trailingZeros);
                
                while (w < 32 - i)
                {
                    int iw = i + w;

                    uint sAoType1 = (uint)aoTypeMap[z + 2 + (i + 0 + w) * 34];
                    uint sAoType2 = (uint)aoTypeMap[z + 2 + (i + 1 + w) * 34];
                    uint sAoType3 = (uint)aoTypeMap[z + 2 + (i + 2 + w) * 34];

                    ulong sFront1 = bitMap[z + 2 + (i + 0 + w) * 34];
                    ulong sFront2 = bitMap[z + 2 + (i + 1 + w) * 34];
                    ulong sFront3 = bitMap[z + 2 + (i + 2 + w) * 34];

                    uint sType = typeMap[z + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                    uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    int sAo = VH.GetFlippedAO(sFront1, sFront2, sFront3, trailingZeros);
                    Block sBlock = blocks[trailingZeros + z * 32 + iw * 1024];

                    if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                int packedAo = VH.GetPackedAO(ao);

                var definition = block.Definition();
                vertexData.Add(new(definition.GetSolidGeometryIndex(5), trailingZeros | (i << 5) | (z << 10), packedAo, h | (w << 5)));
            }
        }
    }


    public static void HandleGreedyRight2(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int x)
    {
        for (int i = 0; i < 32; i++)
        {
            uint aoType = (uint)aoTypeMap[i + 1];

            ulong front1 = bitMap[i];
            ulong front2 = bitMap[i + 1];
            ulong front3 = bitMap[i + 2];
            
            uint type = typeMap[x + i * 32] | aoType;
            uint row = data[i] & (uint)((~front2) >> 1);

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

                Block block = blocks[x + trailingZeros * 32 + i * 1024];
                if (block.BlockId() == 0)
                    continue;

                int ao = VH.GetAO(front1, front2, front3, trailingZeros);

                while (w < 32 - i)
                {
                    int iw = i + w;

                    uint sAoType = (uint)aoTypeMap[iw + 1];

                    ulong sFront1 = bitMap[iw];
                    ulong sFront2 = bitMap[iw + 1];
                    ulong sFront3 = bitMap[iw + 2];

                    uint sType = typeMap[x + iw * 32] | sAoType;
                    uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    int sAo = VH.GetAO(sFront1, sFront2, sFront3, trailingZeros);
                    Block sBlock = blocks[x + trailingZeros * 32 + iw * 1024];

                    if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                int packedAo = VH.GetPackedAO(ao);

                var definition = block.Definition();
                vertexData.Add(new(definition.GetSolidGeometryIndex(1), x | (i << 5) | (trailingZeros << 10), packedAo, (w << 5) | (h << 10)));
            }
        }
    }

    public static void HandleGreedyLeft2(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int x)
    {
        for (int i = 0; i < 32; i++)
        {
            uint aoType = (uint)aoTypeMap[i + 1];

            ulong front1 = bitMap[i];
            ulong front2 = bitMap[i + 1];
            ulong front3 = bitMap[i + 2];
            
            uint type = typeMap[x + i * 32] | aoType;
            uint row = data[i] & (uint)((~front2) >> 1);

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

                Block block = blocks[x + trailingZeros * 32 + i * 1024];
                if (block.BlockId() == 0)
                    continue;

                int ao = VH.GetFlippedAO(front1, front2, front3, trailingZeros);
                
                while (w < 32 - i)
                {
                    int iw = i + w;

                    uint sAoType = (uint)aoTypeMap[iw + 1];

                    ulong sFront1 = bitMap[iw];
                    ulong sFront2 = bitMap[iw + 1];
                    ulong sFront3 = bitMap[iw + 2];

                    uint sType = typeMap[x + iw * 32] | sAoType;
                    uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    int sAo = VH.GetFlippedAO(sFront1, sFront2, sFront3, trailingZeros);
                    Block sBlock = blocks[x + trailingZeros * 32 + iw * 1024];

                    if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                int packedAo = VH.GetPackedAO(ao);

                var definition = block.Definition();
                vertexData.Add(new(definition.GetSolidGeometryIndex(3), x | (i << 5) | (trailingZeros << 10), packedAo, (w << 5) | (h << 10)));
            }
        }
    }

    public static void HandleGreedyTop2(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int y)
    {
        for (int i = 0; i < 32; i++)
        {
            uint aoType1 = (uint)aoTypeMap[i + 0 + (y + 2) * 34];
            uint aoType2 = (uint)aoTypeMap[i + 1 + (y + 2) * 34];
            uint aoType3 = (uint)aoTypeMap[i + 2 + (y + 2) * 34];

            ulong front1 = bitMap[i + 0 + (y + 2) * 34];
            ulong front2 = bitMap[i + 1 + (y + 2) * 34];
            ulong front3 = bitMap[i + 2 + (y + 2) * 34];

            uint type = typeMap[i + y * 32] | aoType1 | aoType2 | aoType3;
            uint row = data[i] & (uint)((~front2) >> 1);

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

                Block block = blocks[trailingZeros + i * 32 + y * 1024];
                if (block.BlockId() == 0)
                    continue;
                                
                int ao = VH.GetAO(front1, front2, front3, trailingZeros);

                while (w < 32 - i)
                {
                    int iw = i + w;

                    uint sAoType1 = (uint)aoTypeMap[iw + 0 + (y + 2) * 34];
                    uint sAoType2 = (uint)aoTypeMap[iw + 1 + (y + 2) * 34];
                    uint sAoType3 = (uint)aoTypeMap[iw + 2 + (y + 2) * 34];

                    ulong sFront1 = bitMap[iw + 0 + (y + 2) * 34];
                    ulong sFront2 = bitMap[iw + 1 + (y + 2) * 34];
                    ulong sFront3 = bitMap[iw + 2 + (y + 2) * 34];

                    uint sType = typeMap[iw + y * 32] | sAoType1 | sAoType2 | sAoType3;
                    uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    int sAo = VH.GetAO(sFront1, sFront2, sFront3, trailingZeros);
                    Block sBlock = blocks[trailingZeros + iw * 32 + y * 1024];

                    if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                int packedAo = VH.GetPackedAO(ao);

                var definition = block.Definition();
                vertexData.Add(new(definition.GetSolidGeometryIndex(2), trailingZeros | (y << 5) | (i << 10), packedAo, h | (w << 10)));
            }
        }
    }

    public static void HandleGreedyBottom2(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, ulong* aoTypeMap, uint* typeMap, uint* data, int y)
    {
        for (int i = 0; i < 32; i++)
        {
            uint aoType1 = (uint)aoTypeMap[i + 0 + y * 34];
            uint aoType2 = (uint)aoTypeMap[i + 1 + y * 34];
            uint aoType3 = (uint)aoTypeMap[i + 2 + y * 34];

            ulong front1 = bitMap[i + 0 + y * 34];
            ulong front2 = bitMap[i + 1 + y * 34];
            ulong front3 = bitMap[i + 2 + y * 34];

            uint type = typeMap[i + y * 32] | aoType1 | aoType2 | aoType3;
            uint row = data[i] & (uint)((~front2) >> 1);

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

                Block block = blocks[trailingZeros + i * 32 + y * 1024];
                if (block.BlockId() == 0)
                    continue;
                
                int ao = VH.GetFlippedAO(front1, front2, front3, trailingZeros);

                while (w < 32 - i)
                {
                    int iw = i + w;

                    uint sAoType1 = (uint)aoTypeMap[iw + 0 + y * 34];
                    uint sAoType2 = (uint)aoTypeMap[iw + 1 + y * 34];
                    uint sAoType3 = (uint)aoTypeMap[iw + 2 + y * 34];

                    ulong sFront1 = bitMap[iw + 0 + y * 34];
                    ulong sFront2 = bitMap[iw + 1 + y * 34];
                    ulong sFront3 = bitMap[iw + 2 + y * 34];

                    uint sType = typeMap[iw + y * 32] | sAoType1 | sAoType2 | sAoType3;
                    uint sRow = data[iw] & (uint)((~sFront2) >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    int sAo = VH.GetFlippedAO(sFront1, sFront2, sFront3, trailingZeros);
                    Block sBlock = blocks[trailingZeros + iw * 32 + y * 1024];

                    if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                int packedAo = VH.GetPackedAO(ao);

                var definition = block.Definition();
                vertexData.Add(new(definition.GetSolidGeometryIndex(4), trailingZeros | (y << 5) | (i << 10), packedAo, h | (w << 10)));
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
}