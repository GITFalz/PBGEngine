using PBG.UI.Creator;


namespace PBG.UI
{
    public class UIButton : UIPanel
    {
        public UIButton() : this("UIButton") {}
        public UIButton(string name) : base() 
        { 
            Name = name; 
            Tag = UIElementTag.UIButton;
        }

        public UIButton(params IStyleData[] styles) : this()
        { 
            Class(styles);
        }

        public UIButton(string name, params IStyleData[] styles) : this(name)
        { 
            Class(styles);
        }
        
        public UIButton Ref(ref UIButton text)
        {
            text = this;
            return text;
        }

        public UIButton Out(out UIButton text)
        {
            text = this;
            return text;
        }

        public UIButton Class(params IStyleData[] styles) => Style(this, styles);

        public UIButton OnHoverEnter(Action<UIButton>? action)    { UIEventExtensions.OnHoverEnter(this, action); return this; }
        public UIButton OnHover(Action<UIButton>? action)         { UIEventExtensions.OnHover(this, action); return this; }
        public UIButton OnClick(Action<UIButton>? action)         { UIEventExtensions.OnClick(this, action); return this; }
        public UIButton OnHold(Action<UIButton>? action)          { UIEventExtensions.OnHold(this, action); return this; }
        public UIButton OnRelease(Action<UIButton>? action)       { UIEventExtensions.OnRelease(this, action); return this; }
        public UIButton OnHoverExit(Action<UIButton>? action)     { UIEventExtensions.OnHoverExit(this, action); return this; }
    }
}