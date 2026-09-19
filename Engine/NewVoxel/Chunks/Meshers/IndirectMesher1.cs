using PBG.MathLibrary;


namespace PBG.NewVoxel;

using MH = MesherHelper;

public unsafe static class IndirectMesher1
{
    public static bool GenerateMesh(VoxelChunk chunk, List<Vector4i> vertexData, Vector3i worldPosition, VoxelChunk blocks, out int vertexCount)
    {
        vertexCount = 0;
        int vertCount = 0;
        //uint[] bitMap = new uint[32*32*32];

        //Stopwatch sw = Stopwatch.StartNew();

        var renderer = chunk.Renderer;

        VoxelChunk[] sideChunks = new VoxelChunk[27];
        for (int i = 0; i < 27; i++)
        {
            sideChunks[i] = renderer.GetChunk(chunk.RelativePosition + _neighbourOffsets[i], out var c) ? c : VoxelChunk.Empty;
        }

        int count = 0;

        int ti = 0;
        uint sideBlockMask = 0;

        for (int y = -1; y <= 1; y++)
        for (int z = -1; z <= 1; z++)
        for (int x = -1; x <= 1; x++)
        { 
            Block sideBlock;
            if ((uint)x < 32 && (uint)y < 32 && (uint)z < 32)
            {
                sideBlock = blocks[x, y, z];
            }
            else
            {
                int cx = x < 0 ? -1 : 0;
                int cy = y < 0 ? -1 : 0;
                int cz = z < 0 ? -1 : 0;
                int chunkIdx = (cy + 1) * 9 + (cz + 1) * 3 + (cx + 1);

                sideBlock = sideChunks[chunkIdx][x & 31, y & 31, z & 31];
            }

            uint aoBit = sideBlock.IsAir() ? 0u : 1u;
            sideBlockMask |= aoBit << ti;
            ti++;
        }

        uint sideBlockXMask = sideBlockMask;

        for (int x = 0; x < 32; x++)
        {
            if (x > 0)
            {
                sideBlockXMask &= 0x6DB6DB6; //00000110110110110110110110110110 ( y mask )
                sideBlockXMask >>= 1;

                int tx = (x + 1) & 31;
                int cx = x < 32 ? 1 : 2;

                uint xside = 0;

                xside |= sideChunks[cx][tx, 31, 31].IsAir() ? 0 : 0b100u;
                xside |= sideChunks[cx+3][tx, 31, 0].IsAir() ? 0 : 0b100000u;
                xside |= sideChunks[cx+3][tx, 31, 1].IsAir() ? 0 : 0b100000000u;

                xside |= sideChunks[cx+9][tx, 0, 31].IsAir() ? 0 : 0b100000000000u;
                xside |= sideChunks[cx+12][tx, 0, 0].IsAir() ? 0 : 0b100000000000000u;
                xside |= sideChunks[cx+12][tx, 0, 1].IsAir() ? 0 : 0b100000000000000000u;

                xside |= sideChunks[cx+9][tx, 1, 31].IsAir() ? 0 : 0b100000000000000000000u;
                xside |= sideChunks[cx+12][tx, 1, 0].IsAir() ? 0 : 0b100000000000000000000000u;
                xside |= sideChunks[cx+12][tx, 1, 1].IsAir() ? 0 : 0b100000000000000000000000000u;

                sideBlockXMask |= xside;
            }

            uint sideBlockYMask = sideBlockXMask;

            for (int y = 0; y < 32; y++)
            {
                if (y > 0)
                {
                    sideBlockYMask &= 0x7FFFE00; //00000111111111111111111000000000 ( y mask )
                    sideBlockYMask >>= 9;

                    int ty = (y + 1) & 31;

                    int cxa = MH.ChunkDir(x - 1) + 1;
                    int cxb = MH.ChunkDir(x)     + 1;
                    int cxc = MH.ChunkDir(x + 1) + 1;

                    int cy = y < 31 ? 9 : 18;

                    uint yside = 0;

                    yside |= sideChunks[cxa + cy + 0][(x-1) & 31, ty, 31].IsAir() ? 0 : 0b1u;
                    yside |= sideChunks[cxb + cy + 0][ x,         ty, 31].IsAir() ? 0 : 0b10u;
                    yside |= sideChunks[cxc + cy + 0][(x+1) & 31, ty, 31].IsAir() ? 0 : 0b100u;

                    yside |= sideChunks[cxa + cy + 3][(x-1) & 31, ty,  0].IsAir() ? 0 : 0b1000u;
                    yside |= sideChunks[cxb + cy + 3][ x,         ty,  0].IsAir() ? 0 : 0b10000u;
                    yside |= sideChunks[cxc + cy + 3][(x+1) & 31, ty,  0].IsAir() ? 0 : 0b100000u;

                    yside |= sideChunks[cxa + cy + 3][(x-1) & 31, ty,  1].IsAir() ? 0 : 0b1000000u;
                    yside |= sideChunks[cxb + cy + 3][ x,         ty,  1].IsAir() ? 0 : 0b10000000u;
                    yside |= sideChunks[cxc + cy + 3][(x+1) & 31, ty,  1].IsAir() ? 0 : 0b100000000u;

                    sideBlockYMask |= yside << 18;
                }

                uint sideBlockMaskMem = sideBlockYMask;

                for (int z = 0; z < 32; z++)
                {
                    if (chunk.Status == ChunkStatus.Canceled)
                    {
                        vertexCount = vertCount;
                        return false;
                    }

                    var block = blocks[x, y, z];
                    
                    if (z > 0)
                    {
                        sideBlockMaskMem &= 0x7E3F1F8; //00000111111000111111000111111000 ( z mask )
                        sideBlockMaskMem >>= 3;

                        bool interior = x > 0 && x < 31 && y > 0 && y < 31 && z < 31;

                        if (interior)
                        {
                            sideBlockMaskMem |= blocks[x - 1, y - 1, z + 1].IsAir() ? 0u : 0b1000000u;
                            sideBlockMaskMem |= blocks[x,     y - 1, z + 1].IsAir() ? 0u : 0b10000000u;
                            sideBlockMaskMem |= blocks[x + 1, y - 1, z + 1].IsAir() ? 0u : 0b100000000u;
                            
                            sideBlockMaskMem |= blocks[x - 1, y    , z + 1].IsAir() ? 0u : 0b1000000000000000u;
                            sideBlockMaskMem |= blocks[x,     y    , z + 1].IsAir() ? 0u : 0b10000000000000000u;
                            sideBlockMaskMem |= blocks[x + 1, y    , z + 1].IsAir() ? 0u : 0b100000000000000000u;

                            sideBlockMaskMem |= blocks[x - 1, y + 1, z + 1].IsAir() ? 0u : 0b1000000000000000000000000u;
                            sideBlockMaskMem |= blocks[x,     y + 1, z + 1].IsAir() ? 0u : 0b10000000000000000000000000u;
                            sideBlockMaskMem |= blocks[x + 1, y + 1, z + 1].IsAir() ? 0u : 0b100000000000000000000000000u;
                        }
                        else
                        {
                            int tx1 = x == 0 ? 0 : 1;
                            int tx2 = x < 31 ? 1 : 2;

                            int ty1 = y == 0 ? 0 : 9;
                            int ty2 = y < 31 ? 9 :18;

                            int tz = z == 31 ? 6 : 3;

                            // better for next step i guess, no negative number
                            int mx = x + 32;
                            int my = y + 32;
                            int mz = z + 32;

                            int nx1 = (mx - 1) & 31;
                            int nx2 = (mx + 1) & 31;

                            int ny1 = (my - 1) & 31;
                            int ny2 = (my + 1) & 31;

                            int nz = (mz + 1) & 31;

                            

                            sideBlockMaskMem |= sideChunks[tx1 + ty1 + tz][nx1, ny1, nz].IsAir() ? 0 : 0b1000000u;
                            sideBlockMaskMem |= sideChunks[  1 + ty1 + tz][ x , ny1, nz].IsAir() ? 0 : 0b10000000u;
                            sideBlockMaskMem |= sideChunks[tx2 + ty1 + tz][nx2, ny1, nz].IsAir() ? 0 : 0b100000000u;

                            sideBlockMaskMem |= sideChunks[tx1 +   9 + tz][nx1,  y , nz].IsAir() ? 0 : 0b1000000000000000u;
                            sideBlockMaskMem |= sideChunks[  1 +   9 + tz][ x ,  y , nz].IsAir() ? 0 : 0b10000000000000000u;
                            sideBlockMaskMem |= sideChunks[tx2 +   9 + tz][nx2,  y , nz].IsAir() ? 0 : 0b100000000000000000u;

                            sideBlockMaskMem |= sideChunks[tx1 + ty2 + tz][nx1, ny2, nz].IsAir() ? 0 : 0b1000000000000000000000000u;
                            sideBlockMaskMem |= sideChunks[  1 + ty2 + tz][ x , ny2, nz].IsAir() ? 0 : 0b10000000000000000000000000u;
                            sideBlockMaskMem |= sideChunks[tx2 + ty2 + tz][nx2, ny2, nz].IsAir() ? 0 : 0b100000000000000000000000000u;
                        }  
                    }

                    if (block.IsAir())
                        continue;

                    count++;

                    var definition = block.Definition();
                    int pos = x | (y << 5) | (z << 10);

                    var newBlockFaces = definition.NewBlockFaces[0];

                    HandleFrontFaceAO(vertexData, chunk, definition, sideChunks, sideBlockMaskMem, pos, x, y, z, newBlockFaces, ref vertCount);
                    HandleRightFaceAO(vertexData, chunk, definition, sideChunks, sideBlockMaskMem, pos, x, y, z, newBlockFaces, ref vertCount);
                    HandleTopFaceAO(vertexData, chunk, definition, sideChunks, sideBlockMaskMem, pos, x, y, z, newBlockFaces, ref vertCount);
                    HandleLeftFaceAO(vertexData, chunk, definition, sideChunks, sideBlockMaskMem, pos, x, y, z, newBlockFaces, ref vertCount);
                    HandleBottomFaceAO(vertexData, chunk, definition, sideChunks, sideBlockMaskMem, pos, x, y, z, newBlockFaces, ref vertCount);
                    HandleBackFaceAO(vertexData, chunk, definition, sideChunks, sideBlockMaskMem, pos, x, y, z, newBlockFaces, ref vertCount);

                    var iFaces = definition.NewBlockFaces[0].InternalFaces;
                    for (int i = 0; i < iFaces.Length; i++)
                    {
                        var face = iFaces[i];
                        vertexData.Add(new(face.GeometryIndex, pos, 0, 0));
                        vertCount++;
                    }
                }
            }
        }

        vertexCount = vertCount;


        //Console.WriteLine($"Setup:          {setupTime / (double)Stopwatch.Frequency:F6}s");
        //Console.WriteLine($"Neighbour AO:   {neighbourTime / (double)Stopwatch.Frequency:F6}s");
        //Console.WriteLine($"Face gen:       {faceTime / (double)Stopwatch.Frequency:F6}s");
        //Console.WriteLine($"Internal faces: {internalFaceTime / (double)Stopwatch.Frequency:F6}s");
        /*
        Console.WriteLine($"Count: {count}, Total:          {sw.Elapsed.TotalSeconds:F6}s");

        VoxelRenderer.DebugAOMasks[chunk.RelativePosition] = bitMap;
        */

        return true;
    }

