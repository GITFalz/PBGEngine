using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using PBG.Core;
using PBG.MathLibrary;
using PBG.Noise;

namespace PBG.NewVoxel;

using VH = VoxelHelper;

[InternalSystemInit]
public unsafe static class ChunkGenerationAvx2
{
    const int CHUNK_SIZE = 32;


    static V256i BLOCK_TEST_V;
    static V256i BLOCK_GRASS_V;
    static V256i BLOCK_DIRT_V;
    static V256i BLOCK_STONE_V;
    static V256i BLOCK_GRAVEL_V;
    static V256i BLOCK_LOG_V;
    static V256i BLOCK_LEAF_V;
    static V256i BLOCK_SNOW_V;
    static V256i BLOCK_LIMESTONE_V;
    static V256i BLOCK_GRANITE_V;
    static V256i BLOCK_ANDESITE_V;
    static V256i BLOCK_MOSSY_STONE_V;
    static V256i BLOCK_STONE_STAIR_V;


    static readonly V256f WorldHeightV256f = V256F.New(224.0f); // 7 chunks * 32 blocks
    static readonly V256f SeaLevelV256f    = V256F.New(64.0f);  // baseline ground height
    static readonly V256f MaxPeakV256f     = V256F.New(200.0f); // cap so peaks stay below world top
    static readonly V256f SnowLineV256f    = V256F.New(150.0f); // terrain above this height gets capped in snow

    static readonly V256f TwoPFive = V256F.New(2.5f);

    static readonly V256f _v10 = V256F.New(0.15f);
    static readonly V256f _v11 = V256F.New(0.37f);

    static readonly V256f _v3 = V256F.New(0.0025f);
    static readonly V256f _v4 = V256F.New(2.05f);
    static readonly V256f _v5 = V256F.New(0.05f);

    static readonly V256f _v6 = V256F.New(0.35f);
    static readonly V256f _v7 = V256F.New(0.75f);

    static readonly V256f _v8 = V256F.New(0.006f);



    private static V256i GetBlock(string name)
    {
        if (BlockData.GetBlockFull(name, out Block block))
            return V256I.New((int)block.blockData);

        return V256I.Zero;
    }

    public static void Init()
    {
        BLOCK_TEST_V = GetBlock("test_block");
        BLOCK_GRASS_V = GetBlock("grass_block");
        BLOCK_DIRT_V = GetBlock("dirt_block");
        BLOCK_STONE_V = GetBlock("stone_block");
        BLOCK_GRAVEL_V = GetBlock("gravel_block");
        BLOCK_LOG_V = GetBlock("log_block");
        BLOCK_LEAF_V = GetBlock("leaf_block");
        BLOCK_SNOW_V = GetBlock("snow_block");
        BLOCK_LIMESTONE_V = GetBlock("limestone_block");
        BLOCK_GRANITE_V = GetBlock("granite_block");
        BLOCK_ANDESITE_V = GetBlock("andesite_block");
        BLOCK_MOSSY_STONE_V = GetBlock("mossy_stone_block");
        BLOCK_STONE_STAIR_V = GetBlock("stone_stair_block");
    }


