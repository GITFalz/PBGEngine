using PBG;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Threads;
using PBG.UI;
using PBG.UI.Creator;

public class Model
{   
    /*
    // Shader info
    private static ShaderProgram _shaderProgram = new ShaderProgram("model/model.vert", "model/model.frag");
    private static int _modelLocation = _shaderProgram.GetLocation("model");
    private static int _viewLocation = _shaderProgram.GetLocation("view");
    private static int _projectionLocation = _shaderProgram.GetLocation("projection");
    private static int _mirrorLocation = _shaderProgram.GetLocation("mirror");
    private static int _colorAlphaLocation = _shaderProgram.GetLocation("colorAlpha");
    
    private static ShaderProgram _animationShader = new ShaderProgram("model/modelAnimation.vert", "model/model.frag");
    private static int _animationModelLocation = _animationShader.GetLocation("model");
    private static int _animationViewLocation = _animationShader.GetLocation("view");
    private static int _animationProjectionLocation = _animationShader.GetLocation("projection");
    private static int _animationMirrorLocation = _animationShader.GetLocation("mirror");
    private static int _animationColorAlphaLocation = _animationShader.GetLocation("colorAlpha");
    */

    public static ModelCopy randomCopy = new();
    public static ModelCopy Copy = new();


    public string Name = "Model";
    public bool IsShown = true;
    public bool IsSelected = false;
    public bool ShowWireframe = true;

    public string? TextureFilePath = null;
    //public TextureLocation TextureLocation = TextureLocation.NormalTexture;

    /*
    private ShaderProgram _activeShader = _shaderProgram;
    private int _activeModelLocation = _modelLocation;
    private int _activeViewLocation = _viewLocation;
    private int _activeProjectionLocation = _projectionLocation;
    private int _activeMirrorLocation = _mirrorLocation;
    private int _activeColorAlphaLocation = _colorAlphaLocation;
    public Texture Texture = new Texture(Path.Combine(Game.TexturePath, "empty.png"), TextureLocation.NormalTexture);
    */


    public HashSet<Vertex> SelectedVertices = new();
    public HashSet<Edge> SelectedEdges = new();
    public HashSet<Triangle> SelectedTriangles = new();
    public List<(Triangle triangle, Vector2 position, int index)> Triangles = [];
    public Dictionary<BonePivot, (Vector2, float)> BonePivots = [];


    //public SSBO<Matrix4> BoneMatrices = new SSBO<Matrix4>();
    public List<Matrix4> BoneMatricesList = [];

    public Vector3 Position
    {
        get => _position;
        set
        {
            _position = value;
            ModelMatrix = Matrix4.CreateFromQuaternion(_rotation) * Matrix4.CreateTranslation(_position);
        }
    }
    private Vector3 _position = Vector3.Zero;

    public Quaternion Rotation
    {
        get => _rotation;
        set
        {
            _rotation = value;
            ModelMatrix = Matrix4.CreateFromQuaternion(_rotation) * Matrix4.CreateTranslation(_position);
        }
    }
    private Quaternion _rotation = Quaternion.Identity;
    public Matrix4 ModelMatrix = Matrix4.Identity;

    public ModelMesh Mesh;
    public Rig? Rig;
    public Rig? AnimationRig;
    private int _animationId = 0;
    public Dictionary<int, NewAnimation> Animations = [];
    public NewAnimation? Animation;
    private NormalizedAnimation? _normalizedAnimation;

    public bool AnimationState = false;

    public bool RenderBones = false;
    public bool Animate
    {
        get => _animate;
        set
        {
            _animate = value;
            if (value && AnimationRig != null && Animation != null)
                _normalizedAnimation = new NormalizedAnimation(AnimationRig, Animation);
        }
    }
    private bool _animate = false;

    public GeneralModelingEditor Editor;

    public Model(GeneralModelingEditor editor)
    {
        Editor = editor;
        Mesh = new ModelMesh(this);
        Rig = new("Base");

        Rig.Create();
        Rig.Initialize();
        Rig.RootBone.UpdateGlobalTransformation();

        BoneMatricesList.Clear();
        foreach (var bone in Rig.BonesList)
        {
            BoneMatricesList.Add(bone.GlobalAnimatedMatrix);
        }
        //BoneMatrices.Renew(BoneMatricesList);

        Mesh.InitRig();

        //BindTexture = () => Texture.Bind(TextureUnit.Texture0);
        //UnbindTexture = () => Texture.Unbind();
    }

