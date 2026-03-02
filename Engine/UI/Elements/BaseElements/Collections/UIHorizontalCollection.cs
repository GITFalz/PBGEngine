using PBG.MathLibrary;
using PBG.UI.Creator;


namespace PBG.UI
{
    public class UIHCol : UIHCol<UIHCol>
    {
        private UIHCol(string name, Class classes, UIElementBase[] subs, params Event<UIHCol>[] events) : base(classes.Styles, events)
        {
            Name = name;
            Tag = UIElementTag.UICollection;
            if (subs != null && subs.Length > 0)
                AddElements(subs);
        }


        // ----- ORIGINAL 11 PUBLIC CONSTRUCTORS (unchanged signatures) -----

        public UIHCol(params UIStyleData[] classes) : this("UICollection", new Class(classes), [], []) { }
        public UIHCol(string name, params UIStyleData[] classes) : this(name, new Class(classes), [], []) { }
        
        public UIHCol(Class classes) : this("UICollection", classes, [], [] ) { }
        public UIHCol(Class classes, Event<UIHCol> e1) : this("UICollection", classes, [], e1) { }
        public UIHCol(Class classes, Event<UIHCol> e1, Event<UIHCol> e2) : this("UICollection", classes, [], e1, e2) { }
        public UIHCol(Class classes, Event<UIHCol> e1, Event<UIHCol> e2, Event<UIHCol> e3) : this("UICollection", classes, [], e1, e2, e3) { }
        public UIHCol(Class classes, Event<UIHCol> e1, Event<UIHCol> e2, Event<UIHCol> e3, Event<UIHCol> e4) : this("UICollection", classes, [], e1, e2, e3, e4) { }
        
        public UIHCol(string name, Class classes) : this(name, classes, [], [] ) { }
        public UIHCol(string name, Class classes, Event<UIHCol> e1) : this(name, classes, [], e1) { }
        public UIHCol(string name, Class classes, Event<UIHCol> e1, Event<UIHCol> e2) : this(name, classes, [], e1, e2) { }
        public UIHCol(string name, Class classes, Event<UIHCol> e1, Event<UIHCol> e2, Event<UIHCol> e3) : this(name, classes, [], e1, e2, e3) { }
        public UIHCol(string name, Class classes, Event<UIHCol> e1, Event<UIHCol> e2, Event<UIHCol> e3, Event<UIHCol> e4) : this(name, classes, [], e1, e2, e3, e4) { }
        
        public UIHCol(Class classes, UIElementBase[] subs) : this("UICollection", classes, subs, []) { }
        public UIHCol(Class classes, Event<UIHCol> e1, UIElementBase[] subs) : this("UICollection", classes, subs, e1) { }
        public UIHCol(Class classes, Event<UIHCol> e1, Event<UIHCol> e2, UIElementBase[] subs) : this("UICollection", classes, subs, e1, e2) { }
        public UIHCol(Class classes, Event<UIHCol> e1, Event<UIHCol> e2, Event<UIHCol> e3, UIElementBase[] subs) : this("UICollection", classes, subs, e1, e2, e3) { }
        public UIHCol(Class classes, Event<UIHCol> e1, Event<UIHCol> e2, Event<UIHCol> e3, Event<UIHCol> e4, UIElementBase[] subs) : this("UICollection", classes, subs, e1, e2, e3, e4) { }

        public UIHCol(string name, Class classes, UIElementBase[] subs) : this(name, classes, subs, []) { }
        public UIHCol(string name, Class classes, Event<UIHCol> e1, UIElementBase[] subs) : this(name, classes, subs, e1) { }
        public UIHCol(string name, Class classes, Event<UIHCol> e1, Event<UIHCol> e2, UIElementBase[] subs) : this(name, classes, subs, e1, e2) { }
        public UIHCol(string name, Class classes, Event<UIHCol> e1, Event<UIHCol> e2, Event<UIHCol> e3, UIElementBase[] subs) : this(name, classes, subs, e1, e2, e3) { }
        public UIHCol(string name, Class classes, Event<UIHCol> e1, Event<UIHCol> e2, Event<UIHCol> e3, Event<UIHCol> e4, UIElementBase[] subs) : this(name, classes, subs, e1, e2, e3, e4) { }
    }
    
    public class UIHCol<TSelf> : UICol<TSelf> where TSelf : UIHCol<TSelf>
    {
        // This function is used when GrowFromChildren is true
        public UIHCol(UIStyleData[] classes, IEvent[] events) : base(classes, events) { Tag = UIElementTag.UIHorizontalCollection; }

        public override void CollectionFirstPass()
        {
            float offsetY(UIElementBase child) => child.IsTopAligned() ? Border.Y : (child.IsBottomAligned() ? Border.W : 0);

            float totalWidth = Border.X;
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

        public float GetTotalXSize()
        {
            float totalOffset = Border.X;
            ForeachChildren(child =>
            {
                if (child.Visible || !IgnoreInvisibleElements)
                {
                    totalOffset += child.Width.Value + Spacing;
                }
            });
            return totalOffset - Spacing + Border.Z;
        }
    }
}