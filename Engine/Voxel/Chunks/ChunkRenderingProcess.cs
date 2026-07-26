using PBG.MathLibrary;
using PBG.Threads;
using PBG.Data;
using System.Diagnostics;

namespace PBG.Voxel
{
    public abstract class BaseChunkRenderingProcess : ThreadProcess
    {
        public bool GenerateAmbientOcclusion = false;

        public abstract void SetAmbientOcclusion(int index, byte ao);
    }

    public class DefaultChunkRenderingProcess : BaseChunkRenderingProcess
    {
        public VoxelChunk Chunk;
        public byte[] ambientOcclusionData = new byte[34 * 34 * 34]; //32 * 32 * 32 * 6];
        private DefaultVoxelChunkHandlerNew _handler;

        public List<Vector4i> VertexData = [];
        public int VertexCount = 0;

        public static RollingAverageTimer Timer = new();
        public Stopwatch timer;

        public DefaultChunkRenderingProcess(VoxelChunk chunk)
        {
            Chunk = chunk;
            Chunk.Process = this;
            GenerateAmbientOcclusion = chunk.Renderer.AmbientOcclusion;
            _handler = new DefaultVoxelChunkHandlerNew(Chunk);
        }

        public override bool Function()
        {
            Console.WriteLine("meshing: " + Chunk.WorldPosition);
            if (Chunk.Blocks == null)
            {
                Console.WriteLine("no blocks");
                return true;
            }

            VertexData = new List<Vector4i>((int)ChunkDataPool.SLOT_SIZE);

            timer = Stopwatch.StartNew();
            var result = VoxelChunkGenerator.GenerateIndirectMesh(this, VertexData, Chunk.WorldPosition, Chunk.Blocks, out VertexCount);
            timer.Stop();
            if (VertexCount > 0)
                Timer.AddSample(timer.Elapsed.Milliseconds);
            return result;
        }

        public override void SetAmbientOcclusion(int index, byte ao) => ambientOcclusionData[index] = ao;

        public override void OnCompleteBase()
        {
            if (GameTime.FpsUpdated)
            {
                Info.AverageRenderingSpeed = Timer.GetAverageMs();
            }

            Chunk.Renderer.RerenderMap.Remove(Chunk);
            if (Chunk.Restart)
            {
                Chunk.Renderer.RerenderMap.Add(Chunk);
                Chunk.Renderer.RerenderingQueue.AddLast(Chunk);
                Chunk.Restart = false;
                return;
            }

            if (Failed)
            {
                return;
            } 

            Chunk.Process = null;

            try
            {   
                if (VertexCount == 0)
                {
                    Chunk.Allocation.Size = 0;
                    Chunk.HasBlocks = false;
                    Chunk.Renderer.VisibleChunks.Remove(Chunk);
                }
                else if (Chunk.Renderer.DataPool.TryAllocate((uint)VertexCount, out var alloc))
                {
                    Chunk.Allocation.Set(alloc);
                    alloc.DataPool.Update(Chunk, [..VertexData], VertexCount);
                    
                    if (!Chunk.HasBlocks)
                        Chunk.Renderer.VisibleChunks.Add(Chunk);

                    Chunk.HasBlocks = VertexCount > 0; 
                }
                else
                {
                    Console.WriteLine("Couldn't find a available data pool");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }

            Chunk.Status = ChunkStatus.Rendered;

            VertexData = [];
        }
    }
}