    static V256f Fbm2DSimd(V256f x, V256f y, int octaves)
    {
        V256f f = V256f.Zero;
        V256f w = V256F.Half;

        for (int i = 0; i < octaves; i++)
        {
            f = Avx.Add(f, Avx.Multiply(w, PerlinNoiseAvx2.Noise(x, y)));

            x = Avx.Multiply(x, V256F.Two);
            y = Avx.Multiply(y, V256F.Two);
            w = Avx.Multiply(w, V256F.Half);
        }

        return f;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static V256f SmoothstepSimd(V256f edge0, V256f edge1, V256f x)
    {
        V256f t = Avx.Divide(
            Avx.Subtract(x, edge0),
            Avx.Subtract(edge1, edge0));

        t = V256F.Clamp01(t);

        V256f t2 = Avx.Multiply(t, t);
        V256f threeMinus2t = Avx.Subtract(
            V256F.Three,
            Avx.Multiply(V256F.Two, t));

        return Avx.Multiply(t2, threeMinus2t);
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    static V256f GetTerrainHeightSimd(V256f worldX, V256f worldZ)
    {
        var continent = Fbm2DSimd(worldX * _v3, worldZ * _v3, 4);
        continent = ((continent + V256F.One) * V256F.Half).Clamp01();
        continent = SmoothstepSimd(_v6, _v7, continent);

        V256f ridged = V256F.Zero;
        V256f amplitude = V256F.Half;
        V256f frequency = _v8;
        V256f amplitudeSum = V256F.Zero;
        
        for (int i = 0; i < 6; i++)
        {
            V256f n = PerlinNoiseAvx2.Noise(worldX * frequency, worldZ * frequency);
            n = V256F.Subtract(V256F.One, V256F.Abs(n));
            n = V256F.Multiply(n, n);
            ridged = V256F.Add(ridged, V256F.Multiply(n, amplitude));
            amplitudeSum = V256F.Add(amplitudeSum, amplitude);
            frequency = V256F.Multiply(frequency, _v4);
            amplitude = V256F.Multiply(amplitude, V256F.Half);
        }
        ridged = V256F.Divide(ridged, amplitudeSum);

        V256f detail = V256F.Multiply(Fbm2DSimd(V256F.Multiply(worldX, _v5), V256F.Multiply(worldZ, _v5), 3), V256F.Four);

        V256f mountainAmount = ridged * continent;
        V256f h = V256F.Add(V256F.Add(SeaLevelV256f, V256F.Multiply(mountainAmount, V256F.Subtract(MaxPeakV256f, SeaLevelV256f))), detail);
 
        return V256F.Clamp(h, V256F.One, V256F.Subtract(WorldHeightV256f, V256F.One));
    }


    static readonly V256f _graniteCheck = V256F.New(0.12f);
    static readonly V256f _andesiteCheck = V256F.New(0.24f);
    static readonly V256f _limestoneCheck = V256F.New(0.36f);

    static readonly V256f _OO8 = V256F.New(0.08f);



    static readonly V256f _v1 = V256F.New(0.36f);
    static readonly V256f _v2 = V256F.New(0.15f);
    
    static V256i GetStoneVariantSimd(V256f worldX, V256f worldY, V256f worldZ, V256f terrainHeight)
    {
        V256f n = PerlinNoiseAvx2.Noise((worldX * _OO8) + (worldY * _OO8), (worldZ * _OO8) - (worldY * _OO8));

        n = (n + V256F.One) * V256F.Half;
 
        V256f mossChance = V256F.Clamp01(V256F.One - ((terrainHeight - SeaLevelV256f) / (SnowLineV256f - SeaLevelV256f)));

        V256i isGranite = Avx.CompareLessThan(n, _graniteCheck).ToInt();
        V256i isAndesite = Avx.CompareLessThan(n, _andesiteCheck).ToInt();
        V256i isLimestone = Avx.CompareLessThan(n, _limestoneCheck).ToInt();
        V256i isMossyStone = Avx.CompareLessThan(n, _v1 + (_v2 * mossChance)).ToInt();

        var result = BLOCK_STONE_V;
        result = V256I.IfElseFull(isMossyStone, BLOCK_MOSSY_STONE_V, result);
        result = V256I.IfElseFull(isLimestone, BLOCK_LIMESTONE_V, result);
        result = V256I.IfElseFull(isAndesite, BLOCK_ANDESITE_V, result);
        result = V256I.IfElseFull(isGranite, BLOCK_GRANITE_V, result);
        return result;
    }




    public static void Vector256HeightSimple(int baseX, int baseZ, ChunkGeneration.CacheHandler<V256f> handler)
    {
        V256f* height = handler.Cache[0];

        V256f one = V256F.One;
        V256f xOffsets = V256.Create(0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f);
            
        // Process 8 columns at a time in X
        for (int z = 0; z < 32; z++)
        {
            V256f wz = V256.Create((float)(baseZ + z));

            for (int x = 0; x < 32; x += 8)
            {
                V256f wx = V256F.Add(V256F.New((float)(baseX + x)), xOffsets);
                V256f h = GetTerrainHeightSimd(wx, wz);
                //V256f hx = GetTerrainHeightSimd(Avx.Add(wx, one), wz);
                //V256f hz = GetTerrainHeightSimd(wx, Avx.Add(wz, one));

                int idx = (x >> 3) + z * 4;

                height[idx]  = h;
                //heightX[idx] = hx;
                //heightZ[idx] = hz;
            }
        }
    }

    public static void Vector256Height(int baseX, int baseZ, ChunkGeneration.CacheHandler<V256f> handler)
    {
        V256f* height  = handler.Cache[0];
        V256f* heightX = handler.Cache[1];
        V256f* heightZ = handler.Cache[2];

        V256f one = V256F.One;
        V256f xOffsets = V256.Create(0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f);
            
        // Process 8 columns at a time in X
        for (int z = 0; z < 32; z++)
        {
            V256f wz = V256.Create((float)(baseZ + z));

            for (int x = 0; x < 32; x += 8)
            {
                V256f wx = V256F.Add(V256F.New((float)(baseX + x)), xOffsets);
                V256f h = GetTerrainHeightSimd(wx, wz);
                V256f hx = GetTerrainHeightSimd(Avx.Add(wx, one), wz);
                V256f hz = GetTerrainHeightSimd(wx, Avx.Add(wz, one));

                int idx = (x >> 3) + z * 4;

                height[idx]  = h;
                heightX[idx] = hx;
                heightZ[idx] = hz;
            }
        }
    }


    public static void Vector256Height(int baseX, int baseZ, ChunkGeneration.CacheHandler<float> handler)
    {
        float* height  = handler.Cache[0];
        float* heightX = handler.Cache[1];
        float* heightZ = handler.Cache[2];

        V256f one = V256F.One;
        V256f xOffsets = V256.Create(0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f);

        int ptrIndex = 0;
            
        // Process 8 columns at a time in X
        for (int z = 0; z < 32; z++)
        {
            V256f wz = V256.Create((float)(baseZ + z));

            for (int x = 0; x < 32; x += 8)
            {
                V256f wx = V256F.Add(V256F.New((float)(baseX + x)), xOffsets);
                V256f h = GetTerrainHeightSimd(wx, wz);
                V256f hx = GetTerrainHeightSimd(Avx.Add(wx, one), wz);
                V256f hz = GetTerrainHeightSimd(wx, Avx.Add(wz, one));

                int idx = (x >> 3) + z * 4;

                *(V256f*)(height  + ptrIndex) = h;
                *(V256f*)(heightX + ptrIndex) = hx;
                *(V256f*)(heightZ + ptrIndex) = hz;

                ptrIndex += 8;
            }
        }
    }




    public static void Vector256PopulateSimple(VoxelChunk chunk, int baseX, int baseY, int baseZ, ChunkGeneration.CacheHandler<V256f> handler)
    {
        chunk.HasBlocks = true;
        Block* blocks = chunk.Blocks;

        V256f* height  = handler.Cache[0];

        for (int x = 0; x < CHUNK_SIZE; x += 8)
        {
            for (int z = 0; z < CHUNK_SIZE; z++)
            {
                int idx = (x >> 3) + z * 4;

                V256i terrainHeight = V256I.FloorToInt(height[idx]);

                V256i worldY = V256I.New(baseY);

                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    int blockIndex = x + z * 32 + y * 1024;

                    worldY = V256I.Add(worldY, V256I.One);

                    // Solid if worldY <= terrainHeight
                    V256i isSolid =
                        V256I.CompareGreaterThanOrEqual(
                            terrainHeight,
                            worldY);

                    V256i block =
                        V256I.IfElseFull(
                            isSolid,
                            BLOCK_STONE_V,
                            V256I.Zero);

                    *(V256i*)(blocks + blockIndex) = block;
                }
            }
        }
    }

    static readonly V256u _full = V256U.New(0xFFFFFFFF);
    static readonly V256u _oneMask = V256U.New(1);

    public static void Vector256Populate(VoxelChunk chunk, int baseX, int baseY, int baseZ, ChunkGeneration.CacheHandler<V256f> handler)
    {
        chunk.HasBlocks = true;
        Block* blocks = chunk.Blocks;

        V256f* height  = handler.Cache[0];
        V256f* heightX = handler.Cache[1];
        V256f* heightZ = handler.Cache[2];

        // Lane i must carry x+i, not a broadcast of x - each of the 8 lanes
        // represents a *different* column, and per-column noise (gravel, stone
        // variant) depends on this varying correctly across lanes.
        V256f laneOffsets = Vector256.Create(0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f);

        for (int x = 0; x < CHUNK_SIZE; x += 8)
        {
            V256f worldX = V256F.Add(V256F.New(x + baseX), laneOffsets);

            V256f xGravel = V256F.Multiply(worldX, _v10);

            for (int z = 0; z < CHUNK_SIZE; z++)
            {
                int idx = (x >> 3) + (z * 4);

                V256f worldZ = V256F.New(z + baseZ);

                V256f terrainHeightF = height[idx];
                V256i terrainHeight = V256I.FloorToInt(terrainHeightF);

                V256f slope = V256F.Add(V256F.Abs(terrainHeightF - heightX[idx]), V256F.Abs(V256F.Subtract(terrainHeightF, heightZ[idx])));
                V256i isSteep = Avx.CompareGreaterThan(slope, TwoPFive).AsInt32();
                V256i isSnowCap = Avx.CompareGreaterThan(terrainHeightF, SnowLineV256f).AsInt32();

                V256f zGravel = V256F.Multiply(worldZ, _v10);

                // Start one below baseY so the increment at the top of the
                // loop lands on baseY for y == 0, not baseY + 1.
                V256i worldYI = V256I.New(baseY - 1);

                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    if (y == 0 && chunk.WorldPosition.Y == 0)
                    {
                        SetBlocksV256i(chunk, blocks, BLOCK_STONE_V, x, y, z);
                        continue;
                    }

                    worldYI = V256I.Add(worldYI, V256I.One);

                    V256i isAir = Avx2.CompareGreaterThan(worldYI, terrainHeight);

                    if (V256I.AllTrue(isAir))
                    {
                        SetBlocksV256i(chunk, blocks, V256I.Zero, x, y, z);
                        continue;
                    }

                    // Derive worldY fresh from worldYI every iteration instead
                    // of maintaining a separate accumulator - keeps it from
                    // ever drifting out of sync after an air-skip above.
                    V256f worldY = Avx.ConvertToVector256Single(worldYI);

                    V256i blockId = V256I.Zero;

                    V256i depth = V256I.Subtract(terrainHeight, worldYI);

                    V256i isSurface = Avx2.CompareEqual(depth, V256I.Zero);
                    V256i isShallow = Avx2.And(
                        Avx2.CompareGreaterThan(depth, V256I.Zero),
                        V256I.CompareLessThanOrEqual(depth, V256I.Three)
                    );
                    V256i isDeep = Avx2.CompareGreaterThan(depth, V256I.New(3));

                    V256i stoneVariant = GetStoneVariantSimd(worldX, worldY, worldZ, terrainHeightF);
                    V256i needsGravel = Avx2.And(isDeep, isSteep);

                    V256f gravelNoise;
                    if (Avx.TestZ(needsGravel, needsGravel))
                    {
                        gravelNoise = V256F.Zero;
                    }
                    else
                    {
                        var yGravel = V256F.Multiply(worldY, _v11);
                        gravelNoise = PerlinNoiseAvx2.Noise(
                            V256F.Add(V256F.Add(xGravel, yGravel), V256F.New(500f)),
                            V256F.Add(V256F.Subtract(zGravel, yGravel), V256F.New(500f))
                        );
                    }

                    // Bitcast the mask, don't numerically convert it -
                    // ConvertToVector256Int32 turns the true-lane bit pattern
                    // (NaN as a float) into 0x80000000 instead of -1.
                    V256i isGravel = Avx.CompareGreaterThan(gravelNoise, V256F.New(0.6f)).AsInt32();
                    isGravel = Avx2.And(isSteep, isGravel);

                    V256i surfaceID = BLOCK_GRASS_V;
                    surfaceID = V256I.IfElseFull(isSnowCap, BLOCK_SNOW_V, surfaceID);
                    surfaceID = V256I.IfElseFull(isSteep, stoneVariant, surfaceID);

                    V256i shallowID = BLOCK_DIRT_V;
                    shallowID = V256I.IfElseFull(isSnowCap, V256I.IfElseFull(Avx2.CompareEqual(depth, V256I.One), BLOCK_SNOW_V, BLOCK_STONE_V), shallowID);
                    shallowID = V256I.IfElseFull(isSteep, stoneVariant, shallowID);

                    V256i deepID = stoneVariant;
                    deepID = V256I.IfElseFull(isGravel, BLOCK_GRAVEL_V, deepID);

                    blockId = V256I.IfElseFull(isSurface, surfaceID, blockId);
                    blockId = V256I.IfElseFull(isShallow, shallowID, blockId);
                    blockId = V256I.IfElseFull(isDeep, deepID, blockId);

                    SetBlocksV256i(chunk, blocks, blockId, x, y, z);
                }
            }
        }
    }

