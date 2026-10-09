using PBG.Graphics;
using PBG.MathLibrary;

namespace PBG.NewVoxel;

public class ChunkManager
{
    public VoxelRenderer Renderer;
    public BoundingBoxRenderer BoundingBoxes;

    public int RenderDistance = 64;

    public int Lod = 4;
    public int LodMult => 1 << Lod;
        

    public AChunk[] Chunks = [];

    public ChunkManager(VoxelRenderer renderer)
    {
        Renderer = renderer;
    }

    public void LoadChunks()
    {
        List<AChunk> chunks = new List<AChunk>((RenderDistance * 2) * (RenderDistance * 2));

        int i = 0;
        for (int x = -RenderDistance; x < RenderDistance; x++)
        {
            for (int z = -RenderDistance; z < RenderDistance; z++)
            {
                Vector3i relativePosition = new Vector3i(x, 0, z) * LodMult;

                chunks.Add(new Chunk(relativePosition, Lod));

                i++;
            }
        }

        chunks.Sort((a, b) =>
        {
            float da = Vector3.DistanceSquared(a.WorldPosition, Vector3.Zero);
            float db = Vector3.DistanceSquared(b.WorldPosition, Vector3.Zero);

            return da.CompareTo(db);
        });

        Chunks = [..chunks];
    }

    public void CheckResolution(float distance)
    {
        if (Lod <= 0)
            return;

        float dist = (LodMult * distance) * (LodMult * distance);

        int i = 0;
        for (int x = 0; x < RenderDistance * 2; x++)
        {
            for (int z = 0; z < RenderDistance * 2; z++)
            {
                var chunk = Chunks[i];
                if (Vector3.DistanceSquared(chunk.Center.YTo0(), new Vector3(0, 256, 0)) <= dist)
                {
                    Chunks[i] = new ChunkMap(chunk.RelativePosition, Lod);
                    Chunks[i].CheckResolution(distance, new Vector3(0, 256, 0));
                }

                i++;
            }
        }
    }

    public void GenerateBoundingBoxes()
    {
        List<BoundingBoxData> boxes = [];

        for (int i = 0; i < Chunks.Length; i++)
        {
            var chunk = Chunks[i];
            chunk.GetBoundingBoxes(boxes);
        }

        BoundingBoxes.UpdateBoundingBoxes([..boxes]);
    }

    public void GenerateChunks(VoxelRenderer voxelRenderer)
    {
        for (int i = 0; i < Chunks.Length; i++)
        {
            var chunk = Chunks[i];
            chunk.GenerateChunks(voxelRenderer);
        }
    }
}