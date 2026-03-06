using PBG;
using PBG.Data;
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.UI;
using PBG.UI.FileManager;
using Silk.NET.Input;

public class ModelingEditor : BaseEditor
{
    public ModelingBase CurrentMode;

    public ModelingSelectionMode SelectionMode;
    public ModelingEditingMode EditingMode;

    public bool CanStash = true;
    public bool CanGenerateBuffers = true;

    public bool ModeClicked = false;
    public bool HoldingTransform = false;
    public bool HoldingRotation = false;

    public UIVCol ModeCollection;


    public ModelingEditor(GeneralModelingEditor editor) : base(editor)
    {
        Editor = editor;

        SelectionMode = new ModelingSelectionMode(this);
        EditingMode = new ModelingEditingMode(this);

        CurrentMode = EditingMode;
    }

    public void SwitchMode(ModelingBase mode)
    {
        CurrentMode.Exit();
        CurrentMode = mode;
        CurrentMode.Start();
    }

    public override void Start()
    {
        Console.WriteLine("Start Modeling Editor");

        CurrentMode.Start();

        if (Editor.freeCamera)
        {
            Game.Instance.CursorMode = CursorMode.Disabled;
            Camera.Unlock();
        }
        else
        {
            Game.Instance.CursorMode = CursorMode.Normal;
            Camera.Lock();
        }
    }

    public override void Resize()
    {
        SelectionMode.Resize();
        EditingMode.Resize();
    }

    public override void Awake()
    {
        Console.WriteLine("Awake Modeling Editor");

        SelectionMode.Resize();
        EditingMode.Resize();

        Editor.UI.SetTransformAction(SetTransform);
        Editor.UI.SetScaleAction(SetScale);
        Editor.UI.SetRotationAction(SetRotation);

        HoldingTransform = false;

        TransformGizmo.GenerateWorldSpacePoints();
        RotationGizmo.GenerateWorldSpacePoints();

        CurrentMode.Start();

        if (Model == null)
            return;

        Model.SetModeling();
        Model?.UpdateVertexPosition();
    }

