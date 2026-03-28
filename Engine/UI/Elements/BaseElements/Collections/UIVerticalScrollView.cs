using PBG.Data;
using PBG.MathLibrary;
using PBG.UI.Creator;


namespace PBG.UI
{
    public class UIVScroll : UIVScroll<UIVScroll>
    {
        public UIVScroll() : base() { Name = "UIVScroll"; }
        
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

        public UIVScroll Class(params IStyleData[] styles) => InternalClass(this, styles);

        public UIVScroll OnHoverEnter(Action<UIVScroll>? action)    { SetOnHoverEnter(action); return this; }
        public UIVScroll OnHover(Action<UIVScroll>? action)         { SetOnHover(action); return this; }
        public UIVScroll OnClick(Action<UIVScroll>? action)         { SetOnClick(action); return this; }
        public UIVScroll OnHold(Action<UIVScroll>? action)          { SetOnHold(action); return this; }
        public UIVScroll OnRelease(Action<UIVScroll>? action)       { SetOnRelease(action); return this; }
        public UIVScroll OnHoverExit(Action<UIVScroll>? action)     { SetOnHoverExit(action); return this; }

        public UIVScroll this[params IUIChild[] subElements]
        {
            get { AddElements(subElements); return this; }
        }

        public Action<UIVScroll>? ScrollAction = null;

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
    }
    public class UIVScroll<TSelf> : UIVCol<TSelf> where TSelf : UIVScroll<TSelf>
    {
        protected override float TotalHeight => Border.Y - ScrollPosition;
        public float ScrollPosition = 0;

        public UIVScroll() : base() { Tag = UIElementTag.UIVerticalScrollView; }

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