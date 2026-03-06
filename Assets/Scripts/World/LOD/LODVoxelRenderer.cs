using PBG.MathLibrary;
using PBG;
using PBG.Core;
using PBG.Data;
using PBG.Rendering;
using PBG.Threads;
using PBG.Voxel;

public class LODVoxelRenderer : ScriptingNode
{
    public Dictionary<Vector3i, LODBaseChunk> LODChunks = [];

    public List<LODChunk> Chunks = [];

    public HashSet<LODChunk> RerenderMap = [];
    public HashSet<LODChunk> FreedMap = [];

    public Queue<LODChunk> GenerationQueue = [];
    public Queue<LODChunk> PopulationQueue = [];
    public LinkedList<LODChunk> RenderingQueue = [];
    public LinkedList<LODChunk> RerenderingQueue = [];
    public Queue<LODChunk> ToBeFreedQueue = [];

    public LODWorldGenerator Generator = new();

    private Camera _camera;

    private VoxelRenderer voxelRenderer;

    private (int left, int right, int bottom, int top) _viewport;
    private int _width;
    private int _height;

    public LODVoxelRenderer()
    {
        _camera = new Camera(Game.Width, Game.Height, (0, 0, 0));
        _viewport = (0, 0, 0, 0);
        _width = Game.Width;
        _height = Game.Height;
    }

    void Start()
    {
        //Init(3);

        voxelRenderer = Transform.GetComponent<VoxelRenderer>(); 
    }

    void Awake()
    {
    }

    void Resize()
    {
        _width = Game.Width - (_viewport.left + _viewport.right);
        _height = Game.Height - (_viewport.bottom + _viewport.top);
        _camera = new Camera(_width, _height, (0, 0, 0));
        _camera.GetProjectionMatrix();
    }

    public void Init(int level)
    {
        if (level <= -1)
            return;

        List<LODChunk> chunks = [];
            
        var p = Mathf.Pow(2, level + 5);
        for (int x = -8; x < 8; x++)
        {
            for (int z = -8; z < 8; z++)
            {
                for (int y = 0; y < 1; y++)
                {
                    var position = new Vector3i(x, y, z) * p;
                    var distance = Vector2.Distance((0, 0), position.Xz);

                    if (distance < p * 8)
                    {
                        InitInner(chunks, level - 1, position);
                    }
                    else
                    {
                        LODChunk chunk = new(this, position, level);
                        chunks.Add(chunk);
                    }
                }
            }
        }
        
        chunks.Sort((a, b) => Vector2.DistanceSquared(a.Center.Xz, Vector2.Zero).CompareTo(Vector2.DistanceSquared(b.Center.Xz, Vector2.Zero)));

        for (int i = 0; i < chunks.Count; i++)
        {
            GenerationQueue.Enqueue(chunks[i]);
        }
    }

    public void InitInner(List<LODChunk> chunks, int level, Vector3i parentPosition)
    {
        if (level <= -1)
            return;

        var p = Mathf.Pow(2, level + 5);
        for (int x = 0; x < 2; x++)
        {
            for (int z = 0; z < 2; z++)
            {
                for (int y = 0; y < 2; y++)
                {
                    var position = new Vector3i(x, y, z) * p + parentPosition;
                    var distance = Vector2.Distance((0, 0), position.Xz);

                    if (distance < p * 8)
                    {
                        if (level == 0)
                        {

                        }
                        else
                        {
                            InitInner(chunks, level - 1, position);
                        }
                    }
                    else
                    {
                        LODChunk chunk = new(this, position, level);
                        chunks.Add(chunk);
                    }
                }
            }
        }
    }