    public override void Update()
    {
        if (Input.IsKeyPressed(Key.Enter) && FileManager.IsVisible)
        {
            if (FileManager.HandleType == FileManagerType.Import && FileManager.SelectedFiles.Count > 0)
            {
                foreach (var path in FileManager.SelectedFiles)
                {
                    if (Path.GetExtension(path) == ".model")
                    {
                        if (ModelManager.LoadModelFromPath(path, out var model))
                        {
                            var button = Editor.GetModelButton(model);
                            Editor.UI.Hierarchy.AddElement(button);
                            Editor.UI.Hierarchy.UIController?.AddElement(button);
                        }
                    }
                }
                FileManager.ToggleOff();
            }
            else if (FileManager.HandleType == FileManagerType.Export)
            {
                var path = FileManager.GetSaveFilePath();
                if (path != null)
                {
                    SaveModel(path);
                }
            }
        }

        CurrentMode.Update();

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
                Model?.UpdateVertexPosition();
            }
        }

        if (Model == null)
            return;

        ModeClicked = false;
    }

    public void SaveModel(string path)
    {
        if (!path.EndsWith(".model"))
            path += ".model";
            
        ModelManager.SelectedModel?.SaveModelToPath(path);
        FileManager.ToggleOff();
    }
    

    public override void Render()
    {
        Editor.RenderModel();

        CurrentMode.Render();
    }

    public override void EndRender()
    {
        
    }

    public override void Exit()
    {
        //Camera.SetSmoothFactor(true);
        //Camera.SetPositionSmoothFactor(true);
        ClearStash();
        HoldingTransform = false;
    }

    public void SetTransform(int i, float value)
    {

    }

    public void SetScale(int i, float value)
    {

    }

    public void SetRotation(int i, float value)
    {

    }
    
    public void SetTransform(Vector3 transform) => Editor.UI.SetTransform(transform);
    public void SetScale(Vector3 scale) => Editor.UI.SetTransform(scale);
    public void SetRotation(Vector3 rotation) => Editor.UI.SetTransform(rotation);

    public void SwitchSelection(RenderType st)
    {
        var old = ModelingEditingMode.selectionType;
        ModelingEditingMode.selectionType = st;
        if (old != st)
        {
            for (int i = 0; i < ModelManager.SelectedModels.Count; i++)
            {
                var model = ModelManager.SelectedModels[i];
                model.Mesh.RegenerateVertices();
            }
            EditingMode.Regenerate = true;
        }
    }


    public void Handle_Undo()
    {
        if (Model == null)
            return;

        GetLastMesh();

        Model.Mesh.UpdateNormals();
        Model.Mesh.RegenerateAll();

        Model.UpdateVertexPosition();
        Model.GenerateVertexColor();
    }

    public void Handle_Copy()
    {
        if (Model == null)
            return;

        ModelCopy.CopyInto(Model.Copy, Model.SelectedVertices);
    }

    // Paste
    public void Handle_Paste(bool stash = true)
    {
        if (Model == null)
            return;
            
        if (stash)
            StashMesh();

        ModelCopy copy = Model.Copy.Copy();
        Model.SelectedVertices = [.. copy.newSelectedVertices];

        Model.Mesh.AddCopy(copy);

        if (!CanGenerateBuffers)
            return;

        Model.Mesh.RegenerateAll();
        
        Model.UpdateVertexPosition();
        Model.GenerateVertexColor();
    }

    public static void Paste(ModelCopy copy, ModelMesh mesh)
    {
        mesh.AddCopy(copy.Copy());
    }


    public Vector3 GetSnappingMovement()
    {
        Camera camera = Camera;

        float scale = 1f;
        if (ModelManager.SelectedModel != null)
        {
            scale = Vector3.Distance(camera.Position, ModelManager.SelectedModel.Position);
        }

        Vector2 mouseDelta = Input.GetMouseDelta() * 0.001f * scale;
        Vector3 move = camera.right * mouseDelta.X + camera.up * -mouseDelta.Y;

        move *= ModelSettings.Axis;
        if (move.Length == 0) return Vector3.Zero;
        
        if (ModelSettings.Snapping)
        {
            Vector3 Offset = Vector3.Zero;

            Vector3 snappingOffset = ModelSettings.SnappingOffset;
            float snappingFactor = ModelSettings.SnappingFactor;

            snappingOffset += move;
            if (snappingOffset.X > snappingFactor)
            {
                Offset.X = snappingFactor;
                snappingOffset.X -= snappingFactor;
            }
            if (snappingOffset.X < -snappingFactor)
            {
                Offset.X = -snappingFactor;
                snappingOffset.X += snappingFactor;
            }
            if (snappingOffset.Y > snappingFactor)
            {
                Offset.Y = snappingFactor;
                snappingOffset.Y -= snappingFactor;
            }
            if (snappingOffset.Y < -snappingFactor)
            {
                Offset.Y = -snappingFactor;
                snappingOffset.Y += snappingFactor;
            }
            if (snappingOffset.Z > snappingFactor)
            {
                Offset.Z = snappingFactor;
                snappingOffset.Z -= snappingFactor;
            }
            if (snappingOffset.Z < -snappingFactor)
            {
                Offset.Z = -snappingFactor;
                snappingOffset.Z += snappingFactor;
            }

            ModelSettings.SnappingOffset = snappingOffset;
        
            move = Offset;
        }

        return move;
    }

    // Stashing
    public void StashMesh(int maxCount = 30)
    {
        /*
        if (Model == null || !CanStash)
            return;

        string fileName = Editor.currentModelName + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
        string folderPath = Path.Combine(Game.undoModelPath, Editor.currentModelName);

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        if (Editor.MeshSaveNames.Count >= maxCount)
        {
            string name = Editor.MeshSaveNames[0];
            string path = Path.Combine(folderPath, name + ".model");
            if (!File.Exists(path))
                throw new FileNotFoundException($"File {path} not found");

            File.Delete(path);
            Editor.MeshSaveNames.RemoveAt(0);
        }
        Console.WriteLine("Stashing mesh");
        Editor.MeshSaveNames.Add(fileName);
        Model.Mesh.SaveModel(fileName, folderPath);
        */
    }

    public void ClearStash()
    {
        /*
        string folderPath = Path.Combine(Game.undoModelPath, Editor.currentModelName);
        foreach (string file in Directory.GetFiles(folderPath))
        {
            File.Delete(file);
        }
        */
    }

    public void GetLastMesh()
    {
        /*
        if (Model == null || Editor.MeshSaveNames.Count == 0)
            return;

        string name = Editor.MeshSaveNames[^1];
        string path = Path.Combine(Path.Combine(Game.undoModelPath, Editor.currentModelName), $"{name}.model");

        Console.WriteLine(path);

        if (!File.Exists(path))
            return;

        Console.WriteLine("Getting last mesh");
        
        Editor.MeshSaveNames.RemoveAt(Editor.MeshSaveNames.Count - 1);
        Model.Mesh.LoadModel(name, Path.Combine(Game.undoModelPath, Editor.currentModelName));
        */
    }

    // Data
    public readonly List<Vector3> AxisIgnore = new()
    {
        new Vector3(0, 1, 1), // X
        new Vector3(1, 0, 1), // Y
        new Vector3(1, 1, 0), // Z
    };
}

public enum RenderType
{
    Vertex = 0,
    Edge = 1,
    Face = 2,
}