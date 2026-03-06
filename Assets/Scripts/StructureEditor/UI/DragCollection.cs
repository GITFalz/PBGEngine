using PBG.UI.Creator;
using PBG.UI;
using static PBG.UI.Styles;
using PBG.MathLibrary;
using PBG.Data;

public class DragBlockUI(StructureNodeManager nodeManager) : UIScript
{
    public UICol DragBlockCollection = null!;
    private UIImg _blockImg = null!;
    private UIText _blockText = null!;

    public override UIElementBase Script() =>
    newCol(Class(w_[232], top_left, h_[38], blank_sharp_g_[30], depth_[50], hidden, allow_passing_mouse), OnHold(DragBlockHold), OnRelease(DragBlockEnd), Sub([
        newImg(Class(middle_left, h_[38], w_[38], bg_white), ref _blockImg),
        newText("block", Class(mc_[30], fs_[1], middle_left, left_[40], depth_[5]), ref _blockText)
    ]), ref DragBlockCollection);

    public void DragBlockStart(UICol collection)
    {
        string name = collection.Dataset.String("block");
        DragBlockCollection.SetVisible(true);
        DragBlockCollection.Clicked = true;
        DragBlockCollection.Dataset["block"] = name;
        DragBlockCollection.BaseOffset = collection.Origin - (0, nodeManager.structureNodeUI.NoisePaletteBlockSelection.ScrollPosition);
        DragBlockCollection.ApplyChanges(UIChange.Transform);

        _blockImg.UpdateItem(name);
        _blockText.UpdateText(name);
    }

    public void DragBlockHold(UICol collection)
    {
        Vector2 delta = Input.GetMouseDelta();
        if (delta == Vector2.Zero)
            return;
        collection.BaseOffset += delta;
        collection.ApplyChanges(UIChange.Transform);
    }

    public void DragBlockEnd(UICol collection)
    {
        collection.SetVisible(false);
    }
}