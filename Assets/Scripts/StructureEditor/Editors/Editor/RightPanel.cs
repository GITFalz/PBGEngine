using PBG.MathLibrary;
using PBG.MathLibrary;
using PBG.UI;
using PBG.UI.Creator;
using static PBG.UI.Styles;

public partial class StructureEditor
{
    public class RightPanel(StructureEditor editor) : UIScript
    {
        public UICol BlockSelectionPanel = null!;
        public UIImg CurrentBlockImg = null!;
        public UIVScroll BlockCollection = null!;

        private UIField SizeXField = null!;
        private UIField SizeYField = null!;
        private UIField SizeZField = null!;

        private UIField PositionXField = null!;
        private UIField PositionYField = null!;
        private UIField PositionZField = null!;

        private UIVScroll BoundingBoxPanel = null!;
        private UIVScroll ExtendersPanel = null!;
        private UIVScroll ConnectionPointsPanel = null!;
        private UIVScroll CategoriesPanel = null!;
        private UIVScroll AvoidPanel = null!;
        private UIVScroll RulesetPointsPanel = null!;

        private UIVCol SettingsSection = null!;
        private UIVCol BoundingBoxSection = null!;
        private UIVCol ExtendersSection = null!;
        private UIVCol ConnectionPointSection = null!;
        private UIVCol RulesetPointSection = null!;

        private UIVCol? PreviousSection;

        public int SizeX
        {
            get => SizeXField.GetInt(1);
            set => SizeXField.UpdateText(value.ToString());
        }
        public int SizeY
        {
            get => SizeYField.GetInt(1);
            set => SizeYField.UpdateText(value.ToString());
        }
        public int SizeZ
        {
            get => SizeZField.GetInt(1);
            set => SizeZField.UpdateText(value.ToString());
        }
        public Vector3i Size
        {
            get => (SizeX, SizeY, SizeZ);
            set { SizeX = value.X; SizeY = value.Y; SizeZ = value.Z; }
        }

        public int PositionX
        {
            get => PositionXField.GetInt(0);
            set => PositionXField.UpdateText(value.ToString());
        }
        public int PositionY
        {
            get => PositionYField.GetInt(0);
            set => PositionYField.UpdateText(value.ToString());
        }
        public int PositionZ
        {
            get => PositionZField.GetInt(0);
            set => PositionZField.UpdateText(value.ToString());
        }
        public Vector3i Position
        {
            get => (PositionX, PositionY, PositionZ);
            set { PositionX = value.X; PositionY = value.Y; PositionZ = value.Z; }
        }


        private UIField ExtenderSizeXField = null!;
        private UIField ExtenderSizeZField = null!;

        private UIField ExtenderPositionXField = null!;
        private UIField ExtenderPositionYField = null!;
        private UIField ExtenderPositionZField = null!;


        public int ExtenderSizeX
        {
            get => ExtenderSizeXField.GetInt(1);
            set => ExtenderSizeXField.UpdateText(value.ToString());
        }
        public int ExtenderSizeZ
        {
            get => ExtenderSizeZField.GetInt(1);
            set => ExtenderSizeZField.UpdateText(value.ToString());
        }
        public Vector3i ExtenderSize
        {
            get => (ExtenderSizeX, 0, ExtenderSizeZ);
            set { ExtenderSizeX = value.X; ExtenderSizeZ = value.Z; }
        }

        public int ExtenderPositionX
        {
            get => ExtenderPositionXField.GetInt(0);
            set => ExtenderPositionXField.UpdateText(value.ToString());
        }
        public int ExtenderPositionY
        {
            get => ExtenderPositionYField.GetInt(0);
            set => ExtenderPositionYField.UpdateText(value.ToString());
        }
        public int ExtenderPositionZ
        {
            get => ExtenderPositionZField.GetInt(0);
            set => ExtenderPositionZField.UpdateText(value.ToString());
        }
        public Vector3i ExtenderPosition
        {
            get => (ExtenderPositionX, ExtenderPositionY, ExtenderPositionZ);
            set { ExtenderPositionX = value.X; ExtenderPositionY = value.Y; ExtenderPositionZ = value.Z; }
        }


        private UIField ConnectionPositionXField = null!;
        private UIField ConnectionPositionYField = null!;
        private UIField ConnectionPositionZField = null!;

        public float ConnectionPositionX
        {
            get => ConnectionPositionXField.GetFloat(0);
            set => ConnectionPositionXField.UpdateText(value.ToString());
        }
        public float ConnectionPositionY
        {
            get => ConnectionPositionYField.GetFloat(0);
            set => ConnectionPositionYField.UpdateText(value.ToString());
        }
        public float ConnectionPositionZ
        {
            get => ConnectionPositionZField.GetFloat(0);
            set => ConnectionPositionZField.UpdateText(value.ToString());
        }
        public Vector3 ConnectionPosition
        {
            get => (ConnectionPositionX, ConnectionPositionY, ConnectionPositionZ);
            set { ConnectionPositionX = value.X; ConnectionPositionY = value.Y; ConnectionPositionZ = value.Z; }
        }


