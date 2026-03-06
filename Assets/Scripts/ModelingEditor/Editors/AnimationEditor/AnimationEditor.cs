using System.Diagnostics.CodeAnalysis;
using PBG;
using PBG.Compiler.Lines;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Parse;
using PBG.Rendering;
using PBG.UI;
using Silk.NET.Input;

public class AnimationEditor : BaseEditor
{
    // === UI COMPONENTS ===
    public UIController TimelineUIController = null!;

    // Position - Blue
    public static Vector4 PositionColor => TimelineUI.PositionColor * 0.8f;
    public static Vector4 PositionSelection => TimelineUI.PositionColor * 1.2f;

    // Rotation - Red
    public static Vector4 RotationColor => TimelineUI.RotationColor * 0.8f;
    public static Vector4 RotationSelection => TimelineUI.RotationColor * 1.2f;

    // Scale - Green
    public static Vector4 ScaleColor => TimelineUI.ScaleColor * 0.8f;
    public static Vector4 ScaleSelection => TimelineUI.ScaleColor * 1.2f;

    public Vector2 _oldMousePos = Vector2.Zero;
    public Vector2 _rotationAxis = Vector2.Zero;

    public bool SaveAnimation
    {
        get { return _saveAnimation; }
        set
        {
            _saveAnimation = value;
            ToggleAnimationSavingUI();
        }
    }
    
    private bool _saveAnimation = false;
    public bool GenerateEveryKeyframe = false;

    public bool RightClickMenu
    {
        get { return _rightClickMenu; }
        set
        {
            _rightClickMenu = value;
            ToggleRightClickMenu();
        }
    }
    private bool _rightClickMenu = false;

    public string SelectedBoneName = "";
    public Bone? SelectedBone = null;

    public List<BonePivot> SelectedBonePivots = new();
    public List<Bone> SelectedBones = new();

    // === RENDERING / SHADER RESOURCES ===
    /*
    public static ShaderProgram TimelineShader = new ShaderProgram("Animation/timeline.vert", "Animation/timeline.frag");
    public static ShaderProgram KeyframeShader = new ShaderProgram("Animation/keyframe.vert", "Animation/keyframe.frag");
    public static VAO TimelineVao = new();
    public static VAO KeyframeVao = new();
    public static SSBO<int> KeyframeSSBO = new();
    */
    public static int KeyframeLinesCount = 0;

    /*
    private int _timelineModelLoc = TimelineShader.GetLocation("model");
    private int _timelineProjectionLoc = TimelineShader.GetLocation("projection");
    private int _timelineCellWidthLoc = TimelineShader.GetLocation("cellWidth");
    private int _timelineFirstValueLoc = TimelineShader.GetLocation("firstValue");
    private int _timelineOffsetLoc = TimelineShader.GetLocation("offset");
    private int _timelineTextLoc = TimelineShader.GetLocation("text");

    private int _keyframeModelLoc = KeyframeShader.GetLocation("model");
    private int _keyframeProjectionLoc = KeyframeShader.GetLocation("projection");
    private int _keyframeSizeLoc = KeyframeShader.GetLocation("size");
    private int _keyframeScrollLoc = KeyframeShader.GetLocation("scroll");
    private int _keyframeCellWidthLoc = KeyframeShader.GetLocation("cellWidth");
    */

    // === ANIMATION LOGIC ===
    public bool Playing = false;

    // === PRIVATE / INTERNAL STATE ===
    private bool regenerateVertexUi = true;

    // === KEYFRAME ELEMENTS ===
    public Dictionary<string, NewBoneAnimation> BoneAnimations = [];

    // === TIMELINE ===
    public Vector2 TimelinePosition = Vector2.Zero;
    public int CurrentFrame = 0;

    public TimelineUI UI = null!;

    public bool HoldingTransform = false;
    public bool HoldingRotation = false;


    // Bone Copy
    public class AnimationBoneCopy(AnimationEditor Editor)
    {
        public Bone? Bone;

        public Vector3? Position;
        public Quaternion? Rotation;
        public Vector3? Scale;

        private void CopyCheck(Bone bone)
        {
            if (Bone != bone)
            {
                Position = null;
                Rotation = null;
                Scale = null;
                Bone = bone;
            }      
        }

        // Copy
        public void CopyPosition(Bone bone)
        {
            CopyCheck(bone);
            Position = bone.Position;
        }

        public void CopyRotation(Bone bone)
        {
            CopyCheck(bone);
            Rotation = bone.Rotation;
        }

        public void CopyScale(Bone bone)
        {
            CopyCheck(bone);
            Scale = bone.Scale;
        }

        // Paste
        public enum FlipAxis
        {
            None = 0b000,
            X = 0b001,
            Y = 0b010,
            Z = 0b100,
            XY = 0b011,
            XZ = 0b101,
            YZ = 0b110,
            XYZ = 0b111
        }

        public void Paste(Bone bone)
        {
            ApplyTransform(bone, FlipAxis.None);
        }

