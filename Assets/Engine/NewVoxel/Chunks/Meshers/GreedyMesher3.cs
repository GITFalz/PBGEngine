using PBG.MathLibrary;

namespace PBG.NewVoxel;

using VH = VoxelHelper;

public unsafe static class GreedyMesher3
{
    public static bool GenerateMesh(VoxelChunk chunk, List<Vector4i> vertexData, Vector3i worldPosition, int workerId, out int vertexCount)
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
        uint* typeMap = meshData.TypeMap;
        uint* zTypeMap = meshData.ZTypeMap;

        uint* localBitMap = meshData.LocalBitMap;

        uint* frontSlice = meshData.FrontSlice;
        uint* backSlice = meshData.BackSlice;

        uint* rightSlice = meshData.RightSlice;
        uint* leftSlice = meshData.LeftSlice;

        uint* topSlice = meshData.TopSlice;
        uint* bottomSlice = meshData.BottomSlice;

        uint* rightMask = meshData.RightMask;
        uint* xSlice = meshData.XSlice;
        uint* leftMask = meshData.LeftMask;


        for (int i = 0; i < 32; i++)
        {  
            int iz = (i + 1) * 34;

            for (int s = 0; s < 32; s++)
            {
                int sz = (s + 1) * 34;
                int si = i + 1 + sz;

                if (i == 0)
                {
                    ulong row1 = ExtractBitMap(bitMap + sz + 1, 0);
                    ulong row2 = ExtractBitMap(bitMap + sz + 1, 1);

                    leftMask[s] = (uint)row1;
                    xSlice[s]   = (uint)row2;
                }

                ulong row = ExtractBitMap(bitMap + sz + 1, (byte)(i + 2));

                rightMask[s]    = (uint)row;
                
                frontSlice[s]   = (uint)(bitMap[si] >> 1);
                backSlice[s]    = (uint)(bitMap[si] >> 1);

                topSlice[s]     = (uint)(bitMap[s + 1 + iz] >> 1);
                bottomSlice[s]  = (uint)(bitMap[s + 1 + iz] >> 1);
            }

            Copy32(xSlice, rightSlice);
            Copy32(xSlice, leftSlice);

            HandleGreedyFront( blocks, vertexData, bitMap, typeMap, frontSlice, i);
            HandleGreedyBack(  blocks, vertexData, bitMap, typeMap, backSlice, i);

            HandleGreedyTop(   blocks, vertexData, bitMap, typeMap, topSlice, i);
            HandleGreedyBottom(blocks, vertexData, bitMap, typeMap, bottomSlice, i);

            HandleGreedyRight( blocks, vertexData, rightMask, zTypeMap, rightSlice, i);
            HandleGreedyLeft(  blocks, vertexData, leftMask, zTypeMap, leftSlice, i);

            uint* temp = leftMask;
            leftMask = xSlice;
            xSlice = rightMask;
            rightMask = temp;
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

    public static void HandleGreedyFront(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, uint* typeMap, uint* data, int z)
    {
        for (int i = 0; i < 32; i++)
        {
            ulong front = ~bitMap[z + (i + 1) * 34];
            uint type = typeMap[z + i * 32];
            uint row = data[i] & (uint)(front >> 1);

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
                
                while (w < 32 - i)
                {
                    int iw = i + w;

                    ulong sFront = ~bitMap[z + (i + 1 + w) * 34];
                    uint sType = typeMap[z + iw * 32];
                    uint sRow = data[iw] & (uint)(sFront >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    if ((sRow & mask) != mask || block != blocks[trailingZeros + z * 32 + iw * 1024])
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                var definition = block.Definition();
                vertexData.Add(new(definition.GetSolidGeometryIndex(0), trailingZeros | (i << 5) | (z << 10), 0, h | (w << 5)));
            }
        }
    }

    public static void HandleGreedyBack(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, uint* typeMap, uint* data, int z)
    {
        for (int i = 0; i < 32; i++)
        {
            ulong front = ~bitMap[z + 2 + (i + 1) * 34];
            uint type = typeMap[z + i * 32];
            uint row = data[i] & (uint)(front >> 1);

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
                
                while (w < 32 - i)
                {
                    int iw = i + w;

                    ulong sFront = ~bitMap[z + 2 + (i + 1 + w) * 34];
                    uint sType = typeMap[z + iw * 32];
                    uint sRow = data[iw] & (uint)(sFront >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    if ((sRow & mask) != mask || block != blocks[trailingZeros + z * 32 + iw * 1024])
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                var definition = block.Definition();
                vertexData.Add(new(definition.GetSolidGeometryIndex(5), trailingZeros | (i << 5) | (z << 10), 0, h | (w << 5)));
            }
        }
    }


    public static void HandleGreedyRight(Block* blocks, List<Vector4i> vertexData, uint* bitMap, uint* typeMap, uint* data, int x)
    {
        for (int i = 0; i < 32; i++)
        {
            uint front = ~bitMap[i];
            uint type = typeMap[x + i * 32];
            uint row = data[i] & front;

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
                
                while (w < 32 - i)
                {
                    int iw = i + w;

                    uint sFront = ~bitMap[iw];
                    uint sType = typeMap[x + iw * 32];
                    uint sRow = data[iw] & sFront;

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    if ((sRow & mask) != mask || block != blocks[x + trailingZeros * 32 + iw * 1024])
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                var definition = block.Definition();
                vertexData.Add(new(definition.GetSolidGeometryIndex(1), x | (i << 5) | (trailingZeros << 10), 0, (w << 5) | (h << 10)));
            }
        }
    }

    public static void HandleGreedyLeft(Block* blocks, List<Vector4i> vertexData, uint* bitMap, uint* typeMap, uint* data, int x)
    {
        for (int i = 0; i < 32; i++)
        {
            uint front = ~bitMap[i];
            uint type = typeMap[x + i * 32];
            uint row = data[i] & front;

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
                
                while (w < 32 - i)
                {
                    int iw = i + w;

                    uint sFront = ~bitMap[iw];
                    uint sType = typeMap[x + iw * 32];
                    uint sRow = data[iw] & sFront;

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    if ((sRow & mask) != mask || block != blocks[x + trailingZeros * 32 + iw * 1024])
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                var definition = block.Definition();
                vertexData.Add(new(definition.GetSolidGeometryIndex(3), x | (i << 5) | (trailingZeros << 10), 0, (w << 5) | (h << 10)));
            }
        }
    }

    public static void HandleGreedyTop(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, uint* typeMap, uint* data, int y)
    {
        for (int i = 0; i < 32; i++)
        {
            ulong front = ~bitMap[i + 1 + (y + 2) * 34];
            uint type = typeMap[i + y * 32];
            uint row = data[i] & (uint)(front >> 1);

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
                
                while (w < 32 - i)
                {
                    int iw = i + w;

                    ulong sFront = ~bitMap[iw + 1 + (y + 2) * 34];
                    uint sType = typeMap[iw + y * 32];
                    uint sRow = data[iw] & (uint)(sFront >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    if ((sRow & mask) != mask || block != blocks[trailingZeros + iw * 32 + y * 1024])
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                var definition = block.Definition();
                vertexData.Add(new(definition.GetSolidGeometryIndex(2), trailingZeros | (y << 5) | (i << 10), 0, h | (w << 10)));
            }
        }
    }

    public static void HandleGreedyBottom(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, uint* typeMap, uint* data, int y)
    {
        for (int i = 0; i < 32; i++)
        {
            ulong front = ~bitMap[i + 1 + y * 34];
            uint type = typeMap[i + y * 32];
            uint row = data[i] & (uint)(front >> 1);

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
                
                while (w < 32 - i)
                {
                    int iw = i + w;

                    ulong sFront = ~bitMap[iw + 1 + y * 34];
                    uint sType = typeMap[iw + y * 32];
                    uint sRow = data[iw] & (uint)(sFront >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    if ((sRow & mask) != mask || block != blocks[trailingZeros + iw * 32 + y * 1024])
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                var definition = block.Definition();
                vertexData.Add(new(definition.GetSolidGeometryIndex(4), trailingZeros | (y << 5) | (i << 10), 0, h | (w << 10)));
            }
        }
    }


    private static ulong ExtractBitMap(ulong* ptr, byte shift) =>
        (ulong)VH.ExtractBitsX64(ptr + 0,  shift)       | 
        (ulong)VH.ExtractBitsX64(ptr + 8,  shift) << 8  | 
        (ulong)VH.ExtractBitsX64(ptr + 16, shift) << 16 | 
        (ulong)VH.ExtractBitsX64(ptr + 24, shift) << 24;

    private static void Copy32(uint* src, uint* dst)
    {
        Buffer.MemoryCopy(src, dst, 128, 128);
    }
}