    public void AddAnimation(out int id, out NewAnimation animation)
    {
        id = _animationId;
        animation = new("new", id);
        Animations.Add(id, animation);
        Animation = animation;
        _animationId++;
    }

    public NewAnimation AddAnimation(string name) => AddAnimation(out _, name);
    public NewAnimation AddAnimation(out int id, string name)
    {
        id = _animationId;
        NewAnimation animation = new(name, id);
        Animations.Add(id, animation);
        Animation = animation;
        _animationId++;
        return animation;
    }

    public bool DeleteAnimation(int id) => Animations.Remove(id);

    public void InitRig()
    {
        if (Rig == null)
            return;

        Rig.Create();
        Rig.Initialize();

        BoneMatricesList.Clear();
        foreach (var bone in Rig.BonesList)
        {
            BoneMatricesList.Add(bone.GlobalAnimatedMatrix);
        }
        //BoneMatrices.Renew(BoneMatricesList);

        Mesh.InitRig();
    }

    public void UpdateMatrices(Rig rig)
    {
        BoneMatricesList.Clear();
        foreach (var bone in rig.BonesList)
        {
            BoneMatricesList.Add(bone.GlobalAnimatedMatrix);
        }
        //BoneMatrices.Update(BoneMatricesList, 0);
    }

    public void UpdateRig()
    {
        if (AnimationState)
        {
            if (AnimationRig != null)
            {
                UpdateMatrices(AnimationRig);
                Mesh.UpdateRig(AnimationRig);
            }
        }
        else
        {
            if (Rig != null)
            {
                UpdateMatrices(Rig);
                Mesh.UpdateRig(Rig);
            }
        }
    }

    public void SetModeling()
    {
        /*
        _activeShader = _shaderProgram;
        _activeModelLocation = _modelLocation;
        _activeViewLocation = _viewLocation;
        _activeProjectionLocation = _projectionLocation;
        _activeMirrorLocation = _mirrorLocation;
        _activeColorAlphaLocation = _colorAlphaLocation;
        */
        Mesh.Init();
        Mesh.UpdateModel();
    }

    public void SetAnimation()
    {
        /*
        _activeShader = _animationShader;
        _activeModelLocation = _animationModelLocation;
        _activeViewLocation = _animationViewLocation;
        _activeProjectionLocation = _animationProjectionLocation;
        _activeMirrorLocation = _animationMirrorLocation;
        _activeColorAlphaLocation = _animationColorAlphaLocation;
        */
        if (AnimationRig != null)
        {
            Mesh.Bind(AnimationRig);
            Mesh.UpdateModel();
        }
    }

    public void SetStaticRig()
    {
        if (Rig != null)
        {
            //BoneMatrices.Renew(Rig.GetGlobalAnimatedMatrices());
            Mesh.InitRig();
        }
    }

    public void SetAnimationRig()
    {
        if (Rig != null)
        {
            AnimationRig?.Delete();
            AnimationRig = Rig.Copy();
        }
    }

    public void SetAnimationFrame(int index)
    {
        if (Animation == null || AnimationRig == null)
            return;

        foreach (var bone in AnimationRig.BonesList)
        {
            var frame = Animation.GetSpecificFrame(bone.Name, index);
            if (frame == null)
                continue;

            bone.Position = frame.Position;
            bone.Rotation = frame.Rotation;
            bone.Scale = frame.Scale;
            bone.LocalAnimatedMatrix = frame.GetLocalTransform();
        }

        AnimationRig.RootBone.UpdateGlobalTransformation();

        foreach (var bone in AnimationRig.BonesList)
        {
            BoneMatricesList[bone.Index] = bone.GlobalAnimatedMatrix;
        }

        Mesh.UpdateRig(AnimationRig);

        //BoneMatrices.Update(BoneMatricesList, 0);
    }

    public float GetAnimationTime() => _normalizedAnimation?.GetTime() ?? 0f;

    public Action BindTexture;
    public Action UnbindTexture;

    public void Renew()
    {
        if (TextureFilePath != null)
            Renew(TextureFilePath);
    }

    public void Renew(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        TextureFilePath = filePath;
        //TextureLocation = TextureLocation.NormalTexture;
        //Texture.Renew(filePath, TextureLocation, TextureType.Nearest);

        //BindTexture = () => Texture.Bind(TextureUnit.Texture0);
        //UnbindTexture = () => Texture.Unbind();
    }

