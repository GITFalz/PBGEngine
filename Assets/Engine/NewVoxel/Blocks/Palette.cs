namespace PBG.NewVoxel;

public struct Palette
{
    public Block[] Blocks;

    public Palette()
    {
        Blocks = new Block[256];
        Blocks[0] = Block.Air;
    }
}