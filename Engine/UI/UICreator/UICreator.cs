

namespace PBG.UI.Creator
{
    public record Class(params UIStyleData[] Styles)
    { 
        public Class(Class classes, params UIStyleData[] styles) : this([..classes.Styles, ..styles]) {}
    }
    
    // Non-generic Events class that infers type from context
    public record Events(params IEvent[] Actions);
    
    // Non-generic base interface for events
    public interface IEvent
    {
        void Apply(UIElementBase element);
    }

    // Generic base class for type-safe implementation
    public abstract record Event<TSelf> : IEvent where TSelf : UIElement<TSelf>
    {
        public abstract void Apply(UIElement<TSelf> element);
        
        // Implementation for non-generic interface
        public void Apply(UIElementBase element)
        {
            if (element is UIElement<TSelf> typedElement)
                Apply(typedElement);
        }
    }
    
    // Event implementations remain the same but implement IEvent
    public record OnClickEvent<TSelf>(Action<TSelf>? Action) : Event<TSelf> where TSelf : UIElement<TSelf>
    {
        public override void Apply(UIElement<TSelf> element) => element.SetOnClick(Action);
    }

    public record OnHoverEnterEvent<TSelf>(Action<TSelf>? Action) : Event<TSelf> where TSelf : UIElement<TSelf>
    {
        public override void Apply(UIElement<TSelf> element) => element.SetOnHoverEnter(Action);
    }

    public record OnHoverEvent<TSelf>(Action<TSelf>? Action) : Event<TSelf> where TSelf : UIElement<TSelf>
    {
        public override void Apply(UIElement<TSelf> element) => element.SetOnHover(Action);
    }

    public record OnHoldEvent<TSelf>(Action<TSelf>? Action) : Event<TSelf> where TSelf : UIElement<TSelf>
    {
        public override void Apply(UIElement<TSelf> element) => element.SetOnHold(Action);
    }

    public record OnReleaseEvent<TSelf>(Action<TSelf>? Action) : Event<TSelf> where TSelf : UIElement<TSelf>
    {
        public override void Apply(UIElement<TSelf> element) => element.SetOnRelease(Action);
    }

    public record OnHoverExitEvent<TSelf>(Action<TSelf>? Action) : Event<TSelf> where TSelf : UIElement<TSelf>
    {
        public override void Apply(UIElement<TSelf> element) => element.SetOnHoverExit(Action);
    }

    public record OnTextChangeEvent<TSelf>(Action<UIField>? Action) : Event<TSelf> where TSelf : UIElement<TSelf>
    {
        public override void Apply(UIElement<TSelf> element)
        {
            if (element is UIField field)
                field.SetOnTextChange(Action);
        }
    }
    
    public record OnTextEnterEvent<TSelf>(Action<UIField>? Action) : Event<TSelf> where TSelf : UIElement<TSelf>
    {
        public override void Apply(UIElement<TSelf> element) 
        { 
            if (element is UIField field) 
                field.SetOnTextEnter(Action); 
        }
    }

    public record OnVerticalScrollEvent<T>(Action<UIVScroll>? Action) : Event<T> where T : UIElement<T>
    {
        public override void Apply(UIElement<T> element)
        {
            if (element is UIVScroll scroll) 
                scroll.SetOnScroll(Action); 
        }
    }
    public record OnHorizontalScrollEvent<TSelf>(Action<UIHScroll>? Action) : Event<TSelf> where TSelf : UIElement<TSelf>
    {
        public override void Apply(UIElement<TSelf> element)
        {
            if (element is UIHScroll scroll) 
                scroll.SetOnScroll(Action); 
        }
    }
}