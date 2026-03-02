using PBG.MathLibrary;

namespace PBG.UI
{
    public static class Constraint
    {
        public static IUIConstraint GetUIConstraint(UIConstraint constraint, float value)
        {
            return _constraints[constraint](value);
        }

        private static readonly Dictionary<UIConstraint, Func<float, IUIConstraint>> _constraints = new()
        {
            { UIConstraint.SetLeftPx,  (o) => new UISetOffsetConstraint(o, 0) },
            { UIConstraint.AddLeftPx,  (o) => new UIAddOffsetConstraint(o, 0) },
            { UIConstraint.SetLeftPc,  (o) => new UISetHorizontalPercentConstraint(o, 0) },
            { UIConstraint.AddLeftPc,  (o) => new UIAddHorizontalPercentConstraint(o, 0) },

            { UIConstraint.SetTopPx,   (o) => new UISetOffsetConstraint(o, 1) },
            { UIConstraint.AddTopPx,   (o) => new UIAddOffsetConstraint(o, 1) },
            { UIConstraint.SetTopPc,   (o) => new UISetVerticalPercentConstraint(o, 1) },
            { UIConstraint.AddTopPc,   (o) => new UIAddVerticalPercentConstraint(o, 1) },

            { UIConstraint.SetRightPx, (o) => new UISetOffsetConstraint(o, 2) },
            { UIConstraint.AddRightPx, (o) => new UIAddOffsetConstraint(o, 2) },
            { UIConstraint.SetRightPc, (o) => new UISetHorizontalPercentConstraint(o, 2) },
            { UIConstraint.AddRightPc, (o) => new UIAddHorizontalPercentConstraint(o, 2) },

            { UIConstraint.SetBottomPx,(o) => new UISetOffsetConstraint(o, 3) },
            { UIConstraint.AddBottomPx,(o) => new UIAddOffsetConstraint(o, 3) },
            { UIConstraint.SetBottomPc,(o) => new UISetVerticalPercentConstraint(o, 3) },
            { UIConstraint.AddBottomPc,(o) => new UIAddVerticalPercentConstraint(o, 3) },
        };
    }

    public interface IUIConstraint
    {
        void ApplyConstraint(ref Vector4 offset, float width, float height);
    }

    public struct UISetOffsetConstraint(float o, int i) : IUIConstraint
    {
        public void ApplyConstraint(ref Vector4 offset, float width, float height) => offset[i] = o;
    }

    public struct UISetHorizontalPercentConstraint(float p, int i) : IUIConstraint
    {
        readonly float P = p / 100f;
        public void ApplyConstraint(ref Vector4 offset, float width, float height) => offset[i] = width * P;
    }

    public struct UISetVerticalPercentConstraint(float p, int i) : IUIConstraint
    {
        readonly float P = p / 100f;
        public void ApplyConstraint(ref Vector4 offset, float width, float height) => offset[i] = height * P;
    }

    public struct UIAddOffsetConstraint(float o, int i) : IUIConstraint
    {
        public void ApplyConstraint(ref Vector4 offset, float width, float height) => offset[i] += o;
    }

    public struct UIAddHorizontalPercentConstraint(float p, int i) : IUIConstraint
    {
        readonly float P = p / 100f;
        public void ApplyConstraint(ref Vector4 offset, float width, float height) => offset[i] += width * P;
    }

    public struct UIAddVerticalPercentConstraint(float p, int i) : IUIConstraint
    {
        readonly float P = p / 100f;
        public void ApplyConstraint(ref Vector4 offset, float width, float height) => offset[i] += height * P;
    }
    public enum UIConstraint
    {
        SetLeftPx,
        AddLeftPx,
        SetLeftPc,
        AddLeftPc,

        SetTopPx,
        AddTopPx,
        SetTopPc,
        AddTopPc,

        SetRightPx,
        AddRightPx,
        SetRightPc,
        AddRightPc,

        SetBottomPx,
        AddBottomPx,
        SetBottomPc,
        AddBottomPc
    }
}