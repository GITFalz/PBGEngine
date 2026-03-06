using PBG.MathLibrary;
using PBG;
using PBG.Core;
using PBG.Data;
using PBG.Graphics;
using PBG.Hash;
using PBG.Rendering;
using PBG.Threads;
using PBG.Voxel;
using Silk.NET.Input;

public class StructureMeshRenderer
{
    /*
    public static ShaderProgram gridShader = new ShaderProgram("StructureEditor/structureGrid.vert", "StructureEditor/structureGrid.frag");

    public static int gridModelLocation = gridShader.GetLocation("model");
    public static int gridViewLocation = gridShader.GetLocation("view");
    public static int gridProjectionLocation = gridShader.GetLocation("projection");
    public static int gridSizeLocation = gridShader.GetLocation("size");
    public static int gridCamPosLocation = gridShader.GetLocation("cameraPos");

    public static VAO gridVao = new VAO();
    */

    public ScriptingNode Parent = null!;
    public Camera Camera => Parent.Scene.DefaultCamera;

    public VoxelRenderer Renderer = null!;

    public bool Rendering = true;

    public StructureMeshRenderer(ScriptingNode parent)
    {
        Parent = parent;
    }

    public void Start()
    {
        Renderer = Parent.Scene.GetNode("Root/Structure/Editor").GetComponent<VoxelRenderer>();

        Camera.Position = new Vector3(0, 10, 20);
        Camera.Pitch = -25;
        Camera.Yaw = -90;
        Camera.SetCameraMode(CameraMode.Free);
        Camera.Unlock();
        Camera.Lock();
    }

    public void Awake()
    {
        Camera.Position = new Vector3(0, 10, 20);
        Camera.Pitch = -25;
        Camera.Yaw = -90;
        Camera.SetCameraMode(CameraMode.Free);
        Camera.Unlock();
        Camera.Lock();

        Game.SetCursorState(CursorMode.Normal);
    }

    public void Resize()
    {

    }

    public void Update()
    {
        if (StructureEngineManager.CanRotate)
        {
            if (Input.IsKeyPressed(Key.Escape))
            {
                StructureEngineManager.CanRotate = false;
                Game.SetCursorState(CursorMode.Normal);
                Camera.Lock();
            }
        }
    }

    public void Render()
    {
        /*
        if (!Rendering)
            return;
            
        GL.Viewport(240, 0, Game.Width - 480, Game.Height - 60);

        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        gridShader.Bind();

        Vector2 gridSize = new Vector2(1000, 1000);
        Matrix4 gridModel = Matrix4.CreateTranslation(new Vector3(-gridSize.X * 0.5f, -0.02f, -gridSize.Y * 0.5f) + new Vector3(Camera.Position.X, 0, Camera.Position.Z));
        Matrix4 view = Camera.ViewMatrix;
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Camera.FOV), (float)(Game.Width - 480) / (Game.Height - 60), 0.1f, 1000f);

        GL.UniformMatrix4(gridModelLocation, false, ref gridModel);
        GL.UniformMatrix4(gridViewLocation, false, ref view);
        GL.UniformMatrix4(gridProjectionLocation, false, ref projection);
        GL.Uniform2(gridSizeLocation, ref gridSize);
        GL.Uniform3(gridCamPosLocation, new Vector3(Camera.Position.X, 0, Camera.Position.Z));

        gridVao.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        Shader.Error("Structure shader error: ");

        gridVao.Unbind();
        gridShader.Unbind();

        GL.Viewport(0, 0, Game.Width, Game.Height);
        */
    }

    public void Exit()
    {
        Game.SetCursorState(CursorMode.Normal);
    }

    public void Dispose()
    {
        //gridShader.DeleteBuffer();
        //gridVao.DeleteBuffer();
    }

    public void UpdateValue(int index, float value)
    {
        //ReloadMesh();
    }
}

public class TreeGenerationInfo
{   
    // Bounds
    public int MinX = 0;
    public int MinY = 0;
    public int MinZ = 0;
    public int MaxX = 0;
    public int MaxY = 0;
    public int MaxZ = 0;

    // Base
    public uint Seed { get; set; } = 0;

    // Trunk
    public int Count { get; set; } = 1;
    public float HeightMin { get; set; } = 5f;
    public float HeightMax { get; set; } = 15f;
    public float SplitMin { get; set; } = 0.9f;
    public float SplitMax { get; set; } = 1.8f;
    public float ThicknessStart { get; set; } = 1.5f;
    public float ThicknessEnd { get; set; } = 0.5f;

    // Tilt
    public float TiltFactorXMin { get; set; } = -0.3f;
    public float TiltFactorXMax { get; set; } = 0.3f;
    public float TiltFactorYMin { get; set; } = -0.3f;
    public float TiltFactorYMax { get; set; } = 0.3f;

