namespace PBG.UI.Creator
{
    public abstract partial class UIScript
    {
        public static UIElementBase newVScroll(Class classes, UIElementBase[] subElements, ref UIVScroll target)
        {
            target = new(classes, subElements);
            return target;
        }
        public static UIElementBase newVScroll(Class classes, Event<UIVScroll> event1, UIElementBase[] subElements, ref UIVScroll target)
        {
            target = new(classes, event1, subElements);
            return target;
        }
        public static UIElementBase newVScroll(Class classes, Event<UIVScroll> event1, Event<UIVScroll> event2, UIElementBase[] subElements, ref UIVScroll target)
        {
            target = new(classes, event1, event2, subElements);
            return target;
        }
        public static UIElementBase newVScroll(Class classes, Event<UIVScroll> event1, Event<UIVScroll> event2, Event<UIVScroll> event3, UIElementBase[] subElements, ref UIVScroll target)
        {
            target = new(classes, event1, event2, event3, subElements);
            return target;
        }
        public static UIElementBase newVScroll(Class classes, Event<UIVScroll> event1, Event<UIVScroll> event2, Event<UIVScroll> event3, Event<UIVScroll> event4, UIElementBase[] subElements, ref UIVScroll target)
        {
            target = new(classes, event1, event2, event3, event4, subElements);
            return target;
        }
        public static UIElementBase newVScroll(string name, Class classes, UIElementBase[] subElements, ref UIVScroll target)
        {
            target = new(name, classes, subElements);
            return target;
        }
        public static UIElementBase newVScroll(string name, Class classes, Event<UIVScroll> event1, UIElementBase[] subElements, ref UIVScroll target)
        {
            target = new(name, classes, event1, subElements);
            return target;
        }
        public static UIElementBase newVScroll(string name, Class classes, Event<UIVScroll> event1, Event<UIVScroll> event2, UIElementBase[] subElements, ref UIVScroll target)
        {
            target = new(name, classes, event1, event2, subElements);
            return target;
        }
        public static UIElementBase newVScroll(string name, Class classes, Event<UIVScroll> event1, Event<UIVScroll> event2, Event<UIVScroll> event3, UIElementBase[] subElements, ref UIVScroll target)
        {
            target = new(name, classes, event1, event2, event3, subElements);
            return target;
        }
        public static UIElementBase newVScroll(string name, Class classes, Event<UIVScroll> event1, Event<UIVScroll> event2, Event<UIVScroll> event3, Event<UIVScroll> event4, UIElementBase[] subElements, ref UIVScroll target)
        {
            target = new(name, classes, event1, event2, event3, event4, subElements);
            return target;
        }
    }
}