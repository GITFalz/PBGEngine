using System.Diagnostics;
using PBG.MathLibrary;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.Data;
using PBG.Threads;
using PBG.Voxel;
using PBG.Graphics;
using PBG;
using Silk.NET.Vulkan;

public class WorldGenerator : VoxelRendererGenerator
{
    public static RollingAverageTimer timer = new();

    public static ComputeShader? HeightMapCompute;
    public static Descriptor? _descriptor;
    public static Texture? texture;
    public static int ChunkHeightLocation = -1;
    public static bool Saved = false;

    public static bool RunCompute = false;
    public static float[] TextureData = [];
    public static int CacheNodes = 0;

    public static void Reload(int cacheNodes)
    {
        texture?.Dispose(); 
        texture = new(new()
        {
            Width = 1025,
            Height = cacheNodes,
            Format = Format.R32G32B32A32Sfloat,
            IsStorageImage = true
        });

        if (HeightMapCompute != null)
        {
            HeightMapCompute.Renew();
        }
        else
        {
            HeightMapCompute = new(new(Game.ShaderPath / "computeShaders" / "world_vulkan" / "heightMap.comp"));
            HeightMapCompute.Compile();
        }

        ChunkHeightLocation = HeightMapCompute.GetLocation("ubo.uChunkWorldPosition");
        RunCompute = cacheNodes > 0;
        TextureData = new float[4096 * cacheNodes];
        CacheNodes = cacheNodes;

        _descriptor ??= HeightMapCompute.GetDescriptorSet();
        _descriptor.BindTexture(texture, 0);
    }

    public override void GenerateChunk(VoxelRenderer renderer)
    {
        if (CacheManager.GenerationQueue.Count > 0)
        {
            for (int i = 0; i < 5.Min(CacheManager.GenerationQueue.Count); i++)
            {
                if (CacheManager.GenerationQueue.Count > 0)
                {
                    var cache = CacheManager.GenerationQueue.Dequeue();
                    
                    Compute(cache.WorldPosition);

                    cache.IsReady = true;
                    cache.WaitHandle.Set();
                }
            }
        }
        else
        {
            for (int i = 0; i < renderer.MaxChunkGenerationPerFrame.Min(renderer.GenerationQueue.Count); i++)
            {
                if (renderer.GenerationQueue.Count > 0)
                {
                    var chunk = renderer.GenerationQueue.Dequeue();

                    timer.Start();
                    
                    Compute(chunk.WorldPosition);

                    NoiseNodeManager.RunMain(chunk);
                    
                    chunk.Status = ChunkStatus.Generated;
                    if (chunk.Blocks != null && chunk.Blocks.HasBlocks)
                    {
                        renderer.RenderingQueue.AddLast(chunk);
                    }

                    chunk.Renderer.Counter++;
                        

                    timer.End();

                    var process = new WorldGenerationProcess(renderer, chunk);
                    TaskPool.QueueAction(process);
                }
            }

            Info.AverageChunkGenerationSpeed(timer.GetAverageMs());
        }
    }

    public void Compute(Vector3i worldPosition)
    {
        if (RunCompute && HeightMapCompute != null)
        {
            ColumnCache[] caches = new ColumnCache[CacheNodes];
            bool ready = true;
            for (int a = 0; a < CacheNodes; a++)
            {
                var cache = CacheManager.GetOrAdd(new Vector3i(VoxelData.ChunkRelative(worldPosition).Xz, a));
                caches[a] = cache;
                ready = ready && cache.IsReady;
            }

            if (!ready)  
            {
                HeightMapCompute.Bind(out var commandBuffer);
                _descriptor!.Bind(commandBuffer, PipelineBindPoint.Compute);

                _descriptor!.Uniform(ChunkHeightLocation, worldPosition);

                HeightMapCompute.DispatchBarrier(_descriptor, 4, 1, 4);

                TextureData = texture!.GetPixels();
                
                int j = 0;
                for (int b = 0; b < CacheNodes; b++)
                {
                    ColumnCache cache;
                    cache = caches[b];

                    if (!cache.IsReady)
                    {
                        for (int y = 0; y < 32; y++)
                        {
                            for (int x = 0; x < 32; x++)
                            {
                                cache.Set(x, y, (TextureData[j], TextureData[j+1], TextureData[j+2]));
                                j+=4;
                            }
                        }
                    }
                    else
                    {
                        j += 4096;
                    }
                    cache.IsReady = true;
                }
            }
        }
    }
}

public class WorldGenerationProcess : ThreadProcess
{
    private VoxelRenderer _renderer;
    private VoxelChunk _chunk;

    public WorldGenerationProcess(VoxelRenderer renderer, VoxelChunk chunk)
    {
        _renderer = renderer;
        _chunk = chunk;
    }

    private const int SampleCount = 100;

    private static readonly long[] _samples = new long[SampleCount];
    private static long _sum;
    private static int _index;
    private static int _count;

    public static void AddSample(long ms)
    {
        int i = Interlocked.Increment(ref _index) - 1;
        int slot = i % SampleCount;

        long old = Interlocked.Exchange(ref _samples[slot], ms);

        Interlocked.Add(ref _sum, ms - old);

        if (_count < SampleCount)
            Interlocked.Increment(ref _count);
    }

    public static float GetAverageMs()
    {
        int count = Volatile.Read(ref _count);
        if (count == 0) return 0f;

        long sum = Volatile.Read(ref _sum);
        return (float)sum / count;
    }

    private long _ms = 0;

    public override bool Function()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        var result = NoiseNodeManager.Run(this, _chunk);

        stopwatch.Stop();
        _ms = stopwatch.ElapsedMilliseconds;

        //Console.WriteLine("generated in " + _ms + " ms");

        return result;
    }

    public override void OnCompleteBase()
    {
        AddSample(_ms);

        float avg = GetAverageMs();
        Info.AverageChunkGenerationSpeed(avg);

        //Console.WriteLine(Succeded);

        if (Succeded && _chunk.Blocks != null)
        {
            _chunk.Status = ChunkStatus.Generated;
            if (_chunk.Blocks.HasBlocks)
                _renderer.RenderingQueue.AddLast(_chunk);
        }
    }
}