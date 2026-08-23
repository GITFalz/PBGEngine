using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PBG.UI
{
    public enum UIAlign
    {
        TopLeft = 1,
        TopCenter = 2,
        TopRight = 4,
        MiddleLeft = 8,
        MiddleCenter = 16,
        MiddleRight = 32,
        BottomLeft = 64,
        BottomCenter = 128,
        BottomRight = 256
    }

    public enum UIAlignMasks
    {
        Left = 73,
        Center = 146,
        Right = 292,
        Top = 7,
        Middle = 112,
        Bottom = 448,
    }
}