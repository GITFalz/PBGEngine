using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PBG.MathLibrary;
using PBG.Threads;

namespace PBG.Voxel
{
    public static partial class VoxelChunkGenerator
    {
        private static int[] _xCounter = null!;

        public static void InitCache()
        {
            _xCounter = new int[(TaskPool.ThreadCount + 1) * BlockData.BLOCK_COUNT];
        }

        private static int[] _sides = [5, 3, 4, 1, 2, 0];
        private static Vector3i[] _offsets = [
            new(0, 0, -1),
            new(1, 0, 0),
            new(0, 1, 0),
            new(-1, 0, 0),
            new(0, -1, 0),
            new(0, 0, 1)
        ];

        private static int[] _sideOffsets = [
            10, 14, 22, 12, 4, 16
        ];

        public static int GetAO(uint sideBlockMask, int a1, int b1, int c1)
        {
            return AO_LUT[((sideBlockMask >> a1) & 1) | (((sideBlockMask >> b1) & 1) << 1) | (((sideBlockMask >> c1) & 1) << 2)];
        }

        private static int[] aoCorners =
        [
            // ─────────────────────────────────────────────
            // if ((occlusion & 0b000001) == 0)   ← bit 0
            0,  1,  9, 18, 9, 19, 20,19,11, 2, 11, 1,   // 0

            // ─────────────────────────────────────────────
            // if ((occlusion & 0b000010) == 0)   ← bit 1
            2,  5, 11, 20,11,23,26,23,17,8, 17, 5,   // 1

            // ─────────────────────────────────────────────
            // if ((occlusion & 0b000100) == 0)   ← bit 2
            18,19,21, 24,21,25, 26,25,23, 20,23,19,     // 2

            // ─────────────────────────────────────────────
            // if ((occlusion & 0b001000) == 0)   ← bit 3
            6,  3, 15, 24,15,21, 18,21, 9, 0,  9, 3,   // 3

            // ─────────────────────────────────────────────
            // if ((occlusion & 0b010000) == 0)   ← bit 4
            2,  1,  5, 8,  5,  7, 6,  7,  3, 0,  3,  1, // 4

            // ─────────────────────────────────────────────
            // if ((occlusion & 0b100000) == 0)   ← bit 5
            8,  7, 17, 26,17,25, 24,25,15, 6, 15,  7   // 5
        ];

        private static int[] AO_LUT =
        [
            0, 1, 1, 2, 1, 2, 3, 3
        ];

