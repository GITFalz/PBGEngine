using PBG.UI.Creator;


namespace PBG.UI
{
    public class UIImg : UIPanel
    {
        public UIImg() : base() 
        { 
            Name = "UIImg"; 
            Tag = UIElementTag.UIImage;
        }

        public UIImg(params IStyleData[] styles) : this()
        { 
            Styles.bg_white.Set(this);
            Class(styles);
        }
        
        public UIImg Ref(ref UIImg text)
        {
            text = this;
            return text;
        }

        public UIImg Out(out UIImg text)
        {
            text = this;
            return text;
        }

        public new UIImg Class(params IStyleData[] styles) => InternalClass(this, styles);

        public UIImg OnHoverEnter(Action<UIImg>? action)    { UIEventExtensions.OnHoverEnter(this, action); return this; }
        public UIImg OnHover(Action<UIImg>? action)         { UIEventExtensions.OnHover(this, action); return this; }
        public UIImg OnClick(Action<UIImg>? action)         { UIEventExtensions.OnClick(this, action); return this; }
        public UIImg OnHold(Action<UIImg>? action)          { UIEventExtensions.OnHold(this, action); return this; }
        public UIImg OnRelease(Action<UIImg>? action)       { UIEventExtensions.OnRelease(this, action); return this; }
        public UIImg OnHoverExit(Action<UIImg>? action)     { UIEventExtensions.OnHoverExit(this, action); return this; }
    }
}