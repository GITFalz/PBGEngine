using PBG.MathLibrary;

namespace PBG.Editor;

public partial class EditorUI
{
    // Backgrounds
    public static readonly Vector4 Bg0        = new(0.055f, 0.055f, 0.063f, 1f); // #0e0e10
    public static readonly Vector4 Bg1        = new(0.078f, 0.078f, 0.086f, 1f); // #141416
    public static readonly Vector4 Bg2        = new(0.102f, 0.102f, 0.118f, 1f); // #1a1a1e
    public static readonly Vector4 Bg3        = new(0.133f, 0.133f, 0.157f, 1f); // #222228
    public static readonly Vector4 Bg4        = new(0.165f, 0.165f, 0.196f, 1f); // #2a2a32

    // Borders
    public static readonly Vector4 Border     = new(0.180f, 0.180f, 0.220f, 1f); // #2e2e38
    public static readonly Vector4 Border2    = new(0.227f, 0.227f, 0.275f, 1f); // #3a3a46

    // Text
    public static readonly Vector4 Text0      = new(0.910f, 0.910f, 0.941f, 1f); // #e8e8f0
    public static readonly Vector4 Text1      = new(0.627f, 0.627f, 0.722f, 1f); // #a0a0b8
    public static readonly Vector4 Text2      = new(0.376f, 0.376f, 0.471f, 1f); // #606078

    // Accent
    public static readonly Vector4 Accent     = new(0.486f, 0.416f, 0.969f, 1f); // #7c6af7
    public static readonly Vector4 Accent2    = new(0.369f, 0.314f, 0.831f, 1f); // #5e50d4
    public static readonly Vector4 AccentDim  = new(0.486f, 0.416f, 0.969f, 0.15f);

    // Semantic
    public static readonly Vector4 Green      = new(0.298f, 0.686f, 0.490f, 1f); // #4caf7d
    public static readonly Vector4 Red        = new(0.878f, 0.361f, 0.361f, 1f); // #e05c5c
    public static readonly Vector4 Amber      = new(0.878f, 0.651f, 0.314f, 1f); // #e0a650

    // Component icon tints (background fill)
    public static readonly Vector4 IconTransformBg = new(0.376f, 0.706f, 0.863f, 0.15f);
    public static readonly Vector4 IconTransformFg = new(0.376f, 0.706f, 0.863f, 1f);  // #60b4dc
    public static readonly Vector4 IconMeshBg      = new(0.298f, 0.686f, 0.490f, 0.15f);
    public static readonly Vector4 IconMeshFg      = new(0.298f, 0.686f, 0.490f, 1f);
    public static readonly Vector4 IconScriptBg    = new(0.486f, 0.416f, 0.969f, 0.15f);
    public static readonly Vector4 IconScriptFg    = new(0.486f, 0.416f, 0.969f, 1f);
    public static readonly Vector4 IconRbBg        = new(0.878f, 0.651f, 0.314f, 0.15f);
    public static readonly Vector4 IconRbFg        = new(0.878f, 0.651f, 0.314f, 1f);
}