        public static bool GenerateIndirectMesh(DefaultChunkRenderingProcess process, Vector3i worldPosition, ChunkBlocks blocks)
        {
            Stopwatch sw = Stopwatch.StartNew();

            long setupTime = 0;
            long neighbourTime = 0;
            long faceTime = 0;
            long internalFaceTime = 0;

            var renderer = process.Chunk.Renderer;

            // setup
            long t = sw.ElapsedTicks;
            VoxelChunk emptyChunk = new VoxelChunk(renderer, (0, 0, 0));
            VoxelChunk[] sideChunks = new VoxelChunk[27];
            for (int i = 0; i < 27; i++)
                sideChunks[i] = renderer.GetChunk(process.Chunk.RelativePosition + _neighbourOffsets[i]) ?? emptyChunk;
            setupTime += sw.ElapsedTicks - t;

            for (int x = 0; x < 32; x++)
            for (int y = 0; y < 32; y++)
            for (int z = 0; z < 32; z++)
            {
                var block = blocks.Get(x, y, z);
                if (block.IsAir())
                    continue;

                int ti = 0;
                uint sideBlockMask = 0;

                // neighbour sampling
                t = sw.ElapsedTicks;
                for (int y1 = -1; y1 <= 1; y1++)
                for (int z1 = -1; z1 <= 1; z1++)
                for (int x1 = -1; x1 <= 1; x1++)
                { 
                    int nx = x + x1;
                    int ny = y + y1;
                    int nz = z + z1;

                    Block sideBlock;
                    if ((uint)(x + x1) < 32 && (uint)(y + y1) < 32 && (uint)(z + z1) < 32)
                    {
                        sideBlock = blocks.Get(x + x1, y + y1, z + z1);
                    }
                    else
                    {
                        int cx = nx < 0 ? -1 : nx >= 32 ? 1 : 0;
                        int cy = ny < 0 ? -1 : ny >= 32 ? 1 : 0;
                        int cz = nz < 0 ? -1 : nz >= 32 ? 1 : 0;
                        int chunkIdx = (cy + 1) * 9 + (cz + 1) * 3 + (cx + 1);

                        sideBlock = sideChunks[chunkIdx]?.Get(nx & 31, ny & 31, nz & 31) ?? Block.Air;
                    }

                    uint aoBit = sideBlock.IsAir() ? 0u : 1u;
                    sideBlockMask |= aoBit << ti;
                    ti++;
                }
                neighbourTime += sw.ElapsedTicks - t;

                var definition = block.Definition();
                int pos = x | (y << 5) | (z << 10);

                // visible faces
                t = sw.ElapsedTicks;
                
                HandleFrontFaceAO(process, definition, sideChunks, sideBlockMask, pos, x, y, z);
                HandleRightFaceAO(process, definition, sideChunks, sideBlockMask, pos, x, y, z);
                HandleTopFaceAO(process, definition, sideChunks, sideBlockMask, pos, x, y, z);
                HandleLeftFaceAO(process, definition, sideChunks, sideBlockMask, pos, x, y, z);
                HandleBottomFaceAO(process, definition, sideChunks, sideBlockMask, pos, x, y, z);
                HandleBackFaceAO(process, definition, sideChunks, sideBlockMask, pos, x, y, z);

                faceTime += sw.ElapsedTicks - t;

                // internal faces
                t = sw.ElapsedTicks;
                var iFaces = definition.NewBlockFaces[0].InternalFaces;
                for (int i = 0; i < iFaces.Length; i++)
                {
                    var face = iFaces[i];
                    process.VertexData.Add(new(face.GeometryIndex, pos, 0, 0));
                }
                internalFaceTime += sw.ElapsedTicks - t;
            }


            Console.WriteLine($"Setup:          {setupTime / (double)Stopwatch.Frequency:F6}s");
            Console.WriteLine($"Neighbour AO:   {neighbourTime / (double)Stopwatch.Frequency:F6}s");
            Console.WriteLine($"Face gen:       {faceTime / (double)Stopwatch.Frequency:F6}s");
            Console.WriteLine($"Internal faces: {internalFaceTime / (double)Stopwatch.Frequency:F6}s");
            Console.WriteLine($"Total:          {sw.Elapsed.TotalSeconds:F6}s");

            return true;
        }

        private static void HandleFrontFaceAO(DefaultChunkRenderingProcess process, BlockDefinition definition, VoxelChunk[] sideChunks, uint sideBlockMask, int pos, int lx, int ly, int lz)
        {
            Block sideBlock = lz == 0 ? sideChunks[10].Get(lx, ly, 31) : process.Chunk.Get(lx, ly, lz - 1);
            var sDef = sideBlock.Definition();

            if (!sideBlock.IsAir() && definition.NewBlockFaces[0].IsOccluded(sDef.NewBlockFaces[0], 0, 5))
                return;
            
            var faces = definition.NewBlockFaces[0].GetFaces(0);
            
            int a = GetAO(sideBlockMask, 0, 1, 9);
            int b = GetAO(sideBlockMask, 18, 9, 19);
            int c = GetAO(sideBlockMask, 20, 19, 11);
            int d = GetAO(sideBlockMask, 2, 11, 1);
            
            byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

            for (int i = 0; i < faces.Length; i++)
            {
                var face = faces[i];
                process.VertexData.Add(new(face.GeometryIndex, pos, r, 0));
            }
        }

