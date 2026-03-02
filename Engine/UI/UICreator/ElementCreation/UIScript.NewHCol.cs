namespace PBG.UI.Creator
{
    public abstract partial class UIScript
    {
        public static UIElementBase newHCol(Class classes, UIElementBase[] subElements, ref UIHCol target)
        {
            target = new(classes, subElements);
            return target;
        }
        public static UIElementBase newHCol(Class classes, Event<UIHCol> event1, UIElementBase[] subElements, ref UIHCol target)
        {
            target = new(classes, event1, subElements);
            return target;
        }
        public static UIElementBase newHCol(Class classes, Event<UIHCol> event1, Event<UIHCol> event2, UIElementBase[] subElements, ref UIHCol target)
        {
            target = new(classes, event1, event2, subElements);
            return target;
        }
        public static UIElementBase newHCol(Class classes, Event<UIHCol> event1, Event<UIHCol> event2, Event<UIHCol> event3, UIElementBase[] subElements, ref UIHCol target)
        {
            target = new(classes, event1, event2, event3, subElements);
            return target;
        }
        public static UIElementBase newHCol(Class classes, Event<UIHCol> event1, Event<UIHCol> event2, Event<UIHCol> event3, Event<UIHCol> event4, UIElementBase[] subElements, ref UIHCol target)
        {
            target = new(classes, event1, event2, event3, event4, subElements);
            return target;
        }
        public static UIElementBase newHCol(string name, Class classes, Event<UIHCol> event1, UIElementBase[] subElements, ref UIHCol target)
        {
            target = new(name, classes, event1, subElements);
            return target;
        }
        public static UIElementBase newHCol(string name, Class classes, Event<UIHCol> event1, Event<UIHCol> event2, UIElementBase[] subElements, ref UIHCol target)
        {
            target = new(name, classes, event1, event2, subElements);
            return target;
        }
        public static UIElementBase newHCol(string name, Class classes, Event<UIHCol> event1, Event<UIHCol> event2, Event<UIHCol> event3, UIElementBase[] subElements, ref UIHCol target)
        {
            target = new(name, classes, event1, event2, event3, subElements);
            return target;
        }
        public static UIElementBase newHCol(string name, Class classes, Event<UIHCol> event1, Event<UIHCol> event2, Event<UIHCol> event3, Event<UIHCol> event4, UIElementBase[] subElements, ref UIHCol target)
        {
            target = new(name, classes, event1, event2, event3, event4, subElements);
            return target;
        }
    }
}