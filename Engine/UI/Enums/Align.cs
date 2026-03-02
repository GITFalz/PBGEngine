using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PBG.UI
{
    public enum UIAlign
    {
        TopLeft = 0x1,
        TopCenter = 0x2,
        TopRight = 0x4,
        MiddleLeft = 0x8,
        MiddleCenter = 0x10,
        MiddleRight = 0x20,
        BottomLeft = 0x40,
        BottomCenter = 0x80,
        BottomRight = 0x100
    }

    public enum UIAlignMasks
    {
        Left = 0x49,
        Center = 0x92,
        Right = 0x124,
        Top = 0x7,
        Middle = 0x39,
        Bottom = 0x1c,
    }
}