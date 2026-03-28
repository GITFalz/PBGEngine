using PBG.Data;
using PBG.MathLibrary;
using PBG.UI.Creator;


namespace PBG.UI
{
    public class UIHScroll : UIHScroll<UIHScroll>
    {
        public UIHScroll() : base() { Name = "UIHScroll"; }
        
        public UIHScroll Ref(ref UIHScroll text)
        {
            text = this;
            return text;
        }

        public UIHScroll Out(out UIHScroll text)
        {
            text = this;
            return text;
        }

        public UIHScroll Class(params IStyleData[] styles) => InternalClass(this, styles);

        public UIHScroll OnHoverEnter(Action<UIHScroll>? action)    { SetOnHoverEnter(action); return this; }
        public UIHScroll OnHover(Action<UIHScroll>? action)         { SetOnHover(action); return this; }
        public UIHScroll OnClick(Action<UIHScroll>? action)         { SetOnClick(action); return this; }
        public UIHScroll OnHold(Action<UIHScroll>? action)          { SetOnHold(action); return this; }
        public UIHScroll OnRelease(Action<UIHScroll>? action)       { SetOnRelease(action); return this; }
        public UIHScroll OnHoverExit(Action<UIHScroll>? action)     { SetOnHoverExit(action); return this; }

        public UIHScroll this[params IUIChild[] subElements]
        {
            get { AddElements(subElements); return this; }
        }

        public Action<UIHScroll>? ScrollAction = null;

        public override void OnHoverAction()
        {
            base.OnHoverAction();
            Scroll(this);
        }

        public override bool IsInteractable() => true;   

        public UIHScroll SetOnScroll(Action<UIHScroll>? action)
        {
            ScrollAction = action; return this;
        }
    }   
    public class UIHScroll<TSelf> : UIHCol<TSelf> where TSelf : UIHScroll<TSelf>
    {
        public UIHScroll() : base() { Tag = UIElementTag.UIHorizontalScrollView; }
        
        public float ScrollPosition = 0;

        public override void CollectionFirstPass()
        {
            float offsetY(UIElementBase child) => child.IsTopAligned() ? Border.Y : (child.IsBottomAligned() ? Border.W : 0);

            float totalWidth = Border.X - ScrollPosition;
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

        public static void Scroll(UIHScroll scrollView)
        {
            float scrollDelta = -Input.GetMouseScrollDelta().Y;
            if (scrollDelta == 0 || scrollView.ContainsHoveringScrollView())
                return;
            
            var smallestSize = scrollView.GetMaskedSize();

            float max = Mathf.Max(0, scrollView.GetTotalXSize() - smallestSize.X + (scrollView.AllowScrollingToTop ? scrollView.Size.X : 0));
            float newScroll = scrollView.ScrollPosition + scrollDelta * scrollView.ScrollingSpeed;
            float oldScrollPosition = scrollView.ScrollPosition;
            scrollView.ScrollPosition = Mathf.Clampy(newScroll, 0, max);
            float delta = scrollView.ScrollPosition - oldScrollPosition;
            scrollView.ForeachChildren(child =>
            {
                child.CollectionOffset.X -= delta;
            });
        
            scrollView.SecondPass();
            scrollView.UpdateTransform();
            scrollView.ScrollAction?.Invoke(scrollView);
        }
    }
}