using System.Security.Principal;
using PBG.Data;
using PBG.Graphics;
using PBG.UI.Creator;



namespace PBG.UI
{
    public class UIField : UIText
    {
        private bool _removeTriggered = false;
        public bool IsActive => UIController.ActiveInputField == this;

        public UIField() : this("") {}
        public UIField(string text) : base(text) 
        { 
            Name = "UIField"; 
            Tag = UIElementTag.UIField; 
        }

        public UIField(string text, params IStyleData[] styles) : this(text)
        { 
            Class(styles);
        }
        
        public UIField Ref(ref UIField text)
        {
            text = this;
            return text;
        }

        public UIField Out(out UIField text)
        {
            text = this;
            return text;
        }

        public UIField Class(params IStyleData[] styles) => InternalClass(this, styles);

        public UIField OnHoverEnter(Action<UIField>? action)    { UIEventExtensions.OnHoverEnter(this, action); return this; }
        public UIField OnHover(Action<UIField>? action)         { UIEventExtensions.OnHover(this, action); return this; }
        public UIField OnClick(Action<UIField>? action)         { UIEventExtensions.OnClick(this, action); return this; }
        public UIField OnHold(Action<UIField>? action)          { UIEventExtensions.OnHold(this, action); return this; }
        public UIField OnRelease(Action<UIField>? action)       { UIEventExtensions.OnRelease(this, action); return this; }
        public UIField OnHoverExit(Action<UIField>? action)     { UIEventExtensions.OnHoverExit(this, action); return this; }
        public UIField OnTextChange(Action<UIField>? action)    { UIEventExtensions.OnTextChange(this, action); return this; }
        public UIField OnTextEnter(Action<UIField>? action)     { UIEventExtensions.OnTextEnter(this, action); return this; }

        public override bool Test()
        {
            if (UIController.IsActiveInputfield(this) && Input.IsMousePressed(MouseButton.Left) && !IsMouseOver())
            {
                UIController.RemoveInputfield();
            }
            return base.Test();
        }

        protected override UIElementBase CheckCursor()
        {
            if (UIController.ActiveInputField == this)
            {
                if (UIController.CursorCharacter > _text.Length && !_removeTriggered)
                {
                    UIController.CursorCharacter = _text.Length;
                    UIController?.TextMesh.SetCursor(this);
                }
            }
            return this;
        }

        public UIField SetOnTextChange(Action<UIField>? action)
        {
            UIController?.SetAsInteractable(this, action != null);
            _onTextChange = action;
            return this;
        }

        public UIField SetOnTextEnter(Action<UIField>? action)
        {
            UIController?.SetAsInteractable(this, action != null);
            _onTextEnter = action;
            return this;
        }

        public void SetCursor() => UIController?.TextMesh.SetCursor(this);

        public void AddCharacter(char character)
        {
            if (!TextShaderHelper.CharExists(character) || _text.Length >= MaxCharCount) 
                return;

            int oldCharCount = _text.Length;
            string formatedText = Format(_text.Insert(UIController.CursorCharacter, character.ToString()));   
            _checkCursor = false;
            SetText(formatedText);
            _checkCursor = true;
            if (_text.Length > oldCharCount)
            {
                UIController.CursorCharacter++;
                UIController?.TextMesh.SetCursor(this);
            }
            UpdateCharacters();
            _onTextChange?.Invoke(this);
        }

        public void AddText(string text)
        {
            int oldCharCount = _text.Length;
            int count = 0;
            string newText = _text;
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (!TextShaderHelper.CharExists(c))
                    continue;

                if (_text.Length + count >= MaxCharCount)
                    break;

                newText = newText.Insert(UIController.CursorCharacter + count, c.ToString());
                count++;
            }
            _checkCursor = false;
            SetText(newText);
            _checkCursor = true;
            if (_text.Length > oldCharCount)
            {
                UIController.CursorCharacter += count;
                UIController?.TextMesh.SetCursor(this);
            }
            UpdateCharacters();
            _onTextChange?.Invoke(this);
        }

        public void RemoveCharacter()
        {
            if (_text.Length <= 0 || UIController.CursorCharacter <= 0)
                return;

            int oldCharCount = _text.Length;
            _removeTriggered = true;
            _checkCursor = false;
            SetText(_text.Remove(UIController.CursorCharacter - 1, 1));
            _checkCursor = true;
            _removeTriggered = false;
            if (_text.Length < oldCharCount)
            {
                UIController.CursorCharacter--;   
                UIController?.TextMesh.SetCursor(this);
            }
            UpdateCharacters();
            _onTextChange?.Invoke(this);
        }

        public void RemoveText(int start, int count, bool updateBuffers = true)
        {
            if (_text.Length <= 0)
                return;

            int oldCharCount = _text.Length;
            _removeTriggered = true;
            _checkCursor = false;
            SetText(_text.Remove(start, count));
            _checkCursor = true;
            _removeTriggered = false;
            if (_text.Length < oldCharCount)
            {
                if (UIController.CursorCharacter > start)
                    UIController.CursorCharacter -= oldCharCount - _text.Length;   
                UIController?.TextMesh.SetCursor(this);
            }
            
            if (updateBuffers)
            {
                UpdateCharacters();
                _onTextChange?.Invoke(this);
            }
        }

        public override void OnClickAction()
        {
            UIController.SetInputfield(this);
            base.OnClickAction();
        }

        public override bool IsInteractable() => true;   

        public PBG.UI.TextInputType TextType = PBG.UI.TextInputType.Any;

        public Action<UIField>? _onTextChange = null;
        public Action<UIField>? _onTextEnter = null;

        public void SetTextType(PBG.UI.TextInputType textType)
        {
            TextType = textType;
        }
    
        public static string SetLastCharToSpace(string Text)
        {
            for (int i = Text.Length - 1; i >= 0; i--)
            {
                if (Text[i] != ' ')
                {
                    Text = Text.Remove(i, 1).Insert(i, " ");
                    break;
                }
            }
            return Text;
        }
        
        public string Format(string text)
        {
            switch (TextType)
            {
                case TextInputType.Any:
                    return text;
        
                case TextInputType.Numeric:
                    return new string(text.Where(char.IsDigit).ToArray());
        
                case TextInputType.Decimal:
                    bool dotFound = false;
                    return new string(text.Where(c =>
                    {
                        if (char.IsDigit(c))
                            return true;
                        if ((c == '.' || c == ',') && !dotFound)
                        {
                            dotFound = true;
                            return true;
                        }
                        return false;
                    }).ToArray());
        
                case TextInputType.Alphabetic:
                    return new string(text.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c)).ToArray());
        
                case TextInputType.Alphanumeric:
                    return new string(text.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());
        
                case TextInputType.AlphabeticDecimal:
                    bool decimalDotFound = false;
                    return new string(text.Where(c =>
                    {
                        if (char.IsLetterOrDigit(c))
                            return true;
                        if ((c == '.' || c == ',') && !decimalDotFound)
                        {
                            decimalDotFound = true;
                            return true;
                        }
                        return false;
                    }).ToArray());
        
                case TextInputType.SpecialCharacters:
                    return new string(text.Where(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)).ToArray());
        
                default:
                    return text;
            }
        }
    }

    public enum TextInputType
    {
        Any,
        AlphabeticDecimal,
        Alphanumeric,
        Alphabetic,
        Decimal,
        Numeric,
        SpecialCharacters
    }
}
