using PBG.UI.Creator;


namespace PBG.UI
{
    public class UIImg : UIImg<UIImg>
    {
        public UIImg() : base() { Name = "UIImg"; }
        
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

        public UIImg Class(params IStyleData[] styles) => InternalClass(this, styles);

        public UIImg OnHoverEnter(Action<UIImg>? action)    { SetOnHoverEnter(action); return this; }
        public UIImg OnHover(Action<UIImg>? action)         { SetOnHover(action); return this; }
        public UIImg OnClick(Action<UIImg>? action)         { SetOnClick(action); return this; }
        public UIImg OnHold(Action<UIImg>? action)          { SetOnHold(action); return this; }
        public UIImg OnRelease(Action<UIImg>? action)       { SetOnRelease(action); return this; }
        public UIImg OnHoverExit(Action<UIImg>? action)     { SetOnHoverExit(action); return this; }
    }
    public class UIImg<TSelf> : UIPanel<TSelf> where TSelf : UIImg<TSelf>
    {
        public UIImg() : base() { Tag = UIElementTag.UIImage; }
    }
}