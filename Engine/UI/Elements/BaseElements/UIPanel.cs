using System.Diagnostics.CodeAnalysis;
using PBG.MathLibrary;
using PBG.UI.Creator;


namespace PBG.UI
{
    public interface IUIPanel
    {
        Vector4 Color { get; set; }
        Vector4 Transform { get; set; }
        Vector4 BorderColor { get; set; }
        Vector4 BorderUI { get; set; }
        int TextureID { get; set; }
        bool Visible { get; }
        int MaskIndex { get; }
        Vector2 Size { get; set; }
        Vector2 Slice { get; set; }
        Vector2 AnimationTranslation { get; set; }
        float AnimationScale { get; set; }
        float AnimationRotation { get; set; }
        bool IsValid => Visible && (Color.W + BorderColor.W != 0);
        UIElementBase UpdateColor();
        string GetName();
    }

    public abstract class UIPanel<TSelf> : UIElement<TSelf>, IUIPanel where TSelf : UIPanel<TSelf>
    {
        public int TextureID { get; set; } = -1;
        public Vector2 Slice { get; set; } = (-1, -1);
        public Vector4 BorderColor { get; set; } = Vector4.Zero;
        public Vector4 BorderUI { get; set;} = Vector4.Zero;

        public UIPanel(UIStyleData[] classes, IEvent[] events) : base((0, 0, 0, 0), classes, events) { }

        public string GetName() => Name;

        public override void FirstPass()
        {
            base.FirstPass();
        }

        public override void UpdateChildMaskIndex(int index) => UIController?.UIMesh.UpdateMaskIndex(this, index);
        public override void UpdateTextureIndex(int textureIndex)
        {
            TextureID = textureIndex;
            UIController?.UIMesh.UpdateTextureIndex(this);
        }
        public override void UpdateIconIndex(int iconIndex)
        {
            TextureID = iconIndex | 0x20000000;
            UIController?.UIMesh.UpdateTextureIndex(this);
        }

        public override void UpdateItem(string name)
        {
            if (!ItemDataManager.AllItems.TryGetValue(name, out var item))
                return;

            TextureID = item.Index | 0x40000000;
            UIController?.UIMesh.UpdateTextureIndex(this);
        }

        public override void Generate()
        {
            if (ParentElement != null && !ParentElement.Visible)
                Visible = false;
                
            ControllerCheck().UIMesh.AddElement(this);
        }
        public override bool GetMaskPanel([NotNullWhen(true)] out PBG.Rendering.Mask.UIMaskStruct? mask)
        {
            mask = null;
            if (UIController == null)
                return false;

            return UIController.MaskData.GetMask(MaskIndex, out mask);
        }
        public override UIElementBase UpdateTransform() { UIController?.UIMesh.UpdateTransform(this); return this; }
        public override UIElementBase UpdateScale() { UIController?.UIMesh.UpdateScale(this); return this; }
        public override UIElementBase UpdateColor() { UIController?.UIMesh.UpdateColor(this); return this; }
        public override UIElementBase UpdateBorderUI() { UIController?.UIMesh.UpdateBorderUI(this); return this; }
        public override UIElementBase UpdateBorderColor() { UIController?.UIMesh.UpdateBorderColor(this); return this; }
        public override UIElementBase UpdateBorderColor(Vector4 color) { BorderColor = color; return UpdateBorderColor(); }
        public override UIElementBase UpdateAnimationTranslation() { UIController?.UIMesh.UpdateAnimationTranslation(this); return this; }
        public override UIElementBase UpdateAnimationScale() { UIController?.UIMesh.UpdateAnimationScale(this); return this; }
        public override UIElementBase UpdateAnimationRotation() { UIController?.UIMesh.UpdateAnimationRotation(this); return this; }

        public override UIElementBase SetVisible(bool visible)
        {
            base.SetVisible(visible);
            UIController?.UIMesh.QueueUpdateVisibility();
            return this;
        }
        public override void Destroy() => ControllerCheck().UIMesh.RemoveElement(this);
    }
}