        private static void HandleRightFaceAO(DefaultChunkRenderingProcess process, BlockDefinition definition, VoxelChunk[] sideChunks, uint sideBlockMask, int pos, int lx, int ly, int lz)
        {
            Block sideBlock = lx == 31 ? sideChunks[14].Get(0, ly, lz) : process.Chunk.Get(lx + 1, ly, lz);
            var sDef = sideBlock.Definition();

            if (!sideBlock.IsAir() && definition.NewBlockFaces[0].IsOccluded(sDef.NewBlockFaces[0], 1, 3))
                return;
            
            var faces = definition.NewBlockFaces[0].GetFaces(1);
            
            int a = GetAO(sideBlockMask, 2, 5, 11);
            int b = GetAO(sideBlockMask, 20, 11, 23);
            int c = GetAO(sideBlockMask, 26, 23, 17);
            int d = GetAO(sideBlockMask, 8, 17, 5);
            
            byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

            for (int i = 0; i < faces.Length; i++)
            {
                var face = faces[i];
                process.VertexData.Add(new(face.GeometryIndex, pos, r, 0));
            }
        }

        private static void HandleTopFaceAO(DefaultChunkRenderingProcess process, BlockDefinition definition, VoxelChunk[] sideChunks, uint sideBlockMask, int pos, int lx, int ly, int lz)
        {
            Block sideBlock = ly == 31 ? sideChunks[22].Get(lx, 0, lz) : process.Chunk.Get(lx, ly + 1, lz);
            
            var sDef = sideBlock.Definition();
            if (!sideBlock.IsAir() && definition.NewBlockFaces[0].IsOccluded(sDef.NewBlockFaces[0], 2, 4))
                return;
            
            var faces = definition.NewBlockFaces[0].GetFaces(2);
            
            int a = GetAO(sideBlockMask, 18, 19, 21);
            int b = GetAO(sideBlockMask, 24, 21, 25);
            int c = GetAO(sideBlockMask, 26, 25, 23);
            int d = GetAO(sideBlockMask, 20, 23, 19);
            
            byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

            for (int i = 0; i < faces.Length; i++)
            {
                var face = faces[i];
                process.VertexData.Add(new(face.GeometryIndex, pos, r, 0));
            }
        }

        private static void HandleLeftFaceAO(DefaultChunkRenderingProcess process, BlockDefinition definition, VoxelChunk[] sideChunks, uint sideBlockMask, int pos, int lx, int ly, int lz)
        {
            Block sideBlock = lx == 0 ? sideChunks[12].Get(31, ly, lz) : process.Chunk.Get(lx - 1, ly, lz);
            
            var sDef = sideBlock.Definition();
            if (!sideBlock.IsAir() && definition.NewBlockFaces[0].IsOccluded(sDef.NewBlockFaces[0], 3, 1))
                return;
            
            var faces = definition.NewBlockFaces[0].GetFaces(3);
            
            int a = GetAO(sideBlockMask, 6, 3, 15);
            int b = GetAO(sideBlockMask, 24, 15, 21);
            int c = GetAO(sideBlockMask, 18, 21, 9);
            int d = GetAO(sideBlockMask, 0, 9, 3);
            
            byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

            for (int i = 0; i < faces.Length; i++)
            {
                var face = faces[i];
                process.VertexData.Add(new(face.GeometryIndex, pos, r, 0));
            }
        }

        private static void HandleBottomFaceAO(DefaultChunkRenderingProcess process, BlockDefinition definition, VoxelChunk[] sideChunks, uint sideBlockMask, int pos, int lx, int ly, int lz)
        {
            Block sideBlock = ly == 0 ? sideChunks[4].Get(lx, 31, lz) : process.Chunk.Get(lx, ly - 1, lz);
            
            var sDef = sideBlock.Definition();
            if (!sideBlock.IsAir() && definition.NewBlockFaces[0].IsOccluded(sDef.NewBlockFaces[0], 4, 2))
                return;
            
            var faces = definition.NewBlockFaces[0].GetFaces(4);
            
            int a = GetAO(sideBlockMask, 2, 1, 5);
            int b = GetAO(sideBlockMask, 8, 5, 7);
            int c = GetAO(sideBlockMask, 6, 7, 3);
            int d = GetAO(sideBlockMask, 0, 3, 1);
            
            byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

            for (int i = 0; i < faces.Length; i++)
            {
                var face = faces[i];
                process.VertexData.Add(new(face.GeometryIndex, pos, r, 0));
            }
        }

