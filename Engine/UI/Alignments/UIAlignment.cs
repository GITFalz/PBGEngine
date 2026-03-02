namespace PBG.UI;

public class UIAlignment(UIController controller)
{
    public int Left = 0;
    public int Right = 0;
    public int Top = 0;
    public int Bottom = 0;

    public int Width => Game.Width - (Left + Right);
    public int Height => Game.Height - (Top + Bottom);
}