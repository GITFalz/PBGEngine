using PBG.MathLibrary;

namespace PBG.NewVoxel;

public static class VoxelUtils
{
    public static float GetPriority(VoxelChunk chunk, Vector3 compare) => Vector3.DistanceSquared(chunk.RelativePosition, compare);
}