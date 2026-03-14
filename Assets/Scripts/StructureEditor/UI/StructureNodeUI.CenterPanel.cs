using PBG.UI;
using static PBG.UI.Styles;

public partial class StructureNodeUI
{
    private UIElementBase CenterPanel() =>
    newCol(Class(w_minus_[100, 500], h_minus_[100, 60], bottom_left, left_[250]), OnClick(nodeManager.CenterOnClick), Sub(
        newText("0", Class(mc_[20], fs_[1], bottom_left, left_[5], bottom_[20]), ref _fpsText),
        newText("0", Class(mc_[20], fs_[1], bottom_left, left_[5], bottom_[5]), ref _ramText),
        new UIImg(Class(w_[30], h_[30], middle_center, icon_[16], gray_[50]))
    ), ref _centerPanel);
}