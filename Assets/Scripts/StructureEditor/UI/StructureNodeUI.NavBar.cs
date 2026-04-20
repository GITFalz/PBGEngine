using PBG.Assets.Scripts.NoiseNodes;
using PBG.UI;
using PBG.Core;
using static PBG.UI.Styles;

public partial class StructureNodeUI
{
    private UIElementBase NavigationBar() =>
    new UICol(w_full, h_[60], blank_full_g_[30])[
        new UICol(w_full, h_full_minus_[2], blank_full_g_[20], top_center)[
            new UIText("PBG Editor", fs_[2f], top_[18], top_left, left_[18]),
            new UICol(h_[40], blank_sharp_g_[25], w_[120], right_[5], middle_right)
            .OnClick(_ => {
                GLSLManager.CompileCompute();
                NoiseNodeManager.Load(NodeManager.CurrentPath);
                Scene.LoadScene("World");
            })[
                new UIText("World", mc_[5], fs_[1.2f], middle_center)
            ],
            new UIHCol(grow_children, top_left, left_[240], h_[60], spacing_[8])[
                new UICol(middle_left, w_[100], h_[44], blank_sharp_g_[30], left_[5])
                    .OnClick(_ => nodeManager.SwitchTree())[
                    new UIText("Tree", middle_center, fs_[1.2f])
                ].Ref(ref nodeManager.TreeButton),
                new UICol(middle_left, w_[100], h_[44], blank_sharp_g_[20])
                    .OnClick(_ => nodeManager.SwitchNoise())[
                    new UIText("Noise", middle_center, fs_[1.2f])
                ].Ref(ref nodeManager.NoiseButton),
                new UICol(middle_left, w_[100], h_[44], blank_sharp_g_[20])
                    .OnClick(_ => nodeManager.SwitchStructure())[
                    new UIText("Structure", middle_center, fs_[1.2f])
                ].Ref(ref nodeManager.StructureButton)
            ]
        ]
    ];
}