using PBG.UI;
using static PBG.UI.Styles;

public partial class StructureNodeUI
{
    private UIElementBase RightPanel() =>
    new UICol(w_[240], h_full_minus_[60], bottom_right, blank_full_g_[30])[
        RightTreePanel(),
        RightNoisePanel(),
        _rightStructurePanel
    ];
}