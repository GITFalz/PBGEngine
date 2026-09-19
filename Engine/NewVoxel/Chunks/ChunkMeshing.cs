using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using PBG.Data;
using PBG.MathLibrary;

namespace PBG.NewVoxel;

[InternalSystemCleanup]
public static class ChunkMesher
{
    public static void Cleanup()
    {
        DebugDump.ToFile(_times, "times");
    }

    static List<double> _times = [];

    public static ChunkMeshingStatus MeshChunk(VoxelChunk chunk, int workerID, out VoxelChunkData? chunkData)
    {
        try
        {
            //Stopwatch sw = Stopwatch.StartNew();
            chunkData = null;
            
            if (chunk.Status != ChunkStatus.Meshing)
                return ChunkMeshingStatus.Failed;

            chunkData = new(chunk);
            List<Vector4i> vertexData = [];

            WorldNodeEditor.GlobalCount++;
            bool result = VoxelChunkGenerator.GenerateGreedyMesh5(chunk, vertexData, chunk.WorldPosition, workerID, out _);
            
            if (!result || chunk.Status != ChunkStatus.Meshing)
                return ChunkMeshingStatus.Failed;

            chunkData.VertexData = [..vertexData];
            //_times.Add(sw.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
        return ChunkMeshingStatus.Succeded;
    }

    public static ChunkMeshingStatus MeshChunk2(VoxelChunk chunk, int workerID)
    {
        try
        {
            Stopwatch sw = Stopwatch.StartNew();
            if (chunk.Status != ChunkStatus.Meshing)
                return ChunkMeshingStatus.Failed;

            var gen = chunk.NewGen();

            WorldNodeEditor.GlobalCount++;

            MeshMapping mapping = new(chunk, gen);
            bool result = VoxelChunkGenerator.GenerateGreedyMesh7YByte(chunk, ref mapping, workerID);
            mapping.Upload();
            
            if (!result || chunk.Status != ChunkStatus.Meshing)
                return ChunkMeshingStatus.Failed;

            _times.Add(sw.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
        return ChunkMeshingStatus.Succeded;
    }

    
    public static void UploadChunk(VoxelChunkData chunkData)
    {
        //Console.WriteLine("Uploading");
        var chunk = chunkData.Chunk;
        var renderer = chunk.Renderer;
        var VertexCount = chunkData.VertexData.Length;

        if (chunk.Status != ChunkStatus.QueuedToUpload)
            return;

        try
        {   
            chunk.FreeAllocation();

            if (VertexCount == 0)
            {
                chunk.Allocation.Size = 0;
                chunk.HasBlocks = false;
                chunk.Status = ChunkStatus.Rendered;
                chunkData.Clear();
                return;
            }     
            
            if (renderer.DataPool.TryAllocate(chunk, (uint)VertexCount, out var alloc))
            {
                alloc.DataPool.Update(chunk, chunkData.VertexData, VertexCount);
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

        chunk.Status = ChunkStatus.Rendered;
        chunkData.Clear();
    } 
}

public enum ChunkMeshingStatus
{
    Failed,
    NoBlocks,
    Succeded
}