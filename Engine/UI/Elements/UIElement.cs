using System.Data;
using PBG.Data;
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.UI.Creator;

namespace PBG.UI
{
    public enum UIElementTag
    {
        Any,
        UIText,
        UIField,
        UIImage,
        UIButton,
        UIGraph,
        UICollection,
        UIHorizontalCollection,
        UIHorizontalScrollView,
        UIVerticalCollection,
        UIVerticalScrollView,
    }

    public enum UIChange
    {
        None = 0,
        Transform = 1 << 0,
        Scale = 1 << 1,
        Color = 1 << 2,
        Characters = 1 << 3,
        Border = 1 << 4,
        BorderColor = 1 << 5,
    }
}