namespace PBG.UI.Creator
{
    public abstract partial class UIScript
    {
        public static UIElementBase newImg(Class classes, ref UIImg target)
        {
            target = new(classes);
            return target;
        }
        public static UIElementBase newImg(Class classes, Event<UIImg> event1, ref UIImg target)
        {
            target = new(classes, event1);
            return target;
        }
        public static UIElementBase newImg(Class classes, Event<UIImg> event1, Event<UIImg> event2, ref UIImg target)
        {
            target = new(classes, event1, event2);
            return target;
        }
        public static UIElementBase newImg(Class classes, Event<UIImg> event1, Event<UIImg> event2, Event<UIImg> event3, ref UIImg target)
        {
            target = new(classes, event1, event2, event3);
            return target;
        }
        public static UIElementBase newImg(Class classes, Event<UIImg> event1, Event<UIImg> event2, Event<UIImg> event3, Event<UIImg> event4, ref UIImg target)
        {
            target = new(classes, event1, event2, event3, event4);
            return target;
        }
        public static UIElementBase newImg(string name, Class classes, Event<UIImg> event1, ref UIImg target)
        {
            target = new(name, classes, event1);
            return target;
        }
        public static UIElementBase newImg(string name, Class classes, Event<UIImg> event1, Event<UIImg> event2, ref UIImg target)
        {
            target = new(name, classes, event1, event2);
            return target;
        }
        public static UIElementBase newImg(string name, Class classes, Event<UIImg> event1, Event<UIImg> event2, Event<UIImg> event3, ref UIImg target)
        {
            target = new(name, classes, event1, event2, event3);
            return target;
        }
        public static UIElementBase newImg(string name, Class classes, Event<UIImg> event1, Event<UIImg> event2, Event<UIImg> event3, Event<UIImg> event4, ref UIImg target)
        {
            target = new(name, classes, event1, event2, event3, event4);
            return target;
        }
    }
}