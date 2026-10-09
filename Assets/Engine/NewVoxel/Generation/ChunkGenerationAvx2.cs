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
    static V256i BLOCK_SAND_V;
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

    static readonly V256f _v8 = V256F.New(0.002f);



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
        BLOCK_SAND_V = GetBlock("sand_block");
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


    public static void Vector256Height(VoxelChunk chunk, int baseX, int baseZ, ChunkGeneration.CacheHandler<float> handler)
    {
        float* height  = handler.Cache[0];
        float* heightX = handler.Cache[1];
        float* heightZ = handler.Cache[2];

        int lodLevel = chunk.LodMult;

        V256f one = V256F.One;
        V256f xOffsets = V256.Create(0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f) * lodLevel;

        int ptrIndex = 0;

        // Process 8 columns at a time in X
        for (int z = 0; z < CHUNK_SIZE; z++)
        {
            V256f wz = V256.Create((float)(baseZ + z * lodLevel));

            for (int x = 0; x < CHUNK_SIZE; x += 8)
            {
                V256f wx = V256F.Add(V256F.New((float)(baseX + x * lodLevel)), xOffsets);
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
 

    public static void Vector256PopulateOrderedYByte(byte* blocks, int lodLevel, int baseX, int baseY, int baseZ, ChunkGeneration.CacheHandler<float> handler)
    {
        float* height  = handler.Cache[0];
        float* heightX = handler.Cache[1];
        float* heightZ = handler.Cache[2];

        int index = 0;

        int yIncrement = 8 * lodLevel;

        V256f laneOffsets = Vector256.Create(0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f) * lodLevel;

        V256i lodV = V256I.New(lodLevel - 1);
        V256i invLodV = lodV ^ V256U.MaxValue.AsInt32();

        var rows = stackalloc V256i[4];

        for (int z = 0; z < CHUNK_SIZE; z++)
        {  
            V256f worldZ = V256F.New(z * lodLevel + baseZ);
            V256i worldZI = worldZ.ToInt();

            V256f zGravel = worldZ * _v10;

            for (int x = 0; x < CHUNK_SIZE; x++)
            {  
                V256f worldX = V256F.New(x * lodLevel + baseX);
                V256i worldXI = worldX.ToInt();

                V256f xGravel = worldX * _v10;

                V256f terrainHeight  = V256F.New(height[index]);
                V256f terrainHeightX = V256F.New(heightX[index]);
                V256f terrainHeightZ = V256F.New(heightZ[index]);

                V256i terrainHeightI = terrainHeight.FloorToInt();

                V256f slope     = V256F.Abs(terrainHeight - terrainHeightX) + V256F.Abs(terrainHeight - terrainHeightZ);

                V256i isSteep   = slope.CompareG(TwoPFive).AsInt32();
                V256i isSnowCap = terrainHeight.CompareG(SnowLineV256f).AsInt32();

                for (int i = 0; i < 4; i++)
                {
                    int y = i * yIncrement;

                    V256f worldY = V256F.New(y + baseY) + laneOffsets;
                    V256i worldYI = worldY.ToInt();

                    V256i isAir = worldYI.CompareG(terrainHeightI);

                    if (V256I.AllTrue(isAir))
                    {
                        rows[i] = V256I.Zero;
                        continue;
                    }

                    V256i blockId = V256I.Zero;

                    V256i depth = (terrainHeightI & invLodV) - worldYI;

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

                    rows[i] = blockId;
                }

                V256b row = VH.ShortenToV256b(rows[0], rows[1], rows[2], rows[3]);

                *(V256b*)(blocks + x * 32 + z * 1024) = row;

                index++;
            }
        }
    }

    public static void HeightVoronoiTest(VoxelChunk chunk, int baseX, int baseZ, ChunkGeneration.CacheHandler<float> handler)
    {
        float* height  = handler.Cache[0];
        int lodLevel = chunk.LodMult;

        V256f one = V256F.One;
        V256f xOffsets = V256.Create(0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f) * lodLevel;

        int ptrIndex = 0;

        V256f SmoothLerp(V256f min, V256f max, V256f value, float smoothing)
        {
            var t = ((value - min) / (max - min)).Clamp01();

            // Smoothstep
            var smooth = t * t * (V256F.New(3f) - V256F.New(2f) * t);

            // Blend between linear and smooth
            t = V256F.Lerp(t, smooth, V256F.New(smoothing));

            return min + (max - min) * t;
        }

        V256f GetMountainMask(V256f x, V256f y)
        {
            var n0 = PerlinNoiseAvx2.Noise01(
                x * V256F.New(0.0005f),
                y * V256F.New(0.0005f));

            var n1 = PerlinNoiseAvx2.Noise01(
                x * V256F.New(0.001f),
                y * V256F.New(0.001f));

            var n2 = PerlinNoiseAvx2.Noise01(
                x * V256F.New(0.004f),
                y * V256F.New(0.004f));

            var mountainNoise =
                n0 * V256F.New(0.65f) +
                n1 * V256F.New(0.25f) +
                n2 * V256F.New(0.10f);

            return SmoothLerp(
                V256F.New(0f),
                V256F.New(0.3f),
                mountainNoise, 
                1f);
        }

        V256f MountainNoise(V256f x, V256f y)
        {
            V256f noise = V256F.Zero;

            V256f amplitude = V256F.One;
            V256f frequency = V256F.New(0.0015f);

            // Large mountain formations
            noise += PerlinNoiseAvx2.Noise01(x * frequency, y * frequency) * amplitude;

            amplitude *= V256F.New(0.5f);
            frequency *= V256F.New(2.0f);

            // Medium-scale formations
            noise += PerlinNoiseAvx2.Noise01(x * frequency, y * frequency) * amplitude;

            amplitude *= V256F.New(0.5f);
            frequency *= V256F.New(2.0f);

            // Mountain detail
            noise += PerlinNoiseAvx2.Noise01(x * frequency, y * frequency) * amplitude;

            amplitude *= V256F.New(0.5f);
            frequency *= V256F.New(2.0f);

            // Small detail
            noise += PerlinNoiseAvx2.Noise01(x * frequency, y * frequency) * amplitude;

            // Normalize because amplitudes sum to 1.875
            noise *= V256F.New(1.0f / 1.875f);

            // Make high areas more mountainous
            noise *= noise;

            return noise;
        }

        // Process 8 columns at a time in X
        for (int z = 0; z < CHUNK_SIZE; z++)
        {
            V256f wz = V256.Create((float)(baseZ + z * lodLevel));

            for (int x = 0; x < CHUNK_SIZE; x += 8)
            {
                V256f wx = V256F.Add(V256F.New((float)(baseX + x * lodLevel)), xOffsets);

                var displacementx = PerlinNoiseAvx2.Noise01(wx * V256F.New(0.1f), wz * V256F.New(0.1f));
                var displacementy = PerlinNoiseAvx2.Noise01(wx * V256F.New(0.1f) + V256F.New(1000), wz * V256F.New(0.1f) + V256F.New(1000));

                displacementx *= V256F.New(0.25f);
                displacementy *= V256F.New(0.25f);

                //var terrainHeight = MountainNoise(worldX, worldZ);
                var terrainNoise = VoronoiNoiseAvx2.DistanceVoronoi(wx * V256F.New(0.02f) + displacementx, wz * V256F.New(0.02f) + displacementy);
                //terrainNoise *= V256F.One - VoronoiNoiseAvx2.EdgeVoronoi(worldX * V256F.New(0.02f), worldZ * V256F.New(0.02f));
                terrainNoise *= V256F.New(0.25f);

                var terrainHeight = MountainNoise(wx, wz);
                
                terrainNoise *= (terrainHeight - V256F.New(0.2f)).Clamp01();

                terrainHeight += terrainNoise;
                terrainHeight *= V256F.New(500f);

                var mountainMask = GetMountainMask(wx, wz);

                var plainsNoise = MountainNoise(wx + displacementx, wz + displacementy) * V256F.New(5f);

                terrainHeight = V256F.Lerp(V256F.New(50) + plainsNoise, terrainHeight, mountainMask);
                terrainHeight += V256F.New(65);

                *(V256f*)(height  + ptrIndex) = terrainHeight;

                ptrIndex += 8;
            }
        }
    }

    /*
    public static void PopulateVoronoiTest(byte* blocks, int lodLevel, int baseX, int baseY, int baseZ)
    {
        float* height  = handler.Cache[0];
        int index = 0;

        int yIncrement = 8 * lodLevel;

        V256f laneOffsets = Vector256.Create(0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f) * lodLevel;

        V256i lodV = V256I.New(lodLevel - 1);
        V256i invLodV = lodV ^ V256U.MaxValue.AsInt32();

        var rows = stackalloc V256i[4];

        for (int z = 0; z < CHUNK_SIZE; z++)
        {  
            V256f worldZ = V256F.New(z * lodLevel + baseZ);
            V256i worldZI = worldZ.ToInt();

            for (int x = 0; x < CHUNK_SIZE; x++)
            {  
                V256f worldX = V256F.New(x * lodLevel + baseX);
                V256i worldXI = worldX.ToInt();

                var terrainHeight = V256F.New(height[index]);
                terrainHeight *= V256F.New(150);
                var terrainHeightI = terrainHeight.ToInt();

                // height code

                for (int i = 0; i < 4; i++)
                {
                    int y = i * yIncrement;

                    V256f worldY = V256F.New(y + baseY) + laneOffsets;
                    V256i worldYI = worldY.ToInt();

                    V256i isSolid = terrainHeightI.CompareGE(worldYI);

                    V256i block = isSolid.IfThenElse(BLOCK_STONE_V, V256I.Zero);

                    rows[i] = block;

                    // block code
                }

                V256b row = VH.ShortenToV256b(rows[0], rows[1], rows[2], rows[3]);

                *(V256b*)(blocks + x * 32 + z * 1024) = row;

                index++;
            }
        }
    }
    */

    public static void PopulateVoronoiTest(byte* blocks, int lodLevel, int baseX, int baseY, int baseZ, ChunkGeneration.CacheHandler<float> handler)
    {
        var height = handler.Cache[0];
        int index = 0;

        int yIncrement = 8 * lodLevel;

        V256f laneOffsets = Vector256.Create(0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f) * lodLevel;

        V256i lodV = V256I.New(lodLevel - 1);
        V256i invLodV = lodV ^ V256U.MaxValue.AsInt32();

        var rows = stackalloc V256i[4];

        for (int z = 0; z < CHUNK_SIZE; z++)
        {  
            V256f worldZ = V256F.New(z * lodLevel + baseZ);
            V256i worldZI = worldZ.ToInt();

            for (int x = 0; x < CHUNK_SIZE; x++)
            {  
                V256f worldX = V256F.New(x * lodLevel + baseX);
                V256i worldXI = worldX.ToInt();

                var terrainHeight = V256F.New(height[index]);
                var terrainHeightI = terrainHeight.ToInt() & invLodV;

                // height code

                for (int i = 0; i < 4; i++)
                {
                    int y = i * yIncrement;

                    V256f worldY = V256F.New(y + baseY) + laneOffsets;
                    V256i worldYI = worldY.ToInt();

                    V256i isSolid = terrainHeightI.CompareGE(worldYI);

                    V256i block = isSolid.IfThenElse(BLOCK_STONE_V, V256I.Zero);

                    rows[i] = block;
                }

                V256b row = VH.ShortenToV256b(rows[0], rows[1], rows[2], rows[3]);

                *(V256b*)(blocks + x * 32 + z * 1024) = row;

                index++;
            }
        }
    }
}