        public void PasteFlipX(Bone bone) => ApplyTransform(bone, FlipAxis.X);
        public void PasteFlipY(Bone bone) => ApplyTransform(bone, FlipAxis.Y);
        public void PasteFlipZ(Bone bone) => ApplyTransform(bone, FlipAxis.Z);
        public void PasteFlipXY(Bone bone) => ApplyTransform(bone, FlipAxis.XY);
        public void PasteFlipXZ(Bone bone) => ApplyTransform(bone, FlipAxis.XZ);
        public void PasteFlipYZ(Bone bone) => ApplyTransform(bone, FlipAxis.YZ);
        public void PasteFlipXYZ(Bone bone) => ApplyTransform(bone, FlipAxis.XYZ);

        private void ApplyTransform(Bone bone, FlipAxis flip)
        {
            bool fx = ((int)flip & 0b001) != 0;
            bool fy = ((int)flip & 0b010) != 0;
            bool fz = ((int)flip & 0b100) != 0;

            // ---- Position ----
            if (Position != null)
            {
                var p = Position.Value;
                bone.Position = (
                    fx ? -p.X : p.X,
                    fy ? -p.Y : p.Y,
                    fz ? -p.Z : p.Z
                );
            }

            // ---- Rotation (via Euler angles) ----
            if (Rotation != null)
            {
                var q = Rotation.Value;

                // Convert to Euler deg
                var radians = q.ToEuler();
                var angles = Mathf.RadiansToDegrees(radians);

                if (fx) angles.Y *= -1; // flipping X mirrors around Y/Z
                if (fy) angles.X *= -1;
                if (fz) angles.Y *= -1; // typical flip logic depends on your system

                // Rebuild quaternion
                radians = Mathf.DegreesToRadians(angles);
                bone.Rotation = Quaternion.FromEuler(radians);
            }

            // ---- Scale ----
            if (Scale != null)
            {
                var s = Scale.Value;
                bone.Scale = (
                    fx ? -s.X : s.X,
                    fy ? -s.Y : s.Y,
                    fz ? -s.Z : s.Z
                );
            }

            bone.GetRootBone().UpdateGlobalTransformation();
            Editor.Model?.UpdateRig();
        }
    }

    public AnimationBoneCopy BoneCopy;


    public AnimationEditor(GeneralModelingEditor editor) : base(editor)
    {
        BoneCopy = new(this);
    }

    public override void Start() 
    { 
        TimelineUIController = Editor.Transform.ParentNode!.GetNode("TimelineUI").GetComponent<UIController>();
        TimelineUIController.Alignment.Left = 200;
        TimelineUIController.Alignment.Right = 200;
        TimelineUIController.Alignment.Top = 50;
        Started = true;
        UI = new TimelineUI(this);
        TimelineUIController.AddElement(UI);
    }

    public override void Resize()
    {
        
    }

    public override void Awake()
    {
        Editor.Transform.ParentNode!.GetNode("TimelineUI").Disabled = false;

        Editor.UI.SetTransformAction(SetTransform);
        Editor.UI.SetScaleAction(SetScale);
        Editor.UI.SetRotationAction(SetRotation);

        Editor.UI.SetTransform((0, 0, 0));
        Editor.UI.SetRotation((0, 0, 0));
        Editor.UI.SetScale((0, 0, 0));

        TransformGizmo.GenerateWorldSpacePoints();
        RotationGizmo.GenerateWorldSpacePoints();

        Console.WriteLine("Awake Animation Editor");

        foreach (var (_, model) in ModelManager.Models)
        {
            model.RenderBones = true;
        }

        if (Model != null)
        {
            Model.SetAnimationRig();
            Model.SetAnimation();
            Model.AnimationState = true;
            Model.ShowWireframe = false;

            if (Model.AnimationRig != null)
            {
                UI.Init(Model.AnimationRig);
            }

            Editor.UI.RegenerateAnimationButtons(Model);
            GenerateAnimationTimeline(Model);
        }

        Handle_BoneMovement();

        Info.RenderInfo = false;
        regenerateVertexUi = true;

        Editor.UI.AnimationEdit.SetVisible(false);
    }

