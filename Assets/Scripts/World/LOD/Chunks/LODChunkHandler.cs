using System.Runtime.CompilerServices;
using PBG.MathLibrary;
using PBG.Voxel;

public class LODChunkHandler(LODChunk chunk) : BaseVoxelChunkHandler(chunk.WorldPosition, chunk.Blocks)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Block GetBlock(Vector3i position)
    {
        int lx = position.X - chunk.WorldPosition.X;
        int ly = position.Y - chunk.WorldPosition.Y;
        int lz = position.Z - chunk.WorldPosition.Z;

        if ((uint)lx < 32u & (uint)ly < 32u & (uint)lz < 32u)
            return Blocks[ChunkBlocks.GetIndex(lx, ly, lz)];
        return Block.Air;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void AddFace(VoxelFace face, Vector3 position)
    {
        uint o = (uint)chunk.ChunkVertices.Count;
        BlockDefinition.AddFace(chunk.ChunkVertices, face, position);
        chunk.ChunkIndices.Add(0+o);
        chunk.ChunkIndices.Add(1+o);
        chunk.ChunkIndices.Add(2+o);
        chunk.ChunkIndices.Add(2+o);
        chunk.ChunkIndices.Add(3+o);
        chunk.ChunkIndices.Add(0+o);
    }
}