using PBG.Data;
using PBG.MathLibrary;
using PBG.UI.Creator;


namespace PBG.UI
{
    public class UIHScroll : UIHScroll<UIHScroll>
    {
        private UIHScroll(string name, Class classes, UIElementBase[] subs, params Event<UIHScroll>[] events) : base(classes.Styles, events)
        {
            Name = name;
            Tag = UIElementTag.UICollection;
            if (subs != null && subs.Length > 0)
                AddElements(subs);
        }


        // ----- ORIGINAL 11 PUBLIC CONSTRUCTORS (unchanged signatures) -----

        public UIHScroll(params UIStyleData[] classes) : this("UICollection", new Class(classes), [], []) { }
        public UIHScroll(string name, params UIStyleData[] classes) : this(name, new Class(classes), [], []) { }
        
        public UIHScroll(Class classes) : this("UICollection", classes, [], [] ) { }
        public UIHScroll(Class classes, Event<UIHScroll> e1) : this("UICollection", classes, [], e1) { }
        public UIHScroll(Class classes, Event<UIHScroll> e1, Event<UIHScroll> e2) : this("UICollection", classes, [], e1, e2) { }
        public UIHScroll(Class classes, Event<UIHScroll> e1, Event<UIHScroll> e2, Event<UIHScroll> e3) : this("UICollection", classes, [], e1, e2, e3) { }
        public UIHScroll(Class classes, Event<UIHScroll> e1, Event<UIHScroll> e2, Event<UIHScroll> e3, Event<UIHScroll> e4) : this("UICollection", classes, [], e1, e2, e3, e4) { }
        
        public UIHScroll(string name, Class classes) : this(name, classes, [], [] ) { }
        public UIHScroll(string name, Class classes, Event<UIHScroll> e1) : this(name, classes, [], e1) { }
        public UIHScroll(string name, Class classes, Event<UIHScroll> e1, Event<UIHScroll> e2) : this(name, classes, [], e1, e2) { }
        public UIHScroll(string name, Class classes, Event<UIHScroll> e1, Event<UIHScroll> e2, Event<UIHScroll> e3) : this(name, classes, [], e1, e2, e3) { }
        public UIHScroll(string name, Class classes, Event<UIHScroll> e1, Event<UIHScroll> e2, Event<UIHScroll> e3, Event<UIHScroll> e4) : this(name, classes, [], e1, e2, e3, e4) { }
        
        public UIHScroll(Class classes, UIElementBase[] subs) : this("UICollection", classes, subs, []) { }
        public UIHScroll(Class classes, Event<UIHScroll> e1, UIElementBase[] subs) : this("UICollection", classes, subs, e1) { }
        public UIHScroll(Class classes, Event<UIHScroll> e1, Event<UIHScroll> e2, UIElementBase[] subs) : this("UICollection", classes, subs, e1, e2) { }
        public UIHScroll(Class classes, Event<UIHScroll> e1, Event<UIHScroll> e2, Event<UIHScroll> e3, UIElementBase[] subs) : this("UICollection", classes, subs, e1, e2, e3) { }
        public UIHScroll(Class classes, Event<UIHScroll> e1, Event<UIHScroll> e2, Event<UIHScroll> e3, Event<UIHScroll> e4, UIElementBase[] subs) : this("UICollection", classes, subs, e1, e2, e3, e4) { }

        public UIHScroll(string name, Class classes, UIElementBase[] subs) : this(name, classes, subs, []) { }
        public UIHScroll(string name, Class classes, Event<UIHScroll> e1, UIElementBase[] subs) : this(name, classes, subs, e1) { }
        public UIHScroll(string name, Class classes, Event<UIHScroll> e1, Event<UIHScroll> e2, UIElementBase[] subs) : this(name, classes, subs, e1, e2) { }
        public UIHScroll(string name, Class classes, Event<UIHScroll> e1, Event<UIHScroll> e2, Event<UIHScroll> e3, UIElementBase[] subs) : this(name, classes, subs, e1, e2, e3) { }
        public UIHScroll(string name, Class classes, Event<UIHScroll> e1, Event<UIHScroll> e2, Event<UIHScroll> e3, Event<UIHScroll> e4, UIElementBase[] subs) : this(name, classes, subs, e1, e2, e3, e4) { }

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
        public UIHScroll(UIStyleData[] classes, IEvent[] events) : base(classes, events) { Tag = UIElementTag.UIHorizontalScrollView; }
        
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