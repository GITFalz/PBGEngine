using PBG;
using PBG.Data;
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.UI;
using PBG.UI.FileManager;
using Silk.NET.Input;

public class RiggingEditor : BaseEditor
{
    public string SelectedBoneName = "";
    public Bone? SelectedBone = null;

    public List<BonePivot> SelectedBonePivots = new();
    public List<Bone> SelectedBones = new();

    public bool renderSelection = false;
    public Vector2 oldMousePos = Vector2.Zero;

    // Input data
    private bool _d_pressed = false;
    private bool _modelSelected = true;
    private bool _writeName = false;


    public RiggingEditor(GeneralModelingEditor editor) : base(editor) {}

    public void SetBonePositionText()
    {
        if (SelectedBone == null)
            return;

        Editor.UI.SetTransform(
            SelectedBone.Position.X,
            SelectedBone.Position.Y,
            SelectedBone.Position.Z
        );
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

    public override void Start()
    {
        Started = true;
        Console.WriteLine("Start Rigging Editor");
    }

    public override void Resize()
    {

    }

    public override void Awake()
    {
        Editor.UI.SetTransformAction(SetTransform);
        Editor.UI.SetScaleAction(SetScale);
        Editor.UI.SetRotationAction(SetRotation);

        Editor.UI.SetTransform((0, 0, 0));
        Editor.UI.SetRotation((0, 0, 0));
        Editor.UI.SetScale((0, 0, 0));

        ModelSettings.WireframeVisible = true;
        
        for (int i = 0; i < ModelManager.SelectedModels.Count; i++)
        {
            var model = ModelManager.SelectedModels[i];
            model.SetModeling();
            model.RenderBones = true;
        }

        if (Model == null)
            return;

        Model.UpdateBonePosition(Model.Rig, Editor.ProjectionMatrix, Camera.ViewMatrix);
    }
    
    public override void Render()
    {
        Editor.RenderModel();

        if (renderSelection)
        {
            /*
            ModelingEditingMode.selectionShader.Bind();

            Matrix4 model = Matrix4.CreateTranslation((oldMousePos.X - 200, oldMousePos.Y - 50, 0));
            Matrix4 projection = WindowProjection;
            Vector2 selectionSize = Input.GetMousePosition() - oldMousePos;
            Vector3 color = new Vector3(1, 0.5f, 0.25f);

            GL.UniformMatrix4(ModelingEditingMode.SelectionModelLocation, true, ref model);
            GL.UniformMatrix4(ModelingEditingMode.SelectionProjectionLocation, true, ref projection);
            GL.Uniform2(ModelingEditingMode.SelectionSizeLocation, selectionSize);
            GL.Uniform3(ModelingEditingMode.SelectionColorLocation, color);

            ModelingEditingMode.selectionVao.Bind();

            GL.DrawArrays(PrimitiveType.Lines, 0, 8);

            ModelingEditingMode.selectionVao.Unbind();

            ModelingEditingMode.selectionShader.Unbind();
            */
        }
    }

    public override void EndRender()
    {
        
    }

    public override void Update()
    {
        if (Input.IsKeyDown(Key.ControlLeft) && Input.IsKeyPressed(Key.A))
        {
            Handle_SelectAllVertices();
        }

        if (Input.IsKeyPressed(Key.Enter) && FileManager.IsVisible)
        {
            if (FileManager.HandleType == FileManagerType.Import && FileManager.SelectedFiles.Count > 0)
            {
                if (Model != null && RigManager.LoadRigFromPath(FileManager.SelectedFiles.First(), out var rig))
                {
                    Model.Rig?.Delete();
                    Model.Rig = rig;
                    Model.SetStaticRig();
                    Model?.UpdateBonePosition(Model.Rig, Editor.ProjectionMatrix, Camera.ViewMatrix);
                }
                FileManager.ToggleOff();
            }
            else if (FileManager.HandleType == FileManagerType.Export)
            {
                if (!_writeName)
                {
                    FileManager.UI.SetFieldAsWriteName();
                    _writeName = true;
                }
                else
                {
                    if (Model?.Rig != null)
                    {
                        string path = FileManager.UI.CurrentPath;
                        string name = FileManager.UI.PathText;
                        if (name.Length > 0)
                        {
                            if (!name.EndsWith(".rig"))
                                name += ".rig";

                            path = Path.Combine(path, name);
                            RigManager.SaveRigToPath(Model.Rig, path);
                        }
                    }
                    FileManager.UI.SetFieldAsPath();
                    FileManager.ToggleOff();
                    _writeName = false;
                }
            }
        }
        
        if (Input.IsKeyPressed(Key.Escape))
        {
            Editor.freeCamera = !Editor.freeCamera;

            if (Editor.freeCamera)
            {
                Game.Instance.CursorMode = CursorMode.Disabled;
                Camera.SetCameraMode(CameraMode.Free);
                renderSelection = false;
            }
            else
            {
                Game.Instance.CursorMode = CursorMode.Normal;
                Camera.SetCameraMode(CameraMode.Fixed);
                Model?.UpdateBonePosition(Model.Rig, Editor.ProjectionMatrix, Camera.ViewMatrix);
                Model?.UpdateVertexPosition();
            }
        }

        if (Input.IsMousePressed(MouseButton.Left) && !_modelSelected)
        {
            for (int i = 0; i < ModelManager.SelectedModels.Count; i++)
            {
                var model = ModelManager.SelectedModels[i];
                ModelManager.UnSelect(model);
            }
            ModelManager.SelectedModels = [];
        }

        if (!Editor.freeCamera)
        {
            MultiSelect();
        }

        RigUpdate();
    }

    private void RigUpdate()
    {
        if (Input.IsMousePressed(MouseButton.Left) && !ClickedMenu)
        {
            TestBonePosition();
            HandleVertexSelection();
        }

        TestAdd();
        TestRemove();

        if (Input.IsKeyDown(Key.ControlLeft))
        {
            if (Input.IsKeyPressed(Key.B))
            {
                BindSelectedVertices();
            }

            if (Input.IsKeyPressed(Key.N)) Model?.GetConnectedVertices();
        }

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
            Model?.UpdateBonePosition(Model.Rig, Editor.ProjectionMatrix, Camera.ViewMatrix);
        }

        if (Input.IsKeyReleased(Key.R))
        {
            Game.SetCursorState(CursorMode.Normal);
            Model?.UpdateBonePosition(Model.Rig, Editor.ProjectionMatrix, Camera.ViewMatrix);
        }
    }

