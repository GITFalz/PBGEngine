using PBG.Graphics;
using PBG.MathLibrary;

namespace PBG.NewVoxel;

public class ChunkMap : AChunk
{
    public AChunk[] Chunks = new AChunk[8];

    public ChunkMap(Vector3i relative, int lodLevel) : base(relative, lodLevel)
    {
        int lowerLod = lodLevel - 1;
        int lowerMult = 1 << lowerLod;

        int x = Math.Abs(relative.X) < Math.Abs(relative.X + lowerMult) ? 0 : 1;
        int z = Math.Abs(relative.Z) < Math.Abs(relative.Z + lowerMult) ? 0 : 2;
        int y = Math.Abs(relative.Y) < Math.Abs(relative.Y + lowerMult) ? 0 : 4;

        int nx = 1 - x;
        int nz = 2 - z;
        int ny = 4 - y;

        Chunks[x  + z  + y ] = new Chunk(relative,                                        lowerLod);
        Chunks[nx + z  + y ] = new Chunk(relative + (lowerMult, 0,         0),            lowerLod);
        Chunks[x  + nz + y ] = new Chunk(relative + (0,         0,         lowerMult),    lowerLod);
        Chunks[nx + nz + y ] = new Chunk(relative + (lowerMult, 0,         lowerMult),    lowerLod);

        Chunks[x  + z  + ny] = new Chunk(relative + (0,         lowerMult, 0),            lowerLod);
        Chunks[nx + z  + ny] = new Chunk(relative + (lowerMult, lowerMult, 0),            lowerLod);
        Chunks[x  + nz + ny] = new Chunk(relative + (0,         lowerMult, lowerMult),    lowerLod);
        Chunks[nx + nz + ny] = new Chunk(relative + (lowerMult, lowerMult, lowerMult),    lowerLod);
    }

    public override void CheckResolution(float distance, Vector3 position)
    {
        if (LodLevel <= 1)
            return;

        int lowerLod = LodLevel - 1;
        int lowerMult = 1 << lowerLod;

        float dist = (lowerMult * distance) * (lowerMult * distance);

        for (int i = 0; i < 8; i++)
        {
            var chunk = Chunks[i];
            if (Vector3.DistanceSquared(chunk.Center.YTo0(), position) <= dist)
            {
                Chunks[i] = new ChunkMap(chunk.RelativePosition, chunk.LodLevel);
                Chunks[i].CheckResolution(distance, position);
            }
        }
    }

    public override void GenerateChunks(VoxelRenderer renderer)
    {
        for (int i = 0; i < 8; i++)
        {
            var chunk = Chunks[i];
            chunk.GenerateChunks(renderer);
        }
    }

    public override void GetBoundingBoxes(List<BoundingBoxData> boxes)
    {
        for (int i = 0; i < 8; i++)
        {
            var chunk = Chunks[i];
            chunk.GetBoundingBoxes(boxes);
        }
    }
}