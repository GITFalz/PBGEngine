using System.Diagnostics.CodeAnalysis;
using PBG.MathLibrary;
using PBG.Rendering.Meshes;
using PBG.UI.Creator;


namespace PBG.UI
{
    public interface IUIText
    {
        Vector4 Color { get; set; }
        Vector4 Transform { get; }
        string Text { get; }
        int? MaxCharCount { get; set; }
        float FontSize { get; set; }
        bool Visible { get; }
        int MaskIndex { get; set; }
        public TextAlign TextAlign { get; set; }

        Vector2 AnimationTranslation { get; set; }
        float AnimationScale { get; set; }
        float AnimationRotation { get; set; }

        string GetText();
        IUIText SetText(string text);
        UIElementBase UpdateCharacters();
        Vector2 GetCenter();
        TextAlign GetTextAlign();
        TextMesh GetTextMesh();
        UIElementBase GetElement();
    }

    public class UIText : UIText<UIText>
    {
        public UIText() : base("") { Name = "UIText"; }
        public UIText(string text) : base(text) { Name = "UIText"; }
        
        public UIText Ref(ref UIText text)
        {
            text = this;
            return text;
        }

        public UIText Out(out UIText text)
        {
            text = this;
            return text;
        }

        public UIText Class(params IStyleData[] styles) => InternalClass(this, styles);

        public UIText OnHoverEnter(Action<UIText>? action)    { SetOnHoverEnter(action); return this; }
        public UIText OnHover(Action<UIText>? action)         { SetOnHover(action); return this; }
        public UIText OnClick(Action<UIText>? action)         { SetOnClick(action); return this; }
        public UIText OnHold(Action<UIText>? action)          { SetOnHold(action); return this; }
        public UIText OnRelease(Action<UIText>? action)       { SetOnRelease(action); return this; }
        public UIText OnHoverExit(Action<UIText>? action)     { SetOnHoverExit(action); return this; }
    }

    public class UIText<TSelf> : UIElement<TSelf>, IUIText where TSelf : UIText<TSelf>
    {
        public UIText(string text) : base((1, 1, 1, 1)) 
        { 
            Tag = UIElementTag.UIText; 

            MaxCharCount ??= text.Length;
            SetText(text);
        }

        public TextAlign TextAlign { get; set; } = TextAlign.Left;
        public int? MaxCharCount { get; set; } = null;
        public float FontSize { get; set; } = 1f;
        protected string _text = "";
        public string Text { get; protected set; } = "                    ";

        protected bool _checkCursor = true;

        public IUIText SetText(string text)
        {
            Text = ClampText(text, 0, MaxCharCount ?? 20);
            _text = Text;
            Text = FillWithSpaces(Text, MaxCharCount ?? 20);
            Width = UISize.Pixels((int)(7 * (MaxCharCount ?? 20) * FontSize));
            Height = UISize.Pixels((int)(9 * FontSize));
            if (_checkCursor) CheckCursor();
            return this;
        }

        public void UpdateText(string text)
        {
            SetText(text);
            UpdateCharacters();
        }

        protected virtual UIElementBase CheckCursor() => this;

        public Vector2 GetCenter() => Center;
        public TextAlign GetTextAlign() => TextAlign;
        public TextMesh GetTextMesh() => UIController!.TextMesh;
        public UIElementBase GetElement() => this;

        public string GetText() => _text;
        public float GetFloat(float replacement = 0) => Parse.Float.Parse(_text, replacement);
        public int GetInt(int replacement = 0) => Parse.Int.Parse(_text, replacement);
        public byte GetByte(byte replacement = 0) => (byte)Parse.Int.Parse(_text, replacement);

        public void SetTextCharCount(string text)
        {
            MaxCharCount = text.Length;
            SetText(text);
        }

        public string GetTrimmedText() => Text.Trim();
        public float CharHeight => 9 * FontSize;
        public float TextWidth => 7 * FontSize * (MaxCharCount ?? 20);

        public override void FirstPass()
        {
            SetText(_text);
            base.FirstPass();
        }

        public override void SecondPass()
        {
            base.SecondPass();
        }

        public override void Generate()
        {
            if (ParentElement != null && !ParentElement.Visible)
                Visible = false;

            ControllerCheck().TextMesh.AddElement(this);
        }
        public override bool GetMaskPanel([NotNullWhen(true)] out PBG.Rendering.Mask.UIMaskStruct? mask) => ControllerCheck().MaskData.GetMask(MaskIndex, out mask);

        public override void UpdateChildMaskIndex(int index) => UIController?.TextMesh.UpdateMaskIndex(this, index);
        public override UIElementBase UpdateTransform() { UIController?.TextMesh.UpdateTransform(this); return this; }
        public override UIElementBase UpdateScale() {  return this; }
        public override UIElementBase UpdateColor() { UIController?.TextMesh.UpdateColor(this); return this; }
        public override UIElementBase UpdateBorderUI() { return this; }
        public override UIElementBase UpdateBorderColor() { return this; }
        public override UIElementBase UpdateBorderColor(Vector4 color) { return this; }
        public override UIElementBase UpdateAnimationTranslation() { UIController?.TextMesh.UpdateAnimationTranslation(this); return this; }
        public override UIElementBase UpdateAnimationScale() { UIController?.TextMesh.UpdateAnimationScale(this); return this; }
        public override UIElementBase UpdateAnimationRotation() { UIController?.TextMesh.UpdateAnimationRotation(this); return this; }
        public override void Destroy() => ControllerCheck().TextMesh.RemoveElement(this);
        public override UIElementBase SetVisible(bool visible)
        {
            if (Visible != visible)
            {
                base.SetVisible(visible);
                UIController?.TextMesh.QueueUpdateVisibility();
            }
            return this;
        }

        public override UIElementBase UpdateCharacters()
        {
            UIController?.TextMesh.UpdateCharacters(this);
            return this;
        }

        public static string ClampText(string text, int min, int max)
        {
            if (text.Length < min)
            {
                return text.PadRight(min, ' ');
            }
            else if (text.Length > max)
            {
                return text[..max];
            }
            return text;
        }

        public static string FillWithSpaces(string text, int fullSize) => text.Length >= fullSize ? text : text.PadRight(fullSize);
    }
}

public enum TextAlign
{
    Left = 0,
    Center = 1,
    Right = 2,
}