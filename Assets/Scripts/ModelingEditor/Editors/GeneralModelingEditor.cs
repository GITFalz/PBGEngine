using PBG;
using PBG.Core;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Modeling;
using PBG.Rendering;
using PBG.UI;
using PBG.UI.FileManager;

public class GeneralModelingEditor : ScriptingNode
{
    public static GeneralModelingEditor Instance = null!;

    public static Matrix4 GetProjectionMatrix() => Instance.ProjectionMatrix;
    public static Matrix4 GetViewMatrix() => Instance.Scene.DefaultCamera.ViewMatrix;

    public BaseEditor CurrentEditor;
    public ModelingEditor modelingEditor;
    public RiggingEditor riggingEditor;
    public AnimationEditor animationEditor;
    public TextureEditor textureEditor;

    public GeneralEditorUI UI;

    public bool BackfaceCulling
    {
        get => ModelSettings.BackfaceCulling;
        set => ModelSettings.BackfaceCulling = value;
    }

    public Action LoadAction = () => { };
    public Action SaveAction = () => { };

    public Action FileManagerLoadAction = () => { };

    public bool freeCamera = false;
    public bool Regenerate = false;
    public bool ClickedMenu = false;

    public FileManager FileManager = null!;

    public bool RenderingGrid = true;

    public Matrix4 ProjectionMatrix;
    public Matrix4 WindowProjection;

    public TransformGizmo TransformGizmo;
    public RotationGizmo RotationGizmo;

    public Viewport ModelsViewport = null!;

    public GeneralModelingEditor(FileManager fileManager)
    {
        Instance = this;
        FileManager = fileManager;
        UI = new(this);
    }

    public void Start()
    {
        Console.WriteLine("Animation Editor Start");

        ModelSettings.Camera = Scene.DefaultCamera;

        modelingEditor = new ModelingEditor(this);
        riggingEditor = new RiggingEditor(this);
        animationEditor = new AnimationEditor(this);
        textureEditor = new TextureEditor(this);

        CurrentEditor = modelingEditor;

        TransformGizmo = new(Scene.DefaultCamera, ProjectionMatrix);
        RotationGizmo = new(Scene.DefaultCamera, ProjectionMatrix);

        foreach (var (name, model) in ModelManager.Models)
        {
            GenerateModelButton(model);
        }

        LoadAction = () =>
        {
            LoadModel();
            GenerateModelButton(ModelManager.SelectedModel);
        };
        SaveAction = SaveModel;
        FileManagerLoadAction = () =>
        {
            foreach (var (name, model) in ModelManager.Models)
            {
                GenerateModelButton(model);
            }
        };

        var controller = Transform.ParentNode.GetNode("MainUI").GetComponent<UIController>();
        controller.AddElement(UI);

        ModelsViewport = Transform.ParentNode.GetNode("Modeling").GetComponent<Viewport>();
    }

    public void Resize()
    {
        ProjectionMatrix = Matrix4.CreatePerspective(
            Mathf.DegreesToRadians(Scene.DefaultCamera.FOV),
            (float)Game.Width / (float)Game.Height,
            0.1f,
            1000f
        );
        WindowProjection = Matrix4.CreateOrthographicOffCenter(0, Game.Width - 400, Game.Height - 50, 0, -2, 2);

        CurrentEditor.Resize();

        TransformGizmo.Projection = ProjectionMatrix;
        RotationGizmo.Projection = ProjectionMatrix;
        TransformGizmo.GenerateWorldSpacePoints();
        RotationGizmo.GenerateWorldSpacePoints();
    }