        private static void HandleBackFaceAO(DefaultChunkRenderingProcess process, BlockDefinition definition, VoxelChunk[] sideChunks, uint sideBlockMask, int pos, int lx, int ly, int lz)
        {
            Block sideBlock = lz == 31 ? sideChunks[16].Get(lx, ly, 0) : process.Chunk.Get(lx, ly, lz + 1);
            
            var sDef = sideBlock.Definition();
            if (!sideBlock.IsAir() && definition.NewBlockFaces[0].IsOccluded(sDef.NewBlockFaces[0], 5, 0))
                return;
            
            var faces = definition.NewBlockFaces[0].GetFaces(5);

            int a = GetAO(sideBlockMask, 8, 7, 17);
            int b = GetAO(sideBlockMask, 26, 17, 25);
            int c = GetAO(sideBlockMask, 24, 25, 15);
            int d = GetAO(sideBlockMask, 6, 15, 7);
            
            byte r = (byte)((a & 3) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6));

            for (int i = 0; i < faces.Length; i++)
            {
                var face = faces[i];
                process.VertexData.Add(new(face.GeometryIndex, pos, r, 0));
            }
        }

        public class VoxelFaces
        {
            public Block Block;
            public uint Occlusion;
            public VoxelFace[]?[] Faces = [null, null, null, null, null, null];
            public VoxelFace[]? InternalFaces = null;
        }

        public static bool GenerateGreedyMeshNew(DefaultChunkRenderingProcess process, Vector3i worldPosition, ChunkBlocks blocks, VoxelRenderer renderer, DefaultVoxelChunkHandlerNew handler)
        {
            BinaryGreedyMeshNoAO(process, worldPosition, blocks, renderer, handler);
            return true;
        }


        
        public static void BinaryGreedyMeshNoAO(DefaultChunkRenderingProcess process, Vector3i worldPosition, ChunkBlocks chunkBlocks, VoxelRenderer renderer, DefaultVoxelChunkHandlerNew handler)
        {
            Span<uint> xData = stackalloc uint[32 * chunkBlocks.UniqueBlockCount];
            int cIndex = process.ThreadIndex * BlockData.BLOCK_COUNT;

            GreedyMeshFront(process, xData, chunkBlocks, handler, worldPosition, renderer, cIndex);
            GreedyMeshRight(xData, chunkBlocks, handler, worldPosition, renderer, cIndex);
            GreedyMeshTop(xData, chunkBlocks, handler, worldPosition, renderer, cIndex);
            GreedyMeshLeft(xData, chunkBlocks, handler, worldPosition, renderer, cIndex);
            GreedyMeshBottom(xData, chunkBlocks, handler, worldPosition, renderer, cIndex);
            GreedyMeshBack(xData, chunkBlocks, handler, worldPosition, renderer, cIndex);

            for (int b = 0; b < chunkBlocks.UniqueBlockCount; b++)
            {
                var block = chunkBlocks.UniqueBlockTypes[b];
                _xCounter[cIndex + block] = 0;
            }
        }

        
        private static void GreedyMeshAndEmit(Span<uint> xData, int dataIndex, uint blockId, int side, int axis, DefaultVoxelChunkHandlerNew handler)
        {
            var def = BlockData.BlockDefinitions[blockId];
            var faces = def.NewBlockFaces[0].GetFaces(side);   // cache this if you want

            for (int x = 0; x < 32; x++)
            {
                int i = dataIndex + x;
                uint data = xData[i];

                int y = 0;
                while (y < 32)
                {
                    y += System.Numerics.BitOperations.TrailingZeroCount(data >> y);
                    if (y >= 32) continue;

                    int h = System.Numerics.BitOperations.TrailingZeroCount(~(data >> y));
                    uint hmask = h == 32 ? uint.MaxValue : (uint)((1ul << h) - 1);
                    uint mask = hmask << y;

                    xData[i] &= ~mask;

                    int w = 1;
                    while (x + w < 32)
                    {
                        if (((xData[dataIndex + x + w] >> y) & hmask) != hmask)
                            break;
                        xData[dataIndex + x + w] &= ~mask;
                        w++;
                    }

                    Vector3i pos = side switch
                    {
                        0 => (x, y, axis),
                        1 => (axis, y, x),
                        2 => (x, axis, y),
                        3 => (axis, y, x),
                        4 => (x, axis, y),
                        5 => (x, y, axis),
                        _ => throw new ArgumentException()
                    };

                    for (int j = 0; j < faces.Length; j++)
                    {
                        var face = faces[j];
                        face.UvA *= (w, h);
                        face.UvB *= (w, h);
                        face.UvC *= (w, h);
                        face.UvD *= (w, h);

                        switch (side)
                        {
                            case 0: face.B.Y += h-1; face.C.Y += h-1; face.C.X += w-1; face.D.X += w-1; break; 
                            case 1: face.B.Y += h-1; face.C.Y += h-1; face.C.Z += w-1; face.D.Z += w-1; break; 
                            case 2: face.B.Z += h-1; face.C.Z += h-1; face.C.X += w-1; face.D.X += w-1; break; 
                            case 3: face.C.Y += h-1; face.B.Y += h-1; face.B.Z += w-1; face.A.Z += w-1; break; 
                            case 4: face.C.Z += h-1; face.B.Z += h-1; face.B.X += w-1; face.A.X += w-1; break; 
                            case 5: face.C.Y += h-1; face.B.Y += h-1; face.B.X += w-1; face.A.X += w-1; break; 
                        }

                        AddFace(handler, face, pos);
                    }

                    y += h;
                }
            }
        }

