namespace PBG.UI;

public struct UIAlignment
{
    public int Left = 0;
    public int Right = 0;
    public int Top = 0;
    public int Bottom = 0;

    public int Width => Game.Width - (Left + Right);
    public int Height => Game.Height - (Top + Bottom);

    public UIAlignment() {}
    public UIAlignment(int left, int right, int top, int bottom)
    {
        Left = left;
        Right = right;
        Top = top;
        Bottom = bottom;
    }
}