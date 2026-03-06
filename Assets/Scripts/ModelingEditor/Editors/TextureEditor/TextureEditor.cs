using System.Drawing;
using PBG;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.Threads;
using PBG.UI;
using PBG.UI.Creator;
using PBG.UI.FileManager;
using Silk.NET.Input;

public class TextureEditor : BaseEditor 
{
    /*
    public static ShaderProgram UvShader = new ShaderProgram("Uv/Uv.vert", "Uv/Uv.frag");
    public static ShaderProgram UvEdgeShader = new ShaderProgram("Uv/Edge.vert", "Uv/Edge.frag");
    public static ShaderProgram UvVertShader = new ShaderProgram("Uv/Vertex.vert", "Uv/Vertex.frag");
    */

    private Vector2i _windowPosition = new Vector2i(200, 50);

    public float SeparationPercent = 0.5f;
    private Vector2i _drawingCanvasSize = new Vector2i(400, 400);
    private Vector2i _modelDisplaySize = new Vector2i(400, 400);

    private Vector2 _distance = new Vector2(200, -100);
    private Vector2 _canvasPosition = new Vector2(0, 0);

    private bool _hoveringCanvas = false;
    private bool _hoveringModelDisplay = false;
    
    private Triangle? _oldHoveredTriangle = null;
    private Vector2 _modelBrushUv = Vector2.Zero;
    private Vector2? _oldModelBrushUv = null;
    private bool _modelDrawing = false;

    public UvMesh UvMesh = new();

    // Color picker
    public int ColorPickerWidth = 300;
    public int ColorPickerHeight = 200;
    public Vector2i ColorPickerPosition = new Vector2i(100, 100);
    public ColorPicker ColorPicker = null!;
    private bool _canDraw = true;

    private bool _regenerateColors = true;

    public bool renderSelection = false;
    public Vector2 oldMousePos = Vector2.Zero;

    public struct PositionData
    {
        public HashSet<Uv> Uvs;

        public PositionData() { Uvs = []; }
        public PositionData(params Uv[] uvs ) { Uvs = [.. uvs]; }

        public bool Add(Uv uv) => Uvs.Add(uv);
    }
    public Dictionary<Vector2, PositionData> Uvs = [];
    public Dictionary<Vector2, Vertex> Vertices = [];

    public List<Uv> SelectedUvs = [];
    public List<(UvTriangle, int)> IncludedSelectedTriangles = []; // All triangles where at least 1 uv is selected;


    public string? CurrentFilePath = null;

    // Scaling
    private Vector2 selectedCenter;
    private float _currentScale = 1f;
    private List<Vector2> _currentScalePositions = [];

    public TextureEditor(GeneralModelingEditor editor) : base(editor)
    {
        _ = new DrawingPanel(1000, 1000);

        _drawingCanvasSize = new Vector2i((int)((Game.Width - 400) * SeparationPercent), Game.Height - 50);
        _modelDisplaySize = new Vector2i(Game.Width - 400 - _drawingCanvasSize.X, Game.Height - 50);
        
        DrawingPanel.CanvasPosition = ((float)_drawingCanvasSize.X / 2f - 50, (float)_drawingCanvasSize.Y / 2f - 50);
    }

    public override void Start()
    {
        Started = true;
        
        ColorPicker = Editor.Scene.GetNode("Root/ColorPicker").GetComponent<ColorPicker>();
        ColorPicker.Transform.Disabled = true;

        DrawingPanel.ColorPickAction = (r, g, b) => ColorPicker.SetColor(r, g, b, false); 
    }

    public override void Resize()
    {
        float tX = ((float)DrawingPanel.CanvasPosition.X + ((float)DrawingPanel.Width / 2f)) / (float)_drawingCanvasSize.X;
        float tY = ((float)DrawingPanel.CanvasPosition.Y + ((float)DrawingPanel.Height / 2f)) / (float)_drawingCanvasSize.Y;

        _drawingCanvasSize = new Vector2i((int)((Game.Width - 400) * SeparationPercent), Game.Height - 50);
        _modelDisplaySize = new Vector2i(Game.Width - 400 - _drawingCanvasSize.X, Game.Height - 50);

        DrawingPanel.CanvasPosition = (tX * _drawingCanvasSize.X - ((float)DrawingPanel.Width / 2f), tY * _drawingCanvasSize.Y - ((float)DrawingPanel.Height / 2f));
        DrawingPanel.WindowPosition = (_windowPosition.X, _windowPosition.Y);
        DrawingPanel.WindowWidth = _drawingCanvasSize.X;
        DrawingPanel.WindowHeight = _drawingCanvasSize.Y;

        DrawingPanel.SetDrawingCanvasPosition(DrawingPanel.CanvasPosition.X + _windowPosition.X, DrawingPanel.CanvasPosition.Y + (Game.Height - DrawingPanel.WindowHeight) - DrawingPanel.WindowPosition.Y);
        
        Editor.ModelsViewport.SetViewport(200 + _drawingCanvasSize.X, 200, 0, 50);
    }

