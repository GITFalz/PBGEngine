using System.Runtime.CompilerServices;

namespace PBG.NewVoxel;

using VH = VoxelHelper;

public unsafe static class MesherHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetAO(uint sideBlockMask, uint maskF, uint mask6)
    {
        uint masked = sideBlockMask & maskF;
        return masked == mask6 ? 3 : System.Numerics.BitOperations.PopCount(masked);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetAO(uint sideBlockMask, int a1, int b1, int c1)
    {
        return GetAOValue(((sideBlockMask >> a1) & 1) | (((sideBlockMask >> b1) & 1) << 1) | (((sideBlockMask >> c1) & 1) << 2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetAOValue(uint mask) => mask switch
    {
        0 => 0, 1 => 1, 2 => 1, 3 => 2,
        4 => 1, 5 => 2, 6 => 3, 7 => 3,
        _ => 0
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ChunkDir(int v) => v < 0 ? -1 : v >= 32 ? 1 : 0;




    #region Old functions
    public static ulong[] GetScalarBitMap(Block* blocks, ref NeighbourChunks neighbours)
    {
        ulong[] bitMap = new ulong[34 * 34];

        int maxYlayerIndex = 31744;
        
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

            for (int z = 0; z < 32; z++)
            {
                int blockIndex = z * 32 + yIndex;
                int mapIndex = (z + 1) + (y + 1) * 34;

                ulong row = 0;

                // handle chunk that is in -x at index 12 and i need the last block
                if (!neighbours.Blocks12[blockIndex+31].IsAir()) 
                    row |= 1UL;

                // handle chunk that is in +x at index 14 and i need the first block
                if (!neighbours.Blocks14[blockIndex].IsAir()) 
                    row |= 0x200000000UL; // binary 1 with 33 zeros behind

                for (int x = 0; x < 32; x++)
                {
                    if (!blocks[blockIndex + x].IsAir())
                        row |= 2UL << x; // same as 1UL << (x + 1)
                }

                bitMap[mapIndex] = row;

                // handle z and y axis sides

                // handle chunk that is in -z at index 10 and i need the last block
                if (!neighbours.Blocks10[indexNz + z].IsAir()) // simulate x progression using z
                    rowNz |= 2UL << z;

                // handle chunk that is in +z at index 16 and i need the first block
                if (!neighbours.Blocks16[indexPz + z].IsAir()) // simulate x progression using z
                    rowPz |= 2UL << z;


                // handle chunk that is in -y at index 4 and i need the last block
                if (!neighbours.Blocks4[indexNy + z].IsAir()) // simulate x progression using z
                    rowNy |= 2UL << z;

                // handle chunk that is in +y at index 22 and i need the first block
                if (!neighbours.Blocks22[indexPy + z].IsAir()) // simulate x progression using z
                    rowPy |= 2UL << z;
            }

            int rowZindex = (y + 1) * 34;

            bitMap[rowZindex] = rowNz;
            bitMap[33 + rowZindex] = rowPz;

            bitMap[y + 1] = rowNy; // on the z axis of the bit map (y simulates z)
            bitMap[1123 + y] = rowPy; // on the z axis of the bit map but at the last row (y simulates z) (1123 is 33 x 34 = 1122 and +1 like the line before (y + 1))
        }

        return bitMap;
    }


    public static ulong[] GetVector256BitMap(Block* blocks, ref NeighbourChunks neighbours, int workerId)
    {
        ulong[] bitMap = VoxelChunkGenerator.BitMaps[workerId];

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

                row |= VH.GetV256Row(ptr, 0) | VH.GetV256Row(ptr, 1) | VH.GetV256Row(ptr, 2) | VH.GetV256Row(ptr, 3);

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
            rowNz |= VH.GetV256Row(ptr10, 0) | VH.GetV256Row(ptr10, 1) | VH.GetV256Row(ptr10, 2) | VH.GetV256Row(ptr10, 3);
            
            // handle chunk that is in +z at index 16 and i need the first block
            rowPz |= VH.GetV256Row(ptr16, 0) | VH.GetV256Row(ptr16, 1) | VH.GetV256Row(ptr16, 2) | VH.GetV256Row(ptr16, 3);

            // handle chunk that is in -y at index 4 and i need the last block
            rowNy |= VH.GetV256Row(ptr4,  0) | VH.GetV256Row(ptr4,  1) | VH.GetV256Row(ptr4,  2) | VH.GetV256Row(ptr4,  3);

            // handle chunk that is in +y at index 22 and i need the first block
            rowPy |= VH.GetV256Row(ptr22, 0) | VH.GetV256Row(ptr22, 1) | VH.GetV256Row(ptr22, 2) | VH.GetV256Row(ptr22, 3);

            int rowZindex = (y + 1) * 34;

            bitMap[rowZindex] = rowNz << 1;
            bitMap[33 + rowZindex] = rowPz << 1;

            bitMap[y + 1] = rowNy << 1; // on the z axis of the bit map (y simulates z)
            bitMap[1123 + y] = rowPy << 1; // on the z axis of the bit map but at the last row (y simulates z) (1123 is 33 x 34 = 1122 and +1 like the line before (y + 1))
        }

        return bitMap;
    }

    #endregion
}