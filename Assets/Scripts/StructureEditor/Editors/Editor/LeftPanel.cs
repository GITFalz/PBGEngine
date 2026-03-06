using PBG;
using PBG.MathLibrary;
using PBG.UI;
using PBG.UI.Creator;
using PBG.Voxel;
using static PBG.UI.Styles;

public partial class StructureEditor
{
    public class LeftPanel(StructureEditor editor) : UIScript
    {
        private UIField StructureNameField = null!;

        private UIVScroll BoundingBoxPanel = null!;

        private UIField BoundingBoxNameField = null!;

        private UIField SizeXField = null!;
        private UIField SizeYField = null!;
        private UIField SizeZField = null!;

        private UIField PositionXField = null!;
        private UIField PositionYField = null!;
        private UIField PositionZField = null!;

        private UIImg IsCoreButton = null!;

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

        private string _name => StructureNameField.GetTrimmedText().Length == 0 ? "Base" : StructureNameField.GetTrimmedText();
        private string _path => Path.Combine(Game.CustomPath, "structures", _name);

        public void Save() => StructureLoader.Save(_name, editor.BoundingBoxes);

        public override UIElementBase Script() =>
        new UIVCol(Class(w_full_minus_[2], h_full, blank_full_g_[20], hidden), Sub([
            new UIVScroll(Class(w_full, h_minus_[40f, 5], spacing_[5], mask_children), [
                new UICol(Class(w_full_minus_[10], top_center, h_[25], top_[5], blank_sharp_g_[10]), Sub(
                    newField("", Class(mc_[20], middle_left, left_[5]), ref StructureNameField)
                )),
                new UICol(Class(w_full_minus_[10], top_center, h_[25], top_[5]), Sub(
                    new UICol(Class(h_[25], w_half_minus_[2], blank_sharp_g_[25]), 
                    OnClickCol(_ => Save()),
                    Sub(
                        new UIText("Save", Class(middle_center))
                    )),
                    new UICol(Class(h_[25], w_half_minus_[2], blank_sharp_g_[25], top_right), 
                    OnClickCol(_ => {
                        if (StructureLoader.Load(_path, out var info) && info.StructureBoundingBoxes.Count > 0)
                        {
                            editor.BoundingBoxes = info.StructureBoundingBoxes;
                            editor.SelectedBoundingBox = info.StructureBoundingBoxes[0];
                            RegenerateBoundingBoxes();
                            editor.LeftUIPanel.Size = editor.SelectedBoundingBox.Size;
                            editor.LeftUIPanel.Position = editor.SelectedBoundingBox.SavePosition;
                            editor.LoadSelectedBoundingBox();
                        }
                    }),
                    Sub(
                        new UIText("Load", Class(middle_center))
                    ))
                )),
                new UIVScroll(Class(w_full_minus_[10], top_center, h_[300], blank_sharp_g_[10], mask_children), Sub([
                    
                ]))
            ]),
            new UIImg(Class(w_full, h_[2], blank_full_g_[30], top_[5])),
            new UIVScroll(Class(w_full, h_minus_[60f, 7], mask_children), [
                new UIVCol(Class(w_full, h_[300], spacing_[5], top_[5]), Sub([
                    new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                        new UIText("STRUCTURES", Class(fs_[1.2f], middle_left)),
                        new UICol(Class(w_[25], h_[25], middle_right, right_[30]), [
                            new UIImg(Class(w_full, h_full, icon_[22], bg_white), OnClickImg(img => {
                                editor.ShowBoundingBoxes = !editor.ShowBoundingBoxes;
                                img.UpdateIconIndex(editor.ShowBoundingBoxes ? 22 : 23);
                            }))  
                        ]),
                        new UICol(Class(w_[25], h_[25], middle_right, blank_sharp_g_[25], hover_scale_easeout_[1.2f, 0.2f]), Sub(
                            new UIImg(Class(w_full, h_full, icon_[16], bg_white), OnClickImg(_ => {
                                string name = "Bounding Box";
                                HashSet<string> names = [];
                                for (int i = 0; i < editor.BoundingBoxes.Count; i++)
                                {
                                    names.Add(editor.BoundingBoxes[i].Name);
                                }
                                int j = 1;
                                while (names.Contains(name))
                                {
                                    name = $"Bounding Box {j}";
                                    j++;
                                }
                                var box = new StructureData()
                                {
                                    Name = name,
                                    Size = (1, 1, 1),
                                    Blocks = [Block.Air]
                                };
                                var button = BoundingBoxButton(box);
                                BoundingBoxPanel.AddElement(button);
                                UIController.AddElement(button);
                                editor.BoundingBoxes.Add(box);
                                Select(box);
                            }))
                        ))
                    )),
                    newVScroll(Class(w_full_minus_[10], h_full_minus_[30], top_center, blank_sharp_g_[10], mask_children, border_[5, 5, 5, 5], spacing_[5]), Sub([
                        ..Foreach(editor.BoundingBoxes, BoundingBoxButton)
                    ]), ref BoundingBoxPanel)
                ])),
                new UIImg(Class(w_full, h_[2], blank_full_g_[30], top_[5])),
                new UIVCol(Class(w_full, grow_children, top_[5]), Sub([
                    new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                        new UIText("Settings", Class(fs_[1.2f], middle_left))
                    )),
                    new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                        new UIText("Name", Class(fs_[1f], middle_left))
                    )),
                    new UICol(Class(w_full_minus_[10], h_[25], blank_sharp_g_[10], top_center), Sub(
                        newField("Name", Class(mc_[20], middle_left, left_[5]), OnTextChange(f => {
                            var text = editor.SelectedBoundingBox?.Element?.QueryElement<UIText>();
                            if (text != null && editor.SelectedBoundingBox != null)
                            {
                                text.UpdateText(f.GetText());
                                editor.SelectedBoundingBox.Name = f.GetText();
                            }
                        }), ref BoundingBoxNameField)
                    )),
                    new UICol(Class(w_full_minus_[10], h_[25], top_center), Sub(
                        new UIText("Is Core?", Class(fs_[1f], middle_left)),
                        newImg(Class(w_[20], h_[20], blank_sharp_g_[10], middle_right), OnClickImg(i => {
                            if (editor.SelectedBoundingBox == null)
                                return;
                                
                            foreach (var bb in editor.BoundingBoxes)
                            {
                                if (bb != editor.SelectedBoundingBox)
                                    bb.Core = false;
                            }
                            editor.SelectedBoundingBox.Core = !editor.SelectedBoundingBox.Core;
                            i.UpdateColor(new Vector4(new Vector3(editor.SelectedBoundingBox.Core ? 0.3f : 0.1f), 1f));
                        }), ref IsCoreButton)
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
                            new ChangeElement(editor, SizeXField, 1, int.MaxValue, () => {})
                        )),
                        new UICol(Class(w_full, h_[25]), Sub(
                            new UIText("Y", Class(middle_left, fs_[1.2f])),
                            new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                newField("1", Class(mc_[20], middle_left, left_[5]), ref SizeYField)
                            )),
                            new ChangeElement(editor, SizeYField, 1, int.MaxValue, () => {})
                        )),
                        new UICol(Class(w_full, h_[25]), Sub(
                            new UIText("Z", Class(middle_left, fs_[1.2f])),
                            new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                newField("1", Class(mc_[20], middle_left, left_[5]), ref SizeZField)
                            )),
                            new ChangeElement(editor, SizeZField, 1, int.MaxValue, () => {})
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
                            new ChangeElement(editor, PositionXField, int.MinValue, int.MaxValue, () => {})
                        )),
                        new UICol(Class(w_full, h_[25]), Sub(
                            new UIText("Y", Class(middle_left, fs_[1.2f])),
                            new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                newField("0", Class(mc_[20], middle_left, left_[5]), ref PositionYField)
                            )),
                            new ChangeElement(editor, PositionYField, int.MinValue, int.MaxValue, () => {})
                        )),
                        new UICol(Class(w_full, h_[25]), Sub(
                            new UIText("Z", Class(middle_left, fs_[1.2f])),
                            new UICol(Class(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65]), Sub(
                                newField("0", Class(mc_[20], middle_left, left_[5]), ref PositionZField)
                            )),
                            new ChangeElement(editor, PositionZField, int.MinValue, int.MaxValue, () => {})
                        ))
                    ))
                ])),
                new UICol(Class(h_[25], w_full, top_[5], top_center, blank_sharp_g_[25]), 
                OnClickCol(_ => editor.OpenScript()),
                Sub(
                    new UIText("Script", Class(middle_center))
                )),
                new UICol(Class(h_[25], w_full, top_[5], top_center, blank_sharp_g_[25]), 
                OnClickCol(_ => editor.GenerateStructure()),
                Sub(
                    new UIText("Generate", Class(middle_center))
                )),
                new UICol(Class(h_[25], w_full, top_[5], top_center, blank_sharp_g_[25]), 
                OnClickCol(_ => editor.ClearTerrain()),
                Sub(
                    new UIText("Clear Terrain", Class(middle_center))
                )),
                new UICol(Class(h_[25], w_full, top_[5], top_center, blank_sharp_g_[25]), 
                OnClickCol(_ => editor.GenerateTerrain()),
                Sub(
                    new UIText("Generate Terrain", Class(middle_center))
                ))
            ])
        ]));

        private UIElementBase BoundingBoxButton(StructureData box) 
        {
            var col = new UICol(Class(w_full_minus_[10], h_[25], top_center, blank_sharp, rgba_[0.25f, 0.25f, 0.25f, box == editor.SelectedBoundingBox ? 1 : 0]), 
            OnClickCol(_ => Select(box)),
            Sub(
                new UIText(box.Name, Class(mc_[20], middle_left, left_[5])),
                new UIImg(Class(icon_[27], w_[20], h_[20], middle_right, right_[30], bg_white), OnClickImg(_ => editor.SaveSelectedBoundingBox())),
                new UIImg(Class(icon_[18], w_[20], h_[20], middle_right, right_[5], bg_white), OnClickImg(_ => editor.DeleteBoundingBox(box)))
            ));
            box.Element = col;
            return col;
        }

        public void RegenerateBoundingBoxes()
        {
            BoundingBoxPanel.DeleteChildren();
            if (editor.SelectedBoundingBox == null)
                return;

            for (int i = 0; i < editor.BoundingBoxes.Count; i++)
            {
                var boundingBox = editor.BoundingBoxes[i];
                var button = BoundingBoxButton(boundingBox);
                BoundingBoxPanel.AddElement(button);
                UIController.AddElement(button);
            }
            editor.RightUIPanel.SelectedConnection = null;
            editor.RightUIPanel.SelectedRuleset = null;
            editor.RightUIPanel.RegenerateBoundingBoxes();
            editor.RightUIPanel.RegenerateExtenders();
            editor.RightUIPanel.RegenerateConnectionPoints();
            editor.RightUIPanel.RegenerateRulesetPoints();
            IsCoreButton.UpdateColor(new Vector4(new Vector3(editor.SelectedBoundingBox.Core ? 0.3f : 0.1f), 1f));
            editor.UpdateBoundingBox = true;
        }

        private void Select(StructureData box)
        {
            if (editor.SelectedBoundingBox == box)
                return;

            editor.SelectedBoundingBox?.Element.UpdateColor(new Vector4(0, 0, 0, 0));
            editor.SelectedBoundingBox = box;
            editor.LoadSelectedBoundingBox();
            editor.RightUIPanel.SelectedConnection = null;
            editor.RightUIPanel.SelectedRuleset = null;
            editor.RightUIPanel.RegenerateBoundingBoxes();
            editor.RightUIPanel.RegenerateExtenders();
            editor.RightUIPanel.RegenerateConnectionPoints();
            editor.RightUIPanel.RegenerateRulesetPoints();
            box.Element.UpdateColor(new Vector4(0.25f, 0.25f, 0.25f, 1f));
            IsCoreButton.UpdateColor(new Vector4(new Vector3(editor.SelectedBoundingBox.Core ? 0.3f : 0.1f), 1f));
            Size = box.Size;
            Position = box.SavePosition;
            editor.UpdateBoundingBox = true;
            BoundingBoxNameField.UpdateText(box.Name);
            editor.ScriptUI.SetLines(editor.SelectedBoundingBox.Lines);
        }
    }
}