        static bool test = true;

        
        private static void GreedyMeshFront(DefaultChunkRenderingProcess process, Span<uint> xData, ChunkBlocks chunkBlocks, DefaultVoxelChunkHandlerNew handler, Vector3i worldPosition, VoxelRenderer renderer, int cIndex)
        {
            int blockCounter = 0;
            var depth = 0;
            var offset = (0, 0, -1);

            Vector3i position;
            int index = 0;

            if (test)
            {
                // Front   
                for (int y = worldPosition.Y - 1; y < worldPosition.Y + 33; y++)
                {
                    for (int x = worldPosition.X - 1; x < worldPosition.X + 33; x++)
                    {
                        var block = renderer.GetBlock((x, y, worldPosition.Z - 1));
                        process.SetAmbientOcclusion(index, (byte)(block.State() & 1));
                        index++;
                    }
                    index += 1122;
                }
                
                // Right
                index = 67;
                for (int y = worldPosition.Y - 1; y < worldPosition.Y + 33; y++)
                {
                    for (int z = worldPosition.Z; z < worldPosition.Z + 32; z++)
                    {
                        var block = renderer.GetBlock((worldPosition.X + 32, y, z));
                        process.SetAmbientOcclusion(index, (byte)(block.State() & 1));
                        index += 34;
                    }
                    index += 68;
                }

                // Top
                index = 38183;
                for (int z = worldPosition.Z; z < worldPosition.Z + 32; z++)
                {
                    for (int x = worldPosition.X; x < worldPosition.X + 32; x++)
                    {
                        var block = renderer.GetBlock((x, worldPosition.Y + 32, z));
                        process.SetAmbientOcclusion(index, (byte)(block.State() & 1));
                        index++;
                    }
                    index+=2;
                }

                // Left
                index = 34;
                for (int y = worldPosition.Y - 1; y < worldPosition.Y + 33; y++)
                {
                    for (int z = worldPosition.Z; z < worldPosition.Z + 32; z++)
                    {
                        var block = renderer.GetBlock((worldPosition.X - 1, y, z));
                        process.SetAmbientOcclusion(index, (byte)(block.State() & 1));
                        index += 34;
                    }
                    index += 68;
                }

                // Bottom
                index = 35;
                for (int z = worldPosition.Z; z < worldPosition.Z + 32; z++)
                {
                    for (int x = worldPosition.X; x < worldPosition.X + 32; x++)
                    {
                        var block = renderer.GetBlock((x, worldPosition.Y - 1, z));
                        process.SetAmbientOcclusion(index, (byte)(block.State() & 1));
                        index++;
                    }
                    index+=2;
                }

                // Back
                index = 1122;
                for (int y = worldPosition.Y - 1; y < worldPosition.Y + 33; y++)
                {
                    for (int x = worldPosition.X - 1; x < worldPosition.X + 33; x++)
                    {
                        var block = renderer.GetBlock((x, y, worldPosition.Z + 32));
                        process.SetAmbientOcclusion(index, (byte)(block.State() & 1));
                        index++;
                    }
                    index += 1122;
                }
            }

            for (int axis = 0; axis < 32; axis++)
            {
                for (int x = 0; x < 32; x++)
                {
                    for (int y = 0; y < 32; y++)
                    {
                        index = x + 1 + (axis + 1) * 34 + (y + 1) * 1156;
                        position = (x, y, axis);

                        var block = chunkBlocks.GetInner(position);
                        int id = (int)block.ID;
                        if (!block.IsAir())
                        {
                            process.SetAmbientOcclusion(index, 1);

                            var definition = block.Definition();
                            var internalfaces = definition.NewBlockFaces[0].InternalFaces;

                            for (int j = 0; j < internalfaces.Length; j++)
                            {
                                var face = internalfaces[j];
                                handler.AddFace(face, position);
                            }

                            int counter = _xCounter[cIndex + id];
                            if (counter == 0)
                            {
                                counter = blockCounter * 32;
                                _xCounter[cIndex + id] = counter+1;
                                counter++;
                                blockCounter++;
                            }

                            Block sideBlock;
                            if (axis == depth)
                            {
                                sideBlock = renderer.GetBlock(worldPosition + position + offset);
                            }
                            else
                            {
                                sideBlock = chunkBlocks.GetInner(position + offset);
                            }

                            if (sideBlock.IsAir() || !definition.NewBlockFaces[0].IsOccluded(sideBlock.Definition().NewBlockFaces[0], 0, 5))
                            {
                                int i = counter-1;
                                xData[i + x] = xData[i + x] | (1u << y);
                            }             
                        }

                        //index += 1156;
                    }
                }

                for (int b = 0; b < chunkBlocks.UniqueBlockCount; b++)
                {   
                    var block = chunkBlocks.UniqueBlockTypes[b];
                    int i = _xCounter[cIndex + block];
                    if (i > 0)
                        GreedyMeshAndEmit(xData, i-1, block, 0, axis, handler);
                }

                for (int i = 0; i < xData.Length; i++)
                {
                    xData[i] = 0;
                }
            }
        }

        
        private static void GreedyMeshRight(Span<uint> xData, ChunkBlocks chunkBlocks, DefaultVoxelChunkHandlerNew handler, Vector3i worldPosition, VoxelRenderer renderer, int cIndex)
        {
            var depth = 31;
            var offset = (1, 0, 0);

            Vector3i position;

            for (int axis = 0; axis < 32; axis++)
            {
                for (int x = 0; x < 32; x++)
                {
                    for (int y = 0; y < 32; y++)
                    {
                        position = (axis, y, x);

                        var block = chunkBlocks.GetInner(position);
                        int id = (int)block.ID;
                        if (block.IsAir())
                        {
                            continue;
                        }

                        var definition = block.Definition();

                        Block sideBlock;
                        if (axis == depth)
                        {
                            sideBlock = renderer.GetBlock(worldPosition + position + offset);
                        }
                        else
                        {
                            sideBlock = chunkBlocks.GetInner(position + offset);
                        }

                        if (!sideBlock.IsAir() && definition.NewBlockFaces[0].IsOccluded(sideBlock.Definition().NewBlockFaces[0], 1, 3))
                        {
                            continue;
                        }           
                        
                        int i = _xCounter[cIndex + id]-1;
                        xData[i + x] = xData[i + x] | (1u << y);
                    }
                }

                for (int b = 0; b < chunkBlocks.UniqueBlockCount; b++)
                {   
                    var block = chunkBlocks.UniqueBlockTypes[b];
                    int i = _xCounter[cIndex + block];
                    if (i > 0)
                        GreedyMeshAndEmit(xData, i-1, block, 1, axis, handler);
                }

                for (int i = 0; i < xData.Length; i++)
                {
                    xData[i] = 0;
                }
            }
        }

        
        private static void GreedyMeshTop(Span<uint> xData, ChunkBlocks chunkBlocks, DefaultVoxelChunkHandlerNew handler, Vector3i worldPosition, VoxelRenderer renderer, int cIndex)
        {
            var depth = 31;
            var offset = (0, 1, 0);

            Vector3i position;

            for (int axis = 0; axis < 32; axis++)
            {
                for (int x = 0; x < 32; x++)
                {
                    for (int y = 0; y < 32; y++)
                    {
                        position = (x, axis, y);

                        var block = chunkBlocks.GetInner(position);
                        int id = (int)block.ID;
                        if (block.IsAir())
                        {
                            continue;
                        }

                        var definition = block.Definition();

                        Block sideBlock;
                        if (axis == depth)
                        {
                            sideBlock = renderer.GetBlock(worldPosition + position + offset);
                        }
                        else
                        {
                            sideBlock = chunkBlocks.GetInner(position + offset);
                        }

                        if (!sideBlock.IsAir() && definition.NewBlockFaces[0].IsOccluded(sideBlock.Definition().NewBlockFaces[0], 2, 4))
                        {
                            continue;
                        }           
                        
                        int i = _xCounter[cIndex + id]-1;
                        xData[i + x] = xData[i + x] | (1u << y);
                    }
                }

                for (int b = 0; b < chunkBlocks.UniqueBlockCount; b++)
                {   
                    var block = chunkBlocks.UniqueBlockTypes[b];
                    int i = _xCounter[cIndex + block];
                    if (i > 0)
                        GreedyMeshAndEmit(xData, i-1, block, 2, axis, handler);
                }

                for (int i = 0; i < xData.Length; i++)
                {
                    xData[i] = 0;
                }
            }
        }

        
        private static void GreedyMeshLeft(Span<uint> xData, ChunkBlocks chunkBlocks, DefaultVoxelChunkHandlerNew handler, Vector3i worldPosition, VoxelRenderer renderer, int cIndex)
        {
            var depth = 0;
            var offset = (-1, 0, 0);

            Vector3i position;

            for (int axis = 0; axis < 32; axis++)
            {
                for (int x = 0; x < 32; x++)
                {
                    for (int y = 0; y < 32; y++)
                    {
                        position = (axis, y, x);

                        var block = chunkBlocks.GetInner(position);
                        int id = (int)block.ID;
                        if (block.IsAir())
                        {
                            continue;
                        }

                        var definition = block.Definition();

                        Block sideBlock;
                        if (axis == depth)
                        {
                            sideBlock = renderer.GetBlock(worldPosition + position + offset);
                        }
                        else
                        {
                            sideBlock = chunkBlocks.GetInner(position + offset);
                        }

                        if (!sideBlock.IsAir() && definition.NewBlockFaces[0].IsOccluded(sideBlock.Definition().NewBlockFaces[0], 3, 1))
                        {
                            continue;
                        }           
                        
                        int i = _xCounter[cIndex + id]-1;
                        xData[i + x] = xData[i + x] | (1u << y);
                    }
                }

                for (int b = 0; b < chunkBlocks.UniqueBlockCount; b++)
                {   
                    var block = chunkBlocks.UniqueBlockTypes[b];
                    int i = _xCounter[cIndex + block];
                    if (i > 0)
                        GreedyMeshAndEmit(xData, i-1, block, 3, axis, handler);
                }

                for (int i = 0; i < xData.Length; i++)
                {
                    xData[i] = 0;
                }
            }
        }

        
        private static void GreedyMeshBottom(Span<uint> xData, ChunkBlocks chunkBlocks, DefaultVoxelChunkHandlerNew handler, Vector3i worldPosition, VoxelRenderer renderer, int cIndex)
        {
            var depth = 0;
            var offset = (0, -1, 0);

            Vector3i position;

            for (int axis = 0; axis < 32; axis++)
            {
                for (int x = 0; x < 32; x++)
                {
                    for (int y = 0; y < 32; y++)
                    {
                        position = (x, axis, y);

                        var block = chunkBlocks.GetInner(position);
                        int id = (int)block.ID;
                        if (block.IsAir())
                        {
                            continue;
                        }

                        var definition = block.Definition();

                        Block sideBlock;
                        if (axis == depth)
                        {
                            sideBlock = renderer.GetBlock(worldPosition + position + offset);
                        }
                        else
                        {
                            sideBlock = chunkBlocks.GetInner(position + offset);
                        }

                        if (!sideBlock.IsAir() && definition.NewBlockFaces[0].IsOccluded(sideBlock.Definition().NewBlockFaces[0], 4, 2))
                        {
                            continue;
                        }           
                        
                        int i = _xCounter[cIndex + id]-1;
                        xData[i + x] = xData[i + x] | (1u << y);
                    }
                }

                for (int b = 0; b < chunkBlocks.UniqueBlockCount; b++)
                {   
                    var block = chunkBlocks.UniqueBlockTypes[b];
                    int i = _xCounter[cIndex + block];
                    if (i > 0)
                        GreedyMeshAndEmit(xData, i-1, block, 4, axis, handler);
                }

                for (int i = 0; i < xData.Length; i++)
                {
                    xData[i] = 0;
                }
            }
        }

        
        private static void GreedyMeshBack(Span<uint> xData, ChunkBlocks chunkBlocks, DefaultVoxelChunkHandlerNew handler, Vector3i worldPosition, VoxelRenderer renderer, int cIndex)
        {
            var depth = 31;
            var offset = (0, 0, 1);

            Vector3i position;

            for (int axis = 0; axis < 32; axis++)
            {
                for (int x = 0; x < 32; x++)
                {
                    for (int y = 0; y < 32; y++)
                    {
                        position = (x, y, axis);

                        var block = chunkBlocks.GetInner(position);
                        int id = (int)block.ID;
                        if (block.IsAir())
                        {
                            continue;
                        }

                        var definition = block.Definition();

                        Block sideBlock;
                        if (axis == depth)
                        {
                            sideBlock = renderer.GetBlock(worldPosition + position + offset);
                        }
                        else
                        {
                            sideBlock = chunkBlocks.GetInner(position + offset);
                        }

                        if (!sideBlock.IsAir() && definition.NewBlockFaces[0].IsOccluded(sideBlock.Definition().NewBlockFaces[0], 5, 0))
                        {
                            continue;
                        }           
                        
                        int i = _xCounter[cIndex + id]-1;
                        xData[i + x] = xData[i + x] | (1u << y);
                    }
                }

                for (int b = 0; b < chunkBlocks.UniqueBlockCount; b++)
                {   
                    var block = chunkBlocks.UniqueBlockTypes[b];
                    int i = _xCounter[cIndex + block];
                    if (i > 0)
                        GreedyMeshAndEmit(xData, i-1, block, 5, axis, handler);
                }

                for (int i = 0; i < xData.Length; i++)
                {
                    xData[i] = 0;
                }
            }
        }

