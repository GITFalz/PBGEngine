namespace PBG.UI.Creator
{
    public abstract partial class UIScript
    {
        public static UIElementBase newButton(Class classes, ref UIButton target)
        {
            target = new(classes);
            return target;
        }
        public static UIElementBase newButton(Class classes, Event<UIButton> event1, ref UIButton target)
        {
            target = new(classes, event1);
            return target;
        }
        public static UIElementBase newButton(Class classes, Event<UIButton> event1, Event<UIButton> event2, ref UIButton target)
        {
            target = new(classes, event1, event2);
            return target;
        }
        public static UIElementBase newButton(Class classes, Event<UIButton> event1, Event<UIButton> event2, Event<UIButton> event3, ref UIButton target)
        {
            target = new(classes, event1, event2, event3);
            return target;
        }
        public static UIElementBase newButton(Class classes, Event<UIButton> event1, Event<UIButton> event2, Event<UIButton> event3, Event<UIButton> event4, ref UIButton target)
        {
            target = new(classes, event1, event2, event3, event4);
            return target;
        }
        public static UIElementBase newButton(string name, Class classes, Event<UIButton> event1, ref UIButton target)
        {
            target = new(name, classes, event1);
            return target;
        }
        public static UIElementBase newButton(string name, Class classes, Event<UIButton> event1, Event<UIButton> event2, ref UIButton target)
        {
            target = new(name, classes, event1, event2);
            return target;
        }
        public static UIElementBase newButton(string name, Class classes, Event<UIButton> event1, Event<UIButton> event2, Event<UIButton> event3, ref UIButton target)
        {
            target = new(name, classes, event1, event2, event3);
            return target;
        }
        public static UIElementBase newButton(string name, Class classes, Event<UIButton> event1, Event<UIButton> event2, Event<UIButton> event3, Event<UIButton> event4, ref UIButton target)
        {
            target = new(name, classes, event1, event2, event3, event4);
            return target;
        }
    }
}