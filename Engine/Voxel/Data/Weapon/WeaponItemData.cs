using System.Runtime.InteropServices;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Voxel;

public class WeaponItemData : ItemData
{
    public uint WeaponIndex;
    //public SimpleModel Model;
    
    /*
    public WeaponItemData(uint index, SimpleModel model)
    {
        WeaponIndex = index;
        Name = model.Name;
        //Model = model;
        ItemDataManager.WeaponCount++;
        Base(); 
    }
    */

    public override void GenerateIcon()
    {
        /*
        var size = Model.VertexPositionMax - Model.VertexPositionMin;

        Matrix4 model = Matrix4.CreateTranslation(GetCenteringOffset(Model.VertexPositionMin, Model.VertexPositionMax)) * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(45)) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(135)) * Matrix4.CreateScale(40 * Mathf.Max(size.X, size.Y, size.Z)) * Matrix4.CreateTranslation(64, 64, 0);
        Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0, 128, 128, 0, -64, 64);
        Matrix4 view = Matrix4.Identity;

        Model.Render(model, view, projection);
        */

        //ItemDataManager.Data.Add(FBO.GetPixels(0, 0, 128, 128, false));
    }

    public static Vector3 GetCenteringOffset(Vector3 min, Vector3 max)
    {
        Vector3 center = (min + max) * 0.5f;
        return -center; // how much you need to move the model
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