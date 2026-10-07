namespace PBG.UI;

public static partial class CustomStyles
{
    public readonly static UnaryType<string> item_ = new((name, e) =>
    {
        if (ItemDataManager.AllItems.TryGetValue(name, out var item) && e is UIPanel panel)
            panel.TextureID = item.Index | 0x40000000;  
    });
}