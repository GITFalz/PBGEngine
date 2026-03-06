using Compiler;
using PBG.MathLibrary;
using PBG.Compiler;
using PBG.Data;
using PBG.UI;
using PBG.UI.Creator;
using static PBG.UI.Styles;
using Silk.NET.Input;

public class StructureEditorScriptUI(StructureEditor editor) : UIScript
{
    public int LineIndex = 0;
    public UIField? CurrentField = null;

    private UIVScroll IndexCanvasCol = null!;
    private UIVScroll TextCanvasCol = null!;

    private UICol HighlightCol = null!;
    private bool _clearHighlights = false;

    private UIVCol InfoPanel = null!;
    private UIVCol InfoTextCol = null!;

    private UIImg LoadingIcon = null!;
    public UIText SuccessText = null!;
    public UIText ErrorText = null!;
    public bool Loading = false;

    private float? _saveTimer = null;

    public override UIElementBase Script() =>
    new UICol(Class(w_full_minus_[480], h_full_minus_[60], top_center, top_[60], mask_children), Sub(
        new UICol(Class(w_full, h_[30], blank_full_g_[20]), Sub(
            new UIImg(Class(w_[20], h_[20], icon_[15], bg_white, left_[5], middle_left, hover_scale_easeout_[1.2f, 0.2f]), OnClickImg(_ => editor.CloseScript())),
            newImg(Class(w_[20], h_[20], icon_[24], bg_white, right_[55], middle_right, hidden), OnClickImg(_ => Save()), ref LoadingIcon),
            newText("Compilation Successfull", Class(middle_right, rgba_[0, 1, 0, 1], right_[55], hidden), ref SuccessText),
            newText("An error occured", Class(middle_right, rgba_[1, 0, 0, 1], right_[55], hidden), ref ErrorText),
            new UIImg(Class(w_[20], h_[20], icon_[27], bg_white, right_[30], middle_right, hover_scale_easeout_[1.2f, 0.2f]), OnClickImg(_ => Save())),
            new UIImg(Class(w_[20], h_[20], icon_[0], bg_white, right_[5], middle_right, hover_scale_easeout_[1.2f, 0.2f]), OnClickImg(_ => Compile()))
        )),
        newVScroll(Class(w_[40], h_full_minus_[30], bottom_left, spacing_[5], border_[0, 5, 0, 0]), Sub(
            new UIText("1", Class(left_[5]))
        ), ref IndexCanvasCol),
        new UIImg(w_[2], h_full_minus_[30], bottom_left, blank_full_g_[30], left_[40]),
        newCol(Class(bottom_right, w_full_minus_[45], h_full_minus_[33]), Sub(), ref HighlightCol),
        newVCol(Class(depth_[10], grow_children, blank_full_g_[20], hidden), [
            new UICol(Class(w_full, h_[15], blank_full_g_[30]), []),
            newVCol("test", Class(grow_children, border_[5, 5, 5, 5]), [
                new UIText("No error yet"),
            ], ref InfoTextCol)
        ], ref InfoPanel),
        newVScroll(Class(w_full_minus_[42], h_full_minus_[30], bottom_right, spacing_[5], border_[0, 5, 0, 0]), 
        OnHoverVScroll(_ => Hover()),
        Sub(
            new UIField("", Class(mc_[1000], data_["index", 0], left_[5], depth_[2]), OnClickField(LineClick), OnTextChange(OnFieldChange))
        ), ref TextCanvasCol)
    ));

    private UIField? _newSetField = null;
    private int _oldCursorChar = 0;

    public void SetLines(List<string> lines)
    {
        UIController.RemoveInputfield();
        LineIndex = 0;
        TextCanvasCol.DeleteChildren();
        if (lines.Count == 0)
            lines.Add("");
        for (int i = 0; i < lines.Count; i++)
        {
            var newLine = new UIField(lines[i], Class(mc_[1000], data_["index", 0], left_[5], depth_[2]), OnClickField(LineClick), OnTextChange(OnFieldChange));
            TextCanvasCol.AddElement(newLine);
            UIController.AddElement(newLine);
        }
        RegenerateIndices();
    }

    private void Compile()
    {
        List<string> lines = [];
        foreach (var element in TextCanvasCol.ChildElements)
        {
            if (element is UIField field)
                lines.Add(field.GetText());
        }

        Compile(lines);
    }

