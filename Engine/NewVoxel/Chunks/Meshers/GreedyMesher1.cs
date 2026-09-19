using PBG.MathLibrary;

namespace PBG.NewVoxel;

using VH = VoxelHelper;

public unsafe static class GreedyMesher1
{
    public static bool GenerateMesh(VoxelChunk chunk, List<Vector4i> vertexData, Vector3i worldPosition, int workerId, out int vertexCount)
    {
        vertexCount = 0;
        int vertCount = 0;

        Block* blocks = chunk.Blocks;

        var renderer = chunk.Renderer;

        NeighbourChunks neighbours = new(renderer, chunk.RelativePosition);

        //Stopwatch sw = Stopwatch.StartNew();

        GetVector256BitMap2(blocks, ref neighbours, workerId);

        //string time = ""+sw.Elapsed.TotalMilliseconds;

        //sw.Restart();

        //VoxelChunkMesher.GetVector256BitMap2(blocks, ref neighbours, workerId);

        //Console.WriteLine(time + " " + sw.Elapsed.TotalMilliseconds);

        //var meshData = VoxelChunkMesher.GetMeshData(workerId);

        ulong[] bitMap = VoxelChunkGenerator.BitMaps[workerId];
        ulong[] bitMapX = VoxelChunkGenerator.SolidMaps[workerId];
        MeshSlices slices = VoxelChunkGenerator.Slices[workerId];

        uint* frontSlice = slices.FrontSlice;
        uint* backSlice = slices.BackSlice;

        uint* rightSlice = slices.RightSlice;
        uint* leftSlice = slices.LeftSlice;

        uint* topSlice = slices.TopSlice;
        uint* bottomSlice = slices.BottomSlice;

        for (int i = 0; i < 32; i++)
        {  
            int iz = (i + 1) * 34;
            for (int s = 0; s < 32; s++)
            {
                int sz = (s + 1) * 34;

                int si = i + 1 + sz;
                
                frontSlice[s]   = (uint)(bitMap[si] >> 1);
                backSlice[s]    = (uint)(bitMap[si] >> 1);

                rightSlice[s]   = (uint)(bitMapX[si] >> 1);
                leftSlice[s]    = (uint)(bitMapX[si] >> 1);

                topSlice[s]     = (uint)(bitMap[s + 1 + iz] >> 1);
                bottomSlice[s]  = (uint)(bitMap[s + 1 + iz] >> 1);

            }

            // greedy
            //if (i == 0)
            HandleGreedyFront(vertexData, bitMap, frontSlice, i);
            HandleGreedyBack(vertexData, bitMap, backSlice, i);

            HandleGreedyTop(vertexData, bitMap, topSlice, i);
            HandleGreedyBottom(vertexData, bitMap, bottomSlice, i);

            HandleGreedyRight(vertexData, bitMapX, rightSlice, i);
            HandleGreedyLeft(vertexData, bitMapX, leftSlice, i);
        }

        /*
        if (worldPosition == (0, 64, -32))
        {
            string b = "";
            for (int i = 0; i < 34 * 34; i++)
            {
                for (int j = 0; j < 34; j++)
                {
                    b += ((bitMapX[i] >> j) & 1) == 1 ? '1' : '0';
                }
            }
            File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "bits.txt"), b);
        }
        */

        /*
        if (worldPosition == (0, 64, -32))
        {
            GenerateIndirectMesh2(chunk, vertexData, worldPosition, workerId, out vertCount);
            for (int x = 0; x < vertexData.Count; x++)
            {
                var d = vertexData[x];
                d.W = 0b100000;
                vertexData[x] = d;
            }
        }
        */

        vertexCount = vertCount;

        return true;
    }

    public static void HandleGreedyFront(List<Vector4i> vertexData, ulong[] bitMap, uint* data, int z)
    {
        for (int i = 0; i < 32; i++)
        {
            ulong front = ~bitMap[z + (i + 1) * 34];
            uint row = data[i] & (uint)(front >> 1);

            while (row != 0)
            {
                int trailingZeros = Bit.TrailingZeros(row);

                uint newRow = row >> trailingZeros;

                int h = Bit.TrailingZeros(~newRow);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;

                int w = 1;

                while (w < 32 - i)
                {
                    ulong sFront = ~bitMap[z + (i + 1 + w) * 34];
                    uint sRow = data[i + w] & (uint)(sFront >> 1);

                    if ((sRow & mask) != mask)
                        break;

                    data[i + w] &= ~mask;
                    w++;
                }

                w--;
                h--;

                vertexData.Add(new(0, trailingZeros | (i << 5) | (z << 10), 0, h | (w << 5)));
            }
        }
    }

