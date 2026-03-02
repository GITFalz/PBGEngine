using PBG.MathLibrary;
using PBG.UI;

namespace PBG.UI
{
    public static class UIHelper
    {
        public static UIAlign TopLeft => UIAlign.TopLeft;
        public static UIAlign TopCenter => UIAlign.TopCenter;
        public static UIAlign TopRight => UIAlign.TopRight;

        public static UIAlign MiddleLeft => UIAlign.MiddleLeft;
        public static UIAlign MiddleCenter => UIAlign.MiddleCenter;
        public static UIAlign MiddleRight => UIAlign.MiddleRight;

        public static UIAlign BottomLeft => UIAlign.BottomLeft;
        public static UIAlign BottomCenter => UIAlign.BottomCenter;
        public static UIAlign BottomRight => UIAlign.BottomRight;


        public static TextInputType Any => TextInputType.Any;
        public static TextInputType AlphabeticDecimal => TextInputType.AlphabeticDecimal;
        public static TextInputType Alphanumeric => TextInputType.Alphanumeric;
        public static TextInputType Alphabetic => TextInputType.Alphabetic;
        public static TextInputType Decimal => TextInputType.Decimal;
        public static TextInputType Numeric => TextInputType.Numeric;
        public static TextInputType SpecialCharacters => TextInputType.SpecialCharacters;


        public static UISize Px(this int value) => UISize.Pixels(value);

        public static UISize Pc(this int value) => UISize.Percent(value);
        public static UISize Pc(this float value) => UISize.Percent(value);

        public static UISize Pc(this int value, int calc) => UISize.PercentCalc(value, calc);
        public static UISize Pc(this float value, int calc) => UISize.PercentCalc(value, calc);
        public static UISize Pc(this int value, float calc) => UISize.PercentCalc(value, calc);
        public static UISize Pc(this float value, float calc) => UISize.PercentCalc(value, calc);


        public readonly static Vector4 GRAY_000 = (0f, 0f, 0f, 1f);
        public readonly static Vector4 GRAY_005 = (0.05f, 0.05f, 0.05f, 1f);
        public readonly static Vector4 GRAY_010 = (0.10f, 0.10f, 0.10f, 1f);
        public readonly static Vector4 GRAY_015 = (0.15f, 0.15f, 0.15f, 1f);
        public readonly static Vector4 GRAY_020 = (0.20f, 0.20f, 0.20f, 1f);
        public readonly static Vector4 GRAY_025 = (0.25f, 0.25f, 0.25f, 1f);
        public readonly static Vector4 GRAY_030 = (0.30f, 0.30f, 0.30f, 1f);
        public readonly static Vector4 GRAY_035 = (0.35f, 0.35f, 0.35f, 1f);
        public readonly static Vector4 GRAY_040 = (0.40f, 0.40f, 0.40f, 1f);
        public readonly static Vector4 GRAY_045 = (0.45f, 0.45f, 0.45f, 1f);
        public readonly static Vector4 GRAY_050 = (0.50f, 0.50f, 0.50f, 1f);
        public readonly static Vector4 GRAY_055 = (0.55f, 0.55f, 0.55f, 1f);
        public readonly static Vector4 GRAY_060 = (0.60f, 0.60f, 0.60f, 1f);
        public readonly static Vector4 GRAY_065 = (0.65f, 0.65f, 0.65f, 1f);
        public readonly static Vector4 GRAY_070 = (0.70f, 0.70f, 0.70f, 1f);
        public readonly static Vector4 GRAY_075 = (0.75f, 0.75f, 0.75f, 1f);
        public readonly static Vector4 GRAY_080 = (0.80f, 0.80f, 0.80f, 1f);
        public readonly static Vector4 GRAY_085 = (0.85f, 0.85f, 0.85f, 1f);
        public readonly static Vector4 GRAY_090 = (0.90f, 0.90f, 0.90f, 1f);
        public readonly static Vector4 GRAY_095 = (0.95f, 0.95f, 0.95f, 1f);
        public readonly static Vector4 GRAY_100 = (1.00f, 1.00f, 1.00f, 1f);

        public readonly static Vector4 ACCENT_BLUE = (0.20f, 0.60f, 1.00f, 1f);
        public readonly static Vector4 ACCENT_BLUE_HOVER = (0.30f, 0.70f, 1.00f, 1f);
        public readonly static Vector4 ACCENT_RED = (0.95f, 0.35f, 0.40f, 1f);
        public readonly static Vector4 ACCENT_GREEN = (0.25f, 0.80f, 0.50f, 1f);
        public readonly static Vector4 ACCENT_ORANGE = (1.00f, 0.60f, 0.20f, 1f);
        public readonly static Vector4 ACCENT_PURPLE = (0.70f, 0.50f, 1.00f, 1f);
        public readonly static Vector4 ACCENT_YELLOW = (1.00f, 0.85f, 0.30f, 1f);

        public readonly static Vector4 TRANSPARENT = (0, 0, 0, 0);

        public readonly static Vector2 SLICE_75 = (7.5f, 0.05f);
        public readonly static Vector2 SLICE_100 = (7.5f, 0.05f);

        public static Vector4 WHITE => GRAY_100;
        public static Vector4 BLACK => GRAY_000;
    }
}