    private void Compile(List<string> lines)
    {
        ClearMarkers();

        UIImg MakeButton(CompilerLog log)
        {
            UIImg img = new(Class(w_[7 * log.Token.Count + 4], h_[9 + 4], rgba_v4_[log.Color], left_[7 * log.Token.IndexStart], top_[14 * log.Index], blank_full),
            OnHoverEnterImg(_ => ShowInfo(log)), OnHoverImg(_ => HoverInfo(log)), OnHoverExitImg(_ => HideInfo()));
            HighlightCol.AddElement(img);
            UIController.AddElement(img);
            return img;
        }

        if (editor.SelectedBoundingBox != null && !StructureCompiler.CompileDefault(lines, ref editor.SelectedBoundingBox))
        {
            var data = StructureCompiler.Compiler.CompileData;
            Console.WriteLine("test");
            data.Print();
            if (data.ErrorLog.Count > 0)
            {
                MakeButton(data.ErrorLog[0]);
            }

            for (int i = 0; i < data.WarningLog.Count; i++)
            {
                MakeButton(data.WarningLog[i]);
            }

            _clearHighlights = true;

            Loading = false;

            LoadingIcon.SetVisible(false);
            SuccessText.SetVisible(false);
            ErrorText.SetVisible(true);
        }
        else if (editor.SelectedBoundingBox != null)
        {
            Console.WriteLine("Compilation Sucessfull!!!");

            var data = StructureCompiler.Compiler.CompileData;
            for (int i = 0; i < data.WarningLog.Count; i++)
            {
                MakeButton(data.WarningLog[i]);
            }

            _clearHighlights = true;

            StructureCompiler.Run();
            editor.SelectedBoundingBox.Executor.Lines = StructureCompiler.Compiler.Lines;

            Loading = false;

            LoadingIcon.SetVisible(false);
            SuccessText.SetVisible(true);
            ErrorText.SetVisible(false);
        }

        HighlightCol.ApplyChanges(UIChange.Scale);
    }
    
    public void Save()
    {
        if (editor.SelectedBoundingBox == null)
            return;

        List<string> lines = [];
        foreach (var element in TextCanvasCol.ChildElements)
        {
            if (element is UIField field)
                lines.Add(field.GetText());
        }

        Compile(lines);
        editor.SelectedBoundingBox.Lines = lines;
    }

    private void ShowInfo(CompilerLog log)
    {
        InfoPanel.Dataset["token"] = log.Token;
        UIText text = new UIText(log.Line);
        InfoTextCol.DeleteChildren();
        InfoTextCol.AddElement(text);
        UIController.AddElement(text);
        InfoPanel.QueueAlign();
        InfoPanel.QueueUpdateScaling();
        InfoPanel.QueueUpdateTransformation();
        InfoPanel.GetElement<UICol>()?.UpdateColor(log.Color);
    }

    private void HoverInfo(CompilerLog log)
    {
        if (!InfoPanel.Visible)
            InfoPanel.SetVisible(true);

        var currentToken = InfoPanel.Dataset.Get<Token>("token");
        if (currentToken != log.Token)
        {
            ShowInfo(log);
        }

        if (Input.MouseDelta != Vector2i.Zero)
        {
            InfoPanel.BaseOffset = Input.MousePosition - (240, 60);
            InfoPanel.ApplyChanges(UIChange.Transform);
        }
    }

    private void HideInfo()
    {
        InfoPanel.SetVisible(false);
    }

    private void OnFieldChange(UIField _)
    {
        PlayLoading();
        ClearMarkers();
    }

    private void PlayLoading()
    {
        if (_saveTimer == null)
        {
            Loading = true;

            LoadingIcon.SetVisible(true);
            SuccessText.SetVisible(false);
            ErrorText.SetVisible(false);
            
            LoadingIcon.Animation = new();
            LoadingIcon.Animation.SetRotation(360f);
            LoadingIcon.Animation.SetRotationDuration(1f);
            LoadingIcon.AnimationEnter();
        }

        _saveTimer = GameTime.TotalTime;
    }

    private void ClearMarkers()
    {
        if (_clearHighlights)
        { 
            HighlightCol.DeleteChildren(); 
            _clearHighlights = false; 
        }
    }

    public void Update()
    {
        if (!LoadingIcon.IsAnimating && Loading)
        {
            LoadingIcon.AnimationRotation = 0;
            LoadingIcon.Animation = new();
            LoadingIcon.Animation.SetRotation(360f);
            LoadingIcon.Animation.SetRotationDuration(1f);
            LoadingIcon.AnimationEnter();
        }

        if (_saveTimer != null && _saveTimer + 2 < GameTime.TotalTime)
        {
            _saveTimer = null;
            LoadingIcon.AnimationExit();
            Save();
        }
    }

