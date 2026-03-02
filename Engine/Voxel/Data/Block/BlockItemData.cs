using System.Runtime.InteropServices;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Voxel;

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

    public override void GenerateIcon()
    {
        /*
        List<BlockVertexData> vertices = [];
        List<uint> indices = [];
        Block.GenerateFullBlock(new IconVoxelHandler(vertices, indices), (-0.5f, -0.5f, -0.5f));

        VAO vao = new();
        Shader.Error("Error creating block data VAO");
        IBO ibo = new(indices);
        Shader.Error("Error creating block data IBO");
        VBO<BlockVertexData> vertexVBO = new(vertices);
        Shader.Error("Error creating block data VBO");

        vao.Bind();

        vertexVBO.Bind();
        int stride = Marshal.SizeOf<BlockVertexData>();

        vao.Link(0, 3, VertexAttribPointerType.Float, stride, 0);
        vao.Link(1, 2, VertexAttribPointerType.Float, stride, sizeof(float) * 3);
        vao.Link(2, 3, VertexAttribPointerType.Float, stride, sizeof(float) * 5);
        vao.IntLink(3, 1, VertexAttribIntegerType.Int, stride, sizeof(float) * 8);

        vertexVBO.Unbind();

        vao.Unbind();

        GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        vao.Bind();
        ibo.Bind();

        GL.DrawElements(PrimitiveType.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0);

        Shader.Error("Rendering blocks for icons");

        ibo.Unbind();
        vao.Unbind();

        ItemDataManager.Data.Add(FBO.GetPixels(0, 0, 128, 128, false));

        vertexVBO.DeleteBuffer();
        ibo.DeleteBuffer();
        vao.DeleteBuffer();
        */
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