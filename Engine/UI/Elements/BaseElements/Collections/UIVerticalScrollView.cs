using PBG.Data;
using PBG.MathLibrary;
using PBG.UI.Creator;


namespace PBG.UI
{
    public class UIVScroll : UIVCol
    {
        public Action<UIVScroll>? ScrollAction = null;
        protected override float TotalHeight => Border.Y - ScrollPosition;
        public float ScrollPosition = 0;

        public UIVScroll() : base() 
        { 
            Name = "UIVScroll";
            Tag = UIElementTag.UIVerticalScrollView;
        }

        public UIVScroll(params IStyleData[] styles) : this()
        { 
            Class(styles);
        }
        
        public UIVScroll Ref(ref UIVScroll text)
        {
            text = this;
            return text;
        }

        public UIVScroll Out(out UIVScroll text)
        {
            text = this;
            return text;
        }

        public new UIVScroll Class(params IStyleData[] styles) => InternalClass(this, styles);

        public UIVScroll OnHoverEnter(Action<UIVScroll>? action)    { UIEventExtensions.OnHoverEnter(this, action); return this; }
        public UIVScroll OnHover(Action<UIVScroll>? action)         { UIEventExtensions.OnHover(this, action); return this; }
        public UIVScroll OnClick(Action<UIVScroll>? action)         { UIEventExtensions.OnClick(this, action); return this; }
        public UIVScroll OnHold(Action<UIVScroll>? action)          { UIEventExtensions.OnHold(this, action); return this; }
        public UIVScroll OnRelease(Action<UIVScroll>? action)       { UIEventExtensions.OnRelease(this, action); return this; }
        public UIVScroll OnHoverExit(Action<UIVScroll>? action)     { UIEventExtensions.OnHoverExit(this, action); return this; }

        public new UIVScroll this[params IUIChild[] subElements]
        {
            get { AddElements(subElements); return this; }
        }

        public override void OnHoverAction()
        {
            base.OnHoverAction();
            Scroll(this);
        }

        public override bool IsInteractable() => true;   

        public UIVScroll SetOnScroll(Action<UIVScroll>? action)
        {
            ScrollAction = action; return this;
        }

        public static void Scroll(UIVScroll scrollView)
        {
            float scrollDelta = -Input.GetMouseScrollDelta().Y;
            if (scrollDelta == 0 || scrollView.ContainsHoveringScrollView())
                return;

            var smallestSize = scrollView.GetMaskedSize();

            float max = Mathf.Max(0, scrollView.GetTotalYSize() - smallestSize.Y + (scrollView.AllowScrollingToTop ? scrollView.Size.Y : 0));
            float newScroll = scrollView.ScrollPosition + scrollDelta * scrollView.ScrollingSpeed;
            float oldScrollPosition = scrollView.ScrollPosition;
            scrollView.ScrollPosition = Mathf.Clampy(newScroll, 0, max);
            float delta = scrollView.ScrollPosition - oldScrollPosition;
            for (int i = 0; i < scrollView.ChildElements.Count; i++)
            {
                var child = scrollView.ChildElements[i];
                child.CollectionOffset.Y -= delta;
            };

            scrollView.SecondPass();
            scrollView.UpdateTransform();
            scrollView.ScrollAction?.Invoke(scrollView);
        }
    }
}