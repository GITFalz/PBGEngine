using PBG.MathLibrary;
using PBG.Threads;
using PBG.Data;

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

        public static RollingAverageTimer Timer = new();

        public DefaultChunkRenderingProcess(VoxelChunk chunk)
        {
            Chunk = chunk;
            Chunk.Process = this;
            GenerateAmbientOcclusion = chunk.Renderer.AmbientOcclusion;
            _handler = new DefaultVoxelChunkHandlerNew(Chunk);
        }

        public override bool Function()
        {
            if (Chunk.Blocks == null)
            {
                Console.WriteLine("no blocks");
                return true;
            }

            Timer.Start();
            //var result = VoxelChunkGenerator.GenerateGreedyMeshNew(this, Chunk.WorldPosition, Chunk.Blocks, Chunk.Renderer, _handler);
            var result = VoxelChunkGenerator.GenerateIndirectMesh(this, Chunk.WorldPosition, Chunk.Blocks);
            Timer.End();

            return result;
        }

        public override void SetAmbientOcclusion(int index, byte ao) => ambientOcclusionData[index] = ao;

        public override void OnCompleteBase()
        {
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

            try
            {   
                if (VertexData.Count == 0)
                {
                    Chunk.Allocation.Size = 0;
                    Chunk.HasBlocks = false;
                    Chunk.Renderer.VisibleChunks.Remove(Chunk);
                }
                else if (ChunkDataPool.TryAllocate((uint)VertexData.Count, out var alloc))
                {
                    Chunk.Allocation.Set(alloc);
                    alloc.DataPool.Update(Chunk, [..VertexData]);
                    
                    if (!Chunk.HasBlocks)
                        Chunk.Renderer.VisibleChunks.Add(Chunk);

                    Chunk.HasBlocks = VertexData.Count > 0; 
                }
                else
                {
                    Console.WriteLine("Couldn't find a available data pool");
                }

                /*
                if (VertexData.Count == 0)
                {
                    Chunk.HasBlocks = false;
                    Chunk.Renderer.VisibleChunks.Remove(Chunk);
                }
                else
                {
                    Chunk.GenerateChunkMesh(VertexData);

                    if (!Chunk.HasBlocks)
                        Chunk.Renderer.VisibleChunks.Add(Chunk);

                    Chunk.HasBlocks = VertexData.Count > 0; 
                }
                */

                /*
                Chunk.GenerateChunkMesh(_handler);
                Chunk.AmbientOcclusionTexture?.DeleteBuffer();
                Chunk.AmbientOcclusionTexture = new(1156, 34, ambientOcclusionData, TextureType.Nearest, PixelInternalFormat.R8ui, PixelFormat.RedInteger);
                Chunk.Restart = false;
                */
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }

            Chunk.Status = ChunkStatus.Rendered;
            Chunk.Process = null;

            VertexData = [];
        }
    }
}