    static V256f laneOffsets = Vector256.Create(0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f);

    private static int GetAirBlockCount(V256i row1, V256i row2, V256i row3, V256i row4)
    {
        var oRow1 = row1.CompareE(V256I.Zero);
        var oRow2 = row2.CompareE(V256I.Zero);
        var oRow3 = row3.CompareE(V256I.Zero);
        var oRow4 = row4.CompareE(V256I.Zero);

        uint mask1 = (uint)Avx.MoveMask(oRow1.AsSingle());
        uint mask2 = (uint)Avx.MoveMask(oRow2.AsSingle());
        uint mask3 = (uint)Avx.MoveMask(oRow3.AsSingle());
        uint mask4 = (uint)Avx.MoveMask(oRow4.AsSingle());

        uint combined = mask1 | (mask2 << 8) | (mask3 << 16) | (mask4 << 24);

        return Bit.PopCount(combined);
    }
 
    public static void Vector256PopulateOrderedY(VoxelChunk chunk, int baseX, int baseY, int baseZ, ChunkGeneration.CacheHandler<float> handler)
    {
        chunk.HasBlocks = true;

        var blocks = chunk.Blocks;
        var countMap = chunk.CountMap;

        float* height  = handler.Cache[0];
        float* heightX = handler.Cache[1];
        float* heightZ = handler.Cache[2];

        int index = 0;

        for (int z = 0; z < CHUNK_SIZE; z++)
        {  
            V256f worldZ = V256F.New(z + baseZ);
            V256i worldZI = worldZ.ToInt();

            V256f zGravel = worldZ * _v10;

            for (int x = 0; x < CHUNK_SIZE; x++)
            {  
                V256f worldX = V256F.New(x + baseX);
                V256i worldXI = worldX.ToInt();

                V256f xGravel = worldX * _v10;

                V256f terrainHeight  = V256F.New(height[index]);
                V256f terrainHeightX = V256F.New(heightX[index]);
                V256f terrainHeightZ = V256F.New(heightZ[index]);

                V256i terrainHeightI = terrainHeight.FloorToInt();

                V256f slope     = V256F.Abs(terrainHeight - terrainHeightX) + V256F.Abs(terrainHeight - terrainHeightZ);

                V256i isSteep   = slope.CompareG(TwoPFive).AsInt32();
                V256i isSnowCap = terrainHeight.CompareG(SnowLineV256f).AsInt32();

                for (int y = 0; y < CHUNK_SIZE; y+=8)
                {  
                    int blockIndex = y + x * 32 + z * 1024;

                    V256f worldY = V256F.New(y + baseY) + laneOffsets;
                    V256i worldYI = worldY.ToInt();

                    V256i isAir = worldYI.CompareG(terrainHeightI);

                    if (V256I.AllTrue(isAir))
                    {
                        *(V256u*)(blocks + blockIndex) = V256u.Zero;
                        continue;
                    }

                    V256i blockId = V256I.Zero;

                    V256i depth = terrainHeightI - worldYI;

                    V256i isSurface = depth.CompareE(V256I.Zero);
                    V256i isShallow = depth.CompareG(V256I.Zero) & depth.CompareLE(V256I.Three);

                    V256i isDeep = depth.CompareG(V256I.Three);

                    V256i stoneVariant = GetStoneVariantSimd(worldX, worldY, worldZ, terrainHeight);
                    V256i needsGravel = isDeep & isSteep;

                    V256f gravelNoise;
                    if (Avx.TestZ(needsGravel, needsGravel))
                    {
                        gravelNoise = V256F.Zero;
                    }
                    else
                    {
                        var yGravel = worldY * _v11;
                        gravelNoise = PerlinNoiseAvx2.Noise(xGravel + yGravel + V256F.New(500f), zGravel - yGravel + V256F.New(500f));
                    }

                    V256i isGravel = gravelNoise.CompareG(V256F.New(0.6f)).AsInt32();
                    isGravel = isSteep & isGravel;

                    V256i surfaceID = BLOCK_GRASS_V;
                    surfaceID = isSnowCap.IfThenElse(BLOCK_SNOW_V, surfaceID);
                    surfaceID = isSteep.IfThenElse(stoneVariant, surfaceID);

                    V256i shallowID = BLOCK_DIRT_V;
                    V256i snowStone = depth.CompareE(V256I.One).IfThenElse(BLOCK_SNOW_V, BLOCK_STONE_V);
                    shallowID = isSnowCap.IfThenElse(snowStone, shallowID);
                    shallowID = isSteep.IfThenElse(stoneVariant, shallowID);

                    V256i deepID = stoneVariant;
                    deepID = isGravel.IfThenElse(BLOCK_GRAVEL_V, deepID);

                    blockId = isSurface.IfThenElse(surfaceID, blockId);
                    blockId = isShallow.IfThenElse(shallowID, blockId);
                    blockId = isDeep.IfThenElse(deepID, blockId);

                    V256i isBottom = worldYI.CompareE(V256I.Zero);

                    blockId = isBottom.IfThenElse(BLOCK_STONE_V, blockId);

                    *(V256i*)(blocks + blockIndex) = blockId;
                }

                index++;
            }
        }
    }


