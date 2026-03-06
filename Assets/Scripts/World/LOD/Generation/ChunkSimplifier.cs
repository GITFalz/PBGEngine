using PBG.MathLibrary;
using PBG.Voxel;

public static class ChunkSimplifier
{
    public static void Simplify(VoxelChunk chunk)
    {
        GridBlock[] gridBlocks = new GridBlock[4096];

        for (int i = 0; i < 4096; i++)
        {
            int gX = i & 15;
            int gY = (i >> 4) & 15;
            int gZ = (i >> 8) & 15;
            Vector3i position = (gX * 2, gY * 2, gZ * 2);

            var gb = new GridBlock();
            gb.SetPosition(position);
            gb.SetSize((2, 2, 2));
            gridBlocks[i] = gb;
        }

        for (int i = 0; i < 4096; i++)
        {
            var gb = gridBlocks[i];

        }
    }

    public class GridBlock
    {
        public int Position;
        public int Size;
        public BlockBias[] BlockBiases = [];


        public Vector3i GetPosition() => (Position & 0xFF, (Position >> 0xFF) & 0xFF, (Position >> 0xFFFF) & 0xFF);
        public void SetPosition(Vector3i p) => Position = (p.X & 0xFF) | ((p.Y & 0xFF) << 0xFF) | ((p.Z & 0xFF) << 0xFFFF);

        public Vector3i GetSize() => (Size & 0xFF, (Size >> 0xFF) & 0xFF, (Size >> 0xFFFF) & 0xFF);
        public void SetSize(Vector3i s) => Size = (s.X & 0xFF) | ((s.Y & 0xFF) << 0xFF) | ((s.Z & 0xFF) << 0xFFFF);

        public void InitBlockBiases(VoxelChunk chunk)
        {
            Vector3i position = GetPosition();
            
        }
    }

    public struct BlockBias
    {
        public Block Block;
        public Vector3 Bias;
    }
}