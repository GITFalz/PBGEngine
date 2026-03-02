using PBG.UI.Creator;


namespace PBG.UI
{
    public class UIImg : UIImg<UIImg>
    {
        private UIImg(string name, Class classes, params IEvent[] events) : base(classes.Styles, events)
        {
            Name = name;
            Tag = UIElementTag.UIImage;
        }

        // ORIGINAL PUBLIC CONSTRUCTORS
        public UIImg(params UIStyleData[] classes) : this("UIImg", new Class(classes), []) { }
        public UIImg(Class classes) : this("UIImg", classes, []) { }
        public UIImg(string name, params UIStyleData[] classes) : this(name, new Class(classes), []) { }
        public UIImg(string name, Class classes) : this(name, classes, []) { }
        
        public UIImg(Class classes, Event<UIImg> e1) : this("UIImg", classes, e1) { }
        public UIImg(Class classes, Event<UIImg> e1, Event<UIImg> e2) : this("UIImg", classes, e1, e2) { }
        public UIImg(Class classes, Event<UIImg> e1, Event<UIImg> e2, Event<UIImg> e3) : this("UIImg", classes, e1, e2, e3) { }
        public UIImg(Class classes, Event<UIImg> e1, Event<UIImg> e2, Event<UIImg> e3, Event<UIImg> e4) : this("UIImg", classes, e1, e2, e3, e4) { }
        
        public UIImg(string name, Class classes, Event<UIImg> e1) : this(name, classes, [e1]) { }
        public UIImg(string name, Class classes, Event<UIImg> e1, Event<UIImg> e2) : this(name, classes, [e1, e2]) { }
        public UIImg(string name, Class classes, Event<UIImg> e1, Event<UIImg> e2, Event<UIImg> e3) : this(name, classes, [e1, e2, e3]) { }
        public UIImg(string name, Class classes, Event<UIImg> e1, Event<UIImg> e2, Event<UIImg> e3, Event<UIImg> e4) : this(name, classes, [e1, e2, e3, e4]) { }
    }
    public class UIImg<TSelf> : UIPanel<TSelf> where TSelf : UIImg<TSelf>
    {
        public UIImg(UIStyleData[] classes, IEvent[] events) : base(classes, events) { Tag = UIElementTag.UIImage; }
    }
}