    public static void Vector256PopulateOrderedYByte(VoxelChunk chunk, int baseX, int baseY, int baseZ, ChunkGeneration.CacheHandler<float> handler)
    {
        chunk.HasBlocks = true;

        var blocks = chunk.Blocks;
        var countMap = chunk.CountMap;

        float* height  = handler.Cache[0];
        float* heightX = handler.Cache[1];
        float* heightZ = handler.Cache[2];

        int index = 0;

        for (int z = 0; z < CHUNK_SIZE; z++)
        {  
            V256f worldZ = V256F.New(z + baseZ);
            V256i worldZI = worldZ.ToInt();

            V256f zGravel = worldZ * _v10;

            for (int x = 0; x < CHUNK_SIZE; x++)
            {  
                V256f worldX = V256F.New(x + baseX);
                V256i worldXI = worldX.ToInt();

                V256f xGravel = worldX * _v10;

                V256f terrainHeight  = V256F.New(height[index]);
                V256f terrainHeightX = V256F.New(heightX[index]);
                V256f terrainHeightZ = V256F.New(heightZ[index]);

                V256i terrainHeightI = terrainHeight.FloorToInt();

                V256f slope     = V256F.Abs(terrainHeight - terrainHeightX) + V256F.Abs(terrainHeight - terrainHeightZ);

                V256i isSteep   = slope.CompareG(TwoPFive).AsInt32();
                V256i isSnowCap = terrainHeight.CompareG(SnowLineV256f).AsInt32();


                V256i row1 = GenerateBlocksOrderedYByte(x, 0,  z, baseX, baseY, baseZ, worldX, worldZ, terrainHeight, terrainHeightI, isSteep, isSnowCap, xGravel, zGravel);
                V256i row2 = GenerateBlocksOrderedYByte(x, 8,  z, baseX, baseY, baseZ, worldX, worldZ, terrainHeight, terrainHeightI, isSteep, isSnowCap, xGravel, zGravel);
                V256i row3 = GenerateBlocksOrderedYByte(x, 16, z, baseX, baseY, baseZ, worldX, worldZ, terrainHeight, terrainHeightI, isSteep, isSnowCap, xGravel, zGravel);
                V256i row4 = GenerateBlocksOrderedYByte(x, 24, z, baseX, baseY, baseZ, worldX, worldZ, terrainHeight, terrainHeightI, isSteep, isSnowCap, xGravel, zGravel);

                

                V256b row = VH.ShortenToV256b(row1, row2, row3, row4);

                *(V256b*)(chunk.ByteBlocks + x * 32 + z * 1024) = row;

                index++;
            }
        }
    }


