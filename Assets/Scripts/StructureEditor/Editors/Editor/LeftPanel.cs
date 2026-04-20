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
        new UIVCol(w_full_minus_[2], h_full, blank_full_g_[20], hidden)[
            new UIVScroll(w_full, h_minus_[40f, 5], spacing_[5], mask_children)[
                new UICol(w_full_minus_[10], top_center, h_[25], top_[5], blank_sharp_g_[10])[
                    new UIField("", mc_[20], middle_left, left_[5]).Ref(ref StructureNameField)
                ],
                new UICol(w_full_minus_[10], top_center, h_[25], top_[5])[
                    new UICol(h_[25], w_half_minus_[2], blank_sharp_g_[25])
                    .OnClick(_ => Save())[
                        new UIText("Save", middle_center)
                    ],
                    new UICol(h_[25], w_half_minus_[2], blank_sharp_g_[25], top_right)
                    .OnClick(_ => {
                        if (StructureLoader.Load(_path, out var info) && info.StructureBoundingBoxes.Count > 0)
                        {
                            editor.BoundingBoxes = info.StructureBoundingBoxes;
                            editor.SelectedBoundingBox = info.StructureBoundingBoxes[0];
                            RegenerateBoundingBoxes();
                            editor.LeftUIPanel.Size = editor.SelectedBoundingBox.Size;
                            editor.LeftUIPanel.Position = editor.SelectedBoundingBox.SavePosition;
                            editor.LoadSelectedBoundingBox();
                        }
                    })[
                        new UIText("Load", middle_center)
                    ]
                ],
                new UIVScroll(w_full_minus_[10], top_center, h_[300], blank_sharp_g_[10], mask_children)
            ],
            new UIImg(w_full, h_[2], blank_full_g_[30], top_[5]),
            new UIVScroll(w_full, h_minus_[60f, 7], mask_children)[
                new UIVCol(w_full, h_[300], spacing_[5], top_[5])[
                    new UICol(w_full_minus_[10], h_[25], top_center)[
                        new UIText("STRUCTURES", fs_[1.2f], middle_left),
                        new UICol(w_[25], h_[25], middle_right, right_[30])[
                            new UIImg(w_full, h_full, icon_[22], bg_white).OnClick(img => {
                                editor.ShowBoundingBoxes = !editor.ShowBoundingBoxes;
                                img.UpdateIconIndex(editor.ShowBoundingBoxes ? 22 : 23);
                            })  
                        ],
                        new UICol(w_[25], h_[25], middle_right, blank_sharp_g_[25], hover_scale_easeout_[1.2f, 0.2f])[
                            new UIImg(w_full, h_full, icon_[16], bg_white).OnClick(_ => {
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
                            })
                        ]
                    ],
                    new UIVScroll(w_full_minus_[10], h_full_minus_[30], top_center, blank_sharp_g_[10], mask_children, border_[5, 5, 5, 5], spacing_[5])[
                        Foreach(editor.BoundingBoxes, BoundingBoxButton)
                    ].Ref(ref BoundingBoxPanel)
                ],
                new UIImg(w_full, h_[2], blank_full_g_[30], top_[5]),
                new UIVCol(w_full, grow_children, top_[5])[
                    new UICol(w_full_minus_[10], h_[25], top_center)[
                        new UIText("Settings", fs_[1.2f], middle_left)
                    ],
                    new UICol(w_full_minus_[10], h_[25], top_center)[
                        new UIText("Name", fs_[1f], middle_left)
                    ],
                    new UICol(w_full_minus_[10], h_[25], blank_sharp_g_[10], top_center)[
                        new UIField("Name", mc_[20], middle_left, left_[5]).OnTextChange(f => {
                            var text = editor.SelectedBoundingBox?.Element?.QueryElement<UIText>();
                            if (text != null && editor.SelectedBoundingBox != null)
                            {
                                text.UpdateText(f.GetText());
                                editor.SelectedBoundingBox.Name = f.GetText();
                            }
                        }).Ref(ref BoundingBoxNameField)
                    ],
                    new UICol(w_full_minus_[10], h_[25], top_center)[
                        new UIText("Is Core?", fs_[1f], middle_left),
                        new UIImg(w_[20], h_[20], blank_sharp_g_[10], middle_right).OnClick(i => {
                            if (editor.SelectedBoundingBox == null)
                                return;
                                
                            foreach (var bb in editor.BoundingBoxes)
                            {
                                if (bb != editor.SelectedBoundingBox)
                                    bb.Core = false;
                            }
                            editor.SelectedBoundingBox.Core = !editor.SelectedBoundingBox.Core;
                            i.UpdateColor(new Vector4(new Vector3(editor.SelectedBoundingBox.Core ? 0.3f : 0.1f), 1f));
                        }).Ref(ref IsCoreButton)
                    ],
                    new UICol(w_full_minus_[10], h_[25], top_center)[
                        new UIText("Scale", fs_[1f], middle_left)
                    ],
                    new UIVCol(w_full_minus_[10], h_[85], spacing_[5], top_center)[
                        new UICol(w_full, h_[25])[
                            new UIText("X", middle_left, fs_[1.2f]),
                            new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                new UIField("1", mc_[20], middle_left, left_[5]).Ref(ref SizeXField)
                            ],
                            ChangeElement(editor, SizeXField, 1, int.MaxValue, () => {})
                        ],
                        new UICol(w_full, h_[25])[
                            new UIText("Y", middle_left, fs_[1.2f]),
                            new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                new UIField("1", mc_[20], middle_left, left_[5]).Ref(ref SizeYField)
                            ],
                            ChangeElement(editor, SizeYField, 1, int.MaxValue, () => {})
                        ],
                        new UICol(w_full, h_[25])[
                            new UIText("Z", middle_left, fs_[1.2f]),
                            new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                new UIField("1", mc_[20], middle_left, left_[5]).Ref(ref SizeZField)
                            ],
                            ChangeElement(editor, SizeZField, 1, int.MaxValue, () => {})
                        ]
                    ],
                    new UICol(w_full_minus_[10], h_[25], top_center)[
                        new UIText("Position", fs_[1f], middle_left)
                    ],
                    new UIVCol(w_full_minus_[10], h_[85], spacing_[5], top_center)[
                        new UICol(w_full, h_[25])[
                            new UIText("X", middle_left, fs_[1.2f]),
                            new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                new UIField("0", mc_[20], middle_left, left_[5]).Ref(ref PositionXField)
                            ],
                            ChangeElement(editor, PositionXField, int.MinValue, int.MaxValue, () => {})
                        ],
                        new UICol(w_full, h_[25])[
                            new UIText("Y", middle_left, fs_[1.2f]),
                            new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                new UIField("0", mc_[20], middle_left, left_[5]).Ref(ref PositionYField)
                            ],
                            ChangeElement(editor, PositionYField, int.MinValue, int.MaxValue, () => {})
                        ],
                        new UICol(w_full, h_[25])[
                            new UIText("Z", middle_left, fs_[1.2f]),
                            new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                new UIField("0", mc_[20], middle_left, left_[5]).Ref(ref PositionZField)
                            ],
                            ChangeElement(editor, PositionZField, int.MinValue, int.MaxValue, () => {})
                        ]
                    ]
                ],
                new UICol(h_[25], w_full, top_[5], top_center, blank_sharp_g_[25])
                .OnClick(_ => editor.OpenScript())[
                    new UIText("Script", middle_center)
                ],
                new UICol(h_[25], w_full, top_[5], top_center, blank_sharp_g_[25])
                .OnClick(_ => editor.GenerateStructure())[
                    new UIText("Generate", middle_center)
                ],
                new UICol(h_[25], w_full, top_[5], top_center, blank_sharp_g_[25])
                .OnClick(_ => editor.ClearTerrain())[
                    new UIText("Clear Terrain", middle_center)
                ],
                new UICol(h_[25], w_full, top_[5], top_center, blank_sharp_g_[25])
                .OnClick(_ => editor.GenerateTerrain())[
                    new UIText("Generate Terrain", middle_center)
                ]
            ]
        ];

        private UIElementBase BoundingBoxButton(StructureData box) 
        {
            var col = new UICol(w_full_minus_[10], h_[25], top_center, blank_sharp, rgba_[0.25f, 0.25f, 0.25f, box == editor.SelectedBoundingBox ? 1 : 0]) 
            .OnClick(_ => Select(box))[
                new UIText(box.Name, mc_[20], middle_left, left_[5]),
                new UIImg(icon_[27], w_[20], h_[20], middle_right, right_[30], bg_white).OnClick(_ => editor.SaveSelectedBoundingBox()),
                new UIImg(icon_[18], w_[20], h_[20], middle_right, right_[5], bg_white).OnClick(_ => editor.DeleteBoundingBox(box))
            ];
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