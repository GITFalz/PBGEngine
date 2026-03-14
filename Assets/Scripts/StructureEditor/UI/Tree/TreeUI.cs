using PBG.UI;
using static PBG.UI.Styles;

public partial class StructureNodeUI
{
    private UIElementBase LeftTreePanel() =>
    newVCol(Class(w_full_minus_[2], h_full, blank_full_g_[20], top_left, spacing_[5]), Sub([
        new UIVCol(Class(w_full, top_center, grow_children), Sub([
            new UICol(Class(h_[25], w_full_minus_[10], top_center), Sub([
                new UIText("BOUNDS", Class(fs_[1.2f], middle_left)),
            ])),
            new UICol(Class(h_[25], w_full_minus_[10], top_center), Sub([
                new UIText("Min", Class(fs_[1f], middle_left))
            ])),
            new UICol(Class(w_full_minus_[10], h_[30], top_center), Sub([
                new UICol(Class(h_[25], w_[31f], blank_sharp_g_[10], top_left), Sub([
                    newField("0", Class(middle_left, left_[7], mc_[8], fs_[1f]), ref _treeBoundsMinX)
                ])),
                new UICol(Class(h_[25], w_[31f], blank_sharp_g_[10], top_center),Sub([
                    newField("0", Class(middle_left, left_[7], mc_[8], fs_[1f]), ref _treeBoundsMinY)
                ])),
                new UICol(Class(h_[25], w_[31f], blank_sharp_g_[10], top_right), Sub([
                    newField("0", Class(middle_left, left_[7], mc_[8], fs_[1f]), ref _treeBoundsMinZ)
                ])),
            ])),
            new UICol(Class(h_[20], w_full_minus_[10], top_center), Sub([
                new UIText("Max", Class(fs_[1f], middle_left))
            ])),
            new UICol(Class(w_full_minus_[10], h_[30], top_center), Sub([
                new UICol(Class(h_[25], w_[31f], blank_sharp_g_[10], top_left), Sub([
                    newField("0", Class(middle_left, left_[7], mc_[8], fs_[1f]), ref _treeBoundsMaxX)
                ])),
                new UICol(Class(h_[25], w_[31f], blank_sharp_g_[10], top_center), Sub([
                    newField("0", Class(middle_left, left_[7], mc_[8], fs_[1f]), ref _treeBoundsMaxY)
                ])),
                new UICol(Class(h_[25], w_[31f], blank_sharp_g_[10], top_right), Sub([
                    newField("0", Class(middle_left, left_[7], mc_[8], fs_[1f]), ref _treeBoundsMaxZ)
                ])),
            ])),
            new UIImg(Class(h_[2], w_full, blank_full_g_[0.3f])),
            new UICol(Class(h_[25], w_full_minus_[10], top_center), Sub([
                new UIText("ANALYSER", Class(fs_[1.2f], middle_left)),
            ])),
            new UICol(Class(h_[25], w_full_minus_[10], top_center), Sub([
                new UIText("Count", Class(fs_[1f], middle_left))
            ])),
            new UICol(Class(h_[25], w_full_minus_[10], top_center, blank_sharp_g_[10]),
            Sub([
                newField("100", Class(middle_left, left_[7], mc_[8], fs_[1f]), ref _treeAnalyserCount)
            ])),
            new UICol(Class(h_[25], w_full_minus_[10], top_center, blank_sharp_g_[30], top_[5]),
            OnClickCol(c => AnalyseTree()), Sub([
                new UIText("Analyse", Class(mc_[7], fs_[1.2f], middle_left, left_[7])),
            ])),
            newImg(Class(h_[20], w_full_minus_[10], top_center, blank_sharp, rgba_[1, 0, 0, 1], slice_null, top_[5]), ref TreeAnalyserLoadingBar),
        ])),
        new UIVCol(Class(w_full_minus_[10], top_center, grow_children, spacing_[5]), Sub([
            new UICol(Class(h_[25], w_full, top_center), Sub([
                new UIText("FILE", Class(fs_[1.2f], middle_left)),
            ])),
            new UICol(Class(h_[25], w_full, blank_sharp_g_[10], top_center), Sub([
                newField("", Class(middle_left, left_[7], mc_[20], fs_[1f]), ref _treeFileName)
            ])),
            new UICol(Class(h_[25], w_full, top_center), Sub([
                new UICol(Class(h_full, w_[49f], blank_sharp_g_[25], middle_left), OnClickCol(_ => SaveTree()), Sub(
                    new UIText("Save", Class(middle_center))
                )),
                new UICol(Class(h_full, w_[49f], blank_sharp_g_[25], middle_right), OnClickCol(_ => LoadTree()), Sub(
                    new UIText("Load", Class(middle_center))
                ))
            ])),
        ]))
    ]), ref _leftTreePanel);


