using PBG.UI;
using static PBG.UI.Styles;

public partial class StructureNodeUI
{
    public UIElementBase LeftNoisePanel() =>
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
            ]), ref _noisePaletteBlockSelection),
        ]), ref _noisePaletteCollection),
        newVCol(Class(w_full_minus_[10], grow_children, border_[5, 5, 5, 5], spacing_[5], ignore_invisible, not_toggle_old_invisible), Sub([
            new UICol(Class(w_full, h_[20]), Sub(
                new UIText("Group input settings", Class(mc_[20], fs_[1], middle_left))
            )),
            new UICol(Class(w_full, h_[30], blank_sharp_g_[10]), Sub(
                newField("", Class(mc_[18], fs_[1], middle_left, left_[5]),
                OnTextChange(StructureNodeManager.SetGroupFieldNameCall),
                ref _groupInputName)
            )),
            new UICol(Class(w_full, h_[30], blank_sharp_g_[40]),
            OnClickCol(_ => {
                StructureNodeManager.GroupRemoveFieldCall(_groupInputName.GetText());
                _groupInputSettings.SetVisible(false);
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
                    ), ref _groupFloatButton),
                    newCol(Class(w_[32f], h_[30], blank_sharp_g_[40], top_center),
                    OnClickCol(_ => { if (StructureNodeManager.GroupInputField != null) StructureNodeManager.SetGroupFieldTypeCall(new NodeValue_Int(StructureNodeManager.GroupInputField.Node, 0)); ResetGroupInputValues("int", 1, [0]); }),
                    Sub(
                        new UIText("int", Class(mc_[3], fs_[1], middle_center))
                    ), ref _groupIntButton),
                    newCol(Class(w_[32f], h_[30], blank_sharp_g_[40], top_right),
                    OnClickCol(_ => { if (StructureNodeManager.GroupInputField != null) StructureNodeManager.SetGroupFieldTypeCall(new NodeValue_Vector2(StructureNodeManager.GroupInputField.Node, 0, 0)); ResetGroupInputValues("vec2", 2, [0, 0]); }),
                    Sub(
                        new UIText("vec2", Class(mc_[4], fs_[1], middle_center))
                    ), ref _grouPBGector2Button)
                ])),
                new UICol(Class(w_full, h_[30]), Sub([
                    newCol(Class(w_[32f], h_[30], blank_sharp_g_[40]),
                    OnClickCol(_ => { if (StructureNodeManager.GroupInputField != null) StructureNodeManager.SetGroupFieldTypeCall(new NodeValue_Vector2Int(StructureNodeManager.GroupInputField.Node, 0, 0)); ResetGroupInputValues("ivec2", 2, [0, 0]); }),
                    Sub(
                        new UIText("ivec2", Class(mc_[5], fs_[1], middle_center))
                    ), ref _grouPBGector2iButton),
                    newCol(Class(w_[32f], h_[30], blank_sharp_g_[40], top_center),
                    OnClickCol(_ => { if (StructureNodeManager.GroupInputField != null) StructureNodeManager.SetGroupFieldTypeCall(new NodeValue_Vector3(StructureNodeManager.GroupInputField.Node, 0, 0, 0)); ResetGroupInputValues("vec3", 3, [0, 0, 0]); }),
                    Sub(
                        new UIText("vec3", Class(mc_[4], fs_[1], middle_center))
                    ), ref _grouPBGector3Button),
                    newCol(Class(w_[32f], h_[30], blank_sharp_g_[40], top_right),
                    OnClickCol(_ => { if (StructureNodeManager.GroupInputField != null) StructureNodeManager.SetGroupFieldTypeCall(new NodeValue_Vector3Int(StructureNodeManager.GroupInputField.Node, 0, 0, 0)); ResetGroupInputValues("ivec3", 3, [0, 0, 0]); }),
                    Sub(
                        new UIText("ivec3", Class(mc_[5], fs_[1], middle_center))
                    ), ref _grouPBGector3iButton)
                ])),
                newCol(Class(w_full, h_[30], blank_sharp_g_[10]), Sub(
                    new UIField("0", Class(mc_[20], fs_[1], middle_left, left_[5]),
                    OnHoldField(i => StructureNodeManager.GroupInputField?.Value.SetSlideValue(i, 0)),
                    OnTextChange(i => StructureNodeManager.GroupInputField?.Value.UpdateValue(0, i.GetFloat())))
                ), ref _grouPBGalueIndex0),
                newCol(Class(w_full, h_[30], blank_sharp_g_[10]), Sub(
                    new UIField("0", Class(mc_[20], fs_[1], middle_left, left_[5]),
                    OnHoldField(i => StructureNodeManager.GroupInputField?.Value.SetSlideValue(i, 1)),
                    OnTextChange(i => StructureNodeManager.GroupInputField?.Value.UpdateValue(1, i.GetFloat())))
                ), ref _grouPBGalueIndex1),
                newCol(Class(w_full, h_[30], blank_sharp_g_[10]), Sub(
                    new UIField("0", Class(mc_[20], fs_[1], middle_left, left_[5]),
                    OnHoldField(i => StructureNodeManager.GroupInputField?.Value.SetSlideValue(i, 2)),
                    OnTextChange(i => StructureNodeManager.GroupInputField?.Value.UpdateValue(2, i.GetFloat())))
                ), ref _grouPBGalueIndex2),
            ]))
        ]), ref _groupInputSettings)
    ]), ref _leftNoiseSection);


    private UIElementBase RightNoisePanel() =>
    newCol(Class(w_full_minus_[2], h_full, blank_full_g_[20], top_right, invisible, mask_children), Sub([
        new UICol(Class(h_[30], w_full_minus_[10], top_[5], top_center), Sub([
            new UICol(Class(h_[30], w_half_minus_[2], blank_sharp_g_[30]),
            OnClickCol(_ => NoiseBasic()), Sub([
                new UIText("Basic", Class(middle_center, mc_[5], fs_[1f]))
            ])),
            new UICol(Class(h_[30], w_half_minus_[2], blank_sharp_g_[30], top_right),
            OnClickCol(_ => NoiseGroup()), Sub([
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
                new UICol("save-collection", Class(w_half_minus_[2], h_full, blank_sharp_g_[30]), OnClickCol(_ => NoiseSave()), Sub([
                    new UIText("Save", Class(middle_center, mc_[4], fs_[1]))
                ])),
                new UICol("load-collection", Class(w_half_minus_[2], h_full, blank_sharp_g_[30], top_right), OnClickCol(_ => NoiseLoad()), Sub([
                    new UIText("Load", Class(middle_center, mc_[4], fs_[1]))
                ]))
            ])),
            newVScroll(Class(w_full_minus_[10], h_full_minus_[375], blank_sharp_g_[10], border_bottom_[5], mask_children), Sub([
                ..Run(GenerateBasicElements)
            ]), ref _sidePanelFileList)
        ]), ref _noiseNodesPanel),
    ]), ref _rightNoisePanel);
}