    public override void Exit()
    {
        foreach (var (_, model) in ModelManager.Models)
        {
            model.RenderBones = false;
        }

        Model?.SetStaticRig();
    }

    public void SetTransform(int i, float value)
    {
        if (SelectedBone != null)
        {
            Vector3 position = SelectedBone.Position;
            position[i] = value;
            SelectedBone.Position = position;   
            Model?.Rig?.RootBone.UpdateGlobalTransformation();
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
            Model?.Rig?.RootBone.UpdateGlobalTransformation();
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
            Model?.Rig?.RootBone.UpdateGlobalTransformation();
            Model?.UpdateRig();
        }
    }

    public void BindSelectedVertices()
    {
        if (Model == null || Model.Rig == null)
            return;

        var bones = GetSelectedBones();
        if (bones.Count == 0)
            return;

        var bone = bones.First();
        foreach (var vert in Model.SelectedVertices)
        {
            vert.Bone = bone;
            vert.BoneName = bone.Name;
        }

        //Model.BindRig();
        //Model.Mesh.UpdateModel();
    }

    public void Handle_SelectAllVertices(bool generateColors = true)
    {
        if (Model == null)
            return;

        if (Input.IsKeyDown(Key.ShiftLeft))
        {
            Model.GetConnectedVertices();
        }
        else
        {
            Model.SelectedVertices.Clear();

            foreach (var vert in Model.Mesh.VertexList)
            {
                Model.SelectedVertices.Add(vert);
            }
        }     
        if (generateColors)     
            Model.GenerateVertexColor();
    }

