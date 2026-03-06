using System.Diagnostics;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.Threads;
using PBG.Voxel;

public class LODWorldGenerationProcess : ThreadProcess
{
    private LODVoxelRenderer _renderer;
    private LODChunk _chunk;

    public LODWorldGenerationProcess(LODVoxelRenderer renderer, LODChunk chunk)
    {
        _renderer = renderer;
        _chunk = chunk;
    }

    public override bool Function()
    {
        _chunk.Blocks = new Block[32768];
        Stopwatch stopwatch = Stopwatch.StartNew();
        var result = NoiseNodeManager.RunLOD(this, _chunk.WorldPosition.Xz, _chunk, _chunk.Level);
        stopwatch.Stop();
        //Console.WriteLine(stopwatch.ElapsedMilliseconds + " " + result);
        return result;
    }

    public override void OnCompleteBase()
    {
        //Console.WriteLine("Chunk " + _chunk.WorldPosition + " generation is " + Succeded);
        if (Succeded)
        {
            _chunk.Status = ChunkStatus.Generated;
            if (_chunk.HasSolidBlocks())
            {
                //Console.WriteLine("Chunk " + _chunk.WorldPosition + " has blocks and is now being generated");
                _renderer.RenderingQueue.AddLast(_chunk);
            }
            else
            {
                //Console.WriteLine("no blocks");
            }
        }
    }
}