    public void Update()
    {
        if (AnimationRig == null || _normalizedAnimation == null)
            return;

        if (Animate)
        {
            _normalizedAnimation.Update();
            for (int i = 0; i < AnimationRig.BonesList.Count; i++)
            {
                var bone = AnimationRig.BonesList[i];
                var frame = _normalizedAnimation.GetBoneKeyframe(bone.Index);
                if (frame == null)
                    continue;

                bone.Set(frame.Position, frame.Rotation, frame.Scale);
            }

            AnimationRig.RootBone.UpdateGlobalTransformation();

            for (int i = 0; i < AnimationRig.BonesList.Count; i++)
            {
                var bone = AnimationRig.BonesList[i];
                BoneMatricesList[bone.Index] = bone.GlobalAnimatedMatrix;
            }

            Mesh.UpdateRig(AnimationRig);
            //BoneMatrices.Update(BoneMatricesList, 0);
        }
    }

    public void Render()
    {
        RenderMirror();

        if (Rig != null && RenderBones && IsSelected)
        {
            Mesh.RenderBones(ModelMatrix);
        }
    }

    public void RenderMirror() => RenderMirror(Editor.ProjectionMatrix);
    public void RenderMirror(Matrix4 projection)
    {
        /*
        var camera = Editor.Scene.DefaultCamera;

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);

        _activeShader.Bind();

        Matrix4 model = ModelMatrix;
        Matrix4 view = camera.ViewMatrix;

        if (IsSelected && ModelSettings.Mirrors.Length > 1)
        {
            var mirrors = ModelSettings.Mirrors;
            for (int i = 0; i < mirrors.Length; i++)
            {
                Vector3 flip = mirrors[i];
                GL.CullFace(ModelSettings.BackfaceCulling ? (i % 2 == 0 ? TriangleFace.Back : TriangleFace.Front) : TriangleFace.FrontAndBack);

                model = Matrix4.CreateScale(flip) * ModelMatrix;

                Render(model, view, projection, flip);
            }
        }
        else
        {
            Render(model, view, projection, (1, 1, 1));
        }

        _activeShader.Unbind();

        GL.CullFace(TriangleFace.Back);
        */
    }

    private void Render(Matrix4 model, Matrix4 view, Matrix4 projection, Vector3 flip)
    {
        /*
        GL.UniformMatrix4(_activeModelLocation, false, ref model);
        GL.UniformMatrix4(_activeViewLocation, false, ref view);
        GL.UniformMatrix4(_activeProjectionLocation, false, ref projection);
        GL.Uniform3(_activeMirrorLocation, flip);
        GL.Uniform1(_activeColorAlphaLocation, ModelSettings.MeshAlpha);

        BindTexture();

        if (Rig != null)
        {
            BoneMatrices.Bind(0);
            Mesh.Render();
            BoneMatrices.Unbind();
        }
        else
        {
            Mesh.Render();
        }

        UnbindTexture();
        */
    }

