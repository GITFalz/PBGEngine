using PBG.MathLibrary;
using PBG.Rendering;
using PBG.UI.FileManager;

public abstract class BaseEditor(GeneralModelingEditor editor)
{
    public GeneralModelingEditor Editor = editor;
    public bool ClickedMenu => Editor.ClickedMenu;
    public Model? Model => ModelManager.SelectedModel;
    public Camera Camera => Editor.Scene.DefaultCamera;
    public Matrix4 WindowProjection => Editor.WindowProjection;
    public TransformGizmo TransformGizmo => Editor.TransformGizmo;
    public RotationGizmo RotationGizmo => Editor.RotationGizmo;
    public FileManager FileManager => Editor.FileManager;

    public bool Started = false;
    public bool blocked = false;
    public string FileName => ""; //Editor.FileName.Text;
    public bool WriteName
    {
        get => Editor._writeName;
        set => Editor._writeName = value;
    }

    public abstract void Start();
    public abstract void Resize();
    public abstract void Awake();
    public abstract void Update();
    public abstract void Render();
    public abstract void EndRender();
    public abstract void Exit();

    public void TransformGizmoAction(Quaternion rotation, Vector2 mouseDelta, int color, Action<Vector3> action) => Editor.TransformGizmoAction(rotation, mouseDelta, color, action);
    public void RotationGizmoAction(Quaternion rotation, Vector2 mouseDelta, Vector2 axisScreen, int color, Action<Vector3, float> action) => Editor.RotationGizmoAction(rotation, mouseDelta, axisScreen, color, action);

    public void ResetGizmoRotation()
    {
        TransformGizmo.Rotation = Quaternion.Identity;
        RotationGizmo.Rotation = Quaternion.Identity;
    }

    public void SetGizmoRotation(Quaternion rotation)
    {
        TransformGizmo.Rotation = rotation;
        RotationGizmo.Rotation = rotation;
    }

    public void SetGizmoRotation(Model model)
    {
        TransformGizmo.Rotation = model.Rotation;
        RotationGizmo.Rotation = model.Rotation;
    }
}