    public static V256i GenerateBlocksOrderedYByte(
        int x, int y, int z, 
        int baseX, int baseY, int baseZ, 
        V256f worldX, V256f worldZ, 
        V256f terrainHeight, V256i terrainHeightI,
        V256i isSteep, V256i isSnowCap,
        V256f xGravel, V256f zGravel)
    {
        V256f worldY = V256F.New(y + baseY) + laneOffsets;
        V256i worldYI = worldY.ToInt();

        V256i isAir = worldYI.CompareG(terrainHeightI);

        if (V256I.AllTrue(isAir))
        {
            return V256I.Zero;
        }

        V256i blockId = V256I.Zero;

        V256i depth = terrainHeightI - worldYI;

        V256i isSurface = depth.CompareE(V256I.Zero);
        V256i isShallow = depth.CompareG(V256I.Zero) & depth.CompareLE(V256I.Three);

        V256i isDeep = depth.CompareG(V256I.Three);

        V256i stoneVariant = GetStoneVariantSimd(worldX, worldY, worldZ, terrainHeight);
        V256i needsGravel = isDeep & isSteep;

        V256f gravelNoise;
        if (Avx.TestZ(needsGravel, needsGravel))
        {
            gravelNoise = V256F.Zero;
        }
        else
        {
            var yGravel = worldY * _v11;
            gravelNoise = PerlinNoiseAvx2.Noise(xGravel + yGravel + V256F.New(500f), zGravel - yGravel + V256F.New(500f));
        }

        V256i isGravel = gravelNoise.CompareG(V256F.New(0.6f)).AsInt32();
        isGravel = isSteep & isGravel;

        V256i surfaceID = BLOCK_GRASS_V;
        surfaceID = isSnowCap.IfThenElse(BLOCK_SNOW_V, surfaceID);
        surfaceID = isSteep.IfThenElse(stoneVariant, surfaceID);

        V256i shallowID = BLOCK_DIRT_V;
        V256i snowStone = depth.CompareE(V256I.One).IfThenElse(BLOCK_SNOW_V, BLOCK_STONE_V);
        shallowID = isSnowCap.IfThenElse(snowStone, shallowID);
        shallowID = isSteep.IfThenElse(stoneVariant, shallowID);

        V256i deepID = stoneVariant;
        deepID = isGravel.IfThenElse(BLOCK_GRAVEL_V, deepID);

        blockId = isSurface.IfThenElse(surfaceID, blockId);
        blockId = isShallow.IfThenElse(shallowID, blockId);
        blockId = isDeep.IfThenElse(deepID, blockId);

        V256i isBottom = worldYI.CompareE(V256I.Zero);

        blockId = isBottom.IfThenElse(BLOCK_STONE_V, blockId);

        return blockId;
    }


