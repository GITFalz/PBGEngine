using PBG.UI;
using PBG.UI.Creator;
using static PBG.UI2.Styles;
using static PBG.Editor.EditorUI;
using PBG.MathLibrary;

namespace PBG.Editor;

public class FileMover : UIScript
{
    public static FileMover Instance = null!;

    private UIText? _row1 = null;
    private UIText? _row2 = null;
    private Vector2 _oldPosition = Vector2.Zero;

    public FileMover() { Instance = this; }

    public override UIElementBase Script() =>
    new UICol().Class(w_[90], h_[90], depth_[20], top_left, blank_sharp, rgba_v4_[Bg2], hover_color_[Bg2, Bg4], hover_color_easeout, hover_color_duration_[0.5f], hidden)[
        new UIImg().Class(w_[50], h_[50], top_center, top_[10], icon_[20], rgba_v4_[Text1])
    ];
    
    public void Show()
    {
        Element.SetVisible(true);
    }

    public void SetPosition(Vector2 position)
    {
        if (_oldPosition == position)
            return;

        Element.BaseOffset = position;
        Element.ApplyChanges(UIChange.Transform);

        _oldPosition = position;
    }

    public void Hide()
    {
        Element.SetVisible(false);
        _row1?.Delete();
        _row2?.Delete();
    }

    public void SetText(string text)
    {
        _row1 = new UIText(text).Class(bottom_center, bottom_[text.Length > 12 ? 15 : 5], mc_[12.Min(text.Length)], rgba_v4_[Text1]);
        ((UICol)Element).AddElement(_row1);
        UIController?.AddElement(_row1);
        if (text.Length > 12)
        {
            string trimmed = text[12..];
            _row2 = new UIText(trimmed).Class(bottom_center, bottom_[5], mc_[12.Min(trimmed.Length)], rgba_v4_[Text1]);
            ((UICol)Element).AddElement(_row2);
            UIController?.AddElement(_row2);
        }
    }
}