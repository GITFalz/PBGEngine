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
            CurrentAllocation = alloc.Value;
        }
        else
        {
            Console.WriteLine("Couldn't find a available data pool, this shouldn't happen");
        }
    }
    
    public void AddFace(Vector4i faceData)
    {
        if (CurrentAllocation.VertexCount == NewChunkDataPool.SLOT_SIZE)
        {
            // upload the current allocation
            Upload();

            // the max size has been reached, we need to get a new allocation
            if (DataPool.TryAllocate2(Chunk, out var alloc))
            {
                CurrentAllocation = alloc.Value;
            }
            else
            {
                Console.WriteLine("Couldn't find a available data pool, this shouldn't happen");
            }
        }

        *(CurrentAllocation.Memory + CurrentAllocation.VertexCount) = faceData;
        CurrentAllocation.VertexCount++;
    }

    public void Upload()
    {
        if (CurrentAllocation.VertexCount == 0 || Gen != Chunk.Gen)
        {
            CurrentAllocation.RemoveAllocation();
            return;
        }

        uint stride = Vector4i.ByteSize;

        var uploadData = new UploadData()
        {
            Chunk = Chunk,
            Gen = Gen,
            Index = CurrentAllocation.Offset,
            VertexCount = CurrentAllocation.VertexCount,
            OffsetInBytes = CurrentAllocation.Offset * NewChunkDataPool.SLOT_SIZE * stride,
            SizeInBytes = CurrentAllocation.VertexCount * stride
        };

        var uploadQueue = CurrentAllocation.DataPool.UploadQueues[CurrentAllocation.FrameIndex];
        
        uploadQueue.Enqueue(uploadData);
    }
}