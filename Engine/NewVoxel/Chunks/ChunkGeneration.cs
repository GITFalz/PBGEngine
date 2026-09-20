using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using PBG.Core;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Noise;

namespace PBG.NewVoxel;

[InternalSystemInit(InitPriority.ScriptSystem)]
public unsafe static class ChunkGeneration
{
    private static Stopwatch _sw = Stopwatch.StartNew();

    const int CHUNK_SIZE = 32;


    // Block palette
    static int BLOCK_TEST;
    static int BLOCK_GRASS;
    static int BLOCK_DIRT;
    static int BLOCK_STONE;
    static int BLOCK_GRAVEL;
    static int BLOCK_LOG;
    static int BLOCK_LEAF;
    static int BLOCK_SNOW;
    static int BLOCK_LIMESTONE;
    static int BLOCK_GRANITE;
    static int BLOCK_ANDESITE;
    static int BLOCK_MOSSY_STONE;
    static int BLOCK_STONE_STAIR;

    const float WorldHeight = 224.0f; // 7 chunks * 32 blocks
    const float SeaLevel    = 64.0f;  // baseline ground height
    const float MaxPeak     = 200.0f; // cap so peaks stay below world top
    const float SnowLine    = 150.0f; // terrain above this height gets capped in snow

    static readonly V256f WorldHeightV256f = V256F.New(224.0f); // 7 chunks * 32 blocks
    static readonly V256f SeaLevelV256f    = V256F.New(64.0f);  // baseline ground height
    static readonly V256f MaxPeakV256f     = V256F.New(200.0f); // cap so peaks stay below world top
    static readonly V256f SnowLineV256f    = V256F.New(150.0f); // terrain above this height gets capped in snow
    
    private static int GetBlock(string name)
    {
        if (!BlockData.GetBlockInfo(name, out var info))
        {
            return 0;
        }

        var block = new Block(info.ID);
        block.SetOcclusion(0b111111);
        return (int)block.blockData;
    }

    public static void Init()
    {
        BLOCK_TEST = GetBlock("test_block");
        BLOCK_GRASS = GetBlock("grass_block");
        BLOCK_DIRT = GetBlock("dirt_block");
        BLOCK_STONE = GetBlock("stone_block");
        BLOCK_GRAVEL = GetBlock("gravel_block");
        BLOCK_LOG = GetBlock("log_block");
        BLOCK_LEAF = GetBlock("leaf_block");
        BLOCK_SNOW = GetBlock("snow_block");
        BLOCK_LIMESTONE = GetBlock("limestone_block");
        BLOCK_GRANITE = GetBlock("granite_block");
        BLOCK_ANDESITE = GetBlock("andesite_block");
        BLOCK_MOSSY_STONE = GetBlock("mossy_stone_block");
        BLOCK_STONE_STAIR = GetBlock("stone_stair_block");

        int threadCount = VoxelRenderer.GenerationThreads;

        _scalarWorkerStates = new GenerationWorkerState<float>[threadCount];
        _vector256WorkerStates = new GenerationWorkerState<V256f>[threadCount];

        _vector256BlockCache = new V256i[threadCount][];

        for (int i = 0; i < threadCount; i++)
        {
            _scalarWorkerStates[i] = new(32, 32);
            _vector256WorkerStates[i] = new(4, 32);
            _vector256BlockCache[i] = new V256i[4 * 32];
        }
    }