        private UIField RulesetPositionXField = null!;
        private UIField RulesetPositionYField = null!;
        private UIField RulesetPositionZField = null!;

        public float RulesetPositionX
        {
            get => RulesetPositionXField.GetFloat(0);
            set => RulesetPositionXField.UpdateText(value.ToString());
        }
        public float RulesetPositionY
        {
            get => RulesetPositionYField.GetFloat(0);
            set => RulesetPositionYField.UpdateText(value.ToString());
        }
        public float RulesetPositionZ
        {
            get => RulesetPositionZField.GetFloat(0);
            set => RulesetPositionZField.UpdateText(value.ToString());
        }
        public Vector3 RulesetPosition
        {
            get => (RulesetPositionX, RulesetPositionY, RulesetPositionZ);
            set { RulesetPositionX = value.X; RulesetPositionY = value.Y; RulesetPositionZ = value.Z; }
        }

        public StructureBoundingBox? SelectedBoundingBox = null;
        public StructureExtender? SelectedExtender = null;
        public ConnectionPoint? SelectedConnection = null;
        public RulesetPoint? SelectedRuleset = null;

        public override void AfterScript()
        {
            PreviousSection = BoundingBoxSection;
        }

        public override UIElementBase Script() =>
        new UICol(Class(w_full, h_full, invisible), [
            newCol(Class(w_full, h_full, not_toggle_old_invisible),[
                new UICol(Class(w_[70], h_[70], top_right, right_[230], bottom_[10], blank_sharp_g_[30], depth_[-40]), [
                    newImg(Class(w_[60], h_[60], bottom_left, item_["test_block"], bg_white), ref CurrentBlockImg)
                ]),
                newVScroll(Class(w_[70], h_full_minus_[70], bottom_right, right_[240], spacing_[10], border_[0, 10, 0, 0], mask_children, depth_[-40], scroll_speed_[30]), 
                OnClickVScroll(_ => { }), // empty event to make the hovering variable update
                [
                    ..Foreach(ItemDataManager.AllItems, (name, item) =>
                    {
                        if (item is BlockItemData blockItem)
                        {
                            return new UICol(Class(left_[30], w_[100], h_[60], blank_sharp_g_[20], hover_translation_[(-20, 0)], hover_translation_duration_[0.3f], hover_translation_easeout, hover_color_[(0.2f, 0.2f, 0.2f, 1), (0.3f, 0.3f, 0.3f, 1)], hover_color_duration_[0.3f], hover_color_easeout), 
                            OnClickCol(_ => editor.SetBlock(name)), [
                                new UIImg(Class(w_[50], h_[50], middle_left, left_[5], item_[name], gray_[80]))
                            ]);
                        }
                        return null;
                    })
                ], ref BlockCollection),
            ], ref BlockSelectionPanel),
            new UIVScroll(Class(w_full_minus_[2], h_full, blank_full_g_[20], top_right, mask_children, scroll_speed_[20], ignore_invisible), Sub(
                new UIVCol(Class(w_full, grow_children, top_[5], border_[5, 0, 5, 0], spacing_[5]), [
                    new UICol(Class(w_full_minus_[10], h_[20], blank_full_g_[30]), 
                    OnClickCol(_ => SwitchPanel(SettingsSection)), [
                        new UIText("SETTINGS", Class(left_[5], middle_left))
                    ]),
                    new UICol(Class(w_full_minus_[10], h_[20], blank_full_g_[30]), 
                    OnClickCol(_ => SwitchPanel(BoundingBoxSection)), [
                        new UIText("BOUNDING BOXES", Class(left_[5], middle_left))
                    ]),
                    new UICol(Class(w_full_minus_[10], h_[20], blank_full_g_[30]), 
                    OnClickCol(_ => SwitchPanel(ExtendersSection)), [
                        new UIText("EXTENTION BOXES", Class(left_[5], middle_left))
                    ]),
                    new UICol(Class(w_full_minus_[10], h_[20], blank_full_g_[30]), 
                    OnClickCol(_ => SwitchPanel(ConnectionPointSection)), [
                        new UIText("CONNECTION POINTS", Class(left_[5], middle_left))
                    ]),
                    new UICol(Class(w_full_minus_[10], h_[20], blank_full_g_[30]), 
                    OnClickCol(_ => SwitchPanel(RulesetPointSection)), [
                        new UIText("RULESET POINTS", Class(left_[5], middle_left))
                    ])
                ]),
                newVCol(Class(w_full, top_[5], hidden, not_toggle_old_invisible, grow_children), [
                    new UIVCol(Class(w_full, grow_children, top_[5]), Sub([
                        new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                            new UIText("Camera Speed", Class(fs_[1.2f], middle_left))
                        )),
                        new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UICol(Class(w_[100f], h_full, blank_sharp_g_[10], top_right), Sub(
                                    new UIField(""+editor.Camera.SPEED, Class(mc_[5], middle_left, left_[5]), OnTextChange(f => {
                                        var value = f.GetFloat();
                                        value.ClampSety(0.01f, 100);
                                        editor.Camera.SetCameraSpeed(value);
                                    }))
                                ))
                            ))
                        ))
                    ]))
                ], ref SettingsSection),
                newVCol(Class(w_full, top_[5], hidden, not_toggle_old_invisible, grow_children), [
                    new UIVCol(Class(w_full, h_[300], spacing_[5], top_[5]), Sub([
                        new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                            new UIText("BOUNDING BOXES", Class(fs_[1.2f], middle_left)),
                            new UICol(Class(w_[25], h_[25], middle_right, blank_sharp_g_[25], hover_scale_easeout_[1.2f, 0.2f]), Sub(
                                new UIImg(Class(w_full, h_full, icon_[16], bg_white), OnClickImg(_ => {
                                    if (editor.SelectedBoundingBox == null)
                                        return;

                                    StructureBoundingBox boundingBox = new((1, 1, 1), (0, 0, 0));
                                    var button = BoundingBoxButton(editor.SelectedBoundingBox.BoundingBoxes.Count, boundingBox);
                                    BoundingBoxPanel.AddElement(button);
                                    UIController.AddElement(button);
                                    editor.SelectedBoundingBox.BoundingBoxes.Add(boundingBox);
                                    SelectBoundingBox(boundingBox);
                                }))
                            ))
                        )),
                        newVScroll(Class(w_full_minus_[10], h_full_minus_[30], top_center, blank_sharp_g_[10], mask_children, border_[5, 5, 5, 5], spacing_[5]), Sub([
                            ..Foreach(editor.SelectedBoundingBox?.BoundingBoxes ?? [], BoundingBoxButton)
                        ]), ref BoundingBoxPanel)
                    ])),
                    new UIImg(Class(w_full, h_[2], blank_full_g_[30], top_[5])),
                    new UIVCol(Class(w_full, grow_children, top_[5]), Sub([
                        new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                            new UIText("Settings", Class(fs_[1.2f], middle_left))
                        )),
                        new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                            new UIText("Scale", Class(fs_[1f], middle_left))
                        )),
                        new UIVCol(Class(w_full_minus_[10], h_[85], spacing_[5], top_center), Sub(
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UIText("X", Class(middle_left, fs_[1.2f])),
                                new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                    newField("1", Class(mc_[20], middle_left, left_[5]), ref SizeXField)
                                )),
                                new ChangeElement(editor, SizeXField, 1, int.MaxValue, UpdateBoundingBoxData)
                            )),
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UIText("Y", Class(middle_left, fs_[1.2f])),
                                new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                    newField("1", Class(mc_[20], middle_left, left_[5]), ref SizeYField)
                                )),
                                new ChangeElement(editor, SizeYField, 1, int.MaxValue, UpdateBoundingBoxData)
                            )),
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UIText("Z", Class(middle_left, fs_[1.2f])),
                                new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                    newField("1", Class(mc_[20], middle_left, left_[5]), ref SizeZField)
                                )),
                                new ChangeElement(editor, SizeZField, 1, int.MaxValue, UpdateBoundingBoxData)
                            ))
                        )),
                        new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                            new UIText("Position", Class(fs_[1f], middle_left))
                        )),
                        new UIVCol(Class(w_full_minus_[10], h_[85], spacing_[5], top_center), Sub(
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UIText("X", Class(middle_left, fs_[1.2f])),
                                new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                    newField("0", Class(mc_[20], middle_left, left_[5]), ref PositionXField)
                                )),
                                new ChangeElement(editor, PositionXField, int.MinValue, int.MaxValue, UpdateBoundingBoxData)
                            )),
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UIText("Y", Class(middle_left, fs_[1.2f])),
                                new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                    newField("0", Class(mc_[20], middle_left, left_[5]), ref PositionYField)
                                )),
                                new ChangeElement(editor, PositionYField, int.MinValue, int.MaxValue, UpdateBoundingBoxData)
                            )),
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UIText("Z", Class(middle_left, fs_[1.2f])),
                                new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                    newField("0", Class(mc_[20], middle_left, left_[5]), ref PositionZField)
                                )),
                                new ChangeElement(editor, PositionZField, int.MinValue, int.MaxValue, UpdateBoundingBoxData)
                            ))
                        ))
                    ])),
                ], ref BoundingBoxSection),
                newVCol(Class(w_full, top_[5], hidden, not_toggle_old_invisible, grow_children), [
                    new UIVCol(Class(w_full, h_[300], spacing_[5], top_[5]), Sub([
                        new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                            new UIText("EXTENSION BOXES", Class(fs_[1.2f], middle_left)),
                            new UICol(Class(w_[25], h_[25], middle_right, blank_sharp_g_[25], hover_scale_easeout_[1.2f, 0.2f]), Sub(
                                new UIImg(Class(w_full, h_full, icon_[16], bg_white), OnClickImg(_ => {
                                    if (editor.SelectedBoundingBox == null)
                                        return;

                                    StructureExtender extender = new((1, 0, 1), (0, 0, 0));
                                    var button = ExtenderButton(editor.SelectedBoundingBox.Extenders.Count, extender);
                                    ExtendersPanel.AddElement(button);
                                    UIController.AddElement(button);
                                    editor.SelectedBoundingBox.Extenders.Add(extender);
                                    SelectExtender(extender);
                                }))
                            ))
                        )),
                        newVScroll(Class(w_full_minus_[10], h_full_minus_[30], top_center, blank_sharp_g_[10], mask_children, border_[5, 5, 5, 5], spacing_[5]), Sub([
                            ..Foreach(editor.SelectedBoundingBox?.Extenders ?? [], ExtenderButton)
                        ]), ref ExtendersPanel)
                    ])),
                    new UIImg(Class(w_full, h_[2], blank_full_g_[30], top_[5])),
                    new UIVCol(Class(w_full, grow_children, top_[5]), Sub([
                        new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                            new UIText("Settings", Class(fs_[1.2f], middle_left))
                        )),
                        new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                            new UIText("Scale", Class(fs_[1f], middle_left))
                        )),
                        new UIVCol(Class(w_full_minus_[10], h_[85], spacing_[5], top_center), Sub(
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UIText("X", Class(middle_left, fs_[1.2f])),
                                new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                    newField("1", Class(mc_[20], middle_left, left_[5]), ref ExtenderSizeXField)
                                )),
                                new ChangeElement(editor, ExtenderSizeXField, 1, int.MaxValue, UpdateExtenderData)
                            )),
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UIText("Z", Class(middle_left, fs_[1.2f])),
                                new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                    newField("1", Class(mc_[20], middle_left, left_[5]), ref ExtenderSizeZField)
                                )),
                                new ChangeElement(editor, ExtenderSizeZField, 1, int.MaxValue, UpdateExtenderData)
                            ))
                        )),
                        new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                            new UIText("Position", Class(fs_[1f], middle_left))
                        )),
                        new UIVCol(Class(w_full_minus_[10], h_[85], spacing_[5], top_center), Sub(
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UIText("X", Class(middle_left, fs_[1.2f])),
                                new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                    newField("0", Class(mc_[20], middle_left, left_[5]), ref ExtenderPositionXField)
                                )),
                                new ChangeElement(editor, ExtenderPositionXField, int.MinValue, int.MaxValue, UpdateExtenderData)
                            )),
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UIText("Y", Class(middle_left, fs_[1.2f])),
                                new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                    newField("0", Class(mc_[20], middle_left, left_[5]), ref ExtenderPositionYField)
                                )),
                                new ChangeElement(editor, ExtenderPositionYField, int.MinValue, int.MaxValue, UpdateExtenderData)
                            )),
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UIText("Z", Class(middle_left, fs_[1.2f])),
                                new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                    newField("0", Class(mc_[20], middle_left, left_[5]), ref ExtenderPositionZField)
                                )),
                                new ChangeElement(editor, ExtenderPositionZField, int.MinValue, int.MaxValue, UpdateExtenderData)
                            ))
                        ))
                    ])),
                ], ref ExtendersSection),
                newVCol(Class(w_full, top_[5], hidden, not_toggle_old_invisible, grow_children), [
                    new UIVCol(Class(w_full, h_[300], spacing_[5], top_[5]), Sub([
                        new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                            new UIText("CONNECTION POINTS", Class(fs_[1.2f], middle_left)),
                            new UICol(Class(w_[25], h_[25], middle_right, blank_sharp_g_[25], hover_scale_easeout_[1.2f, 0.2f]), Sub(
                                new UIImg("test", Class(w_full, h_full, icon_[16], bg_white), OnClickImg(_ => {
                                    if (editor.SelectedBoundingBox == null)
                                        return;

                                    Vector3 pos = new Vector3(editor.SelectedBoundingBox.Size) * 0.5f;
                                    pos.Z = 0;
                                    ConnectionPoint connection = new(pos, 0, 0);
                                    var name = GetUniqueConnectionName();
                                    var button = ConnectionPointButton(name, connection);
                                    ConnectionPointsPanel.AddElement(button);
                                    UIController.AddElement(button);
                                    editor.SelectedBoundingBox.ConnectionPoints.Add(name, connection);
                                    SelectConnectionPoint(connection);
                                }))
                            ))
                        )),
                        newVScroll(Class(w_full_minus_[10], h_full_minus_[30], top_center, blank_sharp_g_[10], mask_children, border_[5, 5, 5, 5], spacing_[5]), Sub([
                            ..Foreach(editor.SelectedBoundingBox?.ConnectionPoints ?? [], ConnectionPointButton)
                        ]), ref ConnectionPointsPanel)
                    ])),
                    new UIImg(Class(w_full, h_[2], blank_full_g_[30], top_[5])),
                    new UIVCol(Class(w_full, grow_children, top_[5]), Sub([
                        new UIVCol(Class(w_full_minus_[10], h_[55], spacing_[5], top_center), Sub(
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UICol(Class(w_[32f], h_full, top_left, blank_sharp_g_[20]), OnClickCol(_ => SetSide(0)), Sub(
                                    new UIText("Front", Class(middle_center))
                                )),
                                new UICol(Class(w_[32f], h_full, top_center, blank_sharp_g_[20]), OnClickCol(_ => SetSide(1)), Sub(
                                    new UIText("Right", Class(middle_center))
                                )),
                                new UICol(Class(w_[32f], h_full, top_right, blank_sharp_g_[20]), OnClickCol(_ => SetSide(2)), Sub(
                                    new UIText("Top", Class(middle_center))
                                ))
                            )),
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UICol(Class(w_[32f], h_full, top_left, blank_sharp_g_[20]), OnClickCol(_ => SetSide(3)), Sub(
                                    new UIText("Left", Class(middle_center))
                                )),
                                new UICol(Class(w_[32f], h_full, top_center, blank_sharp_g_[20]), OnClickCol(_ => SetSide(4)), Sub(
                                    new UIText("Bottom", Class(middle_center))
                                )),
                                new UICol(Class(w_[32f], h_full, top_right, blank_sharp_g_[20]), OnClickCol(_ => SetSide(5)), Sub(
                                    new UIText("Back", Class(middle_center))
                                ))
                            ))
                        )),
                        new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                            new UIText("Position", Class(fs_[1f], middle_left))
                        )),
                        new UIVCol(Class(w_full_minus_[10], h_[85], spacing_[5], top_center), Sub(
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UIText("X", Class(middle_left, fs_[1.2f])),
                                new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                    newField("0", Class(mc_[20], middle_left, left_[5]), ref ConnectionPositionXField)
                                )),
                                new ChangeElement(editor, ConnectionPositionXField, int.MinValue, int.MaxValue, UpdateConnectionPosition)
                            )),
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UIText("Y", Class(middle_left, fs_[1.2f])),
                                new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                    newField("0", Class(mc_[20], middle_left, left_[5]), ref ConnectionPositionYField)
                                )),
                                new ChangeElement(editor, ConnectionPositionYField, int.MinValue, int.MaxValue, UpdateConnectionPosition)
                            )),
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UIText("Z", Class(middle_left, fs_[1.2f])),
                                new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                    newField("0", Class(mc_[20], middle_left, left_[5]), ref ConnectionPositionZField)
                                )),
                                new ChangeElement(editor, ConnectionPositionZField, int.MinValue, int.MaxValue, UpdateConnectionPosition)
                            ))
                        ))
                    ])),
                    new UIImg(Class(w_full, h_[2], blank_full_g_[30], top_[5])),
                    new UIVCol(Class(w_full, h_[300], spacing_[5], top_[5]), Sub([
                        new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                            new UIText("CATEGORIES", Class(fs_[1.2f], middle_left)),
                            new UICol(Class(w_[25], h_[25], middle_right, blank_sharp_g_[25], hover_scale_easeout_[1.2f, 0.2f]), Sub(
                                new UIImg(Class(w_full, h_full, icon_[16], bg_white), OnClickImg(_ => { 
                                    if (SelectedConnection == null)
                                        return;

                                    var name = GetUniqueCategoryName(SelectedConnection);
                                    var field = new UIField(name, Class(top_[5], left_[5], mc_[20]), OnTextChange(_ => {
                                        if (SelectedConnection == null)
                                            return;

                                        SelectedConnection.Categories = [];
                                        for (int i = 0; i < CategoriesPanel.ChildElements.Count; i++)
                                        {
                                            if (CategoriesPanel.ChildElements[i] is UIField f)
                                                SelectedConnection.Categories.Add(f.GetTrimmedText());
                                        }
                                    }));
                                    CategoriesPanel.AddElement(field);
                                    UIController.AddElement(field);
                                }))
                            ))
                        )),
                        newVScroll(Class(w_full_minus_[10], h_full_minus_[30], top_center, blank_sharp_g_[10], mask_children, border_[5, 5, 5, 5], spacing_[5]), Sub([
                        ]), ref CategoriesPanel)
                    ])),
                    new UIImg(Class(w_full, h_[2], blank_full_g_[30], top_[5])),
                    new UIVCol(Class(w_full, h_[300], spacing_[5], top_[5]), Sub([
                        new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                            new UIText("CATEGORIES TO AVOID", Class(fs_[1.2f], middle_left)),
                            new UICol(Class(w_[25], h_[25], middle_right, blank_sharp_g_[25], hover_scale_easeout_[1.2f, 0.2f]), Sub(
                                new UIImg(Class(w_full, h_full, icon_[16], bg_white), OnClickImg(_ => { 
                                    if (SelectedConnection == null)
                                        return;

                                    var field = new UIField("category", Class(top_[5], left_[5], mc_[20]), OnTextChange(_ => {
                                        if (SelectedConnection == null)
                                            return;

                                        SelectedConnection.Avoid = [];
                                        for (int i = 0; i < AvoidPanel.ChildElements.Count; i++)
                                        {
                                            if (AvoidPanel.ChildElements[i] is UIField f)
                                                SelectedConnection.Avoid.Add(f.GetTrimmedText());
                                        }
                                    }));
                                    AvoidPanel.AddElement(field);
                                    UIController.AddElement(field);
                                }))
                            ))
                        )),
                        newVScroll(Class(w_full_minus_[10], h_full_minus_[30], top_center, blank_sharp_g_[10], mask_children, border_[5, 5, 5, 5], spacing_[5]), Sub([
                        ]), ref AvoidPanel)
                    ])),
                ], ref ConnectionPointSection),
                newVCol(Class(w_full, top_[5], hidden, not_toggle_old_invisible, grow_children), [
                    new UIVCol(Class(w_full, h_[300], spacing_[5], top_[5]), Sub([
                        new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                            new UIText("RULESETS", Class(fs_[1.2f], middle_left)),
                            new UICol(Class(w_[25], h_[25], middle_right, blank_sharp_g_[25], hover_scale_easeout_[1.2f, 0.2f]), Sub(
                                new UIImg(Class(w_full, h_full, icon_[16], bg_white), OnClickImg(_ => {
                                    if (editor.SelectedBoundingBox == null)
                                        return;

                                    Vector3 pos = Mathf.Floor(new Vector3(editor.SelectedBoundingBox.Size) * 0.5f);
                                    RulesetPoint rulseset = new(pos);
                                    var name = GetUniqueRulesetName();
                                    var button = RulesetPointButton(name, rulseset);
                                    RulesetPointsPanel.AddElement(button);
                                    UIController.AddElement(button);
                                    editor.SelectedBoundingBox.RulesetPoints.Add(name, rulseset);
                                    SelectRulesetPoint(rulseset);
                                }))
                            ))
                        )),
                        newVScroll(Class(w_full_minus_[10], h_full_minus_[30], top_center, blank_sharp_g_[10], mask_children, border_[5, 5, 5, 5], spacing_[5]), Sub([
                            ..Foreach(editor.SelectedBoundingBox?.RulesetPoints ?? [], RulesetPointButton)
                        ]), ref RulesetPointsPanel)
                    ])),
                    new UIImg(Class(w_full, h_[2], blank_full_g_[30], top_[5])),
                    new UIVCol(Class(w_full, grow_children, top_[5]), Sub([
                        new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                            new UIText("Position", Class(fs_[1f], middle_left))
                        )),
                        new UIVCol(Class(w_full_minus_[10], h_[85], spacing_[5], top_center), Sub(
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UIText("X", Class(middle_left, fs_[1.2f])),
                                new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                    newField("0", Class(mc_[20], middle_left, left_[5]), ref RulesetPositionXField)
                                )),
                                new ChangeElement(editor, RulesetPositionXField, int.MinValue, int.MaxValue, UpdateRulesetPosition)
                            )),
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UIText("Y", Class(middle_left, fs_[1.2f])),
                                new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                    newField("0", Class(mc_[20], middle_left, left_[5]), ref RulesetPositionYField)
                                )),
                                new ChangeElement(editor, RulesetPositionYField, int.MinValue, int.MaxValue, UpdateRulesetPosition)
                            )),
                            new UICol(Class(w_full, h_[25]), Sub(
                                new UIText("Z", Class(middle_left, fs_[1.2f])),
                                new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                    newField("0", Class(mc_[20], middle_left, left_[5]), ref RulesetPositionZField)
                                )),
                                new ChangeElement(editor, RulesetPositionZField, int.MinValue, int.MaxValue, UpdateRulesetPosition)
                            ))
                        ))
                    ]))
                ], ref RulesetPointSection)
            ))
        ]);

        private void SwitchPanel(UIVCol element)
        { 
            PreviousSection?.SetVisible(false); 
            element.SetVisible(true); 
            PreviousSection = element; 
            Element.QueueAlign(); 
            Element.QueueUpdateTransformation(); 
        }

        private string GetUniqueConnectionName()
        {
            string name = "connection_1";
            int i = 2;
            while (editor.SelectedBoundingBox != null && editor.SelectedBoundingBox.ConnectionPoints.ContainsKey(name))
            {
                name = $"connection_{i}";
                i++;
            }
            return name;
        }

        private string GetUniqueCategoryName(ConnectionPoint connection)
        {
            string name = "category_1";
            int i = 2;
            while (connection.Categories.Contains(name))
            {
                name = $"category_{i}";
                i++;
            }
            return name;
        }

        private string GetUniqueRulesetName()
        {
            string name = "rule_1";
            int i = 2;
            while (editor.SelectedBoundingBox != null && editor.SelectedBoundingBox.RulesetPoints.ContainsKey(name))
            {
                name = $"rule_{i}";
                i++;
            }
            return name;
        }

        public void RegenerateBoundingBoxes()
        {
            BoundingBoxPanel.DeleteChildren();
            if (editor.SelectedBoundingBox == null)
                return;

            for (int i = 0; i < editor.SelectedBoundingBox.BoundingBoxes.Count; i++)
            {
                var boundingBox = editor.SelectedBoundingBox.BoundingBoxes[i];
                var button = BoundingBoxButton(i, boundingBox);
                BoundingBoxPanel.AddElement(button);
                UIController.AddElement(button);
            }
            editor.UpdateBoundingBox = true;
        }

        private UIElementBase BoundingBoxButton(int index, StructureBoundingBox boundingBox) 
        {
            var col = new UICol(Class(w_full_minus_[10], h_[25], top_center, blank_sharp_g_[10]), 
            OnClickCol(_ => SelectBoundingBox(boundingBox)),
            Sub(
                new UIText(""+index, Class(mc_[20], middle_left, left_[5])),
                new UIImg(Class(icon_[18], w_[20], h_[20], middle_right, right_[5], bg_white), OnClickImg(c =>
                {
                    if (editor.SelectedBoundingBox == null)
                        return;

                    c.ParentElement?.Delete();
                    editor.SelectedBoundingBox.BoundingBoxes.Remove(boundingBox);
                    if (boundingBox == SelectedBoundingBox)
                        SelectedBoundingBox = null;

                    RegenerateBoundingBoxes();
                }))
            ));
            boundingBox.Element = col;
            return col;
        }

        public void RegenerateExtenders()
        {
            ExtendersPanel.DeleteChildren();
            if (editor.SelectedBoundingBox == null)
                return;

            for (int i = 0; i < editor.SelectedBoundingBox.Extenders.Count; i++)
            {
                var extender = editor.SelectedBoundingBox.Extenders[i];
                var button = ExtenderButton(i, extender);
                ExtendersPanel.AddElement(button);
                UIController.AddElement(button);
            }
            editor.UpdateBoundingBox = true;
        }

        private UIElementBase ExtenderButton(int index, StructureExtender extender) 
        {
            var col = new UICol(Class(w_full_minus_[10], h_[25], top_center, blank_sharp_g_[10]), 
            OnClickCol(_ => SelectExtender(extender)),
            Sub(
                new UIText(""+index, Class(mc_[20], middle_left, left_[5])),
                new UIImg(Class(icon_[18], w_[20], h_[20], middle_right, right_[5], bg_white), OnClickImg(c =>
                {
                    if (editor.SelectedBoundingBox == null)
                        return;

                    c.ParentElement?.Delete();
                    editor.SelectedBoundingBox.Extenders.Remove(extender);
                    if (extender == SelectedExtender)
                        SelectedExtender = null;

                    RegenerateExtenders();
                }))
            ));
            extender.Element = col;
            return col;
        }

        public void RegenerateConnectionPoints()
        {
            ConnectionPointsPanel.DeleteChildren();
            if (editor.SelectedBoundingBox == null)
                return;
  
            foreach (var (name, connection) in editor.SelectedBoundingBox.ConnectionPoints)
            {
                var button = ConnectionPointButton(name, connection);
                ConnectionPointsPanel.AddElement(button);
                UIController.AddElement(button);
            }
            editor.UpdateBoundingBox = true;
        }

        private UIElementBase ConnectionPointButton(string name, ConnectionPoint connection) 
        {
            var text = new UIText(name, Class(mc_[20], middle_left, left_[5]));
            var col = new UICol(Class(w_full_minus_[10], h_[25], top_center, blank_sharp_g_[10]), 
            OnClickCol(_ => SelectConnectionPoint(connection)),
            Sub(
                text,
                new UIImg(Class(icon_[18], w_[20], h_[20], middle_right, right_[5], bg_white), OnClickImg(c =>
                {
                    if (editor.SelectedBoundingBox == null)
                        return;
                    c.ParentElement?.Delete();
                    editor.SelectedBoundingBox.ConnectionPoints.Remove(text.GetText());
                    if (connection == SelectedConnection)
                        SelectedConnection = null;

                    RegenerateConnectionPoints();
                }))
            ));
            connection.Element = col;
            return col;
        }

        public void RegenerateRulesetPoints()
        {
            RulesetPointsPanel.DeleteChildren();
            if (editor.SelectedBoundingBox == null)
                return;

            foreach (var (name, rule) in editor.SelectedBoundingBox.RulesetPoints)
            {
                var button = RulesetPointButton(name, rule);
                RulesetPointsPanel.AddElement(button);
                UIController.AddElement(button);
            }
            editor.UpdateBoundingBox = true;
        }

        private UIElementBase RulesetPointButton(string name, RulesetPoint ruleset) 
        {
            var text = new UIText(name, Class(mc_[20], middle_left, left_[5]));
            var col = new UICol(Class(w_full_minus_[10], h_[25], top_center, blank_sharp_g_[10]), 
            OnClickCol(_ => SelectRulesetPoint(ruleset)),
            Sub(
                text,
                new UIImg(Class(icon_[18], w_[20], h_[20], middle_right, right_[5], bg_white), OnClickImg(c =>
                {
                    if (editor.SelectedBoundingBox == null)
                        return;
                    c.ParentElement?.Delete();
                    editor.SelectedBoundingBox.RulesetPoints.Remove(text.GetText());
                    if (ruleset == SelectedRuleset)
                        SelectedRuleset = null;

                    RegenerateRulesetPoints();
                }))
            ));
            ruleset.Element = col;
            return col;
        }

        private void SetSide(int side)
        {
            if (editor.SelectedBoundingBox == null || SelectedConnection == null)
                return;

            var size = editor.SelectedBoundingBox.Size;
            var pos = new Vector3(size) * 0.5f;

            SelectedConnection.Side = side;
            switch (side)
            {
                case 0: SelectedConnection.Yrotation = 0; break;
                case 1: SelectedConnection.Yrotation = 3; break;
                case 3: SelectedConnection.Yrotation = 1; break;
                case 5: SelectedConnection.Yrotation = 2; break;
            }

            switch (side)
            {
                case 0: pos.Z = 0;  break;
                case 1: pos.X = size.X; break;
                case 2: pos.Y = size.Y; break;
                case 3: pos.X = 0; break;
                case 4: pos.Y = 0; break;
                case 5: pos.Z = size.Z; break;
            }
            SelectedConnection.Position = pos;
            ConnectionPosition = pos;

            editor.UpdateBoundingBox = true;
        }

        private void UpdateBoundingBoxData()
        {
            if (SelectedBoundingBox == null)
                return;

            SelectedBoundingBox.Position = Position;
            SelectedBoundingBox.Size = Size;
        }

        private void UpdateExtenderData()
        {
            if (SelectedExtender == null)
                return;

            SelectedExtender.Position = ExtenderPosition;
            SelectedExtender.Size = ExtenderSize;
        }

        private void UpdateConnectionPosition()
        {
            if (SelectedConnection == null)
                return;

            SelectedConnection.Position = ConnectionPosition;
        }

        private void UpdateRulesetPosition()
        {
            if (SelectedRuleset == null) 
                return;

            SelectedRuleset.Position = Mathf.Round(RulesetPosition);
        }

        private void SelectBoundingBox(StructureBoundingBox boundingBox)
        {
            if (editor.SelectedBoundingBox == null)
                return;

            SelectedBoundingBox?.Element?.UpdateColor(new Vector4(0, 0, 0, 0));
            SelectedBoundingBox = boundingBox;
            SelectedBoundingBox.Element.UpdateColor(new Vector4(0.25f, 0.25f, 0.25f, 1f));
            Position = boundingBox.Position;
            Size = boundingBox.Size;
            editor.UpdateBoundingBox = true;
        }

        private void SelectExtender(StructureExtender extender)
        {
            if (editor.SelectedBoundingBox == null)
                return;

            SelectedExtender?.Element?.UpdateColor(new Vector4(0, 0, 0, 0));
            SelectedExtender = extender;
            SelectedExtender.Element.UpdateColor(new Vector4(0.25f, 0.25f, 0.25f, 1f));
            ExtenderPosition = extender.Position;
            ExtenderSize = extender.Size;
            editor.UpdateBoundingBox = true;
        }

        private void SelectConnectionPoint(ConnectionPoint connection)
        {
            if (editor.SelectedBoundingBox == null)
                return;

            SelectedConnection?.Element?.UpdateColor(new Vector4(0, 0, 0, 0));
            SelectedConnection = connection;
            SelectedConnection.Element.UpdateColor(new Vector4(0.25f, 0.25f, 0.25f, 1f));
            ConnectionPosition = connection.Position;
            editor.UpdateBoundingBox = true;

            CategoriesPanel.DeleteChildren();
            AvoidPanel.DeleteChildren();

            for (int i = 0; i < connection.Categories.Count; i++)
            {
                var cat = SelectedConnection.Categories[i];

                var field = new UIField(cat, Class(top_[5], left_[5], mc_[20]), OnTextChange(_ => {
                    if (SelectedConnection == null)
                        return;

                    SelectedConnection.Categories = [];
                    for (int i = 0; i < CategoriesPanel.ChildElements.Count; i++)
                    {
                        if (CategoriesPanel.ChildElements[i] is UIField f)
                            SelectedConnection.Categories.Add(f.GetTrimmedText());
                    }
                }));
                CategoriesPanel.AddElement(field);
                UIController.AddElement(field);
            }

            for (int i = 0; i < connection.Avoid.Count; i++)
            {
                var cat = SelectedConnection.Avoid[i];

                var field = new UIField(cat, Class(top_[5], left_[5], mc_[20]), OnTextChange(_ => {
                    if (SelectedConnection == null)
                        return;

                    SelectedConnection.Avoid = [];
                    for (int i = 0; i < AvoidPanel.ChildElements.Count; i++)
                    {
                        if (AvoidPanel.ChildElements[i] is UIField f)
                            SelectedConnection.Avoid.Add(f.GetTrimmedText());
                    }
                }));
                AvoidPanel.AddElement(field);
                UIController.AddElement(field);
            }
        }

        private void SelectRulesetPoint(RulesetPoint ruleset)
        {
            if (editor.SelectedBoundingBox == null)
                return;

            SelectedRuleset?.Element?.UpdateColor(new Vector4(0, 0, 0, 0));
            SelectedRuleset = ruleset;
            SelectedRuleset.Element.UpdateColor(new Vector4(0.25f, 0.25f, 0.25f, 1f));
            RulesetPosition = ruleset.Position;
            editor.UpdateBoundingBox = true;
        }
    }
}