using PBG.MathLibrary;

namespace PBG.NewVoxel;

public class VoxelChunkData(VoxelChunk chunk)
{
    public VoxelChunk Chunk = chunk;
    public Vector2u[] VertexData = [];

    public void Clear()
    {
        VertexData = [];
    }
}