    /// <summary>
    /// Terrain surface height for a world XZ column. Pure 2D noise - never
    /// touches Y - which is what makes the column-cached approach above
    /// valid and correct.
    /// </summary>
    static float GetTerrainHeight(float worldX, float worldZ)
    {
        // Large-scale mask: decides WHERE mountain ranges rise up vs flatter
        // land. Low frequency so ranges span many chunks.
        float continent = Fbm2D(worldX * 0.0025f, worldZ * 0.0025f, 4); // -1..1 (ish)
        continent = Mathf.Clamp01y((continent + 1f) * 0.5f);
        continent = Smoothstep(0.35f, 0.75f, continent);
 
        // Ridged multi-fractal: 1 - abs(noise) folds smooth Perlin waves
        // into sharp ridges, which reads as jagged mountain silhouettes.
        float ridged = 0f;
        float amplitude = 0.5f;
        float frequency = 0.006f;
        float amplitudeSum = 0f;
        for (int i = 0; i < 6; i++)
        {
            float n = NoiseLib.Noise(worldX * frequency, worldZ * frequency);
            n = 1f - Mathf.Abs(n); // fold into a ridge
            n = n * n;             // sharpen the ridge crest
            ridged += n * amplitude;
            amplitudeSum += amplitude;
            frequency *= 2.05f;    // slightly detuned lacunarity avoids grid artifacts
            amplitude *= 0.5f;
        }
        ridged /= amplitudeSum; // 0..1
 
        // Small extra detail noise so slopes aren't perfectly smooth.
        float detail = Fbm2D(worldX * 0.05f, worldZ * 0.05f, 3) * 4f; // -4..4 blocks
 
        float mountainAmount = ridged * continent;
        float h = SeaLevel + mountainAmount * (MaxPeak - SeaLevel) + detail;
 
        return Mathf.Clampy(h, 1f, WorldHeight - 1f);
    }

    static readonly V256f _v3 = V256F.New(0.0025f);
    static readonly V256f _v4 = V256F.New(2.05f);
    static readonly V256f _v5 = V256F.New(0.05f);

    static readonly V256f _v6 = V256F.New(0.35f);
    static readonly V256f _v7 = V256F.New(0.75f);

    static readonly V256f _v8 = V256F.New(0.006f);

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    static V256f GetTerrainHeightSimd(V256f worldX, V256f worldZ)
    {
        V256f continent = Fbm2DSimd(V256F.Multiply(worldX, _v3), V256F.Multiply(worldZ, _v3), 4);
        continent = V256F.Clamp01(V256F.Multiply(V256F.Add(continent, V256F.One), V256F.Half));
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

        V256f detail = V256F.Multiply(Fbm2DSimd(V256F.Multiply(worldX, _v5), V256F.Multiply(worldZ, _v5), 3), V256F.Four); // -4..4 blocks

        V256f mountainAmount = ridged * continent;
        V256f h = V256F.Add(V256F.Add(SeaLevelV256f, V256F.Multiply(mountainAmount, V256F.Subtract(MaxPeakV256f, SeaLevelV256f))), detail);
 
        return V256F.Clamp(h, V256F.One, V256F.Subtract(WorldHeightV256f, V256F.One));
    }
 
    /// <summary>
    /// Picks a rocky block type so underground/exposed stone isn't one flat
    /// grey mass. NoiseLib only exposes 2D noise, so depth variation is
    /// faked by folding worldY into the sample coordinates rather than a
    /// true 3D lookup - cheap, and visually indistinguishable here.
    /// </summary>
    static int GetStoneVariant(int worldX, int worldY, int worldZ, float terrainHeight)
    {
        float n = NoiseLib.Noise(worldX * 0.08f + worldY * 0.08f,
                                  worldZ * 0.08f - worldY * 0.08f);
        n = (n + 1f) * 0.5f; // 0..1
 
        float mossChance = Mathf.Clamp01y(1f - (terrainHeight - SeaLevel) / (SnowLine - SeaLevel));
 
        if (n < 0.12f) return BLOCK_GRANITE;
        if (n < 0.24f) return BLOCK_ANDESITE;
        if (n < 0.36f) return BLOCK_LIMESTONE;
        if (n < 0.36f + 0.15f * mossChance) return BLOCK_MOSSY_STONE;
        return BLOCK_STONE;
    }
 
    static float Fbm2D(float x, float y, int octaves)
    {
        float f = 0f;
        float w = 0.5f;
        for (int i = 0; i < octaves; i++)
        {
            f += w * NoiseLib.Noise(x, y);
            x *= 2f;
            y *= 2f;
            w *= 0.5f;
        }
        return f;
    }
 
