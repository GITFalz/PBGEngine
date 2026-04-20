using PBG;
using PBG.Core;
using PBG.Data;
using PBG.MathLibrary;
using PBG.Modeling;
using PBG.UI;
using PBG.UI.Creator;
using PBG.UI.FileManager;

using static PBG.UI.Styles;

public class GeneralEditorUI : UIScript
{
    public const int BASE_BACKGROUND = 20;
    public const int BASE_BUTTON = 30;
    public const int BASE_BORDER = 25;
    public const int HOVER_BACKGROUND = 30;
    public const int HOVER_BUTTON = 40;
    public const int HOVER_BORDER = 35;

    public GeneralModelingEditor Editor;
    public string CurrentEditor = "Modeling";

    public AnimationEditor EditA => Editor.animationEditor;

    public UIVCol ModelingLeftPanel = null!;
    public UICol ModelingRightPanel = null!;
    public UIVCol ModelingEdit = null!;

    public UIVCol RiggingLeftPanel = null!;
    public UICol RiggingRightPanel = null!;

    public UIVCol AnimationLeftPanel = null!;
    public UICol AnimationRightPanel = null!;
    public UIVCol AnimationEdit = null!;

    public UIVScroll TextureLeftPanel = null!;
    public UIVCol TextureRightPanel = null!;
    public UICol TextureEditorSlider = null!;

    public UIField TextureWidthField = null!;
    public UIField TextureHeightField = null!;

    public UIField MeshUnitsField = null!;
    public UIField UvUnitsField = null!;

    public UIVScroll Hierarchy = null!;

    public UIVScroll AnimationHierarchy = null!;

    public UIElementBase[] ModelingElements;
    public UIElementBase[] RiggingElements;
    public UIElementBase[] AnimationElements;
    public UIElementBase[] TextureElements;

    private UIText FpsText = null!;
    private UIText RamText = null!;

    private UICol WorldTransformButton = null!;
    private UICol LocalTransformButton = null!;

    private Action<int, float> _transformAction = (i, v) => { };
    private Action<int, float> _scaleAction = (i, v) => { };
    private Action<int, float> _rotationAction = (i, v) => { };

    private Action<float, float, float> _modelingSetTransform = null!;
    private Action<float, float, float> _modelingSetScale = null!;
    private Action<float, float, float> _modelingSetRotation = null!;

    private Action<float, float, float> _riggingSetTransform = null!;
    private Action<float, float, float> _riggingSetScale = null!;
    private Action<float, float, float> _riggingSetRotation = null!;

    private Action<float, float, float> _animationSetTransform = null!;
    private Action<float, float, float> _animationSetScale = null!;
    private Action<float, float, float> _animationSetRotation = null!;

    private Action<float, float, float> _setTransform;
    private Action<float, float, float> _setScale;
    private Action<float, float, float> _setRotation;

    public bool HoveringCenter = false;

    public GeneralEditorUI(GeneralModelingEditor editor)
    {
        Editor = editor;
        ModelingElements = [ModelingLeftPanel, ModelingRightPanel];
        RiggingElements = [RiggingLeftPanel, RiggingRightPanel];
        AnimationElements = [AnimationLeftPanel, AnimationRightPanel, AnimationEdit];
        TextureElements = [TextureLeftPanel, TextureRightPanel, TextureEditorSlider];

        _setTransform = _modelingSetTransform;
        _setScale = _modelingSetScale;
        _setRotation = _modelingSetRotation;
    }

