using System.Diagnostics.CodeAnalysis;
using PBG.Data;
using PBG.MathLibrary;

namespace PBG.Voxel;

public static class ChunkMesher
{
    public static ChunkMeshingStatus MeshChunk(VoxelChunk chunk, out VoxelChunkData? chunkData)
    {
        chunkData = null;
        
        if (chunk.GetStatus() == ChunkStatus.Canceled)
            return ChunkMeshingStatus.Failed;

        if (chunk.Blocks == null)
        {
            Console.WriteLine("no blocks");
            return ChunkMeshingStatus.NoBlocks;
        }

        chunkData = new(chunk);
        List<Vector4i> vertexData = new List<Vector4i>((int)ChunkDataPool.SLOT_SIZE);

        //timer = Stopwatch.StartNew();
        var result = VoxelChunkGenerator.GenerateIndirectMesh(chunk, vertexData, chunk.WorldPosition, chunk.Blocks, out int VertexCount);
        //timer.Stop();
        //if (VertexCount > 0)
            //Timer.AddSample(timer.Elapsed.Milliseconds);
        if (!result)
            return ChunkMeshingStatus.Failed;

        chunkData.VertexData = [..vertexData];
        return ChunkMeshingStatus.Succeded;
    }
    
    public static void UploadChunk(VoxelChunkData chunkData)
    {
        var chunk = chunkData.Chunk;
        var renderer = chunk.Renderer;
        var VertexCount = chunkData.VertexData.Length;

        if (chunk.GetStatus() == ChunkStatus.Canceled)
            return;

        if (chunk.Restart)
        {
            renderer.EnqueueRendering(chunk);
            chunk.Restart = false;
            return;
        }

        chunk.Process = null;

        try
        {   
            if (VertexCount == 0)
            {
                chunk.Allocation.Size = 0;
                chunk.HasBlocks = false;
                renderer.VisibleChunks.Remove(chunk);
            }
            else if (renderer.DataPool.TryAllocate((uint)VertexCount, out var alloc))
            {
                chunk.Allocation.Set(alloc);
                alloc.DataPool.Update(chunk, chunkData.VertexData, VertexCount);
                
                if (!chunk.HasBlocks)
                    renderer.VisibleChunks.Add(chunk);

                chunk.HasBlocks = VertexCount > 0; 
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

        chunk.SetStatus(ChunkStatus.Rendered);
        chunkData.Clear();
    } 
}

public enum ChunkMeshingStatus
{
    Failed,
    NoBlocks,
    Succeded
}