    public override void Awake()
    {
        Editor.RenderingGrid = false;

        DrawingPanel.IsDrawing = true;
        Game.ForceSyncedRendering = true;

        float tX = ((float)DrawingPanel.CanvasPosition.X + ((float)DrawingPanel.Width / 2f)) / (float)_drawingCanvasSize.X;
        float tY = ((float)DrawingPanel.CanvasPosition.Y + ((float)DrawingPanel.Height / 2f)) / (float)_drawingCanvasSize.Y;

        _drawingCanvasSize = new Vector2i((int)((Game.Width - 400) * SeparationPercent), Game.Height - 50);
        _modelDisplaySize = new Vector2i(Game.Width - 400 - _drawingCanvasSize.X, Game.Height - 50);

        DrawingPanel.CanvasPosition = (tX * _drawingCanvasSize.X - ((float)DrawingPanel.Width / 2f), tY * _drawingCanvasSize.Y - ((float)DrawingPanel.Height / 2f));

        DrawingPanel.WindowPosition = (_windowPosition.X, _windowPosition.Y);
        DrawingPanel.WindowWidth = _drawingCanvasSize.X;
        DrawingPanel.WindowHeight = _drawingCanvasSize.Y;

        DrawingPanel.SetDrawingCanvasPosition(DrawingPanel.CanvasPosition.X + _windowPosition.X, DrawingPanel.CanvasPosition.Y + (Game.Height - DrawingPanel.WindowHeight));

        Editor.ModelsViewport.SetViewport(200 + _drawingCanvasSize.X, 200, 0, 50);

        _regenerateColors = true;

        if (Model == null)
            return;

        var proj = Matrix4.CreatePerspective(Mathf.DegreesToRadians(Editor.Scene.DefaultCamera.FOV), (float)_modelDisplaySize.X / (float)_modelDisplaySize.Y, 0.1f, 1000f);
        Model.UpdateVertexPosition(proj, Camera.ViewMatrix, _modelDisplaySize.X, _modelDisplaySize.Y, (DrawingPanel.WindowPosition.X + DrawingPanel.WindowWidth, DrawingPanel.WindowPosition.Y));
        //Model.BindTexture =() => DrawingPanel.BindTexture(TextureUnit.Texture0);
        //Model.UnbindTexture = DrawingPanel.UnbindTexture;
        CurrentFilePath = Model.TextureFilePath;
        Model.SetModeling();

        LoadUvs();
        
        if (CurrentFilePath != null)
            DrawingPanel.LoadTexture(CurrentFilePath);
    }

    public override void Update()
    {
        Vector2 mousePos = Input.GetMousePosition();
        
        _hoveringCanvas = mousePos.X >= _windowPosition.X &&
            mousePos.X <= _windowPosition.X + _drawingCanvasSize.X &&
            mousePos.Y >= _windowPosition.Y &&
            mousePos.Y <= _windowPosition.Y + _drawingCanvasSize.Y;

        _hoveringModelDisplay = mousePos.X >= _windowPosition.X + _drawingCanvasSize.X &&
            mousePos.X <= _windowPosition.X + _drawingCanvasSize.X + _modelDisplaySize.X &&
            mousePos.Y >= _windowPosition.Y &&
            mousePos.Y <= _windowPosition.Y + _modelDisplaySize.Y;

        if (Input.IsKeyDown(Key.ControlLeft) && Input.IsKeyPressed(Key.S))
        {
            if (CurrentFilePath != null)
            {
                DrawingPanel.SaveTexture(CurrentFilePath);
                if (Model != null) 
                    Model.TextureFilePath = CurrentFilePath;
            }
        }

        if (Input.IsKeyPressed(Key.Enter) && FileManager.IsVisible)
        {
            if (FileManager.HandleType == FileManagerType.Import && FileManager.SelectedFiles.Count > 0)
            {
                var path = FileManager.SelectedFiles.First();
                if (Path.GetExtension(path) == ".png")
                {
                    DrawingPanel.LoadTexture(path);
                    CurrentFilePath = path;
                    if (Model != null)
                        Model.TextureFilePath = path;
                }
                FileManager.ToggleOff();
            }
            else if (FileManager.HandleType == FileManagerType.Export)
            {
                var path = FileManager.GetSaveFilePath();
                if (path != null)
                {
                    SaveTexture(path);
                }
            }
        }

        if (DrawingPanel.DrawingMode != DrawingMode.Pick && Input.IsMouseDown(MouseButton.Left) && ColorPicker.IsHovering())
        {
            _canDraw = false;
        }

        if (!_canDraw && Input.IsMouseReleased(MouseButton.Left))
        {
            _canDraw = true;
        }

        if (!Editor.freeCamera && DrawingPanel.DrawingMode == DrawingMode.None)
        {
            MultiSelect();
        }

        if (_hoveringModelDisplay && !ColorPicker.IsHovering())
        {
            UpdateModel();
        }

        if (_hoveringCanvas && !ColorPicker.IsHovering())
        {
            UpdateUv();
        }
    }

    private void UpdateModel()
    {
        var oldModelBrushUv = _modelBrushUv;
        if (TriangleHoverTest(out var changeTriangle))
        {
            _oldModelBrushUv = oldModelBrushUv;
        }
        if (changeTriangle)
        {
            _oldModelBrushUv = _modelBrushUv;
        }

        if (Input.IsMousePressed(MouseButton.Left))
        {
            if (!FileManager.IsHovering && Editor.UI.HoveringCenter && DrawingPanel.DrawingMode == DrawingMode.None)
            {
                VertexClickTest();
            }

            _oldModelBrushUv = _modelBrushUv;
        }

        _modelDrawing = DrawingPanel.IsDrawing && Input.IsMouseDown(MouseButton.Left) && ((int)DrawingPanel.DrawingMode & (int)DrawingModeSet.BrushCircleSet) != 0 && (_oldModelBrushUv != _modelBrushUv || !Input.GetMouseDelta().Equals(Vector2.Zero));
        if (Input.IsMousePressed(MouseButton.Left))
        {
            _modelDrawing = true;
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
                TransformGizmo.GenerateWorldSpacePoints();
                RotationGizmo.GenerateWorldSpacePoints();

                var proj = Matrix4.CreatePerspective(Mathf.DegreesToRadians(Editor.Scene.DefaultCamera.FOV), (float)_modelDisplaySize.X / (float)_modelDisplaySize.Y, 0.1f, 1000f);
                Model?.UpdateVertexPosition(proj, Camera.ViewMatrix, _modelDisplaySize.X, _modelDisplaySize.Y, (DrawingPanel.WindowPosition.X + DrawingPanel.WindowWidth, DrawingPanel.WindowPosition.Y));
            }
        }

