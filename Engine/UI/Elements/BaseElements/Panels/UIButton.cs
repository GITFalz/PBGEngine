using PBG.UI.Creator;


namespace PBG.UI
{
    public class UIButton : UIButton<UIButton>
    {
        private UIButton(string name, Class classes, params IEvent[] events) : base(classes.Styles, events)
        {
            Name = name;
            Tag = UIElementTag.UIImage;
        }

        // ORIGINAL PUBLIC CONSTRUCTORS
        public UIButton(params UIStyleData[] classes) : this("UIButton", new Class(classes), []) { }
        public UIButton(Class classes) : this("UIButton", classes, []) { }
        public UIButton(string name, params UIStyleData[] classes) : this(name, new Class(classes), []) { }
        public UIButton(string name, Class classes) : this(name, classes, []) { }
    
        public UIButton(Class classes, Event<UIButton> e1) : this("UIButton", classes, e1) { }
        public UIButton(Class classes, Event<UIButton> e1, Event<UIButton> e2) : this("UIButton", classes, e1, e2) { }
        public UIButton(Class classes, Event<UIButton> e1, Event<UIButton> e2, Event<UIButton> e3) : this("UIButton", classes, e1, e2, e3) { }
        public UIButton(Class classes, Event<UIButton> e1, Event<UIButton> e2, Event<UIButton> e3, Event<UIButton> e4) : this("UIButton", classes, e1, e2, e3, e4) { }
        public UIButton(Class classes, Event<UIButton> e1, Event<UIButton> e2, Event<UIButton> e3, Event<UIButton> e4, Event<UIButton> e5) : this("UIButton", classes, e1, e2, e3, e4, e5) { }
        
        public UIButton(string name, Class classes, Event<UIButton> e1) : this(name, classes, [e1]) { }
        public UIButton(string name, Class classes, Event<UIButton> e1, Event<UIButton> e2) : this(name, classes, [e1, e2]) { }
        public UIButton(string name, Class classes, Event<UIButton> e1, Event<UIButton> e2, Event<UIButton> e3) : this(name, classes, [e1, e2, e3]) { }
        public UIButton(string name, Class classes, Event<UIButton> e1, Event<UIButton> e2, Event<UIButton> e3, Event<UIButton> e4) : this(name, classes, [e1, e2, e3, e4]) { }
        public UIButton(string name, Class classes, Event<UIButton> e1, Event<UIButton> e2, Event<UIButton> e3, Event<UIButton> e4, Event<UIButton> e5) : this(name, classes, [e1, e2, e3, e4, e5]) { }
    }
    public class UIButton<TSelf> : UIPanel<TSelf> where TSelf : UIButton<TSelf>
    {
        public UIButton(UIStyleData[] classes, IEvent[] events) : base(classes, events) { Tag = UIElementTag.UIButton; }
    }
}