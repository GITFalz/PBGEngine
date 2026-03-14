using System.Runtime.InteropServices;
using PBG.Graphics;
using PBG.MathLibrary;
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

    public override void RenderIcon(Vector2 position, float scale) { RenderIcon((position.X, position.Y, 0), scale); }
    public override void RenderIcon(Vector3 position, float scale) 
    {
        /*
        IconShader.Bind();
        ItemDataManager.Image.Bind(TextureUnit.Texture0);
        IconVAO.Bind();

        Matrix4 model = Matrix4.CreateScale(scale) * Matrix4.CreateTranslation(position.X, position.Y, position.Z);
        Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0, Game.Width, Game.Height, 0, -1, 1);

        GL.UniformMatrix4(IconModelLocation, true, ref model);
        GL.UniformMatrix4(IconProjectionLocation, true, ref projection);
        GL.Uniform2(IconSizeLocation, new Vector2(100, 100));
        GL.Uniform1(IconTextureLocation, 0);
        GL.Uniform1(IconIndexLocation, Index);

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        //Shader.Error("Error rendering icon: ");

        IconVAO.Unbind();
        ItemDataManager.Image.Unbind();
        IconShader.Unbind();
        */
    }

    /*
    public override void LeftClick(ItemSlot slot)
    {
        if (RemoveBlock(PlayerData.LookingAtBlockPosition, out Block swappedBlock) && BlockManager.GetBlock(swappedBlock.BlockId(), out var block))
        {
            Console.WriteLine($"Swapped {block}");
            PlayerInventoryManager.AddBlock(block);
        }
    }
    public override void RightClick(ItemSlot slot) 
    { 
        if (slot.Amount <= 0 || slot.Inventory == null)
            return;

        if (PlaceBlock(PlayerData.LookingAtBlockPlacementPosition, Block))
        {
            slot.Inventory.Remove(slot, 1);
        }
    }
    */
}