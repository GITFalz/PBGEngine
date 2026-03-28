using PBG.UI.Creator;


namespace PBG.UI
{
    public class UIButton : UIButton<UIButton>
    {
        public UIButton() : base() { Name = "UIButton"; }
        
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

        public UIButton Class(params IStyleData[] styles) => InternalClass(this, styles);

        public UIButton OnHoverEnter(Action<UIButton>? action)    { SetOnHoverEnter(action); return this; }
        public UIButton OnHover(Action<UIButton>? action)         { SetOnHover(action); return this; }
        public UIButton OnClick(Action<UIButton>? action)         { SetOnClick(action); return this; }
        public UIButton OnHold(Action<UIButton>? action)          { SetOnHold(action); return this; }
        public UIButton OnRelease(Action<UIButton>? action)       { SetOnRelease(action); return this; }
        public UIButton OnHoverExit(Action<UIButton>? action)     { SetOnHoverExit(action); return this; }
    }
    public class UIButton<TSelf> : UIPanel<TSelf> where TSelf : UIButton<TSelf>
    {
        public UIButton() : base() { Tag = UIElementTag.UIButton; }
    }
}