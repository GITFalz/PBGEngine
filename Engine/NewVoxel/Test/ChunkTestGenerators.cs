using PBG.MathLibrary;
using PBG.NewVoxel;
using PBG.Noise;

/// <summary>
/// A grab-bag of chunk generators for stress-testing the voxel renderer.
/// Each one hits different render cases: flat surfaces, curved surfaces, thin
/// slivers, overhangs, disconnected floating chunks, high-frequency noise, etc.
///
/// Usage: call any Generate_X(chunk) from wherever you currently do the
/// flat-plane test fill.
/// </summary>
public static class ChunkTestGenerators
{
    private const int ChunkSize = 32;

    // Flip this if terrain repeats identically every single chunk (a dead
    // giveaway that WorldPosition is chunk-index space, not block space).
    private const bool WORLD_POS_IS_CHUNK_SPACE = false;

    private static readonly Block Solid = new Block(BlockState.Solid, 1);

        // ---------------------------------------------------------------
    // Lookup table for UI generation — button label -> generator function.
    // Loop over this to spawn one button per test.
    // ---------------------------------------------------------------
    public static readonly Dictionary<string, Action<VoxelChunk>> Generators = new Dictionary<string, Action<VoxelChunk>>
    {
        { "Noise Heightmap",       Generate_NoiseHeightmap },
        { "Noise FBM Mountains",   Generate_NoiseFbmMountains },
        { "Noise 3D Caves",        Generate_Noise3DCaves },
        { "Voronoi Cellular Terrain", Generate_VoronoiCellularTerrain },
        { "Voronoi Edge Canyons",  Generate_VoronoiEdgeCanyons },
        { "Voronoi Crystal Pillars", Generate_VoronoiCrystalPillars },
        { "Voronoi Checker Floor", Generate_VoronoiCheckerFloor },
        { "Voronoi Biomes",        Generate_VoronoiBiomes },
        { "Voronoi Worley Flow",   Generate_VoronoiWorleyFlow },
        { "Floating Islands",      Generate_FloatingIslands },
        { "Sphere Control",        Generate_SphereControl },
    };


    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private static (int baseX, int baseY, int baseZ) GetWorldOrigin(VoxelChunk chunk)
    {
        int bx = chunk.WorldPosition.X;
        int by = chunk.WorldPosition.Y;
        int bz = chunk.WorldPosition.Z;

        if (WORLD_POS_IS_CHUNK_SPACE)
        {
            bx *= ChunkSize;
            by *= ChunkSize;
            bz *= ChunkSize;
        }

        return (bx, by, bz);
    }

    private static void FillColumn(VoxelChunk chunk, int x, int z, int localHeight)
    {
        localHeight = Clamp(localHeight, 0, ChunkSize);
        for (int y = 0; y < localHeight; y++)
            chunk[x, y, z] = Solid;
    }

