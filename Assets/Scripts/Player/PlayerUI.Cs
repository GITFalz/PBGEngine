using PBG.UI;
using PBG.UI.Creator;
using static PBG.UI.Styles;

public class PlayerCrossHair : UIScript
{
    public override UIElementBase Script() =>
    new UICol(Class(w_full, h_full), [
        new UIImg(Class(w_[30], h_[30], middle_center, icon_[16], gray_[50]))
    ]);
}