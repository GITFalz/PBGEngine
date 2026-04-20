using PBG.UI;
using static PBG.UI.Styles;

public partial class StructureNodeUI
{
    public UIElementBase LeftNoisePanel() =>
    new UIVCol(w_full_minus_[2], h_full, blank_full_g_[20], top_left, invisible)[
        new UIVCol(w_full, grow_children, top_center, spacing_[5], blank_sharp_g_[15])[
            new UIVScroll("in_scroll", w_full_minus_[4], h_[296], top_center, spacing_[2], border_[2, 2, 2, 2], mask_children)[
                Foreach(ItemDataManager.AllItems, (name, block) => {
                    if (block is not BlockItemData) return null;
                    return new UICol(w_full, top_center, h_[38], blank_sharp_g_[30], data_["block", name])
                        .OnClick(nodeManager.dragBlockUI.DragBlockStart)[
                            new UIImg(middle_left, h_[38], w_[38], item_[name], bg_white),
                            new UIText(name, mc_[name.Length], fs_[1], middle_left, left_[40])
                        ]
                    ; 
                })
            ].Ref(ref _noisePaletteBlockSelection)
        ].Ref(ref _noisePaletteCollection),
        new UIVCol(w_full_minus_[10], grow_children, border_[5, 5, 5, 5], spacing_[5], ignore_invisible, not_toggle_old_invisible)[
            new UICol(w_full, h_[20])[
                new UIText("Group input settings", mc_[20], fs_[1], middle_left)
            ],
            new UICol(w_full, h_[30], blank_sharp_g_[10])[
                new UIField("", mc_[18], fs_[1], middle_left, left_[5])
                .OnTextChange(StructureNodeManager.SetGroupFieldNameCall)
                .Ref(ref _groupInputName)
            ],
            new UICol(w_full, h_[30], blank_sharp_g_[40])
            .OnClick(_ => {
                StructureNodeManager.GroupRemoveFieldCall(_groupInputName.GetText());
                _groupInputSettings.SetVisible(false);
            })[
                new UIText("Delete", mc_[6], fs_[1], middle_center)
            ],
            new UIVCol("values", w_full, grow_children, spacing_[5])[
                new UICol(w_full, h_[30])[
                    new UICol(w_[32f], h_[30], blank_sharp_g_[40])
                    .OnClick(_ => { if (StructureNodeManager.GroupInputField != null) StructureNodeManager.SetGroupFieldTypeCall(new NodeValue_Float(StructureNodeManager.GroupInputField.Node, 0f)); ResetGroupInputValues("float", 1, [0]); })[
                        new UIText("float", mc_[5], fs_[1], middle_center)
                    ].Ref(ref _groupFloatButton),
                    new UICol(w_[32f], h_[30], blank_sharp_g_[40], top_center)
                    .OnClick(_ => { if (StructureNodeManager.GroupInputField != null) StructureNodeManager.SetGroupFieldTypeCall(new NodeValue_Int(StructureNodeManager.GroupInputField.Node, 0)); ResetGroupInputValues("int", 1, [0]); })[
                        new UIText("int", mc_[3], fs_[1], middle_center)
                    ].Ref(ref _groupIntButton),
                    new UICol(w_[32f], h_[30], blank_sharp_g_[40], top_right)
                    .OnClick(_ => { if (StructureNodeManager.GroupInputField != null) StructureNodeManager.SetGroupFieldTypeCall(new NodeValue_Vector2(StructureNodeManager.GroupInputField.Node, 0, 0)); ResetGroupInputValues("vec2", 2, [0, 0]); })[
                        new UIText("vec2", mc_[4], fs_[1], middle_center)
                    ].Ref(ref _grouPBGector2Button)
                ],
                new UICol(w_full, h_[30])[
                    new UICol(w_[32f], h_[30], blank_sharp_g_[40])
                    .OnClick(_ => { if (StructureNodeManager.GroupInputField != null) StructureNodeManager.SetGroupFieldTypeCall(new NodeValue_Vector2Int(StructureNodeManager.GroupInputField.Node, 0, 0)); ResetGroupInputValues("ivec2", 2, [0, 0]); })[
                        new UIText("ivec2", mc_[5], fs_[1], middle_center)
                    ].Ref(ref _grouPBGector2iButton),
                    new UICol(w_[32f], h_[30], blank_sharp_g_[40], top_center)
                    .OnClick(_ => { if (StructureNodeManager.GroupInputField != null) StructureNodeManager.SetGroupFieldTypeCall(new NodeValue_Vector3(StructureNodeManager.GroupInputField.Node, 0, 0, 0)); ResetGroupInputValues("vec3", 3, [0, 0, 0]); })[
                        new UIText("vec3", mc_[4], fs_[1], middle_center)
                    ].Ref(ref _grouPBGector3Button),
                    new UICol(w_[32f], h_[30], blank_sharp_g_[40], top_right)
                    .OnClick(_ => { if (StructureNodeManager.GroupInputField != null) StructureNodeManager.SetGroupFieldTypeCall(new NodeValue_Vector3Int(StructureNodeManager.GroupInputField.Node, 0, 0, 0)); ResetGroupInputValues("ivec3", 3, [0, 0, 0]); })[
                        new UIText("ivec3", mc_[5], fs_[1], middle_center)
                    ].Ref(ref _grouPBGector3iButton)
                ],
                new UICol(w_full, h_[30], blank_sharp_g_[10])[
                    new UIField("0", mc_[20], fs_[1], middle_left, left_[5])
                    .OnHold(i => StructureNodeManager.GroupInputField?.Value.SetSlideValue(i, 0))
                    .OnTextChange(i => StructureNodeManager.GroupInputField?.Value.UpdateValue(0, i.GetFloat()))
                ].Ref(ref _grouPBGalueIndex0),
                new UICol(w_full, h_[30], blank_sharp_g_[10])[
                    new UIField("0", mc_[20], fs_[1], middle_left, left_[5])
                    .OnHold(i => StructureNodeManager.GroupInputField?.Value.SetSlideValue(i, 1))
                    .OnTextChange(i => StructureNodeManager.GroupInputField?.Value.UpdateValue(1, i.GetFloat()))
                ].Ref(ref _grouPBGalueIndex1),
                new UICol(w_full, h_[30], blank_sharp_g_[10])[
                    new UIField("0", mc_[20], fs_[1], middle_left, left_[5])
                    .OnHold(i => StructureNodeManager.GroupInputField?.Value.SetSlideValue(i, 2))
                    .OnTextChange(i => StructureNodeManager.GroupInputField?.Value.UpdateValue(2, i.GetFloat()))
                ].Ref(ref _grouPBGalueIndex2)
            ]
        ].Ref(ref _groupInputSettings)
    ].Ref(ref _leftNoiseSection);


