using PBG.MathLibrary;

namespace PBG.NewVoxel;

public unsafe struct MeshMapping
{
    public VoxelChunk Chunk;
    public NewChunkDataPool DataPool;
    public Allocation CurrentAllocation;
    public ulong Gen;

    public MeshMapping(VoxelChunk chunk, ulong gen)
    {
        Chunk = chunk;
        DataPool = chunk.Renderer.DataPool;
        Gen = gen;

        if (DataPool.TryAllocate2(Chunk, out var alloc))
        {
            //chunk.AddStatusChange((VoxelChunk.sw.Elapsed.TotalMilliseconds, "allocate base " + alloc.Value));
            CurrentAllocation = alloc.Value;
        }
        else
        {
            Console.WriteLine("Couldn't find a available data pool, this shouldn't happen");
        }
    }
    
    public void AddFace(Vector2u faceData)
    {
        if (CurrentAllocation.VertexCount == NewChunkDataPool.SLOT_SIZE)
        {
            // upload the current allocation
            Upload();

            
            // the max size has been reached, we need to get a new allocation
            if (DataPool.TryAllocate2(Chunk, out var alloc))
            {
                //Chunk.AddStatusChange((VoxelChunk.sw.Elapsed.TotalMilliseconds, "allocate extra " + alloc.Value));
                CurrentAllocation = alloc.Value;
            }
            else
            {
                Console.WriteLine("Couldn't find a available data pool, this shouldn't happen");
            }
        }

        *(CurrentAllocation.Memory + CurrentAllocation.VertexCount) = faceData;
        CurrentAllocation.VertexCount++;

        VoxelRenderer.TotalVertexCount++;
    }

    public void Upload()
    {
        if (CurrentAllocation.VertexCount == 0 || Gen != Chunk.Gen)
        {
            CurrentAllocation.RemoveAllocation();
            return;
        }

        var uploadData = new UploadData()
        {
            Chunk = Chunk,
            Gen = Gen,
            Index = CurrentAllocation.Offset,
            VertexCount = CurrentAllocation.VertexCount
        };

        var uploadQueue = CurrentAllocation.DataPool.UploadQueues[CurrentAllocation.FrameIndex];
        
        uploadQueue.Enqueue(uploadData);
    }
}