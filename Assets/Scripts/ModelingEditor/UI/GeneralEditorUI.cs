using PBG;
using PBG.Core;
using PBG.Data;
using PBG.MathLibrary;
using PBG.Modeling;
using PBG.UI;
using PBG.UI.Creator;
using PBG.UI.FileManager;
using Silk.NET.Input;
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
    new UICol(Class(w_full, h_full), Sub([
        new UICol("left-side-panel", Class(w_[200], blank_full_g_[BASE_BACKGROUND], h_full_minus_[50], bottom_left, border_ui_[0, 0, 2, 0], border_color_g_[BASE_BORDER]),
        OnClickCol(_ => Editor.ClickedMenu = true),
        Sub([
            newVCol(Class(w_full, h_full, spacing_[5]), Sub(
                new UICol(Class(w_full, h_[30]), Sub(
                    new UICol(Class(w_half_minus_[7.5f], left_[5], h_full_minus_[10], top_[5], blank_sharp),
                    OnHoverEnterCol(c => c.UpdateColor((0.3f, 0.3f, 0.3f, 1))),
                    OnHoverExitCol(c => c.UpdateColor((0.0f, 0.0f, 0.0f, 0f))),
                    OnClickCol(_ => Editor.modelingEditor.SwitchMode(Editor.modelingEditor.EditingMode)),
                    Sub(
                        new UIText("EDIT", Class(mc_[4], fs_[1f], middle_center))
                    )),
                    new UICol(Class(w_half_minus_[7.5f], right_[5], h_full_minus_[10], top_[5], blank_sharp, top_right),
                    OnHoverEnterCol(c => c.UpdateColor((0.3f, 0.3f, 0.3f, 1))),
                    OnHoverExitCol(c => c.UpdateColor((0.0f, 0.0f, 0.0f, 0f))),
                    OnClickCol(_ => Editor.modelingEditor.SwitchMode(Editor.modelingEditor.SelectionMode)),
                    Sub(
                        new UIText("SELECT", Class(mc_[6], fs_[1f], middle_center))
                    ))
                )),
                new UICol(Class(h_[20], w_full_minus_[10], top_center), Sub(
                    new UIText("Hierarchy", Class(mc_[9], fs_[1.2f], middle_left)),
                    new UICol(Class(w_[20], h_[20], middle_right, right_[25], blank_full_g_[BASE_BACKGROUND], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                    OnClickCol(_ => {
                        Editor.FileManager.SetAction(FileManagerType.Export);
                        Editor.FileManager.ToggleOn();
                        Editor.FileManager.FileType = ".model";
                        Editor.FileManager.SaveFile = Editor.modelingEditor.SaveModel;
                    }),
                    Sub(
                        new UIImg(Class(w_[20], h_[20], icon_[42], middle_center, bg_white))
                    )),
                    new UICol(Class(w_[20], h_[20], middle_right, blank_full_g_[BASE_BACKGROUND], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                    OnClickCol(_ =>
                    {
                        Editor.FileManager.SetAction(FileManagerType.Import);
                        Editor.FileManager.ToggleOn();
                    }),
                    Sub(
                        new UIText("+", Class(mc_[1], fs_[2f], middle_center))
                    ))
                )),
                newVScroll(Class(w_full_minus_[4], top_center, h_half_minus_[30], border_[5, 5, 5, 5], spacing_[5], blank_sharp_g_[10]), Sub(

                ), ref Hierarchy)
            ), ref ModelingLeftPanel),
            newVCol(Class(w_full, h_full, spacing_[5], top_[5], hidden), Sub(
                new UICol(Class(h_[20], w_full_minus_[10], top_center), Sub(
                    new UIText("Rig", Class(mc_[3], fs_[1.2f], middle_left)),
                    new UICol(Class(w_[20], h_[20], middle_right, right_[25], blank_full_g_[BASE_BACKGROUND]),
                    OnClickCol(_ => {
                        Editor.FileManager.SetAction(FileManagerType.Export);
                        Editor.SetFileManagerExportAsModel();
                        Editor.FileManager.ToggleOn();
                    }),
                    Sub(
                        new UIImg(Class(w_[20], h_[20], icon_[42], middle_center, bg_white))
                    )),
                    new UICol(Class(w_[20], h_[20], middle_right, blank_full_g_[BASE_BACKGROUND]),
                    OnClickCol(_ =>
                    {
                        Editor.FileManager.SetAction(FileManagerType.Import);
                        Editor.FileManager.ToggleOn();
                    }),
                    Sub(
                        new UIText("+", Class(mc_[1], fs_[2f], middle_center))
                    ))
                ))
            ), ref RiggingLeftPanel),
            newVCol(Class(w_full, h_full, spacing_[5], top_[5], hidden), Sub(
                new UICol(Class(h_[20], w_full_minus_[10], top_center), Sub(
                    new UIText("Animation", Class(mc_[9], fs_[1.2f], middle_left)),
                    new UICol(Class(w_[20], h_[20], middle_right, right_[25], blank_full_g_[BASE_BACKGROUND]),
                    OnClickCol(_ => {
                        Editor.FileManager.SetAction(FileManagerType.Export);
                        Editor.SetFileManagerExportAsModel();
                        Editor.FileManager.ToggleOn();
                    }),
                    Sub(
                        new UIImg(Class(w_[20], h_[20], icon_[42], middle_center, bg_white))
                    )),
                    new UICol(Class(w_[20], h_[20], middle_right, blank_full_g_[BASE_BACKGROUND]),
                    OnClickCol(_ =>
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
                    }),
                    Sub(
                        new UIText("+", Class(mc_[1], fs_[2f], middle_center))
                    ))
                )),
                newVScroll(Class(w_full_minus_[4], top_center, h_half_minus_[30], border_[5, 5, 5, 5], spacing_[5], blank_full_g_[15]), Sub(

                ), ref AnimationHierarchy)
            ), ref AnimationLeftPanel),
            newVScroll(Class(w_full_minus_[2], h_full_minus_[30], mask_children, hidden), Sub([
                new UICol(Class(w_full, h_[30]), Sub(
                    new UIText("TOOLS", Class(mc_[5], middle_left, left_[5], fs_[1.2f]))
                )),
                new UIVCol(Class(w_full, grow_children, border_[5, 5, 5, 5], spacing_[5], blank_full_g_[15]), Sub(
                    new UICol(Class(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected),
                    OnClickCol(c => TextureDrawingModeButton(c, DrawingMode.None)),
                    Sub(
                        new UIImg(Class(w_[26], h_[26], icon_[15], middle_left, left_[5], bg_white)),
                        new UIText("None", Class(mc_[5], middle_left, left_[40], fs_[1.2f]))
                    )),
                    new UICol(Class(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected),
                    OnClickCol(c => TextureDrawingModeButton(c, DrawingMode.Move)),
                    Sub(
                        new UIImg(Class(w_[26], h_[26], icon_[45], middle_left, left_[5], bg_white)),
                        new UIText("Move", Class(mc_[4], middle_left, left_[40], fs_[1.2f]))
                    )),
                    new UICol(Class(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected),
                    OnClickCol(c => TextureDrawingModeButton(c, DrawingMode.Brush)),
                    Sub(
                        new UIImg(Class(w_[26], h_[26], icon_[43], middle_left, left_[5], bg_white)),
                        new UIText("Brush", Class(mc_[5], middle_left, left_[40], fs_[1.2f]))
                    )),
                    new UICol(Class(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected),
                    OnClickCol(c => TextureDrawingModeButton(c, DrawingMode.Pencil)),
                    Sub(
                        new UIImg(Class(w_[26], h_[26], icon_[48], middle_left, left_[5], bg_white)),
                        new UIText("Pencil", Class(middle_left, left_[40], fs_[1.2f]))
                    )),
                    new UICol(Class(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected),
                    OnClickCol(c => TextureDrawingModeButton(c, DrawingMode.Eraser)),
                    Sub(
                        new UIImg(Class(w_[26], h_[26], icon_[44], middle_left, left_[5], bg_white)),
                        new UIText("Eraser", Class(mc_[6], middle_left, left_[40], fs_[1.2f]))
                    )),
                    new UICol(Class(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected),
                    OnClickCol(c => TextureDrawingModeButton(c, DrawingMode.Blur)),
                    Sub(
                        new UIImg(Class(w_[26], h_[26], icon_[46], middle_left, left_[5], bg_white)),
                        new UIText("Blur", Class(mc_[4], middle_left, left_[40], fs_[1.2f]))
                    )),
                    new UICol(Class(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected),
                    OnClickCol(c => TextureDrawingModeButton(c, DrawingMode.Pick)),
                    Sub(
                        new UIImg(Class(w_[26], h_[26], icon_[49], middle_left, left_[5], bg_white)),
                        new UIText("Pick", Class(mc_[4], middle_left, left_[40], fs_[1.2f]))
                    )),
                    new UICol(Class(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected),
                    OnClickCol(c => TextureDrawingModeButton(c, DrawingMode.Selection)),
                    Sub(
                        new UIImg(Class(w_[26], h_[26], icon_[47], middle_left, left_[5], bg_white)),
                        new UIText("Selection", Class(mc_[9], middle_left, left_[40], fs_[1.2f]))
                    ))
                )),
                new UICol(Class(w_full_minus_[10], h_[32], border_ui_[0, 2, 0, 0], border_color_g_[BASE_BORDER], top_center), Sub(
                    new UIText("COLOR", Class(mc_[5], middle_left, top_[1], fs_[1.2f])),
                    new UIImg(Class(w_[24], h_[24], middle_right, icon_[22], bg_white), OnClickImg(img => {
                        Editor.textureEditor.ColorPicker.Transform.Disabled = !Editor.textureEditor.ColorPicker.Transform.Disabled;
                        img.UpdateIconIndex(Editor.textureEditor.ColorPicker.Transform.Disabled ? 23 : 22);
                    }))
                )),
                new UIVCol("color-pickers", Class(w_full, grow_children, spacing_[5], border_[5, 5, 5, 5], blank_full_g_[15]), Sub([
                ..Forloop(0, 10, (i) => GenerateColorPickers())
                ])),
                new UICol(Class(w_full, h_[30]), [
                    new UICol(Class(w_full_minus_[6], middle_center, h_[24], blank_full_g_[25], border_ui_[2, 2, 2, 2], border_color_g_[35], hover_color_g_[25, 30], hover_color_duration_[0.2f], hover_color_easeinout),
                    OnClickCol(_ => {
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
                    }), [
                        new UIImg(Class(w_[24], h_[24], middle_center, icon_[16], bg_white))
                    ])
                ])
            ]), ref TextureLeftPanel),
            new UICol(Class(w_full_minus_[10], h_[30], bottom_center, bottom_[25]), Sub(
                newText("Fps: 0", Class(mc_[12], fs_[1], middle_left), ref FpsText)
            )),
            new UICol(Class(w_full_minus_[10], h_[30], bottom_center), Sub(
                newText("Ram: 0", Class(mc_[20], fs_[1], middle_left), ref RamText)
            ))
        ])),
        new UICol("nav-bar", Class(w_full, h_[50], blank_full_g_[BASE_BACKGROUND], border_ui_[0, 0, 0, 2], border_color_g_[BASE_BORDER]),
        OnClickCol(_ => Editor.ClickedMenu = true),
        Sub(
            new UIHCol(Class(w_full, h_full),
            Sub(
                new UICol(Class(w_[100], h_[40], left_[5], top_[5], blank_sharp_g_[BASE_BUTTON], hover_scale_[1.05f], hover_scale_duration_[0.25f], hover_scale_easeout),
                OnClickCol(_ => Scene.LoadScene("MainMenu")),
                Sub(
                    new UIText("Main Menu", Class(middle_center, mc_[9], fs_[1]))
                )),
                new UICol(Class(w_[100], h_[40], left_[5], top_[5], blank_sharp_g_[BASE_BUTTON], hover_scale_[1.05f], hover_scale_duration_[0.25f], hover_scale_easeout),
                OnClickCol(_ => SwitchScene("Modeling")),
                Sub(
                    new UIText("Modeling", Class(middle_center, mc_[8], fs_[1]))
                )),
                new UICol(Class(w_[100], h_[40], left_[5], top_[5], blank_sharp_g_[BASE_BUTTON], hover_scale_[1.05f], hover_scale_duration_[0.25f], hover_scale_easeout),
                OnClickCol(_ => SwitchScene("Rigging")),
                Sub(
                    new UIText("Rigging", Class(middle_center, mc_[7], fs_[1]))
                )),
                new UICol(Class(w_[100], h_[40], left_[5], top_[5], blank_sharp_g_[BASE_BUTTON], hover_scale_[1.05f], hover_scale_duration_[0.25f], hover_scale_easeout),
                OnClickCol(_ => SwitchScene("Animation")),
                Sub(
                    new UIText("Animation", Class(middle_center, mc_[9], fs_[1]))
                )),
                new UICol(Class(w_[100], h_[40], left_[5], top_[5], blank_sharp_g_[BASE_BUTTON], hover_scale_[1.05f], hover_scale_duration_[0.25f], hover_scale_easeout),
                OnClickCol(_ => SwitchScene("Texture")),
                Sub(
                    new UIText("Texture", Class(middle_center, mc_[7], fs_[1]))
                ))
            ))
        )),
        new UICol("center", Class(w_full_minus_[400], left_[200], h_full_minus_[50], top_[50]),
        OnHoverCol(_ => HoveringCenter = true),
        Sub(
            newVCol(Class(grow_children, top_right, top_[10], right_[10], blank_round_g_[BASE_BACKGROUND], w_[160], spacing_[5], border_[5, 5, 5, 5], mask_children, depth_[3]),
            OnClickVCol(_ => Editor.ClickedMenu = true),
            OnHover<UIVCol>(_ => HoveringCenter = false),
            Sub(
                new UICol(Class(w_full_minus_[10], h_[25]), Sub(
                    new UICol(Class(w_[49f], h_full, blank_sharp_g_[BASE_BUTTON]), Sub(
                        new UIText("Mirror", Class(mc_[6], fs_[1], middle_center))
                    )),
                    new UICol("apply", Class(w_[49f], h_full, blank_sharp_g_[BASE_BUTTON], top_right),
                    OnHoverEnterCol(c => c.UpdateColor((0.5f, 0.5f, 0.5f, 1))),
                    OnHoverExitCol(c => c.UpdateColor((0.4f, 0.4f, 0.4f, 1))),
                    OnClickCol(_ => Editor.ApplyMirror()), Sub(
                        new UIText("Apply", Class(mc_[5], fs_[1], middle_center))
                    ))
                )),
                new UICol(Class(w_full_minus_[10], h_[25]), Sub(
                    new UICol(Class(w_[32f], h_full, blank_sharp_g_[BASE_BUTTON]), Sub(
                        new UIText("X", Class(mc_[1], fs_[1], middle_left, left_[5])),
                        new UIButton(Class(w_[15], h_[15], blank_round_g_[ModelSettings.Mirror.X == 1 ? 60 : 20], right_[5], middle_right), OnClickButton(i => Toggle(i, "X", SwitchMirror)))
                    )),
                    new UICol(Class(w_[32f], h_full, blank_sharp_g_[BASE_BUTTON], top_center), Sub(
                        new UIText("Y", Class(mc_[1], fs_[1], middle_left, left_[5])),
                        new UIButton(Class(w_[15], h_[15], blank_round_g_[ModelSettings.Mirror.Y == 1 ? 60 : 20], right_[5], middle_right), OnClickButton(i => Toggle(i, "Y", SwitchMirror)))
                    )),
                    new UICol(Class(w_[32f], h_full, blank_sharp_g_[BASE_BUTTON], top_right), Sub(
                        new UIText("Z", Class(mc_[1], fs_[1], middle_left, left_[5])),
                        new UIButton(Class(w_[15], h_[15], blank_round_g_[ModelSettings.Mirror.Z == 1 ? 60 : 20], right_[5], middle_right), OnClickButton(i => Toggle(i, "Z", SwitchMirror)))
                    ))
                )),
                new UICol(Class(w_full_minus_[10], h_[25]), Sub(
                    new UICol(Class(w_full, h_full, blank_sharp_g_[BASE_BUTTON]), Sub(
                        new UIText("Axis", Class(mc_[5], fs_[1], middle_center))
                    ))
                )),
                new UICol(Class(w_full_minus_[10], h_[25]), Sub(
                    new UICol(Class(w_[32f], h_full, blank_sharp_g_[BASE_BUTTON]), Sub(
                        new UIText("X", Class(mc_[1], fs_[1], middle_left, left_[5])),
                        new UIButton(Class(w_[15], h_[15], blank_round_g_[ModelSettings.Axis.X == 1 ? 60 : 20], right_[5], middle_right), OnClickButton(i => Toggle(i, "X", SwitchAxis)))
                    )),
                    new UICol(Class(w_[32f], h_full, blank_sharp_g_[BASE_BUTTON], top_center), Sub(
                        new UIText("Y", Class(mc_[1], fs_[1], middle_left, left_[5])),
                        new UIButton(Class(w_[15], h_[15], blank_round_g_[ModelSettings.Axis.Y == 1 ? 60 : 20], right_[5], middle_right), OnClickButton(i => Toggle(i, "Y", SwitchAxis)))
                    )),
                    new UICol(Class(w_[32f], h_full, blank_sharp_g_[BASE_BUTTON], top_right), Sub(
                        new UIText("Z", Class(mc_[1], fs_[1], middle_left, left_[5])),
                        new UIButton(Class(w_[15], h_[15], blank_round_g_[ModelSettings.Axis.Z == 1 ? 60 : 20], right_[5], middle_right), OnClickButton(i => Toggle(i, "Z", SwitchAxis)))
                    ))
                )),
                new UICol(Class(w_full_minus_[10], h_[25]), Sub(
                    new UICol(Class(w_[50f], h_full, blank_sharp_g_[BASE_BUTTON]),
                    OnClickCol(_ => Game.SetCursorState(CursorMode.Disabled)),
                    OnHold(GridHold),
                    OnReleaseCol(_ => Game.SetCursorState(CursorMode.Normal)),
                    Sub(
                        new UIText("Snap", Class(mc_[5], fs_[1], middle_left, left_[5])),
                        new UIButton(Class(w_[15], h_[15], blank_round_g_[ModelSettings.Snapping ? 60 : 20], right_[5], middle_right), OnClickButton(i => Toggle(i, ref ModelSettings.Snapping)))
                    )),
                    new UICol(Class(w_[70], h_full, middle_right, blank_sharp_g_[10]), Sub(
                        new UIField("1", Class(mc_[6], fs_[1], middle_right, text_align_right, right_[5]), OnTextChange(f => {
                            ModelSettings.SnappingFactor = f.GetFloat(0);
                        }))
                    ))
                )),
                new UICol(Class(w_full_minus_[10], h_[25], top_center, blank_sharp_g_[BASE_BUTTON]),
                Sub(
                    new UIText("Grid aligned", Class(mc_[12], fs_[1], middle_left, left_[5])),
                    new UIButton(Class(w_[15], h_[15], blank_round_g_[ModelSettings.GridAligned ? 60 : 20], right_[5], middle_right), OnClickButton(i => Toggle(i, ref ModelSettings.GridAligned)))
                )),
                new UICol(Class(w_full_minus_[10], h_[25]), Sub(
                    newCol(Class(w_[49f], h_full, blank_sharp_g_[ModelSettings.IsLocalMode ? 40 : 50]),
                    OnClickCol(c => {
                        ModelSettings.IsLocalMode = false;
                        c.UpdateColor(0.5f);
                        LocalTransformButton.UpdateColor(0.4f);
                    }),
                    Sub(
                        new UIText("World", Class(mc_[6], fs_[1], middle_center))
                    ), ref WorldTransformButton),
                    newCol(Class(w_[49f], h_full, blank_sharp_g_[!ModelSettings.IsLocalMode ? 40 : 50], top_right),
                    OnClickCol(c => {
                        ModelSettings.IsLocalMode = true;
                        c.UpdateColor(0.5f);
                        WorldTransformButton.UpdateColor(0.4f);
                    }),
                    Sub(
                        new UIText("Local", Class(mc_[5], fs_[1], middle_center))
                    ), ref LocalTransformButton)
                ))
            ), ref ModelingEdit),
            newVCol(Class(grow_children, blank_round_g_[BASE_BACKGROUND], w_[160], spacing_[5], border_[5, 5, 5, 5], mask_children, depth_[6], hidden),
            OnClickVCol(_ => Editor.ClickedMenu = true),
            OnHover<UIVCol>(_ => HoveringCenter = false),
            Sub(
                new UICol(Class(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON]), 
                OnHoldCol(_ => {
                    var delta = Input.MouseDelta;
                    if (delta == Vector2.Zero) 
                        return;

                    AnimationEdit.BaseOffset += delta;
                    AnimationEdit.ApplyChanges(UIChange.Transform);
                }),
                Sub(
                    new UIText("Bone", Class(mc_[4], fs_[1], middle_left, left_[10])),
                    new UIImg(Class(w_[20], h_[20], top_right, bg_white, icon_[15]), OnClickImg(_ => AnimationEdit.SetVisible(false)))
                )),
                new UIImg(Class(w_full_minus_[10], h_[2], blank_full_g_[BASE_BUTTON])),
                new UICol(Class(w_full_minus_[10], h_[20]), Sub(
                    new UIText("Copy", Class(mc_[4], fs_[1], middle_left))
                )),
                new UICol(Class(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout), 
                OnClickCol(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.CopyPosition(EditA.SelectedBone); }), Sub(
                    new UIText("position", Class(mc_[8], fs_[1], middle_center))
                )),
                new UICol(Class(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout), 
                OnClickCol(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.CopyRotation(EditA.SelectedBone); }), Sub(
                    new UIText("rotation", Class(mc_[8], fs_[1], middle_center))
                )),
                new UICol(Class(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout), 
                OnClickCol(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.CopyScale(EditA.SelectedBone); }), Sub(
                    new UIText("scale", Class(mc_[5], fs_[1], middle_center))
                )),
                new UIImg(Class(w_full_minus_[10], h_[2], blank_full_g_[BASE_BUTTON])),
                new UICol(Class(w_full_minus_[10], h_[20]), Sub(
                    new UIText("Paste", Class(mc_[5], fs_[1], middle_left))
                )),
                new UICol(Class(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout), 
                OnClickCol(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.Paste(EditA.SelectedBone); }), Sub(
                    new UIText("paste", Class(mc_[5], fs_[1], middle_center))
                )),
                new UICol(Class(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout), 
                OnClickCol(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.PasteFlipX(EditA.SelectedBone); }), Sub(
                    new UIText("flip x", Class(mc_[6], fs_[1], middle_center))
                )),
                new UICol(Class(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout), 
                OnClickCol(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.PasteFlipY(EditA.SelectedBone); }), Sub(
                    new UIText("flip y", Class(mc_[6], fs_[1], middle_center))
                )),
                new UICol(Class(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout), 
                OnClickCol(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.PasteFlipZ(EditA.SelectedBone); }), Sub(
                    new UIText("flip z", Class(mc_[6], fs_[1], middle_center))
                )),
                new UICol(Class(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout), 
                OnClickCol(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.PasteFlipXY(EditA.SelectedBone); }), Sub(
                    new UIText("flip x/y", Class(mc_[8], fs_[1], middle_center))
                )),
                new UICol(Class(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout), 
                OnClickCol(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.PasteFlipYZ(EditA.SelectedBone); }), Sub(
                    new UIText("flip y/z", Class(mc_[8], fs_[1], middle_center))
                )),
                new UICol(Class(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout), 
                OnClickCol(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.PasteFlipXZ(EditA.SelectedBone); }), Sub(
                    new UIText("flip x/z", Class(mc_[8], fs_[1], middle_center))
                )),
                new UICol(Class(w_full_minus_[10], h_[20], blank_sharp_g_[BASE_BUTTON], hover_color_g_[40, 50], hover_color_duration_[0.2f], hover_color_easeout), 
                OnClickCol(_ => { if (EditA.SelectedBone != null) EditA.BoneCopy.PasteFlipXYZ(EditA.SelectedBone); }), Sub(
                    new UIText("flip x/y/z", Class(mc_[10], fs_[1], middle_center))
                ))
            ), ref AnimationEdit),
            newCol(Class(w_minus_[50f, 0], h_full, hidden, top_left), [
                new UICol(Class(w_[3], h_full, blank_full_g_[30], left_[1.5f], top_right), [
                    new UIImg(Class(w_[20], h_[50], blank_sharp_g_[20], border_ui_[2, 2, 2, 2], border_color_g_[30], middle_center),
                    OnHoverImg(_ => HoveringCenter = false),
                    OnClickImg(img => img.Dataset["left"] = TextureEditorSlider.Width.Value * TextureEditorSlider.ParentElement!.Size.X),
                    OnHoldImg(img => {
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
                    }))
                ])
            ], ref TextureEditorSlider)
        )),
        new UICol("right-side-panel", Class(w_[200], blank_full_g_[BASE_BACKGROUND], h_full_minus_[50], bottom_right, border_ui_[2, 0, 0, 0], border_color_g_[BASE_BORDER]),
        OnClickCol(_ => Editor.ClickedMenu = true),
        Sub(
            newCol(Class(w_full, h_full, top_[5]), Sub(
                new UIVCol(Class(w_full, grow_children, spacing_[5]), Sub(
                    new UIText("Properties", Class(mc_[10], fs_[1.2f], left_[5])),
                    XYZField("Transform", Transform, out _modelingSetTransform),
                    XYZField("Scale", Scale, out _modelingSetScale),
                    XYZField("Rotation", Rotation, out _modelingSetRotation),
                    new UIText("Mesh", Class(mc_[4], fs_[1.2f], left_[5])),
                    new UICol(Class(w_full_minus_[10], h_[30], left_[5]), Sub(
                        new UICol(Class(w_[32f], top_left, h_full, blank_sharp_g_[BASE_BUTTON]),
                        OnClickCol(_ => Editor.modelingEditor.SwitchSelection(RenderType.Vertex)),
                        Sub(
                            new UIImg(Class(texture_[97], w_[30], h_[30], middle_center, slice_null, rgb_[1, 1, 1]))
                        )),
                        new UICol(Class(w_[32f], top_center, h_full, blank_sharp_g_[BASE_BUTTON]),
                        OnClickCol(_ => Editor.modelingEditor.SwitchSelection(RenderType.Edge)),
                        Sub(
                            new UIImg(Class(texture_[98], w_[30], h_[30], middle_center, slice_null, rgb_[1, 1, 1]))
                        )),
                        new UICol(Class(w_[32f], top_right, h_full, blank_sharp_g_[BASE_BUTTON]),
                        OnClickCol(_ => Editor.modelingEditor.SwitchSelection(RenderType.Face)),
                        Sub(
                            new UIImg(Class(texture_[99], w_[30], h_[30], middle_center, slice_null, rgb_[1, 1, 1]))
                        ))
                    ))
                ))
            ), ref ModelingRightPanel),
            newCol(Class(w_full, h_full, top_[5], hidden), Sub(
                new UIVCol(Class(w_full, grow_children, spacing_[5]), Sub(
                    new UIText("Properties", Class(mc_[10], fs_[1.2f], left_[5])),
                    XYZField("Transform", Transform, out _riggingSetTransform),
                    XYZField("Scale", Scale, out _riggingSetScale),
                    XYZField("Rotation", Rotation, out _riggingSetRotation),
                    new UIText("Mesh", Class(mc_[4], fs_[1.2f], left_[5]))
                ))
            ), ref RiggingRightPanel),
            newCol(Class(w_full, h_full, top_[5], hidden), Sub(
                new UIVCol(Class(w_full, grow_children, spacing_[5]), Sub(
                    new UIText("Properties", Class(mc_[10], fs_[1.2f], left_[5])),
                    XYZField("Transform", Transform, out _animationSetTransform),
                    XYZField("Scale", Scale, out _animationSetScale),
                    XYZField("Rotation", Rotation, out _animationSetRotation)
                ))
            ), ref AnimationRightPanel),
            newVCol(Class(w_full_minus_[2], top_right, h_full, hidden), Sub(
                new UICol(Class(w_full, h_[30]), Sub(
                    new UIText("FILE", Class(mc_[4], middle_left, left_[5], fs_[1.2f]))
                )),
                new UIVCol(Class(w_full, grow_children, border_[5, 5, 5, 5], spacing_[5], blank_full_g_[15]), Sub(
                    new UICol(Class(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected),
                    OnClickCol(c => {
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
                    }),
                    Sub(
                        new UIText("Save", Class(mc_[4], middle_center, fs_[1.2f]))
                    )),
                    new UICol(Class(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected),
                    OnClickCol(c => {
                        Editor.FileManager.SetAction(FileManagerType.Export);
                        Editor.FileManager.ToggleOn();
                        Editor.FileManager.FileType = ".png";
                        Editor.FileManager.SaveFile = Editor.textureEditor.SaveTexture;
                    }),
                    Sub(
                        new UIText("Export", Class(mc_[6], middle_center, fs_[1.2f]))
                    )),
                    new UICol(Class(w_full_minus_[10], h_[30], blank_full_g_[20], hover_color_g_[20, 30], hover_color_duration_[0.1f], hover_color_ignore_when_selected),
                    OnClickCol(c => {
                        Editor.FileManager.SetAction(FileManagerType.Import);
                        Editor.FileManager.ToggleOn();
                    }),
                    Sub(
                        new UIText("Import", Class(mc_[6], middle_center, fs_[1.2f]))
                    ))
                )),
                new UIImg(Class(w_full, h_[2], blank_full_g_[BASE_BORDER])),
                // New texture section
                new UICol(Class(w_full, h_[30]), Sub(
                    new UIText("TEXTURE", Class(middle_left, left_[5], fs_[1.2f]))
                )),
                new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                    new UIText("Width", Class(fs_[1], middle_left)),
                    new UICol(Class(w_[70], h_full, top_right, blank_sharp_g_[10]), Sub(
                        newField("100", Class(mc_[5], fs_[1], text_align_right, middle_right, right_[5]), ref TextureWidthField)
                    ))
                )),
                new UICol(Class(w_full_minus_[10], h_[25], top_center, top_[5]), Sub(
                    new UIText("Height", Class(fs_[1], middle_left)),
                    new UICol(Class(w_[70], h_full, top_right, blank_sharp_g_[10]), Sub(
                        newField("100", Class(mc_[5], fs_[1], text_align_right, middle_right, right_[5]), ref TextureHeightField)
                    ))
                )),
                new UICol(Class(w_full_minus_[10], h_[25], top_center, blank_sharp_g_[30], top_[5]), 
                OnClickCol(_ => DrawingPanel.Renew(TextureWidthField.GetInt(100), TextureHeightField.GetInt(100))),
                Sub(
                    new UIText("Create", Class(middle_center))
                )),
                new UIImg(Class(w_full, h_[2], blank_full_g_[BASE_BORDER], top_[5])),
                new UICol(Class(w_full, h_[30]), Sub(
                    new UIText("UV SCALING", Class(mc_[10], middle_left, left_[5], fs_[1.2f]))
                )),
                new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                    new UIText("Pixel Size", Class(fs_[1], middle_left)),
                    new UICol(Class(w_[70], h_full, top_right, blank_sharp_g_[10]), Sub(
                        newField("1", Class(mc_[10], fs_[1], text_align_right, middle_right, right_[5]), ref MeshUnitsField)
                    ))
                )),
                new UICol(Class(w_full_minus_[10], h_[25], top_center, blank_sharp_g_[30], top_[5]), 
                OnClickCol(_ => Editor.textureEditor.Handle_PixelMapping(MeshUnitsField.GetFloat(1f))),
                Sub(
                    new UIText("Scale", Class(mc_[5], fs_[1], middle_center))
                ))
                
            ), ref TextureRightPanel),
            new UICol(Class(w_full_minus_[10], h_[25], bottom_[95], bottom_center, blank_sharp_g_[BASE_BUTTON]),
            Sub(
                new UIText("Culling", Class(mc_[7], fs_[1], middle_left, left_[5])),
                new UIButton(Class(w_[15], h_[15], blank_round_g_[ModelSettings.BackfaceCulling ? 60 : 20], right_[5], middle_right), OnClickButton(i => Toggle(i, ref ModelSettings.BackfaceCulling)))
            )),
            new UICol(Class(w_full_minus_[10], h_[25], bottom_[65], bottom_center, blank_sharp_g_[BASE_BUTTON]),
            Sub(
                new UIText("Wireframe", Class(mc_[9], fs_[1], middle_left, left_[5])),
                new UIButton(Class(w_[15], h_[15], blank_round_g_[ModelSettings.WireframeVisible ? 60 : 20], right_[5], middle_right), OnClickButton(i => Toggle(i, ref ModelSettings.WireframeVisible)))
            )),
            new UICol(Class(w_full_minus_[10], h_[25], bottom_[35], bottom_center), Sub(
                new UICol(Class(w_[60f], h_full, blank_sharp_g_[BASE_BUTTON]),
                OnClickCol(_ => Game.SetCursorState(CursorMode.Disabled)),
                OnHold(AlphaHold),
                OnReleaseCol(_ => Game.SetCursorState(CursorMode.Normal)),
                Sub(
                    new UIText("Alpha", Class(mc_[5], fs_[1], middle_center))
                )),
                new UICol(Class(w_[70], h_full, middle_right, blank_sharp_g_[10]), Sub(
                    new UIField("1", Class(mc_[6], fs_[1], middle_right, text_align_right, right_[5]))
                ))
            )),
            new UICol(Class(w_full_minus_[10], h_[25], bottom_[5], bottom_center), Sub(
                new UIText("Camera Speed", Class(mc_[12], fs_[1], middle_left)),
                new UICol(Class(w_[70], h_full, top_right, blank_sharp_g_[10]), Sub(
                    new UIField("75", Class(mc_[5], fs_[1], text_align_right, middle_right, right_[5]),
                    OnTextChange(i => Editor.Scene.DefaultCamera.SetCameraSpeed(i.GetFloat())))
                ))
            ))
        ))
    ]));

    private UIElementBase GenerateColorPickers() =>
    new UIHCol(Class(w_full_minus_[5], h_[30], spacing_[5], border_[0, 0, 0, 0]), [
    ..Forloop(0, 5, (j) =>
        new UIImg(Class(blank_full_g_[100], w_minus_[20f, 5], h_[30], border_ui_[3, 3, 3, 3], border_color_[(0, 0, 0, 0)], hover_scale_[1.2f], hover_scale_duration_[0.2f], hover_scale_easeout), OnClickImg(image => {
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
        }))
    )
    ]);
    
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
        var col = new UIVCol(Class(w_full_minus_[10], grow_children, spacing_[5], left_[5], blank_sharp_g_[BASE_BUTTON], border_[3, 3, 3, 3]), Sub(
            new UIText(name, Class(mc_[name.Length], fs_[1])),
            new UICol(Class(w_full_minus_[6], h_[15]), Sub(
                XYZLabel("X", top_left),
                XYZLabel("Y", top_center),
                XYZLabel("Z", top_right)
            )),
            new UICol(Class(w_full_minus_[6], h_[25]), Sub(
                XYZField(0, top_left, 0, action, out var xField),
                XYZField(0, top_center, 1, action, out var yField),
                XYZField(0, top_right, 2, action, out var zField)
            ))
        ));
        setAction = (x, y, z) => SetValue(x, y, z, xField, yField, zField);
        return col;
    }
    

    private UICol XYZLabel(string label, UIStyleData alignment) =>
    new UICol(Class(w_[32f], h_full, alignment), Sub(new UIText(label, Class(mc_[1], fs_[1], bottom_left, left_[3]))));
    private UIHScroll XYZField(float value, UIStyleData alignment, int index, Action<int, float> action, out UIField field)
    {
        field = new UIField("" + value, Class(mc_[20], fs_[0.9f], middle_left, left_[3]));
        field.SetOnTextChange(f => action(index, f.GetFloat()));
        return new UIHScroll(Class(w_[32f], h_full, blank_sharp_g_[10], mask_children, alignment), Sub(
            field
        ));
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
        var col = new UICol(Class(w_full_minus_[10], h_[30], blank_sharp, rgba_v4_[model.IsSelected ? (0.3f, 0.3f, 0.3f, 1f) : (0f, 0f, 0f, 0f)]),
        OnHoverEnterCol(c => c.UpdateColor((0.3f, 0.3f, 0.3f, 1f))),
        OnHoverExitCol(c =>
        {
            if (!model.IsSelected)
                c.UpdateColor((0f, 0f, 0f, 0f));
        }));
        col.AddElements(
            new UIButton(Class(w_full_minus_[70], h_full), OnClickButton(_ =>
            {
                if (!model.IsSelected)
                {
                    ModelManager.Select(model);
                }
                else
                {
                    ModelManager.UnSelect(model);
                }
            })),
            new UIText(model.Name, Class(mc_[model.Name.Length], fs_[1f], middle_left, left_[5])),
            new UIHCol(Class(w_[64], h_[16], middle_right, spacing_[5]), Sub(
                new UICol(Class(w_[16], h_[16], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                OnClickCol(c =>
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
                }),
                Sub(
                    new UIImg(Class(w_full, h_full, icon_[22], bg_white))
                )),
                new UICol(Class(w_[16], h_[16], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                OnClickCol(c =>
                {
                    var icon = c.GetElement<UIImg>();
                    if (icon != null)
                    {
                        icon.UpdateIconIndex(icon.TextureID == (41 | 0x20000000) ? 40 : 41);
                    }
                }),
                Sub(
                    new UIImg(Class(w_full, h_full, icon_[41], bg_white))
                )),
                new UICol(Class(w_[16], h_[16], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                OnClickCol(c =>
                {
                    col.Delete();
                    model.Delete();
                    Hierarchy.QueueAlign();
                    Hierarchy.QueueUpdateTransformation();
                }),
                Sub(
                    new UIImg(Class(w_full, h_full, icon_[18], bg_white))
                ))
            ))
        );
        return col;
    }

    public UIElementBase GetModelButton(PBG_Model model)
    {
        var col = new UICol(Class(w_full_minus_[10], h_[30], blank_sharp, rgba_v4_[model.IsSelected ? (0.3f, 0.3f, 0.3f, 1f) : (0f, 0f, 0f, 0f)]),
        OnHoverEnterCol(c => c.UpdateColor((0.3f, 0.3f, 0.3f, 1f))),
        OnHoverExitCol(c =>
        {
            if (!model.IsSelected)
                c.UpdateColor((0f, 0f, 0f, 0f));
        }));
        col.AddElements(
            new UIButton(Class(w_full_minus_[70], h_full), OnClickButton(_ =>
            {
                if (!model.IsSelected)
                {
                    PBG_Model.Select(model);
                }
                else
                {
                    PBG_Model.UnSelect(model);
                }
            })),
            new UIText(model.Name, Class(mc_[model.Name.Length], fs_[1f], middle_left, left_[5])),
            new UIHCol(Class(w_[64], h_[16], middle_right, spacing_[5]), Sub(
                new UICol(Class(w_[16], h_[16], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                OnClickCol(c =>
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
                }),
                Sub(
                    new UIImg(Class(w_full, h_full, icon_[22], bg_white))
                )),
                new UICol(Class(w_[16], h_[16], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                OnClickCol(c =>
                {
                    var icon = c.GetElement<UIImg>();
                    if (icon != null)
                    {
                        icon.UpdateIconIndex(icon.TextureID == (41 | 0x20000000) ? 40 : 41);
                    }
                }),
                Sub(
                    new UIImg(Class(w_full, h_full, icon_[41], bg_white))
                )),
                new UICol(Class(w_[16], h_[16], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                OnClickCol(c =>
                {
                    col.Delete();
                    model.Delete();
                    Hierarchy.QueueAlign();
                    Hierarchy.QueueUpdateTransformation();
                }),
                Sub(
                    new UIImg(Class(w_full, h_full, icon_[18], bg_white))
                ))
            ))
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
        var col = new UICol(Class(w_full_minus_[10], blank_full_g_[selected ? 30 : 20], grow_children, data_["ID", id], ignore_invisible),
        OnHoverEnterCol(c =>
        {
            c.UpdateColor((0.3f, 0.3f, 0.3f, 1f));
        }),
        OnHoverExitCol(c =>
        {
            if (model.Animation != animation)
            {
                c.UpdateColor((0.2f, 0.2f, 0.2f, 1f));
            }
        }));
        col.AddElements(
            new UICol(Class(w_full, h_[30]), Sub(
                new UIButton($"button_{id}", Class(w_full, h_full), OnClickButton(b =>
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
                })),
                new UIText(animation.Name, Class(mc_[22], fs_[1f], middle_left, left_[5])),
                new UICol(Class(w_[16], h_[16], middle_right, right_[5], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                OnClickCol(c =>
                {
                    col.Delete();
                    model.DeleteAnimation(id);
                    AnimationHierarchy.QueueAlign();
                    AnimationHierarchy.QueueUpdateTransformation();
                }),
                Sub(
                    new UIImg(Class(w_full, h_full, icon_[18], bg_white))
                ))
            )),
            new UICol("settings", Class(w_full, h_[30], blank_full_g_[20], top_[30], hidden), Sub(
                new UICol(Class(w_full_minus_[10], h_[20], top_[5], top_center, blank_full_g_[10]), Sub(
                    new UIField(animation.Name, Class(mc_[22], middle_left, left_[3]), OnTextChange(f =>
                    {
                        animation.Name = f.GetText();
                        col.QueryElement<UIText>()?.UpdateText(animation.Name);
                    }))
                ))
            ))
        );          
        return col;
    }
}