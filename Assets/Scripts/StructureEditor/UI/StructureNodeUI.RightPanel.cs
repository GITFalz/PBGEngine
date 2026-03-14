using PBG.UI;
using static PBG.UI.Styles;

public partial class StructureNodeUI
{
    private UIElementBase RightPanel() =>
    new UICol(Class(w_[240], h_full_minus_[60], bottom_right, blank_full_g_[30]), Sub([
        RightTreePanel(),
        RightNoisePanel(),
        _rightStructurePanel
    ]));
}