    public void RenderWireframe() => RenderWireframe(Editor.ProjectionMatrix);
    public void RenderWireframe(Matrix4 projection)
    {
        /*
        if (ModelSettings.WireframeVisible && IsSelected)
        {
            var camera = Editor.Scene.DefaultCamera;

            GL.DepthMask(false);
            GL.DepthFunc(DepthFunction.Always);

            Matrix4 model = ModelMatrix;
            Matrix4 view = camera.ViewMatrix;

            ModelSettings.EdgeShader.Bind();

            GL.UniformMatrix4(ModelSettings.edgeModelLocation, false, ref model);
            GL.UniformMatrix4(ModelSettings.edgeViewLocation, false, ref view);
            GL.UniformMatrix4(ModelSettings.edgeProjectionLocation, false, ref projection);

            BoneMatrices.Bind(0);

            Mesh.RenderEdges();

            BoneMatrices.Unbind();

            Shader.Error("Rendering edges error: ");

            ModelSettings.EdgeShader.Unbind();

            ModelSettings.VertexShader.Bind();

            GL.UniformMatrix4(ModelSettings.edgeModelLocation, false, ref model);
            GL.UniformMatrix4(ModelSettings.edgeViewLocation, false, ref view);
            GL.UniformMatrix4(ModelSettings.edgeProjectionLocation, false, ref projection);

            Mesh.RenderVertices();

            Shader.Error("Rendering vertices error: ");

            ModelSettings.VertexShader.Unbind();

            GL.Disable(EnableCap.DepthTest);
            GL.DepthMask(true);
            GL.DepthFunc(DepthFunction.Lequal);
        }
        */
    }

    
    public void UpdateVertexPosition() => UpdateVertexPosition(Editor.ProjectionMatrix, Editor.Scene.DefaultCamera.ViewMatrix, Game.Width - 400, Game.Height - 50, (200, 50));
    public void UpdateVertexPosition(Matrix4 projectionMatrix, Matrix4 viewMatrix, float width, float height, Vector2 position)
    {
        Triangles = [];

        System.Numerics.Matrix4x4 projection = projectionMatrix.num();
        System.Numerics.Matrix4x4 view = viewMatrix.num();

        if (ModelingEditingMode.selectionType == RenderType.Vertex)
        {
            for (int i = 0; i < Mesh.VertexList.Count; i++)
            {
                var vert = Mesh.VertexList[i];
                Vector3 vertPosition = (ModelMatrix.Transposed() * new Vector4(vert, 1.0f)).Xyz;
                Vector2? screenPos = Mathf.WorldToScreen(vertPosition, projection, view, width, height, out var clipW) + position;
                if (screenPos == null)
                {
                    vert.ClipW = -1;
                    continue;
                }
                    

                vert.Screen = screenPos.Value;
                vert.ClipW = clipW;
            }
        }
        if (ModelingEditingMode.selectionType == RenderType.Face)
        {
            for (int i = 0; i < Mesh.TriangleList.Count; i++)
            {
                var tris = Mesh.TriangleList[i];
                Vector3 vertPosition = (ModelMatrix.Transposed() * new Vector4(tris.CalculateCenter(), 1.0f)).Xyz;
                Vector2? screenPos = Mathf.WorldToScreen(vertPosition, projection, view, width, height) + position;
                if (screenPos == null)
                    continue;

                Triangles.Add((tris, screenPos.Value, i));
            }
        }
    }

    public void GenerateVertexColor()
    {
        if (ModelingEditingMode.selectionType == RenderType.Vertex)
        {
            for (int i = 0; i < Mesh.VertexList.Count; i++)
            {
                var vert = Mesh.VertexList[i];
                vert.Color = SelectedVertices.Contains(vert) ? (0.25f, 0.3f, 1) : (0f, 0f, 0f);

                if (Mesh.Vertices.Count <= i)
                    continue;
                    
                var vertexData = Mesh.Vertices[i];
                vertexData.Color = new Vector4(vert.Color.X, vert.Color.Y, vert.Color.Z, 1);
                Mesh.Vertices[i] = vertexData;
            }

            for (int i = 0; i < Mesh.EdgeList.Count; i++)
            {
                var edge = Mesh.EdgeList[i];
                int edgeIndex = i * 2;
                if (Mesh.EdgeColors.Count > edgeIndex)
                    Mesh.EdgeColors[edgeIndex] = edge.A.Color;

                if (Mesh.EdgeColors.Count > edgeIndex + 1)
                    Mesh.EdgeColors[edgeIndex + 1] = edge.B.Color;
            }
        }
        
        if (ModelingEditingMode.selectionType == RenderType.Face)
        {
            for (int i = 0; i < Mesh.TriangleList.Count; i++)
            {
                var triangle = Mesh.TriangleList[i];
                triangle.Color = SelectedTriangles.Contains(triangle) ? (0.25f, 0.3f, 1) : (0f, 0f, 0f);

                if (Mesh.Vertices.Count <= i)
                    continue;
                var vertexData = Mesh.Vertices[i];
                vertexData.Color = new Vector4(triangle.Color, 1);
                Mesh.Vertices[i] = vertexData;
            }

            for (int i = 0; i < Mesh.EdgeList.Count; i++)
            {
                var edge = Mesh.EdgeList[i];
                int edgeIndex = i * 2;
                if (Mesh.EdgeColors.Count > edgeIndex)
                    Mesh.EdgeColors[edgeIndex] = edge.A.Color;

                if (Mesh.EdgeColors.Count > edgeIndex + 1)
                    Mesh.EdgeColors[edgeIndex + 1] = edge.B.Color;
            }
        }

        Mesh.UpdateVertexColors();
        Mesh.UpdateEdgeColors();
    }