    public override void Render()
    {
        Editor.RenderModel();

        Vector2 size = (Game.Width - 800f, 28);

        if (size.X > 0 && size.Y > 0)
        {
            /*
            GL.Viewport(600, Game.Height - (int)UI.TimelineCollection.Origin.Y - 87, (int)size.X, (int)size.Y);

            Matrix4 tickModel = Matrix4.CreateTranslation((0, 0, 0.3f));
            Matrix4 tickProjection = Matrix4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, -1, 1);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            TimelineShader.Bind();

            float width = TimelineUI.TimelineCellSize * 5;
            int snap = Mathf.RoundToInt(TimelinePosition.X / (width));


            GL.UniformMatrix4(_timelineModelLoc, false, ref tickModel);
            GL.UniformMatrix4(_timelineProjectionLoc, false, ref tickProjection);
            GL.Uniform1(_timelineCellWidthLoc, TimelineUI.TimelineCellSize);
            GL.Uniform1(_timelineFirstValueLoc, snap * 5);
            GL.Uniform1(_timelineOffsetLoc, (float)(snap * width) + TimelineUI.TimelineCellSize * 0.5f - TimelinePosition.X);
            GL.Uniform1(_timelineTextLoc, 0);

            TimelineVao.Bind();
            UIData.PixelPerfectUI.TextTexture.Bind(TextureUnit.Texture0);

            GL.DrawArrays(PrimitiveType.Triangles, 0, 6 * Mathf.CeilToInt(size.X / width));

            Shader.Error("Tick shader error: ");

            UIData.PixelPerfectUI.TextTexture.Unbind();
            TimelineVao.Unbind();

            TimelineShader.Unbind();

            GL.Viewport(200, 0, Game.Width - 400, Game.Height - 50);
            */
        }

        size = UI.KeyframeCollection.ParentElement!.Size;

        if (size.X > 0 && size.Y > 0 && KeyframeLinesCount > 0)
        {
            /*
            UIController.BindFramebuffer();

            Matrix4 tickModel = Matrix4.CreateTranslation((400, UI.KeyframeCollection.ParentElement!.Origin.Y - 30, UIController.CumulativeDepth - 0.00013f));
            Matrix4 tickProjection = WindowProjection;

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            KeyframeShader.Bind();

            GL.UniformMatrix4(_keyframeModelLoc, true, ref tickModel);
            GL.UniformMatrix4(_keyframeProjectionLoc, true, ref tickProjection);
            GL.Uniform2(_keyframeSizeLoc, new Vector2(size.X, 30f));
            GL.Uniform2(_keyframeScrollLoc, TimelinePosition);
            GL.Uniform1(_keyframeCellWidthLoc, TimelineUI.TimelineCellSize);

            KeyframeVao.Bind();
            KeyframeSSBO.Bind(0);

            GL.DrawArrays(PrimitiveType.Triangles, 0, 6 * KeyframeLinesCount);

            Shader.Error("Keyframe shader error: ");

            KeyframeSSBO.Unbind();
            KeyframeVao.Unbind();

            KeyframeShader.Unbind();

            UIController.UnbindFramebuffer();
            */
        }

        SetGizmoPosition();
        
        if (SelectedBone != null)
        {
            /*
            TransformGizmo.Render();
            GL.Disable(EnableCap.CullFace);
            RotationGizmo.Render();
            GL.Enable(EnableCap.CullFace);
            */
        }
    }

    public override void EndRender()
    {
 
    }

    public override void Update()
    {
        if (Input.IsKeyDown(Key.ControlLeft) && Input.IsKeyDown(Key.ShiftLeft) && Input.IsKeyPressed(Key.P))
        {
            ModelSettings.RenderScreenSpacePositions = !ModelSettings.RenderScreenSpacePositions;
            return;
        }

        Vector2 mouseDelta = Input.MouseDelta;

        Vector2 keyframeScreenPos = new Vector2(220, Game.Height - 223);
        Vector2 timerScreenPos = new Vector2(220, Game.Height - 256);

        if (Input.IsMousePressed(MouseButton.Right) && Editor.UI.HoveringCenter)
        {
            Editor.UI.AnimationEdit.BaseOffset = Input.MousePosition - (280, 50);
            Editor.UI.AnimationEdit.SetVisible(!Editor.UI.AnimationEdit.Visible);
            Editor.UI.AnimationEdit.ApplyChanges(UIChange.Transform);
        }

        if (Input.IsKeyPressed(Key.Escape))
        {
            Editor.freeCamera = !Editor.freeCamera;

            if (Editor.freeCamera)
            {
                HoldingTransform = false;
                Game.Instance.CursorMode = CursorMode.Disabled;
                Camera.SetCameraMode(CameraMode.Free);
            }
            else
            {
                Game.Instance.CursorMode = CursorMode.Normal;
                Camera.SetCameraMode(CameraMode.Fixed);
                TransformGizmo.GenerateWorldSpacePoints();
                RotationGizmo.GenerateWorldSpacePoints();
                if (Model == null)
                {
                    Console.WriteLine("Buddy you fucked up, model is null somehow");
                }
                Model?.UpdateBonePosition(Model.AnimationRig, Editor.ProjectionMatrix, Camera.ViewMatrix);
            }
        }

        if (!Editor.freeCamera)
        {
            if (Input.IsKeyPressed(Key.Space) && ModelManager.SelectedModels.Count > 0)
            {
                Playing = !Playing;
                if (Playing)
                    ForSelectedModels(model => { model.Animate = true; });
                else
                    ForModels(model => { model.Animate = false; });
                regenerateVertexUi = !Playing;
            }

            if (Input.IsKeyPressed(Key.Delete))
            {
                UI.DeleteSelectedKeyframes();
            }

            if (TransformGizmo.Hover(out var transform) && Input.IsMousePressed(MouseButton.Left))
            {
                HoldingTransform = true;
                Game.SetCursorState(CursorMode.Disabled);
            }

            if (RotationGizmo.Hover(out var rotation) && Input.IsMousePressed(MouseButton.Left))
            {
                int index = (rotation.Value.Info >> 3) switch
                {
                    0b1 => 0,
                    0b10 => 1,
                    0b100 => 2,
                    _ => 0
                };

                HoldingRotation = true;
                _rotationAxis = RotationGizmo.SliderDirection(index);
                Game.SetCursorState(CursorMode.Disabled);
            }

            if (mouseDelta != Vector2.Zero && SelectedBone != null && Model != null)
            {
                Quaternion rot = Model.Rotation * SelectedBone.Rotation;
                
                if (HoldingTransform && transform != null)
                {
                    TransformGizmoAction(rot, mouseDelta, transform.Value.Info, MoveSelectedBone);
                }
                
                if (HoldingRotation && rotation != null)
                {
                    RotationGizmoAction(rot, mouseDelta, _rotationAxis, rotation.Value.Info, RotateSelectedBone);
                }
            }
        }

        if (Input.IsMouseReleased(MouseButton.Left))
        {
            HoldingTransform = false;
            HoldingRotation = false;
            TransformGizmo.GenerateWorldSpacePoints();
            RotationGizmo.GenerateWorldSpacePoints();
            Model?.UpdateBonePosition(Model.AnimationRig, Editor.ProjectionMatrix, Camera.ViewMatrix);
            Game.SetCursorState(CursorMode.Normal);
        }

        if (Playing && Model != null)
        {
            Model.Update();
            UI.SetCursorPosition(Model.GetAnimationTime() * NewAnimation.FRAMES * TimelineUI.TimelineCellSize - 4);
        }
        else
            RigUpdate();
    }

