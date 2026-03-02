namespace PBG.UI.Creator
{
    public abstract partial class UIScript
    {
        public static UIElementBase newField(string text, Class classes, ref UIField target)
        {
            target = new(text, classes);
            return target;
        }
        public static UIElementBase newField(string text, Class classes, Event<UIField> event1, ref UIField target)
        {
            target = new(text, classes, event1);
            return target;
        }
        public static UIElementBase newField(string text, Class classes, Event<UIField> event1, Event<UIField> event2, ref UIField target)
        {
            target = new(text, classes, event1, event2);
            return target;
        }
        public static UIElementBase newField(string text, Class classes, Event<UIField> event1, Event<UIField> event2, Event<UIField> event3, ref UIField target)
        {
            target = new(text, classes, event1, event2, event3);
            return target;
        }
        public static UIElementBase newField(string text, Class classes, Event<UIField> event1, Event<UIField> event2, Event<UIField> event3, Event<UIField> event4, ref UIField target)
        {
            target = new(text, classes, event1, event2, event3, event4);
            return target;
        }
        public static UIElementBase newField(string text, string name, Class classes, Event<UIField> event1, ref UIField target)
        {
            target = new(text, name, classes, event1);
            return target;
        }
        public static UIElementBase newField(string text, string name, Class classes, Event<UIField> event1, Event<UIField> event2, ref UIField target)
        {
            target = new(text, name, classes, event1, event2);
            return target;
        }
        public static UIElementBase newField(string text, string name, Class classes, Event<UIField> event1, Event<UIField> event2, Event<UIField> event3, ref UIField target)
        {
            target = new(text, name, classes, event1, event2, event3);
            return target;
        }
        public static UIElementBase newField(string text, string name, Class classes, Event<UIField> event1, Event<UIField> event2, Event<UIField> event3, Event<UIField> event4, ref UIField target)
        {
            target = new(text, name, classes, event1, event2, event3, event4);
            return target;
        }
    }
}