    public void SaveModel(string fileName)
    {
        if (fileName.Length == 0)
        {
            return;
        }

        string undoPath = Path.Combine(Game.UndoModelPath, fileName);
        if (!Directory.Exists(undoPath)) 
            Directory.CreateDirectory(undoPath);

        string path = Path.Combine(Game.ModelPath, fileName + ".model");
        SaveModelToPath(path);
    }

    public void SaveModelToPath(string path) => ModelLoader.SaveModel(this, path);

    public bool LoadModel(string fileName)
    {
        if (fileName.Length == 0)
        {
            return false;
        }

        string undoPath = Path.Combine(Game.UndoModelPath, fileName);
        if (!Directory.Exists(undoPath))
            Directory.CreateDirectory(undoPath);

        string path = Path.Combine(Game.ModelPath, fileName + ".model");
        return LoadModelFromPath(path);
    }

    public bool LoadModelFromPath(string path) => ModelLoader.LoadModel(this, path);

    public void Unload()
    {
        Mesh.Unload();
    }

    public void Delete()
    {
        ModelManager.UnSelect(this);
        Clear();
        Mesh.Delete();
        //BoneMatrices.DeleteBuffer();
        //Texture.DeleteBuffer();
        ModelManager.DeleteModel(this);
    }

    public void Clear()
    {
        SelectedVertices = [];
        SelectedEdges = [];
        SelectedTriangles = [];
        BonePivots = [];
        BoneMatricesList = [];
    }


    public void GetConnectedVertices()
    {
        if (SelectedVertices.Count == 0)
            return;

        HashSet<Vertex> copy = [.. SelectedVertices];
        SelectedVertices = [];

        foreach (var vert in copy)
        {
            vert.GetConnectedVertices(SelectedVertices, []);
        }

        GenerateVertexColor();
    }

    public void UpdateBonePosition(Rig? rig, Matrix4 projection, Matrix4 view)
    {
        if (rig == null)
            return;

        BonePivots = [];

        foreach (var (_, bone) in rig.Bones)
        {
            Vector3 pivot = (new Vector4(bone.Pivot.Get, 1f) * ModelMatrix).Xyz;
            Vector3 end = (new Vector4(bone.End.Get, 1f) * ModelMatrix).Xyz;

            Vector2? screenPos1 = Mathf.WorldToScreen(pivot, Mathf.Num(projection), Mathf.Num(view), Game.Width - 400, Game.Height - 50);
            Vector2? screenPos1Side = Mathf.WorldToScreen(pivot + Editor.Scene.DefaultCamera.right.Normalized() * 0.3f * 0.1f, Mathf.Num(projection), Mathf.Num(view), Game.Width - 400, Game.Height - 50);
            Vector2? screenPos2 = Mathf.WorldToScreen(end, Mathf.Num(projection), Mathf.Num(view), Game.Width - 400, Game.Height - 50);
            Vector2? screenPos2Side = Mathf.WorldToScreen(end + Editor.Scene.DefaultCamera.right.Normalized() * 0.2f * 0.1f, Mathf.Num(projection), Mathf.Num(view), Game.Width - 400, Game.Height - 50);

            if (screenPos1 != null && screenPos1Side != null)
            {
                float distance = Vector2.Distance(screenPos1.Value, screenPos1Side.Value);
                BonePivots.Add(bone.Pivot, (screenPos1.Value + (200, 50), distance));
                Vector2 size = new Vector2(distance, distance) * 2f;
            }


            if (screenPos2 != null && screenPos2Side != null)
            {
                float distance = Vector2.Distance(screenPos2.Value, screenPos2Side.Value);
                BonePivots.Add(bone.End, (screenPos2.Value + (200, 50), distance));
                Vector2 size = new Vector2(distance, distance) * 2f;
            }
        }
    }

    public static List<Edge> GetFullSelectedEdges(IEnumerable<Vertex> selectedVertices)
    {
        HashSet<Edge> edges = [];

        foreach (var vert in selectedVertices)
        {
            foreach (var edge in vert.ParentEdges)
            {
                if (selectedVertices.Contains(edge.Not(vert)))
                    edges.Add(edge);
            }
        }

        return edges.ToList();
    }

    public static HashSet<Triangle> GetFullSelectedTriangles(IEnumerable<Vertex> selectedVertices)
    {
        HashSet<Triangle> triangles = [];

        foreach (var triangle in GetSelectedTriangles(selectedVertices))
        {
            if (IsTriangleFullySelected(selectedVertices, triangle))
                triangles.Add(triangle);
        }

        return triangles;
    }