    private void Hover()
    {
        if (_newSetField != null)
        {
            UIController.SetInputfield(_newSetField);
            CurrentField = _newSetField;
            _newSetField = null;
        }

        if (Input.IsKeyPressed(Key.Tab) && CurrentField != null)
        {
            var text = CurrentField.GetText();
            int spaces = 4 - (text.Length % 4);
            for (int i = 0; i < spaces; i++)
            {
                text += " ";
            }
            CurrentField.UpdateText(text);
            UIController.CursorCharacter = CurrentField.GetText().Length;
            CurrentField.SetCursor();
            PlayLoading();
        }

        if (Input.IsKeyPressed(Key.Backspace))
        {
            int index = CurrentField?.Dataset.Int("index") ?? 0;
            if (UIController.CursorCharacter == 0 && index > 0 && _oldCursorChar == 0)
            {
                UIController.RemoveInputfield();
                var previousField = TextCanvasCol.ChildElements[index - 1];
                if (previousField is UIField field)
                {
                    LineIndex = index - 1;
                    
                    var length = field.GetText().Length;
                    var newText = field.GetText() + (CurrentField?.GetText() ?? "");

                    field.UpdateText(newText);
                    UIController.CursorCharacter = length;
                    UIController.ActiveInputField = field;
                    field.SetCursor();

                    CurrentField?.Delete();
                    CurrentField = field;

                    TextCanvasCol.Align();
                    TextCanvasCol.QueueUpdateTransformation();

                    UIController.RegenerateBuffers = true;

                    RegenerateIndices();
                    PlayLoading();
                }
            }
        }

        if (Input.IsKeyPressed(Key.Enter))
        {
            if (LineIndex + 1 == TextCanvasCol.ChildElements.Count)
            {
                UIField newLine = GetEnterField();

                TextCanvasCol.AddElement(newLine);
                UIController.AddElement(newLine);

                RegenerateIndices();
                PlayLoading();

                _newSetField = newLine;
                LineIndex++;
            }
            else if (LineIndex + 1 <= TextCanvasCol.ChildElements.Count)
            {
                UIField newLine = GetEnterField();

                TextCanvasCol.Insert(LineIndex + 1, newLine);
                TextCanvasCol.Align();
                TextCanvasCol.QueueUpdateTransformation();
                UIController.AddElement(newLine);

                _newSetField = newLine;
                LineIndex++;

                RegenerateIndices();
                PlayLoading();
            }
        }

        _oldCursorChar = UIController.CursorCharacter;
    }

    private void RegenerateIndices()
    {
        IndexCanvasCol.DeleteChildren();

        for (int i = 0; i < TextCanvasCol.ChildElements.Count; i++)
        {
            var line = TextCanvasCol.ChildElements[i];
            line.Dataset["index"] = i;

            UIText newIndex = new(""+(i + 1), Class(left_[5]));
            IndexCanvasCol.AddElement(newIndex);
            UIController.AddElement(newIndex);
        }
    }

    private UIField GetEnterField()
    {
        UIField newLine;
        if (UIController.CursorCharacter == 0)
        {
            newLine = new(CurrentField?.GetText() ?? "", Class(mc_[1000], left_[5], depth_[2]), OnClickField(LineClick), OnTextChange(OnFieldChange));
            CurrentField?.UpdateText("");
        }
        else if (UIController.CursorCharacter == (CurrentField?.GetText() ?? "").Length)
        {
            newLine = new("", Class(mc_[1000], left_[5], depth_[2]), OnClickField(LineClick), OnTextChange(OnFieldChange));
        }
        else
        {
            var text = CurrentField?.GetText() ?? "";
            string currentText = "";
            string newText = "";
            for (int i = 0; i < text.Length; i++)
            {
                if (i < UIController.CursorCharacter)
                {
                    currentText += text[i];
                }
                else
                {
                    newText += text[i];
                }
            }
            CurrentField?.UpdateText(currentText);
            newLine = new(newText, Class(mc_[1000], left_[5], depth_[2]), OnClickField(LineClick), OnTextChange(OnFieldChange));
        }
        return newLine;
    }

    private void LineClick(UIField field)
    {
        LineIndex = field.Dataset.Int("index");
        CurrentField = field;
    }
}