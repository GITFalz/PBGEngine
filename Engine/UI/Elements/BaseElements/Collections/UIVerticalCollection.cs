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
        protected virtual float TotalHeight => Border.Y;

        public UIVCol() : base() { Tag = UIElementTag.UIVerticalCollection; }
        
        public override void CollectionFirstPass()
        {
            if (!GrowFromChildren)
            {
                CalculateHeight();
                CalculateWidth();
            }
            else if (!Width.IsNone())
            {
                CalculateWidth();
            }

            if (FitChildren)
                HandleFitChildren();
            else if (GrowFromChildren)
                HandleGrowFromChildren();
            else
                HandleBasicFirstPass();
        }

        private void HandleBasicFirstPass()
        {
            float totalHeight = TotalHeight;

            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                child.PercentAlignement = PercentAlignementType.None;
                child.FirstPass();
                if (!child.Visible && IgnoreInvisibleElements)
                    continue;

                float xOffset = OffsetX(child);
                child.CollectionOffset = (xOffset, totalHeight);

                totalHeight += child.BaseOffset.Y + child.Size.Y + Spacing;
            };
        }

        /// <summary>
        /// Calculates the width and horizontal offsets of child elements when using
        /// a fit-children layout. Fixed-width children are allocated space first,
        /// then remaining space is distributed between percentage-width children.
        /// 
        /// Percentage widths are normalized so that if multiple children use 100%,
        /// they share the available space equally instead of overflowing.
        /// </summary>
        private void HandleFitChildren()
        {
            float availableHeight = Size.Y;
            float totalHeight = TotalHeight;
            float totalPercent = 0;

            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                child.FirstPass();
                if (!child.Visible && IgnoreInvisibleElements)
                    continue;
                    
                if (child.Height.IsPercent())
                {
                    totalPercent += child.Height.Value;
                }
                else
                {
                    availableHeight -= child.Size.Y;
                }
            };

            availableHeight -= (Spacing * (ChildElements.Count - 1)) + Border.Y + Border.W;
            availableHeight = availableHeight.Max(0);

            availableHeight *= 1 / totalPercent;

            var sizeY = SizeY;
            SizeY = availableHeight;

            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                if (!child.Visible && IgnoreInvisibleElements)
                    continue;

                float xOffset = OffsetX(child);
                child.CollectionOffset = (xOffset, totalHeight);

                if (child.Height.IsPercent())
                {
                    child.CalculateHeight();
                }
                
                totalHeight += child.BaseOffset.Y + child.Size.Y + Spacing;
            }

            SizeY = sizeY;
        }

        private void HandleGrowFromChildren()
        {
            float totalHeight = TotalHeight;
            float maxWidth = 0;

            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                child.PercentAlignement = PercentAlignementType.None;
                child.FirstPass();
                if (!child.Visible && IgnoreInvisibleElements)
                    continue;

                float xOffset = OffsetX(child);
                child.CollectionOffset = (xOffset, totalHeight);

                if (Width.IsNone())
                {
                    if (child.Width.IsPercent())
                        child.PercentAlignement = PercentAlignementType.Horizontal;
                    else
                        maxWidth = Mathf.Max(maxWidth, Border.X + child.BaseOffset.X + child.Size.X + Border.Z);
                }

                totalHeight += child.BaseOffset.Y + child.Size.Y + Spacing;
            };

            Height = UISize.Pixels(totalHeight - Spacing + Border.W);
            if (Width.IsNone())
                Width = UISize.None(maxWidth);

            CalculateWidth();
            CalculateHeight();

            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                if (child.PercentAlignement.HasFlag(PercentAlignementType.Horizontal))
                {
                    child.Width.AddedOffset = -(Border.X + Border.Z);
                    child.CalculateWidth();
                }
            };
        }
        
        public float GetTotalYSize()
        {
            float totalOffset = Border.Y;
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                if (child.Visible || !IgnoreInvisibleElements)
                {
                    totalOffset += child.BaseOffset.Y + child.Size.Y + Spacing;
                }
            };
            return totalOffset - Spacing + Border.W;
        }
    }
}