    private UIElementBase RightTreePanel() =>
    newVScroll(Class(w_full_minus_[2], h_full, blank_full_g_[20], top_right, spacing_[5], allow_scrolling_to_top, scroll_speed_[10f], mask_children), Sub([
        TreeSections(
            TreeSection("BASE",
                TreeField("Seed", (0, 0, int.MaxValue, 1), ref _treeSeedField)
            ),
            new UIImg(Class(w_full, h_[2], blank_full_g_[0.3f])),
            TreeSection("TRUNK",
                TreeField("Count", (3, 1, 100, 1), ref _treeTrunkCountField),
                TreeField("Height", (15, 0f, 1000f, 0.1f), (25, 0f, 1000f, 0.1f), ref _treeTrunkHeightMinField, ref _treeTrunkHeightMaxField),
                TreeField("Split", (0.9f, 0f, 20f, 0.01f), (1.8f, 0f, 20f, 0.01f), ref _treeTrunkSplitMinField, ref _treeTrunkSplitMaxField),
                TreeField("Thickness", (1.3f, 0f, 100f, 0.01f), (0.6f, 0f, 100f, 0.01f), ref _treeTrunkThicknessMinField, ref _treeTrunkThicknessMaxField)
            ),
            new UIImg(Class(w_full, h_[2], blank_full_g_[0.3f])),
            TreeSection("TILT",
                TreeField("X Axis", (-0.5f, -10f, 10f, 0.01f), (0.5f, -10f, 10f, 0.01f), ref _treeTiltFactorXMinField, ref _treeTiltFactorXMaxField),
                TreeField("Y Axis", (-0.5f, -10f, 10f, 0.01f), (0.5f, -10f, 10f, 0.01f), ref _treeTiltFactorYMinField, ref _treeTiltFactorYMaxField)
            ),
            new UIImg(Class(w_full, h_[2], blank_full_g_[0.3f])),
            TreeSection("BRANCHES",
                TreeField("Count", (5, 0, 100, 1), (7, 0, 100, 1), ref _treeBranchCountMinField, ref _treeBranchCountMaxField),
                TreeField("Position Variance", (0.2f, 0f, 1f, 0.01f), ref _treeBranchPositionVarianceField),
                TreeField("Length", (3f, 0f, 500f, 0.1f), (5f, 0f, 500f, 0.1f), ref _treeBranchLengthMinField, ref _treeBranchLengthMaxField),
                TreeField("Falloff", (0.3f, 0f, 1f, 0.01f), ref _treeBranchLengthFalloffField),
                TreeField("Thickness", (0.6f, 0f, 50f, 0.01f), (0.6f, 0f, 50f, 0.01f), ref _treeBranchThicknessMinField, ref _treeBranchThicknessMaxField),
                TreeField("First Trunk", (1, 1, 100, 1), (1, 1, 100, 1), ref _treeBranchFirstTrunkMinField, ref _treeBranchFirstTrunkMaxField),
                TreeField("Trunk Start", (0.2f, 0f, 1f, 0.01f), (1f, 0f, 1f, 0.01f), ref _treeBranchTrunkStartField, ref _treeBranchTrunkEndField),
                TreeField("Angle", (0f, 0f, 360f, 1f), (360f, 0f, 360f, 1f), ref _treeBranchAngleMinField, ref _treeBranchAngleMaxField),
                TreeField("Tilt", (0f, -90f, 90f, 1f), (0f, -90f, 90f, 1f), ref _treeBranchTiltMinField, ref _treeBranchTiltMaxField)
            ),
            new UIImg(Class(w_full, h_[2], blank_full_g_[0.3f])),
            TreeSection("LEAVES",
                TreeOptions("Cluster Type", ["Sphere", "Cube", "Cone", "Cylinder"], 0, i => _leavesTypeIndex = i),
                TreeToggle("Follow Branch Direction", false, b => _leavesFollowBranchDirection = b, ref _leavesFollowBranchDirectionButton),
                TreeField("Radius", (2f, 0f, 100f, 0.1f), (3f, 0f, 100f, 0.1f), ref _leavesRadiusMinField, ref _leavesRadiusMaxField),
                TreeField("Height", (2f, 0f, 100f, 0.1f), (3f, 0f, 100f, 0.1f), ref _leavesHeightMinField, ref _leavesHeightMaxField),
                TreeField("Position", (0.7f, 0f, 1f, 0.01f), (0.7f, 0f, 1f, 0.01f), ref _leavesPositionMinField, ref _leavesPositionMaxField),
                TreeField("Count", (1, 0, 50, 1), (3, 0, 50, 1), ref _leavesCountMinField, ref _leavesCountMaxField),
                TreeField("Density", (0.5f, 0f, 1f, 0.01f), ref _leavesDensityField),
                TreeField("Falloff", (0.3f, 0f, 1f, 0.01f), ref _leavesFalloffField), 
                TreeField("Scale X", (1f, 0.1f, 100f, 0.01f), (1f, 0.1f, 100f, 0.01f), ref _leavesScaleXMinField, ref _leavesScaleXMaxField),
                TreeField("Scale Y", (1f, 0.1f, 100f, 0.01f), (1f, 0.1f, 100f, 0.01f), ref _leavesScaleYMinField, ref _leavesScaleYMaxField),
                TreeField("Scale Z", (1f, 0.1f, 100f, 0.01f), (1f, 0.1f, 100f, 0.01f), ref _leavesScaleZMinField, ref _leavesScaleZMaxField)  
            )
        ),
    ]), ref _rightTreePanel);
}