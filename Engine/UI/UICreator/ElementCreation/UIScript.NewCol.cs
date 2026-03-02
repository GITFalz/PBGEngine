namespace PBG.UI.Creator
{
    public abstract partial class UIScript
    {
        public static UIElementBase newCol(Class classes, UIElementBase[] subElements, ref UICol target)
        {
            target = new(classes, subElements);
            return target;
        }
        public static UIElementBase newCol(Class classes, Event<UICol> event1, UIElementBase[] subElements, ref UICol target)
        {
            target = new(classes, event1, subElements);
            return target;
        }
        public static UIElementBase newCol(Class classes, Event<UICol> event1, Event<UICol> event2, UIElementBase[] subElements, ref UICol target)
        {
            target = new(classes, event1, event2, subElements);
            return target;
        }
        public static UIElementBase newCol(Class classes, Event<UICol> event1, Event<UICol> event2, Event<UICol> event3, UIElementBase[] subElements, ref UICol target)
        {
            target = new(classes, event1, event2, event3, subElements);
            return target;
        }
        public static UIElementBase newCol(Class classes, Event<UICol> event1, Event<UICol> event2, Event<UICol> event3, Event<UICol> event4, UIElementBase[] subElements, ref UICol target)
        {
            target = new(classes, event1, event2, event3, event4, subElements);
            return target;
        }
        public static UIElementBase newCol(string name, Class classes, UIElementBase[] subElements, ref UICol target)
        {
            target = new(name, classes, subElements);
            return target;
        }
        public static UIElementBase newCol(string name, Class classes, Event<UICol> event1, UIElementBase[] subElements, ref UICol target)
        {
            target = new(name, classes, event1, subElements);
            return target;
        }
        public static UIElementBase newCol(string name, Class classes, Event<UICol> event1, Event<UICol> event2, UIElementBase[] subElements, ref UICol target)
        {
            target = new(name, classes, event1, event2, subElements);
            return target;
        }
        public static UIElementBase newCol(string name, Class classes, Event<UICol> event1, Event<UICol> event2, Event<UICol> event3, UIElementBase[] subElements, ref UICol target)
        {
            target = new(name, classes, event1, event2, event3, subElements);
            return target;
        }
        public static UIElementBase newCol(string name, Class classes, Event<UICol> event1, Event<UICol> event2, Event<UICol> event3, Event<UICol> event4, UIElementBase[] subElements, ref UICol target)
        {
            target = new(name, classes, event1, event2, event3, event4, subElements);
            return target;
        }
    }
}