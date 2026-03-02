using PBG.Physics;
using PBG.Graphics;
using PBG.Voxel;
using PBG.MathLibrary;

public abstract class ItemData
{
    //public static ShaderProgram IconShader = new ShaderProgram("Utils/Rectangle.vert", "Utils/ArrayImage.frag");
    //public static VAO IconVAO = new VAO();

    public static int IconModelLocation = -1;
    public static int IconProjectionLocation = -1;
    public static int IconSizeLocation = -1;
    public static int IconTextureLocation = -1;
    public static int IconIndexLocation = -1;

    //protected static FBO FBO => ItemDataManager.FBO;

    protected static List<byte[]> Data => ItemDataManager.Data;

    protected static int CubeModelLocation => ItemDataManager.CubeModelLocation;
    protected static int CubeProjectionLocation => ItemDataManager.CubeProjectionLocation;
    protected static int CubeTextureLocation => ItemDataManager.CubeTextureLocation;
    protected static int CubeIndicesLocation => ItemDataManager.CubeIndicesLocation;

    public string Name = "empty";
    public int Index = 0;
    public int MaxStackSize = 0;

    static ItemData()
    {
        /*
        IconShader.Bind();

        IconModelLocation = IconShader.GetLocation("model");
        IconProjectionLocation = IconShader.GetLocation("projection");
        IconSizeLocation = IconShader.GetLocation("size");
        IconTextureLocation = IconShader.GetLocation("textureArray");
        IconIndexLocation = IconShader.GetLocation("index");

        IconShader.Unbind();
        */

        //FBO.Bind();
    }

    public void Base()
    {
        ItemDataManager.AllItems.Add(Name, this);
        ItemDataManager.Items.Add(this);
        Index = ItemDataManager.AllItems.Count - 1;
    } 

    public abstract void GenerateIcon();
    public abstract void RenderIcon(Vector2 position, float scale);
    public abstract void RenderIcon(Vector3 position, float scale);
    //public abstract void LeftClick(ItemSlot slot);
    //public abstract void RightClick(ItemSlot slot);
    
    public bool IsEmpty() => this is EmptyItemData;

    public override bool Equals(object? obj)
    {
        if (obj is ItemData itemData)
        {
            return Index == itemData.Index;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Index.GetHashCode();
    }

    /*
    public static bool PlaceBlock(Vector3i position, Block block)
    {
        return PlayerData.LookingAtBlock && !BlockCollision.IsColliding(PlayerData.PhysicsBody.GetCollider(), position, 1) && WorldManager.SetBlock(PlayerData.LookingAtBlockPlacementPosition, block, out VoxelChunk? chunkData);
    }

    public static bool RemoveBlock(Vector3i position, out Block swappedBlock)
    {
        return WorldManager.GetBlockState(position, out swappedBlock) == 0 && PlayerData.LookingAtBlock && WorldManager.SetBlock(position, Block.Air, out _);  
    }
    */
}