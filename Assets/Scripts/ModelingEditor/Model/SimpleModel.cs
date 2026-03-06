using PBG;
using PBG.Core;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.UI.Creator;

public class SimpleModel
{   
    // Shader info
    /*
    private static ShaderProgram _animationShader = new ShaderProgram("model/modelAnimation.vert", "model/model.frag");
    private static int _animationModelLocation = _animationShader.GetLocation("model");
    private static int _animationViewLocation = _animationShader.GetLocation("view");
    private static int _animationProjectionLocation = _animationShader.GetLocation("projection");
    private static int _animationTextureLocation = _animationShader.GetLocation("texture0");
    private static int _animationColorAlphaLocation = _animationShader.GetLocation("colorAlpha");
    */

    public static ModelCopy randomCopy = new();
    public static ModelCopy Copy = new();


    public string Name = "Model";
    public bool IsShown = true;
    public bool IsSelected = false;

    public TextureType TextureType = TextureType.Linear;
    public string TextureFilePath = "empty.png";
    //public TextureLocation TextureLocation = TextureLocation.NormalTexture;

    //public Texture Texture = new Texture(Path.Combine(Game.TexturePath, "empty.png"), TextureLocation.NormalTexture);


    public HashSet<Vertex> SelectedVertices = new();
    public HashSet<Edge> SelectedEdges = new();
    public HashSet<Triangle> SelectedTriangles = new();
    public List<(Vertex vertex, Vector2 position, int index)> Vertices = [];
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

    public SimpleModelMesh Mesh;
    public Rig? Rig;
    public Dictionary<string, NormalizedAnimation> Animations = [];

    private Queue<(NormalizedAnimation animation, Action playType)> _animationQueue = new();
    public NormalizedAnimation? Animation
    {
        get => _animation;
        set
        {
            _animation?.Reset();
            _animation = value;   
        }
    }
    private NormalizedAnimation? _animation;

    public bool Animate = false;

    private Camera _camera => Scene.CurrentScene!.DefaultCamera;

    public Vector3 VertexPositionMin;
    public Vector3 VertexPositionMax;

    public SimpleModel(string modelPath)
    {
        if (!File.Exists(modelPath))
            throw new FileNotFoundException("[Error] : Model at path: '" + modelPath + "' was not found");

        Mesh = new SimpleModelMesh(this);
        Rig = new("Base");

        ModelLoader.LoadModel(this, modelPath, out var info);

        Name = Path.GetFileNameWithoutExtension(modelPath);

        VertexPositionMin = info.Min;
        VertexPositionMax = info.Max;
    }

    public void UpdateMatrices()
    {
        if (Rig == null)
            return;

        BoneMatricesList.Clear();
        foreach (var bone in Rig.BonesList)
        {
            BoneMatricesList.Add(bone.GlobalAnimatedMatrix);
        }
        //BoneMatrices.Update(BoneMatricesList, 0);
    }

    public float GetAnimationTime() => Animation?.GetTime() ?? 0f;

    public Action BindTexture;
    public Action UnbindTexture;

    public void Renew() => Renew(TextureFilePath);
    public void Renew(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        TextureFilePath = filePath;
        //TextureLocation = TextureLocation.NormalTexture;
        //Texture.Renew(filePath, TextureLocation);

        //BindTexture = () => Texture.Bind(TextureUnit.Texture0);
        //UnbindTexture = () => Texture.Unbind();
    }

    public bool PlayAnimation(string name)
    {
        if (!Animations.TryGetValue(name, out var animation))
        {
            Animation = null;
            Animate = false;
            return PlayNextQueuedAnimation();
        }
        else
        {
            _animationQueue.Clear(); // Clear the queue when playing a new animation
            Animation = animation;
            Animation.SetAsOnce();
            Animate = true;
            return true;
        }
    }