    private void SetGizmoPosition()
    {
        if (SelectedBone != null && Model != null)
        {
            TransformGizmo.Position = Model.Position + Vector3.Transform(SelectedBone.Position, Model.Rotation);
            RotationGizmo.Position = TransformGizmo.Position;

            TransformGizmo.Rotation = ModelSettings.IsLocalMode ? Model.Rotation * SelectedBone.Rotation : Quaternion.Identity;
            RotationGizmo.Rotation = TransformGizmo.Rotation;
        }
    }

    private void RigUpdate()
    {
        if (Input.IsMousePressed(MouseButton.Left) && !ClickedMenu && !HoldingTransform && !HoldingRotation)
        {
            TestBonePosition();
        }

        if (SelectedBone != null && Input.IsKeyPressed(Key.U))
        {
            AddPositionKeyframe();
        }

        if (SelectedBone != null && Input.IsKeyPressed(Key.I))
        {
            AddRotationKeyframe();
        }

        TestRemove();

        if (Input.IsKeyPressed(Key.G))
        {
            Game.SetCursorState(CursorMode.Disabled);
        }

        if (Input.IsKeyPressed(Key.R))
        {
            Game.SetCursorState(CursorMode.Disabled);
        }

        if (Input.IsKeyDown(Key.G))
        {
            Handle_BoneMovement();
            SetBonePositionText();
            SetBoneRotationText();
        }

        if (Input.IsKeyDown(Key.R))
        {
            Handle_BoneRotation();
            SetBoneRotationText();
        }

        if (Input.IsKeyReleased(Key.G))
        {
            Game.SetCursorState(CursorMode.Normal);
            Model?.UpdateBonePosition(Model.AnimationRig, Editor.ProjectionMatrix, Camera.ViewMatrix);
            regenerateVertexUi = true;
        }

        if (Input.IsKeyReleased(Key.R))
        {
            Game.SetCursorState(CursorMode.Normal);
            Model?.UpdateBonePosition(Model.AnimationRig, Editor.ProjectionMatrix, Camera.ViewMatrix);
        }

        if (regenerateVertexUi)
        {
            Model?.UpdateBonePosition(Model.AnimationRig, Editor.ProjectionMatrix, Camera.ViewMatrix);
            regenerateVertexUi = false;
        }
    }

    public override void Exit()
    {
        Editor.Transform.ParentNode!.GetNode("TimelineUI").Disabled = true;

        foreach (var (_, model) in ModelManager.Models)
        {
            model.RenderBones = false;
        }

        if (Model != null)
        {
            Model.RenderBones = false;
            Model.AnimationState = false;
            Model.SetAnimationRig();
            Model.ShowWireframe = true;
        }

        UI.ClearTimeline();

        Info.RenderInfo = true;

        Playing = false;
    }

    private void MoveSelectedBone(Vector3 move)
    {
        if (SelectedBone != null && Model != null)
        {
            SelectedBone.Position += Vector3.Transform(move, Model.Rotation.Inverted());
            SetTransform(SelectedBone.Position);
            Model?.AnimationRig?.RootBone.UpdateGlobalTransformation();
            Model?.UpdateRig();
        }
    }

