using PBG.UI;
using static PBG.UI.Styles;

public partial class StructureNodeUI
{
    private UIElementBase LeftPanel() =>
    new UICol(w_[240], h_full_minus_[60], bottom_left, blank_full_g_[30])[
        LeftTreePanel(),
        LeftNoisePanel(),
        _leftStructurePanel
    ].Ref(ref nodeManager.LeftPanelCollection);
}