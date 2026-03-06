using PBG.UI.Creator;
using PBG.UI;
using PBG.Core;
using static PBG.UI.Styles;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.MathLibrary;
using PBG.Data;
using PBG.MathLibrary;
using PBG;
using PBG.Threads;
using Newtonsoft.Json;

public class StructureNodeUI(StructureNodeManager nodeManager) : UIScript
{
    public List<UIElementBase> TreeElements;
    public List<UIElementBase> NoiseElements;
    public List<UIElementBase> StructureElements;

    public UIVCol NoiseNodesPanel = null!;
    public UIVScroll SidePanelFileList = null!;

    // Tree
    public UIVCol LeftTreePanel = null!;
    public UIVScroll RightTreePanel = null!;
    public UICol CenterPanel = null!;

    // Noise
    public UIVCol LeftNoisePanel = null!;
    public UICol RightNoisePanel = null!;
    public UIVCol NoisePaletteCollection = null!;
    public UIVScroll NoisePaletteBlockSelection = null!;

    // Structure
    public UIElementBase LeftStructurePanel = null!;
    public UIElementBase RightStructurePanel = null!;






    public static UIField GroupInputName = null!;
    public static UIVCol GroupInputSettings = null!;
    public static UICol? CurrentGroupInputType = null;
    public static UICol GroupFloatButton = null!;
    public static UICol GroupIntButton = null!;
    public static UICol GrouPBGector2Button = null!;
    public static UICol GrouPBGector2iButton = null!;
    public static UICol GrouPBGector3Button = null!;
    public static UICol GrouPBGector3iButton = null!;
    public static UICol GrouPBGalueIndex0 = null!;
    public static UICol GrouPBGalueIndex1 = null!;
    public static UICol GrouPBGalueIndex2 = null!;




    // Point node position
    public UIVCol PointNodePaletteCollection = null!;
    public UIVScroll PointNodePaletteBlockSelection = null!;

    public UIText _fpsText = null!;
    public UIText _ramText = null!;

    // Base
    private UIField _treeSeedField = null!;
    
    // Trunk
    private UIField _treeTrunkCountField = null!;
    private UIField _treeTrunkHeightMinField = null!;
    private UIField _treeTrunkHeightMaxField = null!;
    private UIField _treeTrunkSplitMinField = null!;
    private UIField _treeTrunkSplitMaxField = null!;
    private UIField _treeTrunkThicknessMinField = null!;
    private UIField _treeTrunkThicknessMaxField = null!;

    // Tilt
    private UIField _treeTiltFactorXMinField = null!;
    private UIField _treeTiltFactorXMaxField = null!;
    private UIField _treeTiltFactorYMinField = null!;
    private UIField _treeTiltFactorYMaxField = null!;

    // Branches
    private UIField _treeBranchCountMinField = null!;
    private UIField _treeBranchCountMaxField = null!;
    private UIField _treeBranchPositionVarianceField = null!;
    private UIField _treeBranchLengthMinField = null!;
    private UIField _treeBranchLengthMaxField = null!;
    private UIField _treeBranchLengthFalloffField = null!;
    private UIField _treeBranchThicknessMinField = null!;
    private UIField _treeBranchThicknessMaxField = null!;
    private UIField _treeBranchFirstTrunkMinField = null!;
    private UIField _treeBranchFirstTrunkMaxField = null!;
    private UIField _treeBranchTrunkStartField = null!;
    private UIField _treeBranchTrunkEndField = null!;
    private UIField _treeBranchAngleMinField = null!;
    private UIField _treeBranchAngleMaxField = null!;
    private UIField _treeBranchTiltMinField = null!;
    private UIField _treeBranchTiltMaxField = null!;

    // Leaves
    private int _leavesTypeIndex = 0;
    private bool _leavesFollowBranchDirection = false;
    private UICol _leavesFollowBranchDirectionButton = null!;
    private UIField _leavesRadiusMinField = null!;
    private UIField _leavesRadiusMaxField = null!;
    private UIField _leavesHeightMinField = null!;
    private UIField _leavesHeightMaxField = null!;
    private UIField _leavesPositionMinField = null!;
    private UIField _leavesPositionMaxField = null!;
    private UIField _leavesCountMinField = null!;
    private UIField _leavesCountMaxField = null!;
    private UIField _leavesDensityField = null!;
    private UIField _leavesFalloffField = null!;
    private UIField _leavesScaleXMinField = null!;
    private UIField _leavesScaleXMaxField = null!;
    private UIField _leavesScaleYMinField = null!;
    private UIField _leavesScaleYMaxField = null!;
    private UIField _leavesScaleZMinField = null!;
    private UIField _leavesScaleZMaxField = null!;

    // Analyser
    private UIField _treeAnalyserCount = null!;
    public UIImg TreeAnalyserLoadingBar = null!;

    private UIField _treeBoundsMinX = null!;
    private UIField _treeBoundsMinY = null!;
    private UIField _treeBoundsMinZ = null!;