    static float Smoothstep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01y((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    /*
    public interface IMath<T>
    {
        static abstract T Add(T a, T b);
        static abstract T Mul(T a, T b);
        static abstract T Clamp01(T x);
    }

    public readonly struct ScalarMath : IMath<float>
    {
        public static float Add(float a, float b) => a + b;
        public static float Mul(float a, float b) => a * b;
        public static float Clamp01(float x) => Math.Clamp(x, 0f, 1f);
    }
    
    public readonly struct SimdMath : IMath<V256f>
    {
        public static V256f Add(V256f a, V256f b) => a + b;
        public static V256f Mul(V256f a, V256f b) => a * b;
        public static V256f Clamp01(V256f x) => Avx.Min(Avx.Max(x, V256.Zero), V256.One);
    }


    static T Generate<T, TMath>()
    where TMath : IMath<T>
    {
        T x = ...;

        x = TMath.Add(x, ...);
        x = TMath.Mul(x, ...);
        x = TMath.Clamp01(x);

        return x;
    }
    */

    const bool scalarCache = true;

    public static void RemoveCache(Vector2i position)
    {
        if (scalarCache)
            RemoveScalarCache(position);
        else
            RemoveV256Cache(position);
    }


    public static void TryAddCache(Vector2i position)
    {
        if (scalarCache)
            TryAddScalarCache(position);
        else
            TryAddV256Cache(position);
    }



    public static void RemoveV256Cache(Vector2i position)
    {
        lock (_cacheLock)
        {
            _v256fCacheHandler.Remove(position, out var handler);
            handler?.RequestDispose();
        }
    }


    public static void TryAddV256Cache(Vector2i position)
    {
        lock (_cacheLock)
        {
            if (_v256fCacheHandler.ContainsKey(position))
                return;

            CacheHandler<V256f> handler = new(3, 4, 32);
            _v256fCacheHandler.Add(position, handler);
        }
    }

    private static bool TryGetV256Cache(Vector2i position, [NotNullWhen(true)] out CacheHandler<V256f>? handler)
    {
        lock (_cacheLock)
        {
            handler = null;
            if (!_v256fCacheHandler.TryGetValue(position, out var h))
                return false;

            handler = h;
            return true;
        }
    }



    public static void RemoveScalarCache(Vector2i position)
    {
        lock (_cacheLock)
        {
            _scalarCacheHandler.Remove(position, out var handler);
            handler?.RequestDispose();
        }
    }

    public static void TryAddScalarCache(Vector2i position)
    {
        lock (_cacheLock)
        {
            if (_scalarCacheHandler.ContainsKey(position))
                return;

            CacheHandler<float> handler = new(3, 32, 32);
            _scalarCacheHandler.Add(position, handler);
        }
    }

    private static bool TryGetScalarCache(Vector2i position, [NotNullWhen(true)] out CacheHandler<float>? handler)
    {
        lock (_cacheLock)
        {
            handler = null;
            if (!_scalarCacheHandler.TryGetValue(position, out var h))
                return false;

            handler = h;
            return true;
        }
    }


    private static object _cacheLock = new();

    private static Dictionary<Vector2i, CacheHandler<float>> _scalarCacheHandler = [];
    private static Dictionary<Vector2i, CacheHandler<V256f>> _v256fCacheHandler = [];

    public static int ScalarCacheCount => _scalarCacheHandler.Count;
    public static int V256FCacheCount => _v256fCacheHandler.Count;

    private static GenerationWorkerState<float>[] _scalarWorkerStates;
    private static GenerationWorkerState<V256f>[] _vector256WorkerStates;
    private static V256i[][] _vector256BlockCache;

    private sealed class GenerationWorkerState<T>
    {
        public readonly T[] Height;
        public readonly T[] HeightX;
        public readonly T[] HeightZ;

        public GenerationWorkerState(int xSize, int zSize)
        {
            Height  = new T[xSize * zSize];
            HeightX = new T[xSize * zSize];
            HeightZ = new T[xSize * zSize];
        }
    }

    private sealed class NewCacheHandler
    {
        public object CacheLock = new();
        public bool Generated = false;
        
        

        public NewCacheHandler()
        {
            
        }
    }

    public sealed class CacheHandler<T> : IDisposable where T : unmanaged
    {
        public object CacheLock = new();
        public bool Generated = false;

        // Encoding: bit 31 = dispose-pending flag, bits 0-30 = active reader count
        private const int DisposeFlag = unchecked((int)0x80000000);
        private const int CountMask = 0x7FFFFFFF;

        private int _state; // 0 = idle, no readers, no dispose requested
        
        
        private T* _mapPtr;
        public T*[] Cache;

        public CacheHandler(int count, int xSize, int zSize)
        {
            _mapPtr = MemoryHelper.Alloc<T>(count * xSize * zSize);
            Cache = new T*[count];
            for (int i = 0; i < count; i++)
                Cache[i] = _mapPtr + i * xSize * zSize;
        }

        

        // Called by any chunk wanting to read — multiple can succeed concurrently
        public bool TryEnter()
        {
            while (true)
            {
                int current = _state;
                if ((current & DisposeFlag) != 0) return false; // dispose requested/done, refuse new entrants

                int updated = current + 1; // bump count, flag bit untouched (still 0 here)
                if (Interlocked.CompareExchange(ref _state, updated, current) == current)
                    return true;
            }
        }

        // Called when a chunk is done reading
        public void Exit()
        {
            while (true)
            {
                int current = _state;
                int count = current & CountMask;
                int updated = current - 1;

                if (Interlocked.CompareExchange(ref _state, updated, current) == current)
                {
                    bool disposePending = (current & DisposeFlag) != 0;
                    bool wasLastReader = count == 1;
                    if (disposePending && wasLastReader)
                        Dispose(); // we're the last one out, and a dispose was waiting on us
                    return;
                }
            }
        }

        // Called by the disposer side — marks pending, disposes immediately if already idle
        public bool RequestDispose()
        {
            while (true)
            {
                int current = _state;
                if ((current & DisposeFlag) != 0) return true; // already requested/disposed

                int updated = current | DisposeFlag; // set flag, keep count as-is
                if (Interlocked.CompareExchange(ref _state, updated, current) == current)
                {
                    if ((current & CountMask) == 0)
                        Dispose(); // no active readers right now — free immediately
                    // else: some Exit() call will see the flag and dispose when count hits 0
                    return true;
                }
            }
        }

        public void Dispose()
        {
            MemoryHelper.Free(_mapPtr);
            Cache = [];
        }
    }

    private sealed class HeightMapCache<T>
    {
        public T[] Data;

        public HeightMapCache(int xSize, int zSize)
        {
            Data = new T[xSize * zSize];
        }
    }


    public static bool GenerateChunk(VoxelChunk chunk, int workerId)
    {
        double startMs = _sw.Elapsed.TotalMilliseconds;

        var status = chunk.Status;
        chunk.Status = ChunkStatus.Generating;

        /*
        if (status == ChunkStatus.Canceled)
        {
            chunk.Renderer.DeletionQueue.Enqueue(chunk);
            return false;
        }
        */

        chunk.HasBlocks = false;

        Vector3i iLocal = Vector3i.Zero;
        Vector3i iPosition = chunk.WorldPosition;

                // --- KEY OPTIMIZATION ---
        // In the GLSL version, GetTerrainHeight() (and its 6-octave ridged
        // noise stack) ran once per *voxel* thread, including once per Y
        // layer, even though the result never depends on Y. On a GPU that's
        // hidden by massive parallelism; on a CPU it's 32x wasted work per
        // column. Here we compute the height (and the two neighbor heights
        // needed for slope/steepness) exactly once per X/Z column, then
        // reuse those 3 floats for all 32 voxels stacked above/below.


        int baseX = chunk.WorldPosition.X;
        int baseY = chunk.WorldPosition.Y;
        int baseZ = chunk.WorldPosition.Z;
        
        //ScalarHeight(baseX, baseZ, workerId);
        //ScalarPopulate(chunk, baseX, baseY, baseZ, workerId);

        int type = 5;
        switch (type) 
        {
            case 0:
                if (TryGetV256Cache(chunk.RelativePosition.Xz, out var handler))
                {
                    lock (handler.CacheLock)
                    {
                        if (!handler.Generated)
                            ChunkGenerationAvx2.Vector256HeightSimple(baseX, baseZ, handler);

                        handler.Generated = true;
                    }
                    ChunkGenerationAvx2.Vector256PopulateSimple(chunk, baseX, baseY, baseZ, handler);
                }
                break;

            case 1:
                if (TryGetV256Cache(chunk.RelativePosition.Xz, out handler))
                {
                    lock (handler.CacheLock)
                    {
                        if (!handler.Generated)
                            ChunkGenerationAvx2.Vector256Height(baseX, baseZ, handler);

                        handler.Generated = true;
                    }
                    ChunkGenerationAvx2.Vector256Populate(chunk, baseX, baseY, baseZ, handler);
                }
                break;

            case 2:
                if (chunk.RelativePosition == (0, 0, 0))
                {
                    for (int i = 0; i < 32 * 32 * 32; i++)
                    {
                        chunk.Blocks[i] = Block.Air;

                        int x = (i >> 5) & 31;
                        int y = i & 31;
                        int z = (i >> 10) & 31;

                        const float cx = 15.5f;
                        const float cy = 15.5f;
                        const float cz = 15.5f;

                        const float rx = 12.5f;
                        const float ry = 15.5f;
                        const float rz = 10.5f;

                        float dx = (x - cx) / rx;
                        float dy = (y - cy) / ry;
                        float dz = (z - cz) / rz;

                        if (dx * dx + dy * dy + dz * dz <= 1.0f)
                        {
                            var block = new Block((uint)BLOCK_STONE);
                            chunk.Blocks[i] = block;
                        }
                        else
                            chunk.Blocks[i] = Block.Air;
                    }
                }
                else
                {
                    chunk.ClearBlocks();
                }
                break;

            case 3:

                float bx = chunk.WorldPosition.X;
                float by = chunk.WorldPosition.Y;
                float bz = chunk.WorldPosition.Z;

                const float frequency = 0.06f;
                const float threshold = 0.15f;

                int index = 0;
                for (int y = 0; y < 32; y++)
                for (int z = 0; z < 32; z++)
                for (int x = 0; x < 32; x++)
                {
                    Block block = Block.Air;

                    float wx = (bx + x) * frequency;
                    float wy = (by + y) * frequency;
                    float wz = (bz + z) * frequency;

                    float n = NoiseLib.Noise(wx, wy, wz);

                    if (n > threshold)
                    {
                        block = new Block((uint)BLOCK_STONE);
                    }

                    chunk.Blocks[index] = block;
                    index++;
                }
                break;

            case 4:
                if (TryGetV256Cache(chunk.RelativePosition.Xz, out handler))
                {
                    lock (handler.CacheLock)
                    {
                        if (!handler.Generated)
                            ChunkGenerationAvx2.Vector256Height(baseX, baseZ, handler);

                        handler.Generated = true;
                    }

                    if (!handler.TryEnter())
                    {
                        Console.WriteLine($"Chunk: {chunk.RelativePosition} failed");
                        return false;
                    }

                    ChunkGenerationAvx2.Vector256Populate(chunk, baseX, baseY, baseZ, handler);

                    handler.Exit();
                }
                break;

            case 5:
                if (TryGetScalarCache(chunk.RelativePosition.Xz, out var scalarHandler))
                {
                    lock (scalarHandler.CacheLock)
                    {
                        if (!scalarHandler.Generated)
                            ChunkGenerationAvx2.Vector256Height(baseX, baseZ, scalarHandler);

                        scalarHandler.Generated = true;
                    }

                    if (!scalarHandler.TryEnter())
                    {
                        Console.WriteLine($"Chunk: {chunk.RelativePosition} failed");
                        return false;
                    }

                    ChunkGenerationAvx2.Vector256PopulateOrderedYByte(chunk, baseX, baseY, baseZ, scalarHandler);

                    scalarHandler.Exit();
                }
                break;
        }

        //uint* blocks = (uint*)chunk.Blocks;

        /*

        for (int i = 0; i < CHUNK_SIZE; i++)
        {
            int z = i * 32;
            int y = i * 1024;

            // -z rows, FRONT
            {
                uint* ptr = (uint*)chunk.BlockMap[z]; // + y;

                var row1 = V256U.Load(ptr);
                var row2 = V256U.Load(ptr + 8);
                var row3 = V256U.Load(ptr + 16);
                var row4 = V256U.Load(ptr + 24);

                var oRow1 = row1.CompareEqual(V256U.Zero);
                var oRow2 = row2.CompareEqual(V256U.Zero);
                var oRow3 = row3.CompareEqual(V256U.Zero);
                var oRow4 = row4.CompareEqual(V256U.Zero);

                ulong mask1 = (uint)Avx2.MoveMask(oRow1.AsByte());
                ulong mask2 = (uint)Avx2.MoveMask(oRow2.AsByte());
                ulong mask3 = (uint)Avx2.MoveMask(oRow3.AsByte());
                ulong mask4 = (uint)Avx2.MoveMask(oRow4.AsByte());

                ulong compact1 = Bit.Extract(mask1 | (mask2 << 32), 0x1111111111111111ul);
                ulong compact2 = Bit.Extract(mask3 | (mask4 << 32), 0x1111111111111111ul);

                uint row = (uint)(compact1 | (compact2 << 16));

                chunk.FrontOcclusion[i] = ~row;
            }

            // +z rows, BACK
            {
                uint* ptr = (uint*)chunk.BlockMap[z + 31];

                var row1 = V256U.Load(ptr);
                var row2 = V256U.Load(ptr + 8);
                var row3 = V256U.Load(ptr + 16);
                var row4 = V256U.Load(ptr + 24);

                var oRow1 = row1.CompareEqual(V256U.Zero);
                var oRow2 = row2.CompareEqual(V256U.Zero);
                var oRow3 = row3.CompareEqual(V256U.Zero);
                var oRow4 = row4.CompareEqual(V256U.Zero);

                ulong mask1 = (uint)Avx2.MoveMask(oRow1.AsByte());
                ulong mask2 = (uint)Avx2.MoveMask(oRow2.AsByte());
                ulong mask3 = (uint)Avx2.MoveMask(oRow3.AsByte());
                ulong mask4 = (uint)Avx2.MoveMask(oRow4.AsByte());

                ulong compact1 = Bit.Extract(mask1 | (mask2 << 32), 0x1111111111111111ul);
                ulong compact2 = Bit.Extract(mask3 | (mask4 << 32), 0x1111111111111111ul);

                uint row = (uint)(compact1 | (compact2 << 16));

                chunk.BackOcclusion[i] = ~row;
            }



            // -y rows, BOTTOM
            {
                uint* ptr = (uint*)chunk.BlockMap[i];

                var row1 = V256U.Load(ptr);
                var row2 = V256U.Load(ptr + 8);
                var row3 = V256U.Load(ptr + 16);
                var row4 = V256U.Load(ptr + 24);

                var oRow1 = row1.CompareEqual(V256U.Zero);
                var oRow2 = row2.CompareEqual(V256U.Zero);
                var oRow3 = row3.CompareEqual(V256U.Zero);
                var oRow4 = row4.CompareEqual(V256U.Zero);

                ulong mask1 = (uint)Avx2.MoveMask(oRow1.AsByte());
                ulong mask2 = (uint)Avx2.MoveMask(oRow2.AsByte());
                ulong mask3 = (uint)Avx2.MoveMask(oRow3.AsByte());
                ulong mask4 = (uint)Avx2.MoveMask(oRow4.AsByte());

                ulong compact1 = Bit.Extract(mask1 | (mask2 << 32), 0x1111111111111111ul);
                ulong compact2 = Bit.Extract(mask3 | (mask4 << 32), 0x1111111111111111ul);

                uint row = (uint)(compact1 | (compact2 << 16));

                chunk.BottomOcclusion[i] = ~row;
            }

            // +y rows, TOP
            {
                uint* ptr = (uint*)chunk.BlockMap[i + 992];

                var row1 = V256U.Load(ptr);
                var row2 = V256U.Load(ptr + 8);
                var row3 = V256U.Load(ptr + 16);
                var row4 = V256U.Load(ptr + 24);

                var oRow1 = row1.CompareEqual(V256U.Zero);
                var oRow2 = row2.CompareEqual(V256U.Zero);
                var oRow3 = row3.CompareEqual(V256U.Zero);
                var oRow4 = row4.CompareEqual(V256U.Zero);

                ulong mask1 = (uint)Avx2.MoveMask(oRow1.AsByte());
                ulong mask2 = (uint)Avx2.MoveMask(oRow2.AsByte());
                ulong mask3 = (uint)Avx2.MoveMask(oRow3.AsByte());
                ulong mask4 = (uint)Avx2.MoveMask(oRow4.AsByte());

                ulong compact1 = Bit.Extract(mask1 | (mask2 << 32), 0x1111111111111111ul);
                ulong compact2 = Bit.Extract(mask3 | (mask4 << 32), 0x1111111111111111ul);

                uint row = (uint)(compact1 | (compact2 << 16));

                chunk.TopOcclusion[i] = ~row;
            }
        }

        */

        /*
        if (chunk.Status == ChunkStatus.Canceled)
        {
            chunk.Renderer.DeletionQueue.Enqueue(chunk);
            return false;
        }
        */

        chunk.Status = ChunkStatus.Generated;

        WorldNodeEditor.ChunkGenerationTimer.AddSample(_sw.Elapsed.TotalMilliseconds - startMs);
        //WorldNodeEditor.GenerationCount++;

        return true;
    }

    private static void ScalarHeight(int chunkWorldX, int chunkWorldZ, int workerId)
    {
        var workerState = _scalarWorkerStates[workerId];
        float[] height  = workerState.Height;
        float[] heightX = workerState.HeightX;
        float[] heightZ = workerState.HeightZ;

        int xzIndex;
        for (int x = 0; x < CHUNK_SIZE; x++)
        {
            for (int z = 0; z < CHUNK_SIZE; z++)
            {
                xzIndex = x + z * 32;

                int worldX = x + chunkWorldX;
                int worldZ = z + chunkWorldZ;
 
                height[xzIndex]  = GetTerrainHeight(worldX,     worldZ);
                heightX[xzIndex] = GetTerrainHeight(worldX + 1, worldZ);
                heightZ[xzIndex] = GetTerrainHeight(worldX,     worldZ + 1);
            }
        }
    }



    private unsafe static void ScalarPopulate(VoxelChunk chunk, int baseX, int baseY, int baseZ, int workerId)
    {
        Block* blocks = chunk.Blocks;

        var workerState = _scalarWorkerStates[workerId];
        float[] height  = workerState.Height;
        float[] heightX = workerState.HeightX;
        float[] heightZ = workerState.HeightZ;

        int xzIndex;
        for (int x = 0; x < CHUNK_SIZE; x++)
        {
            for (int z = 0; z < CHUNK_SIZE; z++)
            {
                xzIndex = x + z * 32;

                int worldX = x + baseX;
                int worldZ = z + baseZ;
 
                float terrainHeightF = height[xzIndex];
                int terrainHeight = (int)terrainHeightF;
 
                // Slope/steepness from the cached neighbor heights - no
                // extra noise sampling needed here, just a couple of subs.
                float slope = Mathf.Abs(terrainHeightF - heightX[xzIndex])
                            + Mathf.Abs(terrainHeightF - heightZ[xzIndex]);
                bool isSteep   = slope > 2.5f;
                bool isSnowCap = terrainHeightF > SnowLine;
 
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    int worldY = y + baseY;
                    int index = x + z * CHUNK_SIZE + y * CHUNK_SIZE * CHUNK_SIZE;
 
                    if (worldY > terrainHeight)
                    {
                        blocks[index] = Block.Air; // air / cleared
                        continue;
                    }
 
                    int depthFromSurface = terrainHeight - worldY; // 0 = surface
                    int blockId;
 
                    if (depthFromSurface == 0)
                    {
                        // Surface layer
                        if (isSteep)
                            blockId = GetStoneVariant(worldX, worldY, worldZ, terrainHeightF);
                        else if (isSnowCap)
                            blockId = BLOCK_SNOW;
                        else
                            blockId = BLOCK_GRASS;
                    }
                    else if (depthFromSurface <= 3)
                    {
                        // Just below the surface
                        if (isSteep)
                            blockId = GetStoneVariant(worldX, worldY, worldZ, terrainHeightF);
                        else if (isSnowCap)
                            blockId = (depthFromSurface == 1) ? BLOCK_SNOW : BLOCK_STONE;
                        else
                            blockId = BLOCK_DIRT;
                    }
                    else
                    {
                        // Deep underground: mostly stone with rocky variety,
                        // occasional gravel pockets on steep faces.
                        float gravelNoise = NoiseLib.Noise(worldX * 0.15f + worldY * 0.37f + 500f,
                                                            worldZ * 0.15f - worldY * 0.37f + 500f);
                        if (isSteep && gravelNoise > 0.6f)
                            blockId = BLOCK_GRAVEL;
                        else
                            blockId = GetStoneVariant(worldX, worldY, worldZ, terrainHeightF);
                    }
 
                    blocks[index] = new Block((uint)blockId);
                    chunk.HasBlocks |= blockId > 0;
                }
            }
        }
    }

    /*
                    
    // now we need to set the data
    iLocal.X = x;
    iPosition.X = x + chunk.WorldPosition.X;
    
    Vector2 worldXZ = iPosition.Xz;

    float terrainHeightF = GetTerrainHeight(worldXZ);
    int terrainHeight = (int)terrainHeightF;

    if (iPosition.Y > terrainHeight)
    {
        continue; // air, already cleared above
    }

    // Estimate local slope by comparing to neighboring columns' heights.
    // Steep faces show bare rock instead of grass/snow, like real mountains.
    float hX = GetTerrainHeight(worldXZ + new Vector2(1.0f, 0.0f));
    float hZ = GetTerrainHeight(worldXZ + new Vector2(0.0f, 1.0f));
    float slope = Mathf.Abs(terrainHeightF - hX) + Mathf.Abs(terrainHeightF - hZ);
    bool isSteep = slope > 2.5;

    int depthFromSurface = terrainHeight - iPosition.Y; // 0 = surface block
    bool isSnowCap = terrainHeightF > snowLine;

    uint block;

    if (depthFromSurface == 0)
    {
        // Surface layer
        if (isSteep)
        {
            block = GetStoneVariant(iPosition, terrainHeightF);
        }
        else if (isSnowCap)
        {
            block = BLOCK_SNOW;
        }
        else
        {
            block = BLOCK_GRASS;
        }
    }
    else if (depthFromSurface <= 3)
    {
        // Just below the surface
        if (isSteep)
        {
            block = GetStoneVariant(iPosition, terrainHeightF);
        }
        else if (isSnowCap)
        {
            // Thin patchy snow cover over rock near the peaks
            block = (depthFromSurface == 1) ? BLOCK_SNOW : BLOCK_STONE;
        }
        else
        {
            block = BLOCK_DIRT;
        }
    }
    else
    {
        // Deep underground: mostly stone with rocky variety, occasional
        // gravel pockets scattered in via noise.
        float gravelNoise = NoiseLib.Noise(new Vector3(iPosition) * 0.15f + 500.0f);
        if (isSteep && gravelNoise > 0.6)
        {
            block = BLOCK_GRAVEL;
        }
        else
        {
            block = GetStoneVariant(iPosition, terrainHeightF);
        }
    }
    
    blocks[index] = new Block(block);
    */
}