    private void RotateSelectedBone(Vector3 axis, float delta)
    {
        if (SelectedBone != null && Model != null)
        {
            Vector3 localAxis = Vector3.Transform(axis, Model.Rotation.Inverted());
            var rotationDelta = Quaternion.FromAxisAngle(localAxis.Normalized(), delta);
            SelectedBone.Rotation = rotationDelta * SelectedBone.Rotation;
            SetRotation(Mathf.ToDegrees(SelectedBone.EulerRotation));
            Model.AnimationRig?.RootBone.UpdateGlobalTransformation();
            Model.UpdateRig(); 
        }
    }

    public void SetTransform(int i, float value)
    {
        if (SelectedBone != null)
        {
            Vector3 position = SelectedBone.Position;
            position[i] = value;
            SelectedBone.Position = position;   
            Model?.AnimationRig?.RootBone.UpdateGlobalTransformation();
            Model?.UpdateRig();
        }
    }

    public void SetScale(int i, float value)
    {
        if (SelectedBone != null)
        {
            Vector3 scale = SelectedBone.Scale;
            scale[i] = value;
            SelectedBone.Scale = scale;   
            Model?.AnimationRig?.RootBone.UpdateGlobalTransformation();
            Model?.UpdateRig();
        }
    }
    
    public void SetRotation(int i, float value)
    {
        if (SelectedBone != null)
        {
            Vector3 rotation = SelectedBone.EulerRotation;
            rotation[i] = Mathf.DegreesToRadians(value);
            SelectedBone.EulerRotation = rotation;   
            Model?.AnimationRig?.RootBone.UpdateGlobalTransformation();
            Model?.UpdateRig();
        }
    }


    public void SetTransform(Vector3 transform) => Editor.UI.SetTransform(transform);
    public void SetScale(Vector3 scale) => Editor.UI.SetScale(scale);
    public void SetRotation(Vector3 rotation) => Editor.UI.SetRotation(rotation);

    public void ForSelectedModels(Action<Model> action)
    {
        for (int i = 0; i < ModelManager.SelectedModels.Count; i++)
        {
            var model = ModelManager.SelectedModels[i];
            action(model);
        }
    }

    public void ForModels(Action<Model> action)
    {
        foreach (var (_, model) in ModelManager.Models)
        {
            action(model);
        }
    }

    public void MoveBone(int i, string text)
    {
        if (SelectedBone == null)
            return;

        float value = Float.Parse(text);
        Vector3 position = SelectedBone.Position;
        position[i] = value;
        SelectedBone.Position = position;
        if (Model == null || Model.AnimationRig == null)
            return;
        Model.AnimationRig.RootBone.UpdateGlobalTransformation();
        Model.UpdateRig();
        foreach (var bone in Model.AnimationRig.BonesList)
        {
            bone.UpdateEndTarget();
        }
    }

    public void RotateBone(int i, string text)
    {
        if (SelectedBone == null)
            return;

        float value = Mathf.DegreesToRadians(Float.Parse(text));
        Vector3 rotation = SelectedBone.EulerRotation;
        rotation[i] = value;
        SelectedBone.EulerRotation = rotation;
        if (Model == null || Model.AnimationRig == null)
            return;
        Model.AnimationRig.RootBone.UpdateGlobalTransformation();
        Model.UpdateRig();
        foreach (var bone in Model.AnimationRig.BonesList)
        {
            bone.UpdateEndTarget();
        }
    }

    public void ToggleElements(bool visible, params UIElementBase[] elemnts)
    {
        if (visible)
        {
            foreach (var element in elemnts)
            {
                element.SetVisible(true);
            }
        }
        else
        {
            foreach (var element in elemnts)
            {
                element.SetVisible(false);
            }
        }
        //MainPanelStacking.ResetInit();
        //MainPanelStacking.Align();
        //MainPanelStacking.UpdateTransformation();
    }

    public bool SetAnimationState(int frame, float t, out float time)
    {
        time = 0f;
        if (Model?.AnimationRig == null) 
            return false;

        foreach (var (_, animation) in BoneAnimations)
        {
            var boneAnimation = animation;
            var a = boneAnimation.GetKeyframe(frame);
            var b = boneAnimation.GetKeyframe(frame + 1);
            var ab = a.Lerp(b, t);
            time = ab.Time;

            if (Model.AnimationRig.GetBone(boneAnimation.BoneName, out var bone))
            {
                Bone? rigBone = null;
                Model.Rig?.GetBone(boneAnimation.BoneName, out rigBone);

                if (boneAnimation.PositionKeyframes.Count > 0)
                    bone.Position = ab.Position;
                else if (rigBone != null)
                    bone.Position = rigBone.Position;
                    
                if (boneAnimation.RotationKeyframes.Count > 0)
                    bone.Rotation = ab.Rotation;
                else if (rigBone != null)
                    bone.Rotation = rigBone.Rotation;
                
                if (boneAnimation.ScaleKeyframes.Count > 0)
                    bone.Scale = ab.Scale;
                else if (rigBone != null)
                    bone.Scale = rigBone.Scale;
            }
        }

        Model.AnimationRig.RootBone.UpdateGlobalTransformation();
        Model.UpdateRig();
        return true;
    }