    private UIElementBase RightNoisePanel() =>
    new UICol(w_full_minus_[2], h_full, blank_full_g_[20], top_right, invisible, mask_children)[
        new UICol(h_[30], w_full_minus_[10], top_[5], top_center)[
            new UICol(h_[30], w_half_minus_[2], blank_sharp_g_[30])
            .OnClick(_ => NoiseBasic())[
                new UIText("Basic", middle_center, mc_[5], fs_[1f])
            ],
            new UICol(h_[30], w_half_minus_[2], blank_sharp_g_[30], top_right)
            .OnClick(_ => NoiseGroup())[
                new UIText("Group", middle_center, mc_[5], fs_[1f])
            ]
        ],
        new UIVCol(w_full, h_full, border_[5, 5, 5, 5], spacing_[5], top_[35])[
            new UICol(h_[20], w_full)[
                new UIText("Basic", mc_[5], middle_left, fs_[1.2f])
            ],
            new UICol("file-name-collection", w_full_minus_[10], h_[30], blank_sharp_g_[10])[
                new UIField("Base", middle_left, left_[5], mc_[25], fs_[1]).OnTextChange(SetName).Ref(ref NodeManager.FileNameInputField)
            ],
            new UICol(w_full_minus_[10], h_[30])[
                new UICol("save-collection", w_half_minus_[2], h_full, blank_sharp_g_[30]).OnClick(_ => NoiseSave())[
                    new UIText("Save", middle_center, mc_[4], fs_[1])
                ],
                new UICol("load-collection", w_half_minus_[2], h_full, blank_sharp_g_[30], top_right).OnClick(_ => NoiseLoad())[
                    new UIText("Load", middle_center, mc_[4], fs_[1])
                ]
            ],
            new UIVScroll(w_full_minus_[10], h_full_minus_[375], blank_sharp_g_[10], border_bottom_[5], mask_children)[
                Run(GenerateBasicElements)
            ].Ref(ref _sidePanelFileList)
        ].Ref(ref _noiseNodesPanel)
    ].Ref(ref _rightNoisePanel);
}