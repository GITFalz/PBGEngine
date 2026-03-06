using System.Diagnostics;
using PBG.Voxel;

public class LODChunkRenderingProcess : BaseChunkRenderingProcess
{
    public LODChunk Chunk;
    public byte[] ambientOcclusionData = new byte[32 * 32 * 32 * 6];

    public LODChunkRenderingProcess(LODChunk chunk)
    {
        Chunk = chunk;
        Chunk.Process = this;
        GenerateAmbientOcclusion = false;
    }

    public override bool Function()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        //var result = VoxelChunkGenerator.GenerateChunkMesh(this, new LODChunkHandler(Chunk));
        stopwatch.Start();
        //Console.WriteLine("Rendered " + Chunk.WorldPosition + " in " + stopwatch.ElapsedMilliseconds + " " + result);
        return false;
    }

    public override void SetAmbientOcclusion(int index, byte ao) => ambientOcclusionData[index] = ao;

    public override void OnCompleteBase()
    {
        //Console.WriteLine("Finished rendering " + Chunk.WorldPosition);
        if (Failed)
        {
            return;
        }

        try
        {
            Chunk.GenerateChunkMesh();
            if (Chunk.HasBlocks)
                Chunk.Renderer.Chunks.Add(Chunk);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }

        Chunk.Status = ChunkStatus.Rendered;
        Chunk.Process = null;
    }
}