using PBG.UI;
using PBG.UI.Creator;
using static PBG.UI2.Styles;

namespace PBG.Editor;

public partial class EditorUI : UIScript
{
    public static EditorUI Instance = null!;
    public UIText FPSText = null!;
    
    public EditorUI() { Instance = this; }

    public override UIElementBase Script() =>
    new UICol().Class(w_full, h_full)[
        new UICol().Class(w_full, h_[30], blank_full, rgba_v4_[Bg1])[
            new UIText("PBGEditor").Class(middle_left, left_[10], fs_[1.5f]),
            new UIText("FPS:").Class(middle_right, right_[10], fs_[1.2f], mc_[10]).Ref(ref FPSText)
        ],
        LeftPanel,
        CenterPanel,
        RightPanel
    ];
}