    public static void Vector256PopulateOrderedBlockMap(VoxelChunk chunk, int baseX, int baseY, int baseZ, ChunkGeneration.CacheHandler<V256f> handler)
    {
        chunk.HasBlocks = true;
        var blockMap = chunk.BlockMap;
        var countMap = chunk.CountMap;

        for (int y = 0; y < CHUNK_SIZE; y++)
        {  
            for (int z = 0; z < CHUNK_SIZE; z++)
            {
                var row1 = GenerateBlocksOrdered(chunk,  0, y, z, baseX, baseY, baseZ, handler);
                var row2 = GenerateBlocksOrdered(chunk,  8, y, z, baseX, baseY, baseZ, handler);
                var row3 = GenerateBlocksOrdered(chunk, 16, y, z, baseX, baseY, baseZ, handler);
                var row4 = GenerateBlocksOrdered(chunk, 24, y, z, baseX, baseY, baseZ, handler);

                var blockCount = 32 - GetAirBlockCount(row1, row2, row3, row4);

                int zy = z + y * 32;

                var count = countMap[zy];
                countMap[zy] = (byte)blockCount;
                
                if (blockCount == 0)
                {
                    if (count != 0)
                    {
                        MemoryHelper.Free(blockMap[zy]);
                        blockMap[zy] = VoxelChunk.EmptyMap[zy];
                    }
                    // else: was already air, still air — do nothing
                }
                else if (count == 0)
                {
                    var row = blockMap[zy] = MemoryHelper.Alloc<Block>(32);
                    *(V256i*)(row +  0) = row1;
                    *(V256i*)(row +  8) = row2;
                    *(V256i*)(row + 16) = row3;
                    *(V256i*)(row + 24) = row4;
                }
                else
                {
                    var row = blockMap[zy];
                    *(V256i*)(row +  0) = row1;
                    *(V256i*)(row +  8) = row2;
                    *(V256i*)(row + 16) = row3;
                    *(V256i*)(row + 24) = row4;
                }
                // else do nothing
            }
        }
    }

