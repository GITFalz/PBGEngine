using PBG.MathLibrary;
using PBG.MathLibrary;
using PBG.UI;
using PBG.UI.Creator;
using static PBG.UI.Styles;

public partial class StructureEditor
{
    public partial class RightPanel(StructureEditor editor) : UIScript
    {
        public override void AfterScript()
        {
            PreviousSection = BoundingBoxSection;
        }

        public override UIElementBase Script() =>
        new UICol(w_full, h_full, invisible)[
            new UICol(w_full, h_full, not_toggle_old_invisible)[
                new UICol(w_[70], h_[70], top_right, right_[230], blank_sharp_g_[30], depth_[-40])[
                    new UIImg(w_[60], h_[60], middle_left, item_["test_block"], bg_white).Ref(ref CurrentBlockImg)
                ],
                new UIVScroll(w_[70], h_full_minus_[70], bottom_right, right_[240], spacing_[10], border_[0, 10, 0, 10], mask_children, depth_[-40], scroll_speed_[30])
                .OnClick(_ => { }) // empty event to make the hovering element update
                [
                    Foreach(ItemDataManager.AllItems, (name, item) =>
                    {
                        if (item is BlockItemData blockItem)
                        {
                            return new UICol(left_[30], w_[100], h_[60], blank_sharp_g_[20], hover_translation_[(-20, 0)], hover_translation_duration_[0.3f], hover_translation_easeout, hover_color_[(0.2f, 0.2f, 0.2f, 1), (0.3f, 0.3f, 0.3f, 1)], hover_color_duration_[0.3f], hover_color_easeout)
                            .OnClick(_ => editor.SetBlock(name))[
                                new UIImg(w_[50], h_[50], middle_left, left_[5], item_[name], gray_[80])
                            ];
                        }
                        return null;
                    })
                ].Ref(ref BlockCollection)
            ].Ref(ref BlockSelectionPanel),
            new UIVScroll(w_full_minus_[2], h_full, blank_full_g_[20], top_right, mask_children, scroll_speed_[20], ignore_invisible)[
                new UIVCol(w_full, grow_children, top_[5], border_[5, 0, 5, 0], spacing_[5])[
                    new UICol(w_full_minus_[10], h_[20], blank_full_g_[30])
                    .OnClick(_ => SwitchPanel(SettingsSection))[
                        new UIText("SETTINGS", left_[5], middle_left)
                    ],
                    new UICol(w_full_minus_[10], h_[20], blank_full_g_[30])
                    .OnClick(_ => SwitchPanel(BoundingBoxSection))[
                        new UIText("BOUNDING BOXES", left_[5], middle_left)
                    ],
                    new UICol(w_full_minus_[10], h_[20], blank_full_g_[30])
                    .OnClick(_ => SwitchPanel(ExtendersSection))[
                        new UIText("EXTENTION BOXES", left_[5], middle_left)
                    ],
                    new UICol(w_full_minus_[10], h_[20], blank_full_g_[30])
                    .OnClick(_ => SwitchPanel(ConnectionPointSection))[
                        new UIText("CONNECTION POINTS", left_[5], middle_left)
                    ],
                    new UICol(w_full_minus_[10], h_[20], blank_full_g_[30])
                    .OnClick(_ => SwitchPanel(RulesetPointSection))[
                        new UIText("RULESET POINTS", left_[5], middle_left)
                    ]
                ],
                new UIVCol(w_full, top_[5], hidden, not_toggle_old_invisible, grow_children)[
                    new UIVCol(w_full, grow_children, top_[5])[
                        new UICol(w_full_minus_[10], h_[25], top_center)[
                            new UIText("Camera Speed", fs_[1.2f], middle_left)
                        ],
                        new UICol(w_full_minus_[10], h_[25], top_center)[
                            new UICol(w_full, h_[25])[
                                new UICol(w_[100f], h_full, blank_sharp_g_[10], top_right)[
                                    new UIField(""+editor.Camera.SPEED, mc_[5], middle_left, left_[5]).OnTextChange(f => {
                                        var value = f.GetFloat();
                                        value.ClampSety(0.01f, 100);
                                        editor.Camera.SetCameraSpeed(value);
                                    })
                                ]
                            ]
                        ]
                    ]
                ].Ref(ref SettingsSection),
                new UIVCol(w_full, top_[5], hidden, not_toggle_old_invisible, grow_children)[
                    new UIVCol(w_full, h_[300], spacing_[5], top_[5])[
                        new UICol(w_full_minus_[10], h_[25], top_center)[
                            new UIText("BOUNDING BOXES", fs_[1.2f], middle_left),
                            new UICol(w_[25], h_[25], middle_right, blank_sharp_g_[25], hover_scale_easeout_[1.2f, 0.2f])[
                                new UIImg(w_full, h_full, icon_[16], bg_white).OnClick(_ => {
                                    if (editor.SelectedBoundingBox == null)
                                        return;

                                    StructureBoundingBox boundingBox = new((1, 1, 1), (0, 0, 0));
                                    var button = BoundingBoxButton(editor.SelectedBoundingBox.BoundingBoxes.Count, boundingBox);
                                    BoundingBoxPanel.AddElement(button);
                                    UIController.AddElement(button);
                                    editor.SelectedBoundingBox.BoundingBoxes.Add(boundingBox);
                                    SelectBoundingBox(boundingBox);
                                })
                            ]
                        ],
                        new UIVScroll(w_full_minus_[10], h_full_minus_[30], top_center, blank_sharp_g_[10], mask_children, border_[5, 5, 5, 5], spacing_[5])[
                            Foreach(editor.SelectedBoundingBox?.BoundingBoxes ?? [], BoundingBoxButton)
                        ].Ref(ref BoundingBoxPanel)
                    ],
                    new UIImg(w_full, h_[2], blank_full_g_[30], top_[5]),
                    new UIVCol(w_full, grow_children, top_[5])[
                        new UICol(w_full_minus_[10], h_[25], top_center)[
                            new UIText("Settings", fs_[1.2f], middle_left)
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
                                ChangeElement(editor, SizeXField, 1, int.MaxValue, UpdateBoundingBoxData)
                            ],
                            new UICol(w_full, h_[25])[
                                new UIText("Y", middle_left, fs_[1.2f]),
                                new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                    new UIField("1", mc_[20], middle_left, left_[5]).Ref(ref SizeYField)
                                ],
                                ChangeElement(editor, SizeYField, 1, int.MaxValue, UpdateBoundingBoxData)
                            ],
                            new UICol(w_full, h_[25])[
                                new UIText("Z", middle_left, fs_[1.2f]),
                                new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                    new UIField("1", mc_[20], middle_left, left_[5]).Ref(ref SizeZField)
                                ],
                                ChangeElement(editor, SizeZField, 1, int.MaxValue, UpdateBoundingBoxData)
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
                                ChangeElement(editor, PositionXField, int.MinValue, int.MaxValue, UpdateBoundingBoxData)
                            ],
                            new UICol(w_full, h_[25])[
                                new UIText("Y", middle_left, fs_[1.2f]),
                                new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                    new UIField("0", mc_[20], middle_left, left_[5]).Ref(ref PositionYField)
                                ],
                                ChangeElement(editor, PositionYField, int.MinValue, int.MaxValue, UpdateBoundingBoxData)
                            ],
                            new UICol(w_full, h_[25])[
                                new UIText("Z", middle_left, fs_[1.2f]),
                                new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                    new UIField("0", mc_[20], middle_left, left_[5]).Ref(ref PositionZField)
                                ],
                                ChangeElement(editor, PositionZField, int.MinValue, int.MaxValue, UpdateBoundingBoxData)
                            ]
                        ]
                    ]
                ].Ref(ref BoundingBoxSection),
                new UIVCol(w_full, top_[5], hidden, not_toggle_old_invisible, grow_children)[
                    new UIVCol(w_full, h_[300], spacing_[5], top_[5])[
                        new UICol(w_full_minus_[10], h_[25], top_center)[
                            new UIText("EXTENSION BOXES", fs_[1.2f], middle_left),
                            new UICol(w_[25], h_[25], middle_right, blank_sharp_g_[25], hover_scale_easeout_[1.2f, 0.2f])[
                                new UIImg(w_full, h_full, icon_[16], bg_white).OnClick(_ => {
                                    if (editor.SelectedBoundingBox == null)
                                        return;

                                    StructureExtender extender = new((1, 0, 1), (0, 0, 0));
                                    var button = ExtenderButton(editor.SelectedBoundingBox.Extenders.Count, extender);
                                    ExtendersPanel.AddElement(button);
                                    UIController.AddElement(button);
                                    editor.SelectedBoundingBox.Extenders.Add(extender);
                                    SelectExtender(extender);
                                })
                            ]
                        ],
                        new UIVScroll(w_full_minus_[10], h_full_minus_[30], top_center, blank_sharp_g_[10], mask_children, border_[5, 5, 5, 5], spacing_[5])[
                            Foreach(editor.SelectedBoundingBox?.Extenders ?? [], ExtenderButton)
                        ].Ref(ref ExtendersPanel)
                    ],
                    new UIImg(w_full, h_[2], blank_full_g_[30], top_[5]),
                    new UIVCol(w_full, grow_children, top_[5])[
                        new UICol(w_full_minus_[10], h_[25], top_center)[
                            new UIText("Settings", fs_[1.2f], middle_left)
                        ],
                        new UICol(w_full_minus_[10], h_[25], top_center)[
                            new UIText("Scale", fs_[1f], middle_left)
                        ],
                        new UIVCol(w_full_minus_[10], h_[85], spacing_[5], top_center)[
                            new UICol(w_full, h_[25])[
                                new UIText("X", middle_left, fs_[1.2f]),
                                new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                    new UIField("1", mc_[20], middle_left, left_[5]).Ref(ref ExtenderSizeXField)
                                ],
                                ChangeElement(editor, ExtenderSizeXField, 1, int.MaxValue, UpdateExtenderData)
                            ],
                            new UICol(w_full, h_[25])[
                                new UIText("Z", middle_left, fs_[1.2f]),
                                new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                    new UIField("1", mc_[20], middle_left, left_[5]).Ref(ref ExtenderSizeZField)
                                ],
                                ChangeElement(editor, ExtenderSizeZField, 1, int.MaxValue, UpdateExtenderData)
                            ]
                        ],
                        new UICol(w_full_minus_[10], h_[25], top_center)[
                            new UIText("Position", fs_[1f], middle_left)
                        ],
                        new UIVCol(w_full_minus_[10], h_[85], spacing_[5], top_center)[
                            new UICol(w_full, h_[25])[
                                new UIText("X", middle_left, fs_[1.2f]),
                                new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                    new UIField("0", mc_[20], middle_left, left_[5]).Ref(ref ExtenderPositionXField)
                                ],
                                ChangeElement(editor, ExtenderPositionXField, int.MinValue, int.MaxValue, UpdateExtenderData)
                            ],
                            new UICol(w_full, h_[25])[
                                new UIText("Y", middle_left, fs_[1.2f]),
                                new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                    new UIField("0", mc_[20], middle_left, left_[5]).Ref(ref ExtenderPositionYField)
                                ],
                                ChangeElement(editor, ExtenderPositionYField, int.MinValue, int.MaxValue, UpdateExtenderData)
                            ],
                            new UICol(w_full, h_[25])[
                                new UIText("Z", middle_left, fs_[1.2f]),
                                new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                    new UIField("0", mc_[20], middle_left, left_[5]).Ref(ref ExtenderPositionZField)
                                ],
                                ChangeElement(editor, ExtenderPositionZField, int.MinValue, int.MaxValue, UpdateExtenderData)
                            ]
                        ]
                    ]
                ].Ref(ref ExtendersSection),
                new UIVCol(w_full, top_[5], hidden, not_toggle_old_invisible, grow_children)[
                    new UIVCol(w_full, h_[300], spacing_[5], top_[5])[
                        new UICol(w_full_minus_[10], h_[25], top_center)[
                            new UIText("CONNECTION POINTS", fs_[1.2f], middle_left),
                            new UICol(w_[25], h_[25], middle_right, blank_sharp_g_[25], hover_scale_easeout_[1.2f, 0.2f])[
                                new UIImg("test", w_full, h_full, icon_[16], bg_white).OnClick(_ => {
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
                                })
                            ]
                        ],
                        new UIVScroll(w_full_minus_[10], h_full_minus_[30], top_center, blank_sharp_g_[10], mask_children, border_[5, 5, 5, 5], spacing_[5])[
                            Foreach(editor.SelectedBoundingBox?.ConnectionPoints ?? [], ConnectionPointButton)
                        ].Ref(ref ConnectionPointsPanel)
                    ],
                    new UIImg(w_full, h_[2], blank_full_g_[30], top_[5]),
                    new UIVCol(w_full, grow_children, top_[5])[
                        new UIVCol(w_full_minus_[10], h_[55], spacing_[5], top_center)[
                            new UICol(w_full, h_[25])[
                                new UICol(w_[32f], h_full, top_left, blank_sharp_g_[20]).OnClick(_ => SetSide(0))[
                                    new UIText("Front", middle_center)
                                ],
                                new UICol(w_[32f], h_full, top_center, blank_sharp_g_[20]).OnClick(_ => SetSide(1))[
                                    new UIText("Right", middle_center)
                                ],
                                new UICol(w_[32f], h_full, top_right, blank_sharp_g_[20]).OnClick(_ => SetSide(2))[
                                    new UIText("Top", middle_center)
                                ]
                            ],
                            new UICol(w_full, h_[25])[
                                new UICol(w_[32f], h_full, top_left, blank_sharp_g_[20]).OnClick(_ => SetSide(3))[
                                    new UIText("Left", middle_center)
                                ],
                                new UICol(w_[32f], h_full, top_center, blank_sharp_g_[20]).OnClick(_ => SetSide(4))[
                                    new UIText("Bottom", middle_center)
                                ],
                                new UICol(w_[32f], h_full, top_right, blank_sharp_g_[20]).OnClick(_ => SetSide(5))[
                                    new UIText("Back", middle_center)
                                ]
                            ]
                        ],
                        new UICol(w_full_minus_[10], h_[25], top_center)[
                            new UIText("Position", fs_[1f], middle_left)
                        ],
                        new UIVCol(w_full_minus_[10], h_[85], spacing_[5], top_center)[
                            new UICol(w_full, h_[25])[
                                new UIText("X", middle_left, fs_[1.2f]),
                                new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                    new UIField("0", mc_[20], middle_left, left_[5]).Ref(ref ConnectionPositionXField)
                                ],
                                ChangeElement(editor, ConnectionPositionXField, int.MinValue, int.MaxValue, UpdateConnectionPosition)
                            ],
                            new UICol(w_full, h_[25])[
                                new UIText("Y", middle_left, fs_[1.2f]),
                                new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                    new UIField("0", mc_[20], middle_left, left_[5]).Ref(ref ConnectionPositionYField)
                                ],
                                ChangeElement(editor, ConnectionPositionYField, int.MinValue, int.MaxValue, UpdateConnectionPosition)
                            ],
                            new UICol(w_full, h_[25])[
                                new UIText("Z", middle_left, fs_[1.2f]),
                                new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                    new UIField("0", mc_[20], middle_left, left_[5]).Ref(ref ConnectionPositionZField)
                                ],
                                ChangeElement(editor, ConnectionPositionZField, int.MinValue, int.MaxValue, UpdateConnectionPosition)
                            ]
                        ]
                    ],
                    new UIImg(w_full, h_[2], blank_full_g_[30], top_[5]),
                    new UIVCol(w_full, h_[300], spacing_[5], top_[5])[
                        new UICol(w_full_minus_[10], h_[25], top_center)[
                            new UIText("CATEGORIES", fs_[1.2f], middle_left),
                            new UICol(w_[25], h_[25], middle_right, blank_sharp_g_[25], hover_scale_easeout_[1.2f, 0.2f])[
                                new UIImg(w_full, h_full, icon_[16], bg_white).OnClick(_ => { 
                                    if (SelectedConnection == null)
                                        return;

                                    var name = GetUniqueCategoryName(SelectedConnection);
                                    var field = new UIField(name, top_[5], left_[5], mc_[20]).OnTextChange(_ => {
                                        if (SelectedConnection == null)
                                            return;

                                        SelectedConnection.Categories = [];
                                        for (int i = 0; i < CategoriesPanel.ChildElements.Count; i++)
                                        {
                                            if (CategoriesPanel.ChildElements[i] is UIField f)
                                                SelectedConnection.Categories.Add(f.GetTrimmedText());
                                        }
                                    });
                                    CategoriesPanel.AddElement(field);
                                    UIController.AddElement(field);
                                })
                            ]
                        ],
                        new UIVScroll(w_full_minus_[10], h_full_minus_[30], top_center, blank_sharp_g_[10], mask_children, border_[5, 5, 5, 5], spacing_[5]).Ref(ref CategoriesPanel)
                    ],
                    new UIImg(w_full, h_[2], blank_full_g_[30], top_[5]),
                    new UIVCol(w_full, h_[300], spacing_[5], top_[5])[
                        new UICol(w_full_minus_[10], h_[25], top_center)[
                            new UIText("CATEGORIES TO AVOID", fs_[1.2f], middle_left),
                            new UICol(w_[25], h_[25], middle_right, blank_sharp_g_[25], hover_scale_easeout_[1.2f, 0.2f])[
                                new UIImg(w_full, h_full, icon_[16], bg_white).OnClick(_ => { 
                                    if (SelectedConnection == null)
                                        return;

                                    var field = new UIField("category", top_[5], left_[5], mc_[20]).OnTextChange(_ => {
                                        if (SelectedConnection == null)
                                            return;

                                        SelectedConnection.Avoid = [];
                                        for (int i = 0; i < AvoidPanel.ChildElements.Count; i++)
                                        {
                                            if (AvoidPanel.ChildElements[i] is UIField f)
                                                SelectedConnection.Avoid.Add(f.GetTrimmedText());
                                        }
                                    });
                                    AvoidPanel.AddElement(field);
                                    UIController.AddElement(field);
                                })
                            ]
                        ],
                        new UIVScroll(w_full_minus_[10], h_full_minus_[30], top_center, blank_sharp_g_[10], mask_children, border_[5, 5, 5, 5], spacing_[5]).Ref(ref AvoidPanel)
                    ]
                ].Ref(ref ConnectionPointSection),
                new UIVCol(w_full, top_[5], hidden, not_toggle_old_invisible, grow_children)[
                    new UIVCol(w_full, h_[300], spacing_[5], top_[5])[
                        new UICol(w_full_minus_[10], h_[25], top_center)[
                            new UIText("RULESETS", fs_[1.2f], middle_left),
                            new UICol(w_[25], h_[25], middle_right, blank_sharp_g_[25], hover_scale_easeout_[1.2f, 0.2f])[
                                new UIImg(w_full, h_full, icon_[16], bg_white).OnClick(_ => {
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
                                })
                            ]
                        ],
                        new UIVScroll(w_full_minus_[10], h_full_minus_[30], top_center, blank_sharp_g_[10], mask_children, border_[5, 5, 5, 5], spacing_[5])[
                            Foreach(editor.SelectedBoundingBox?.RulesetPoints ?? [], RulesetPointButton)
                        ].Ref(ref RulesetPointsPanel)
                    ],
                    new UIImg(w_full, h_[2], blank_full_g_[30], top_[5]),
                    new UIVCol(w_full, grow_children, top_[5])[
                        new UICol(w_full_minus_[10], h_[25], top_center)[
                            new UIText("Position", fs_[1f], middle_left)
                        ],
                        new UIVCol(w_full_minus_[10], h_[85], spacing_[5], top_center)[
                            new UICol(w_full, h_[25])[
                                new UIText("X", middle_left, fs_[1.2f]),
                                new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                    new UIField("0", mc_[20], middle_left, left_[5]).Ref(ref RulesetPositionXField)
                                ],
                                ChangeElement(editor, RulesetPositionXField, int.MinValue, int.MaxValue, UpdateRulesetPosition)
                            ],
                            new UICol(w_full, h_[25])[
                                new UIText("Y", middle_left, fs_[1.2f]),
                                new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                    new UIField("0", mc_[20], middle_left, left_[5]).Ref(ref RulesetPositionYField)
                                ],
                                ChangeElement(editor, RulesetPositionYField, int.MinValue, int.MaxValue, UpdateRulesetPosition)
                            ],
                            new UICol(w_full, h_[25])[
                                new UIText("Z", middle_left, fs_[1.2f]),
                                new UICol(w_[60f], h_full, blank_sharp_g_[10], top_right, right_[65])[
                                    new UIField("0", mc_[20], middle_left, left_[5]).Ref(ref RulesetPositionZField)
                                ],
                                ChangeElement(editor, RulesetPositionZField, int.MinValue, int.MaxValue, UpdateRulesetPosition)
                            ]
                        ]
                    ]
                ].Ref(ref RulesetPointSection)
            ]
        ];

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
            var col = new UICol(w_full_minus_[10], h_[25], top_center, blank_sharp_g_[10]).OnClick(_ => SelectBoundingBox(boundingBox))[
                new UIText(""+index, mc_[20], middle_left, left_[5]),
                new UIImg(icon_[18], w_[20], h_[20], middle_right, right_[5], bg_white).OnClick(c =>
                {
                    if (editor.SelectedBoundingBox == null)
                        return;

                    c.ParentElement?.Delete();
                    editor.SelectedBoundingBox.BoundingBoxes.Remove(boundingBox);
                    if (boundingBox == SelectedBoundingBox)
                        SelectedBoundingBox = null;

                    RegenerateBoundingBoxes();
                })
            ];
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
            var col = new UICol(w_full_minus_[10], h_[25], top_center, blank_sharp_g_[10]).OnClick(_ => SelectExtender(extender))[
                new UIText(""+index, mc_[20], middle_left, left_[5]),
                new UIImg(icon_[18], w_[20], h_[20], middle_right, right_[5], bg_white).OnClick(c =>
                {
                    if (editor.SelectedBoundingBox == null)
                        return;

                    c.ParentElement?.Delete();
                    editor.SelectedBoundingBox.Extenders.Remove(extender);
                    if (extender == SelectedExtender)
                        SelectedExtender = null;

                    RegenerateExtenders();
                })
            ];
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
            var text = new UIText(name, mc_[20], middle_left, left_[5]);
            var col = new UICol(w_full_minus_[10], h_[25], top_center, blank_sharp_g_[10]).OnClick(_ => SelectConnectionPoint(connection))[
                text,
                new UIImg(icon_[18], w_[20], h_[20], middle_right, right_[5], bg_white).OnClick(c =>
                {
                    if (editor.SelectedBoundingBox == null)
                        return;
                    c.ParentElement?.Delete();
                    editor.SelectedBoundingBox.ConnectionPoints.Remove(text.GetText());
                    if (connection == SelectedConnection)
                        SelectedConnection = null;

                    RegenerateConnectionPoints();
                })
            ];
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
            var text = new UIText(name, mc_[20], middle_left, left_[5]);
            var col = new UICol(w_full_minus_[10], h_[25], top_center, blank_sharp_g_[10])
            .OnClick(_ => SelectRulesetPoint(ruleset))[
                text,
                new UIImg(icon_[18], w_[20], h_[20], middle_right, right_[5], bg_white).OnClick(c =>
                {
                    if (editor.SelectedBoundingBox == null)
                        return;
                    c.ParentElement?.Delete();
                    editor.SelectedBoundingBox.RulesetPoints.Remove(text.GetText());
                    if (ruleset == SelectedRuleset)
                        SelectedRuleset = null;

                    RegenerateRulesetPoints();
                })
            ];
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

                var field = new UIField(cat, top_[5], left_[5], mc_[20]).OnTextChange(_ => {
                    if (SelectedConnection == null)
                        return;

                    SelectedConnection.Categories = [];
                    for (int i = 0; i < CategoriesPanel.ChildElements.Count; i++)
                    {
                        if (CategoriesPanel.ChildElements[i] is UIField f)
                            SelectedConnection.Categories.Add(f.GetTrimmedText());
                    }
                });
                CategoriesPanel.AddElement(field);
                UIController.AddElement(field);
            }

            for (int i = 0; i < connection.Avoid.Count; i++)
            {
                var cat = SelectedConnection.Avoid[i];

                var field = new UIField(cat, top_[5], left_[5], mc_[20]).OnTextChange(_ => {
                    if (SelectedConnection == null)
                        return;

                    SelectedConnection.Avoid = [];
                    for (int i = 0; i < AvoidPanel.ChildElements.Count; i++)
                    {
                        if (AvoidPanel.ChildElements[i] is UIField f)
                            SelectedConnection.Avoid.Add(f.GetTrimmedText());
                    }
                });
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