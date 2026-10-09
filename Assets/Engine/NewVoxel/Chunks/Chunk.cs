using PBG.Graphics;
using PBG.MathLibrary;

namespace PBG.NewVoxel;

public class Chunk : AChunk
{
    public static Vector4[] colors =
    [
        new(1.0f, 0.25f, 0.25f, 0.25f), // Red
        new(0.25f, 1.0f, 0.35f, 0.25f), // Green
        new(0.25f, 0.45f, 1.0f, 0.25f), // Blue
        new(1.0f, 0.75f, 0.20f, 0.25f), // Yellow
        new(0.75f, 0.30f, 1.0f, 0.25f), // Purple
    ];

    public Chunk(Vector3i relativePosition, int lodLevel): base(relativePosition, lodLevel) {}

    public override void GetBoundingBoxes(List<BoundingBoxData> boxes)
    {
        boxes.Add(new() {
            Position = WorldPosition,
            Size = new Vector3i(32, 32, 32) * LodMult,
            Color = colors[LodLevel]
        });
    }

    public override void GenerateChunks(VoxelRenderer renderer)
    {
        VoxelChunk chunk = new VoxelChunk(renderer, RelativePosition, LodLevel);
        
        renderer.ChunkDictionnary.TryAdd(RelativePosition, chunk);
        ChunkGeneration.TryAddCache(chunk);

        chunk.Status = ChunkStatus.QueuedToGenerate;
        renderer.EnqueueGeneration(chunk);
    }

    public override void CheckResolution(float distance, Vector3 position)
    {
        
    }
}