    private static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);

    private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

    // ---------------------------------------------------------------
    // 1. Noise heightmap terrain — classic 2-param noise as a heightmap.
    //    Good baseline test: rolling hills, no overhangs, checks that
    //    your normals/lighting look right on smooth curved slopes.
    // ---------------------------------------------------------------
    public static void Generate_NoiseHeightmap(VoxelChunk chunk)
    {
        var (bx, by, bz) = GetWorldOrigin(chunk);
        const float frequency = 0.02f;
        const float amplitude = 20f;
        const int baseHeight = 8;

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int z = 0; z < ChunkSize; z++)
            {
                float wx = (bx + x) * frequency;
                float wz = (bz + z) * frequency;

                float n = NoiseLib.Noise(wx, wz); // assume -1..1
                int worldHeight = baseHeight + (int)(n * amplitude);

                int localHeight = worldHeight - by;
                FillColumn(chunk, x, z, localHeight);
            }
        }
    }

    // ---------------------------------------------------------------
    // 2. FBM mountains — layered noise octaves. Tests high-frequency
    //    detail rendering and steep slopes/cliffs.
    // ---------------------------------------------------------------
    public static void Generate_NoiseFbmMountains(VoxelChunk chunk)
    {
        var (bx, by, bz) = GetWorldOrigin(chunk);
        const int baseHeight = 10;

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int z = 0; z < ChunkSize; z++)
            {
                float wx = bx + x;
                float wz = bz + z;

                float amplitude = 1f;
                float frequency = 0.01f;
                float sum = 0f;
                float maxAmp = 0f;

                for (int octave = 0; octave < 4; octave++)
                {
                    sum += NoiseLib.Noise(wx * frequency, wz * frequency) * amplitude;
                    maxAmp += amplitude;
                    amplitude *= 0.5f;
                    frequency *= 2f;
                }

                float n = sum / maxAmp; // normalized -1..1
                int worldHeight = baseHeight + (int)(n * n * n * 40f); // cubed for sharper peaks

                int localHeight = worldHeight - by;
                FillColumn(chunk, x, z, localHeight);
            }
        }
    }

    // ---------------------------------------------------------------
    // 3. 3D noise caves — density field carved by 3-param noise.
    //    Tests overhangs, tunnels, and internal geometry culling.
    //    Best with sizeY > 8 chunks tall so caves have room to breathe.
    // ---------------------------------------------------------------
    public static void Generate_Noise3DCaves(VoxelChunk chunk)
    {
        var (bx, by, bz) = GetWorldOrigin(chunk);
        const float frequency = 0.06f;
        const float threshold = 0.15f;

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    float wx = (bx + x) * frequency;
                    float wy = (by + y) * frequency;
                    float wz = (bz + z) * frequency;

                    float n = NoiseLib.Noise(wx, wy, wz);

                    // solid everywhere except where density dips below threshold
                    if (n > threshold)
                        chunk[x, y, z] = Solid;
                }
            }
        }
    }

    // ---------------------------------------------------------------
    // 4. Voronoi cellular terrain — flat-topped cell plateaus.
    //    Good for testing hard edges / sharp normal discontinuities,
    //    as opposed to the smooth curves noise gives you.
    // ---------------------------------------------------------------
    public static void Generate_VoronoiCellularTerrain(VoxelChunk chunk)
    {
        var (bx, by, bz) = GetWorldOrigin(chunk);
        const float frequency = 0.03f;
        const int baseHeight = 6;

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int z = 0; z < ChunkSize; z++)
            {
                Vector2 p = new Vector2((bx + x) * frequency, (bz + z) * frequency);
                float v = VoronoiLib.Voronoi(p, out _); // ~0..1 per-cell value

                int worldHeight = baseHeight + (int)(v * 24f);
                int localHeight = worldHeight - by;
                FillColumn(chunk, x, z, localHeight);
            }
        }
    }

    // ---------------------------------------------------------------
    // 5. Voronoi edge canyons — VoronoiF2 as a ridge/canyon network.
    //    Sharp thin walls are a great edge-case for face culling bugs.
    // ---------------------------------------------------------------
    public static void Generate_VoronoiEdgeCanyons(VoxelChunk chunk)
    {
        var (bx, by, bz) = GetWorldOrigin(chunk);
        const float frequency = 0.025f;
        const int baseHeight = 16;

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int z = 0; z < ChunkSize; z++)
            {
                Vector2 p = new Vector2((bx + x) * frequency, (bz + z) * frequency);
                float f2 = VoronoiLib.VoronoiF2(p, out _);

                // cell edges (f2 near 0) become deep canyons, cell centers stay high
                int worldHeight = baseHeight - (int)((1f - Clamp01(f2)) * 20f);
                int localHeight = worldHeight - by;
                FillColumn(chunk, x, z, localHeight);
            }
        }
    }

    // ---------------------------------------------------------------
    // 6. Voronoi crystal pillars — VoronoiDistance thresholded into
    //    thin full-height spikes near cell borders. Tests thin geometry,
    //    isolated floating pieces, and lots of exposed faces at once.
    // ---------------------------------------------------------------
    public static void Generate_VoronoiCrystalPillars(VoxelChunk chunk)
    {
        var (bx, by, bz) = GetWorldOrigin(chunk);
        const float frequency = 0.05f;
        const float wallThreshold = 0.06f;

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int z = 0; z < ChunkSize; z++)
            {
                Vector2 p = new Vector2((bx + x) * frequency, (bz + z) * frequency);
                float d = VoronoiLib.VoronoiDistance(p, out _);

                if (d < wallThreshold)
                {
                    // pillar height varies with distance for visual interest
                    int worldHeight = 4 + (int)((wallThreshold - d) / wallThreshold * 28f);
                    int localHeight = worldHeight - by;
                    FillColumn(chunk, x, z, localHeight);
                }
            }
        }
    }

    // ---------------------------------------------------------------
    // 7. Voronoi checkerboard floor — VoronoiChecker as an alternating
    //    flat pattern. Cheap sanity check for texture/material tiling
    //    and perfectly flat-surface lighting.
    // ---------------------------------------------------------------
    public static void Generate_VoronoiCheckerFloor(VoxelChunk chunk)
    {
        var (bx, by, bz) = GetWorldOrigin(chunk);
        const float frequency = 0.08f;

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int z = 0; z < ChunkSize; z++)
            {
                Vector2 p = new Vector2((bx + x) * frequency, (bz + z) * frequency);
                float c = VoronoiLib.VoronoiChecker(p, out _);

                int worldHeight = c > 0.5f ? 6 : 3; // two flat height bands
                int localHeight = worldHeight - by;
                FillColumn(chunk, x, z, localHeight);
            }
        }
    }

    // ---------------------------------------------------------------
    // 8. Voronoi color biomes — Voronoi3 drives both height AND block id
    //    per cell, so neighboring cells look visibly different. Tests
    //    chunk-boundary seams between differently-shaded regions.
    // ---------------------------------------------------------------
    public static void Generate_VoronoiBiomes(VoxelChunk chunk)
    {
        var (bx, by, bz) = GetWorldOrigin(chunk);
        const float frequency = 0.015f;

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int z = 0; z < ChunkSize; z++)
            {
                Vector2 p = new Vector2((bx + x) * frequency, (bz + z) * frequency);
                Vector3 c = VoronoiLib.Voronoi3(p, out _);

                // use one channel of the cell color as a per-biome height offset
                int worldHeight = 5 + (int)(c.X * 18f);

                // NOTE: swap id 1 for whatever block ids you actually have per biome
                ushort blockId = (ushort)(1 + (int)(c.Y * 3f));
                int localHeight = worldHeight - by;

                for (int y = 0; y < Clamp(localHeight, 0, ChunkSize); y++)
                    chunk[x, y, z] = new Block(BlockState.Solid, blockId);
            }
        }
    }

    // ---------------------------------------------------------------
    // 9. Worley-flow terrain — VoronoiWF gives a flowing/warped variant.
    //    Distinct silhouette from the plain cellular test; good for
    //    eyeballing whether two "similar" generators actually read
    //    differently on your renderer.
    // ---------------------------------------------------------------
    public static void Generate_VoronoiWorleyFlow(VoxelChunk chunk)
    {
        var (bx, by, bz) = GetWorldOrigin(chunk);
        const float frequency = 0.02f;
        const int baseHeight = 10;

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int z = 0; z < ChunkSize; z++)
            {
                Vector2 p = new Vector2((bx + x) * frequency, (bz + z) * frequency);
                float wf = VoronoiLib.VoronoiWF(p, out _);

                int worldHeight = baseHeight + (int)(wf * 22f);
                int localHeight = worldHeight - by;
                FillColumn(chunk, x, z, localHeight);
            }
        }
    }

    // ---------------------------------------------------------------
    // 10. Floating islands — noise heightmap masked by voronoi distance
    //     so each cell becomes a separate floating landmass with caves
    //     carved by 3D noise underneath. Combines everything at once;
    //     good final "does it all work together" stress test.
    //     Wants sizeY > 8 chunks so islands can actually float with
    //     open air above and below.
    // ---------------------------------------------------------------
    public static void Generate_FloatingIslands(VoxelChunk chunk)
    {
        var (bx, by, bz) = GetWorldOrigin(chunk);
        const float islandFreq = 0.015f;
        const float caveFreq = 0.05f;
        const float islandThreshold = 0.12f;
        const int islandCenterY = 60;
        const int islandThickness = 14;

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int z = 0; z < ChunkSize; z++)
            {
                Vector2 vp = new Vector2((bx + x) * islandFreq, (bz + z) * islandFreq);
                float d = VoronoiLib.VoronoiDistance(vp, out _);

                if (d > islandThreshold)
                    continue; // gap between islands — leave empty

                float shapeFactor = 1f - (d / islandThreshold); // 1 at center, 0 at edge
                int thickness = (int)(islandThickness * shapeFactor);
                int top = islandCenterY + thickness / 2;
                int bottom = islandCenterY - thickness / 2;

                for (int y = 0; y < ChunkSize; y++)
                {
                    int worldY = by + y;
                    if (worldY < bottom || worldY > top)
                        continue;

                    float wx = (bx + x) * caveFreq;
                    float wy = worldY * caveFreq;
                    float wz = (bz + z) * caveFreq;
                    float cave = NoiseLib.Noise(wx, wy, wz);

                    if (cave > 0.2f) // carve pockets out of the island
                        chunk[x, y, z] = Solid;
                }
            }
        }
    }

    // ---------------------------------------------------------------
    // 11. Sine sphere — no noise libs at all, pure math. Not "cool" but
    //     useful as a ground-truth control: if this renders wrong,
    //     the bug is in your mesher/renderer, not the generators above.
    // ---------------------------------------------------------------
    public static void Generate_SphereControl(VoxelChunk chunk)
    {
        var (bx, by, bz) = GetWorldOrigin(chunk);
        const float radius = 40f;
        Vector3 center = new Vector3(0f, 80f, 0f);

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    float dx = (bx + x) - center.X;
                    float dy = (by + y) - center.Y;
                    float dz = (bz + z) - center.Z;

                    float distSq = dx * dx + dy * dy + dz * dz;
                    if (distSq <= radius * radius)
                        chunk[x, y, z] = Solid;
                }
            }
        }
    }
}