    public void Awake()
    {
        Console.WriteLine("Awake ljhsgbfksbfvkusbvgfsvefs");
        ProjectionMatrix = Matrix4.CreatePerspective(
            Mathf.DegreesToRadians(Scene.DefaultCamera.FOV),
            (float)Game.Width / (float)Game.Height,
            0.1f,
            1000f
        );
        WindowProjection = Matrix4.CreateOrthographicOffCenter(0, Game.Width, Game.Height, 0, -2, 2);

        Scene.DefaultCamera.SetCameraMode(CameraMode.Free);

        Scene.DefaultCamera.Position = new Vector3(7.5f, 5, 7.5f);
        Scene.DefaultCamera.Pitch = -25;
        Scene.DefaultCamera.Yaw = -135;

        Scene.DefaultCamera.UpdateVectors();
        Scene.DefaultCamera.GetViewMatrix();

        Scene.DefaultCamera.SetCameraMode(CameraMode.Fixed);

        CurrentEditor.Awake();

        /*
        if (ModelManager.LoadModel("cube", out var model))
        {
            ModelManager.Select(model);
            var button = GetModelButton(model);
            UI.Hierarchy.AddElement(button);
            UI.Hierarchy.UIController?.AddElement(button);
        }
        */

        ObjLoader.Load(Path.Combine(Game.CustomPath, "models", "arknights-endfield-laevatain", "source", "laevat", "laevat.obj"), Scene.GetNode("Root/Models"));

        TransformGizmo.Projection = ProjectionMatrix;
        RotationGizmo.Projection = ProjectionMatrix;
        TransformGizmo.GenerateWorldSpacePoints();
        RotationGizmo.GenerateWorldSpacePoints();
    }

    public void UpdateHierarchy()
    {
        UI.Hierarchy.DeleteChildren();
        if (Scene.GetNode("Root/Models", out var modelsNode))
        {
            for (int i = 0; i < modelsNode.Children.Count; i++)
            {
                var child = modelsNode.Children[i];
                if (child.GetComponent<PBG_Model>(out var model))
                {
                    var button = GetModelButton(model);
                    UI.Hierarchy.AddElement(button);
                    UI.Hierarchy.UIController?.AddElement(button);
                }
            }  
        }
        
        foreach (var (_, model) in ModelManager.Models)
        {
            var button = GetModelButton(model);
            UI.Hierarchy.AddElement(button);
            UI.Hierarchy.UIController?.AddElement(button);
        }
    }

    public bool _writeName = false;

    public void Update()
    {
        UI.Update();

        CurrentEditor.Update();
    }

    void LateUpdate()
    {
        ClickedMenu = false;
    }

    public void Render()
    {
        /*
        if (RenderingGrid)
            RenderGrid();

        GL.Enable(EnableCap.DepthTest);
        GL.CullFace(TriangleFace.Back);

        CurrentEditor.Render();

        GL.Clear(ClearBufferMask.DepthBufferBit);

        CurrentEditor.EndRender();

        if (ModelSettings.RenderScreenSpacePositions)
        {
            GL.Disable(EnableCap.DepthTest);
            GL.DepthMask(false);
            GL.Disable(EnableCap.CullFace);

            ColoredRectangles.Color = (1, 0, 0, 0.5f);
            ColoredRectangles.Render(Matrix4.Identity, WindowProjection);

            GL.Enable(EnableCap.CullFace);
            GL.DepthMask(true);
            GL.Enable(EnableCap.DepthTest);
        }
        */
    }

    public void RenderGrid() => RenderGrid(ProjectionMatrix);
    public void RenderGrid(Matrix4 projection)
    {
        /*
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        StructureMeshRenderer.gridShader.Bind();

        Vector2 gridSize = new Vector2(200, 200);
        Matrix4 gridModel = Matrix4.CreateTranslation(new Vector3(-gridSize.X * 0.5f, -0.02f, -gridSize.Y * 0.5f) + new Vector3(Scene.DefaultCamera.Position.X, 0, Scene.DefaultCamera.Position.Z));
        Matrix4 view = Scene.DefaultCamera.ViewMatrix;

        GL.UniformMatrix4(StructureMeshRenderer.gridModelLocation, false, ref gridModel);
        GL.UniformMatrix4(StructureMeshRenderer.gridViewLocation, false, ref view);
        GL.UniformMatrix4(StructureMeshRenderer.gridProjectionLocation, false, ref projection);
        GL.Uniform2(StructureMeshRenderer.gridSizeLocation, ref gridSize);
        GL.Uniform3(StructureMeshRenderer.gridCamPosLocation, new Vector3(Scene.DefaultCamera.Position.X, 0, Scene.DefaultCamera.Position.Z));

        StructureMeshRenderer.gridVao.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        Shader.Error("Grid shader error: ");

        StructureMeshRenderer.gridVao.Unbind();
        StructureMeshRenderer.gridShader.Unbind();
        */
    }

