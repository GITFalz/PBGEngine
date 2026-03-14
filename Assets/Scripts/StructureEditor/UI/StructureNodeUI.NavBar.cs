using PBG.Assets.Scripts.NoiseNodes;
using PBG.UI;
using PBG.Core;
using static PBG.UI.Styles;

public partial class StructureNodeUI
{
    private UIElementBase NavigationBar() =>
    new UICol(Class(w_full, h_[60], blank_full_g_[30]), Sub([
        new UICol(Class(w_full, h_full_minus_[2], blank_full_g_[20], top_center), Sub([
            new UIText("PBG Editor", Class(fs_[2f], top_[18], top_left, left_[18])),
            new UICol(Class(h_[40], blank_sharp_g_[25], w_[120], right_[5], middle_right),
            OnClickCol(_ => {
                GLSLManager.CompileCompute();
                NoiseNodeManager.Load(NodeManager.CurrentPath);
                Scene.LoadScene("World");
            }),
            Sub([
                new UIText("World", Class(mc_[5], fs_[1.2f], middle_center))
            ])),
            new UIHCol(Class(grow_children, top_left, left_[240], h_[60], spacing_[8]), Sub([
                newCol(Class(middle_left, w_[100], h_[44], blank_sharp_g_[30], left_[5]),
                    OnClickCol(_ => nodeManager.SwitchTree()),
                    Sub([
                    new UIText("Tree", Class(middle_center, fs_[1.2f])),
                ]), ref nodeManager.TreeButton),
                newCol(Class(middle_left, w_[100], h_[44], blank_sharp_g_[20]),
                    OnClickCol(_ => nodeManager.SwitchNoise()),
                    Sub([
                    new UIText("Noise", Class(middle_center, fs_[1.2f]))
                ]), ref nodeManager.NoiseButton),
                newCol(Class(middle_left, w_[100], h_[44], blank_sharp_g_[20]),
                    OnClickCol(_ => nodeManager.SwitchStructure()),
                    Sub([
                    new UIText("Structure", Class(middle_center, fs_[1.2f]))
                ]), ref nodeManager.StructureButton)
            ]))
        ]))
    ]));
}