    private UIField _treeBoundsMaxX = null!;
    private UIField _treeBoundsMaxY = null!;
    private UIField _treeBoundsMaxZ = null!;

    private UIField _treeFileName = null!;

    public override void PreScript()
    {
        RightStructurePanel = nodeManager.StructureEditor.GetRightPanel();
        LeftStructurePanel = nodeManager.StructureEditor.GetLeftPanel();
    }

    public override void AfterScript()
    {
        TreeElements = [LeftTreePanel, CenterPanel, RightTreePanel];
        NoiseElements = [LeftNoisePanel, RightNoisePanel];
        StructureElements = [LeftStructurePanel, RightStructurePanel, CenterPanel];
    }


    public override UIElementBase Script() =>
    new UICol(Class(w_full, h_full), Sub([
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
        ])),
        newCol(Class(w_[240], h_full_minus_[60], bottom_left, blank_full_g_[30]), Sub([
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
                    OnClickCol(c => {
                        int count = _treeAnalyserCount.GetInt();
                        Action<int> loading = (i) => {
                            nodeManager.TreeUpdateAnalyser = true;
                            nodeManager.TreeAnalyserProgress = (float)i / (float)count;
                        };
                        Action<Vector3i, Vector3i> finished = (a, b) => {
                            _treeBoundsMinX.SetText($"{a.X}").UpdateCharacters();
                            _treeBoundsMinY.SetText($"{a.Y}").UpdateCharacters();
                            _treeBoundsMinZ.SetText($"{a.Z}").UpdateCharacters();
                            _treeBoundsMaxX.SetText($"{b.X}").UpdateCharacters();
                            _treeBoundsMaxY.SetText($"{b.Y}").UpdateCharacters();
                            _treeBoundsMaxZ.SetText($"{b.Z}").UpdateCharacters();
                        };
                        var process = new StructureTreeBoundingBoxAnalyser((0, 0, 0), GetCurrentTreeInfo(), count, loading, finished);
                        TaskPool.QueueAction(process);
                    }),
                    Sub([
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
                        new UICol(Class(h_full, w_[49f], blank_sharp_g_[25], middle_left), OnClickCol(_ => {
                            if (_treeFileName.GetTrimmedText().Length == 0)
                                return;

                            JsonSerializerSettings settings = new()
                            {
                                TypeNameHandling = TypeNameHandling.Auto
                            };

                            var json = JsonConvert.SerializeObject(GetCurrentTreeInfo(), Formatting.Indented, settings);
                            File.WriteAllText(Path.Combine(Game.CustomPath, "trees", _treeFileName.GetTrimmedText() + ".json"), json);
                        }), 
                        Sub(
                            new UIText("Save", Class(middle_center))
                        )),
                        new UICol(Class(h_full, w_[49f], blank_sharp_g_[25], middle_right), OnClickCol(_ => {
                            var path = Path.Combine(Game.CustomPath, "trees", _treeFileName.GetTrimmedText() + ".json");
                            if (!File.Exists(path))
                                return;

                            JsonSerializerSettings settings = new()
                            {
                                TypeNameHandling = TypeNameHandling.Auto
                            };

                            string json = File.ReadAllText(path);
                            TreeGenerationInfo? data = JsonConvert.DeserializeObject<TreeGenerationInfo>(json, settings);
                            if (data == null)
                                return;

                            SetTreeInfo(data);
                            nodeManager.RegenerateTree();
                        }), 
                        Sub(
                            new UIText("Load", Class(middle_center))
                        ))
                    ])),
                ]))
            ]), ref LeftTreePanel),
            newVCol(Class(w_full_minus_[2], h_full, blank_full_g_[20], top_left, invisible), Sub([
                newVCol(Class(w_full, grow_children, top_center, spacing_[5], blank_sharp_g_[15]),
                    Sub([
                    newVScroll("in_scroll", Class(w_full_minus_[4], h_[296], top_center, spacing_[2], border_[2, 2, 2, 2], mask_children), Sub([
                        ..Foreach(ItemDataManager.AllItems, (name, block) => {
                            if (block is not BlockItemData) return null;
                            return new UICol(Class(w_full, top_center, h_[38], blank_sharp_g_[30], data_["block", name]),
                                OnClick(nodeManager.dragBlockUI.DragBlockStart),
                                Sub([
                                    new UIImg(Class(middle_left, h_[38], w_[38], item_[name], bg_white)),
                                    new UIText(name, Class(mc_[name.Length], fs_[1], middle_left, left_[40]))
                                ])
                            ); 
                        }),
                    ]), ref NoisePaletteBlockSelection),
                ]), ref NoisePaletteCollection),
                newVCol(Class(w_full_minus_[10], grow_children, border_[5, 5, 5, 5], spacing_[5], ignore_invisible, not_toggle_old_invisible), Sub([
                    new UICol(Class(w_full, h_[20]), Sub(
                        new UIText("Group input settings", Class(mc_[20], fs_[1], middle_left))
                    )),
                    new UICol(Class(w_full, h_[30], blank_sharp_g_[10]), Sub(
                        newField("", Class(mc_[18], fs_[1], middle_left, left_[5]),
                        OnTextChange(StructureNodeManager.SetGroupFieldNameCall),
                        ref GroupInputName)
                    )),
                    new UICol(Class(w_full, h_[30], blank_sharp_g_[40]),
                    OnClickCol(_ => {
                        StructureNodeManager.GroupRemoveFieldCall(GroupInputName.GetText());
                        GroupInputSettings.SetVisible(false);
                    }),
                    Sub(
                        new UIText("Delete", Class(mc_[6], fs_[1], middle_center))
                    )),
                    new UIVCol("values", Class(w_full, grow_children, spacing_[5]), Sub([
                        new UICol(Class(w_full, h_[30]), Sub([
                            newCol(Class(w_[32f], h_[30], blank_sharp_g_[40]),
                            OnClickCol(_ => { if (StructureNodeManager.GroupInputField != null) StructureNodeManager.SetGroupFieldTypeCall(new NodeValue_Float(StructureNodeManager.GroupInputField.Node, 0f)); ResetGroupInputValues("float", 1, [0]); }),
                            Sub(
                                new UIText("float", Class(mc_[5], fs_[1], middle_center))
                            ), ref GroupFloatButton),
                            newCol(Class(w_[32f], h_[30], blank_sharp_g_[40], top_center),
                            OnClickCol(_ => { if (StructureNodeManager.GroupInputField != null) StructureNodeManager.SetGroupFieldTypeCall(new NodeValue_Int(StructureNodeManager.GroupInputField.Node, 0)); ResetGroupInputValues("int", 1, [0]); }),
                            Sub(
                                new UIText("int", Class(mc_[3], fs_[1], middle_center))
                            ), ref GroupIntButton),
                            newCol(Class(w_[32f], h_[30], blank_sharp_g_[40], top_right),
                            OnClickCol(_ => { if (StructureNodeManager.GroupInputField != null) StructureNodeManager.SetGroupFieldTypeCall(new NodeValue_Vector2(StructureNodeManager.GroupInputField.Node, 0, 0)); ResetGroupInputValues("vec2", 2, [0, 0]); }),
                            Sub(
                                new UIText("vec2", Class(mc_[4], fs_[1], middle_center))
                            ), ref GrouPBGector2Button)
                        ])),
                        new UICol(Class(w_full, h_[30]), Sub([
                            newCol(Class(w_[32f], h_[30], blank_sharp_g_[40]),
                            OnClickCol(_ => { if (StructureNodeManager.GroupInputField != null) StructureNodeManager.SetGroupFieldTypeCall(new NodeValue_Vector2Int(StructureNodeManager.GroupInputField.Node, 0, 0)); ResetGroupInputValues("ivec2", 2, [0, 0]); }),
                            Sub(
                                new UIText("ivec2", Class(mc_[5], fs_[1], middle_center))
                            ), ref GrouPBGector2iButton),
                            newCol(Class(w_[32f], h_[30], blank_sharp_g_[40], top_center),
                            OnClickCol(_ => { if (StructureNodeManager.GroupInputField != null) StructureNodeManager.SetGroupFieldTypeCall(new NodeValue_Vector3(StructureNodeManager.GroupInputField.Node, 0, 0, 0)); ResetGroupInputValues("vec3", 3, [0, 0, 0]); }),
                            Sub(
                                new UIText("vec3", Class(mc_[4], fs_[1], middle_center))
                            ), ref GrouPBGector3Button),
                            newCol(Class(w_[32f], h_[30], blank_sharp_g_[40], top_right),
                            OnClickCol(_ => { if (StructureNodeManager.GroupInputField != null) StructureNodeManager.SetGroupFieldTypeCall(new NodeValue_Vector3Int(StructureNodeManager.GroupInputField.Node, 0, 0, 0)); ResetGroupInputValues("ivec3", 3, [0, 0, 0]); }),
                            Sub(
                                new UIText("ivec3", Class(mc_[5], fs_[1], middle_center))
                            ), ref GrouPBGector3iButton)
                        ])),
                        newCol(Class(w_full, h_[30], blank_sharp_g_[10]), Sub(
                            new UIField("0", Class(mc_[20], fs_[1], middle_left, left_[5]),
                            OnHoldField(i => StructureNodeManager.GroupInputField?.Value.SetSlideValue(i, 0)),
                            OnTextChange(i => StructureNodeManager.GroupInputField?.Value.UpdateValue(0, i.GetFloat())))
                        ), ref GrouPBGalueIndex0),
                        newCol(Class(w_full, h_[30], blank_sharp_g_[10]), Sub(
                            new UIField("0", Class(mc_[20], fs_[1], middle_left, left_[5]),
                            OnHoldField(i => StructureNodeManager.GroupInputField?.Value.SetSlideValue(i, 1)),
                            OnTextChange(i => StructureNodeManager.GroupInputField?.Value.UpdateValue(1, i.GetFloat())))
                        ), ref GrouPBGalueIndex1),
                        newCol(Class(w_full, h_[30], blank_sharp_g_[10]), Sub(
                            new UIField("0", Class(mc_[20], fs_[1], middle_left, left_[5]),
                            OnHoldField(i => StructureNodeManager.GroupInputField?.Value.SetSlideValue(i, 2)),
                            OnTextChange(i => StructureNodeManager.GroupInputField?.Value.UpdateValue(2, i.GetFloat())))
                        ), ref GrouPBGalueIndex2),
                    ]))
                ]), ref GroupInputSettings)
            ]), ref LeftNoisePanel),
            LeftStructurePanel
            
        ]), ref nodeManager.LeftPanelCollection),
        newCol(Class(w_minus_[100, 500], h_minus_[100, 60], bottom_left, left_[250]), OnClick(nodeManager.CenterOnClick), Sub(
            newText("0", Class(mc_[20], fs_[1], bottom_left, left_[5], bottom_[20]), ref _fpsText),
            newText("0", Class(mc_[20], fs_[1], bottom_left, left_[5], bottom_[5]), ref _ramText),
            new UIImg(Class(w_[30], h_[30], middle_center, icon_[16], gray_[50]))
        ), ref CenterPanel),
        new UICol(Class(w_[240], h_full_minus_[60], bottom_right, blank_full_g_[30]), Sub([
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
            ]), ref RightTreePanel),
            newCol(Class(w_full_minus_[2], h_full, blank_full_g_[20], top_right, invisible, mask_children), Sub([
                new UICol(Class(h_[30], w_full_minus_[10], top_[5], top_center), Sub([
                    new UICol(Class(h_[30], w_half_minus_[2], blank_sharp_g_[30]),
                    OnClickCol(_ => {
                        var text = NoiseNodesPanel.GetElement<UICol>()?.GetElement<UIText>();
                        if (text == null) return;
                        if (text.GetTrimmedText() != "Basic")
                        {
                            text.UpdateText("Basic");
                            nodeManager.NodeType = "Basic";
                            RegenerateNodeList();
                            NodeManager.NodeEditorType = NodeEditorType.Node;
                            NodeManager.Clear();
                        }
                    }),
                    Sub([
                        new UIText("Basic", Class(middle_center, mc_[5], fs_[1f]))
                    ])),
                    new UICol(Class(h_[30], w_half_minus_[2], blank_sharp_g_[30], top_right),
                    OnClickCol(_ => {
                        var text = NoiseNodesPanel.GetElement<UICol>()?.GetElement<UIText>();
                        if (text == null) return;
                        if (text.GetTrimmedText() != "Group")
                        {
                            text.UpdateText("Group");
                            nodeManager.NodeType = "Group";
                            RegenerateGroupList();
                            NodeManager.NodeEditorType = NodeEditorType.Group;
                            NodeManager.Clear();
                            var inputNode = new GroupInputNode(null, NodeManager.NodeCollection, (0, 100), [], []);
                            var outputNode = new GroupOutputNode(NodeManager.NodeCollection, (800, 100), []);
                            NodeManager.AddNode(inputNode);
                            NodeManager.AddNode(outputNode);
                        }
                    }),
                    Sub([
                        new UIText("Group", Class(middle_center, mc_[5], fs_[1f]))
                    ]))
                ])),
                newVCol(Class(w_full, h_full, border_[5, 5, 5, 5], spacing_[5], top_[35]), Sub([
                    new UICol(Class(h_[20], w_full), Sub([
                        new UIText("Basic", Class(mc_[5], middle_left, fs_[1.2f])),
                    ])),
                    new UICol("file-name-collection", Class(w_full_minus_[10], h_[30], blank_sharp_g_[10]), Sub([
                        newField("Base", Class(middle_left, left_[5], mc_[25], fs_[1]), OnTextChange(SetName), ref NodeManager.FileNameInputField),
                    ])),
                    new UICol(Class(w_full_minus_[10], h_[30]), Sub([
                        new UICol("save-collection", Class(w_half_minus_[2], h_full, blank_sharp_g_[30]), OnClickCol(_ => {
                            if (nodeManager.NodeType == "Basic")
                            {
                                int oldFileCount = NodeManager.GetCurrentNodeCount();
                                NodeManager.Save();
                                int newFileCount = NodeManager.GetCurrentNodeCount();
                                if (newFileCount != oldFileCount)
                                {
                                    RegenerateNodeList();
                                }
                            }
                            else if (nodeManager.NodeType == "Group")
                            {
                                int oldFileCount = NodeManager.GetCurrentGroupCount();
                                NodeManager.SaveGroup();
                                int newFileCount = NodeManager.GetCurrentGroupCount();
                                if (newFileCount != oldFileCount)
                                {
                                    RegenerateGroupList();
                                    nodeManager.NodeSelector.RegenerateGroupList();
                                }
                            }
                        }), Sub([
                            new UIText("Save", Class(middle_center, mc_[4], fs_[1]))
                        ])),
                        new UICol("load-collection", Class(w_half_minus_[2], h_full, blank_sharp_g_[30], top_right), OnClickCol(_ => {
                            if (nodeManager.NodeType == "Basic")
                            {
                                NodeManager.Load();
                            }
                            else if (nodeManager.NodeType == "Group")
                            {
                                NodeManager.LoadGroup();
                            }
                        }), Sub([
                            new UIText("Load", Class(middle_center, mc_[4], fs_[1]))
                        ]))
                    ])),
                    newVScroll(Class(w_full_minus_[10], h_full_minus_[375], blank_sharp_g_[10], border_bottom_[5], mask_children), Sub([
                        ..Run(GenerateBasicElements)
                    ]), ref SidePanelFileList)
                ]), ref NoiseNodesPanel),
            ]), ref RightNoisePanel),
            RightStructurePanel
        ]))
    ]));

    public void SetName(UIField field) => NodeManager.FileName = field.GetTrimmedText();


    public void Update()
    {
        if (GameTime.FpsUpdated)
        {
            _fpsText.UpdateText("fps: " + GameTime.Fps);
            _ramText.UpdateText("ram: " + GameTime.Ram / (1024 * 1024) + " Mb");
        }
    }


    public void SlideValue(UIField? field, float min, float max, float increment)
    {
        float delta = Input.GetMouseDelta().X;
        if (delta == 0 || field == null)
        {
            return;
        }
        float value = field.GetFloat();
        float oldValue = value;
        value += increment * delta;
        value = Mathf.Clampy(value, min, max);

        if (value != oldValue)
        {
            field.SetText($"{value}").UpdateCharacters();
            nodeManager.TreeSettingsChanged = true;
        } 
    }



    private UIElementBase[] GenerateBasicElements()
    {
        List<UIElementBase> fileCollections = [];
        var files = Directory.GetFiles(Path.Combine(Game.MainPath, "custom", "nodes"));
        // if file has .ProjectVoxeln extension create and add it to the list
        foreach (var file in files)
        {
            if (Path.GetExtension(file) == ".json")
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                fileCollections.Add(
                    new UICol("file-element", Class(left_[5], top_[5], w_full_minus_[10], h_[30], blank_sharp_g_[20]),
                    OnClickCol(_ => {
                        NodeManager.SetName(fileName);
                        NodeManager.Load();
                    }), Sub([
                        new UIText(fileName.Length > 25 ? fileName[..25] : fileName, Class(middle_left, left_[5], mc_[Mathf.Min(fileName.Length, 25)], fs_[1])),
                        new UIText("X", Class(middle_right, right_[5], mc_[1], fs_[1.2f]), OnClickText(_ => NodeManager.DeleteFile(file)))
                    ]))
                );
            }
        }
        return [.. fileCollections];
    }

    public void RegenerateGroupList()
    {
        SidePanelFileList.DeleteChildren();
        var fileElements = GenerateGroupElements();
        SidePanelFileList.AddElements(fileElements);
        UIController.AddElements(fileElements);
    }

    public void RegenerateNodeList()
    {
        SidePanelFileList.DeleteChildren();
        var fileElements = GenerateBasicElements();
        SidePanelFileList.AddElements(fileElements);
        UIController.AddElements(fileElements);
    }

    private UIElementBase[] GenerateGroupElements()
    {
        List<UIElementBase> fileCollections = [];
        var files = Directory.GetFiles(Path.Combine(Game.MainPath, "custom", "groups"));
        // if file has .ProjectVoxeln extension create and add it to the list
        foreach (var file in files)
        {
            if (Path.GetExtension(file) == ".json")
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                fileCollections.Add(
                    new UICol("file-element", Class(left_[5], top_[5], w_full_minus_[10], h_[30], blank_sharp_g_[20]),
                    OnClickCol(_ =>
                    {
                        NodeManager.SetName(fileName);
                        NodeManager.LoadGroup();
                    }), Sub([
                        new UIText(fileName.Length > 25 ? fileName[..25] : fileName, Class(middle_left, left_[5], mc_[Mathf.Min(fileName.Length, 25)], fs_[1])),
                        new UIText("X", Class(middle_right, right_[5], mc_[1], fs_[1.2f]), OnClickText(_ => NodeManager.DeleteFile(file)))
                    ]))
                );
            }
        }
        return [.. fileCollections];
    }



    public UIElementBase TreeSections(params UIElementBase[] sections) => 
    new UIVCol(Class(spacing_[5], w_full, top_right, spacing_[5], grow_children), Sub(sections));

    public UIElementBase TreeSection(string title, params UIElementBase[] contents) => 
    new UIVCol(Class(grow_children, w_full_minus_[10], top_center), Sub([
        new UICol(Class(h_[25], w_full), Sub([
            new UIText(title, Class(mc_[title.Length], fs_[1.2f], middle_left)),
        ])),
        ..contents,
    ]));

    public UIElementBase TreeField(string label, Vector4 data, ref UIField fieldRef) =>
    new UIVCol(Class(grow_children, w_full), Sub([
        new UICol(Class(h_[25], w_full), Sub([
            new UIText(label, Class(mc_[label.Length], fs_[1f], middle_left))
        ])),
        new UICol(Class(h_[25], w_full, blank_sharp_g_[10]),
        OnHoldCol(c => SlideValue(c.GetElement<UIField>(), data.Y, data.Z, data.W)),
        Sub([
            newField(""+data.X, Class(middle_left, left_[7], mc_[8], fs_[1f]), OnTextChange(_ => nodeManager.TreeSettingsChanged = true), ref fieldRef)
        ]))
    ]));

    public UIElementBase TreeField(string label, Vector4 data1, Vector4 data2, ref UIField fieldRef1, ref UIField fieldRef2) =>
    new UIVCol(Class(grow_children, w_full), Sub([
        new UICol(Class(h_[25], w_full), Sub([
            new UIText(label, Class(mc_[label.Length], fs_[1f], middle_left))
        ])),
        new UICol(Class(h_[25], w_full), Sub([
            new UICol(Class(h_[25], w_half_minus_[2], blank_sharp_g_[10]),
            OnHoldCol(c => SlideValue(c.GetElement<UIField>(), data1.Y, data1.Z, data1.W)),
            Sub([
                newField(""+data1.X, Class(middle_left, left_[7], mc_[8], fs_[1f]), OnTextChange(_ => nodeManager.TreeSettingsChanged = true), ref fieldRef1)
            ])),
            new UICol(Class(h_[25], w_half_minus_[2], blank_sharp_g_[10], top_right),
            OnHoldCol(c => SlideValue(c.GetElement<UIField>(), data2.Y, data2.Z, data2.W)),
            Sub([
                newField(""+data2.X, Class(middle_left, left_[7], mc_[8], fs_[1f]), OnTextChange(_ => nodeManager.TreeSettingsChanged = true), ref fieldRef2)
            ]))
        ]))
    ]));

    public UIElementBase TreeOptions(string label, string[] options, int selectedIndex, Action<int> onSelect)
    {
        var col = new UIVCol(Class(grow_children, w_full), Sub([
            new UICol(Class(h_[25], w_full), Sub([
                new UIText(label, Class(mc_[label.Length], fs_[1f], middle_left))
            ]))
        ]));
        onSelect(selectedIndex);
        List<UIElementBase> rows = [];
        for (int i = 0; i < options.Length; i += 3)
        {
            List<UIElementBase> rowOptions = [];
            for (int j = i; j < i + 3 && j < options.Length; j++)
            {
                var option = new UICol(Class(w_[32f], h_[30], blank_sharp_g_[j == selectedIndex ? 40 : 30], _topAlignment[j % 3], data_["option_index", j]),
                OnClickCol(c => {
                    var oldC = col.Dataset.Get<UICol>("selected");
                    if (oldC != null && oldC != c)
                    {
                        oldC.UpdateColor((0.3f, 0.3f, 0.3f, 1f));
                        c.UpdateColor((0.4f, 0.4f, 0.4f, 1f));
                        col.Dataset["selected"] = c;
                        onSelect(c.Dataset.Int("option_index"));
                        nodeManager.TreeSettingsChanged = true;
                    }
                }),
                Sub(
                    new UIText(options[j], Class(mc_[options[j].Length], fs_[1], middle_center))
                ));
                rowOptions.Add(option);
                if (j == selectedIndex)
                {
                    col.Dataset["selected"] = option;
                }
            }
            rows.Add(new UICol(Class(h_[30], w_full, i != 0 ? top_[5] : top_[0]), [..rowOptions]));
        }
        col.AddElements(rows);
        return col;
    }

    public UIElementBase TreeToggle(string label, bool state, Action<bool> onToggle, ref UICol col)
    {
        col = new UICol(Class(h_[25], w_full), Sub(
            new UIText(label, Class(mc_[label.Length], fs_[1f], middle_left)),
            new UIImg(Class(w_[20], h_[20], top_right, right_[2.5f], blank_sharp_g_[state ? 30 : 10], data_["state", state]), OnClickImg(img =>
            {
                bool s = img.Dataset.Bool("state");
                s = !s;
                img.Dataset["state"] = s;
                img.UpdateColor(new Vector4(new Vector3(s ? 0.3f : 0.1f), 1f));
                onToggle(s);
                nodeManager.TreeSettingsChanged = true;
            }))
        ));
        return col;
    }

    private static readonly UIStyleData[] _topAlignment = [top_left, top_center, top_right];

    public static void ResetGroupInputValues(string type, int count, float[] values)
    {
        CurrentGroupInputType?.UpdateColor((0.4f, 0.4f, 0.4f, 1f));
        CurrentGroupInputType = type switch
        {
            "float" => GroupFloatButton,
            "int" => GroupIntButton,
            "vec2" => GrouPBGector2Button,
            "ivec2" => GrouPBGector2iButton,
            "vec3" => GrouPBGector3Button,
            "ivec3" => GrouPBGector3iButton,
            _ => GroupFloatButton
        };
        CurrentGroupInputType?.UpdateColor((0.5f, 0.5f, 0.5f, 1f));
        GrouPBGalueIndex0.SetVisible(count >= 1);
        GrouPBGalueIndex1.SetVisible(count >= 2);
        GrouPBGalueIndex2.SetVisible(count >= 3);
        if (count >= 1) GrouPBGalueIndex0.GetElement<UIField>()?.UpdateText(values[0]+"");
        if (count >= 2) GrouPBGalueIndex1.GetElement<UIField>()?.UpdateText(values[1]+"");
        if (count >= 3) GrouPBGalueIndex2.GetElement<UIField>()?.UpdateText(values[2]+"");
        GroupInputSettings.ApplyChanges(UIChange.Scale);
    }



    public TreeGenerationInfo GetCurrentTreeInfo()
    {
        var info = new TreeGenerationInfo()
        {
            MinX = _treeBoundsMinX.GetInt(),
            MinY = _treeBoundsMinY.GetInt(),
            MinZ = _treeBoundsMinZ.GetInt(),

            MaxX = _treeBoundsMaxX.GetInt(),
            MaxY = _treeBoundsMaxY.GetInt(),
            MaxZ = _treeBoundsMaxZ.GetInt(),

            Seed = (uint)_treeSeedField.GetInt(),
            Count = Math.Max(1, _treeTrunkCountField.GetInt()),

            HeightMin = _treeTrunkHeightMinField.GetFloat(),
            HeightMax = _treeTrunkHeightMaxField.GetFloat(),

            SplitMin = _treeTrunkSplitMinField.GetFloat(),
            SplitMax = _treeTrunkSplitMaxField.GetFloat(),

            ThicknessStart = _treeTrunkThicknessMinField.GetFloat(),
            ThicknessEnd = _treeTrunkThicknessMaxField.GetFloat(),

            TiltFactorXMin = _treeTiltFactorXMinField.GetFloat(),
            TiltFactorXMax = _treeTiltFactorXMaxField.GetFloat(),
            TiltFactorYMin = _treeTiltFactorYMinField.GetFloat(),
            TiltFactorYMax = _treeTiltFactorYMaxField.GetFloat(),

            BranchCountMin = _treeBranchCountMinField.GetInt(),
            BranchCountMax = _treeBranchCountMaxField.GetInt(),

            BranchPositionVariance = _treeBranchPositionVarianceField.GetFloat(),

            BranchLengthMin = _treeBranchLengthMinField.GetFloat(),
            BranchLengthMax = _treeBranchLengthMaxField.GetFloat(),

            BranchLengthFalloff = _treeBranchLengthFalloffField.GetFloat(),

            BranchThicknessMin = _treeBranchThicknessMinField.GetFloat(),
            BranchThicknessMax = _treeBranchThicknessMaxField.GetFloat(),

            BranchFirstTrunkMin = _treeBranchFirstTrunkMinField.GetInt(),
            BranchFirstTrunkMax = _treeBranchFirstTrunkMaxField.GetInt(),

            BranchTrunkStart = _treeBranchTrunkStartField.GetFloat(),
            BranchTrunkEnd = _treeBranchTrunkEndField.GetFloat(),

            BranchAngleMin = _treeBranchAngleMinField.GetFloat(),
            BranchAngleMax = _treeBranchAngleMaxField.GetFloat(),

            BranchTiltMin = _treeBranchTiltMinField.GetFloat(),
            BranchTiltMax = _treeBranchTiltMaxField.GetFloat(),

            // Leaves
            LeafClusterType = _leavesTypeIndex,
            LeafClusterFollowBranchDirection = _leavesFollowBranchDirection,

            LeafClusterRadiusMin = _leavesRadiusMinField.GetFloat(),
            LeafClusterRadiusMax = _leavesRadiusMaxField.GetFloat(),

            LeafClusterHeightMin = _leavesHeightMinField.GetFloat(),
            LeafClusterHeightMax = _leavesHeightMaxField.GetFloat(),

            LeafClusterPositionMin = _leavesPositionMinField.GetFloat(),
            LeafClusterPositionMax = _leavesPositionMaxField.GetFloat(),

            LeafClusterCountMin = _leavesCountMinField.GetInt(),
            LeafClusterCountMax = _leavesCountMaxField.GetInt(),

            LeafClusterDensity = _leavesDensityField.GetFloat(),

            LeafClusterFalloff = _leavesFalloffField.GetFloat(),

            LeafClusterScaleXMin = _leavesScaleXMinField.GetFloat(),    
            LeafClusterScaleXMax = _leavesScaleXMaxField.GetFloat(),    
            LeafClusterScaleYMin = _leavesScaleYMinField.GetFloat(),    
            LeafClusterScaleYMax = _leavesScaleYMaxField.GetFloat(),    
            LeafClusterScaleZMin = _leavesScaleZMinField.GetFloat(),    
            LeafClusterScaleZMax = _leavesScaleZMaxField.GetFloat()
        };

        return info;
    }

    public void SetTreeInfo(TreeGenerationInfo info)
    {
        _treeBoundsMinX.UpdateText(info.MinX.ToString());
        _treeBoundsMinY.UpdateText(info.MinY.ToString());
        _treeBoundsMinZ.UpdateText(info.MinZ.ToString());

        _treeBoundsMaxX.UpdateText(info.MaxX.ToString());
        _treeBoundsMaxY.UpdateText(info.MaxY.ToString());
        _treeBoundsMaxZ.UpdateText(info.MaxZ.ToString());



        _treeSeedField.UpdateText(info.Seed.ToString());
        _treeTrunkCountField.UpdateText(info.Count.ToString());

        _treeTrunkHeightMinField.UpdateText(info.HeightMin.ToString());
        _treeTrunkHeightMaxField.UpdateText(info.HeightMax.ToString());

        _treeTrunkSplitMinField.UpdateText(info.SplitMin.ToString());
        _treeTrunkSplitMaxField.UpdateText(info.SplitMax.ToString());

        _treeTrunkThicknessMinField.UpdateText(info.ThicknessStart.ToString());
        _treeTrunkThicknessMaxField.UpdateText(info.ThicknessEnd.ToString());

        _treeTiltFactorXMinField.UpdateText(info.TiltFactorXMin.ToString());
        _treeTiltFactorXMaxField.UpdateText(info.TiltFactorXMax.ToString());
        _treeTiltFactorYMinField.UpdateText(info.TiltFactorYMin.ToString());
        _treeTiltFactorYMaxField.UpdateText(info.TiltFactorYMax.ToString());

        _treeBranchCountMinField.UpdateText(info.BranchCountMin.ToString());
        _treeBranchCountMaxField.UpdateText(info.BranchCountMax.ToString());

        _treeBranchPositionVarianceField.UpdateText(info.BranchPositionVariance.ToString());

        _treeBranchLengthMinField.UpdateText(info.BranchLengthMin.ToString());
        _treeBranchLengthMaxField.UpdateText(info.BranchLengthMax.ToString());

        _treeBranchLengthFalloffField.UpdateText(info.BranchLengthFalloff.ToString());

        _treeBranchThicknessMinField.UpdateText(info.BranchThicknessMin.ToString());
        _treeBranchThicknessMaxField.UpdateText(info.BranchThicknessMax.ToString());

        _treeBranchFirstTrunkMinField.UpdateText(info.BranchFirstTrunkMin.ToString());
        _treeBranchFirstTrunkMaxField.UpdateText(info.BranchFirstTrunkMax.ToString());

        _treeBranchTrunkStartField.UpdateText(info.BranchTrunkStart.ToString());
        _treeBranchTrunkEndField.UpdateText(info.BranchTrunkEnd.ToString());

        _treeBranchAngleMinField.UpdateText(info.BranchAngleMin.ToString());
        _treeBranchAngleMaxField.UpdateText(info.BranchAngleMax.ToString());

        _treeBranchTiltMinField.UpdateText(info.BranchTiltMin.ToString());
        _treeBranchTiltMaxField.UpdateText(info.BranchTiltMax.ToString());

        // Leaves
        _leavesTypeIndex = info.LeafClusterType;
        _leavesFollowBranchDirection = info.LeafClusterFollowBranchDirection;
        _leavesFollowBranchDirectionButton.Dataset["state"] = !_leavesFollowBranchDirection;
        _leavesFollowBranchDirectionButton.OnClickAction();

        _leavesRadiusMinField.UpdateText(info.LeafClusterRadiusMin.ToString());
        _leavesRadiusMaxField.UpdateText(info.LeafClusterRadiusMax.ToString());

        _leavesHeightMinField.UpdateText(info.LeafClusterHeightMin.ToString());
        _leavesHeightMaxField.UpdateText(info.LeafClusterHeightMax.ToString());

        _leavesPositionMinField.UpdateText(info.LeafClusterPositionMin.ToString());
        _leavesPositionMaxField.UpdateText(info.LeafClusterPositionMax.ToString());

        _leavesCountMinField.UpdateText(info.LeafClusterCountMin.ToString());
        _leavesCountMaxField.UpdateText(info.LeafClusterCountMax.ToString());

        _leavesDensityField.UpdateText(info.LeafClusterDensity.ToString());

        _leavesFalloffField.UpdateText(info.LeafClusterFalloff.ToString());

        _leavesScaleXMinField.UpdateText(info.LeafClusterScaleXMin.ToString());
        _leavesScaleXMaxField.UpdateText(info.LeafClusterScaleXMax.ToString());
        _leavesScaleYMinField.UpdateText(info.LeafClusterScaleYMin.ToString());
        _leavesScaleYMaxField.UpdateText(info.LeafClusterScaleYMax.ToString());
        _leavesScaleZMinField.UpdateText(info.LeafClusterScaleZMin.ToString());
        _leavesScaleZMaxField.UpdateText(info.LeafClusterScaleZMax.ToString());
    }
}