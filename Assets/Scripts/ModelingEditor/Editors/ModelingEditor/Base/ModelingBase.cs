using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.UI.FileManager;

public abstract class ModelingBase
{
    public ModelingEditor Editor;
    public Model? Model => Editor.Model;
    public FileManager FileManager => Editor.Editor.FileManager;
    public Camera Camera => Editor.Camera;
    public TransformGizmo TransformGizmo => Editor.Editor.TransformGizmo;
    public RotationGizmo RotationGizmo => Editor.Editor.RotationGizmo;
    public bool HoldingTransform { get => Editor.HoldingTransform; set => Editor.HoldingTransform = value; }
    public bool HoldingRotation { get => Editor.HoldingRotation; set => Editor.HoldingRotation = value; }
    public bool Regenerate
    {
        get => Editor.Editor.Regenerate;
        set => Editor.Editor.Regenerate = value;
    }

    public bool CanStash
    {
        get => Editor.CanStash;
        set => Editor.CanStash = value;
    }
    public bool CanGenerateBuffers {
        get => Editor.CanGenerateBuffers;
        set => Editor.CanGenerateBuffers = value;
    }
    public bool FreeCamera => Editor.Editor.freeCamera;

    public ModelingBase(ModelingEditor editor)
    {
        Editor = editor;
    }

    public abstract void Start();
    public abstract void Resize();
    public abstract void Update();
    public abstract void Render();
    public abstract void Exit();

    public void TransformGizmoAction(Quaternion rotation, Vector2 mouseDelta, int color, Action<Vector3> action) => Editor.Editor.TransformGizmoAction(rotation, mouseDelta, color, action);
    public void RotationGizmoAction(Quaternion rotation, Vector2 mouseDelta, Vector2 axisScreen, int color, Action<Vector3, float> action) => Editor.Editor.RotationGizmoAction(rotation, mouseDelta, axisScreen, color, action);
}