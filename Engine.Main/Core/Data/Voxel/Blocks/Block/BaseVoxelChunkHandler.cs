using System.Runtime.CompilerServices;
using PBG.Mathematics;

namespace PBG.Voxel;

public abstract class BaseVoxelChunkHandler(Vector3i worldPosition, Block[] blocks)
{
    public Vector3i WorldPosition = worldPosition;
    public Block[] Blocks = blocks;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract Block GetBlock(Vector3i position);
    public abstract void AddFace(VoxelFace face, Vector3 position);
}