    public void Update()
    {
        /*
        if (GenerateChunks)
        {
            Vector3i newPosition = VoxelData.BlockToChunkRelative(Mathf.FloorToInt(Transform.Position));
            if (_enableTerrainGeneration && newPosition.Xz != _currentChunk.Xz)
            {
                _currentChunk.Xz = Mathf.FloorToInt(newPosition.Xz);
                ChunkCheck(_currentChunk);
            }

            ChunkGenerator.GenerateChunk(this);
        }
        
        CheckFrustum();

        Info.SetGenerationQueueCount(GenerationQueue.Count);
        */
        return;
        CheckFrustum();
        
        if (GenerationQueue.Count > 0)
        {
            Generator.Generate(this);
        }

        if (RenderingQueue.Count > 0)
        {
            for (int i = 0; i < 2.Min(RenderingQueue.Count); i++)
            {
                var chunk = RenderingQueue.First;
                if (chunk != null)
                {
                    /*
                    if (!ChunkDictionary.ContainsKey(chunk.Value.RelativePosition))
                    {
                        RenderingQueue.Remove(chunk);
                        continue;
                    }
                    */

                    if (!chunk.Value.ToBeRemoved)
                    {
                        LODChunkRenderingProcess renderingProcess = new LODChunkRenderingProcess(chunk.Value);
                        TaskPool.QueueAction(renderingProcess, TaskPriority.Normal);
                        RenderingQueue.Remove(chunk);
                    }
                    else
                    {
                        RenderingQueue.Remove(chunk);
                        RenderingQueue.AddLast(chunk);
                    }
                }
            }
        }
        //Console.WriteLine("Rendering queue: " + RenderingQueue.Count);
    }

    void LateUpdate()
    {
        //if (!Run) return;
        for (int i = 0; i < Chunks.Count; i++)
        {
            var chunk = Chunks[i];
            chunk.IsDisabled |= !chunk.HasBlocks || chunk.ForceDisabled;
        }
    }

    void Render()
    {
        /*
        GL.Viewport(0, 0, Game.Width, Game.Height);

        Matrix4 view = Scene.DefaultCamera.ViewMatrix;
        Matrix4 projection = _camera.ProjectionMatrix;

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);
        GL.Enable(EnableCap.CullFace);

        VoxelRenderer.WorldShader.Bind();

        GL.UniformMatrix4(VoxelRenderer.WorldShaderLocation.View, false, ref view);
        GL.UniformMatrix4(VoxelRenderer.WorldShaderLocation.Projection, false, ref projection);

        GL.Uniform1(VoxelRenderer.WorldShaderLocation.Texture, 0);
        GL.Uniform3(VoxelRenderer.WorldShaderLocation.LightDirection, voxelRenderer.LightDirection);
        GL.Uniform3(VoxelRenderer.WorldShaderLocation.CameraPosition, Camera.Position);
        GL.Uniform3(VoxelRenderer.WorldShaderLocation.PlayerPosition, Transform.Position);
        GL.Uniform1(VoxelRenderer.WorldShaderLocation.AmbientOcclusionTexture, 1);
        GL.Uniform1(VoxelRenderer.WorldShaderLocation.DoAmbientOcclusion, 0);
        GL.Uniform1(VoxelRenderer.WorldShaderLocation.DoRealtimeShadows, 0);

        BlockData.BlockTextureArray.Bind(TextureUnit.Texture0);

        int count = 0;
        for (int i = 0; i < Chunks.Count; i++)
        {
            var chunk = Chunks[i];
            if (chunk.IsDisabled)
                continue;

            GL.UniformMatrix4(VoxelRenderer.WorldShaderLocation.Model, false, ref chunk.ModelMatrix);
            chunk.Render();
            count++;
        }

        //Console.WriteLine(count);

        BlockData.BlockTextureArray.Unbind();
        VoxelRenderer.WorldShader.Unbind();
        */
    }

    public void CheckFrustum()
    {
        for (int i = 0; i < Chunks.Count; i++)
        {
            var chunk = Chunks[i];
            if (!chunk.HasBlocks)
                continue;

            bool frustum = Scene.DefaultCamera.FrustumIntersectsSphere(chunk.Center, 28 * (1 << chunk.Level));
            chunk.IsDisabled = !frustum;
        }
    }
}