
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.UI;

namespace PBG.Rendering.Meshes
{
    public class TextMesh
    {
        private UIController controller;
        public int LineCount = 0;
        public UILineStruct[] LineStructs = [];

        public UI.IUIText? CursorText = null;
        public int CursorCharacter = -1;
        
        public int GlyphCount = 0;
        public int VisibleGlyphCount = 0;
        public UIGlyphStruct[] GlyphStructs = [];
        public Dictionary<UI.IUIText, TextMetadata> Texts = [];

        public HashSet<IUIText> TextsToBeRemoved = [];
        public HashSet<IUIText> TextsToBeAdded = [];

        private SSBO<UILineStruct> _lineSSBO = new([]);
        private SSBO<UIGlyphStruct> _glyphSSBO = new([]);

        private bool _updateVisibility = false;
        private BufferEnum _bufferUpdateState = BufferEnum.None;

        public readonly Descriptor Descriptor;

        public TextMesh(UIController controller)
        {
            this.controller = controller;
            Descriptor = controller.UIData.GetTextDescriptor();
            Descriptor.BindSSBO(_lineSSBO, 1);
            Descriptor.BindSSBO(_glyphSSBO, 2);
            Descriptor.BindSSBO(controller.MaskData.MaskSSBO, 3);
        }

        public void AddElement(UI.IUIText text)
        {
            TextsToBeAdded.Add(text);
            SetBufferUpdateState(BufferEnum.Recreate);
        }

        public void GenerateCharacters(IUIText textElement)
        {
            if (!Texts.TryGetValue(textElement, out TextMetadata textMetaData))
                return;

            string text = textElement.GetText();
            float maxWidth = textMetaData.CharCount * 7 * textElement.FontSize;
            float width = text.Length * 7 * textElement.FontSize;
            float start = textElement.GetTextAlign() switch
            {
                TextAlign.Left => 0,
                TextAlign.Center => (maxWidth - width) * 0.5f,
                TextAlign.Right => maxWidth - width,
                _ => 0
            };

            for (int i = 0; i < textMetaData.CharCount; i++)
            {
                Vector2 pos = (0, 0);
                Vector2 size = (0, 0);
                int c = -1;
                var glyph = GlyphStructs[textMetaData.StartGlyphIndex + i];
                if (i < text.Length)
                {
                    pos = (start + i * 7 * textElement.FontSize, 0);
                    size = (7 * textElement.FontSize, 9 * textElement.FontSize);
                    c = TextShaderHelper.GetChar(text[i]);
                }
                else if (text.Length == 0 && i == 0)
                {
                    pos = (start + i * 7 * textElement.FontSize, 0);
                    size = (7 * textElement.FontSize, 9 * textElement.FontSize);
                    c = -1;
                }
                glyph.GlyphPosition = pos;
                glyph.GlyphSize = size;
                glyph.Data.Z = c;
                GlyphStructs[textMetaData.StartGlyphIndex + i] = glyph;
            }
            SetBufferUpdateState(BufferEnum.Update);
        }

        public void RemoveElement(UI.IUIText textToRemove)
        {
            TextsToBeRemoved.Add(textToRemove);
            SetBufferUpdateState(BufferEnum.Recreate);
        }

        public void UpdateCharacters(UI.IUIText text)
        {
            GenerateCharacters(text);
        }

        public void UpdateMaskIndex(UI.IUIText text, int index)
        {
            if (Texts.TryGetValue(text, out var textMetadata))
            {
                var lineStruct = LineStructs[textMetadata.LineIndex];
                lineStruct.LineInfo.W = index;
                LineStructs[textMetadata.LineIndex] = lineStruct;
            }
        }