    public void ToggleAnimationSavingUI()
    {
        if (!SaveAnimation)
        {
            GenerateEveryKeyframe = false;
            //_animationGenerateEveryKeyframeButton.TextureIndex = 11;
            //_animationGenerateEveryKeyframeButton.Color = (0.5f, 0.5f, 0.5f, 1f);
            //_animationGenerateEveryKeyframeButton.UpdateTexture();
            //_animationGenerateEveryKeyframeButton.UpdateColor();
        }
    }

    public void ToggleRightClickMenu()
    {
        //RightClickMenuUI.SetPosition(Input.GetMousePosition());
    }

    public void SetBonePositionText()
    {
        if (SelectedBone == null)
            return;

        Editor.UI.SetTransform(SelectedBone.Position);
    }

    public void SetBoneRotationText()
    {
        if (SelectedBone == null)
            return;

        Vector3 rotation = SelectedBone.Rotation.ToEuler();
        Editor.UI.SetRotation(
            Mathf.RadiansToDegrees(rotation.X),
            Mathf.RadiansToDegrees(rotation.Y),
            Mathf.RadiansToDegrees(rotation.Z)
        );
    }

    public void ClearTimeline()
    {
        UI.ClearTimeline(false);
        BoneAnimations = [];
    }

    public void GenerateAnimationTimeline(Model model)
    {
        NewAnimation? animation = model.Animation;
        if (model.AnimationRig == null || animation == null)
            return;

        ClearTimeline();

        int i = 0;
        model.AnimationRig.RootBone.Run((bone, _) =>
        {
            if (!animation.GetBoneAnimation(bone.Name, out var boneAnimation))
            {
                boneAnimation = new NewBoneAnimation(bone.Name);
                animation.AddBoneAnimation(boneAnimation);
            }

            BoneAnimations.Add(bone.Name, boneAnimation);

            foreach (var keyframe in boneAnimation.PositionKeyframes)
            {
                var keyframeButton = CreateKeyElement(keyframe, boneAnimation, bone, keyframe.Index);
                if (keyframeButton != null)
                {
                    UI.AddKeyframeButton(keyframeButton);
                }
                else
                {
                    Console.WriteLine("Filed to generate position keyframe at index " + keyframe.Index + " when generating the timeline");
                }
            }

            foreach (var keyframe in boneAnimation.RotationKeyframes)
            {
                var keyframeButton = CreateKeyElement(keyframe, boneAnimation, bone, keyframe.Index);
                if (keyframeButton != null)
                {
                    UI.AddKeyframeButton(keyframeButton);
                }
                else
                {
                    Console.WriteLine("Filed to generate rotation keyframe at index " + keyframe.Index + " when generating the timeline");
                }
            }
            foreach (var keyframe in boneAnimation.ScaleKeyframes)
            {
                var keyframeButton = CreateKeyElement(keyframe, boneAnimation, bone, keyframe.Index);
                if (keyframeButton != null)
                {
                    UI.AddKeyframeButton(keyframeButton);
                }
                else
                {
                    Console.WriteLine("Filed to generate scale keyframe at index " + keyframe.Index + " when generating the timeline");
                }
            }
            i++;
        });

        for (int j = 0; j < ModelManager.SelectedModels.Count; j++)
        {
            var m = ModelManager.SelectedModels[j];
            AddSideModel(m);
        }

        //TimelineScrollView.ResetInit();
        //KeyframePanelCollection.Align();
    }

