using PBG.MathLibrary;

namespace PBG.NewVoxel;

public unsafe struct Allocation(VoxelChunk chunk)
{
    public VoxelChunk Chunk = chunk;
    public GPUChunkData DataPool;
    public uint VertexCount;
    public uint Offset;
    public uint Size;
    public Vector2u* Memory = null;
    public uint FrameIndex;

    public readonly uint Start => Offset;
    public readonly uint End => Offset + Size;

    public void Set(Allocation allocation)
    {
        VoxelRenderer.TotalVertexCount -= VertexCount;

        DataPool = allocation.DataPool;
        VertexCount = allocation.VertexCount;
        Offset = allocation.Offset;
        Size = allocation.Size;

        VoxelRenderer.TotalVertexCount += VertexCount;
    }

    public void RemoveAllocation()
    {
        //Chunk?.AddStatusChange((VoxelChunk.sw.Elapsed.TotalMilliseconds, "allocate remove " + this));
        if (Size == 0)
            return;

        VoxelRenderer.TotalVertexCount -= VertexCount;

        DataPool.RemoveAllocation(this);
        Memory = null;
        Size = 0;
        Offset = 0;
        VertexCount = 0;
    }

    public void Free()
    {
        //Chunk?.AddStatusChange((VoxelChunk.sw.Elapsed.TotalMilliseconds, "allocate free " + this));
        if (Size == 0)
            return;

        VoxelRenderer.TotalVertexCount -= VertexCount;
        
        DataPool.RemoveAllocation(this);
        DataPool?.Free(ref this);
        Memory = null;
        Size = 0;
        Offset = 0;
        VertexCount = 0;
    }

    public override string ToString()
    {
        return $"[Allocation] : Chunk: {Chunk.WorldPosition}, DataPool: {DataPool?.ID}, Count: {VertexCount}, Offset: {Offset}, Size: {Size}";
    }
}