    private static void HandleFrontFaceAO(List<Vector4i> vertexData, VoxelChunk chunk, BlockDefinition definition, VoxelChunk[] sideChunks, uint sideBlockMask, int pos, int lx, int ly, int lz, NewBlockFaces newBlockFaces, ref int vertexCount)
    {
        Block sideBlock = lz == 0 ? sideChunks[10][lx, ly, 31] : chunk[lx, ly, lz - 1];

        if (!sideBlock.IsAir())
        {
            var sDef = sideBlock.Definition();
            if (newBlockFaces.IsOccluded(sDef.NewBlockFaces[0], 0, 5))
                return;
        }
        
        var faces = newBlockFaces.GetFaces(0);

        int a = MH.GetAO(sideBlockMask, 0, 1, 9);
        int b = MH.GetAO(sideBlockMask, 18, 9, 19);
        int c = MH.GetAO(sideBlockMask, 20, 19, 11);
        int d = MH.GetAO(sideBlockMask, 2, 11, 1);
        
        byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

        for (int i = 0; i < faces.Length; i++)
        {
            var face = faces[i];
            vertexData.Add(new(face.GeometryIndex, pos, r, 0));
            vertexCount++;
        }
    }

    private static void HandleRightFaceAO(List<Vector4i> vertexData, VoxelChunk chunk, BlockDefinition definition, VoxelChunk[] sideChunks, uint sideBlockMask, int pos, int lx, int ly, int lz, NewBlockFaces newBlockFaces, ref int vertexCount)
    {
        Block sideBlock = lx == 31 ? sideChunks[14][0, ly, lz] : chunk[lx + 1, ly, lz];
        
        if (!sideBlock.IsAir())
        {
            var sDef = sideBlock.Definition();
            if (newBlockFaces.IsOccluded(sDef.NewBlockFaces[0], 1, 3))
                return;
        }
        
        var faces = newBlockFaces.GetFaces(1);
        
        int a = MH.GetAO(sideBlockMask, 2, 5, 11);
        int b = MH.GetAO(sideBlockMask, 20, 11, 23);
        int c = MH.GetAO(sideBlockMask, 26, 23, 17);
        int d = MH.GetAO(sideBlockMask, 8, 17, 5);
        
        byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

        for (int i = 0; i < faces.Length; i++)
        {
            var face = faces[i];
            vertexData.Add(new(face.GeometryIndex, pos, r, 0));
            vertexCount++;
        }
    }

