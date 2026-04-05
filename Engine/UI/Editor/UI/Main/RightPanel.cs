using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using PBG.Core;
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.UI;
using static PBG.UI.Styles;

namespace PBG.Editor;

public partial class EditorUI
{
    private UIVScroll _nodeInspector = null!;
    private bool HoveringOverInspector = false;
    private SceneDefinitionNode? _selectedNode = null;

    public UIElementBase RightPanel =>
    new UIVCol().Class(w_[240], h_full_minus_[30], blank_full, rgba_v4_[Bg1], bottom_right, border_ui_[2, 0, 0, 0], border_color_[Border])[
        new UICol().Class(w_full_minus_[2], h_[30], blank_full, rgba_v4_[Bg2], border_ui_[0, 2, 0, 2], border_color_[Border], top_right)[
            new UIText("INSPECTOR").Class(middle_left, left_[10], fs_[1.2f], rgba_v4_[Text2])
        ],
        new UIVScroll().Ref(ref _nodeInspector).Class(w_full_minus_[2], top_right, h_full_minus_[30], mask_children, ignore_invisible)
        .OnHoverEnter(_ => HoveringOverInspector = true)
        .OnHoverExit(_ => HoveringOverInspector = false)
    ];

    private void RefreshInspectorPanel(SceneDefinitionNode? node, TransformNode transform)
    {
        EditorWatcher.Clear();
        _selectedNode = node;
        _nodeInspector.DeleteChildren();
        _nodeInspector.AddElement(NodeTransformUI(transform));
        if (node != null)
        {
            foreach (var script in node.GetScripts())
                _nodeInspector.AddElement(NodeScriptUI(script.ScriptingNode, script.GetMembers(), script.IsScriptValid));
        }
        else
        {
            foreach (var script in transform.Components)
                _nodeInspector.AddElement(NodeScriptUI(script, script.GetMembers(), true));
        }

        _nodeInspector.UIController?.AddElements(_nodeInspector.ChildElements);
    }