        public void UpdateTransform(UI.IUIText text)
        {
            if (!Texts.TryGetValue(text, out TextMetadata metaData))
                return;

            var lineStruct = LineStructs[metaData.LineIndex];
            lineStruct.LineInfo.X = text.Transform.X;
            lineStruct.LineInfo.Y = text.Transform.Y;
            lineStruct.LineInfo.Z = text.Transform.Z;
            lineStruct.Center = text.GetCenter();
            LineStructs[metaData.LineIndex] = lineStruct;

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateColor(UI.IUIText text)
        {
            if (!Texts.TryGetValue(text, out TextMetadata metaData))
                return;

            var lineStruct = LineStructs[metaData.LineIndex];
            lineStruct.Color.X = text.Color.toPackedColor();
            LineStructs[metaData.LineIndex] = lineStruct;

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void SetCursor(UI.IUIText text)
        {
            RemoveCursor();
            if (!Texts.TryGetValue(text, out TextMetadata metaData))
                return;

            if (UIController.CursorCharacter >= 0)
            {
                bool start = true;
                var textLength = text.GetText().Length;
                if (UIController.CursorCharacter < textLength || textLength == 0)
                {
                    CursorCharacter = UIController.CursorCharacter;
                    var glyph = GlyphStructs[metaData.StartGlyphIndex + CursorCharacter];
                    glyph.Data.W = 1;
                    GlyphStructs[metaData.StartGlyphIndex + CursorCharacter] = glyph;
                }
                else
                {
                    CursorCharacter = Mathf.Max(textLength - 1, 0);
                    var glyph = GlyphStructs[metaData.StartGlyphIndex + CursorCharacter];
                    glyph.Data.W = 2;
                    GlyphStructs[metaData.StartGlyphIndex + CursorCharacter] = glyph;
                    start = false;
                }

                if (UIController.SelectionSize >= 0)
                {
                    for (int i = 0; i < UIController.SelectionSize; i++)
                    {
                        var glyph = GlyphStructs[metaData.StartGlyphIndex + CursorCharacter + i];
                        glyph.Data.W = 0b100 | (glyph.Data.W & 0b011);
                        GlyphStructs[metaData.StartGlyphIndex + CursorCharacter + i] = glyph;
                    }
                }
                else
                {
                    for (int i = start ? -1 : 0; i >= UIController.SelectionSize + (start ? 0 : 1); i--)
                    {
                        var glyph = GlyphStructs[metaData.StartGlyphIndex + CursorCharacter + i];
                        glyph.Data.W = 0b100 | (glyph.Data.W & 0b011);
                        GlyphStructs[metaData.StartGlyphIndex + CursorCharacter + i] = glyph;
                    }
                }
            }

            SetBufferUpdateState(BufferEnum.Update);
            CursorText = text;
        }

        public void RemoveCursor()
        {
            if (CursorText == null || !Texts.TryGetValue(CursorText, out TextMetadata metaData))
                return;

            for (int i = 0; i < metaData.CharCount; i++)
            {
                var glyph = GlyphStructs[metaData.StartGlyphIndex + i];
                glyph.Data.W = 0;
                GlyphStructs[metaData.StartGlyphIndex + i] = glyph;
            }

            SetBufferUpdateState(BufferEnum.Update);
            CursorText = null;
            CursorCharacter = -1;
        }
        

        public void UpdateVisibility()
        {
            int visibleIndex = 0;
            VisibleGlyphCount = 0;
            foreach (var (text, metaData) in Texts)
            {
                if (text.Visible)
                {
                    for (int j = 0; j < metaData.CharCount; j++)
                    {
                        int glyphIndex = metaData.StartGlyphIndex + j;
                        var glyph = GlyphStructs[visibleIndex];
                        if (glyphIndex != glyph.Data.X)
                        {
                            glyph.Data.X = glyphIndex;
                        }
                        GlyphStructs[visibleIndex] = glyph;
                        visibleIndex++;
                    }
                    VisibleGlyphCount += metaData.CharCount;
                }
            }

            _updateVisibility = false;
        }

        public void Resize()
        {
            foreach (var (text, metaData) in Texts)
            {
                var lineStruct = LineStructs[metaData.LineIndex];
                lineStruct.LineInfo = new Vector4(text.Transform.Xyz, text.MaskIndex);
                lineStruct.Center = text.GetCenter();   
                LineStructs[metaData.LineIndex] = lineStruct;
            }
            _lineSSBO.Update([..LineStructs]);
        }

        public void Update()
        {
            if (_bufferUpdateState != BufferEnum.None)
            {
                UpdateBuffers();
            }
        }

        public void QueueUpdateVisibility()
        {
            if (_bufferUpdateState != BufferEnum.Recreate)
                SetBufferUpdateState(BufferEnum.Update);
            _updateVisibility = true;
        }

        private void UpdateBuffers()
        {
            switch (_bufferUpdateState)
            {
                case BufferEnum.Update:
                    if (_updateVisibility)
                        UpdateVisibility();

                    _lineSSBO.Update(LineStructs);
                    _glyphSSBO.Update(GlyphStructs);
                    break;
                case BufferEnum.Recreate:

                    foreach (var panel in TextsToBeRemoved)
                        Texts.Remove(panel);

                    foreach (var panel in TextsToBeAdded)
                        Texts.TryAdd(panel, new());
                    
                    LineStructs = new UILineStruct[Texts.Count];
                    List<UIGlyphStruct> newGlyphs = [];

                    LineCount = 0;
                    GlyphCount = 0;
                    foreach (var (textElement, _) in Texts)
                    {
                        var textMetaData = new TextMetadata(LineCount, GlyphCount, textElement.MaxCharCount ?? 20);
                        Texts[textElement] = textMetaData;
                        LineStructs[LineCount] = new UILineStruct
                        {
                            LineInfo = new Vector4(textElement.Transform.Xyz, textElement.MaskIndex),
                            Color = (textElement.Color.toPackedColor(), 0),
                            Center = textElement.GetCenter(),
                            Translation = textElement.AnimationTranslation,
                            ScaleRotation = (textElement.AnimationScale, textElement.AnimationRotation),
                        };

                        var text = textElement.GetText();
                        float maxWidth = textMetaData.CharCount * 7 * textElement.FontSize;
                        float width = text.Length * 7 * textElement.FontSize;
                        float start = textElement.GetTextAlign() switch
                        {
                            TextAlign.Left => 0,
                            TextAlign.Center => (maxWidth - width) * 0.5f,
                            TextAlign.Right => maxWidth - width,
                            _ => 0
                        };
                        
                        for (int i = 0; i < textMetaData.CharCount; i++)
                        {
                            Vector2 pos = (0, 0);
                            Vector2 size = (0, 0);
                            int c = -1;
                            var glyph = new UIGlyphStruct()
                            {
                                Data = (0, LineCount, -1, 0)
                            };
                            if (i < text.Length)
                            {
                                pos = (start + i * 7 * textElement.FontSize, 0);
                                size = (7 * textElement.FontSize, 9 * textElement.FontSize);
                                c = TextShaderHelper.GetChar(text[i]);
                            }
                            else if (text.Length == 0 && i == 0)
                            {
                                pos = (start + i * 7 * textElement.FontSize, 0);
                                size = (7 * textElement.FontSize, 9 * textElement.FontSize);
                                c = -1;
                            }
                            glyph.GlyphPosition = pos;
                            glyph.GlyphSize = size;
                            glyph.Data.Z = c;
                            newGlyphs.Add(glyph);
                        }

                        LineCount++;
                        GlyphCount += textElement.MaxCharCount ?? 20;
                    }

                    GlyphStructs = [..newGlyphs];

                    TextsToBeRemoved = [];
                    TextsToBeAdded = [];

                    UpdateVisibility();

                    _lineSSBO.Renew(LineStructs);
                    _glyphSSBO.Renew(GlyphStructs);
                    Descriptor.BindSSBO(_lineSSBO, 1);
                    Descriptor.BindSSBO(_glyphSSBO, 2);
                    break;
            }

            _bufferUpdateState = BufferEnum.None;
            _updateVisibility = false;
        }

        public void UpdateAnimationTranslation(UI.IUIText text)
        {
            Texts.TryGetValue(text, out var meta);

            var lineStruct = LineStructs[meta.LineIndex];
            lineStruct.Translation = text.AnimationTranslation;
            LineStructs[meta.LineIndex] = lineStruct;

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateAnimationScale(UI.IUIText text)
        {
            Texts.TryGetValue(text, out var meta);

            var lineStruct = LineStructs[meta.LineIndex];
            lineStruct.ScaleRotation.X = text.AnimationScale;
            LineStructs[meta.LineIndex] = lineStruct;

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateAnimationRotation(UI.IUIText text)
        {
            Texts.TryGetValue(text, out var meta);

            var lineStruct = LineStructs[meta.LineIndex];
            lineStruct.ScaleRotation.Y = text.AnimationRotation;
            LineStructs[meta.LineIndex] = lineStruct;

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void Render()
        {
            if (VisibleGlyphCount <= 0)
                return;

            Descriptor.Bind();
            /*
            _vao.Bind();
            _lineSSBO.Bind(0);
            _glyphSSBO.Bind(1);

            GL.DrawArrays(PrimitiveType.Triangles, 0, VisibleGlyphCount * 6);
            Shader.Error($"TextMesh {controller.Name} error: ");

            _lineSSBO.Unbind();
            _glyphSSBO.Unbind();
            _vao.Unbind();
            */
            GFX.Draw((uint)VisibleGlyphCount * 6, 1, 0, 0);
        }

        public void SetBufferUpdateState(BufferEnum state)
        {
            if ((int)_bufferUpdateState < (int)state)
            {
                _bufferUpdateState = state;
            }
        } 

        public void Delete()
        {
            LineStructs = [];
            GlyphStructs = [];
            Texts = [];

            //_vao.DeleteBuffer();
            //_lineSSBO.DeleteBuffer();
            //_glyphSSBO.DeleteBuffer();
        }

        public struct TextMetadata(int lineIndex, int startGlyphIndex, int charCount)
        {
            public int LineIndex = lineIndex;
            public int StartGlyphIndex = startGlyphIndex;
            public int CharCount = charCount;
        }
    }

    public struct UILineStruct
    {
        public Vector4 LineInfo; // xyz is position, w is mask index
        public Vector2i Color;
        public Vector2 Center;
        public Vector2 Translation;
        public Vector2 ScaleRotation;

        public override string ToString()
        {
            return $"Info: {LineInfo}, Color: {Color}";
        }
    }

    public struct UIGlyphStruct
    {
        public Vector2 GlyphPosition;
        public Vector2 GlyphSize;
        public Vector4i Data; // x is real character index, y is line index, z is char index in texture, w not yet defined

        public override string ToString()
        {
            return $"Position: {GlyphPosition}, Size: {GlyphSize}, Data: {Data}";
        }
    }
}
