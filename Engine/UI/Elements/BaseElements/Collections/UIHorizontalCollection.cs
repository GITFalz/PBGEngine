using PBG.MathLibrary;
using PBG.UI.Creator;


namespace PBG.UI
{
    public class UIHCol : UIHCol<UIHCol>
    {
        public UIHCol() : base() { Name = "UIHCol"; }
        
        public UIHCol Ref(ref UIHCol text)
        {
            text = this;
            return text;
        }

        public UIHCol Out(out UIHCol text)
        {
            text = this;
            return text;
        }

        public UIHCol Class(params IStyleData[] styles) => InternalClass(this, styles);

        public UIHCol OnHoverEnter(Action<UIHCol>? action)    { SetOnHoverEnter(action); return this; }
        public UIHCol OnHover(Action<UIHCol>? action)         { SetOnHover(action); return this; }
        public UIHCol OnClick(Action<UIHCol>? action)         { SetOnClick(action); return this; }
        public UIHCol OnHold(Action<UIHCol>? action)          { SetOnHold(action); return this; }
        public UIHCol OnRelease(Action<UIHCol>? action)       { SetOnRelease(action); return this; }
        public UIHCol OnHoverExit(Action<UIHCol>? action)     { SetOnHoverExit(action); return this; }

        public UIHCol this[params IUIChild[] subElements]
        {
            get { AddElements(subElements); return this; }
        }
    }
    
    public class UIHCol<TSelf> : UICol<TSelf> where TSelf : UIHCol<TSelf>
    {
        public UIHCol() : base() { Tag = UIElementTag.UIHorizontalCollection; }

        public override void CollectionFirstPass()
        {
            float offsetY(UIElementBase child) => child.IsTopAligned() ? Border.Y : (child.IsBottomAligned() ? Border.W : 0);

            float totalWidth = Border.X;
            float maxHeight = 0;
            
            HashSet<UIElementBase> percentHeightChildren = [];

            if (!GrowFromChildren)
            {
                CalculateHeight();
                CalculateWidth();
            }
            else if (!Height.IsNone())
            {
                CalculateWidth();
            }
            
            ForeachChildren(child =>
            {
                child.FirstPass();
                if (!child.Visible && IgnoreInvisibleElements)
                    return;

                float yOffset = offsetY(child);

                child.CollectionOffset = (totalWidth, yOffset);

                if (GrowFromChildren)
                {
                    if (child.Height.IsPercent() && Height.IsNone())
                    {
                        percentHeightChildren.Add(child);
                    }
                    else
                    {
                        maxHeight = Mathf.Max(maxHeight, Border.Y + child.BaseOffset.Y + child.Size.Y + Border.W);
                    }
                }    
                
                totalWidth += child.BaseOffset.X + child.Size.X + Spacing;
            });
            if (GrowFromChildren)
            {
                Width = UISize.Pixels(totalWidth - Spacing + Border.Z);
                if (Height.IsNone())
                    Height = UISize.None(maxHeight);
                    
                CalculateWidth();
                CalculateHeight();
                ForeachChildren(percentHeightChildren, child =>
                {
                    child.Height.AddedOffset = -(Border.Y + Border.W);
                    child.CalculateHeight();
                });
            }
        }

        public float GetTotalXSize()
        {
            float totalOffset = Border.X;
            ForeachChildren(child =>
            {
                if (child.Visible || !IgnoreInvisibleElements)
                {
                    totalOffset += child.Width.Value + Spacing;
                }
            });
            return totalOffset - Spacing + Border.Z;
        }
    }
}