    public bool LoopAnimation(string name)
    {
        if (!Animations.TryGetValue(name, out var animation))
        {
            Animation = null;
            Animate = false;
            return PlayNextQueuedAnimation();
        }
        else
        {
            _animationQueue.Clear(); // Clear the queue when playing a new animation
            Animation = animation;
            Animation.SetAsLooping();
            Animate = true;
            return true;
        }
    }

    public bool QueuePlayAnimation(string name)
    {
        if (!Animations.TryGetValue(name, out var animation))
        {
            return false;
        }
        else if (Animation == null)
        {
            Animation = animation;
            Animation.SetAsOnce();
            Animate = true;
            return true;
        }
        else
        {
            _animationQueue.Enqueue((animation, () => Animation?.SetAsOnce()));
            return true;
        }
    }

    public bool QueueLoopAnimation(string name)
    {
        if (!Animations.TryGetValue(name, out var animation))
        {
            return false;
        }
        else if (Animation == null)
        {
            Animation = animation;
            Animation.SetAsLooping();
            Animate = true;
            return true;
        }
        else
        {
            _animationQueue.Enqueue((animation, () => Animation?.SetAsLooping()));
            return true;
        }
    }

    public bool PlayNextQueuedAnimation()
    {
        if (_animationQueue.Count == 0)
            return false;

        var (animation, playType) = _animationQueue.Dequeue();
        Animation = animation;
        playType();
        Animate = true;
        return true;
    }

    public void StopAnimation()
    {
        Animation = null;
        Animate = false;
    }

    public void Reload()
    {
        //Texture.Renew(TextureFilePath, TextureLocation);
    }

    public void Update()
    {
        Handle_Animation();
    }

    private void Handle_Animation()
    {
        if (!Animate || Animation == null || Rig == null)
            return;

        Animation.Update();
        if (Animation.IsOnce && Animation.IsDone && !PlayNextQueuedAnimation())
        {
            Animate = false;
            return;
        }   

        for (int i = 0; i < Rig.BonesList.Count; i++)
        {
            var bone = Rig.BonesList[i];
            var frame = Animation.GetBoneKeyframe(bone.Index);
            if (frame == null)
                continue;

            bone.Set(frame.Position, frame.Rotation, frame.Scale);
        }

        Rig.RootBone.UpdateGlobalTransformation();

        for (int i = 0; i < Rig.BonesList.Count; i++)
        {
            var bone = Rig.BonesList[i];
            BoneMatricesList[bone.Index] = bone.GlobalAnimatedMatrix;
        }

        //BoneMatrices.Update(BoneMatricesList, 0);
    }

    public void Render()
    {
        if (!IsShown)
            return;
            
        Matrix4 model = ModelMatrix;
        Matrix4 view = _camera.ViewMatrix;
        Matrix4 projection = _camera.ProjectionMatrix;

        Render(model, view, projection);
    }

    public void Render(Matrix4 model, Matrix4 view, Matrix4 projection)
    {
        /*
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);

        _animationShader.Bind();

        GL.UniformMatrix4(_animationModelLocation, false, ref model);
        GL.UniformMatrix4(_animationViewLocation, false, ref view);
        GL.UniformMatrix4(_animationProjectionLocation, false, ref projection);
        GL.Uniform1(_animationColorAlphaLocation, ModelSettings.MeshAlpha);
        GL.Uniform1(_animationTextureLocation, 0);

        Texture.Bind(TextureUnit.Texture0);
        BoneMatrices.Bind(0);

        Mesh.Render();
        Shader.Error("[Rendering] : Could not render simple model");

        BoneMatrices.Unbind();
        Texture.Unbind();

        _animationShader.Unbind();
        */
    }

    public void Delete()
    {
        Clear();
        Mesh.Delete();
        //BoneMatrices.DeleteBuffer();
        //Texture.DeleteBuffer();
    }

    public void Clear()
    {
        SelectedVertices = [];
        SelectedEdges = [];
        SelectedTriangles = [];
        Vertices = [];
        BonePivots = [];
        BoneMatricesList = [];
    }
}