    public void AddSideModel(Model model)
    {
        if (Model == model || model.AnimationRig == null || model.Animation == null)
            return;

        NewAnimation animation = model.Animation;

        //UITextButton modelTextButton = new UITextButton(model.Name, TimelineUI, AnchorType.TopLeft, PositionType.Relative, (0.5f, 0.5f, 0.5f), (0, 0, 0), (200, 20), (0, 0, 0, 0), 0, 10, (7.5f, 0.05f));
        //modelTextButton.SetTextCharCount(model.Name, 1f);
        //TimelineScrollView.AddElement(modelTextButton.Collection);
        //TimelineUI.AddElement(modelTextButton.Collection);
        //SideBoneAnimations.Add(model, (modelTextButton, new Dictionary<string, TimelineBoneAnimation>()));

        for (int i = 0; i < model.AnimationRig.BonesList.Count; i++)
        {
            Bone bone = model.AnimationRig.BonesList[i];
            Console.WriteLine("[Generation] : Generating bone " + bone.Name);
            if (!animation.GetBoneAnimation(bone.Name, out var boneAnimation))
            {
                Console.WriteLine("No bone animation was found, generating a new one");
                boneAnimation = new NewBoneAnimation(bone.Name);
                animation.AddBoneAnimation(boneAnimation);
            }

            //SideBoneAnimations[model].Item2.Add(bone.Name, timelineAnimation);

            if (boneAnimation.PositionKeyframes.Count == 0)
            {
                Console.WriteLine("No position keyframe found, adding a default position: " + bone.Position);
                PositionKeyframe keyframe = new PositionKeyframe(0, bone.Position);
                boneAnimation.AddOrUpdateKeyframe(keyframe);
            }
            if (boneAnimation.RotationKeyframes.Count == 0)
            {
                Console.WriteLine("No rotation keyframe found, adding a default rotation: " + bone.Rotation);
                RotationKeyframe keyframe = new RotationKeyframe(0, bone.Rotation);
                boneAnimation.AddOrUpdateKeyframe(keyframe);
            }
            if (boneAnimation.ScaleKeyframes.Count == 0)
            {
                Console.WriteLine("No scale keyframe found, adding a default scale: " + bone.Scale);
                ScaleKeyframe keyframe = new ScaleKeyframe(0, bone.Scale);
                boneAnimation.AddOrUpdateKeyframe(keyframe);
            }

            foreach (var keyframe in boneAnimation.PositionKeyframes)
            {
                var keyframeButton = new UIButton();
                //timelineAnimation.Add(keyframeButton, keyframe);
            }
            foreach (var keyframe in boneAnimation.RotationKeyframes)
            {
                var keyframeButton = new UIButton();
                //timelineAnimation.Add(keyframeButton, keyframe);
            }
            foreach (var keyframe in boneAnimation.ScaleKeyframes)
            {
                var keyframeButton = new UIButton();
                //timelineAnimation.Add(keyframeButton, keyframe);
            }
        }
    }

    public void RemoveSideModel(Model model)
    {
        /*
        if (Model == model || !SideBoneAnimations.ContainsKey(model))
            return;

        foreach (var timelineAnimation in SideBoneAnimations[model].Item2.Values)
        {
            timelineAnimation.ResetKeyframes();
            timelineAnimation.Clear();
        }

        SideBoneAnimations[model].Item1.Collection.Delete();
        SideBoneAnimations.Remove(model);
        */
    }


    public void Handle_BoneMovement()
    {
        if (Model == null || Model.AnimationRig == null)
            return;

        Vector2 mouseDelta = Input.GetMouseDelta();
        if (mouseDelta == Vector2.Zero)
            return;

        foreach (var bone in Model.AnimationRig.BonesList)
        {
            if (bone.Selection == BoneSelection.End)
            {
                bone.Rotate();
            }
            else if (bone.Selection == BoneSelection.Pivot || bone.Selection == BoneSelection.Both)
            {
                bone.Move();
            }
        }

        Model.AnimationRig.RootBone.UpdateGlobalTransformation();
        Model.UpdateRig();

        foreach (var bone in Model.AnimationRig.BonesList)
        {
            bone.UpdateEndTarget();
        }
    }

    public void Handle_BoneRotation()
    {
        if (Model == null || Model.AnimationRig == null)
            return;

        Vector2 mouseDelta = Input.GetMouseDelta();
        if (mouseDelta == Vector2.Zero)
            return;

        foreach (var bone in SelectedBones)
        {
            bone.Rotate(Camera.front, mouseDelta.X);
        }

        Model.AnimationRig.RootBone.UpdateGlobalTransformation(); 
        Model.UpdateRig();

        foreach (var bone in SelectedBones)
        {
            bone.UpdateEndTarget();
        }
    }

    public void TestBonePosition()
    {
        if (Model == null)
            return;

        if (!Input.IsKeyDown(Key.ShiftLeft))
        {
            SelectedBonePivots = [];
            SelectedBones = [];
            SelectedBoneName = "";
            SelectedBone = null;
        }
        
        Vector2 mousePos = Input.GetMousePosition();
        Vector2? closest = null;
        BonePivot? closestBone = null;
    
        foreach (var (pivot, (position, radius)) in Model.BonePivots)
        {
            float distance = Vector2.Distance(mousePos, position);
            float distanceClosest = closest == null ? 1000 : Vector2.Distance(mousePos, (Vector2)closest);

            if (distance < distanceClosest && distance < radius)
            {
                closest = position;
                closestBone = pivot;
            }
        }

        if (closestBone != null)
        {
            if (!SelectedBonePivots.Remove(closestBone))
            {
                SelectedBonePivots.Add(closestBone);
                if (!SelectedBones.Contains(closestBone.Bone))
                    SelectedBones.Add(closestBone.Bone);

                string name = closestBone.Bone.Name;
                SelectedBoneName = name;
                SelectedBone = closestBone.Bone;

                SetGizmoPosition();

                SetBonePositionText();
                SetBoneRotationText();
            }
            else
            {
                SelectedBones.Remove(closestBone.Bone);
                if (SelectedBonePivots.Count > 0)
                {
                    string name = SelectedBones[0].Name;
                    SelectedBoneName = name;
                    SelectedBone = SelectedBones[0];

                    SetGizmoPosition();

                    SetBonePositionText();
                    SetBoneRotationText();
                }
            }
        }

        if (SelectedBonePivots.Count == 0)
        {
            SelectedBoneName = "";
            SelectedBone = null;
            //BoneNameText.SetText("", 1f);
        }

        //BoneNameText.InputField.UpdateCharacters();

        UpdateBoneColors();
    }

