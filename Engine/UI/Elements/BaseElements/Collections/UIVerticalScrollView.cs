using PBG.Data;
using PBG.MathLibrary;
using PBG.UI.Creator;


namespace PBG.UI
{
    public class UIVScroll : UIVScroll<UIVScroll>
    {
        private UIVScroll(string name, Class classes, UIElementBase[] subs, params Event<UIVScroll>[] events) : base(classes.Styles, events)
        {
            Name = name;
            Tag = UIElementTag.UICollection;
            if (subs != null && subs.Length > 0)
                AddElements(subs);
        }


        // ----- ORIGINAL 11 PUBLIC CONSTRUCTORS (unchanged signatures) -----

        public UIVScroll(params UIStyleData[] classes) : this("UICollection", new Class(classes), [], []) { }
        public UIVScroll(string name, params UIStyleData[] classes) : this(name, new Class(classes), [], []) { }
        
        public UIVScroll(Class classes) : this("UICollection", classes, [], [] ) { }
        public UIVScroll(Class classes, Event<UIVScroll> e1) : this("UICollection", classes, [], e1) { }
        public UIVScroll(Class classes, Event<UIVScroll> e1, Event<UIVScroll> e2) : this("UICollection", classes, [], e1, e2) { }
        public UIVScroll(Class classes, Event<UIVScroll> e1, Event<UIVScroll> e2, Event<UIVScroll> e3) : this("UICollection", classes, [], e1, e2, e3) { }
        public UIVScroll(Class classes, Event<UIVScroll> e1, Event<UIVScroll> e2, Event<UIVScroll> e3, Event<UIVScroll> e4) : this("UICollection", classes, [], e1, e2, e3, e4) { }
        
        public UIVScroll(string name, Class classes) : this(name, classes, [], [] ) { }
        public UIVScroll(string name, Class classes, Event<UIVScroll> e1) : this(name, classes, [], e1) { }
        public UIVScroll(string name, Class classes, Event<UIVScroll> e1, Event<UIVScroll> e2) : this(name, classes, [], e1, e2) { }
        public UIVScroll(string name, Class classes, Event<UIVScroll> e1, Event<UIVScroll> e2, Event<UIVScroll> e3) : this(name, classes, [], e1, e2, e3) { }
        public UIVScroll(string name, Class classes, Event<UIVScroll> e1, Event<UIVScroll> e2, Event<UIVScroll> e3, Event<UIVScroll> e4) : this(name, classes, [], e1, e2, e3, e4) { }
        
        public UIVScroll(Class classes, UIElementBase[] subs) : this("UICollection", classes, subs, []) { }
        public UIVScroll(Class classes, Event<UIVScroll> e1, UIElementBase[] subs) : this("UICollection", classes, subs, e1) { }
        public UIVScroll(Class classes, Event<UIVScroll> e1, Event<UIVScroll> e2, UIElementBase[] subs) : this("UICollection", classes, subs, e1, e2) { }
        public UIVScroll(Class classes, Event<UIVScroll> e1, Event<UIVScroll> e2, Event<UIVScroll> e3, UIElementBase[] subs) : this("UICollection", classes, subs, e1, e2, e3) { }
        public UIVScroll(Class classes, Event<UIVScroll> e1, Event<UIVScroll> e2, Event<UIVScroll> e3, Event<UIVScroll> e4, UIElementBase[] subs) : this("UICollection", classes, subs, e1, e2, e3, e4) { }

        public UIVScroll(string name, Class classes, UIElementBase[] subs) : this(name, classes, subs, []) { }
        public UIVScroll(string name, Class classes, Event<UIVScroll> e1, UIElementBase[] subs) : this(name, classes, subs, e1) { }
        public UIVScroll(string name, Class classes, Event<UIVScroll> e1, Event<UIVScroll> e2, UIElementBase[] subs) : this(name, classes, subs, e1, e2) { }
        public UIVScroll(string name, Class classes, Event<UIVScroll> e1, Event<UIVScroll> e2, Event<UIVScroll> e3, UIElementBase[] subs) : this(name, classes, subs, e1, e2, e3) { }
        public UIVScroll(string name, Class classes, Event<UIVScroll> e1, Event<UIVScroll> e2, Event<UIVScroll> e3, Event<UIVScroll> e4, UIElementBase[] subs) : this(name, classes, subs, e1, e2, e3, e4) { }

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
        public float ScrollPosition = 0;

        public UIVScroll(UIStyleData[] classes, IEvent[] events) : base(classes, events) { Tag = UIElementTag.UIVerticalScrollView; }

        public override void CollectionFirstPass()
        {
            float offsetX(UIElementBase child) => child.IsLeftAligned() ? Border.X : (child.IsRightAligned() ? Border.Z : 0);

            float maxWidth = 0;
            float totalHeight = Border.Y - ScrollPosition;
            
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
            scrollView.ForeachChildren(child =>
            {
                child.CollectionOffset.Y -= delta;
            });

            scrollView.SecondPass();
            scrollView.UpdateTransform();
            scrollView.ScrollAction?.Invoke(scrollView);
        }
    }
}