    public void Exit()
    {
        CurrentEditor.Exit();
        ModelManager.Delete();
    }

    public void TransformGizmoAction(Quaternion rotation, Vector2 mouseDelta, int info, Action<Vector3> moveAction)
    {
        Vector3 move = Vector3.Zero;
        int count = 0;
        
        void setMove(int index)
        {
            Vector2 axisScreen = TransformGizmo.SliderDirection(index);
            if (axisScreen == Vector2.Zero) return;

            Vector2 axisDir = axisScreen.Normalized();

            float deltaAlongAxis = Vector2.Dot(mouseDelta, axisDir);

            move[index] = deltaAlongAxis;
        }

        if ((info & 0b1000) != 0)
        {
            setMove(0);
            count++;
        }

        if ((info & 0b10000) != 0)
        {
            setMove(1);
            count++;
        }

        if ((info & 0b100000) != 0)
        {
            setMove(2);
            count++;
        }

        if (count == 0)
            return;
        
        float mag = move.Length;
        if (mag > 0f) move /= mag;

        if (ModelSettings.IsLocalMode)
        {
            move = Vector3.Transform(move, rotation);
        }

        if (move != Vector3.Zero)
        {
            move *= mouseDelta.Length * 0.001f * Vector3.Distance(Scene.DefaultCamera.Position, TransformGizmo.Position);
            moveAction(move);
        }
    }

    public void RotationGizmoAction(Quaternion rotation, Vector2 mouseDelta, Vector2 axisScreen, int info, Action<Vector3, float> rotateAction)
    {
        Vector3 move = Vector3.Zero;

        int index = (info >> 3) switch
        {
            0b1 => 0,
            0b10 => 1,
            0b100 => 2,
            _ => 0
        };

        
        if (axisScreen == Vector2.Zero) return;

        Vector2 axisDir = axisScreen.Normalized();

        float deltaAlongAxis = Vector2.Dot(mouseDelta, axisDir) * 0.01f;

        move[index] = 1f;

        if (ModelSettings.IsLocalMode)
        {
            move = Vector3.Transform(move, rotation);
        }

        if (move != Vector3.Zero)
        {
            rotateAction(move, deltaAlongAxis);
        }
    }

    public void GenerateModelButton(Model? model)
    {
        if (model == null)
            return;
    }

    public UIElementBase GetModelButton(Model model) => UI.GetModelButton(model);
    public UIElementBase GetModelButton(PBG_Model model) => UI.GetModelButton(model);
    
    public void SetFileManagerExportAsModel()
    {
        _writeName = false;
    }
    
    public void DoSwitchScene(BaseEditor editor)
    {
        CurrentEditor.Exit();
        CurrentEditor = editor;
        if (!CurrentEditor.Started)
            CurrentEditor.Start();
        CurrentEditor.Awake();
    }

    public void Load()
    {
        LoadAction?.Invoke();
    }

    public void Save()
    {
        SaveAction?.Invoke();
    }

    public void LoadModel()
    {
        //string fileName = FileName.Text.Trim();
        //ModelManager.LoadModel(fileName);
    }

    public void SaveModel()
    {
        //string fileName = FileName.Text.Trim();
        //ModelManager.SaveModel(fileName);
    }

    public void RenderModel()
    {
        /*
        if (BackfaceCulling)
        {
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
        }
        else
        {
            GL.Disable(EnableCap.CullFace);
        }

        ModelManager.Render();

        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        */
    }

    #region Saved ui functions (Do not delete)

    public void SnappingField()
    {
        //string text = SnappingText.Text;
        //SnappingFactor = Mathf.Clamp(0, 100, Float.Parse(text, 0.0f));
        //Snapping = SnappingFactor != 0.0f;
    }

    public void ApplyMirror()
    {
        var model = ModelManager.SelectedModel;
        if (model == null)
            return;
            
        model.Mesh.ApplyMirror();
        model.Mesh.CombineDuplicateVertices();
        
        model.Mesh.RegenerateAll();
        
        Regenerate = true; 
        
        ModelSettings.Mirror = new Vector3i(0, 0, 0);
    }
    #endregion
}