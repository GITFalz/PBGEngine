using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PBG.UI
{
    public class UISize
    {
        private enum UISizeType
        {
            None,
            Pixel,
            Percent,
            PercentCalc
        }

        private UISizeType SizeType;
        public float Value;
        private float PixelOffset = 0;
        public float AddedOffset = 0;

        private UISize(float value, UISizeType type, float pixels = 0)
        {
            SizeType = type;
            Value = value;
            PixelOffset = pixels;
        }

        public float Compute(float parentSize)
        {
            return SizeType switch
            {
                UISizeType.Pixel => Value,
                UISizeType.Percent => (Value * parentSize) + AddedOffset,
                UISizeType.PercentCalc => (Value * parentSize) + (PixelOffset + AddedOffset),
                _ => Value,
            };
        }

        public bool IsNone() => SizeType == UISizeType.None;
        public bool IsPixels() => SizeType == UISizeType.Pixel;
        public bool IsPercent() => SizeType == UISizeType.Percent || SizeType == UISizeType.PercentCalc;

        public override string ToString()
        {
            return "Type: " + SizeType + " Value: " + Value + " PixelOffset: " + PixelOffset + " AddedOffset: " + AddedOffset;
        }

        public static UISize None(int pixels = 100) => new(pixels, UISizeType.None);
        public static UISize None(float pixels = 100) => new(pixels, UISizeType.None);
        public static UISize Pixels(int pixels) => new(pixels, UISizeType.Pixel);
        public static UISize Pixels(float pixels) => new(pixels, UISizeType.Pixel);

        public static UISize Percent(int percent) => new((float)percent / 100f, UISizeType.Percent);
        public static UISize Percent(float percent) => new((float)percent / 100f, UISizeType.Percent);

        public static UISize PercentCalc(int percent, float pixels) => new(percent / 100f, UISizeType.PercentCalc, pixels);
        public static UISize PercentCalc(float percent, float pixels) => new(percent / 100f, UISizeType.PercentCalc, pixels);

        public static implicit operator UISize(int value) => Pixels(value);
        public static implicit operator UISize(float value) => Percent((int)value);
    }
}