    // Branches
    public int BranchCountMin { get; set; } = 3;
    public int BranchCountMax { get; set; } = 7;
    public float BranchPositionVariance { get; set; } = 0.2f;
    public float BranchLengthMin { get; set; } = 4f;
    public float BranchLengthMax { get; set; } = 8f;
    public float BranchLengthFalloff { get; set; } = 0.3f;
    public float BranchThicknessMin { get; set; } = 0.6f;
    public float BranchThicknessMax { get; set; } = 0.6f;
    public float BranchFirstTrunkMin { get; set; } = 1f;
    public float BranchFirstTrunkMax { get; set; } = 2f;
    public float BranchTrunkStart { get; set; } = 0f;
    public float BranchTrunkEnd { get; set; } = 1f;
    public float BranchAngleMin { get; set; } = 0f;
    public float BranchAngleMax { get; set; } = 360f;
    public float BranchTiltMin { get; set; } = 0f;
    public float BranchTiltMax { get; set; } = 0f;

    // Leaves
    public int LeafClusterType { get; set; } = 0;
    public bool LeafClusterFollowBranchDirection { get; set; } = true;
    public float LeafClusterRadiusMin { get; set; } = 2f;
    public float LeafClusterRadiusMax { get; set; } = 3f;
    public float LeafClusterHeightMin { get; set; } = 3f;
    public float LeafClusterHeightMax { get; set; } = 6f;
    public float LeafClusterPositionMin { get; set; } = 0.8f;
    public float LeafClusterPositionMax { get; set; } = 0.8f;
    public int LeafClusterCountMin { get; set; } = 3;
    public int LeafClusterCountMax { get; set; } = 6;
    public float LeafClusterDensity { get; set; } = 0.8f;
    public float LeafClusterFalloff { get; set; } = 0.5f;
    public float LeafClusterScaleXMin { get; set; } = 0.8f;
    public float LeafClusterScaleXMax { get; set; } = 1.2f;
    public float LeafClusterScaleYMin { get; set; } = 0.8f;
    public float LeafClusterScaleYMax { get; set; } = 1.2f;
    public float LeafClusterScaleZMin { get; set; } = 0.8f;
    public float LeafClusterScaleZMax { get; set; } = 1.2f;

    public TreeGenerationInfo() {}
}


public class StructureTreeGenerationProcess : ThreadProcess
{
    public static HashSet<VoxelChunk> OldAffectedChunks = [];
    private Dictionary<Vector3i, VoxelChunk> _chunks = [];
    private VoxelRenderer _renderer;
    private Vector3i _position;
    private TreeGenerationInfo _info;

    public StructureTreeGenerationProcess(VoxelRenderer renderer, Vector3i position, TreeGenerationInfo info)
    {
        _renderer = renderer;
        _position = position;
        _info = info;
    }

    public override bool Function()
    {
        foreach (var chunk in OldAffectedChunks)
        {
            chunk.Blocks = new(chunk);
        }
        return TreeGenerator.Run(_info, _position, SetBlock);
    }

    public override void Break()
    {
        TreeGenerator.Cancel = true;
        base.Break();
    }

    private void SetBlock(Vector3i blockPosition, Block block, bool forceSet = false)
    {
        Vector3i chunkPosition = VoxelData.ChunkRelative(VoxelData.BlockToChunk(blockPosition));
        if (_chunks.TryGetValue(chunkPosition, out var chunk))
        {
            var currentBlock = chunk.Get(blockPosition);
            if (forceSet || currentBlock.IsAir())
            {
                chunk.Set(blockPosition, block);
            }
        }
        else if (_renderer.GetChunk(chunkPosition, out chunk))
        {
            chunk.Set(blockPosition, block);
            _chunks.Add(chunkPosition, chunk);
        }
    }

    public override void OnCompleteBase()
    {
        if (Succeded)
        {
            foreach (var (_, chunk) in _chunks)
            {
                _renderer.RerenderingQueue.AddLast(chunk);
                OldAffectedChunks.Remove(chunk);
            }
            foreach (var chunk in OldAffectedChunks)
            {
                _renderer.RerenderingQueue.AddLast(chunk);
            }
            OldAffectedChunks.Clear();
            foreach (var (_, chunk) in _chunks)
            {
                OldAffectedChunks.Add(chunk);
            }
        }
        _chunks.Clear();
    }
}

public class StructureTreeBoundingBoxAnalyser : ThreadProcess
{
    private Vector3i _position;
    private TreeGenerationInfo _info;
    private int _count;
    private Action<int> _loading;
    private Action<Vector3i, Vector3i> _finished;

    private Vector3i _a;
    private Vector3i _b;

    public StructureTreeBoundingBoxAnalyser(Vector3i position, TreeGenerationInfo info, int count, Action<int> loading, Action<Vector3i, Vector3i> finished)
    {
        _position = position;
        _info = info;
        _count = count;
        _loading = loading;
        _finished = finished;
        _a = position;
        _b = position;
    }

    public override bool Function()
    {
        for (int i = 0; i < _count; i++)
        {
            _info.Seed++;
            if (!TreeGenerator.Run(_info, _position, SetBlock))
            {
                return false;
            }
            else
            {
                _loading.Invoke(i);
            }
        }
        return true;
    }

    public override void Break()
    {
        TreeGenerator.Cancel = true;
        base.Break();
    }

    private void SetBlock(Vector3i blockPosition, Block block, bool forceSet = false)
    {
        _a = Mathf.Min(blockPosition, _a);
        _b = Mathf.Max(blockPosition, _b);
    }

    public override void OnCompleteBase()
    {
        _finished.Invoke(_a, _b);
    }
}