    public override UIElementBase Script() =>
    new UICol(w_full, h_full)[
        new UICol("left-side-panel", w_[200], blank_full_g_[BASE_BACKGROUND], h_full_minus_[50], bottom_left, border_ui_[0, 0, 2, 0], border_color_g_[BASE_BORDER])
        .OnClick(_ => Editor.ClickedMenu = true)[
            new UIVCol(w_full, h_full, spacing_[5])[
                new UICol(w_full, h_[30])[
                    new UICol(w_half_minus_[7.5f], left_[5], h_full_minus_[10], top_[5], blank_sharp)
                    .OnHoverEnter(c => c.UpdateColor((0.3f, 0.3f, 0.3f, 1)))
                    .OnHoverExit(c => c.UpdateColor((0.0f, 0.0f, 0.0f, 0f)))
                    .OnClick(_ => Editor.modelingEditor.SwitchMode(Editor.modelingEditor.EditingMode))[
                        new UIText("EDIT", mc_[4], fs_[1f], middle_center)
                    ],
                    new UICol(w_half_minus_[7.5f], right_[5], h_full_minus_[10], top_[5], blank_sharp, top_right)
                    .OnHoverEnter(c => c.UpdateColor((0.3f, 0.3f, 0.3f, 1)))
                    .OnHoverExit(c => c.UpdateColor((0.0f, 0.0f, 0.0f, 0f)))
                    .OnClick(_ => Editor.modelingEditor.SwitchMode(Editor.modelingEditor.SelectionMode))[
                        new UIText("SELECT", mc_[6], fs_[1f], middle_center)
                    ]
                ],
                new UICol(h_[20], w_full_minus_[10], top_center)[
                    new UIText("Hierarchy", mc_[9], fs_[1.2f], middle_left),
                    new UICol(w_[20], h_[20], middle_right, right_[25], blank_full_g_[BASE_BACKGROUND], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout)
                    .OnClick(_ => {
                        Editor.FileManager.SetAction(FileManagerType.Export);
                        Editor.FileManager.ToggleOn();
                        Editor.FileManager.FileType = ".model";
                        Editor.FileManager.SaveFile = Editor.modelingEditor.SaveModel;
                    })[
                        new UIImg(w_[20], h_[20], icon_[42], middle_center, bg_white)
                    ],
                    new UICol(w_[20], h_[20], middle_right, blank_full_g_[BASE_BACKGROUND], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout)
                    .OnClick(_ =>
                    {
                        Editor.FileManager.SetAction(FileManagerType.Import);
                        Editor.FileManager.ToggleOn();
                    })[
                        new UIText("+", mc_[1], fs_[2f], middle_center)
                    ]
                ],
                new UIVScroll(w_full_minus_[4], top_center, h_half_minus_[30], border_[5, 5, 5, 5], spacing_[5], blank_sharp_g_[10], mask_children).Ref(ref Hierarchy)
            ].Ref(ref ModelingLeftPanel),
            new UIVCol(w_full, h_full, spacing_[5], top_[5], hidden)[
                new UICol(h_[20], w_full_minus_[10], top_center)[
                    new UIText("Rig", mc_[3], fs_[1.2f], middle_left),
                    new UICol(w_[20], h_[20], middle_right, right_[25], blank_full_g_[BASE_BACKGROUND])
                    .OnClick(_ => {
                        Editor.FileManager.SetAction(FileManagerType.Export);
                        Editor.SetFileManagerExportAsModel();
                        Editor.FileManager.ToggleOn();
                    })[
                        new UIImg(w_[20], h_[20], icon_[42], middle_center, bg_white)
                    ],
                    new UICol(w_[20], h_[20], middle_right, blank_full_g_[BASE_BACKGROUND])
                    .OnClick(_ =>
                    {
                        Editor.FileManager.SetAction(FileManagerType.Import);
                        Editor.FileManager.ToggleOn();
                    })[
                        new UIText("+", mc_[1], fs_[2f], middle_center)
                    ]
                ]
            ].Ref(ref RiggingLeftPanel),
            new UIVCol(w_full, h_full, spacing_[5], top_[5], hidden)[
                new UICol(h_[20], w_full_minus_[10], top_center)[
                    new UIText("Animation", mc_[9], fs_[1.2f], middle_left),
                    new UICol(w_[20], h_[20], middle_right, right_[25], blank_full_g_[BASE_BACKGROUND])
                    .OnClick(_ => {
                        Editor.FileManager.SetAction(FileManagerType.Export);
                        Editor.SetFileManagerExportAsModel();
                        Editor.FileManager.ToggleOn();
                    })[
                        new UIImg(w_[20], h_[20], icon_[42], middle_center, bg_white)
                    ],
                    new UICol(w_[20], h_[20], middle_right, blank_full_g_[BASE_BACKGROUND])
                    .OnClick(_ =>
                    {
                        if (ModelManager.SelectedModel == null)
                            return;

                        var model = ModelManager.SelectedModel;
                        var button = GetAnimationButton(model);
                        AnimationHierarchy.AddElement(button);
                        AnimationHierarchy.UIController?.AddElement(button);
                        AnimationHierarchy.QueueAlign();
                        AnimationHierarchy.QueueUpdateTransformation();
                        AnimationHierarchy.QueueUpdateScaling();
                    })[
                        new UIText("+", mc_[1], fs_[2f], middle_center)
                    ]
                ],
                new UIVScroll(w_full_minus_[4], top_center, h_half_minus_[30], border_[5, 5, 5, 5], spacing_[5], blank_full_g_[15]).Ref(ref AnimationHierarchy)
            ].Ref(ref AnimationLeftPanel),
            new UIVScroll(w_full_minus_[2], h_full_minus_[30], mask_children, hidden)[
                new UICol(w_full, h_[30])[
                    new UIText("TOOLS", mc_[5], middle_left, left_[5], fs_[1.2f])
                ],
                new UIVCol(w_full, grow_children, border_[5, 5, 5, 5], spacing_[5], blank_full_g_[15])[
                    new UICol(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected)
                    .OnClick(c => TextureDrawingModeButton(c, DrawingMode.None))[
                        new UIImg(w_[26], h_[26], icon_[15], middle_left, left_[5], bg_white),
                        new UIText("None", mc_[5], middle_left, left_[40], fs_[1.2f])
                    ],
                    new UICol(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected)
                    .OnClick(c => TextureDrawingModeButton(c, DrawingMode.Move))[
                        new UIImg(w_[26], h_[26], icon_[45], middle_left, left_[5], bg_white),
                        new UIText("Move", mc_[4], middle_left, left_[40], fs_[1.2f])
                    ],
                    new UICol(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected)
                    .OnClick(c => TextureDrawingModeButton(c, DrawingMode.Brush))[
                        new UIImg(w_[26], h_[26], icon_[43], middle_left, left_[5], bg_white),
                        new UIText("Brush", mc_[5], middle_left, left_[40], fs_[1.2f])
                    ],
                    new UICol(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected)
                    .OnClick(c => TextureDrawingModeButton(c, DrawingMode.Pencil))[
                        new UIImg(w_[26], h_[26], icon_[48], middle_left, left_[5], bg_white),
                        new UIText("Pencil", middle_left, left_[40], fs_[1.2f])
                    ],
                    new UICol(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected)
                    .OnClick(c => TextureDrawingModeButton(c, DrawingMode.Eraser))[
                        new UIImg(w_[26], h_[26], icon_[44], middle_left, left_[5], bg_white),
                        new UIText("Eraser", mc_[6], middle_left, left_[40], fs_[1.2f])
                    ],
                    new UICol(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected)
                    .OnClick(c => TextureDrawingModeButton(c, DrawingMode.Blur))[
                        new UIImg(w_[26], h_[26], icon_[46], middle_left, left_[5], bg_white),
                        new UIText("Blur", mc_[4], middle_left, left_[40], fs_[1.2f])
                    ],
                    new UICol(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected)
                    .OnClick(c => TextureDrawingModeButton(c, DrawingMode.Pick))[
                        new UIImg(w_[26], h_[26], icon_[49], middle_left, left_[5], bg_white),
                        new UIText("Pick", mc_[4], middle_left, left_[40], fs_[1.2f])
                    ],
                    new UICol(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected)
                    .OnClick(c => TextureDrawingModeButton(c, DrawingMode.Selection))[
                        new UIImg(w_[26], h_[26], icon_[47], middle_left, left_[5], bg_white),
                        new UIText("Selection", mc_[9], middle_left, left_[40], fs_[1.2f])
                    ]
                ],
                new UICol(w_full_minus_[10], h_[32], border_ui_[0, 2, 0, 0], border_color_g_[BASE_BORDER], top_center)[
                    new UIText("COLOR", mc_[5], middle_left, top_[1], fs_[1.2f]),
                    new UIImg(w_[24], h_[24], middle_right, icon_[22], bg_white).OnClick(img => {
                        Editor.textureEditor.ColorPicker.Transform.Disabled = !Editor.textureEditor.ColorPicker.Transform.Disabled;
                        img.UpdateIconIndex(Editor.textureEditor.ColorPicker.Transform.Disabled ? 23 : 22);
                    })
                ],
                new UIVCol(w_full, grow_children, spacing_[5], border_[5, 5, 5, 5], blank_full_g_[15])[
                    Forloop(0, 10, (i) => GenerateColorPickers())
                ],
                new UICol(w_full, h_[30])[
                    new UICol(w_full_minus_[6], middle_center, h_[24], blank_full_g_[25], border_ui_[2, 2, 2, 2], border_color_g_[35], hover_color_g_[25, 30], hover_color_duration_[0.2f], hover_color_easeinout)
                    .OnClick(_ => {
                        var colorPickers = TextureLeftPanel.GetElement<UIVCol>("color-pickers");
                        if (colorPickers != null) 
                        {
                            var newPickers = GenerateColorPickers();
                            colorPickers.AddElement(newPickers);
                            UIController.AddElement(newPickers);
                            TextureLeftPanel.QueueAlign();
                            TextureLeftPanel.QueueUpdateScaling();
                            TextureLeftPanel.QueueUpdateTransformation();
                        }        
                    })[
                        new UIImg(w_[24], h_[24], middle_center, icon_[16], bg_white)
                    ]
                ]
            ].Ref(ref TextureLeftPanel),
            new UICol(w_full_minus_[10], h_[30], bottom_center, bottom_[25])[
                new UIText("Fps: 0", mc_[12], fs_[1], middle_left).Ref(ref FpsText)
            ],
            new UICol(w_full_minus_[10], h_[30], bottom_center)[
                new UIText("Ram: 0", mc_[20], fs_[1], middle_left).Ref(ref RamText)
            ]
        ],
        new UICol("nav-bar", w_full, h_[50], blank_full_g_[BASE_BACKGROUND], border_ui_[0, 0, 0, 2], border_color_g_[BASE_BORDER])
        .OnClick(_ => Editor.ClickedMenu = true)[
            new UIHCol(w_full, h_full)[
                new UICol(w_[100], h_[40], left_[5], top_[5], blank_sharp_g_[BASE_BUTTON], hover_scale_[1.05f], hover_scale_duration_[0.25f], hover_scale_easeout)
                .OnClick(_ => Scene.LoadScene("MainMenu"))[
                    new UIText("Main Menu", middle_center, mc_[9], fs_[1])
                ],
                new UICol(w_[100], h_[40], left_[5], top_[5], blank_sharp_g_[BASE_BUTTON], hover_scale_[1.05f], hover_scale_duration_[0.25f], hover_scale_easeout)
                .OnClick(_ => SwitchScene("Modeling"))[
                    new UIText("Modeling", middle_center, mc_[8], fs_[1])
                ],
                new UICol(w_[100], h_[40], left_[5], top_[5], blank_sharp_g_[BASE_BUTTON], hover_scale_[1.05f], hover_scale_duration_[0.25f], hover_scale_easeout)
                .OnClick(_ => SwitchScene("Rigging"))[
                    new UIText("Rigging", middle_center, mc_[7], fs_[1])
                ],
                new UICol(w_[100], h_[40], left_[5], top_[5], blank_sharp_g_[BASE_BUTTON], hover_scale_[1.05f], hover_scale_duration_[0.25f], hover_scale_easeout)
                .OnClick(_ => SwitchScene("Animation"))[
                    new UIText("Animation", middle_center, mc_[9], fs_[1])
                ],
                new UICol(w_[100], h_[40], left_[5], top_[5], blank_sharp_g_[BASE_BUTTON], hover_scale_[1.05f], hover_scale_duration_[0.25f], hover_scale_easeout)
                .OnClick(_ => SwitchScene("Texture"))[
                    new UIText("Texture", middle_center, mc_[7], fs_[1])
                ]
            ]
        ],
        new UICol("center", w_full_minus_[400], left_[200], h_full_minus_[50], top_[50])
        .OnHover(_ => HoveringCenter = true)[
            new UIVCol(grow_children, top_right, top_[10], right_[10], blank_round_g_[BASE_BACKGROUND], w_[160], spacing_[5], border_[5, 5, 5, 5], mask_children, depth_[3])
            .Ref(ref ModelingEdit)
            .OnClick(_ => Editor.ClickedMenu = true)
            .OnHover(_ => HoveringCenter = false)[
                new UICol(w_full_minus_[10], h_[25])[
                    new UICol(w_[49f], h_full, blank_sharp_g_[BASE_BUTTON])[
                        new UIText("Mirror", mc_[6], fs_[1], middle_center)
                    ],
                    new UICol("apply", w_[49f], h_full, blank_sharp_g_[BASE_BUTTON], top_right)
                    .OnHoverEnter(c => c.UpdateColor((0.5f, 0.5f, 0.5f, 1)))
                    .OnHoverExit(c => c.UpdateColor((0.4f, 0.4f, 0.4f, 1)))
                    .OnClick(_ => Editor.ApplyMirror())[
                        new UIText("Apply", mc_[5], fs_[1], middle_center)
                    ]
                ],
                new UICol(w_full_minus_[10], h_[25])[
                    new UICol(w_[32f], h_full, blank_sharp_g_[BASE_BUTTON])[
                        new UIText("X", mc_[1], fs_[1], middle_left, left_[5]),
                        new UIButton(w_[15], h_[15], blank_round_g_[ModelSettings.Mirror.X == 1 ? 60 : 20], right_[5], middle_right).OnClick(i => Toggle(i, "X", SwitchMirror))
                    ],
                    new UICol(w_[32f], h_full, blank_sharp_g_[BASE_BUTTON], top_center)[
                        new UIText("Y", mc_[1], fs_[1], middle_left, left_[5]),
                        new UIButton(w_[15], h_[15], blank_round_g_[ModelSettings.Mirror.Y == 1 ? 60 : 20], right_[5], middle_right).OnClick(i => Toggle(i, "Y", SwitchMirror))
                    ],
                    new UICol(w_[32f], h_full, blank_sharp_g_[BASE_BUTTON], top_right)[
                        new UIText("Z", mc_[1], fs_[1], middle_left, left_[5]),
                        new UIButton(w_[15], h_[15], blank_round_g_[ModelSettings.Mirror.Z == 1 ? 60 : 20], right_[5], middle_right).OnClick(i => Toggle(i, "Z", SwitchMirror))
                    ]
                ],
                new UICol(w_full_minus_[10], h_[25])[
                    new UICol(w_full, h_full, blank_sharp_g_[BASE_BUTTON])[
                        new UIText("Axis", mc_[5], fs_[1], middle_center)
                    ]
                ],
                new UICol(w_full_minus_[10], h_[25])[
                    new UICol(w_[32f], h_full, blank_sharp_g_[BASE_BUTTON])[
                        new UIText("X", mc_[1], fs_[1], middle_left, left_[5]),
                        new UIButton(w_[15], h_[15], blank_round_g_[ModelSettings.Axis.X == 1 ? 60 : 20], right_[5], middle_right).OnClick(i => Toggle(i, "X", SwitchAxis))
                    ],
                    new UICol(w_[32f], h_full, blank_sharp_g_[BASE_BUTTON], top_center)[
                        new UIText("Y", mc_[1], fs_[1], middle_left, left_[5]),
                        new UIButton(w_[15], h_[15], blank_round_g_[ModelSettings.Axis.Y == 1 ? 60 : 20], right_[5], middle_right).OnClick(i => Toggle(i, "Y", SwitchAxis))
                    ],
                    new UICol(w_[32f], h_full, blank_sharp_g_[BASE_BUTTON], top_right)[
                        new UIText("Z", mc_[1], fs_[1], middle_left, left_[5]),
                        new UIButton(w_[15], h_[15], blank_round_g_[ModelSettings.Axis.Z == 1 ? 60 : 20], right_[5], middle_right).OnClick(i => Toggle(i, "Z", SwitchAxis))
                    ]
                ],
                new UICol(w_full_minus_[10], h_[25])[
                    new UICol(w_[50f], h_full, blank_sharp_g_[BASE_BUTTON])
                    .OnClick(_ => Game.SetCursorState(CursorMode.Disabled))
                    .OnHold(GridHold)
                    .OnRelease(_ => Game.SetCursorState(CursorMode.Normal))[
                        new UIText("Snap", mc_[5], fs_[1], middle_left, left_[5]),
                        new UIButton(w_[15], h_[15], blank_round_g_[ModelSettings.Snapping ? 60 : 20], right_[5], middle_right).OnClick(i => Toggle(i, ref ModelSettings.Snapping))
                    ],
                    new UICol(w_[70], h_full, middle_right, blank_sharp_g_[10])[
                        new UIField("1", mc_[6], fs_[1], middle_right, text_align_right, right_[5]).OnTextChange(f => {
                            ModelSettings.SnappingFactor = f.GetFloat(0);
                        })
                    ]
                ],
                new UICol(w_full_minus_[10], h_[25], top_center, blank_sharp_g_[BASE_BUTTON])[
                    new UIText("Grid aligned", mc_[12], fs_[1], middle_left, left_[5]),
                    new UIButton(w_[15], h_[15], blank_round_g_[ModelSettings.GridAligned ? 60 : 20], right_[5], middle_right).OnClick(i => Toggle(i, ref ModelSettings.GridAligned))
                ],
                new UICol(w_full_minus_[10], h_[25])[
                    new UICol(w_[49f], h_full, blank_sharp_g_[ModelSettings.IsLocalMode ? 40 : 50])
                    .OnClick(c => {
                        ModelSettings.IsLocalMode = false;
                        c.UpdateColor(0.5f);
                        LocalTransformButton.UpdateColor(0.4f);
                    })[
                        new UIText("World", mc_[6], fs_[1], middle_center)
                    ].Ref(ref WorldTransformButton),
                    new UICol(w_[49f], h_full, blank_sharp_g_[!ModelSettings.IsLocalMode ? 40 : 50], top_right)
                    .OnClick(c => {
                        ModelSettings.IsLocalMode = true;
                        c.UpdateColor(0.5f);
                        WorldTransformButton.UpdateColor(0.4f);
                    })[
                        new UIText("Local", mc_[5], fs_[1], middle_center)
                    ].Ref(ref LocalTransformButton)
                ]
            ],
            new UIVCol(grow_children, blank_round_g_[BASE_BACKGROUND], w_[160], spacing_[5], border_[5, 5, 5, 5], mask_children, depth_[6], hidden)
            .OnClick(_ => Editor.ClickedMenu = true)
            .OnHover(_ => HoveringCenter = false)[
                new UICol(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON])
                .OnHold(_ => {
                    var delta = Input.MouseDelta;
                    if (delta == Vector2.Zero) 
                        return;

                    AnimationEdit.BaseOffset += delta;
                    AnimationEdit.ApplyChanges(UIChange.Transform);
                })[
                    new UIText("Bone", mc_[4], fs_[1], middle_left, left_[10]),
                    new UIImg(w_[20], h_[20], top_right, bg_white, icon_[15]).OnClick(_ => AnimationEdit.SetVisible(false))
                ],
                new UIImg(w_full_minus_[10], h_[2], blank_full_g_[BASE_BUTTON]),
                new UICol(w_full_minus_[10], h_[20])[
                    new UIText("Copy", mc_[4], fs_[1], middle_left)
                ],
                new UICol(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout)
                .OnClick(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.CopyPosition(EditA.SelectedBone); })[
                    new UIText("position", mc_[8], fs_[1], middle_center)
                ],
                new UICol(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout)
                .OnClick(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.CopyRotation(EditA.SelectedBone); })[
                    new UIText("rotation", mc_[8], fs_[1], middle_center)
                ],
                new UICol(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout)
                .OnClick(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.CopyScale(EditA.SelectedBone); })[
                    new UIText("scale", mc_[5], fs_[1], middle_center)
                ],
                new UIImg(w_full_minus_[10], h_[2], blank_full_g_[BASE_BUTTON]),
                new UICol(w_full_minus_[10], h_[20])[
                    new UIText("Paste", mc_[5], fs_[1], middle_left)
                ],
                new UICol(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout)
                .OnClick(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.Paste(EditA.SelectedBone); })[
                    new UIText("paste", mc_[5], fs_[1], middle_center)
                ],
                new UICol(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout)
                .OnClick(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.PasteFlipX(EditA.SelectedBone); })[
                    new UIText("flip x", mc_[6], fs_[1], middle_center)
                ],
                new UICol(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout)
                .OnClick(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.PasteFlipY(EditA.SelectedBone); })[
                    new UIText("flip y", mc_[6], fs_[1], middle_center)
                ],
                new UICol(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout)
                .OnClick(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.PasteFlipZ(EditA.SelectedBone); })[
                    new UIText("flip z", mc_[6], fs_[1], middle_center)
                ],
                new UICol(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout)
                .OnClick(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.PasteFlipXY(EditA.SelectedBone); })[
                    new UIText("flip x/y", mc_[8], fs_[1], middle_center)
                ],
                new UICol(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout)
                .OnClick(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.PasteFlipYZ(EditA.SelectedBone); })[
                    new UIText("flip y/z", mc_[8], fs_[1], middle_center)
                ],
                new UICol(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout)
                .OnClick(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.PasteFlipXZ(EditA.SelectedBone); })[
                    new UIText("flip x/z", mc_[8], fs_[1], middle_center)
                ],
                new UICol(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout)
                .OnClick(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.PasteFlipXYZ(EditA.SelectedBone); })[
                    new UIText("flip x/y/z", mc_[10], fs_[1], middle_center)
                ]
            ].Ref(ref AnimationEdit),
            new UICol(w_minus_[50f, 0], h_full, hidden, top_left)[
                new UICol(w_[3], h_full, blank_full_g_[30], left_[1.5f], top_right)[
                    new UIImg(w_[20], h_[50], blank_sharp_g_[20], border_ui_[2, 2, 2, 2], border_color_g_[30], middle_center)
                    .OnHover(_ => HoveringCenter = false)
                    .OnClick(img => img.Dataset["left"] = TextureEditorSlider.Width.Value * TextureEditorSlider.ParentElement!.Size.X)
                    .OnHold(img => {
                        HoveringCenter = false;
                        var mouseDelta = Input.GetMouseDelta().X;
                        var center = TextureEditorSlider.ParentElement;
                        var slider = TextureEditorSlider;
                        if (mouseDelta != 0 && center != null)
                        {
                            var left = img.Dataset.Float("left");
                            left += mouseDelta;
                            img.Dataset["left"] = left;
                            left = Mathf.Clampy(left, 50, center.Size.X - 50);
                            var percent = left / center.Size.X;
                            slider.Width.Value = percent;
                            center.ApplyChanges(UIChange.Scale);

                            Editor.textureEditor.SeparationPercent = percent;
                            Editor.textureEditor.Resize();
                        }
                    })
                ]
            ].Ref(ref TextureEditorSlider)
        ],
        new UICol(w_[200], blank_full_g_[BASE_BACKGROUND], h_full_minus_[50], bottom_right, border_ui_[2, 0, 0, 0], border_color_g_[BASE_BORDER])
        .OnClick(_ => Editor.ClickedMenu = true)[
            new UICol(w_full, h_full, top_[5])[
                new UIVCol(w_full, grow_children, spacing_[5])[
                    new UIText("Properties", mc_[10], fs_[1.2f], left_[5]),
                    XYZField("Transform", Transform, out _modelingSetTransform),
                    XYZField("Scale", Scale, out _modelingSetScale),
                    XYZField("Rotation", Rotation, out _modelingSetRotation),
                    new UIText("Mesh", mc_[4], fs_[1.2f], left_[5]),
                    new UICol(w_full_minus_[10], h_[30], left_[5])[
                        new UICol(w_[32f], top_left, h_full, blank_sharp_g_[BASE_BUTTON])
                        .OnClick(_ => Editor.modelingEditor.SwitchSelection(RenderType.Vertex))[
                            new UIImg(texture_[97], w_[30], h_[30], middle_center, slice_null, rgb_[1, 1, 1])
                        ],
                        new UICol(w_[32f], top_center, h_full, blank_sharp_g_[BASE_BUTTON])
                        .OnClick(_ => Editor.modelingEditor.SwitchSelection(RenderType.Edge))[
                            new UIImg(texture_[98], w_[30], h_[30], middle_center, slice_null, rgb_[1, 1, 1])
                        ],
                        new UICol(w_[32f], top_right, h_full, blank_sharp_g_[BASE_BUTTON])
                        .OnClick(_ => Editor.modelingEditor.SwitchSelection(RenderType.Face))[
                            new UIImg(texture_[99], w_[30], h_[30], middle_center, slice_null, rgb_[1, 1, 1])
                        ]
                    ]
                ]
            ].Ref(ref ModelingRightPanel),
            new UICol(w_full, h_full, top_[5], hidden)[
                new UIVCol(w_full, grow_children, spacing_[5])[
                    new UIText("Properties", mc_[10], fs_[1.2f], left_[5]),
                    XYZField("Transform", Transform, out _riggingSetTransform),
                    XYZField("Scale", Scale, out _riggingSetScale),
                    XYZField("Rotation", Rotation, out _riggingSetRotation),
                    new UIText("Mesh", mc_[4], fs_[1.2f], left_[5])
                ]
            ].Ref(ref RiggingRightPanel),
            new UICol(w_full, h_full, top_[5], hidden)[
                new UIVCol(w_full, grow_children, spacing_[5])[
                    new UIText("Properties", mc_[10], fs_[1.2f], left_[5]),
                    XYZField("Transform", Transform, out _animationSetTransform),
                    XYZField("Scale", Scale, out _animationSetScale),
                    XYZField("Rotation", Rotation, out _animationSetRotation)
                ]
            ].Ref(ref AnimationRightPanel),
            new UIVCol(w_full_minus_[2], top_right, h_full, hidden)[
                new UICol(w_full, h_[30])[
                    new UIText("FILE", mc_[4], middle_left, left_[5], fs_[1.2f])
                ],
                new UIVCol(w_full, grow_children, border_[5, 5, 5, 5], spacing_[5], blank_full_g_[15])[
                    new UICol(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected)
                    .OnClick(c => {
                        if (Editor.textureEditor.CurrentFilePath != null)
                        {
                            Editor.textureEditor.SaveTexture();
                        }
                        else
                        {
                            Editor.FileManager.SetAction(FileManagerType.Export);
                            Editor.FileManager.ToggleOn();
                            Editor.FileManager.FileType = ".png";
                            Editor.FileManager.SaveFile = Editor.textureEditor.SaveTexture;
                        } 
                    })[
                        new UIText("Save", mc_[4], middle_center, fs_[1.2f])
                    ],
                    new UICol(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected)
                    .OnClick(c => {
                        Editor.FileManager.SetAction(FileManagerType.Export);
                        Editor.FileManager.ToggleOn();
                        Editor.FileManager.FileType = ".png";
                        Editor.FileManager.SaveFile = Editor.textureEditor.SaveTexture;
                    })[
                        new UIText("Export", mc_[6], middle_center, fs_[1.2f])
                    ],
                    new UICol(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected)
                    .OnClick(c => {
                        Editor.FileManager.SetAction(FileManagerType.Import);
                        Editor.FileManager.ToggleOn();
                    })[
                        new UIText("Import", mc_[6], middle_center, fs_[1.2f])
                    ]
                ],
                new UIImg(w_full, h_[2], blank_full_g_[BASE_BORDER]),
                // New texture section
                new UICol(w_full, h_[30])[
                    new UIText("TEXTURE", middle_left, left_[5], fs_[1.2f])
                ],
                new UICol(w_full_minus_[10], h_[25], top_center)[
                    new UIText("Width", fs_[1], middle_left),
                    new UICol(w_[70], h_full, top_right, blank_sharp_g_[10])[
                        new UIField("100", mc_[5], fs_[1], text_align_right, middle_right, right_[5]).Ref(ref TextureWidthField)
                    ]
                ],
                new UICol(w_full_minus_[10], h_[25], top_center, top_[5])[
                    new UIText("Height", fs_[1], middle_left),
                    new UICol(w_[70], h_full, top_right, blank_sharp_g_[10])[
                        new UIField("100", mc_[5], fs_[1], text_align_right, middle_right, right_[5]).Ref(ref TextureHeightField)
                    ]
                ],
                new UICol(w_full_minus_[10], h_[25], top_center, blank_sharp_g_[30], top_[5])
                .OnClick(_ => DrawingPanel.Renew(TextureWidthField.GetInt(100), TextureHeightField.GetInt(100)))[
                    new UIText("Create", middle_center)
                ],
                new UIImg(w_full, h_[2], blank_full_g_[BASE_BORDER], top_[5]),
                new UICol(w_full, h_[30])[
                    new UIText("UV SCALING", mc_[10], middle_left, left_[5], fs_[1.2f])
                ],
                new UICol(w_full_minus_[10], h_[25], top_center)[
                    new UIText("Pixel Size", fs_[1], middle_left),
                    new UICol(w_[70], h_full, top_right, blank_sharp_g_[10])[
                        new UIField("1", mc_[10], fs_[1], text_align_right, middle_right, right_[5]).Ref(ref MeshUnitsField)
                    ]
                ],
                new UICol(w_full_minus_[10], h_[25], top_center, blank_sharp_g_[30], top_[5])
                .OnClick(_ => Editor.textureEditor.Handle_PixelMapping(MeshUnitsField.GetFloat(1f)))[
                    new UIText("Scale", mc_[5], fs_[1], middle_center)
                ]
                
            ].Ref(ref TextureRightPanel),
            new UICol(w_full_minus_[10], h_[25], bottom_[95], bottom_center, blank_sharp_g_[BASE_BUTTON])[
                new UIText("Culling", mc_[7], fs_[1], middle_left, left_[5]),
                new UIButton(w_[15], h_[15], blank_round_g_[ModelSettings.BackfaceCulling ? 60 : 20], right_[5], middle_right).OnClick(i => Toggle(i, ref ModelSettings.BackfaceCulling))
            ],
            new UICol(w_full_minus_[10], h_[25], bottom_[65], bottom_center, blank_sharp_g_[BASE_BUTTON])[
                new UIText("Wireframe", mc_[9], fs_[1], middle_left, left_[5]),
                new UIButton(w_[15], h_[15], blank_round_g_[ModelSettings.WireframeVisible ? 60 : 20], right_[5], middle_right).OnClick(i => Toggle(i, ref ModelSettings.WireframeVisible))
            ],
            new UICol(w_full_minus_[10], h_[25], bottom_[35], bottom_center)[
                new UICol(w_[60f], h_full, blank_sharp_g_[BASE_BUTTON])
                .OnClick(_ => Game.SetCursorState(CursorMode.Disabled))
                .OnHold(AlphaHold)
                .OnRelease(_ => Game.SetCursorState(CursorMode.Normal))[
                    new UIText("Alpha", mc_[5], fs_[1], middle_center)
                ],
                new UICol(w_[70], h_full, middle_right, blank_sharp_g_[10])[
                    new UIField("1", mc_[6], fs_[1], middle_right, text_align_right, right_[5])
                ]
            ],
            new UICol(w_full_minus_[10], h_[25], bottom_[5], bottom_center)[
                new UIText("Camera Speed", mc_[12], fs_[1], middle_left),
                new UICol(w_[70], h_full, top_right, blank_sharp_g_[10])[
                    new UIField("75", mc_[5], fs_[1], text_align_right, middle_right, right_[5])
                    .OnTextChange(i => Editor.Scene.DefaultCamera.SetCameraSpeed(i.GetFloat()))
                ]
            ]
        ]
    ];

    private UIElementBase GenerateColorPickers() =>
    new UIHCol(w_full_minus_[5], h_[30], spacing_[5], border_[0, 0, 0, 0])[
    Forloop(0, 5, (j) =>
        new UIImg(blank_full_g_[100], w_minus_[20f, 5], h_[30], border_ui_[3, 3, 3, 3], border_color_[(0, 0, 0, 0)], hover_scale_[1.2f], hover_scale_duration_[0.2f], hover_scale_easeout).OnClick(image => {
            var vcol = image.ParentElement?.ParentElement;
            if (vcol != null)
            {
                if (vcol.Dataset.ContainsKey("img"))
                {
                    var img = vcol.Dataset.Get<UIImg>("img");
                    img?.UpdateBorderColor(new Vector4(0f));
                }
                vcol.Dataset["img"] = image;
            }

            image.UpdateBorderColor(new Vector4(1f));

            if (Editor.textureEditor.ColorPicker.Transform.Disabled)
                Editor.textureEditor.ColorPicker.Transform.Disabled = false;

            Editor.textureEditor.ColorPicker.SetColorAction = color => {
                image.UpdateColor(new Vector4(color.Xyz, 1f));
                DrawingPanel.BrushColor = new Vector4(color.Xyz, 1f);
            };
            DrawingPanel.BrushColor = new Vector4(image.Color.Xyz, 1f);
        })
    )
    ];
    
    private static void TextureDrawingModeButton(UICol c, DrawingMode mode)
    {
        DrawingPanel.SetDrawingMode(mode); 
        if (c.ParentElement != null && c.ParentElement is UIVCol vcol)
        {
            for (int i = 0; i < vcol.ChildElements.Count; i++)
            {
                var button = vcol.ChildElements[i];
                if (button.IsSelected)
                {
                    button.IsSelected = false;
                    button.HoverExit();
                }
            }
        }
        c.IsSelected = true; 
    }

    private UIElementBase XYZField(string name, Action<int, float> action, out Action<float, float, float> setAction)
    {
        var col = new UIVCol(w_full_minus_[10], grow_children, spacing_[5], left_[5], blank_sharp_g_[BASE_BUTTON], border_[3, 3, 3, 3])[
            new UIText(name, mc_[name.Length], fs_[1]),
            new UICol(w_full_minus_[6], h_[15])[
                XYZLabel("X", top_left),
                XYZLabel("Y", top_center),
                XYZLabel("Z", top_right)
            ],
            new UICol(w_full_minus_[6], h_[25])[
                XYZField(0, top_left, 0, action, out var xField),
                XYZField(0, top_center, 1, action, out var yField),
                XYZField(0, top_right, 2, action, out var zField)
            ]
        ];
        setAction = (x, y, z) => SetValue(x, y, z, xField, yField, zField);
        return col;
    }
    

    private UICol XYZLabel(string label, IStyleData alignment) =>
    new UICol(w_[32f], h_full, alignment)[new UIText(label, mc_[1], fs_[1], bottom_left, left_[3])];

    private UIHScroll XYZField(float value, IStyleData alignment, int index, Action<int, float> action, out UIField field)
    {
        field = new UIField("" + value, mc_[20], fs_[0.9f], middle_left, left_[3]);
        field.SetOnTextChange(f => action(index, f.GetFloat()));
        return new UIHScroll(w_[32f], h_full, blank_sharp_g_[10], mask_children, alignment)[
            field
        ];
    }

    public void SetTransform(float x, float y, float z) => _setTransform(x, y, z);
    public void SetTransform(Vector3 position) => _setTransform(position.X, position.Y, position.Z);
    public void SetScale(float x, float y, float z) => _setScale(x, y, z);
    public void SetScale(Vector3 scale) => _setScale(scale.X, scale.Y, scale.Z);
    public void SetRotation(float x, float y, float z) => _setRotation(x, y, z);
    public void SetRotation(Vector3 rotation) => _setRotation(rotation.X, rotation.Y, rotation.Z);

    public void SetTransformAction(Action<int, float> action) => _transformAction = action;
    public void SetScaleAction(Action<int, float> action) => _scaleAction = action;
    public void SetRotationAction(Action<int, float> action) => _rotationAction = action;

    private void Transform(int index, float value) => _transformAction(index, value);
    private void Scale(int index, float value) =>     _scaleAction(index, value);
    private void Rotation(int index, float value) => _rotationAction(index, value);
    
    private static void SetValue(float x, float y, float z, UIField xField, UIField yField, UIField zField)
    {
        xField.UpdateText(""+x);
        yField.UpdateText(""+y);
        zField.UpdateText(""+z);
    }

    private void SwitchScene(string e)
    {
        if (e == CurrentEditor)
            return;

        SetVisibleCurrentEditor(CurrentEditor, false);
        SetVisibleCurrentEditor(e, true);
        switch (e)
        {
            case "Modeling":
                _setTransform = _modelingSetTransform;
                _setScale = _modelingSetScale;
                _setRotation = _modelingSetRotation;
                Editor.DoSwitchScene(Editor.modelingEditor);
                break;
            case "Rigging":
                _setTransform = _riggingSetTransform;
                _setScale = _riggingSetScale;
                _setRotation = _riggingSetRotation;
                Editor.DoSwitchScene(Editor.riggingEditor);
                break;
            case "Animation":
                _setTransform = _animationSetTransform;
                _setScale = _animationSetScale;
                _setRotation = _animationSetRotation;
                Editor.DoSwitchScene(Editor.animationEditor);
                break;
            case "Texture":
                Editor.DoSwitchScene(Editor.textureEditor);
                break;
        }
        CurrentEditor = e;
    }

    private void SetVisibleCurrentEditor(string editor, bool visible)
    {
        switch (editor)
        {
            case "Modeling":
                ForeachModeling(e => e.SetVisible(visible));
                break;
            case "Rigging":
                ForeachRigging(e => e.SetVisible(visible));
                break;
            case "Animation":
                ForeachAnimation(e => e.SetVisible(visible));
                break;
            case "Texture":
                ForeachTexture(e => e.SetVisible(visible));
                break;
        }
    }

    private static void Toggle(UIButton button, ref bool value)
    {
        value = !value;
        button.Color = value ? (0.6f, 0.6f, 0.6f, 1) : (0.2f, 0.2f, 0.2f, 1);
        button.UpdateColor();
    }
    
    private static void Toggle(UIButton button, string axis, Func<string, bool> action)
    {
        button.Color = action(axis) ? (0.6f, 0.6f, 0.6f, 1) : (0.2f, 0.2f, 0.2f, 1);
        button.UpdateColor();
    }

    private static void AlphaHold(UICol col)
    {
        var field = col.ParentElement?.QueryElement<UIField>();
        if (field != null)
        {
            var mouseDelta = Input.GetMouseDelta();
            if (mouseDelta.X == 0) return;
            var value = field.GetFloat() + mouseDelta.X * 0.001f;
            value = Mathf.Clampy(value, 0, 1);
            ModelSettings.MeshAlpha = value;
            field.UpdateText(value.ToString());
        }
    }

    private static void GridHold(UICol col)
    {
        var field = col.ParentElement?.QueryElement<UIField>();
        if (field != null)
        {
            var mouseDelta = Input.GetMouseDelta();
            if (mouseDelta.X == 0) return;
            var value = field.GetFloat() + mouseDelta.X * 0.001f;
            value = Mathf.Max(0, value);
            ModelSettings.SnappingFactor = value;
            field.UpdateText(value.ToString());
        }
    }

    private static bool SwitchMirror(string axis)
    {
        var mirror = ModelSettings.Mirror;
        switch (axis)
        {
            case "X":
                ModelSettings.Mirror = (mirror.X == 0 ? 1 : 0, mirror.Y, mirror.Z);
                return mirror.X == 1;
            case "Y":
                ModelSettings.Mirror = (mirror.X, mirror.Y == 0 ? 1 : 0, mirror.Z);
                return mirror.Y == 1;
            case "Z":
                ModelSettings.Mirror = (mirror.X, mirror.Y, mirror.Z == 0 ? 1 : 0);
                return mirror.Z == 1;
        }
        return false;
    }

    private static bool SwitchAxis(string axis)
    {
        switch (axis)
        {
            case "X":
                ModelSettings.Axis.X = ModelSettings.Axis.X == 0 ? 1 : 0;
                return ModelSettings.Axis.X == 1;
            case "Y":
                ModelSettings.Axis.Y = ModelSettings.Axis.Y == 0 ? 1 : 0;
                return ModelSettings.Axis.Y == 1;
            case "Z":
                ModelSettings.Axis.Z = ModelSettings.Axis.Z == 0 ? 1 : 0;
                return ModelSettings.Axis.Z == 1;
        }
        return false;
    }



    public void Update()
    {
        if (GameTime.FpsUpdated)
        {
            FpsText.UpdateText("Fps: " + GameTime.Fps);
            RamText.UpdateText($"Ram: {GameTime.Ram / (1024 * 1024)} Mb");
        }
    }
    
    private void ForeachModeling(Action<UIElementBase> element) { for (int i = 0; i < ModelingElements.Length; i++) element(ModelingElements[i]); }
    private void ForeachRigging(Action<UIElementBase> element) { for (int i = 0; i < RiggingElements.Length; i++) element(RiggingElements[i]); }
    private void ForeachAnimation(Action<UIElementBase> element) { for (int i = 0; i < AnimationElements.Length; i++) element(AnimationElements[i]); }
    private void ForeachTexture(Action<UIElementBase> element) { for (int i = 0; i < TextureElements.Length; i++) element(TextureElements[i]); }

    public UIElementBase GetModelButton(Model model)
    {
        var col = new UICol(w_full_minus_[10], h_[30], blank_sharp, rgba_v4_[model.IsSelected ? (0.3f, 0.3f, 0.3f, 1f) : (0f, 0f, 0f, 0f)])
        .OnHoverEnter(c => c.UpdateColor((0.3f, 0.3f, 0.3f, 1f)))
        .OnHoverExit(c =>
        {
            if (!model.IsSelected)
                c.UpdateColor((0f, 0f, 0f, 0f));
        });
        col.AddElements(
            new UIButton(w_full_minus_[70], h_full).OnClick(_ =>
            {
                if (!model.IsSelected)
                {
                    ModelManager.Select(model);
                }
                else
                {
                    ModelManager.UnSelect(model);
                }
            }),
            new UIText(model.Name, mc_[model.Name.Length], fs_[1f], middle_left, left_[5]),
            new UIHCol(w_[64], h_[16], middle_right, spacing_[5])[
                new UICol(w_[16], h_[16], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout)
                .OnClick(c =>
                {
                    model.IsShown = !model.IsShown;
                    if (!model.IsShown && model.IsSelected)
                    {
                        model.IsSelected = false;
                        col.UpdateColor((0f, 0f, 0f, 0f));
                    }
                    c.GetElement<UIImg>()?.UpdateIconIndex(model.IsShown ? 22 : 23);
                    var icons = c.ParentElement?.QueryElements<UIImg>();
                    if (icons != null)
                    {
                        for (int i = 0; i < icons.Count; i++)
                        {
                            icons[i].UpdateColor(model.IsShown ? (1f, 1f, 1f, 1f) : (0.5f, 0.5f, 0.5f, 1f));
                        }
                    }
                    col.GetElement<UIText>()?.UpdateColor(model.IsShown ? (1f, 1f, 1f, 1f) : (0.5f, 0.5f, 0.5f, 1f));
                })[
                    new UIImg(w_full, h_full, icon_[22], bg_white)
                ],
                new UICol(w_[16], h_[16], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout)
                .OnClick(c =>
                {
                    var icon = c.GetElement<UIImg>();
                    if (icon != null)
                    {
                        icon.UpdateIconIndex(icon.TextureID == (41 | 0x20000000) ? 40 : 41);
                    }
                })[
                    new UIImg(w_full, h_full, icon_[41], bg_white)
                ],
                new UICol(w_[16], h_[16], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout)
                .OnClick(c =>
                {
                    col.Delete();
                    model.Delete();
                    Hierarchy.QueueAlign();
                    Hierarchy.QueueUpdateTransformation();
                })[
                    new UIImg(w_full, h_full, icon_[18], bg_white)
                ]
            ]
        );
        return col;
    }

    public UIElementBase GetModelButton(PBG_Model model)
    {
        var col = new UICol(w_full_minus_[10], h_[30], blank_sharp, rgba_v4_[model.IsSelected ? (0.3f, 0.3f, 0.3f, 1f) : (0f, 0f, 0f, 0f)])
        .OnHoverEnter(c => c.UpdateColor((0.3f, 0.3f, 0.3f, 1f)))
        .OnHoverExit(c =>
        {
            if (!model.IsSelected)
                c.UpdateColor((0f, 0f, 0f, 0f));
        });
        col.AddElements(
            new UIButton(w_full_minus_[70], h_full).OnClick(_ =>
            {
                if (!model.IsSelected)
                {
                    PBG_Model.Select(model);
                }
                else
                {
                    PBG_Model.UnSelect(model);
                }
            }),
            new UIText(model.Name, mc_[model.Name.Length], fs_[1f], middle_left, left_[5]),
            new UIHCol(w_[64], h_[16], middle_right, spacing_[5])[
                new UICol(w_[16], h_[16], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout)
                .OnClick(c =>
                {
                    model.IsVisible = !model.IsVisible;
                    if (!model.IsVisible && model.IsSelected)
                    {
                        model.IsSelected = false;
                        col.UpdateColor((0f, 0f, 0f, 0f));
                    }
                    c.GetElement<UIImg>()?.UpdateIconIndex(model.IsVisible ? 22 : 23);
                    var icons = c.ParentElement?.QueryElements<UIImg>();
                    if (icons != null)
                    {
                        for (int i = 0; i < icons.Count; i++)
                        {
                            icons[i].UpdateColor(model.IsVisible ? (1f, 1f, 1f, 1f) : (0.5f, 0.5f, 0.5f, 1f));
                        }
                    }
                    col.GetElement<UIText>()?.UpdateColor(model.IsVisible ? (1f, 1f, 1f, 1f) : (0.5f, 0.5f, 0.5f, 1f));
                })[
                    new UIImg(w_full, h_full, icon_[22], bg_white)
                ],
                new UICol(w_[16], h_[16], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout)
                .OnClick(c =>
                {
                    var icon = c.GetElement<UIImg>();
                    if (icon != null)
                    {
                        icon.UpdateIconIndex(icon.TextureID == (41 | 0x20000000) ? 40 : 41);
                    }
                })[
                    new UIImg(w_full, h_full, icon_[41], bg_white)
                ],
                new UICol(w_[16], h_[16], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout)
                .OnClick(c =>
                {
                    col.Delete();
                    model.Delete();
                    Hierarchy.QueueAlign();
                    Hierarchy.QueueUpdateTransformation();
                })[
                    new UIImg(w_full, h_full, icon_[18], bg_white)
                ]
            ]
        );
        return col;
    }

    public void RegenerateAnimationButtons(Model? model)
    {
        AnimationHierarchy.DeleteChildren();
        if (model != null)
        {
            model.Animation = null;
            foreach (var (id, animation) in model.Animations)
            {
                var button = GetAnimationButton(model, id, animation, false);
                AnimationHierarchy.AddElement(button);
                UIController.AddElement(button);
            }
        }
        AnimationHierarchy.QueueAlign();
        AnimationHierarchy.QueueUpdateScaling();
        AnimationHierarchy.QueueUpdateTransformation();
    }

    public UIElementBase GetAnimationButton(Model model)
    {
        for (int i = 0; i < AnimationHierarchy.ChildElements.Count; i++)
        {
            var child = AnimationHierarchy.ChildElements[i];
            child.GetElement("settings")?.SetVisible(false);
            child.UpdateColor((0.2f, 0.2f, 0.2f, 1f));
            child.ApplyChanges(UIChange.Scale);
        }

        model.AddAnimation(out var id, out var animation);
        Editor.animationEditor.GenerateAnimationTimeline(model);

        return GetAnimationButton(model, id, animation);
    }

    public UIElementBase GetAnimationButton(Model model, int id, NewAnimation animation, bool selected = true)
    {
        var col = new UICol(w_full_minus_[10], blank_full_g_[selected ? 30 : 20], grow_children, data_["ID", id], ignore_invisible)
        .OnHoverEnter(c =>
        {
            c.UpdateColor((0.3f, 0.3f, 0.3f, 1f));
        })
        .OnHoverExit(c =>
        {
            if (model.Animation != animation)
            {
                c.UpdateColor((0.2f, 0.2f, 0.2f, 1f));
            }
        });
        col.AddElements(
            new UICol(w_full, h_[30])[
                new UIButton($"button_{id}", w_full, h_full).OnClick(b =>
                {
                    int? ID = model.Animation?.ID ?? null;

                    for (int i = 0; i < AnimationHierarchy.ChildElements.Count; i++)
                    {
                        var child = AnimationHierarchy.ChildElements[i];
                        if (!child.Hovering && child.Dataset.Int("ID") == ID)
                        {
                            child.UpdateColor((0.2f, 0.2f, 0.2f, 1f));
                            child.GetElement("settings")?.SetVisible(false);
                            child.ApplyChanges(UIChange.Scale);
                        }
                    }

                    model.Animation = animation;
                    Editor.animationEditor.GenerateAnimationTimeline(model);

                    col.GetElement("settings")?.SetVisible(true);
                    col.ApplyChanges(UIChange.Scale);

                    AnimationHierarchy.QueueAlign();
                    AnimationHierarchy.QueueUpdateTransformation();
                    AnimationHierarchy.QueueUpdateScaling();
                }),
                new UIText(animation.Name, mc_[22], fs_[1f], middle_left, left_[5]),
                new UICol(w_[16], h_[16], middle_right, right_[5], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout)
                .OnClick(c =>
                {
                    col.Delete();
                    model.DeleteAnimation(id);
                    AnimationHierarchy.QueueAlign();
                    AnimationHierarchy.QueueUpdateTransformation();
                })[
                    new UIImg(w_full, h_full, icon_[18], bg_white)
                ]
            ],
            new UICol("settings", w_full, h_[30], blank_full_g_[20], top_[30], hidden)[
                new UICol(w_full_minus_[10], h_[20], top_[5], top_center, blank_full_g_[10])[
                    new UIField(animation.Name, mc_[22], middle_left, left_[3]).OnTextChange(f =>
                    {
                        animation.Name = f.GetText();
                        col.QueryElement<UIText>()?.UpdateText(animation.Name);
                    })
                ]
            ]
        );          
        return col;
    }
}