    private static V256i GenerateBlocksOrdered(VoxelChunk chunk, int x, int y, int z, int baseX, int baseY, int baseZ, ChunkGeneration.CacheHandler<V256f> handler)
    {
        V256f* height  = handler.Cache[0];
        V256f* heightX = handler.Cache[1];
        V256f* heightZ = handler.Cache[2];

        // X
        V256f worldX = V256F.Add(V256F.New(x + baseX), laneOffsets);
        V256i worldXI = Avx.ConvertToVector256Int32(worldX);

        V256f xGravel = V256F.Multiply(worldX, _v10);

        // Y
        V256f worldY = V256F.New(y + baseY);
        V256i worldYI = Avx.ConvertToVector256Int32(worldY);

        // Z
        V256f worldZ = V256F.New(z + baseZ);
        V256i worldZI = Avx.ConvertToVector256Int32(worldZ);

        V256f zGravel = V256F.Multiply(worldZ, _v10);


        // Get heightmap data
        int idx = (x >> 3) + (z * 4);

        V256f terrainHeightF = height[idx];
        V256i terrainHeight = V256I.FloorToInt(terrainHeightF);

        V256f slope = V256F.Add(V256F.Abs(terrainHeightF - heightX[idx]), V256F.Abs(V256F.Subtract(terrainHeightF, heightZ[idx])));
        V256i isSteep = Avx.CompareGreaterThan(slope, TwoPFive).AsInt32();
        V256i isSnowCap = Avx.CompareGreaterThan(terrainHeightF, SnowLineV256f).AsInt32();

        
        // generation code
        if (y == 0 && chunk.WorldPosition.Y == 0)
        {
            SetBlocksV256i2(chunk, BLOCK_STONE_V, x, y, z);
            return BLOCK_STONE_V;
        }

        V256i isAir = Avx2.CompareGreaterThan(worldYI, terrainHeight);

        if (V256I.AllTrue(isAir))
        {
            SetBlocksV256i2(chunk, V256I.Zero, x, y, z);
            return V256I.Zero;
        }

        // Derive worldY fresh from worldYI every iteration instead
        // of maintaining a separate accumulator - keeps it from
        // ever drifting out of sync after an air-skip above.
        

        V256i blockId = V256I.Zero;

        V256i depth = V256I.Subtract(terrainHeight, worldYI);

        V256i isSurface = Avx2.CompareEqual(depth, V256I.Zero);
        V256i isShallow = Avx2.And(
            Avx2.CompareGreaterThan(depth, V256I.Zero),
            V256I.CompareLessThanOrEqual(depth, V256I.Three)
        );
        V256i isDeep = Avx2.CompareGreaterThan(depth, V256I.New(3));

        V256i stoneVariant = GetStoneVariantSimd(worldX, worldY, worldZ, terrainHeightF);
        V256i needsGravel = Avx2.And(isDeep, isSteep);

        V256f gravelNoise;
        if (Avx.TestZ(needsGravel, needsGravel))
        {
            gravelNoise = V256F.Zero;
        }
        else
        {
            var yGravel = V256F.Multiply(worldY, _v11);
            gravelNoise = PerlinNoiseAvx2.Noise(
                V256F.Add(V256F.Add(xGravel, yGravel), V256F.New(500f)),
                V256F.Add(V256F.Subtract(zGravel, yGravel), V256F.New(500f))
            );
        }

        V256i isGravel = Avx.CompareGreaterThan(gravelNoise, V256F.New(0.6f)).AsInt32();
        isGravel = Avx2.And(isSteep, isGravel);

        V256i surfaceID = BLOCK_GRASS_V;
        surfaceID = V256I.IfElseFull(isSnowCap, BLOCK_SNOW_V, surfaceID);
        surfaceID = V256I.IfElseFull(isSteep, stoneVariant, surfaceID);

        V256i shallowID = BLOCK_DIRT_V;
        shallowID = V256I.IfElseFull(isSnowCap, V256I.IfElseFull(Avx2.CompareEqual(depth, V256I.One), BLOCK_SNOW_V, BLOCK_STONE_V), shallowID);
        shallowID = V256I.IfElseFull(isSteep, stoneVariant, shallowID);

        V256i deepID = stoneVariant;
        deepID = V256I.IfElseFull(isGravel, BLOCK_GRAVEL_V, deepID);

        blockId = V256I.IfElseFull(isSurface, surfaceID, blockId);
        blockId = V256I.IfElseFull(isShallow, shallowID, blockId);
        blockId = V256I.IfElseFull(isDeep, deepID, blockId);

        SetBlocksV256i2(chunk, blockId, x, y, z);
        return blockId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetBlocksV256i(VoxelChunk chunk, Block* blockPtr, V256i blocks, int x, int y, int z)
    {
        const uint INV_8_BIT_MASK = ~0xFFu;
        int blockIndex = x + z * 32 + y * 1024;

        *(V256i*)(blockPtr + blockIndex) = blocks;

        var isAir = blocks.CompareE(V256I.Zero);
        uint mask = ~isAir.GetExtractedMSB();
        uint old = chunk.SolidMap[z + y * 32] & (INV_8_BIT_MASK << x);
        chunk.SolidMap[z + y * 32] = old | (mask << x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetBlocksV256i2(VoxelChunk chunk, V256i blocks, int x, int y, int z)
    {
        const uint INV_8_BIT_MASK = ~0xFFu;
        var isAir = blocks.CompareE(V256I.Zero);
        uint mask = ~isAir.GetExtractedMSB();
        uint old = chunk.SolidMap[z + y * 32] & (INV_8_BIT_MASK << x);
        chunk.SolidMap[z + y * 32] = old | (mask << x);
    }
}