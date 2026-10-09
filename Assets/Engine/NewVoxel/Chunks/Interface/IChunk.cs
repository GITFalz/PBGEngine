using PBG.Graphics;
using PBG.MathLibrary;

namespace PBG.NewVoxel;

public abstract class AChunk
{
    public Vector3i RelativePosition;
    public Vector3i WorldPosition;
    public Vector3i Center;

    public int LodLevel;
    public int LodMult => 1 << LodLevel;

    public AChunk(Vector3i relativePosition, int lodLevel)
    {
        LodLevel = lodLevel;
        RelativePosition = relativePosition;
        WorldPosition = relativePosition * 32;
        Center = WorldPosition + 16 * LodMult; 
    }

    public abstract void GetBoundingBoxes(List<BoundingBoxData> boxes);
    public abstract void CheckResolution(float distance, Vector3 position);
    public abstract void GenerateChunks(VoxelRenderer renderer);
}