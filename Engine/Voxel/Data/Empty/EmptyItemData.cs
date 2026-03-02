
using PBG.MathLibrary;

public class EmptyItemData : ItemData
{
    public EmptyItemData()
    {
        Name = "empty";
        MaxStackSize = 0;
        Base();
    }

    public override void GenerateIcon() {}
    public override void RenderIcon(Vector2 position, float scale) {}
    public override void RenderIcon(Vector3 position, float scale) {}
    /*
    public override void LeftClick(ItemSlot slot)
    {
        if (RemoveBlock(PlayerData.LookingAtBlockPosition, out Block swappedBlock) && BlockManager.GetBlock(swappedBlock.BlockId(), out var block))
        {
            PlayerInventoryManager.AddBlock(block);
        }
    }
    public override void RightClick(ItemSlot slot) { }
    */
}