    public void MultiSelect()
    {
        if (Model == null)
            return;

        if (Input.IsMousePressed(MouseButton.Left))
        {
            oldMousePos = Input.GetMousePosition();
        }

        if (Input.IsMouseDown(MouseButton.Left) && !blocked)
        {
            renderSelection = true;
            
            Vector2 mousePos = Input.GetMousePosition();
            Vector2 max = Mathf.Max(mousePos, oldMousePos);
            Vector2 min = Mathf.Min(mousePos, oldMousePos); 
            float distance = Vector2.Distance(mousePos, oldMousePos);
            bool regenColor = false;

            if (distance < 5)
                return;

            if (ModelingEditingMode.selectionType == RenderType.Vertex)
            {
                for (int i = 0; i < Model.Mesh.VertexList.Count; i++)
                {
                    var vertex = Model.Mesh.VertexList[i];
                    var position = vertex.Screen;
                    if (position.X >= min.X && position.X <= max.X && position.Y >= min.Y && position.Y <= max.Y)
                    {
                        if (!Model.SelectedVertices.Contains(vertex))
                        {
                            regenColor = true;
                            Model.SelectedVertices.Add(vertex);
                        }
                    }
                    else
                    {
                        if (!Input.IsKeyDown(Key.ShiftLeft) && Model.SelectedVertices.Contains(vertex))
                        {
                            regenColor = true;
                            Model.SelectedVertices.Remove(vertex);
                        }
                    }
                }
            }
            else if (ModelingEditingMode.selectionType == RenderType.Face)
            {
                for (int i = 0; i < Model.Triangles.Count; i++)
                {
                    var (triangle, position, _) = Model.Triangles[i];
                    if (position.X >= min.X && position.X <= max.X && position.Y >= min.Y && position.Y <= max.Y)
                    {
                        if (!Model.SelectedTriangles.Contains(triangle))
                        {
                            regenColor = true;
                            Model.SelectedTriangles.Add(triangle);
                        }
                    }
                    else
                    {
                        if (!Input.IsKeyDown(Key.ShiftLeft) && Model.SelectedTriangles.Contains(triangle))
                        {
                            regenColor = true;
                            Model.SelectedTriangles.Remove(triangle);
                        }
                    }
                }
            }

            if (regenColor)
                Model.GenerateVertexColor();
        }

        if (Input.IsMouseReleased(MouseButton.Left))
        {
            renderSelection = false;
            oldMousePos = Vector2.Zero;
        }
    }

    public void HandleVertexSelection()
    {   
        if (Model == null)
            return;
            
        if (!Input.IsKeyDown(Key.ShiftLeft))
            Model.SelectedVertices.Clear();
        
        Vector2 mousePos = Input.GetMousePosition();
        Vector2? closest = null;
        Vertex? closestVert = null;
    
        for (int i = 0; i < Model.Mesh.VertexList.Count; i++)
        {
            var vertex = Model.Mesh.VertexList[i];
            var position = vertex.Screen;
            float distance = Vector2.Distance(mousePos, position);
            float distanceClosest = closest == null ? 1000 : Vector2.Distance(mousePos, (Vector2)closest);
        
            if (distance < distanceClosest && distance < 10)
            {
                closest = position;
                closestVert = vertex;
            }
        }

        if (closestVert != null && !Model.SelectedVertices.Remove(closestVert))
            Model.SelectedVertices.Add(closestVert);

        Model.GenerateVertexColor();
    }

    public void TestAdd()
    {
        if (Input.IsKeyDown(Key.Q) && Input.IsKeyPressed(Key.D))
        {
            if (!_d_pressed)
            {
                _d_pressed = true;
            }
            else
            {
                _d_pressed = false;
                Console.WriteLine("Added bone");
                AddBone();
            }
        }

        if (Input.IsKeyReleased(Key.Q))
        {
            _d_pressed = false;
        }
    }