    public static HashSet<Triangle> GetSelectedTriangles(IEnumerable<Vertex> selectedVertices)
    {
        HashSet<Triangle> triangles = [];

        foreach (var vert in selectedVertices)
        {
            foreach (var triangle in vert.ParentTriangles)
            {
                triangles.Add(triangle);
            }
        }

        return triangles;
    }

    public static bool IsTriangleFullySelected(IEnumerable<Vertex> selectedVertices, Triangle triangle)
    {
        return selectedVertices.Contains(triangle.A) &&
               selectedVertices.Contains(triangle.B) &&
               selectedVertices.Contains(triangle.C);
    }

    public static HashSet<Vertex> GetVertices(IEnumerable<Triangle> triangles)
    {
        HashSet<Vertex> vertices = [];

        foreach (var triangle in triangles)
        {
            vertices.Add(triangle.A);
            vertices.Add(triangle.B);
            vertices.Add(triangle.C);
        }

        return vertices;
    }

    public static List<Edge> GetEdges(List<Triangle> triangles)
    {
        List<Edge> edges = [];

        foreach (var triangle in triangles)
        {
            if (!edges.Contains(triangle.AB))
                edges.Add(triangle.AB);

            if (!edges.Contains(triangle.BC))
                edges.Add(triangle.BC);

            if (!edges.Contains(triangle.CA))
                edges.Add(triangle.CA);
        }

        return edges;
    }

    public static Vector3 GetSelectedCenter(IEnumerable<Vertex> selectedVertices)
    {
        Vector3 center = Vector3.Zero;
        if (!selectedVertices.Any())
            return center;

        foreach (var vert in selectedVertices)
        {
            center += vert;
        }
        return center / selectedVertices.Count();
    }

    public static void MoveSelectedVertices(Model model, Vector3 move, IEnumerable<Vertex> selectedVertices)
    {
        /*
        bool rotate = model.Rotation != Quaternion.Identity;
        foreach (var vert in selectedVertices)
        {
            if (ModelSettings.GridAligned && ModelSettings.Snapping)
            {
                if (rotate)
                {
                    var position = Vector3.Transform(vert.Position, model.Rotation);
                    position += move;
                    position = Snap(position, ModelSettings.SnappingFactor);
                    vert.SetPosition(Vector3.Transform(position, model.Rotation.Inverted()));
                }
                else
                {
                    vert.SetPosition(Snap(vert.Position + move, ModelSettings.SnappingFactor));
                }
            }
            else
            {
                if (rotate)
                {
                    var position = Vector3.Transform(vert.Position, model.Rotation);
                    position += move;
                    vert.SetPosition(Vector3.Transform(position, model.Rotation.Inverted()));
                }
                else
                {
                    vert.SetPosition(vert.Position + move);
                }
            }
        }
        */
    }

    public static Vector3 Snap(Vector3 position, float snap)
    {
        return new Vector3(
            (float)Math.Round(position.X / snap) * snap,
            (float)Math.Round(position.Y / snap) * snap,
            (float)Math.Round(position.Z / snap) * snap
        );
    }

    public static void Handle_Flattening(List<Triangle> triangles)
    {
        if (triangles.Count == 0)
            return;

        Triangle first = triangles[0];

        Vector3 rotationAxis = Vector3.Cross(first.Normal, (0, 1, 0));

        if (rotationAxis.Length != 0)
        {
            float angle = Mathf.RadiansToDegrees(Vector3.CalculateAngle(first.Normal, (0, 1, 0)));
            Vector3 center = first.GetCenter();
            Vector3 rotatedNormal = Mathf.RotatePoint(first.Normal, Vector3.Zero, rotationAxis, angle);

            if (Vector3.Dot(rotatedNormal, (0, 1, 0)) < 0)
                angle += 180f;

            float minY = float.MaxValue;
            foreach (var vert in first.GetVertices())
            {
                Vector3 position = Mathf.RotatePoint(vert, center, rotationAxis, angle);
                minY = Mathf.Min(minY, position.Y);
            }

            var vertices = GetVertices(triangles);
            foreach (var vert in vertices)
            {
                vert.Position.Y -= minY;
                Vector3 position = Mathf.RotatePoint(vert, center, rotationAxis, angle);
                vert.SetPosition(position);
            }
        }

        first.FlattenRegion(triangles);
    }
}