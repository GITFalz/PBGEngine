using PBG;
using PBG.Data;
using PBG.MathLibrary;
using PBG.UI;
using PBG.UI.FileManager;
using Silk.NET.Input;

public class ModelingSelectionMode : ModelingBase
{
    private bool _oldIsLocal = false;
    private Vector2 _rotationAxis = Vector2.Zero;

    public ModelingSelectionMode(ModelingEditor editor) : base(editor)
    {

    }

    public void SwitchAxis(string axis)
    {
        switch (axis)
        {
            case "X":
                ModelSettings.Axis.X = ModelSettings.Axis.X == 0 ? 1 : 0;
                break;
            case "Y":
                ModelSettings.Axis.Y = ModelSettings.Axis.Y == 0 ? 1 : 0;
                break;
            case "Z":
                ModelSettings.Axis.Z = ModelSettings.Axis.Z == 0 ? 1 : 0;
                break;
        }
    }

    public override void Start()
    {
        ModelSettings.WireframeVisible = false;
    }

    public override void Resize()
    {

    }

    public override void Update()
    {
        Vector2 mouseDelta = Input.MouseDelta;

        if (_oldIsLocal != ModelSettings.IsLocalMode)
        {
            _oldIsLocal = ModelSettings.IsLocalMode;
            if (ModelSettings.IsLocalMode)
            {
                if (ModelManager.SelectedModel != null)
                {
                    TransformGizmo.Rotation = ModelManager.SelectedModel.Rotation;
                    RotationGizmo.Rotation = ModelManager.SelectedModel.Rotation;
                }
            }
            else
            {
                if (ModelManager.SelectedModel != null)
                {
                    TransformGizmo.Rotation = Quaternion.Identity;
                    RotationGizmo.Rotation = Quaternion.Identity;
                }
            }
        }

        if (!FileManager.IsHovering && Input.IsKeyDown(Key.G))
        {
            Vector3 move = Editor.GetSnappingMovement();
            if (move != Vector3.Zero)
            {
                for (int i = 0; i < ModelManager.SelectedModels.Count; i++)
                {
                    var model = ModelManager.SelectedModels[i];
                    model.Position += move;
                }
            }
        }

        if (!FreeCamera)
        {
            if (TransformGizmo.Hover(out var triangle) && Input.IsMousePressed(MouseButton.Left))
            {
                HoldingTransform = true;
                Game.SetCursorState(CursorMode.Disabled);
            }

            if (HoldingTransform && mouseDelta != Vector2.Zero && triangle != null && Model != null)
            {
                TransformGizmoAction(Model.Rotation, mouseDelta, triangle.Value.Info, MoveSelectedModels);
            }

            if (RotationGizmo.Hover(out triangle) && Input.IsMousePressed(MouseButton.Left))
            {
                HoldingRotation = true;
                _rotationAxis = RotationGizmo.SliderDirection(triangle.Value.GetAxis());
                Game.SetCursorState(CursorMode.Disabled);
            }

            if (HoldingRotation && mouseDelta != Vector2.Zero && triangle != null && Model != null)
            {
                RotationGizmoAction(Model.Rotation, mouseDelta, _rotationAxis, triangle.Value.Info, RotateSelectedModels);
            }
        }

        if (Input.IsMouseReleased(MouseButton.Left))
        {
            HoldingTransform = false;
            HoldingRotation = false;

            TransformGizmo.GenerateWorldSpacePoints();
            RotationGizmo.GenerateWorldSpacePoints();

            Game.SetCursorState(CursorMode.Normal);
        }
        
        if (Model == null)
            return;
    }

    public override void Render()
    {
        /*
        GL.Enable(EnableCap.DepthTest);

        TransformGizmo.Render();
        GL.Disable(EnableCap.CullFace);
        RotationGizmo.Render();
        GL.Enable(EnableCap.CullFace);
        */
    }

    public override void Exit()
    {

    }

    private void MoveSelectedModels(Vector3 move)
    {
        foreach (var model in ModelManager.SelectedModels)
            model.Position += move;

        if (ModelManager.SelectedModel != null)
            Editor.SetTransform(ModelManager.SelectedModel.Position);

        TransformGizmo.Position += move;
        RotationGizmo.Position += move;
    }

    private void RotateSelectedModels(Vector3 axis, float delta)
    {
        var rotationDelta = Quaternion.FromAxisAngle(axis.Normalized(), delta);

        foreach (var model in ModelManager.SelectedModels)
        {   
            model.Rotation = rotationDelta * model.Rotation;
        }

        if (ModelManager.SelectedModel != null)
            Editor.SetTransform(ModelManager.SelectedModel.Position);

        if (ModelSettings.IsLocalMode)
        {
            RotationGizmo.Rotation = rotationDelta * RotationGizmo.Rotation;
            TransformGizmo.Rotation = RotationGizmo.Rotation;
        }
    }
}