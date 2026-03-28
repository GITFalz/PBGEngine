using PBG.MathLibrary;
using PBG.UI.Creator;


namespace PBG.UI
{

    public class UIVCol : UIVCol<UIVCol>
    {
        public UIVCol() : base() { Name = "UIVCol"; }
        
        public UIVCol Ref(ref UIVCol text)
        {
            text = this;
            return text;
        }

        public UIVCol Out(out UIVCol text)
        {
            text = this;
            return text;
        }

        public UIVCol Class(params IStyleData[] styles) => InternalClass(this, styles);

        public UIVCol OnHoverEnter(Action<UIVCol>? action)    { SetOnHoverEnter(action); return this; }
        public UIVCol OnHover(Action<UIVCol>? action)         { SetOnHover(action); return this; }
        public UIVCol OnClick(Action<UIVCol>? action)         { SetOnClick(action); return this; }
        public UIVCol OnHold(Action<UIVCol>? action)          { SetOnHold(action); return this; }
        public UIVCol OnRelease(Action<UIVCol>? action)       { SetOnRelease(action); return this; }
        public UIVCol OnHoverExit(Action<UIVCol>? action)     { SetOnHoverExit(action); return this; }

        public UIVCol this[params IUIChild[] subElements]
        {
            get { AddElements(subElements); return this; }
        }
    }
    
    public class UIVCol<TSelf> : UICol<TSelf> where TSelf : UIVCol<TSelf>
    {
        public UIVCol() : base() { Tag = UIElementTag.UIVerticalCollection; }
        
        public override void CollectionFirstPass()
        {
            float offsetX(UIElementBase child) => child.IsLeftAligned() ? Border.X : (child.IsRightAligned() ? Border.Z : 0);

            float maxWidth = 0;
            float totalHeight = Border.Y;
            
            HashSet<UIElementBase> percentWidthChildren = [];
            HashSet<UIElementBase> growChildren = [];

            if (!GrowFromChildren)
            {
                CalculateHeight();
                CalculateWidth();
            }
            else if (!Width.IsNone())
            {
                CalculateWidth();
            }
            
            ForeachChildren(child =>
            {
                child.FirstPass();
                if (!child.Visible && IgnoreInvisibleElements)
                    return;

                float xOffset = offsetX(child);

                child.CollectionOffset = (xOffset, totalHeight);
                
                if (GrowFromChildren && Width.IsNone())
                {
                    if (child.Width.IsPercent())
                    {
                        percentWidthChildren.Add(child);
                    }
                    else
                    {
                        maxWidth = Mathf.Max(maxWidth, Border.X + child.BaseOffset.X + child.Size.X + Border.Z);
                    }
                }

                totalHeight += child.BaseOffset.Y + child.Size.Y + Spacing;
            });
            if (GrowFromChildren)
            {   
                if (Width.IsNone())
                    Width = UISize.None(maxWidth);

                Height = UISize.Pixels(totalHeight - Spacing + Border.W);
                CalculateWidth();
                CalculateHeight();
                ForeachChildren(percentWidthChildren, child =>
                {
                    child.Width.AddedOffset = -(Border.X + Border.Z);
                    child.CalculateWidth();
                });
            }
        }
        
        public float GetTotalYSize()
        {
            float totalOffset = Border.Y;
            ForeachChildren(child =>
            {
                if (child.Visible || !IgnoreInvisibleElements)
                {
                    totalOffset += child.BaseOffset.Y + child.Size.Y + Spacing;
                }
            });
            return totalOffset - Spacing + Border.W;
        }
    }
}