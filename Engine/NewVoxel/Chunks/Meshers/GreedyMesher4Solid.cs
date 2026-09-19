using System.Runtime.CompilerServices;
using PBG.MathLibrary;

namespace PBG.NewVoxel;

using VH = VoxelHelper;

public unsafe static class GreedyMesher4Solid
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

            for (int s = 0; s < 32; s++)
            {
                int sz = (s + 1) * 34;
                int si = i + 1 + sz;

                if (i == 0)
                {
                    ulong row1 = ExtractBitMap(bitMap + sz, 0);
                    ulong row2 = ExtractBitMap(bitMap + sz, 1);

                    uint sRow1 = (uint)(row1 >> 1);
                    uint sRow2 = (uint)(row2 >> 1);

                    // masks
                    leftMask[s] = sRow1;
                    middleMask[s] = sRow2;

                    // types
                    leftType[s+1] = VH.GetAOTypeMap(row1) >> 1;
                    middleType[s+1] = VH.GetAOTypeMap(row2) >> 1;

                    // slices
                    rightSlice[s] = sRow2;
                    leftSlice[s] = sRow2;   
                }
                else
                {
                    // slices
                    rightSlice[s] = (uint)middleMask[s];
                    leftSlice[s] = (uint)middleMask[s];
                }

                ulong row = ExtractBitMap(bitMap + sz, (byte)(i + 2));

                rightMask[s]    = (uint)(row >> 1);

                rightType[s+1]  = VH.GetAOTypeMap(row) >> 1;
                
                frontSlice[s]   = (uint)(bitMap[si] >> 1);
                backSlice[s]    = (uint)(bitMap[si] >> 1);

                topSlice[s]     = (uint)(bitMap[s + 1 + iz] >> 1);
                bottomSlice[s]  = (uint)(bitMap[s + 1 + iz] >> 1);
            }

            HandleGreedyFront( blocks, vertexData, bitMap, aoTypeMap, typeMap, frontSlice, i);
            HandleGreedyBack(  blocks, vertexData, bitMap, aoTypeMap, typeMap, backSlice, i);

            HandleGreedyTop(   blocks, vertexData, bitMap, aoTypeMap, typeMap, topSlice, i);
            HandleGreedyBottom(blocks, vertexData, bitMap, aoTypeMap, typeMap, bottomSlice, i);

            HandleGreedyRight( blocks, vertexData, rightMask, zTypeMap, rightSlice, i);
            HandleGreedyLeft(  blocks, vertexData, leftMask, zTypeMap, leftSlice, i);

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

                int ao = VH.GetAO(front1, front2, front3, trailingZeros);
                Block block = blocks[trailingZeros + z * 32 + i * 1024];
                if (block.BlockId() == 0)
                    continue;

                uint newRow = row >> trailingZeros;
                uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                int h = Bit.TrailingZeros((~newRow) | newType);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;
                type &= ~mask;

                int w = 1;

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
                vertexData.Add(new(0, trailingZeros | (i << 5) | (z << 10), packedAo, h | (w << 5)));
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

                int ao = VH.GetFlippedAO(front1, front2, front3, trailingZeros);
                Block block = blocks[trailingZeros + z * 32 + i * 1024];
                if (block.BlockId() == 0)
                    continue;

                uint newRow = row >> trailingZeros;
                uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                int h = Bit.TrailingZeros((~newRow) | newType);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;
                type &= ~mask;

                int w = 1;
                
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
                vertexData.Add(new(5, trailingZeros | (i << 5) | (z << 10), packedAo, h | (w << 5)));
            }
        }
    }


    public static void HandleGreedyRight(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, uint* typeMap, uint* data, int x)
    {
        for (int i = 0; i < 32; i++)
        {
            ulong front = ~bitMap[i];
            uint type = typeMap[x + i * 32];
            uint row = data[i] & (uint)(front); // >> 1);

            while (row != 0)
            {
                int trailingZeros = Bit.TrailingZeros(row);

                Block block = blocks[x + trailingZeros * 32 + i * 1024];
                if (block.BlockId() == 0)
                    continue;

                uint newRow = row >> trailingZeros;
                uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                int h = Bit.TrailingZeros((~newRow) | newType);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;
                type &= ~mask;

                int w = 1;

                while (w < 32 - i)
                {
                    int iw = i + w;

                    ulong sFront = ~bitMap[iw];
                    uint sType = typeMap[x + iw * 32];
                    uint sRow = data[iw] & (uint)(sFront); // >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    if ((sRow & mask) != mask || block != blocks[x + trailingZeros * 32 + iw * 1024])
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                var definition = block.Definition();
                vertexData.Add(new(1, x | (i << 5) | (trailingZeros << 10), 0, (w << 5) | (h << 10)));
            }
        }
    }

    public static void HandleGreedyLeft(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, uint* typeMap, uint* data, int x)
    {
        for (int i = 0; i < 32; i++)
        {
            ulong front = ~bitMap[i];
            uint type = typeMap[x + i * 32];
            uint row = data[i] & (uint)(front); // >> 1);

            while (row != 0)
            {
                int trailingZeros = Bit.TrailingZeros(row);

                Block block = blocks[x + trailingZeros * 32 + i * 1024];
                if (block.BlockId() == 0)
                    continue;

                uint newRow = row >> trailingZeros;
                uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                int h = Bit.TrailingZeros((~newRow) | newType);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;
                type &= ~mask;

                int w = 1;
                
                while (w < 32 - i)
                {
                    int iw = i + w;

                    ulong sFront = ~bitMap[iw];
                    uint sType = typeMap[x + iw * 32];
                    uint sRow = data[iw] & (uint)(sFront); // >> 1);

                    sRow &= ~(sType & Bit.Invert(trailingZeros));

                    if ((sRow & mask) != mask || block != blocks[x + trailingZeros * 32 + iw * 1024])
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                //Console.WriteLine("KJHFEGKGSFBKHGFKSHEF    " + w + " " + h);

                var definition = block.Definition();
                vertexData.Add(new(3, x | (i << 5) | (trailingZeros << 10), 0, (w << 5) | (h << 10)));
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
                
                int ao = VH.GetAO(front1, front2, front3, trailingZeros);
                Block block = blocks[trailingZeros + i * 32 + y * 1024];
                if (block.BlockId() == 0)
                    continue;

                uint newRow = row >> trailingZeros;
                uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                int h = Bit.TrailingZeros((~newRow) | newType);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;
                type &= ~mask;

                int w = 1;
                
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
                vertexData.Add(new(2, trailingZeros | (y << 5) | (i << 10), packedAo, h | (w << 10)));
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

                int ao = VH.GetFlippedAO(front1, front2, front3, trailingZeros);
                Block block = blocks[trailingZeros + i * 32 + y * 1024];
                if (block.BlockId() == 0)
                    continue;

                uint newRow = row >> trailingZeros;
                uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                int h = Bit.TrailingZeros((~newRow) | newType);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;
                type &= ~mask;

                int w = 1;
                
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
                vertexData.Add(new(4, trailingZeros | (y << 5) | (i << 10), packedAo, h | (w << 10)));
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