using PBG;
using PBG.Data;
using PBG.MathLibrary;
using PBG.UI;
using PBG.UI.Creator;
using Silk.NET.Input;
using static PBG.UI.Styles;

public class TimelineUI(
    AnimationEditor Editor
) : UIScript
{
    private const int BASE_BACKGROUND = GeneralEditorUI.BASE_BACKGROUND;
    private const int BASE_BUTTON = GeneralEditorUI.BASE_BUTTON;
    private const int BASE_BORDER = GeneralEditorUI.BASE_BORDER;
    private const int HOVER_BACKGROUND = GeneralEditorUI.HOVER_BACKGROUND;
    private const int HOVER_BUTTON = GeneralEditorUI.HOVER_BUTTON; 
    private const int HOVER_BORDER = GeneralEditorUI.HOVER_BORDER;

    public static Vector4 PositionColor = (1.0f, 0.4196f, 0.4196f, 1.0f);
    public static Vector4 RotationColor = (0.306f, 0.804f, 0.769f, 1.0f);
    public static Vector4 ScaleColor = (1.0f, 0.902f, 0.428f, 1.0f);

    public static float TimelineCellSize => TimelineZoom * 30f;
    public static float TimelineZoom = 1f;

    private UIVScroll BoneHierarchy = null!;

    public UICol TimelineCollection = null!;
    public UIVScroll BoneCollection = null!;
    public UICol KeyframeCollection = null!;

    public UICol Marker = null!;

    public UIField FrameDisplay = null!;
    public UIText TimeDisplay = null!;

    public Dictionary<Bone, int> BoneIndices = [];
    public Dictionary<IndividualKeyframe, NewBoneAnimation> SelectedKeyframes = [];
    public List<UIElementBase> SelectedKeyframeButtons = [];

    public override UIElementBase Script() =>
    new UICol(Class(w_full_minus_[400], h_[200], max_h_full_minus_[50], min_h_[100], bottom_[50], bottom_left, border_ui_[0, 2, 0, 0], border_color_g_[BASE_BORDER], blank_full_g_[BASE_BACKGROUND]),
    OnClick<UICol>(_ => Editor.Editor.ClickedMenu = true),
    Sub(
        new UICol(Class(w_[30], h_[20], blank_sharp_g_[BASE_BORDER], bottom_[10], top_center), Sub(
            new UICol(Class(w_[26], h_[16], blank_sharp_g_[BASE_BACKGROUND], middle_center),
            OnHold<UICol>(ScaleTimeline),
            OnClick<UICol>(_ => Element.Height.Value = Mathf.Clampy(Element.Height.Value, 100, Game.Height - 50)),
            Sub(
                new UIText("=", Class(mc_[1], fs_[1], middle_center))
            ))
        )),
        new UICol(Class(w_full, h_full, mask_children), Sub(
            new UIVCol(Class(w_full, h_full), Sub(
                new UIHCol(Class(w_full, h_[50], border_ui_[0, 0, 0, 2], border_color_g_[BASE_BORDER], spacing_[5]), Sub(
                    new UICol(Class(w_[30], h_[30], border_ui_[2, 2, 2, 2], blank_full_g_[BASE_BACKGROUND], border_color_g_[BASE_BORDER], left_[15], middle_left, hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                    OnHoverEnter<UICol>(HoverEnter), OnHoverExit<UICol>(HoverExit),
                    OnClick<UICol>(_ =>
                    {
                        var model = ModelManager.SelectedModel;
                        if (model?.Animation != null && !model.Animate)
                        {
                            Editor.Playing = true;
                            model.Animate = true;
                        }
                    }),
                    Sub(
                        new UIImg(Class(w_full_minus_[8], h_full_minus_[8], middle_center, texture_[80], slice_null, rgba_[1, 1, 1, 1]))
                    )),
                    new UICol(Class(w_[30], h_[30], border_ui_[2, 2, 2, 2], blank_full_g_[BASE_BACKGROUND], border_color_g_[BASE_BORDER], middle_left, hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                    OnHoverEnter<UICol>(HoverEnter), OnHoverExit<UICol>(HoverExit),
                    OnClick<UICol>(_ =>
                    {
                        var model = ModelManager.SelectedModel;
                        if (model?.Animation != null && model.Animate)
                        {
                            Editor.Playing = false;
                            model.Animate = false;
                        }
                    }),
                    Sub(
                        new UIImg(Class(w_full_minus_[8], h_full_minus_[8], middle_center, texture_[81], slice_null, rgba_[1, 1, 1, 1]))
                    )),
                    new UICol(Class(w_[30], h_[30], border_ui_[2, 2, 2, 2], blank_full_g_[BASE_BACKGROUND], border_color_g_[BASE_BORDER], middle_left, hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                    OnHoverEnter<UICol>(HoverEnter), OnHoverExit<UICol>(HoverExit), Sub(
                        new UIImg(Class(w_full_minus_[8], h_full_minus_[8], middle_center, texture_[82], slice_null, rgba_[1, 1, 1, 1]))
                    )),
                    new UICol(Class(w_[30], h_[30], border_ui_[2, 2, 2, 2], blank_full_g_[BASE_BACKGROUND], border_color_g_[BASE_BORDER], middle_left, hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                    OnHoverEnter<UICol>(HoverEnter), OnHoverExit<UICol>(HoverExit),
                    OnClick<UICol>(_ => MarkerPosition((Editor.CurrentFrame - 1) * TimelineCellSize - 4)),
                    Sub(
                        new UIImg(Class(w_full_minus_[12], h_full_minus_[12], middle_center, texture_[83], slice_null, rgba_[1, 1, 1, 1]))
                    )),
                    new UICol(Class(w_[30], h_[30], border_ui_[2, 2, 2, 2], blank_full_g_[BASE_BACKGROUND], border_color_g_[BASE_BORDER], middle_left, hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                    OnHoverEnter<UICol>(HoverEnter), OnHoverExit<UICol>(HoverExit),
                    OnClick<UICol>(_ => MarkerPosition((Editor.CurrentFrame + 1) * TimelineCellSize - 4)),
                    Sub(
                        new UIImg(Class(w_full_minus_[12], h_full_minus_[12], middle_center, texture_[84], slice_null, rgba_[1, 1, 1, 1]))
                    )),
                    new UICol(Class(w_[22], h_full), Sub(new UIImg(Class(w_[2], h_[30], middle_center, blank_full_g_[BASE_BORDER])))),
                    new UICol(Class(w_[140], h_[30], middle_left, border_ui_[2, 2, 2, 2], blank_full_g_[10], border_color_g_[BASE_BORDER]), Sub(
                        new UIText("Frame:", Class(mc_[6], fs_[1], middle_left, left_[7])),
                        newField("0", Class(mc_[10], fs_[1f], middle_left, left_[56]),
                        OnTextChange(f => MarkerPosition(f.GetInt() * TimelineCellSize - 4f))
                        , ref FrameDisplay)
                    )),
                    new UICol(Class(w_[140], h_[30], middle_left, border_ui_[2, 2, 2, 2], blank_full_g_[10], border_color_g_[BASE_BORDER]), Sub(
                        newText("00:00.00", Class(mc_[15], fs_[1f], middle_center, text_align_center), ref TimeDisplay)
                    ))
                )),
                new UICol(Class(w_full, h_full_minus_[50]), Sub(
                    new UICol(Class(w_[200], h_full), Sub(
                        new UICol(Class(w_full, h_[30], border_ui_[0, 0, 0, 2], border_color_g_[BASE_BORDER]), Sub(
                            new UIText("BONE HIERARCHY", Class(mc_[14], fs_[1.2f], middle_center))
                        )),
                        newVScroll(Class(w_full, h_full_minus_[30], mask_children, top_[30], ignore_invisible), Sub(

                        ), ref BoneHierarchy)
                    )),
                    new UICol(Class(left_[200], w_full_minus_[200], h_full, border_ui_[2, 0, 0, 0], border_color_g_[BASE_BORDER]), Sub(
                        new UICol(Class(w_full, h_[30], border_ui_[0, 0, 0, 2], border_color_g_[BASE_BORDER]), Sub(
                            new UICol(Class(w_[200], h_full, border_ui_[0, 0, 2, 0], border_color_g_[BASE_BORDER]), Sub(
                                new UIText("PROPERTIES", Class(mc_[10], fs_[1.2f], middle_center))
                            )),
                            newCol(Class(w_full_minus_[200], left_[200], h_full),
                            OnClick<UICol>(SetMarker),
                            OnHover<UICol>(_ =>
                            {
                                float scrollDelta = Input.GetMouseScrollDelta().Y;
                                if (scrollDelta == 0)
                                    return;
                                    
                                if (Input.IsKeyDown(Key.ControlLeft))
                                {
                                    // Zoom
                                    TimelineZoom -= scrollDelta * 0.02f * TimelineZoom;
                                    TimelineZoom = Mathf.Clampy(TimelineZoom, 0.2f, 2f);

                                    KeyframeCollection.ForeachChildren(b =>
                                    {
                                        if (b.Dataset.TryGetValue("keyframe", out var obj) && obj is IndividualKeyframe keyframe)
                                        {
                                            float width = TimelineCellSize;
                                            b.BaseOffset.X = keyframe.Index * width + (width - 20) * 0.5f;
                                        }
                                    });
                                    KeyframeCollection.ApplyChanges(UIChange.Transform);
                                }
                                else
                                {
                                    // Scroll sideways
                                    SetTimelineX(Editor.TimelinePosition.X - scrollDelta * 10f);
                                }
                            }),
                            Sub(
                                new UICol(Class(data_["scroll", 0f], w_[40], h_[20], depth_[10], blank_sharp_g_[BASE_BUTTON], border_ui_[2, 2, 2, 2], border_color_g_[10], top_center, bottom_[10]),
                                OnHold<UICol>(ScrollTimeline),
                                OnRelease<UICol>(c => { c.BaseOffset.X = 0; c.ApplyChanges(UIChange.Transform); c.Dataset["scroll"] = 0f; }),
                                Sub(
                                    new UIText("<=>", Class(mc_[3], fs_[1], middle_center))
                                ))
                            ), ref TimelineCollection)
                        )),
                        new UICol(Class(w_full, h_full_minus_[30], top_[30]), Sub(
                            newVScroll(Class(w_[200], h_full, border_ui_[0, 0, 2, 0], border_color_g_[BASE_BORDER], mask_children),
                            OnHover<UIVScroll>(c =>
                            {
                                float oldY = Editor.TimelinePosition.Y;
                                Editor.TimelinePosition.Y = c.ScrollPosition;
                                if (oldY != c.ScrollPosition)
                                {
                                    KeyframeCollection.BaseOffset.Y = -c.ScrollPosition;
                                    KeyframeCollection.ApplyChanges(UIChange.Transform);
                                }
                            }),
                            Sub(

                            ), ref BoneCollection),
                            new UICol(Class(w_full_minus_[200], left_[200], h_full, mask_children, blank_full_g_[15]), Sub(
                                newCol(Class(), Sub(), ref KeyframeCollection)
                            )),
                            new UICol(Class(w_full_minus_[200], left_[200], h_full_plus_[30], bottom_[30], mask_children), Sub(
                                newCol(Class(w_[8], h_full, data_["pos", 0], right_[4], depth_[3]),
                                
                                Sub(
                                    new UIButton(Class(w_full, h_[30]),
                                    OnClick<UIButton>(b => { if (b.ParentElement != null) b.ParentElement.Dataset["pos"] = b.ParentElement.BaseOffset.X; }),
                                    OnHold<UIButton>(b => { if (b.ParentElement != null) MoveMarker(b.ParentElement); })),
                                    new UIImg(Class(w_[20], h_[20], rgb_[1, 0, 0], texture_[85], slice_null, top_center, bottom_[6])),
                                    new UIImg(Class(w_[2], h_full, top_center, rgb_[1, 0, 0], blank_full))
                                ), ref Marker)
                            ))
                        ))
                    ))
                ))
            ))
        ))
    ));

    public void Init(Rig rig)
    {
        BoneHierarchy.ScrollPosition = 0;
        BoneCollection.ScrollPosition = 0;
        BoneHierarchy.DeleteChildren();
        BoneCollection.DeleteChildren();
        BoneIndices = [];
        UIElementBase? rootBoneElement = null;
        UIVCol? rootBoneChildren = null;
        Dictionary<Bone, UIVCol> uiHierarchy = [];
        List<UIElementBase> bones = [];
        List<int> keyframeLines = [2];
        int i = 0;
        rig.RootBone.Run((bone, indent) =>
        {
            BoneIndices.Add(bone, i);
            var children = new UIVCol(w_full, grow_children, not_toggle_old_invisible, ignore_invisible);
            uiHierarchy[bone] = children;
            var col = new UIHCol(Class(w_full, h_[30], left_[indent * 15 + 5]), Sub(
                new UICol(Class(w_[30], h_[30], data_["visible", true]), OnClick<UICol>(c => ToggleBoneChildren(c, children)), Sub(
                    new UIImg(Class(w_[16], h_[16], middle_center, icon_[1], slice_null, rgba_[1, 1, 1, 1]))
                )),
                new UIText(bone.Name, Class(mc_[bone.Name.Length], fs_[1f], middle_left))
            ));
            if (bone is ChildBone child && uiHierarchy.TryGetValue(child.Parent, out var pcol))
            {
                pcol.AddElements(col, children);
            }
            else
            {
                rootBoneElement = col;
                rootBoneChildren = children;
            }
            var nameCol = new UICol(Class(w_full_minus_[4], top_center, h_[30], blank_full_g_[19], border_ui_[0, keyframeLines.Count == 0 ? 0 : 1, 0, 1], border_color_g_[BASE_BORDER]), Sub(
                new UIText(bone.Name, Class(mc_[bone.Name.Length], middle_left, left_[7])))
            );
            var bonePosition = new UICol(Class(w_full_minus_[4], top_center, h_[30], blank_full_g_[17], border_ui_[0, 1, 0, 1], border_color_g_[BASE_BORDER]), Sub(
                new UICol(Class(h_[20], w_[20], border_ui_[2, 2, 2, 2], border_color_[PositionColor], middle_left, left_[15]), Sub(
                    new UIText("P", Class(mc_[1], fs_[0.9f], middle_center, rgba_v4_[PositionColor]))
                )),
                new UIText("Position", Class(mc_[8], middle_left, left_[40])))
            );
            var boneRotation = new UICol(Class(w_full_minus_[4], top_center, h_[30], blank_full_g_[17], border_ui_[0, 1, 0, 1], border_color_g_[BASE_BORDER]), Sub(
                new UICol(Class(h_[20], w_[20], border_ui_[2, 2, 2, 2], border_color_[RotationColor], middle_left, left_[15]), Sub(
                    new UIText("R", Class(mc_[1], fs_[0.9f], middle_center, rgba_v4_[RotationColor]))
                )),
                new UIText("Rotation", Class(mc_[8], middle_left, left_[40])))
            );
            var boneScale = new UICol(Class(w_full_minus_[4], top_center, h_[30], blank_full_g_[17], border_ui_[0, 1, 0, 1], border_color_g_[BASE_BORDER]), Sub(
                new UICol(Class(h_[20], w_[20], border_ui_[2, 2, 2, 2], border_color_[ScaleColor], middle_left, left_[15]), Sub(
                    new UIText("S", Class(mc_[1], fs_[0.9f], middle_center, rgba_v4_[ScaleColor]))
                )),
                new UIText("Scale", Class(mc_[5], middle_left, left_[40])))
            );

            bones.AddRange(nameCol, bonePosition, boneRotation, boneScale);
            keyframeLines.AddRange(0, 1, 1, 1);
            i++;
        });

        if (rootBoneChildren != null && rootBoneElement != null)
        {
            BoneHierarchy.AddElements(rootBoneElement, rootBoneChildren);
            UIController?.AddElements([rootBoneElement, rootBoneChildren]);
        }
        BoneCollection.AddElements(bones);
        UIController?.AddElements(bones);
        //AnimationEditor.KeyframeSSBO.Renew(keyframeLines);
        AnimationEditor.KeyframeLinesCount = keyframeLines.Count;
    }
    
    private void ToggleBoneChildren(UICol col, UIVCol children)
    {
        if (col.Dataset.Bool("visible"))
        {
            col.Dataset["visible"] = false;
            children.SetVisible(false);
            col.GetElement<UIImg>()?.UpdateIconIndex(0);
        }
        else
        {
            col.Dataset["visible"] = true;
            children.SetVisible(true);
            col.GetElement<UIImg>()?.UpdateIconIndex(1);
        }
        BoneHierarchy.Align();
        BoneHierarchy.UpdateTransform();
    }

    private void HoverEnter(UICol col)
    {
        col.Color = GetColor(HOVER_BACKGROUND);
        col.BorderColor = GetColor(HOVER_BORDER);
        col.ApplyChanges(UIChange.Color | UIChange.BorderColor);
    }

    private void HoverExit(UICol col)
    {
        col.Color = GetColor(BASE_BACKGROUND);
        col.BorderColor = GetColor(BASE_BORDER);
        col.ApplyChanges(UIChange.Color | UIChange.BorderColor);
    }

    private Vector4 GetColor(int g) => new Vector4(new Vector3((float)g * 0.01f), 1f);

    private void ScaleTimeline(UICol _)
    {
        var mouseDelta = Input.GetMouseDelta();
        if (mouseDelta.Y == 0)
            return;
        Element.Height.Value -= mouseDelta.Y;
        Element.ApplyChanges(UIChange.Scale);
    }

    private void ScrollTimeline(UICol c)
    {
        var mouseDelta = Input.GetMouseDelta();
        var factor = MoveScrollTimeline(c, mouseDelta.X);
        SetTimelineX(Editor.TimelinePosition.X + factor * 4000f * GameTime.DeltaTime);
    }

    private void SetTimelineX(float x)
    {
        Editor.TimelinePosition.X = Mathf.Max(0, x);
        Marker.AddedOffset.X = -Editor.TimelinePosition.X;
        KeyframeCollection.AddedOffset.X = -Editor.TimelinePosition.X;
        Marker.ApplyChanges(UIChange.Transform);
        KeyframeCollection.ApplyChanges(UIChange.Transform);
    }

    private float MoveScrollTimeline(UICol c, float xDelta)
    {
        float halfWidth = (TimelineCollection.Size.X / 2f) - 20;
        float scroll = c.Dataset.Float("scroll");
        float pos = scroll + xDelta;
        c.Dataset["scroll"] = pos;
        float clampedPos = Mathf.Clampy(pos, -halfWidth, halfWidth);
        float factor = clampedPos / halfWidth;
        if (xDelta == 0)
            return factor;

        c.BaseOffset.X = Mathf.Clampy(pos, -halfWidth, halfWidth);
        c.ApplyChanges(UIChange.Transform);
        return factor;
    }

    private void SetMarker(UICol c)
    {
        float offset = Input.GetMousePosition().X - (c.Origin.X + 200);
        MarkerPosition(offset + Editor.TimelinePosition.X - 4);
        var button = Marker.GetElement<UIButton>();
        if (button != null) button.Clicked = true;
    }

    private void MarkerPosition(float position)
    {
        SetCursorPosition(position);
        float frame = (Marker.BaseOffset.X + 4) / TimelineCellSize;
        int frameIndex = Mathf.FloorToInt(frame);
        float t = frame - frameIndex;
        if (Editor.SetAnimationState(frameIndex, t, out var time))
        {
            var span = TimeSpan.FromSeconds(time);
            TimeDisplay.UpdateText($"{span.Minutes:00}:{span.Seconds:00}.{(span.Milliseconds / 10):00}");
        }
        Editor.CurrentFrame = Mathf.FloorToInt(frame);
        int frameDisplayValue = FrameDisplay.GetInt();
        if (frameDisplayValue != Editor.CurrentFrame)
        {
            FrameDisplay.UpdateText(Editor.CurrentFrame + "");
        }
    }

    public void SetCursorPosition(float position)
    {
        Marker.BaseOffset.X = Mathf.Max(-4, position);
        Marker.AddedOffset.X = -Editor.TimelinePosition.X;
        Marker.Dataset["pos"] = Marker.BaseOffset.X;
        Marker.ApplyChanges(UIChange.Transform);
    }

    private void MoveMarker(UIElementBase c)
    {
        var mouseDelta = Input.GetMouseDelta();
        float offset = Input.GetMousePosition().X - (TimelineCollection.Origin.X + 200);

        float factor = offset / TimelineCollection.Size.X;
        if (factor < 0.1f)
        {
            SetTimelineX(Editor.TimelinePosition.X + (0.1f - factor) * -10000f * GameTime.DeltaTime);
            MarkerPosition(offset + Editor.TimelinePosition.X - 4);
        }
        else if (factor > 0.9f)
        {
            SetTimelineX(Editor.TimelinePosition.X + (factor - 0.9f) * 10000f * GameTime.DeltaTime);
            MarkerPosition(offset + Editor.TimelinePosition.X - 4);
        }

        if (mouseDelta.X == 0)
            return;

        float pos = c.Dataset.Float("pos");
        pos += mouseDelta.X;
        MarkerPosition(pos);
    }

    public void ClearTimeline(bool clearBones = true)
    {
        SelectedKeyframes = [];
        SelectedKeyframeButtons = [];
        
        KeyframeCollection.DeleteChildren();
        if (clearBones)
        {
            BoneIndices = [];
            BoneCollection.DeleteChildren();
        }      
    }

    public void DeleteSelectedKeyframes()
    {
        foreach (var (keyframe, timeline) in SelectedKeyframes)
        {
            timeline.RemoveKeyframe(keyframe);
        }

        for (int i = 0; i < SelectedKeyframeButtons.Count; i++)
        {
            var button = SelectedKeyframeButtons[i];
            button.Delete();
        }

        SelectedKeyframes = [];
        SelectedKeyframeButtons = [];
    }

    public void AddKeyframeButton(UIElementBase button)
    {
        KeyframeCollection.AddElements(button);
        KeyframeCollection.UIController?.AddElement(button);
    }

    public UIElementBase? CreateKeyframeButton(IndividualKeyframe keyframe, NewBoneAnimation boneAnimation, Bone bone, Vector4 color, Vector4 select, int frame, int type)
    {
        if (!BoneIndices.TryGetValue(bone, out var index))
        {
            Console.WriteLine("Could not find bone " + bone.Name + " in the bone indices dictionnary");
            return null;
        }
        float width = TimelineCellSize;
        return new UICol("keyframe-" + keyframe.Index, Class(w_[20], h_[20], top_left, data_["oldP", 0], data_["color", color], data_["keyframe", keyframe], left_[frame * width + (width - 20) * 0.5f], texture_[65], top_[index * 120 + type * 30 + 35], slice_null, rgba_v4_[color], click_scale_[1.2f], click_scale_duration_[0.25f], click_scale_easeout),
        OnClick<UICol>(c =>
        {
            float width = TimelineCellSize;
            c.Dataset["pos"] = c.BaseOffset.X - (width - 20) * 0.5f;
            if (!Input.IsKeyDown(Key.ShiftLeft))
            {
                for (int i = 0; i < SelectedKeyframeButtons.Count; i++)
                {
                    var button = SelectedKeyframeButtons[i];
                    button.UpdateColor(button.Dataset.Vector4("color"));
                }
                SelectedKeyframes = [];
                SelectedKeyframeButtons = [];
            }

            if (!SelectedKeyframes.Remove(keyframe))
            {
                SelectedKeyframeButtons.Add(c);
                SelectedKeyframes.Add(keyframe, boneAnimation);
                c.UpdateColor(select);
            }
            else
            {
                SelectedKeyframeButtons.Remove(c);
                c.UpdateColor(color);
            }      
        }),
        OnHoverEnter<UICol>(c => c.UpdateColor(select)),
        OnHoverExit<UICol>(c =>
        {
            if (!SelectedKeyframes.ContainsKey(keyframe))
                c.UpdateColor(color);
        }),
        OnHold<UICol>(c =>
        {
            Vector2 mouseDelta = Input.GetMouseDelta();
            if (mouseDelta == Vector2.Zero)
                return;

            var pos = c.Dataset.Float("pos") + mouseDelta.X;
            c.Dataset["pos"] = pos;

            var p = Mathf.Max(0, pos);
            p = Mathf.Round(p / TimelineCellSize);
            var index = Mathf.RoundToInt(p);
            var oldP = c.Dataset.Int("oldP");

            if (index != oldP && !boneAnimation.ContainsIndex(type, index))
            {
                keyframe.SetIndex(index);
                boneAnimation.OrderKeyframes(type);

                float width = TimelineCellSize;
                c.BaseOffset.X = p * width + (width - 20) * 0.5f;
                c.ApplyChanges(UIChange.Transform);

                float frame = (c.BaseOffset.X + 4) / TimelineCellSize;
                Editor.CurrentFrame = Mathf.RoundToInt(frame);
                int frameDisplayValue = FrameDisplay.GetInt();
                if (frameDisplayValue != Editor.CurrentFrame)
                {
                    FrameDisplay.UpdateText(Editor.CurrentFrame + "");
                }
            }
            c.Dataset["oldP"] = index;
        }),
        Sub());
    }
}