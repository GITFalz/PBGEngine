using PBG.MathLibrary;
using PBG.UI.Creator;


namespace PBG.UI
{

    public class UIVCol : UIVCol<UIVCol>
    {
        private UIVCol(string name, Class classes, UIElementBase[] subs, params Event<UIVCol>[] events) : base(classes.Styles, events)
        {
            Name = name;
            Tag = UIElementTag.UICollection;
            if (subs != null && subs.Length > 0)
                AddElements(subs);
        }


        // ----- ORIGINAL 11 PUBLIC CONSTRUCTORS (unchanged signatures) -----

        public UIVCol(params UIStyleData[] classes) : this("UICollection", new Class(classes), [], []) { }
        public UIVCol(string name, params UIStyleData[] classes) : this(name, new Class(classes), [], []) { }
        
        public UIVCol(Class classes) : this("UICollection", classes, [], [] ) { }
        public UIVCol(Class classes, Event<UIVCol> e1) : this("UICollection", classes, [], e1) { }
        public UIVCol(Class classes, Event<UIVCol> e1, Event<UIVCol> e2) : this("UICollection", classes, [], e1, e2) { }
        public UIVCol(Class classes, Event<UIVCol> e1, Event<UIVCol> e2, Event<UIVCol> e3) : this("UICollection", classes, [], e1, e2, e3) { }
        public UIVCol(Class classes, Event<UIVCol> e1, Event<UIVCol> e2, Event<UIVCol> e3, Event<UIVCol> e4) : this("UICollection", classes, [], e1, e2, e3, e4) { }
        
        public UIVCol(string name, Class classes) : this(name, classes, [], [] ) { }
        public UIVCol(string name, Class classes, Event<UIVCol> e1) : this(name, classes, [], e1) { }
        public UIVCol(string name, Class classes, Event<UIVCol> e1, Event<UIVCol> e2) : this(name, classes, [], e1, e2) { }
        public UIVCol(string name, Class classes, Event<UIVCol> e1, Event<UIVCol> e2, Event<UIVCol> e3) : this(name, classes, [], e1, e2, e3) { }
        public UIVCol(string name, Class classes, Event<UIVCol> e1, Event<UIVCol> e2, Event<UIVCol> e3, Event<UIVCol> e4) : this(name, classes, [], e1, e2, e3, e4) { }
        
        public UIVCol(Class classes, UIElementBase[] subs) : this("UICollection", classes, subs, []) { }
        public UIVCol(Class classes, Event<UIVCol> e1, UIElementBase[] subs) : this("UICollection", classes, subs, e1) { }
        public UIVCol(Class classes, Event<UIVCol> e1, Event<UIVCol> e2, UIElementBase[] subs) : this("UICollection", classes, subs, e1, e2) { }
        public UIVCol(Class classes, Event<UIVCol> e1, Event<UIVCol> e2, Event<UIVCol> e3, UIElementBase[] subs) : this("UICollection", classes, subs, e1, e2, e3) { }
        public UIVCol(Class classes, Event<UIVCol> e1, Event<UIVCol> e2, Event<UIVCol> e3, Event<UIVCol> e4, UIElementBase[] subs) : this("UICollection", classes, subs, e1, e2, e3, e4) { }

        public UIVCol(string name, Class classes, UIElementBase[] subs) : this(name, classes, subs, []) { }
        public UIVCol(string name, Class classes, Event<UIVCol> e1, UIElementBase[] subs) : this(name, classes, subs, e1) { }
        public UIVCol(string name, Class classes, Event<UIVCol> e1, Event<UIVCol> e2, UIElementBase[] subs) : this(name, classes, subs, e1, e2) { }
        public UIVCol(string name, Class classes, Event<UIVCol> e1, Event<UIVCol> e2, Event<UIVCol> e3, UIElementBase[] subs) : this(name, classes, subs, e1, e2, e3) { }
        public UIVCol(string name, Class classes, Event<UIVCol> e1, Event<UIVCol> e2, Event<UIVCol> e3, Event<UIVCol> e4, UIElementBase[] subs) : this(name, classes, subs, e1, e2, e3, e4) { }
    }
    
    public class UIVCol<TSelf> : UICol<TSelf> where TSelf : UIVCol<TSelf>
    {
        public UIVCol(UIStyleData[] classes, IEvent[] events) : base(classes, events) { Tag = UIElementTag.UIVerticalCollection; }
        
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