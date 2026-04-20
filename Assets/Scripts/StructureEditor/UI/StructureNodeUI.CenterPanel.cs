using PBG.UI;
using static PBG.UI.Styles;

public partial class StructureNodeUI
{
    private UIElementBase CenterPanel() =>
    new UICol(w_minus_[100, 500], h_minus_[100, 60], bottom_left, left_[250]).OnClick(nodeManager.CenterOnClick)[
        new UIText("0", mc_[20], fs_[1], bottom_left, left_[5], bottom_[20]).Ref(ref _fpsText),
        new UIText("0", mc_[20], fs_[1], bottom_left, left_[5], bottom_[5]).Ref(ref _ramText),
        new UIImg(w_[30], h_[30], middle_center, icon_[16], gray_[50])
    ].Ref(ref _centerPanel);
}