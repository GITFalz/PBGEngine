using System.Reflection;
using PBG.Core;
using PBG.MathLibrary;
using PBG.UI;
using static PBG.UI2.Styles;

namespace PBG.Editor;

public partial class EditorUI
{
    private UIVScroll _nodeInspector = null!;
    private bool HoveringOverInspector = false;
    private SceneBlueprintNode? _selectedNode = null;

    public UIElementBase RightPanel =>
    new UIVCol().Class(w_[240], h_full_minus_[30], blank_full, rgba_v4_[Bg1], bottom_right, border_ui_[2, 0, 0, 0], border_color_[Border])[
        new UICol().Class(w_full_minus_[2], h_[30], blank_full, rgba_v4_[Bg2], border_ui_[0, 2, 0, 2], border_color_[Border], top_right)[
            new UIText("INSPECTOR").Class(middle_left, left_[10], fs_[1.2f], rgba_v4_[Text2])
        ],
        new UIVScroll().Ref(ref _nodeInspector).Class(w_full_minus_[2], top_right, h_full_minus_[30], mask_children, ignore_invisible)
        .OnHoverEnter(_ => HoveringOverInspector = true)
        .OnHoverExit(_ => HoveringOverInspector = false)
    ];

    private void RefreshInspectorPanel(SceneBlueprintNode blueprint)
    {
        _selectedNode = blueprint;
        _nodeInspector.DeleteChildren();
        _nodeInspector.AddElement(NodeTransformUI(blueprint));
        for (int i = 0; i < blueprint.Scripts.Count; i++)
            _nodeInspector.AddElement(NodeScriptUI(blueprint.Scripts[i]));
        _nodeInspector.UIController?.AddElements(_nodeInspector.ChildElements);
    }