    public static void HandleGreedyBack(List<Vector4i> vertexData, ulong[] bitMap, uint* data, int z)
    {
        for (int i = 0; i < 32; i++)
        {
            ulong front = ~bitMap[z + 2 + (i + 1) * 34];
            uint row = data[i] & (uint)(front >> 1);

            while (row != 0)
            {
                int trailingZeros = Bit.TrailingZeros(row);

                uint newRow = row >> trailingZeros;

                int h = Bit.TrailingZeros(~newRow);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;

                int w = 1;

                while (w < 32 - i)
                {
                    ulong sFront = ~bitMap[z + 2 + (i + 1 + w) * 34];
                    uint sRow = data[i + w] & (uint)(sFront >> 1);

                    if ((sRow & mask) != mask)
                        break;

                    data[i + w] &= ~mask;
                    w++;
                }

                w--;
                h--;

                vertexData.Add(new(20, trailingZeros | (i << 5) | (z << 10), 0, h | (w << 5)));
            }
        }
    }


    public static void HandleGreedyRight(List<Vector4i> vertexData, ulong[] bitMap, uint* data, int x)
    {
        for (int i = 0; i < 32; i++)
        {
            ulong front = ~bitMap[x + 2 + (i + 1) * 34];
            uint row = data[i] & (uint)(front >> 1);

            while (row != 0)
            {
                int trailingZeros = Bit.TrailingZeros(row);

                uint newRow = row >> trailingZeros;

                int h = Bit.TrailingZeros(~newRow);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;

                int w = 1;

                while (w < 32 - i)
                {
                    ulong sFront = ~bitMap[x + 2 + (i + w + 1) * 34];
                    uint sRow = data[i + w] & (uint)(sFront >> 1);

                    if ((sRow & mask) != mask)
                        break;

                    data[i + w] &= ~mask;
                    w++;
                }

                w--;
                h--;

                vertexData.Add(new(4, x | (i << 5) | (trailingZeros << 10), 0, (w << 5) | (h << 10)));
            }
        }
    }

    public static void HandleGreedyLeft(List<Vector4i> vertexData, ulong[] bitMap, uint* data, int x)
    {
        for (int i = 0; i < 32; i++)
        {
            ulong front = ~bitMap[x + (i + 1) * 34];
            uint row = data[i] & (uint)(front >> 1);

            while (row != 0)
            {
                int trailingZeros = Bit.TrailingZeros(row);

                uint newRow = row >> trailingZeros;

                int h = Bit.TrailingZeros(~newRow);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;

                int w = 1;

                while (w < 32 - i)
                {
                    ulong sFront = ~bitMap[x + (i + w + 1) * 34];
                    uint sRow = data[i + w] & (uint)(sFront >> 1);

                    if ((sRow & mask) != mask)
                        break;

                    data[i + w] &= ~mask;
                    w++;
                }

                w--;
                h--;

                vertexData.Add(new(12, x | (i << 5) | (trailingZeros << 10), 0, (w << 5) | (h << 10)));
            }
        }
    }

    public static void HandleGreedyTop(List<Vector4i> vertexData, ulong[] bitMap, uint* data, int y)
    {
        for (int i = 0; i < 32; i++)
        {
            ulong front = ~bitMap[i + 1 + (y + 2) * 34];
            uint row = data[i] & (uint)(front >> 1);

            while (row != 0)
            {
                int trailingZeros = Bit.TrailingZeros(row);

                uint newRow = row >> trailingZeros;

                int h = Bit.TrailingZeros(~newRow);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;

                int w = 1;

                while (w < 32 - i)
                {
                    ulong sFront = ~bitMap[i + w + 1 + (y + 2) * 34];
                    uint sRow = data[i + w] & (uint)(sFront >> 1);

                    if ((sRow & mask) != mask)
                        break;

                    data[i + w] &= ~mask;
                    w++;
                }

                w--;
                h--;

                vertexData.Add(new(8, trailingZeros | (y << 5) | (i << 10), 0, h | (w << 10) ));
            }
        }
    }

    public static void HandleGreedyBottom(List<Vector4i> vertexData, ulong[] bitMap, uint* data, int y)
    {
        for (int i = 0; i < 32; i++)
        {
            ulong front = ~bitMap[i + 1 + y * 34];
            uint row = data[i] & (uint)(front >> 1);

            while (row != 0)
            {
                int trailingZeros = Bit.TrailingZeros(row);

                uint newRow = row >> trailingZeros;

                int h = Bit.TrailingZeros(~newRow);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;

                int w = 1;

                while (w < 32 - i)
                {
                    ulong sFront = ~bitMap[i + w + 1 + y * 34];
                    uint sRow = data[i + w] & (uint)(sFront >> 1);

                    if ((sRow & mask) != mask)
                        break;

                    data[i + w] &= ~mask;
                    w++;
                }

                w--;
                h--;

                vertexData.Add(new(16, trailingZeros | (y << 5) | (i << 10), 0, h | (w << 10)));
            }
        }
    }