    private static void HandleTopFaceAO(List<Vector4i> vertexData, VoxelChunk chunk, BlockDefinition definition, VoxelChunk[] sideChunks, uint sideBlockMask, int pos, int lx, int ly, int lz, NewBlockFaces newBlockFaces, ref int vertexCount)
    {
        Block sideBlock = ly == 31 ? sideChunks[22][lx, 0, lz] : chunk[lx, ly + 1, lz];
        
        if (!sideBlock.IsAir())
        {
            var sDef = sideBlock.Definition();
            if (newBlockFaces.IsOccluded(sDef.NewBlockFaces[0], 2, 4))
                return;
        }
        
        var faces = newBlockFaces.GetFaces(2);
        
        int a = MH.GetAO(sideBlockMask, 18, 19, 21);
        int b = MH.GetAO(sideBlockMask, 24, 21, 25);
        int c = MH.GetAO(sideBlockMask, 26, 25, 23);
        int d = MH.GetAO(sideBlockMask, 20, 23, 19);
        
        byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

        for (int i = 0; i < faces.Length; i++)
        {
            var face = faces[i];
            vertexData.Add(new(face.GeometryIndex, pos, r, 0));
            vertexCount++;
        }
    }

    private static void HandleLeftFaceAO(List<Vector4i> vertexData, VoxelChunk chunk, BlockDefinition definition, VoxelChunk[] sideChunks, uint sideBlockMask, int pos, int lx, int ly, int lz, NewBlockFaces newBlockFaces, ref int vertexCount)
    {
        Block sideBlock = lx == 0 ? sideChunks[12][31, ly, lz] : chunk[lx - 1, ly, lz];
        
        if (!sideBlock.IsAir())
        {
            var sDef = sideBlock.Definition();
            if (newBlockFaces.IsOccluded(sDef.NewBlockFaces[0], 3, 1))
                return;
        }
        
        var faces = newBlockFaces.GetFaces(3);
        
        int a = MH.GetAO(sideBlockMask, 6, 3, 15);
        int b = MH.GetAO(sideBlockMask, 24, 15, 21);
        int c = MH.GetAO(sideBlockMask, 18, 21, 9);
        int d = MH.GetAO(sideBlockMask, 0, 9, 3);
        
        byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

        for (int i = 0; i < faces.Length; i++)
        {
            var face = faces[i];
            vertexData.Add(new(face.GeometryIndex, pos, r, 0));
            vertexCount++;
        }
    }