    private UIElementBase NodeTransformUI(SceneBlueprintNode blueprint)
    {
        TransformNode node;
        if (!EditorManager.Stopped && blueprint.RuntimeNode != null)
        {
            node = blueprint.RuntimeNode;
        }
        else
        {
            node = blueprint.Transform;
        }

        // Set rotation axis
        void SetRA(int index, float value)
        {
            var rotation = node.EulerRotation;
            rotation[index] = value;
            node.EulerRotation = rotation;
        }

        var col = new UIVCol().Class(w_full, grow_children)[
            new UICol().Class(w_full, h_[30], border_ui_[0, 2, 0, 0], border_color_[Border], blank_full, rgba_v4_[Bg2])[
                new UIText("Transform").Class(fs_[1.2f], middle_left, left_[5])
            ],
            new UIVCol().Class(w_full, grow_children, spacing_[5], border_[0, 0, 0, 10])[
                new UIVCol().Class(w_full, grow_children, spacing_[5])[
                    new UIText("Position").Class(fs_[0.9f], top_left, left_[5], top_[5]),
                    new UICol().Class(w_full_minus_[10], h_[20], top_center)[
                        new UICol().Class(mask_children, w_[30f], h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2])[
                            new UIField(""+node.Position.X).Out(out var xPositionField).Class(mc_[13], fs_[0.9f], middle_left, left_[5]).OnTextChange(f => node.Position.X = f.GetFloat(0))
                        ],
                        new UICol().Class(mask_children, w_[30f], h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2], middle_center)[
                            new UIField(""+node.Position.X).Out(out var yPositionField).Class(mc_[13], fs_[0.9f], middle_left, left_[5]).OnTextChange(f => node.Position.Y = f.GetFloat(0))
                        ],
                        new UICol().Class(mask_children, w_[30f], h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2], middle_right)[
                            new UIField(""+node.Position.X).Out(out var zPositionField).Class(mc_[13], fs_[0.9f], middle_left, left_[5]).OnTextChange(f => node.Position.Z = f.GetFloat(0))
                        ]
                    ]
                ],
                new UIVCol().Class(w_full, grow_children, spacing_[5])[
                    new UIText("Rotation").Class(fs_[0.9f], top_left, left_[5], top_[5]),
                    new UICol().Class(w_full_minus_[10], h_[20], top_center)[
                        new UICol().Class(mask_children, w_[30f], h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2])[
                            new UIField(""+node.Rotation.ToEuler().X).Out(out var xRotationField).Class(mc_[13], fs_[0.9f], middle_left, left_[5]).OnTextChange(f => SetRA(0, f.GetFloat(0)))
                        ],
                        new UICol().Class(mask_children, w_[30f], h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2], middle_center)[
                            new UIField(""+node.Rotation.ToEuler().Y).Out(out var yRotationField).Class(mc_[13], fs_[0.9f], middle_left, left_[5]).OnTextChange(f => SetRA(1, f.GetFloat(0)))
                        ],
                        new UICol().Class(mask_children, w_[30f], h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2], middle_right)[
                            new UIField(""+node.Rotation.ToEuler().Z).Out(out var zRotationField).Class(mc_[13], fs_[0.9f], middle_left, left_[5]).OnTextChange(f => SetRA(2, f.GetFloat(0)))
                        ]
                    ]
                ],
                new UIVCol().Class(w_full, grow_children, spacing_[5])[
                    new UIText("Scale").Class(fs_[0.9f], top_left, left_[5], top_[5]),
                    new UICol().Class(w_full_minus_[10], h_[20], top_center)[
                        new UICol().Class(mask_children, w_[30f], h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2])[
                            new UIField(""+node.Scale.X).Out(out var xScaleField).Class(mc_[13], fs_[0.9f], middle_left, left_[5]).OnTextChange(f => node.Scale.X = f.GetFloat(1))
                        ],
                        new UICol().Class(mask_children, w_[30f], h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2], middle_center)[
                            new UIField(""+node.Scale.Y).Out(out var yScaleField).Class(mc_[13], fs_[0.9f], middle_left, left_[5]).OnTextChange(f => node.Scale.Y = f.GetFloat(1))
                        ],
                        new UICol().Class(mask_children, w_[30f], h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2], middle_right)[
                            new UIField(""+node.Scale.Z).Out(out var zScaleField).Class(mc_[13], fs_[0.9f], middle_left, left_[5]).OnTextChange(f => node.Scale.Z = f.GetFloat(1))
                        ]
                    ]
                ]
            ]
        ];

        var positionWatcher = new Vector3Watcher(() => node.Position, xPositionField, yPositionField, zPositionField);
        var rotationWatcher = new Vector3Watcher(() => node.EulerRotation, xRotationField, yRotationField, zRotationField);
        var scaleWatcher = new Vector3Watcher(() => node.Scale, xScaleField, yScaleField, zScaleField);

        EditorWatcher.EditorWatchers.Add(positionWatcher);
        EditorWatcher.EditorWatchers.Add(rotationWatcher);
        EditorWatcher.EditorWatchers.Add(scaleWatcher);

        Console.WriteLine(EditorWatcher.EditorWatchers.Count);

        return col;
    }

    private UIElementBase NodeScriptUI(ScriptBlueprint blueprint) =>
    new UIVCol().Class(w_full, grow_children)[
        new UICol().Class(w_full, h_[30], border_ui_[0, 2, 0, 0], border_color_[Border], border_[5, 0, 0, 0], blank_full, rgba_v4_[Bg2])[
            new UIText(blueprint.Name).Class(fs_[1.2f], mc_[blueprint.Name.Length], middle_left, blueprint.IsScriptValid ? text_white : text_red)
        ],
        Foreach(blueprint.GetMembers(), member => 
            GetFieldSection(member)
        )
    ];

    private UIElementBase GetFieldSection(MemberInfo memberInfo)
    {
        return new UICol().Class(w_full, h_[30], border_[5, 0, 0, 0])[
            new UIText(memberInfo.Name).Class(middle_left)
        ];
    }
}