namespace PBG.UI;

public static class UIEventExtensions
{
    public static T OnHoverEnter<T>(T self, Action<T>? action) where T : UIElementBase 
    { 
        self.SetOnHoverEnter(action == null ? null : e => action.Invoke((T)e)); 
        return self;
    }

    public static T OnHover<T>(T self, Action<T>? action) where T : UIElementBase
    { 
        self.SetOnHover(action == null ? null : e => action.Invoke((T)e)); 
        return self;
    }

    public static T OnClick<T>(T self, Action<T>? action) where T : UIElementBase
    { 
        self.SetOnClick(action == null ? null : e => action.Invoke((T)e)); 
        return self;
    }

    public static T OnHold<T>(T self, Action<T>? action) where T : UIElementBase
    { 
        self.SetOnHold(action == null ? null : e => action.Invoke((T)e)); 
        return self;
    }

    public static T OnRelease<T>(T self, Action<T>? action) where T : UIElementBase
    { 
        self.SetOnRelease(action == null ? null : e => action.Invoke((T)e)); 
        return self;
    }

    public static T OnHoverExit<T>(T self, Action<T>? action) where T : UIElementBase
    { 
        self.SetOnHoverExit(action == null ? null : e => action.Invoke((T)e)); 
        return self;
    }

    public static T OnTextChange<T>(T self, Action<T>? action) where T : UIField
    {
        self.SetOnTextChange(action == null ? null : e => action.Invoke((T)e)); 
        return self;
    }

    public static T OnTextEnter<T>(T self, Action<T>? action) where T : UIField
    { 
        self.SetOnTextEnter(action == null ? null : e => action.Invoke((T)e)); 
        return self;
    }
}