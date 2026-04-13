using PBG.Graphics;
using PBG.Graphics.Vulkan;
using PBG.Mathematics;
using PBG.Voxel;
using Silk.NET.Vulkan;

public class BlockItemData : ItemData
{
    public uint BlockIndex;
    public BlockDefinition Block;
    
    public BlockItemData(BlockDefinition block, uint index)
    {
        Block = block;
        Name = block.Name;
        BlockIndex = index;
        MaxStackSize = 999;
        ItemDataManager.BlockCount++;
        Base(); 
    }

    public override void GenerateIcon() => GenerateIcon(GFX.CommandBuffer);
    public void GenerateIcon(CommandBuffer commandBuffer)
    {
        List<BlockVertexData> vertices = [];
        List<uint> indices = [];
        Block.GenerateFullBlock(new IconVoxelHandler(vertices, indices), (-0.5f, -0.5f, -0.5f));

        if (indices.Count == 0 || vertices.Count == 0)
        {
            Console.WriteLine("[Warning] : Block with no indices");
            return;
        }

        IBO ibo = new([..indices]);
        VBO<BlockVertexData> vbo = new([..vertices]);

        vbo.Bind(commandBuffer);
        ibo.Bind(commandBuffer);

        GFX.DrawIndexed(commandBuffer, (uint)indices.Count, 1, 0, 0, 0);

        ibo.Dispose();
        vbo.Dispose();
    }

    public override void RenderIcon(Vector2 position, float scale)
    {
        throw new NotImplementedException();
    }

    public override void RenderIcon(Vector3 position, float scale)
    {
        throw new NotImplementedException();
    }

    private class IconVoxelHandler(List<BlockVertexData> vertices, List<uint> indices) : BaseVoxelChunkHandler((0, 0, 0), [])
    {
        public override Block GetBlock(Vector3i position) => PBG.Voxel.Block.Air;
        public override void AddFace(VoxelFace face, Vector3 position)
        {
            uint o = (uint)vertices.Count;
            BlockDefinition.AddFace(vertices, face, position);
            indices.Add(0+o);
            indices.Add(1+o);
            indices.Add(2+o);
            indices.Add(2+o);
            indices.Add(3+o);
            indices.Add(0+o);
        }
    }
}