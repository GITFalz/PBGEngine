namespace PBG.UI;

public struct UIAlignment(UIController controller)
{
    public int Left = 0;
    public int Right = 0;
    public int Top = 0;
    public int Bottom = 0;

    public int Width => VoxelEngine.Width - (Left + Right);
    public int Height => VoxelEngine.Height - (Top + Bottom);
}