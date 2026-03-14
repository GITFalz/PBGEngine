using PBG.UI;
using static PBG.UI.Styles;

public partial class StructureNodeUI
{
    private UIElementBase LeftPanel() =>
    newCol(Class(w_[240], h_full_minus_[60], bottom_left, blank_full_g_[30]), Sub([
        LeftTreePanel(),
        LeftNoisePanel(),
        _leftStructurePanel
    ]), ref nodeManager.LeftPanelCollection);

    
}