namespace PBG.UI.Creator
{
    public abstract partial class UIScript
    {
        public static UIElementBase newText(string text, Class classes, ref UIText target)
        {
            target = new(text, classes);
            return target;
        }
        public static UIElementBase newText(string text, Class classes, Event<UIText> event1, ref UIText target)
        {
            target = new(text, classes, event1);
            return target;
        }
        public static UIElementBase newText(string text, Class classes, Event<UIText> event1, Event<UIText> event2, ref UIText target)
        {
            target = new(text, classes, event1, event2);
            return target;
        }
        public static UIElementBase newText(string text, Class classes, Event<UIText> event1, Event<UIText> event2, Event<UIText> event3, ref UIText target)
        {
            target = new(text, classes, event1, event2, event3);
            return target;
        }
        public static UIElementBase newText(string text, Class classes, Event<UIText> event1, Event<UIText> event2, Event<UIText> event3, Event<UIText> event4, ref UIText target)
        {
            target = new(text, classes, event1, event2, event3, event4);
            return target;
        }
        public static UIElementBase newText(string text, string name, Class classes, Event<UIText> event1, ref UIText target)
        {
            target = new(text, name, classes, event1);
            return target;
        }
        public static UIElementBase newText(string text, string name, Class classes, Event<UIText> event1, Event<UIText> event2, ref UIText target)
        {
            target = new(text, name, classes, event1, event2);
            return target;
        }
        public static UIElementBase newText(string text, string name, Class classes, Event<UIText> event1, Event<UIText> event2, Event<UIText> event3, ref UIText target)
        {
            target = new(text, name, classes, event1, event2, event3);
            return target;
        }
        public static UIElementBase newText(string text, string name, Class classes, Event<UIText> event1, Event<UIText> event2, Event<UIText> event3, Event<UIText> event4, ref UIText target)
        {
            target = new(text, name, classes, event1, event2, event3, event4);
            return target;
        }
    }
}