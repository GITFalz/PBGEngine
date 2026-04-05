using PBG.Mathematics;
using PBG.UI.Creator;


namespace PBG.UI
{
    public class UIHCol : UICol
    {
        protected virtual float TotalWidth => Border.X;

        public UIHCol() : base() 
        { 
            Name = "UIHCol"; 
            Tag = UIElementTag.UIHorizontalCollection;
        }

        public UIHCol(params IStyleData[] styles) : this()
        { 
            Class(styles);
        }
        
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

        public new UIHCol Class(params IStyleData[] styles) => InternalClass(this, styles);

        public UIHCol OnHoverEnter(Action<UIHCol>? action)    { UIEventExtensions.OnHoverEnter(this, action); return this; }
        public UIHCol OnHover(Action<UIHCol>? action)         { UIEventExtensions.OnHover(this, action); return this; }
        public UIHCol OnClick(Action<UIHCol>? action)         { UIEventExtensions.OnClick(this, action); return this; }
        public UIHCol OnHold(Action<UIHCol>? action)          { UIEventExtensions.OnHold(this, action); return this; }
        public UIHCol OnRelease(Action<UIHCol>? action)       { UIEventExtensions.OnRelease(this, action); return this; }
        public UIHCol OnHoverExit(Action<UIHCol>? action)     { UIEventExtensions.OnHoverExit(this, action); return this; }

        public new UIHCol this[params IUIChild[] subElements]
        {
            get { AddElements(subElements); return this; }
        }

        public override void CollectionFirstPass()
        {
            if (!GrowFromChildren)
            {
                CalculateHeight();
                CalculateWidth();
            }
            else if (!Height.IsNone())
            {
                CalculateHeight();
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
            float totalWidth = TotalWidth;

            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                child.FirstPass();
                if (!child.Visible && IgnoreInvisibleElements)
                    continue;

                float yOffset = OffsetY(child);
                child.CollectionOffset = (totalWidth, yOffset);

                totalWidth += child.BaseOffset.X + child.Size.X + Spacing;
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
            float availableWidth = Size.X;
            float totalWidth = TotalWidth;
            float totalPercent = 0;

            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                child.FirstPass();
                if (!child.Visible && IgnoreInvisibleElements)
                    continue;
                    
                if (child.Width.IsPercent())
                {
                    totalPercent += child.Width.Value;
                }
                else
                {
                    availableWidth -= child.Size.X;
                }
            };

            availableWidth -= (Spacing * (ChildElements.Count - 1)) + Border.X + Border.Z;
            availableWidth = availableWidth.Max(0);

            availableWidth *= 1 / totalPercent;

            var sizeX = SizeX;
            SizeX = availableWidth;

            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                if (!child.Visible && IgnoreInvisibleElements)
                    continue;

                float yOffset = OffsetY(child);
                child.CollectionOffset = (totalWidth, yOffset);

                if (child.Width.IsPercent())
                {
                    child.CalculateWidth();
                }
                
                totalWidth += child.BaseOffset.X + child.Size.X + Spacing;
            }

            SizeX = sizeX;
        }

        private void HandleGrowFromChildren()
        {
            float totalWidth = TotalWidth;
            float maxHeight = 0;

            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                child.PercentAlignement = PercentAlignementType.None;
                child.FirstPass();
                if (!child.Visible && IgnoreInvisibleElements)
                    continue;

                float yOffset = OffsetY(child);
                child.CollectionOffset = (totalWidth, yOffset);

                if (Height.IsNone())
                {
                    if (child.Height.IsPercent())
                        child.PercentAlignement = PercentAlignementType.Vertical;
                    else
                        maxHeight = Mathf.Max(maxHeight, Border.Y + child.BaseOffset.Y + child.Size.Y + Border.W);
                }    
                
                totalWidth += child.BaseOffset.X + child.Size.X + Spacing;
            };

            Width = UISize.Pixels(totalWidth - Spacing + Border.Z);
            if (Height.IsNone())
                Height = UISize.None(maxHeight);
                
            CalculateWidth();
            CalculateHeight();

            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                if (child.PercentAlignement.HasFlag(PercentAlignementType.Vertical))
                {
                    child.Height.AddedOffset = -(Border.Y + Border.W);
                    child.CalculateHeight();
                }
            }
        }

        public float GetTotalXSize()
        {
            float totalOffset = Border.X;
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                if (child.Visible || !IgnoreInvisibleElements)
                {
                    totalOffset += child.Width.Value + Spacing;
                }
            };
            return totalOffset - Spacing + Border.Z;
        }
    }
}