    public static void GetVector256BitMap2(Block* blocks, ref NeighbourChunks neighbours, int workerId)
    {
        ulong[] bitMap = VoxelChunkGenerator.BitMaps[workerId];
        ulong[] bitMapX = VoxelChunkGenerator.SolidMaps[workerId];

        int maxYlayerIndex = 31744;

        for (int y = 0; y < 32; y++)
        {
            // the index progress in the regular block array
            int blockIndex = y * 1024;
            int mapIndex = (y + 1) * 34 + 1;

            for (int z = 0; z < 32; z++)
            {
                ulong row = 0;

                uint* ptr = (uint*)blocks + blockIndex;

                row |= VH.GetV256uRowX64(ptr, 0) | VH.GetV256uRowX64(ptr, 2); // right here

                row <<= 1;

                row |= neighbours.Blocks12[blockIndex + 31].blockData != 0 ? 1UL : 0;
                row |= neighbours.Blocks14[blockIndex].blockData != 0 ? 0x200000000UL : 0;

                bitMap[mapIndex] = row;

                blockIndex += 32;
                mapIndex++;
            }
        }
        
        for (int y = 0; y < 32; y++)
        {
            // the index progress in the regular block array
            int yIndex = y * 1024;
            int zIndexY = y * 32; // simulation of z using y

            // rows in the direction of x, on the plane P (+) / N (-), on the axis z / y
            ulong rowNz = 0;
            ulong rowPz = 0;
            
            ulong rowNy = 0;
            ulong rowPy = 0;

            int indexNz = 992 + yIndex; // represent the block index in the blocks of the chunk next to this one in the direction of -z
            int indexPz = yIndex;

            int indexNy = zIndexY + maxYlayerIndex; // simulate z progression using y at the top y layer
            int indexPy = zIndexY; // simulate z progression using y at the bottom y layer

            uint* ptr10 = (uint*)neighbours.Blocks10 + indexNz;
            uint* ptr16 = (uint*)neighbours.Blocks16 + indexPz;
            uint* ptr4  = (uint*)neighbours.Blocks4  + indexNy;
            uint* ptr22 = (uint*)neighbours.Blocks22 + indexPy;

            // handle chunk that is in -z at index 10 and i need the last block
            rowNz |= VH.GetV256uRowX64(ptr10, 0) | VH.GetV256uRowX64(ptr10, 2);
            
            // handle chunk that is in +z at index 16 and i need the first block
            rowPz |= VH.GetV256uRowX64(ptr16, 0) | VH.GetV256uRowX64(ptr16, 2);

            // handle chunk that is in -y at index 4 and i need the last block
            rowNy |= VH.GetV256uRowX64(ptr4,  0) | VH.GetV256uRowX64(ptr4,  2);

            // handle chunk that is in +y at index 22 and i need the first block
            rowPy |= VH.GetV256uRowX64(ptr22, 0) | VH.GetV256uRowX64(ptr22, 2);

            int rowZindex = (y + 1) * 34;

            bitMap[rowZindex] = rowNz << 1;
            bitMap[33 + rowZindex] = rowPz << 1;

            bitMap[y + 1] = rowNy << 1; // on the z axis of the bit map (y simulates z)
            bitMap[1123 + y] = rowPy << 1; // on the z axis of the bit map but at the last row (y simulates z) (1123 is 33 x 34 = 1122 and +1 like the line before (y + 1))
        }

        // transposing the bits from bitmap to bitmapX
        fixed (ulong* bP = bitMap)
        {
            for (int y = 0; y < 34; y++)
            {
                int baseIndex = y * 34;

                for (int x = 0; x < 34; x++)
                {
                    ulong row = (ulong)VH.ExtractBitsX64(bP + baseIndex + 0,  (byte)x) << 0  | 
                                (ulong)VH.ExtractBitsX64(bP + baseIndex + 8,  (byte)x) << 8  | 
                                (ulong)VH.ExtractBitsX64(bP + baseIndex + 16, (byte)x) << 16 | 
                                (ulong)VH.ExtractBitsX64(bP + baseIndex + 24, (byte)x) << 24 | 
                                (ulong)VH.ExtractBits(   bP + baseIndex + 32, (byte)x) << 32;

                    bitMapX[baseIndex + x] = row;
                }
            }
        }
    }
}