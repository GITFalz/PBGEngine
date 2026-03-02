namespace PBG.UI.Creator
{
    public abstract partial class UIScript
    {
        public static UIElementBase newHScroll(Class classes, UIElementBase[] subElements, ref UIHScroll target)
        {
            target = new(classes, subElements);
            return target;
        }
        public static UIElementBase newHScroll(Class classes, Event<UIHScroll> event1, UIElementBase[] subElements, ref UIHScroll target)
        {
            target = new(classes, event1, subElements);
            return target;
        }
        public static UIElementBase newHScroll(Class classes, Event<UIHScroll> event1, Event<UIHScroll> event2, UIElementBase[] subElements, ref UIHScroll target)
        {
            target = new(classes, event1, event2, subElements);
            return target;
        }
        public static UIElementBase newHScroll(Class classes, Event<UIHScroll> event1, Event<UIHScroll> event2, Event<UIHScroll> event3, UIElementBase[] subElements, ref UIHScroll target)
        {
            target = new(classes, event1, event2, event3, subElements);
            return target;
        }
        public static UIElementBase newHScroll(Class classes, Event<UIHScroll> event1, Event<UIHScroll> event2, Event<UIHScroll> event3, Event<UIHScroll> event4, UIElementBase[] subElements, ref UIHScroll target)
        {
            target = new(classes, event1, event2, event3, event4, subElements);
            return target;
        }
        public static UIElementBase newHScroll(string name, Class classes, Event<UIHScroll> event1, UIElementBase[] subElements, ref UIHScroll target)
        {
            target = new(name, classes, event1, subElements);
            return target;
        }
        public static UIElementBase newHScroll(string name, Class classes, Event<UIHScroll> event1, Event<UIHScroll> event2, UIElementBase[] subElements, ref UIHScroll target)
        {
            target = new(name, classes, event1, event2, subElements);
            return target;
        }
        public static UIElementBase newHScroll(string name, Class classes, Event<UIHScroll> event1, Event<UIHScroll> event2, Event<UIHScroll> event3, UIElementBase[] subElements, ref UIHScroll target)
        {
            target = new(name, classes, event1, event2, event3, subElements);
            return target;
        }
        public static UIElementBase newHScroll(string name, Class classes, Event<UIHScroll> event1, Event<UIHScroll> event2, Event<UIHScroll> event3, Event<UIHScroll> event4, UIElementBase[] subElements, ref UIHScroll target)
        {
            target = new(name, classes, event1, event2, event3, event4, subElements);
            return target;
        }
    }
}