namespace PBG.UI.Creator
{
    public abstract partial class UIScript
    {
        public static UIElementBase newVCol(Class classes, UIElementBase[] subElements, ref UIVCol target)
        {
            target = new(classes, subElements);
            return target;
        }
        public static UIElementBase newVCol(Class classes, Event<UIVCol> event1, UIElementBase[] subElements, ref UIVCol target)
        {
            target = new(classes, event1, subElements);
            return target;
        }
        public static UIElementBase newVCol(Class classes, Event<UIVCol> event1, Event<UIVCol> event2, UIElementBase[] subElements, ref UIVCol target)
        {
            target = new(classes, event1, event2, subElements);
            return target;
        }
        public static UIElementBase newVCol(Class classes, Event<UIVCol> event1, Event<UIVCol> event2, Event<UIVCol> event3, UIElementBase[] subElements, ref UIVCol target)
        {
            target = new(classes, event1, event2, event3, subElements);
            return target;
        }
        public static UIElementBase newVCol(Class classes, Event<UIVCol> event1, Event<UIVCol> event2, Event<UIVCol> event3, Event<UIVCol> event4, UIElementBase[] subElements, ref UIVCol target)
        {
            target = new(classes, event1, event2, event3, event4, subElements);
            return target;
        }
        public static UIElementBase newVCol(string name, Class classes, UIElementBase[] subElements, ref UIVCol target)
        {
            target = new(name, classes, subElements);
            return target;
        }
        public static UIElementBase newVCol(string name, Class classes, Event<UIVCol> event1, UIElementBase[] subElements, ref UIVCol target)
        {
            target = new(name, classes, event1, subElements);
            return target;
        }
        public static UIElementBase newVCol(string name, Class classes, Event<UIVCol> event1, Event<UIVCol> event2, UIElementBase[] subElements, ref UIVCol target)
        {
            target = new(name, classes, event1, event2, subElements);
            return target;
        }
        public static UIElementBase newVCol(string name, Class classes, Event<UIVCol> event1, Event<UIVCol> event2, Event<UIVCol> event3, UIElementBase[] subElements, ref UIVCol target)
        {
            target = new(name, classes, event1, event2, event3, subElements);
            return target;
        }
        public static UIElementBase newVCol(string name, Class classes, Event<UIVCol> event1, Event<UIVCol> event2, Event<UIVCol> event3, Event<UIVCol> event4, UIElementBase[] subElements, ref UIVCol target)
        {
            target = new(name, classes, event1, event2, event3, event4, subElements);
            return target;
        }
    }
}