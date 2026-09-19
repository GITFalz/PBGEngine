using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using PBG.MathLibrary;


namespace PBG.NewVoxel;

using VH = VoxelHelper;
using MH = MesherHelper;

public unsafe static class IndirectMesher3
{
    const int MAX_X = 31;
    const int MAX_Z = 992;
    const int MAX_Y = 31744;

    public readonly static V256u StateMaskV256 = V256U.New(Block.STATE_MASK);
    public readonly static V256u SolidMaskV256 = V256U.New(1 << Block.STATE_SHIFT);

    public static void GetVector256BitMap(VoxelChunk chunk, Block* blocks, List<Vector4i> vertexData, ref NeighbourChunks neighbours, int workerId)
    {
        /*
        uint* ptr10 = (uint*)neighbours.Blocks10 + indexNz;
            uint* ptr16 = (uint*)neighbours.Blocks16 + indexPz;
            uint* ptr4  = (uint*)neighbours.Blocks4  + indexNy;
            uint* ptr22 = (uint*)neighbours.Blocks22 + indexPy;
        */

        VoxelChunk chunk10 = chunk.Renderer.GetChunk(chunk.RelativePosition + ( 0,  0, -1)) ?? VoxelChunk.Empty;
        VoxelChunk chunk16 = chunk.Renderer.GetChunk(chunk.RelativePosition + ( 0,  0,  1)) ?? VoxelChunk.Empty;
        VoxelChunk chunk4  = chunk.Renderer.GetChunk(chunk.RelativePosition + ( 0, -1,  0)) ?? VoxelChunk.Empty;
        VoxelChunk chunk22 = chunk.Renderer.GetChunk(chunk.RelativePosition + ( 0,  1,  0)) ?? VoxelChunk.Empty;


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
            ulong rowNz = (ulong)chunk10.BackOcclusion[y] << 1; //GetSolidRow(ptr10) << 1;
            
            // handle chunk that is in +z at index 16 and i need the first block
            ulong rowPz = (ulong)chunk16.FrontOcclusion[y] << 1; //GetSolidRow(ptr16) << 1;

            // handle chunk that is in -y at index 4 and i need the last block
            ulong rowNy = (ulong)chunk4.TopOcclusion[y] << 1; //GetSolidRow(ptr4) << 1;

            // handle chunk that is in +y at index 22 and i need the first block
            ulong rowPy = (ulong)chunk22.BottomOcclusion[y] << 1; //GetSolidRow(ptr22) << 1;

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

    public static bool GenerateMesh(VoxelChunk chunk, List<Vector4i> vertexData, Vector3i worldPosition, int workerId, out int vertexCount)
    {
        vertexCount = 0;
        int vertCount = 0;

        Block* blocks = chunk.Blocks;

        //SlicePopulationTest(blocks, workerID);

        var renderer = chunk.Renderer;

        NeighbourChunks neighbours = new(renderer, chunk.RelativePosition);

        GetVector256BitMap(chunk, blocks, vertexData, ref neighbours, workerId);

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

        for (int z = 0; z < 32; z++)
        {
            for (int y = 0; y < 32; y++)
            {
                ulong bitRow1 = bitMap[(z    ) + (y    ) * 34];
                ulong bitRow2 = bitMap[(z + 1) + (y    ) * 34]; // bottom
                ulong bitRow3 = bitMap[(z + 2) + (y    ) * 34];

                ulong bitRow4 = bitMap[(z    ) + (y + 1) * 34]; // front
                ulong bitRow5 = bitMap[(z + 1) + (y + 1) * 34]; // center
                ulong bitRow6 = bitMap[(z + 2) + (y + 1) * 34]; // back

                ulong bitRow7 = bitMap[(z    ) + (y + 2) * 34];
                ulong bitRow8 = bitMap[(z + 1) + (y + 2) * 34]; // top
                ulong bitRow9 = bitMap[(z + 2) + (y + 2) * 34];

                for (int x = 0; x < 32; x++)
                {
                    if (chunk.Status == ChunkStatus.Canceled)
                    {
                        vertexCount = vertCount;
                        return false;
                    }

                    int dx = x;
                    int dz = z * 32;
                    int dy = y * 1024;
                    
                    var block = blocks[dx + dz + dy];

                    if (block.IsAir())
                        continue;

                    ulong mask = 7u << x;

                    /*
                    uint aoMap =
                    (uint)((bitRow1 >> x) & 7u)       |
                    (uint)((bitRow2 >> x) & 7u) <<  3 |
                    (uint)((bitRow3 >> x) & 7u) <<  6 |

                    (uint)((bitRow4 >> x) & 7u) <<  9 |
                    (uint)((bitRow5 >> x) & 7u) << 12 |
                    (uint)((bitRow6 >> x) & 7u) << 15 |

                    (uint)((bitRow7 >> x) & 7u) << 18 |
                    (uint)((bitRow8 >> x) & 7u) << 21 |
                    (uint)((bitRow9 >> x) & 7u) << 24;
                    */

                    int pos = x | (y << 5) | (z << 10);

                    // front
                    if (((bitRow4 >> (x + 1)) & 1) == 0)
                    {
                        var geometryIndex = block.GetSolidGeometryIndex(0);
                        vertexData.Add(new(geometryIndex, pos, 0, 0));
                    }

                    // right
                    if (((bitRow5 >> (x + 2)) & 1) == 0)
                    {
                        var geometryIndex = block.GetSolidGeometryIndex(1);
                        vertexData.Add(new(geometryIndex, pos, 0, 0));
                    }

                    // top
                    if (((bitRow8 >> (x + 1)) & 1) == 0)
                    {
                        var geometryIndex = block.GetSolidGeometryIndex(2);
                        vertexData.Add(new(geometryIndex, pos, 0, 0));
                    }

                    // left
                    if (((bitRow5 >> x) & 1) == 0)
                    {
                        var geometryIndex = block.GetSolidGeometryIndex(3);
                        vertexData.Add(new(geometryIndex, pos, 0, 0));
                    }
                    
                    // bottom
                    if (((bitRow2 >> (x + 1)) & 1) == 0)
                    {
                        var geometryIndex = block.GetSolidGeometryIndex(4);
                        vertexData.Add(new(geometryIndex, pos, 0, 0));
                    }

                    // back
                    if (((bitRow6 >> (x + 1)) & 1) == 0)
                    {
                        var geometryIndex = block.GetSolidGeometryIndex(5);
                        vertexData.Add(new(geometryIndex, pos, 0, 0));
                    }
                }
            }
        }

        vertexCount = vertCount;

        return true;
    }

    private static void HandleFrontFaceAO2(List<Vector4i> vertexData, Block* blocks, Block* sideBlocks, uint sideBlockMask, int pos, int lx, int ly, int lz, NewBlockFaces newBlockFaces, ref int vertexCount)
    {
        //Block sideBlock = lz == 0 ? sideChunks[10][lx, ly, 31] : chunk[lx, ly, lz - 1];
        Block sideBlock = lz == 0 ? sideBlocks[lx + ly + 992] : blocks[lx + ly + (lz - 32)];

        if (!sideBlock.IsAir())
        {
            var sDef = sideBlock.Definition();
            if (newBlockFaces.IsOccluded(sDef.NewBlockFaces[0], 0, 5))
                return;
        }
        
        var faces = newBlockFaces.GetFaces(0);

        const uint MASK_1a = (1u << 0)  | (1u << 1)  | (1u << 9);
        const uint MASK_1b =              (1u << 1)  | (1u << 9);
        int a = MH.GetAO(sideBlockMask, MASK_1a, MASK_1b);   // 0, 1, 9

        const uint MASK_2a = (1u << 18) | (1u << 9)  | (1u << 19);
        const uint MASK_2b =              (1u << 9)  | (1u << 19);
        int b = MH.GetAO(sideBlockMask, MASK_2a, MASK_2b);   // 18, 9, 19

        const uint MASK_3a = (1u << 20) | (1u << 19) | (1u << 11);
        const uint MASK_3b =              (1u << 19) | (1u << 11);
        int c = MH.GetAO(sideBlockMask, MASK_3a, MASK_3b);   // 20, 19, 11

        const uint MASK_4a = (1u << 2)  | (1u << 11) | (1u << 1);
        const uint MASK_4b =              (1u << 11) | (1u << 1);
        int d = MH.GetAO(sideBlockMask, MASK_4a, MASK_4b);   // 2, 11, 1
        
        /*
        int a = GetAO(sideBlockMask, 0, 1, 9);
        int b = GetAO(sideBlockMask, 18, 9, 19);
        int c = GetAO(sideBlockMask, 20, 19, 11);
        int d = GetAO(sideBlockMask, 2, 11, 1);
        */
        
        byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

        for (int i = 0; i < faces.Length; i++)
        {
            var face = faces[i];
            vertexData.Add(new(face.GeometryIndex, pos, r, 0));
            vertexCount++;
        }
    }

    private static void HandleRightFaceAO2(List<Vector4i> vertexData, Block* blocks, Block* sideBlocks, uint sideBlockMask, int pos, int lx, int ly, int lz, NewBlockFaces newBlockFaces, ref int vertexCount)
    {
        //Block sideBlock = lx == 31 ? sideChunks[14][0, ly, lz] : chunk[lx + 1, ly, lz];
        Block sideBlock = lx == MAX_X ? sideBlocks[ly + lz] : blocks[lx + 1 + ly + lz];

        if (!sideBlock.IsAir())
        {
            var sDef = sideBlock.Definition();
            if (newBlockFaces.IsOccluded(sDef.NewBlockFaces[0], 1, 3))
                return;
        }
        
        var faces = newBlockFaces.GetFaces(1);

        const uint MASK_1a = (1u << 2)  | (1u << 5)  | (1u << 11);
        const uint MASK_1b =              (1u << 5)  | (1u << 11);
        int a = MH.GetAO(sideBlockMask, MASK_1a, MASK_1b);   // 2, 5, 11

        const uint MASK_2a = (1u << 20) | (1u << 11) | (1u << 23);
        const uint MASK_2b =              (1u << 11) | (1u << 23);
        int b = MH.GetAO(sideBlockMask, MASK_2a, MASK_2b);   // 20, 11, 23

        const uint MASK_3a = (1u << 26) | (1u << 23) | (1u << 17);
        const uint MASK_3b =              (1u << 23) | (1u << 17);
        int c = MH.GetAO(sideBlockMask, MASK_3a, MASK_3b);   // 26, 23, 17

        const uint MASK_4a = (1u << 8)  | (1u << 17) | (1u << 5);
        const uint MASK_4b =              (1u << 17) | (1u << 5);
        int d = MH.GetAO(sideBlockMask, MASK_4a, MASK_4b);   // 8, 17, 5
        
        /*
        int a = GetAO(sideBlockMask, 2, 5, 11);
        int b = GetAO(sideBlockMask, 20, 11, 23);
        int c = GetAO(sideBlockMask, 26, 23, 17);
        int d = GetAO(sideBlockMask, 8, 17, 5);
        */
        
        byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

        for (int i = 0; i < faces.Length; i++)
        {
            var face = faces[i];
            vertexData.Add(new(face.GeometryIndex, pos, r, 0));
            vertexCount++;
        }
    }

    private static void HandleTopFaceAO2(List<Vector4i> vertexData, Block* blocks, Block* sideBlocks, uint sideBlockMask, int pos, int lx, int ly, int lz, NewBlockFaces newBlockFaces, ref int vertexCount)
    {
        //Block sideBlock = ly == 31 ? sideChunks[22][lx, 0, lz] : chunk[lx, ly + 1, lz];
        Block sideBlock = ly == MAX_Y ? sideBlocks[lx + lz] : blocks[lx + ly + 1024 + lz];
        
        if (!sideBlock.IsAir())
        {
            BlockDefinition sDef = sideBlock.Definition();
            if (newBlockFaces.IsOccluded(sDef.NewBlockFaces[0], 2, 4))
                return;
        }
        
        var faces = newBlockFaces.GetFaces(2);

        const uint MASK_1a = (1u << 18) | (1u << 19) | (1u << 21);
        const uint MASK_1b =              (1u << 19) | (1u << 21);
        int a = MH.GetAO(sideBlockMask, MASK_1a, MASK_1b);   // 18, 19, 21

        const uint MASK_2a = (1u << 24) | (1u << 21) | (1u << 25);
        const uint MASK_2b =              (1u << 21) | (1u << 25);
        int b = MH.GetAO(sideBlockMask, MASK_2a, MASK_2b);   // 24, 21, 25

        const uint MASK_3a = (1u << 26) | (1u << 25) | (1u << 23);
        const uint MASK_3b =              (1u << 25) | (1u << 23);
        int c = MH.GetAO(sideBlockMask, MASK_3a, MASK_3b);   // 26, 25, 23

        const uint MASK_4a = (1u << 20) | (1u << 23) | (1u << 19);
        const uint MASK_4b =              (1u << 23) | (1u << 19);
        int d = MH.GetAO(sideBlockMask, MASK_4a, MASK_4b);   // 20, 23, 19
        
        /*
        int a = GetAO(sideBlockMask, 18, 19, 21);
        int b = GetAO(sideBlockMask, 24, 21, 25);
        int c = GetAO(sideBlockMask, 26, 25, 23);
        int d = GetAO(sideBlockMask, 20, 23, 19);
        */
        
        byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

        for (int i = 0; i < faces.Length; i++)
        {
            var face = faces[i];
            vertexData.Add(new(face.GeometryIndex, pos, r, 0));
            vertexCount++;
        }
    }

    private static void HandleLeftFaceAO2(List<Vector4i> vertexData, Block* blocks, Block* sideBlocks, uint sideBlockMask, int pos, int lx, int ly, int lz, NewBlockFaces newBlockFaces, ref int vertexCount)
    {
        //Block sideBlock = lx == 0 ? sideChunks[12][31, ly, lz] : chunk[lx - 1, ly, lz];
        Block sideBlock = lx == 0 ? sideBlocks[31 + ly + lz] : blocks[lx - 1 + ly + lz];
        
        if (!sideBlock.IsAir())
        {
            var sDef = sideBlock.Definition();
            if (newBlockFaces.IsOccluded(sDef.NewBlockFaces[0], 3, 1))
                return;
        }
        
        var faces = newBlockFaces.GetFaces(3);

        const uint MASK_1a = (1u << 6)  | (1u << 3)  | (1u << 15);
        const uint MASK_1b =              (1u << 3)  | (1u << 15);
        int a = MH.GetAO(sideBlockMask, MASK_1a, MASK_1b);   // 6, 3, 15

        const uint MASK_2a = (1u << 24) | (1u << 15) | (1u << 21);
        const uint MASK_2b =              (1u << 15) | (1u << 21);
        int b = MH.GetAO(sideBlockMask, MASK_2a, MASK_2b);   // 24, 15, 21

        const uint MASK_3a = (1u << 18) | (1u << 21) | (1u << 9);
        const uint MASK_3b =              (1u << 21) | (1u << 9);
        int c = MH.GetAO(sideBlockMask, MASK_3a, MASK_3b);   // 18, 21, 9

        const uint MASK_4a = (1u << 0)  | (1u << 9)  | (1u << 3);
        const uint MASK_4b =              (1u << 9)  | (1u << 3);
        int d = MH.GetAO(sideBlockMask, MASK_4a, MASK_4b);   // 0, 9, 3
        
        /*
        int a = GetAO(sideBlockMask, 6, 3, 15);
        int b = GetAO(sideBlockMask, 24, 15, 21);
        int c = GetAO(sideBlockMask, 18, 21, 9);
        int d = GetAO(sideBlockMask, 0, 9, 3);
        */
        
        byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

        for (int i = 0; i < faces.Length; i++)
        {
            var face = faces[i];
            vertexData.Add(new(face.GeometryIndex, pos, r, 0));
            vertexCount++;
        }
    }

    private static void HandleBottomFaceAO2(List<Vector4i> vertexData, Block* blocks, Block* sideBlocks, uint sideBlockMask, int pos, int lx, int ly, int lz, NewBlockFaces newBlockFaces, ref int vertexCount)
    {
        //Block sideBlock = ly == 0 ? sideChunks[4][lx, 31, lz] : chunk[lx, ly - 1, lz];
        Block sideBlock = ly == 0 ? sideBlocks[lx + 31744 + lz] : blocks[lx + (ly - 1024) + lz];
        
        if (!sideBlock.IsAir())
        {
            BlockDefinition sDef = sideBlock.Definition();
            if (newBlockFaces.IsOccluded(sDef.NewBlockFaces[0], 4, 2))
                return;
        }
        
        var faces = newBlockFaces.GetFaces(4);

        const uint MASK_1a = (1u << 2)  | (1u << 1)  | (1u << 5);
        const uint MASK_1b =              (1u << 1)  | (1u << 5);
        int a = MH.GetAO(sideBlockMask, MASK_1a, MASK_1b);   // 2, 1, 5

        const uint MASK_2a = (1u << 8)  | (1u << 5)  | (1u << 7);
        const uint MASK_2b =              (1u << 5)  | (1u << 7);
        int b = MH.GetAO(sideBlockMask, MASK_2a, MASK_2b);   // 8, 5, 7

        const uint MASK_3a = (1u << 6)  | (1u << 7)  | (1u << 3);
        const uint MASK_3b =              (1u << 7)  | (1u << 3);
        int c = MH.GetAO(sideBlockMask, MASK_3a, MASK_3b);   // 6, 7, 3

        const uint MASK_4a = (1u << 0)  | (1u << 3)  | (1u << 1);
        const uint MASK_4b =              (1u << 3)  | (1u << 1);
        int d = MH.GetAO(sideBlockMask, MASK_4a, MASK_4b);   // 0, 3, 1
        
        /*
        int a = GetAO(sideBlockMask, 2, 1, 5);
        int b = GetAO(sideBlockMask, 8, 5, 7);
        int c = GetAO(sideBlockMask, 6, 7, 3);
        int d = GetAO(sideBlockMask, 0, 3, 1);
        */
        
        byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

        for (int i = 0; i < faces.Length; i++)
        {
            var face = faces[i];
            vertexData.Add(new(face.GeometryIndex, pos, r, 0));
            vertexCount++;
        }
    }

    private static void HandleBackFaceAO2(List<Vector4i> vertexData, Block* blocks, Block* sideBlocks, uint sideBlockMask, int pos, int lx, int ly, int lz, NewBlockFaces newBlockFaces, ref int vertexCount)
    {
        //Block sideBlock = lz == 31 ? sideChunks[16][lx, ly, 0] : chunk[lx, ly, lz + 1];
        Block sideBlock = lz == MAX_Z ? sideBlocks[lx + ly] : blocks[lx + ly + lz + 32];
        
        if (!sideBlock.IsAir())
        {
            var sDef = sideBlock.Definition();
            if (newBlockFaces.IsOccluded(sDef.NewBlockFaces[0], 5, 0))
                return;
        }
        
        var faces = newBlockFaces.GetFaces(5);

        const uint MASK_1a = (1u << 8)  | (1u << 7)  | (1u << 17);
        const uint MASK_1b =              (1u << 7)  | (1u << 17);
        int a = MH.GetAO(sideBlockMask, MASK_1a, MASK_1b);   // 8, 7, 17

        const uint MASK_2a = (1u << 26) | (1u << 17) | (1u << 25);
        const uint MASK_2b =              (1u << 17) | (1u << 25);
        int b = MH.GetAO(sideBlockMask, MASK_2a, MASK_2b);   // 26, 17, 25

        const uint MASK_3a = (1u << 24) | (1u << 25) | (1u << 15);
        const uint MASK_3b =              (1u << 25) | (1u << 15);
        int c = MH.GetAO(sideBlockMask, MASK_3a, MASK_3b);   // 24, 25, 15

        const uint MASK_4a = (1u << 6)  | (1u << 15) | (1u << 7);
        const uint MASK_4b =              (1u << 15) | (1u << 7);
        int d = MH.GetAO(sideBlockMask, MASK_4a, MASK_4b);   // 6, 15, 7

        /*
        int a = GetAO(sideBlockMask, 8, 7, 17);
        int b = GetAO(sideBlockMask, 26, 17, 25);
        int c = GetAO(sideBlockMask, 24, 25, 15);
        int d = GetAO(sideBlockMask, 6, 15, 7);
        */
        
        byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

        for (int i = 0; i < faces.Length; i++)
        {
            var face = faces[i];
            vertexData.Add(new(face.GeometryIndex, pos, r, 0));
            vertexCount++;
        }
    }


    public static void GetVector256BitMapX64(Block* blocks, ref NeighbourChunks neighbours, int workerId)
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

                row |= VH.GetV256uRowX64(ptr, 0) | VH.GetV256uRowX64(ptr, 2);

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
    }
}