    private UIElementBase NodeTransformUI(TransformNode node)
    {
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
                    new UIHCol().Class(w_full_minus_[10], h_[20], top_center, fit_children, spacing_[10])[
                        new UICol().Class(mask_children, w_full, h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2])[
                            new UIField(""+node.Position.X).Out(out var xPositionField).Class(mc_[13], fs_[0.9f], middle_left, left_[5]).OnTextChange(f => node.Position.X = f.GetFloat(0))
                        ],
                        new UICol().Class(mask_children, w_full, h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2])[
                            new UIField(""+node.Position.Y).Out(out var yPositionField).Class(mc_[13], fs_[0.9f], middle_left, left_[5]).OnTextChange(f => node.Position.Y = f.GetFloat(0))
                        ],
                        new UICol().Class(mask_children, w_full, h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2])[
                            new UIField(""+node.Position.Z).Out(out var zPositionField).Class(mc_[13], fs_[0.9f], middle_left, left_[5]).OnTextChange(f => node.Position.Z = f.GetFloat(0))
                        ]
                    ]
                ],
                new UIVCol().Class(w_full, grow_children, spacing_[5])[
                    new UIText("Rotation").Class(fs_[0.9f], top_left, left_[5], top_[5]),
                    new UIHCol().Class(w_full_minus_[10], h_[20], top_center, fit_children, spacing_[10])[
                        new UICol().Class(mask_children, w_full, h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2])[
                            new UIField(""+node.Rotation.ToEuler().X).Out(out var xRotationField).Class(mc_[13], fs_[0.9f], middle_left, left_[5]).OnTextChange(f => SetRA(0, f.GetFloat(0)))
                        ],
                        new UICol().Class(mask_children, w_full, h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2])[
                            new UIField(""+node.Rotation.ToEuler().Y).Out(out var yRotationField).Class(mc_[13], fs_[0.9f], middle_left, left_[5]).OnTextChange(f => SetRA(1, f.GetFloat(0)))
                        ],
                        new UICol().Class(mask_children, w_full, h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2])[
                            new UIField(""+node.Rotation.ToEuler().Z).Out(out var zRotationField).Class(mc_[13], fs_[0.9f], middle_left, left_[5]).OnTextChange(f => SetRA(2, f.GetFloat(0)))
                        ]
                    ]
                ],
                new UIVCol().Class(w_full, grow_children, spacing_[5])[
                    new UIText("Scale").Class(fs_[0.9f], top_left, left_[5], top_[5]),
                    new UIHCol().Class(w_full_minus_[10], h_[20], top_center, fit_children, spacing_[10])[
                        new UICol().Class(mask_children, w_full, h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2])[
                            new UIField(""+node.Scale.X).Out(out var xScaleField).Class(mc_[13], fs_[0.9f], middle_left, left_[5]).OnTextChange(f => node.Scale.X = f.GetFloat(1))
                        ],
                        new UICol().Class(mask_children, w_full, h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2])[
                            new UIField(""+node.Scale.Y).Out(out var yScaleField).Class(mc_[13], fs_[0.9f], middle_left, left_[5]).OnTextChange(f => node.Scale.Y = f.GetFloat(1))
                        ],
                        new UICol().Class(mask_children, w_full, h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2])[
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

        return col;
    }

    private UIElementBase NodeScriptUI(ScriptingNode? script, MemberInfo[] members, bool isValid)
    {
        string? name = script?.GetType().Name;
        return new UIVCol().Class(w_full, grow_children)[
            new UICol().Class(w_full, h_[30], border_ui_[0, 2, 0, 0], border_color_[Border], border_[5, 0, 0, 0], blank_full, rgba_v4_[Bg2])[
                new UIText(name ?? "Undefined").Class(fs_[1.2f], mc_[name?.Length ?? 9], middle_left, isValid ? text_white : text_red)
            ],
            Foreach(members, member => script != null ? GetFieldSection(script, member) : null)
        ];
    }

    private UIElementBase GetFieldSection(ScriptingNode script, MemberInfo memberInfo)
    {
        if (memberInfo is FieldInfo fieldInfo)
        {
            var element = GetValues(script, fieldInfo);
            if (element != null)
            {
                return new UIVCol().Class(w_full, grow_children, spacing_[5])[
                    new UIText(fieldInfo.Name).Class(fs_[0.9f], top_left, left_[5], top_[5]),
                    element
                ];
            }
        }
        return new UICol().Class(w_full, h_[30], border_[5, 0, 0, 0])[
            new UIText(memberInfo.Name).Class(middle_left)
        ];
    }

    private UIElementBase? GetValues(ScriptingNode script, FieldInfo fieldInfo)
    {
        var value = fieldInfo.GetValue(script);
        if (value is Mesh mesh && mesh.IsCached)
        {
            return new UICol().Class(w_full_minus_[10], h_[20], top_center)[
                new UICol().Class(mask_children, w_full, h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2])[
                    new UIText(mesh.LocalPath).Class(mc_[40], fs_[0.9f], middle_left, left_[5])
                ]
            ];
        }
        /*
        else if (value is Array array)
        {
            json.Type = value.GetType().FullName;
            json.Values ??= [];

            foreach (object element in array)
                json.Values.Add(GetField(element, false));
        }
        else if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            json.Type = value.GetType().FullName;
            json.Values ??= [];
            
            foreach (object element in enumerable)
                json.Values.Add(GetField(element, false));
        }
        */
        else if (value is ISceneSerializable sceneSerializable)
        {
            if (sceneSerializable is IVector<float> vf)
            {
                var handler = new ScriptFieldHandler<IVector<float>>(fieldInfo, vf.Default);
                return CreateVectorUI(script, handler, vf, vf.Count);
            }

            if (sceneSerializable is IVector<int> vi)
            {
                var handler = new ScriptFieldHandler<IVector<int>>(fieldInfo, vi.Default);
                return CreateVectorUI(script, handler, vi, vi.Count);
            }
        }
        else if (value != null)
        {

        }
        return null;
    }

    private UIElementBase CreateVectorUI<T>(ScriptingNode script, ScriptValueHandler<IVector<T>> handler, IVector<T> value, int count) where T : IEquatable<T>, IParsable<T>
    {
        UIField[] fields = new UIField[count];
        for (int i = 0; i < count; i++)
            fields[i] = new UIField(""+value[i]).Class(middle_left, left_[5], mc_[13], fs_[0.9f]);

        void action(UIField[] fields)
        {
            var vector = value.Default;
            for (int i = 0; i < count; i++)
                if (fields[i].TryGetValue<T>(out var value))
                    vector[i] = value;

            handler.SetValue(script, vector);
        }
        
        var col = new UIHCol().Class(w_full_minus_[10], h_[20], top_center, fit_children, spacing_[10])[
            Forloop(0, count, i => 
            new UICol().Class(mask_children, w_full, h_[20], border_ui_[1, 1, 1, 1], border_color_[Border2], blank_sharp, rgba_v4_[Bg2])[
                fields[i].OnTextChange(_ => action(fields))
            ])
        ];
        
        var watcher = new VectorWatcher<T>(() => handler.GetValue(script), fields);
        EditorWatcher.EditorWatchers.Add(watcher);
        return col;
    }
}

public abstract class ScriptValueHandler
{
    public abstract object Get(ScriptingNode script);
    public abstract void Set(ScriptingNode script, object value);
}

public abstract class ScriptValueHandler<T> : ScriptValueHandler
{
    public abstract T GetValue(ScriptingNode script);
    public abstract void SetValue(ScriptingNode script, T value);
}

public class ScriptFieldHandler<T>(FieldInfo info, T @default) : ScriptValueHandler<T> where T : notnull
{
    public override object Get(ScriptingNode script) => GetValue(script);
    public override T GetValue(ScriptingNode script)
    {
        var value = info.GetValue(script);
        return value is T t ? t : @default;
    }

    public override void Set(ScriptingNode script, object value)
    {
        if (value is T) info.SetValue(script, value);
    }
    public override void SetValue(ScriptingNode script, T value)
    {
        info.SetValue(script, value);
    }
}

public class ScriptPropertyHandler<T>(PropertyInfo info, T @default) : ScriptValueHandler<T> where T : struct
{
    public override object Get(ScriptingNode script) => GetValue(script);
    public override T GetValue(ScriptingNode script)
    {
        var value = info.GetValue(script);
        return value is T t ? t : @default;
    }

    public override void Set(ScriptingNode script, object value)
    {
        if (value is T) info.SetValue(script, value);
    }
    public override void SetValue(ScriptingNode script, T value)
    {
        info.SetValue(script, value);
    }
}