        private static void AddFace(DefaultVoxelChunkHandlerNew handler, VoxelFace face, Vector3 position)
        {
            handler.Vertices.Add(face.A + position);
            handler.Vertices.Add(face.B + position);
            handler.Vertices.Add(face.C + position);
            handler.Vertices.Add(face.D + position);

            handler.Normals.Add(face.Normal);
            handler.Normals.Add(face.Normal);
            handler.Normals.Add(face.Normal);
            handler.Normals.Add(face.Normal);

            handler.Uvs.Add(face.UvA);
            handler.Uvs.Add(face.UvB);
            handler.Uvs.Add(face.UvC);
            handler.Uvs.Add(face.UvD);

            var baseTexture = face.TextureIndex | (face.Side << 16);
            handler.TextureIndices.Add(baseTexture);
            handler.TextureIndices.Add(baseTexture | (1 << 20));
            handler.TextureIndices.Add(baseTexture | (2 << 20));
            handler.TextureIndices.Add(baseTexture | (3 << 20));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Block GetBlock_Fast(Vector3i pos, VoxelChunk chunk)
        {
            // Local position relative to current chunk
            int lx = pos.X - chunk.WorldPosition.X;
            int ly = pos.Y - chunk.WorldPosition.Y;
            int lz = pos.Z - chunk.WorldPosition.Z;

            if ((uint)lx < 32u & (uint)ly < 32u & (uint)lz < 32u)
                return chunk.Get(ChunkBlocks.GetIndex(lx, ly, lz));

            return chunk.Renderer.GetChunk(VoxelData.BlockToChunkRelative(pos), out var c) ? c.Get(ChunkBlocks.GetIndex(lx, ly, lz)) : Block.Air;
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
}