        if (Input.IsKeyDown(Key.ControlLeft))
        {
            if (Input.IsKeyPressed(Key.A))
            {
                if (Model == null)
                    return;

                if (Input.IsKeyDown(Key.SuperLeft))
                {
                    Editor.modelingEditor.EditingMode.Handle_GetConnectedVertices();
                }
                else
                {
                    Model.SelectedVertices.Clear();

                    foreach (var vert in Model.Mesh.VertexList)
                    {
                        Model.SelectedVertices.Add(vert);
                    }
                }     
                
                Model.GenerateVertexColor();
            }
            
            if (Input.IsKeyPressed(Key.M))
            {
                Editor.modelingEditor.EditingMode.Handle_Mapping();
                LoadUvs();
                UpdatePositionData();
                GenerateVertexColor();
            }
        }
    }

    private void UpdateUv()
    {
        Vector2 mousePos = Input.GetMousePosition();

        DrawingPanel.Update();

        bool ctrl = Input.IsKeyDown(Key.ControlLeft);
        if (Input.IsMousePressed(MouseButton.Left) && !ctrl && !FileManager.IsHovering && Editor.UI.HoveringCenter && DrawingPanel.DrawingMode == DrawingMode.None)
        {
            UvClickTest();
        }

        float delta = Input.GetMouseScrollDelta().Y;
        if (ctrl)
        {

            if (Input.IsKeyDown(Key.B)) // Brush size
            {
                if (delta != 0)
                {
                    DrawingPanel.BrushSize += delta * 0.05f * (1 + DrawingPanel.BrushSize);
                    DrawingPanel.BrushSize = Mathf.Clampy(DrawingPanel.BrushSize, 0.01f, 100);
                }
            }
            else if (Input.IsKeyDown(Key.F)) // Falloff
            {
                if (delta != 0)
                {
                    DrawingPanel.Falloff += delta * 0.1f * (1 + DrawingPanel.Falloff);
                    DrawingPanel.Falloff = Mathf.Clampy(DrawingPanel.Falloff, 0, 5);
                }
                DrawingPanel.RenderSet = 1;
            }
            else if (Input.IsKeyDown(Key.W)) // brush strength
            {
                if (delta != 0)
                {
                    DrawingPanel.BrushStrength += delta * 0.1f * (1 + DrawingPanel.BrushStrength);
                    DrawingPanel.BrushStrength = Mathf.Clampy(DrawingPanel.BrushStrength, 0, 1);
                }
                DrawingPanel.RenderSet = 2;
            }
            else
            {
                DrawingPanel.Zoom(delta * (DrawingPanel.DrawingCanvasSize * 5) * 0.01f);
                _regenerateColors = true;
            }

            if (DrawingPanel.DrawingMode == DrawingMode.Move)
            {
                if (Input.IsMousePressed(MouseButton.Left))
                    _distance = mousePos - DrawingPanel.CanvasPosition;

                if (Input.IsMouseDown(MouseButton.Left))
                {
                    Vector2 mouseDelta = Input.GetMouseDelta();
                    if (mouseDelta != Vector2.Zero)
                    {
                        _canvasPosition = mousePos - _distance;
                        DrawingPanel.CanvasPosition.X = (int)_canvasPosition.X;
                        DrawingPanel.CanvasPosition.Y = (int)_canvasPosition.Y;

                        DrawingPanel.SetDrawingCanvasPosition(DrawingPanel.CanvasPosition.X + _windowPosition.X, DrawingPanel.CanvasPosition.Y + (Game.Height - DrawingPanel.WindowHeight) - DrawingPanel.WindowPosition.Y);
                        _regenerateColors = true;
                    }
                }
            }

            if (Input.IsKeyPressed(Key.A))
            {
                if (Input.IsKeyDown(Key.ShiftLeft))
                {
                    HashSet<Uv> uvs = [];
                    for (int i = 0; i < SelectedUvs.Count; i++)
                    {
                        var uv = SelectedUvs[i];
                        uvs.Add(uv.ParentTriangle.A);
                        uvs.Add(uv.ParentTriangle.B);
                        uvs.Add(uv.ParentTriangle.C);
                    }
                    SelectedUvs = [.. uvs];
                    if (Model != null) Model.SelectedVertices = [];
                    for (int i = 0; i < SelectedUvs.Count; i++)
                    {
                        Model?.SelectedVertices.Add(SelectedUvs[i].Vertex);
                    }
                }
                else
                {
                    SelectedUvs = UvMesh.UvList;
                    if (Model != null) Model.SelectedVertices = [];
                    for (int i = 0; i < SelectedUvs.Count; i++)
                    {
                        Model?.SelectedVertices.Add(SelectedUvs[i].Vertex);
                    }
                }

                GenerateVertexColor();
                Model?.GenerateVertexColor();
            }
            return;
        }

        var dirtyUpdate = false;

        if (Input.IsKeyDown(Key.G))
        {
            Handle_VertexMovement();
            dirtyUpdate = true;
        }

        if (Input.IsKeyDown(Key.R))
        {
            Handle_VertexRotation();
            dirtyUpdate = true;
        }

        if (Input.IsKeyPressed(Key.S)) ScalingInit();
        if (Input.IsKeyDown(Key.S)) Handle_ScalingSelectedVertices();
        if (Input.IsKeyReleased(Key.S))
        {
            _regenerateColors = true;
            _currentScalePositions = [];
            _currentScale = 1f;
        }

        if (Input.IsAnyKeyReleased(Key.G, Key.R))
        {
            _regenerateColors = true;   
        }

        if (dirtyUpdate)
        {
            DirtyUpdateModel();
        }

        if (_regenerateColors)
        {
            UpdatePositionData();
            GenerateVertexColor();
            _regenerateColors = false;
        }
    }
    
    public override void Render()
    {
        /*
        Editor.ModelsViewport.ApplyViewport();
        Editor.RenderGrid(Editor.ModelsViewport.ProjectionMatrix);

        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);

        ModelManager.SelectedModel?.RenderMirror(Editor.ModelsViewport.ProjectionMatrix);
        ModelManager.SelectedModel?.RenderWireframe(Editor.ModelsViewport.ProjectionMatrix);
        
        if (!ColorPicker.IsHovering() && Editor.UI.HoveringCenter && _canDraw)
        {
            if (_hoveringCanvas)
                DrawingPanel.RenderFramebuffer();
            else if (_hoveringModelDisplay && _modelDrawing)
                DrawingPanel.RenderFramebuffer(_oldModelBrushUv ?? _modelBrushUv, _modelBrushUv);   
        }

        GL.Viewport(DrawingPanel.WindowPosition.X, DrawingPanel.WindowPosition.Y, DrawingPanel.WindowWidth, DrawingPanel.WindowHeight);

        DrawingPanel.RenderTexture();
        DrawingPanel.RenderSelection();

        if (!ColorPicker.IsHovering() && Editor.UI.HoveringCenter)
        {
            if (_hoveringCanvas)
                DrawingPanel.RenderBrushCircle();
            else if (_hoveringModelDisplay)
                DrawingPanel.RenderBrushCircle(_modelBrushUv);
        }
        
        GL.Disable(EnableCap.DepthTest);

        GL.Clear(ClearBufferMask.DepthBufferBit);
        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.CullFace);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        UvShader.Bind();

        Vector2 position = DrawingPanel.CanvasPosition;
        Matrix4 model = Matrix4.CreateTranslation((position.X, position.Y, 0));
        Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0, DrawingPanel.WindowWidth, DrawingPanel.WindowHeight, 0, -1, 1);
        Vector2 size = DrawingPanel.CanvasScale;
        float colorAlpha = 0.5f;

        int modelLocation = UvShader.GetLocation("model");
        int projectionLocation = UvShader.GetLocation("projection");
        int sizeLocation = UvShader.GetLocation("size");
        int colorAlphaLocation = UvShader.GetLocation("colorAlpha");

        GL.UniformMatrix4(modelLocation, true, ref model);
        GL.UniformMatrix4(projectionLocation, true, ref projection);
        GL.Uniform2(sizeLocation, size);
        GL.Uniform1(colorAlphaLocation, colorAlpha);

        UvMesh.Render();

        UvShader.Unbind();

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Always);

        model = Matrix4.CreateTranslation((position.X, position.Y, 0));

        UvEdgeShader.Bind();

        modelLocation = UvEdgeShader.GetLocation("model");
        projectionLocation = UvEdgeShader.GetLocation("projection");
        sizeLocation = UvEdgeShader.GetLocation("size");

        GL.UniformMatrix4(modelLocation, true, ref model);
        GL.UniformMatrix4(projectionLocation, true, ref projection);
        GL.Uniform2(sizeLocation, size);

        UvMesh.RenderEdges();

        Shader.Error("Rendering edges error: ");

        UvEdgeShader.Unbind();

        UvVertShader.Bind();

        model = Matrix4.CreateTranslation((position.X, position.Y, 0));

        modelLocation = UvVertShader.GetLocation("model");
        projectionLocation = UvVertShader.GetLocation("projection");
        sizeLocation = UvVertShader.GetLocation("size");

        GL.UniformMatrix4(modelLocation, true, ref model);
        GL.UniformMatrix4(projectionLocation, true, ref projection);
        GL.Uniform2(sizeLocation, size);

        UvMesh.RenderVertices();

        Shader.Error("Rendering vertices error: ");

        UvVertShader.Unbind();

        GL.Disable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);

        if (renderSelection)
        {
            ModelingEditingMode.selectionShader.Bind();

            model = Matrix4.CreateTranslation((oldMousePos.X - 200, oldMousePos.Y, 0));
            projection = Matrix4.CreateOrthographicOffCenter(0, DrawingPanel.WindowWidth, DrawingPanel.WindowHeight, 0, -1, 1);
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
        }

        Game.ApplyViewport();
        */
    }

    public override void EndRender()
    {
    }

    public override void Exit()
    {
        Editor.ModelsViewport.SetViewport(0, 0, 0, 0);
        Model?.Renew();

        Editor.RenderingGrid = true;

        DrawingPanel.IsDrawing = false;
        Game.ForceSyncedRendering = false;
    }

    private bool TriangleHoverTest(out bool changeTriangle)
    {
        changeTriangle = false;
        if (Model == null || !Input.IsMouseDown(MouseButton.Left))
            return false;

        Vector2 mousePos = Input.GetMousePosition();

        float? closestDistance = null;
        Triangle? ct = null;
        Vector3 closestBary = Vector3.Zero;

        for (int i = 0; i < Model.Mesh.TriangleList.Count; i++)
        {
            var triangle = Model.Mesh.TriangleList[i];
            if (triangle.A.ClipW < 0 || triangle.B.ClipW < 0 || triangle.C.ClipW < 0)
                continue;

            if (Mathf.PointInTriangle(mousePos, triangle.A.Screen, triangle.B.Screen, triangle.C.Screen))
            {
                var bary = Mathf.Barycentric(mousePos, triangle.A.Screen, triangle.B.Screen, triangle.C.Screen);
                var center = Mathf.PerspectiveCorrectPosition(bary, triangle.A, triangle.B, triangle.C, triangle.A.ClipW, triangle.B.ClipW, triangle.C.ClipW);
                var distance = Vector3.Distance(Camera.Position, center);
                if (closestDistance == null || distance < closestDistance)
                {
                    closestDistance = distance;
                    ct = triangle;
                    closestBary = bary;
                }
            }
        }

        if (ct != null)
        {
            if (_oldHoveredTriangle != ct)
            {
                changeTriangle = true;
                _oldHoveredTriangle = ct;
            }

            var mouseUv = Mathf.PerspectiveCorrectUv(closestBary, ct.UvA, ct.UvB, ct.UvC, ct.A.ClipW, ct.B.ClipW, ct.C.ClipW);
            _modelBrushUv = mouseUv;
            
            return true;
        }
        else
        {
            if (_oldHoveredTriangle != null)
            {
                changeTriangle = true;
                _oldHoveredTriangle = null;
            }
        }

        return false;
    }

    public void SaveTexture()
    {
        if (CurrentFilePath != null)
            SaveTexture(CurrentFilePath);
    }
    
    public void SaveTexture(string path)
    {
        if (!path.EndsWith(".png"))
            path += ".png";

        DrawingPanel.SaveTexture(path);
        CurrentFilePath = path;
        if (Model != null) Model.TextureFilePath = path;
        FileManager.ToggleOff();
    }

    public void LoadUvs()
    {
        if (Model != null)
        {
            Console.WriteLine($"Loading Uvs from selected model {Model.Name}");
            UvMesh.LoadModel(Model);
        }        
    }
    
    public void DirtyUpdateModel()
    {
        if (Model == null)
            return;
        
        for (int i = 0; i < IncludedSelectedTriangles.Count; i++)
        {
            var (uvTriangle, index) = IncludedSelectedTriangles[i];
            if (index >= 0 && index < Model.Mesh.TriangleList.Count)
            {
                var triangle = Model.Mesh.TriangleList[index];
                triangle.UvA = uvTriangle.A;
                triangle.UvB = uvTriangle.B;
                triangle.UvC = uvTriangle.C;
            }
        }

        Model.Mesh.RefreshAndUpdateUvs();   
    }

    public void MultiSelect()
    {
        if (Model == null)
            return;

        if (Input.IsMousePressed(MouseButton.Left))
        {
            oldMousePos = Input.MousePosition;
        }

        if (Input.IsMouseDown(MouseButton.Left) && !blocked)
        {
            renderSelection = true;
            
            Vector2 mousePos = Input.MousePosition;
            Vector2 max = Mathf.Max(mousePos, oldMousePos);
            Vector2 min = Mathf.Min(mousePos, oldMousePos); 
            float distance = Vector2.Distance(mousePos, oldMousePos);
            bool regenColor = false;

            if (distance < 5)
                return;

            foreach (var (pos, data) in Uvs)
            {
                var position = pos * DrawingPanel.CanvasScale + _windowPosition + DrawingPanel.CanvasPosition;
                if (position.X >= min.X && position.X <= max.X && position.Y >= min.Y && position.Y <= max.Y)
                {
                    foreach (var uv in data.Uvs)
                    {
                        if (!SelectedUvs.Contains(uv))
                        {
                            regenColor = true;
                            SelectedUvs.Add(uv);
                            Model.SelectedVertices.Add(uv.Vertex);
                        }
                    }      
                }
                else if (!Input.IsKeyDown(Key.ShiftLeft))
                { 
                    foreach (var uv in data.Uvs)
                    {
                        if (SelectedUvs.Contains(uv))
                        {
                            regenColor = true;
                            SelectedUvs.Remove(uv);
                            Model.SelectedVertices.Remove(uv.Vertex);
                        }
                    } 
                }
            }

            if (regenColor)
            {
                IncludedSelectedTriangles = [];
                for (int i = 0; i < SelectedUvs.Count; i++)
                {
                    var uv = SelectedUvs[i];
                    IncludedSelectedTriangles.Add((uv.ParentTriangle, UvMesh.TriangleList.IndexOf(uv.ParentTriangle)));
                }

                GenerateVertexColor();
                Model?.GenerateVertexColor();
            }
        }

        if (Input.IsMouseReleased(MouseButton.Left))
        {
            renderSelection = false;
            oldMousePos = Vector2.Zero;
        }
    }

    public void ScalingInit()
    {
        if (Model == null)
            return;

        _currentScale = 1;

        Vector2 center = Vector2.Zero;
        if (SelectedUvs.Count == 0)
            selectedCenter = center;

        foreach (var vert in SelectedUvs)
        {
            center += vert;
        }
        selectedCenter =  center / SelectedUvs.Count;

        _currentScalePositions = [];
        foreach( var vert in SelectedUvs)
        {
            _currentScalePositions.Add(vert);
        }
    }

    public void Handle_PixelMapping(float pixelSize)
    {
        //Debug.Print("=== Handle_Mapping Started ===");

        if (Model == null)
        {
            //Debug.Print("Model is null, returning early");
            return;
        }

        if (Model.SelectedVertices.Count == 0)
            return;

        var triangles = Model.GetFullSelectedTriangles(Model.SelectedVertices);
        if (triangles.Count == 0)
            return;

        Vector3 factor = (1f / (float)DrawingPanel.Width / pixelSize, 1, 1f / (float)DrawingPanel.Height / pixelSize); // Skip 1 pixel between regions
    
        List<BoundingBoxRegion> boundingBoxes = [];

        Vector3 offset = (0, 0, 0);

        Vector3 min = Vector3.Zero;
        Vector3 max = Vector3.Zero;

        List<(Triangle original, Triangle copy)> copiedTriangles = [];
        List<Vertex> copiedVertices = [];

        List<(Vector3, Vector3, List<Vertex>)> minMax = [];
        float approximateVolume = 0;

        // Basic flattening and packing
        int regionCount = 0;
        while (triangles.Count > 0)
        {
            regionCount++;

            // Get a region of triangles from a mesh to flatten out
            var tris = triangles.First();
            var selectedTriangles = tris.GetTriangleRegion([], triangles);
            triangles.RemoveWhere(selectedTriangles.Contains);

            Dictionary<Vertex, Vertex> copyVertex = [];
            Dictionary<Edge, Edge> copyEdge = [];
            List<Triangle> triangleCopies = [];

            foreach (var t in selectedTriangles)
            {
                void EnsureVertex(Vertex v) 
                {
                    if (copyVertex.TryAdd(v, v.Copy()))
                        copiedVertices.Add(copyVertex[v]);
                }

                void EnsureEdge(Edge e, Vertex v1, Vertex v2)
                {
                    if (copyEdge.ContainsKey(e)) 
                        return;

                    EnsureVertex(v1);
                    EnsureVertex(v2);
                    copyEdge[e] = new(copyVertex[v1], copyVertex[v2]);
                }

                // Ensure edges and vertices exist
                EnsureEdge(t.AB, t.A, t.B);
                EnsureEdge(t.BC, t.B, t.C);
                EnsureEdge(t.CA, t.C, t.A);

                // Create the copied triangle
                var newT = new Triangle(
                    copyVertex[t.A], 
                    copyVertex[t.B], 
                    copyVertex[t.C], 
                    copyEdge[t.AB], 
                    copyEdge[t.BC], 
                    copyEdge[t.CA]
                );

                triangleCopies.Add(newT);
                copiedTriangles.Add((t, newT));
            }
            Model.Handle_Flattening(triangleCopies);

            // if any vertices are present:
            // 1. Flip the region if the normal of the first triangle is facing down
            // 2. Get the smallest possible bounding box of the selected region and rotate it if needed before moving it next to the last one
            var newVerts = Model.GetVertices(triangleCopies);
            if (newVerts.Count != 0)
            {
                // Flip the region if the normal of the first triangle is facing down
                if (triangleCopies.Count > 0)
                {
                    Triangle first = triangleCopies[0];
                    first.UpdateNormal();

                    if (Vector3.Dot(first.Normal, (0, 1, 0)) < 0 && newVerts.Count > 0)
                    {
                        Vector3 center = Vector3.Zero;
                        foreach (var vert in newVerts)
                        {
                            center += vert;
                        }
                        center /= newVerts.Count;

                        foreach (var vert in newVerts)
                        {
                            vert.SetPosition(Mathf.RotatePoint(vert, center, (1, 0, 0), 180f));
                        }
                    }
                }

                foreach (var vert in newVerts)
                {
                    vert.Position *= factor;
                }

                // Get the smallest possible bounding box of the selected region and rotate it if needed before moving it next to the last one
                Mathf.GetSmallestBoundingBox(newVerts, out min, out max);

                minMax.Add((min, max, [.. newVerts]));

                Vector2 size = max.Xz - min.Xz;
                float volume = size.X * size.Y;
                approximateVolume += volume;
            }
        }

        double sideLength = Math.Sqrt(approximateVolume);
        Vector2 skip = (1f / (float)DrawingPanel.Width, 1f / (float)DrawingPanel.Height); // Skip 1 pixel between regions

        int vertexMoveCount = 0;
        foreach (var (Min, Max, vertices) in minMax)
        {
            Vector3 size = Max - Min;
            Vector3 vOffset = (offset.X - Min.X, 0, -Min.Z);

            BoundingBoxRegion region = new BoundingBoxRegion(Min + vOffset, Max + vOffset, vertices);
            boundingBoxes.Add(region);

            foreach (var vert in vertices)
            {
                vert.SetPosition(vert + vOffset);
                vertexMoveCount++;
            }

            offset.X += size.X + skip.X;
        }

        // Better packing algorithm

        // Packing algorithm
        bool packed = false;
        int packingIteration = 0;
        while (!packed)
        {
            packingIteration++;

            bool needsPacking = false;

            // Get the last region and remove it from the list so we can test it against the others
            BoundingBoxRegion last = boundingBoxes[^1];
            boundingBoxes.RemoveAt(boundingBoxes.Count - 1);
            Vector3 lastSize = last.Size;

            Vector3 bestMin = Vector3.Zero;
            int bestIndex = 0;
            bool foundAtLeastOne = false;

            for (int i = 0; i < boundingBoxes.Count; i++)
            {
                min = boundingBoxes[i].Min;
                max = boundingBoxes[i].Max;

                if (min.X >= sideLength) // If the region is too far to the right, skip it
                {
                    needsPacking = true;
                    continue;
                }

                Vector3 testMinLeft = (min.X, 0, max.Z + skip.Y); // Tesing the region above the current one on the left side
                Vector3 testMinRight = (max.X - lastSize.X, 0, max.Z + skip.Y); // Tesing the region above the current one on the right side

                bool collidingLeft = false;
                for (int j = 0; j < boundingBoxes.Count; j++)
                {
                    if (i == j)
                        continue;

                    last.SetMin(testMinLeft);
                    var bB = boundingBoxes[j];

                    if (last & bB)
                    {
                        collidingLeft = true;
                        break;
                    }
                }

                if (!collidingLeft)
                {
                    bestIndex = i;

                    if (!foundAtLeastOne || testMinLeft.Z < bestMin.Z) // Only set the best index if it is the first one or if it is smaller than the current best
                    {
                        bestMin = testMinLeft;
                        foundAtLeastOne = true;
                    }
                    continue;
                }

                if (max.X < sideLength) // Only test the right side if the region is not too far to the right
                {
                    bool collidingRight = false;
                    for (int j = 0; j < boundingBoxes.Count; j++)
                    {
                        if (i == j)
                            continue;

                        last.SetMin(testMinRight);
                        var bB = boundingBoxes[j];

                        if (last & bB)
                        {
                            collidingRight = true;
                            break;
                        }
                    }

                    if (!collidingRight)
                    {
                        bestIndex = i;
                        if (!foundAtLeastOne || testMinRight.Z < bestMin.Z)
                        {
                            bestMin = testMinRight;
                            foundAtLeastOne = true;
                        }
                        continue;
                    }
                }
            }

            last.SetMin(bestMin);
            boundingBoxes.Insert(bestIndex, last);

            if (!needsPacking)
            {
                packed = true;
                break;
            }
        }

        // Move the regions to their new positions
        foreach (var region in boundingBoxes)
        {
            Vector3 o = region.Min - region.OriginalMin;
            foreach (var vert in region.Vertices)
            {
                vert.SetPosition(vert + o);
            }
        }

        // Get the current bounding box
        min = (float.MaxValue, float.MaxValue, float.MaxValue);
        max = (float.MinValue, float.MinValue, float.MinValue);

        foreach (var vert in copiedVertices)
        {
            min = Mathf.Min(min, vert);
            max = Mathf.Max(max, vert);
        }

        // Move the mesh to 0,0,0
        foreach (var vert in copiedVertices)
        {
            vert.SetPosition(vert - min);
        }

        Vector2 uvMin = Vector2.One;
        Vector2 uvMax = Vector2.Zero;

        for (int i = 0; i < copiedTriangles.Count; i++)
        {
            var (original, copy) = copiedTriangles[i];

            Vector2 uvA = (copy.A.X, copy.A.Z);
            Vector2 uvB = (copy.B.X, copy.B.Z);
            Vector2 uvC = (copy.C.X, copy.C.Z);

            uvMin = Mathf.Min(uvA, uvB, uvC, uvMin);
            uvMax = Mathf.Max(uvA, uvB, uvC, uvMax);

            original.UvA = uvA;
            original.UvB = uvB;
            original.UvC = uvC;
        }


        Model?.Mesh.CheckUselessVertices();
        Model?.Mesh.UpdateAndRegenerateAll();

        LoadUvs();
        UpdatePositionData();
    }

    public void Handle_ScalingSelectedVertices()
    {
        if (Model == null || SelectedUvs.Count < 2)
            return;

        float mouseDelta = Input.GetMouseDelta().X * 0.001f;
        _currentScale += mouseDelta;

        if (ModelSettings.Snapping)
        {
            _currentScale = Mathf.Round(_currentScale / ModelSettings.SnappingFactor) * ModelSettings.SnappingFactor;
        }

        int i = 0;
        foreach( var vert in SelectedUvs)
        { 
            Vector2 direction = _currentScalePositions[i] - selectedCenter; // store original positions before scaling starts
            Vector2 newPosition = selectedCenter + direction * _currentScale;

            if (ModelSettings.Axis.X == 0)
                newPosition.X = vert.Value.X;
            if (ModelSettings.Axis.Y == 0)
                newPosition.Y = vert.Value.Y;

            vert.Set(newPosition);
            i++;
        }

        UvMesh.Update();
    }

    public void GenerateVertexColor()
    {
        for (int i = 0; i < UvMesh.UvList.Count; i++)
        {
            Uv uv = UvMesh.UvList[i];
            uv.Color = SelectedUvs.Contains(uv) ? (0.25f, 0.3f, 1) : (0f, 0f, 0f);

            var vertexData = UvMesh.Vertices[i];
            vertexData.Color = new Vector4(uv.Color.X, uv.Color.Y, uv.Color.Z, 1);
            UvMesh.Vertices[i] = vertexData;
        }

        for (int i = 0; i < UvMesh.EdgeList.Count; i++)
        {
            var edge = UvMesh.EdgeList[i];
            if (UvMesh.EdgeColors.Count > i*2)
                UvMesh.EdgeColors[i*2] = edge.A.Color;

            if (UvMesh.EdgeColors.Count > i*2 + 1)
                UvMesh.EdgeColors[i*2 + 1] = edge.B.Color;
        }

        UvMesh.UpdateVertexColors();
        UvMesh.UpdateEdgeColors();
    }

    public void UpdatePositionData()
    {
        Uvs.Clear();
        foreach (var triangle in UvMesh.TriangleList)
        {
            if (triangle.Hidden)
                continue;

            foreach (var uv in triangle.GetUvs())
            {
                if (!Uvs.ContainsKey(uv))
                    Uvs.Add(uv, new(uv));
                else
                    Uvs[uv].Add(uv);
            }
        }
    }

    public void UvClickTest()
    {
        bool change = false;
        if (!Input.IsKeyDown(Key.ShiftLeft))
        {
            change = SelectedUvs.Count > 0;
            if (Model != null)
                Model.SelectedVertices = [];
                
            SelectedUvs = [];
        }
        
        Vector2 mousePos = Input.GetMousePosition();
        Vector2? closest = null;
        HashSet<Uv>? closestVerts = null;
    
        foreach (var (vert, data) in Uvs)
        {
            Vector2 position = vert * DrawingPanel.CanvasScale + _windowPosition + DrawingPanel.CanvasPosition;
            float distance = Vector2.Distance(mousePos, position);
            float distanceClosest = closest == null ? 1000 : Vector2.Distance(mousePos, (Vector2)closest);
        
            if (distance < distanceClosest && distance < 10)
            {
                closest = position;
                closestVerts = data.Uvs;
            }
        }

        if (closestVerts != null)
        {
            change = true;
            foreach (var uv in closestVerts)
            {
                if (!SelectedUvs.Remove(uv))
                {
                    SelectedUvs.Add(uv);
                    Model?.SelectedVertices.Add(uv.Vertex);
                }
                else
                {
                    Model?.SelectedVertices.Remove(uv.Vertex);
                }
            }
        }

        if (change)
        {
            IncludedSelectedTriangles = [];
            for (int i = 0; i < SelectedUvs.Count; i++)
            {
                var uv = SelectedUvs[i];
                IncludedSelectedTriangles.Add((uv.ParentTriangle, UvMesh.TriangleList.IndexOf(uv.ParentTriangle)));
            }

            GenerateVertexColor();
            Model?.GenerateVertexColor();
        }   
    }

    public void VertexClickTest()
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
            float distance = Vector2.Distance(mousePos, vertex.Screen);
            float distanceClosest = closest == null ? 1000 : Vector2.Distance(mousePos, (Vector2)closest);

            if (distance < distanceClosest && distance < 10)
            {
                closest = vertex.Screen;
                closestVert = vertex;
            }
        }

        if (closestVert != null && !Model.SelectedVertices.Remove(closestVert))
            Model.SelectedVertices.Add(closestVert);

        Model.GenerateVertexColor();
    }

    public void Handle_VertexMovement()
    {
        Vector2 mouseDelta = Input.GetMouseDelta();
        if (mouseDelta != Vector2.Zero)
        {
            var move = GetSnappingMovement();
            MoveSelectedVertices(move, SelectedUvs);

            UvMesh.Update();
        }
    }

    public Vector2 GetSnappingMovement()
    {
        Vector2 mouseDelta = Input.MouseDelta / (DrawingPanel.Width, DrawingPanel.Height) * (1 / DrawingPanel.DrawingCanvasSize);
        Vector2 move = (mouseDelta.X, mouseDelta.Y);

        move *= ModelSettings.Axis.Xy;
        if (move.Length == 0) return Vector2.Zero;

        if (ModelSettings.Snapping)
        {
            Vector2 Offset = Vector2.Zero;

            Vector2 snappingOffset = ModelSettings.SnappingOffset.Xy;
            float snappingFactor = ModelSettings.SnappingFactor;

            snappingOffset += move;
            while (snappingOffset.X > snappingFactor)
            {
                Offset.X += snappingFactor;
                snappingOffset.X -= snappingFactor;
            }
            while (snappingOffset.X < -snappingFactor)
            {
                Offset.X += -snappingFactor;
                snappingOffset.X += snappingFactor;
            }
            while (snappingOffset.Y > snappingFactor)
            {
                Offset.Y += snappingFactor;
                snappingOffset.Y -= snappingFactor;
            }
            while (snappingOffset.Y < -snappingFactor)
            {
                Offset.Y += -snappingFactor;
                snappingOffset.Y += snappingFactor;
            }

            ModelSettings.SnappingOffset = (snappingOffset.X, snappingOffset.Y, 0);

            move = Offset;
        }

        return move;
    }
    
    public static void MoveSelectedVertices(Vector2 move, List<Uv> selectedVertices)
    {
        for (int i = 0; i < selectedVertices.Count; i++)
        {
            var vert = selectedVertices[i];
            if (ModelSettings.GridAligned && ModelSettings.Snapping)
                vert.SnapPosition(move, ModelSettings.SnappingFactor);
            else
                vert.MovePosition(move);
        }
    }

    public void Handle_VertexRotation()
    {
        Vector2 mouseDelta = Input.GetMouseDelta();
        if (mouseDelta.X != 0 && SelectedUvs.Count > 0)
        {
            Vector2 center = Vector2.Zero;
            for (int i = 0; i < SelectedUvs.Count; i++)
            {
                Uv uv = SelectedUvs[i];
                center += uv.Value;
            }
            center /= SelectedUvs.Count;

            for (int i = 0; i < SelectedUvs.Count; i++)
            {
                Uv uv = SelectedUvs[i];
                uv.Set(Mathf.RotateAround((uv.Value.X, 0, uv.Value.Y), (center.X, 0, center.Y), (0, 1, 0), -mouseDelta.X).Xz);
            }

            UvMesh.Update();
        }
    }
}