    public void AddBone()
    {
        if (Model == null || Model.Rig == null)
            return;

        var bones = GetSelectedBones();
        if (bones.Count == 0)
            return;

        var bone = bones.First();
        ChildBone child = new ChildBone("ChildBone" + Model.Rig.Bones.Count, bone);
        child.Position = new Vector3(0, 2, 0) * 0.1f;

        Model.Rig.RootBone.UpdateGlobalTransformation();
        Model.InitRig();
        //Model.BindRig();

        Model?.UpdateBonePosition(Model.Rig, Editor.ProjectionMatrix, Camera.ViewMatrix);
        Model?.UpdateVertexPosition();
    }

    public void TestRemove()
    {
        if (Input.IsKeyPressed(Key.Delete))
        {
            RemoveBone();
        }
    }

    public void RemoveBone()
    {
        if (Model == null || Model.Rig == null)
            return;

        var bones = GetSelectedBones();
        if (bones.Count == 0)
            return;

        foreach (var bone in bones)
        {
            bone.Delete();
        }

        Model.Rig.RootBone.UpdateGlobalTransformation();
        Model.InitRig();
        //Model.BindRig();

        Model?.UpdateBonePosition(Model.Rig, Editor.ProjectionMatrix, Camera.ViewMatrix);
        Model?.UpdateVertexPosition();
    }

    public void Handle_BoneMovement()
    {
        if (Model == null || Model.Rig == null)
            return;

        Vector2 mouseDelta = Input.GetMouseDelta();
        if (mouseDelta == Vector2.Zero)
            return;

        foreach (var bone in SelectedBones)
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

        Model.Rig.RootBone.UpdateGlobalTransformation();
        Model.Mesh.UpdateRig(Model.Rig);

        foreach (var bone in SelectedBones)
        {
            bone.UpdateEndTarget();
        }
    }

    public void Handle_BoneRotation()
    {
        if (Model == null || Model.Rig == null)
            return;

        Vector2 mouseDelta = Input.GetMouseDelta();
        if (mouseDelta == Vector2.Zero)
            return;

        foreach (var bone in SelectedBones)
        {
            bone.Rotate(Camera.front, mouseDelta.X * GameTime.DeltaTime * 50f);
        }

        Model.Rig.RootBone.UpdateGlobalTransformation();
        Model.Mesh.UpdateRig(Model.Rig);

        foreach (var bone in SelectedBones)
        {
            bone.UpdateEndTarget();
        }
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

    public void TestBonePosition()
    {
        if (Model == null)
            return;

        if (!Input.IsKeyDown(Key.ShiftLeft))
        {
            SelectedBonePivots = [];
            SelectedBoneName = "";
            SelectedBone = null;
            //BoneNameText.SetText("", 1f);
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
                SetBonePositionText();
                SetBoneRotationText();
                //BoneNameText.SetText(name, 1f);
            }
            else
            {
                SelectedBones.Remove(closestBone.Bone);
                if (SelectedBonePivots.Count > 0)
                {
                    string name = SelectedBones[0].Name;
                    SelectedBoneName = name;
                    SelectedBone = SelectedBones[0];
                    SetBonePositionText();
                    SetBoneRotationText();
                    //BoneNameText.SetText(name, 1f);
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

        Model.Mesh.UpdateRig(Model.Rig);
    }

    public Vector3 GetMovement()
    {
        Camera camera = Camera;

        Vector2 mouseDelta = Input.GetMouseDelta() * (GameTime.DeltaTime * 10);
        Vector3 move = camera.right * mouseDelta.X + camera.up * -mouseDelta.Y;

        return move;
    }

    public static bool IsPointCloseToLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd, float threshold)
    {
        float distance = DistancePointToLine(point, lineStart, lineEnd);
        return distance <= threshold;
    }
    
    private static float DistancePointToLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 line = lineEnd - lineStart;
        Vector2 pointVector = point - lineStart;
        float lineLengthSquared = line.LengthSquared;
        
        if (lineLengthSquared == 0f)
        {
            return Vector2.Distance(point, lineStart);
        }
        float t = Mathf.Clamp01y(Vector2.Dot(pointVector, line) / lineLengthSquared);
        Vector2 projection = lineStart + t * line;
        return Vector2.Distance(point, projection);
    }
}