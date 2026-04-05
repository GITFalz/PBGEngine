using PBG.Data;
using PBG.MathLibrary;
using PBG.UI.Creator;


namespace PBG.UI
{
    public class UIHScroll : UIHCol
    {
        public float ScrollPosition = 0;
        protected override float TotalWidth => Border.X - ScrollPosition;

        public UIHScroll() : base() 
        { 
            Name = "UIHScroll"; 
            Tag = UIElementTag.UIHorizontalScrollView;
        }

        public UIHScroll(params IStyleData[] styles) : this()
        { 
            Class(styles);
        }
        
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

        public new UIHScroll Class(params IStyleData[] styles) => InternalClass(this, styles);

        public UIHScroll OnHoverEnter(Action<UIHScroll>? action)    { UIEventExtensions.OnHoverEnter(this, action); return this; }
        public UIHScroll OnHover(Action<UIHScroll>? action)         { UIEventExtensions.OnHover(this, action); return this; }
        public UIHScroll OnClick(Action<UIHScroll>? action)         { UIEventExtensions.OnClick(this, action); return this; }
        public UIHScroll OnHold(Action<UIHScroll>? action)          { UIEventExtensions.OnHold(this, action); return this; }
        public UIHScroll OnRelease(Action<UIHScroll>? action)       { UIEventExtensions.OnRelease(this, action); return this; }
        public UIHScroll OnHoverExit(Action<UIHScroll>? action)     { UIEventExtensions.OnHoverExit(this, action); return this; }

        public new UIHScroll this[params IUIChild[] subElements]
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
            for (int i = 0; i < scrollView.ChildElements.Count; i++)
            {
                var child = scrollView.ChildElements[i];
                child.CollectionOffset.X -= delta;
            };
        
            scrollView.SecondPass();
            scrollView.UpdateTransform();
            scrollView.ScrollAction?.Invoke(scrollView);
        }
    }
}