    public HashSet<Bone> GetSelectedBones()
    {
        HashSet<Bone> selectedBones = new HashSet<Bone>();
        foreach (var pivot in SelectedBonePivots)
        {
            selectedBones.Add(pivot.Bone);
        }
        return selectedBones;
    }

    public void UpdateBoneColors()
    {
        if (Model == null)
            return;
            
        HashSet<Bone> seenBones = [];
        foreach (var (pivot, _) in Model.BonePivots)
        {
            Bone bone = pivot.Bone;
            if (SelectedBonePivots.Contains(pivot))
            {
                bool isPivot = pivot.IsPivot();
                if (isPivot)
                {
                    bone.Selection = BoneSelection.Pivot;
                }
                else if (seenBones.Contains(bone)) // Pivot is processed first, so we can check if the bone is already selected
                {
                    bone.Selection = BoneSelection.Both;
                }
                else
                {
                    bone.Selection = BoneSelection.End;
                }
                seenBones.Add(bone);
            }
            else if (!seenBones.Contains(bone))
            {
                bone.Selection = BoneSelection.None;
            }
        }

        Model?.UpdateRig();
    }
    
    public void TestRemove()
    {
        if (Input.IsKeyDown(Key.ControlLeft) && Input.IsKeyPressed(Key.Delete))
        {
            float frame = (UI.Marker.BaseOffset.X + 4) / TimelineUI.TimelineCellSize;
            int frameIndex = Mathf.FloorToInt(frame);
            float t = frame - frameIndex;
            SetAnimationState(frameIndex, t, out _);
            Model?.UpdateBonePosition(Model.AnimationRig, Editor.ProjectionMatrix, Camera.ViewMatrix);
            if (Model == null || Model.AnimationRig == null)
                return;

            Model.UpdateRig();
            foreach (var bone in Model.AnimationRig.BonesList)
            {
                bone.UpdateEndTarget();
            }
        }
    }

    public void AddPositionKeyframe()
    {
        HashSet<Bone> selectedBones = GetSelectedBones();
        foreach (var bone in selectedBones)
        {
            AddKeyframe(bone, 0, CurrentFrame, true);
        }
    }

    public void AddRotationKeyframe()
    {
        HashSet<Bone> selectedBones = GetSelectedBones();
        foreach (var bone in selectedBones)
        {
            AddKeyframe(bone, 1, CurrentFrame, true);
        }
    }

    public void AddScaleKeyframe()
    {
        HashSet<Bone> selectedBones = GetSelectedBones();
        foreach (var bone in selectedBones)
        {
            AddKeyframe(bone, 2, CurrentFrame, true);
        }
    }

    public void AddKeyframe(Bone bone, int type, int frame, bool addToUIController)
    {
        if (!BoneAnimations.TryGetValue(bone.Name, out var boneAnimation))
        {
            Console.WriteLine("Couldn't find bone animation for " + bone.Name);
            return;
        }
        
        IndividualKeyframe keyframe;
        if (type == 0)
            keyframe = new PositionKeyframe(frame, bone.Position);
        else if (type == 1)
            keyframe = new RotationKeyframe(frame, bone.Rotation);
        else if (type == 2)
            keyframe = new ScaleKeyframe(frame, bone.Scale);
        else 
            return;

        if (boneAnimation.AddOrUpdateKeyframe(keyframe))
        {
            var keyframeButton = CreateKeyElement(keyframe, boneAnimation, bone, frame);
            if (keyframeButton == null)
            {
                Console.WriteLine("Failed to create keyframe button for " + bone.Name);
                return;
            }
                
            if (addToUIController)
                UI.AddKeyframeButton(keyframeButton);
        }
        else
        {
            Console.WriteLine("Failed to create keyframe for " + bone.Name);
        }
    }

    public UIElementBase? CreateKeyElement(IndividualKeyframe keyframe, NewBoneAnimation boneAnimation, Bone bone, int frame)
    {
        int type = 0;
        Vector4 color = PositionColor;
        Vector4 select = PositionSelection;
        if (keyframe is RotationKeyframe)
        {
            type = 1;
            color = RotationColor;
            select = RotationSelection;
        }
        else if (keyframe is ScaleKeyframe)
        {
            type = 2;
            color = ScaleColor;
            select = ScaleSelection;
        }
        
        return UI.CreateKeyframeButton(keyframe, boneAnimation, bone, color, select, frame, type);
    }
}