    private static void HandleBottomFaceAO(List<Vector4i> vertexData, VoxelChunk chunk, BlockDefinition definition, VoxelChunk[] sideChunks, uint sideBlockMask, int pos, int lx, int ly, int lz, NewBlockFaces newBlockFaces, ref int vertexCount)
    {
        Block sideBlock = ly == 0 ? sideChunks[4][lx, 31, lz] : chunk[lx, ly - 1, lz];
        
        if (!sideBlock.IsAir())
        {
            var sDef = sideBlock.Definition();
            if (newBlockFaces.IsOccluded(sDef.NewBlockFaces[0], 4, 2))
                return;
        }
        
        var faces = newBlockFaces.GetFaces(4);
        
        int a = MH.GetAO(sideBlockMask, 2, 1, 5);
        int b = MH.GetAO(sideBlockMask, 8, 5, 7);
        int c = MH.GetAO(sideBlockMask, 6, 7, 3);
        int d = MH.GetAO(sideBlockMask, 0, 3, 1);
        
        byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

        for (int i = 0; i < faces.Length; i++)
        {
            var face = faces[i];
            vertexData.Add(new(face.GeometryIndex, pos, r, 0));
            vertexCount++;
        }
    }

    private static void HandleBackFaceAO(List<Vector4i> vertexData, VoxelChunk chunk, BlockDefinition definition, VoxelChunk[] sideChunks, uint sideBlockMask, int pos, int lx, int ly, int lz, NewBlockFaces newBlockFaces, ref int vertexCount)
    {
        Block sideBlock = lz == 31 ? sideChunks[16][lx, ly, 0] : chunk[lx, ly, lz + 1];
        
        if (!sideBlock.IsAir())
        {
            var sDef = sideBlock.Definition();
            if (newBlockFaces.IsOccluded(sDef.NewBlockFaces[0], 5, 0))
                return;
        }
        
        var faces = newBlockFaces.GetFaces(5);

        int a = MH.GetAO(sideBlockMask, 8, 7, 17);
        int b = MH.GetAO(sideBlockMask, 26, 17, 25);
        int c = MH.GetAO(sideBlockMask, 24, 25, 15);
        int d = MH.GetAO(sideBlockMask, 6, 15, 7);
        
        byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

        for (int i = 0; i < faces.Length; i++)
        {
            var face = faces[i];
            vertexData.Add(new(face.GeometryIndex, pos, r, 0));
            vertexCount++;
        }
    }

    public static readonly Vector3i[] _neighbourOffsets = [
        (-1, -1, -1), // 0
        (0, -1, -1), // 1
        (1, -1, -1), // 2

        (-1, -1, 0), // 3
        (0, -1, 0), // 4
        (1, -1, 0), // 5

        (-1, -1, 1), // 6
        (0, -1, 1), // 7
        (1, -1, 1), // 8

        (-1, 0, -1), // 9
        (0, 0, -1), // 10
        (1, 0, -1), // 11

        (-1, 0, 0), // 12
        (0, 0, 0), // 13
        (1, 0, 0), // 14

        (-1, 0, 1), // 15
        (0, 0, 1), // 16
        (1, 0, 1), // 17

        (-1, 1, -1), // 18
        (0, 1, -1), // 19
        (1, 1, -1), // 20

        (-1, 1, 0), // 21
        (0, 1, 0), // 22
        (1, 1, 0), // 23

        (-1, 1, 1), // 24
        (0, 1, 1), // 25
        (1, 1, 1) // 26
    ];
}