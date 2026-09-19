using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using PBG.Core;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.Voxel;
using Silk.NET.Vulkan;

namespace PBG.NewVoxel;

[InternalSystemInit(InitPriority.Shader)]
public unsafe partial class VoxelRenderer : ScriptingNode
{
    public static uint TotalVertexCount = 0;

    public static int GenerationThreads = 5;
    public static int RenderingThreads = 3;

    public static long[] GenerationThreadTimes = [];
    public static long[] RenderingThreadTimes = [];

    public bool AmbientOcclusion = true;
    public bool RealtimeShadows = true;
    public bool NeedsNeighborsToRender = true;
    public bool RenderNormal = true;
    public bool RenderWireframe = false;

    private Vector3 _lightUp;
    public Vector3 LightDirection = (0, -1, 0);

    public static FBO CloseFBO = new FBO(4000, 4000);
    public static FBO MiddleFBO = new FBO(3000, 3000);
    public static FBO FarFBO = new FBO(2000, 2000);

    public static Camera CloseCamera = new(140, 140, -140, 140, (0, 0, 0));
    public static Camera MiddleCamera = new(640, 640, -500, 500, (0, 0, 0));
    public static Camera FarCamera = new(2560, 2560, -2500, 2500, (0, 0, 0));

    public static float CloseTexelSize = 0;
    public static float MiddleTexelSize = 0;
    public static float FarTexelSize = 0;

    private Matrix4 _closeLightSpaceMatrix;
    private Matrix4 _middleLightSpaceMatrix;
    private Matrix4 _farLightSpaceMatrix;

    private float _closeLightTimer = 0.1f;
    private float _middleLightTimer = 0.25f;
    private float _farLightTimer = 0.5f;


    public float closeTimer = 0.05f;
    public float middleTimer = 0.2f;
    public float farTimer = 0.5f;


    public Vector3i CurrentChunk = Vector3i.Zero;
    public Vector3i PreviousChunk = Vector3i.Zero;

    public Vector3i CameraCurrentChunk = Vector3i.Zero;
    public Vector3i CameraPreviousChunk = Vector3i.Zero;


    public int RenderDistance = 32;
    public int WorldHeight = 8;

    private RelativeChunkInfo[] _relativeChunkPositions = [];

    public ConcurrentDictionary<Vector3i, VoxelChunk> ChunkDictionnary = [];
    public ChunkLookupInfo[] ChunkLookupArray1 = [];
    public ChunkLookupInfo[] ChunkLookupArray2 = [];

    public NewChunkDataPool DataPool;
    public static DebugModule? DebugModule;


    private Vector3 _priorityPosition;

    private int _playerState = 0;


    private Skybox skybox = null!;
    

    public bool Run = true;


    public VoxelRenderer()
    {
        
    }


    void Start()
    {
        GenerationThreadTimes = new long[GenerationThreads];
        RenderingThreadTimes = new long[RenderingThreads];

        skybox = Transform.GetComponent<Skybox>();

        CloseCamera.CameraProjection = CameraProjection.OrthographicOffCenter;
        MiddleCamera.CameraProjection = CameraProjection.OrthographicOffCenter;
        FarCamera.CameraProjection = CameraProjection.OrthographicOffCenter;
    }

    void Awake()
    {
        DataPool = new(this);

        AwakeGeneration();

        Camera.SetCameraMode(Rendering.CameraMode.Fixed);
        Game.SetCursorState(CursorMode.Normal);

        GenerateRelativeChunkPositions();
        CheckChunkPositions2();

        Volatile.Write(ref _renderDistance, 0);

        StartWorkers();
    }

    private Task? _renderDistanceTask = null;
    private int _renderDistance = 0;


    void Update()
    {
        if (!Run)
            return;

        if (Input.IsKeyPressed(Key.R))
        {
            GFX.DeviceWaitIdle();
            WorldShader.Shader.RenewDescriptors();
        }


        if (Input.IsKeyPressed(Key.L))
        {
            var relative = VoxelData.BlockToChunkRelative(Mathf.FloorToInt(Camera.Position));
            if (GetChunk(relative, out var chunk))
                DebugDump.ToFile(chunk.StatusChanges, $"log-{relative.X}-{relative.Y}-{relative.Z}");
        }

        if (Input.IsKeyPressed(Key.F1))
        {
            _playerState = 0;
        }

        if (Input.IsKeyPressed(Key.F2))
        {
            _playerState = 1;
        }

        if (_playerState == 0)
        {
            
        }
        else if (_playerState == 1)
        {
            Transform.Position = Camera.Position;
            Transform.Position.Y = 64;
        }


        WorldSettings.Tick(GameTime.DeltaTime);

        float angle = WorldSettings.Time * 360f;
        LightDirection = Mathf.RotatePoint((0, 1, 0), (0, 0, 0), (0, 0, 1), angle);
        var right = Vector3.Normalize(Vector3.Cross(LightDirection, Vector3.UnitY));
        _lightUp = Vector3.Normalize(Vector3.Cross(right, LightDirection));

        if (skybox != null)
        {
            skybox.LightDirection = LightDirection;
            skybox.Time = WorldSettings.Time;
        }

        var currentPosition = VoxelData.BlockToChunkRelative(Transform.Position.Fti());

        if (Volatile.Read(ref _renderDistance) == 0)
        {
            if (CurrentChunk.Xz != currentPosition.Xz)
            {
                PreviousChunk = CurrentChunk;
                CurrentChunk = currentPosition;

                _priorityPosition = currentPosition;

                Volatile.Write(ref _renderDistance, 1);

                //DataPool.OrderChunks(currentPosition + (16, 16, 16));

                _renderDistanceTask = Task.Run(() =>
                {
                    //Stopwatch sw = Stopwatch.StartNew();
                    CheckChunkPositions2();
                    //Console.WriteLine(sw.Elapsed.TotalMilliseconds + " ms");
                })
                .ContinueWith(task =>
                {
                    Volatile.Write(ref _renderDistance, 0);
                }); 
            }
        }

        HandleGenerations();

        HandleReuses();
        HandleUploads();
        HandleDeletions();

        if (GameTime.FpsUpdated)
        {
            //Console.WriteLine(GameTime.Fps);
            DataPool.EmptyCheck();

        }

        if (Input.IsKeyDown(Key.ControlRight))
        {
            if (Input.IsKeyPressed(Key.Escape))
            {
                Camera.SetCameraMode(Rendering.CameraMode.Fixed);
                Game.SetCursorState(CursorMode.Normal);
            }

            if (Input.IsMousePressed(MouseButton.Left))
            {
                Camera.SetCameraMode(Rendering.CameraMode.Free);
                Game.SetCursorState(CursorMode.Disabled);
            }
        }
        
        
        if (Input.IsKeyPressed(Key.J))
        {
            DebugModule?.Dispose();
            DebugModule = new();

            foreach (var (_, chunk) in ChunkDictionnary)
            {
                DebugModule.AddGrid(chunk.WorldPosition, Vector3.UnitX, Vector3.UnitY, new Vector2(32, 32), new Vector2(32, 32), (0, 0), new Vector3(1, 0, 0));
                DebugModule.AddGrid(chunk.WorldPosition + new Vector3(0, 32, 0), Vector3.UnitX, Vector3.UnitY, new Vector2(32, 32), new Vector2(32, 32), (0, 0), new Vector3(1, 0, 0));

                DebugModule.AddGrid(chunk.WorldPosition, Vector3.UnitZ, Vector3.UnitX, new Vector2(32, 32), new Vector2(32, 32), (0, 0), new Vector3(1, 0, 0), IncludedBorder.Left | IncludedBorder.Right);
                DebugModule.AddGrid(chunk.WorldPosition + new Vector3(32, 0, 0), Vector3.UnitZ, Vector3.UnitX, new Vector2(32, 32), new Vector2(32, 32), (0, 0), new Vector3(1, 0, 0), IncludedBorder.Left | IncludedBorder.Right);

                DebugModule.AddGrid(chunk.WorldPosition, Vector3.UnitX, Vector3.UnitZ, new Vector2(32, 32), new Vector2(32, 32), (0, 0), new Vector3(1, 0, 0), IncludedBorder.None);
                DebugModule.AddGrid(chunk.WorldPosition + new Vector3(0, 0, 32), Vector3.UnitX, Vector3.UnitZ, new Vector2(32, 32), new Vector2(32, 32), (0, 0), new Vector3(1, 0, 0), IncludedBorder.None);
            }

            DebugModule.Generate();
        }
    }

    void LateUpdate()
    {
        if (!Run) return;

        while (_closeLightTimer >= closeTimer)
        {
            _closeLightTimer -= closeTimer;
        }

        while (_middleLightTimer >= middleTimer)
        {
            _middleLightTimer -= middleTimer;
        }

        while (_farLightTimer >= farTimer)
        {
            _farLightTimer -= farTimer;
        }

        _closeLightTimer += GameTime.DeltaTime;
        _middleLightTimer += GameTime.DeltaTime;
        _farLightTimer += GameTime.DeltaTime;

        //Info.SetChunkTotalCount(VoxelChunkInstances.Count);
    }

    void Compute()
    {
        if (!Run)
            return;

        DataPool.Reset();
        DataPool.UpdateDrawCommands(0);  

        DataPool.FrustumPass(Camera, 0);

        if (RealtimeShadows)
        {
            if (_closeLightTimer >= closeTimer)
            {
                CloseCamera.Front = LightDirection;
                CloseCamera.Position = Camera.Position;
                CloseCamera.UpdateRightUpVectors();

                CloseTexelSize = (float)CloseCamera.SCREEN_WIDTH / (float)CloseFBO.Width;

                Vector3 right = CloseCamera.Right;
                Vector3 up    = CloseCamera.Up;
                Vector3 p     = CloseCamera.Position;

                float x = Vector3.Dot(p, right);
                float y = Vector3.Dot(p, up);

                p -= right * (x - MathF.Floor(x / CloseTexelSize) * CloseTexelSize);
                p -= up    * (y - MathF.Floor(y / CloseTexelSize) * CloseTexelSize);

                CloseCamera.Position = p;
                CloseCamera.Front = LightDirection;

                CloseCamera.Update();
                DataPool.FrustumPass(CloseCamera, 1);

                DataPool.UpdateDescriptorUniform(d => d.Uniform(WorldShader.CloseTexelSize, CloseTexelSize), 0);
            }

            if (_middleLightTimer >= middleTimer)
            {
                MiddleCamera.Front = LightDirection;
                MiddleCamera.Position = Camera.Position;
                MiddleCamera.UpdateRightUpVectors();

                MiddleTexelSize = (float)MiddleCamera.SCREEN_WIDTH / (float)MiddleFBO.Width;

                Vector3 right = MiddleCamera.Right;
                Vector3 up    = MiddleCamera.Up;
                Vector3 p     = MiddleCamera.Position;

                float x = Vector3.Dot(p, right);
                float y = Vector3.Dot(p, up);

                p -= right * (x - MathF.Floor(x / MiddleTexelSize) * MiddleTexelSize);
                p -= up    * (y - MathF.Floor(y / MiddleTexelSize) * MiddleTexelSize);

                MiddleCamera.Position = p;
                MiddleCamera.Front = LightDirection;

                MiddleCamera.Update();
                DataPool.FrustumPass(MiddleCamera, 2);

                DataPool.UpdateDescriptorUniform(d => d.Uniform(WorldShader.MiddleTexelSize, MiddleTexelSize), 0);
            }

            if (_farLightTimer >= farTimer)
            {
                FarCamera.Front = LightDirection;
                FarCamera.Position = Camera.Position;
                FarCamera.UpdateRightUpVectors();

                FarTexelSize = (float)FarCamera.SCREEN_WIDTH / (float)FarFBO.Width;

                Vector3 right = FarCamera.Right;
                Vector3 up    = FarCamera.Up;
                Vector3 p     = FarCamera.Position;

                float x = Vector3.Dot(p, right);
                float y = Vector3.Dot(p, up);

                p -= right * (x - MathF.Floor(x / FarTexelSize) * FarTexelSize);
                p -= up    * (y - MathF.Floor(y / FarTexelSize) * FarTexelSize);

                FarCamera.Position = p;
                FarCamera.Front = LightDirection;
                
                FarCamera.Update();
                DataPool.FrustumPass(FarCamera, 3);

                DataPool.UpdateDescriptorUniform(d => d.Uniform(WorldShader.FarTexelSize, FarTexelSize), 0);
            }
        }
    }

    void Render()
    {
        if (!Run)
            return;

        if (RealtimeShadows)
        {
            if (_closeLightTimer >= closeTimer)
            {
                CloseFBO.Bind();

                BlankWorldShader.Bind();
                DataPool.RenderBlank(CloseCamera.ViewMatrix, CloseCamera.ProjectionMatrix, 1);

                CloseFBO.Unbind();

                _closeLightSpaceMatrix = CloseCamera.GetViewProjectionMatrix();
            }

            if (_middleLightTimer >= middleTimer)
            {
                MiddleFBO.Bind();

                BlankWorldShader.Bind();
                DataPool.RenderBlank(MiddleCamera.ViewMatrix, MiddleCamera.ProjectionMatrix, 2);

                MiddleFBO.Unbind();

                _middleLightSpaceMatrix = MiddleCamera.GetViewProjectionMatrix();
            }

            if (_farLightTimer >= farTimer)
            {
                FarFBO.Bind();

                BlankWorldShader.Bind();
                DataPool.RenderBlank(FarCamera.ViewMatrix, FarCamera.ProjectionMatrix, 3);

                FarFBO.Unbind();

                _farLightSpaceMatrix = FarCamera.GetViewProjectionMatrix();
            }
        }

        GFX.Viewport(0, 0, Game.Width, Game.Height);

        DebugModule?.Render(Camera, Vector3.Zero);

        if (RenderNormal)
        {
            WorldShader.Shader.Bind();
            DataPool.Render(Camera, 0); 
        }
        
        if (RenderWireframe)
        {
            WireframeWorldShader.Bind();
            DataPool.RenderWireframe();
        }
    }

    void Exit()
    {
        ExitGeneration();
        
        StopWorkers();

        _genQueue.Clear();
        _genSet.Clear();
        _generationSignal.Dispose();

        _renderingQueue.Clear();
        _renderingSignal.Dispose();

        UploadQueue.Clear();

        foreach (var (_, chunk) in ChunkDictionnary)
        {
            chunk.Dispose();
        }

        ChunkDictionnary.Clear();
        _relativeChunkPositions = [];

        DataPool.Dispose();
    }


    public void UpdateUniforms(Camera camera, Descriptor descriptor)
    {
        descriptor.Uniform(WorldShader.View, camera.ViewMatrix);
        descriptor.Uniform(WorldShader.Projection, camera.ProjectionMatrix);
        descriptor.Uniform(WorldShader.CloseLightSpaceMatrix, _closeLightSpaceMatrix);
        descriptor.Uniform(WorldShader.MiddleLightSpaceMatrix, _middleLightSpaceMatrix);
        descriptor.Uniform(WorldShader.FarLightSpaceMatrix, _farLightSpaceMatrix);

        descriptor.Uniform(WorldShader.LightDirection, LightDirection);
        descriptor.Uniform(WorldShader.DoRealtimeShadows, RealtimeShadows ? 1 : 0);
        descriptor.Uniform(WorldShader.DoAmbientOcclusion, AmbientOcclusion ? 1 : 0);
        descriptor.Uniform(WorldShader.PlayerPosition, camera.Position);
        descriptor.Uniform(WorldShader.Time, WorldSettings.Time);
    }


    public VoxelChunk? GetChunk(Vector3i position)
    {
        if (ChunkDictionnary.TryGetValue(position, out var chunk))
            return chunk;
        return null;
    }

    public bool GetChunk(Vector3i position, [NotNullWhen(true)] out VoxelChunk? chunk)
    {
        if (!ChunkDictionnary.TryGetValue(position, out chunk))
            return false;

        return chunk != null;
    }


    public bool SetBlock(int x, int y, int z, Block block) => SetBlock((x, y, z), block, out _);
    public bool SetBlock(int x, int y, int z, Block block, [NotNullWhen(true)] out VoxelChunk? chunk) => SetBlock((x, y, z), block, out chunk);
    public bool SetBlock(Vector3i blockPosition, Block block) => SetBlock(blockPosition, block, out _);
    public unsafe bool SetBlock(Vector3i blockPosition, Block block, [NotNullWhen(true)] out VoxelChunk? chunk)
    {
        Vector3i chunkPosition = VoxelData.BlockToChunkRelative(blockPosition);
        if (!GetChunk(chunkPosition, out chunk))
            return false;

        Vector3i relative = VoxelData.BlockToRelative(blockPosition);
        chunk.Set(VoxelChunk.GetIndex(relative), block);
        EnqueueRendering(chunk);
    
        if (relative.X == 0)
        {
            QueueMeshFromBlock(blockPosition - (1, 0, 0));
        }
        else if (relative.X == 31)
        {
            QueueMeshFromBlock(blockPosition + (1, 0, 0));
        }

        if (relative.Y == 0)
        {
            QueueMeshFromBlock(blockPosition - (0, 1, 0));
        }
        else if (relative.Y == 31)
        {
            QueueMeshFromBlock(blockPosition + (0, 1, 0));
        }

        if (relative.Z == 0)
        {
            QueueMeshFromBlock(blockPosition - (0, 0, 1));
        }
        else if (relative.Z == 31)
        {
            QueueMeshFromBlock(blockPosition + (0, 0, 1));
        }

        return true;
    }

    public bool GetBlock(Vector3i blockPosition, out Block block)
    {
        block = Block.Air;
        var relative = VoxelData.BlockToChunkRelative(blockPosition);
        if (!ChunkDictionnary.TryGetValue(relative, out var chunk))
            return false;

        //block = chunk.Blocks[ChunkBlocks.GetIndex(blockPosition)];
        block = chunk.Get(blockPosition);
        return !block.IsAir();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAir(Vector3i blockPosition)
    {
        var relative = VoxelData.BlockToChunkRelative(blockPosition);
        if (!ChunkDictionnary.TryGetValue(relative, out var chunk))
            return true;

        return chunk.Get(VoxelChunk.GetIndex(blockPosition)).IsAir();
    }



    public void QueueMeshFromBlock(Vector3i blockPosition)
    {
        var pos = VoxelData.BlockToChunkRelative(blockPosition);
        if (GetChunk(pos, out var chunk))
        {
            chunk.Status = ChunkStatus.Canceled;
            EnqueueRendering(chunk);
        }
    }



    private float GetPriority(VoxelChunk chunk) => VoxelUtils.GetPriority(chunk, _priorityPosition);
        

    public void GenerateRelativeChunkPositions()
    {
        Vector3 half = new Vector3(0.5f);
        List<RelativeChunkInfo> positions = [];

        int gridSize = RenderDistance * 2 + 1;
        int gridSizeSquared = gridSize * gridSize;

        for (int dx = -RenderDistance; dx <= RenderDistance; dx++)
        {
            for (int dz = -RenderDistance; dz <= RenderDistance; dz++)
            {
                for (int dy = 0; dy < WorldHeight; dy++)
                {
                    int nx = dx + RenderDistance;
                    int nz = dz + RenderDistance;

                    int index = nx + nz * gridSize + dy * gridSizeSquared; 

                    positions.Add(new()
                    {
                        Position = (dx, dy, dz),
                        Translation = (nx, dy, nz),
                        Index = index
                    });
                }
            } 
        }

        positions.Sort((a, b) =>
        {
            float da = Vector3.DistanceSquared(a.Position, Vector3.Zero);
            float db = Vector3.DistanceSquared(b.Position, Vector3.Zero);

            return da.CompareTo(db);
        });

        _relativeChunkPositions = [..positions];

        ChunkLookupArray1 = new ChunkLookupInfo[_relativeChunkPositions.Length];
        ChunkLookupArray2 = new ChunkLookupInfo[_relativeChunkPositions.Length];
    }

    public void CheckChunkPositions1()
    {
        Stopwatch sw = Stopwatch.StartNew();

        Vector3i currentChunk = CurrentChunk;
        Vector3i previousChunk = PreviousChunk;

        Vector3i currentPosition  = (0, 0, 0);
        Vector3i offset = currentChunk - previousChunk;
        Vector3i mirrored = (0, 0, 0);

        _genQueue.Clear();

        for (int i = 0; i < _relativeChunkPositions.Length; i++)
        {
            ref var data = ref _relativeChunkPositions[i];

            ref var position = ref data.Position;
            ref var index = ref data.Index;

            currentPosition.X = position.X + currentChunk.X;
            currentPosition.Y = position.Y;
            currentPosition.Z = position.Z + currentChunk.Z;

            // If the chunk at the current position doesn't exist, we need to create a new one
            if (!ChunkDictionnary.TryGetValue(currentPosition, out var existingChunk))
            {
                //double add = sw.Elapsed.TotalMilliseconds;
                // we can try to get it from a old position that is now out of the bounds of the render distance
                VoxelChunk chunk;

                // Mirror each shifted axis through the origin, the region of new relative
                // positions and the region of old relative positions are always symmetric
                // around the center, so negating position on axes where we moved maps a new
                // slot to its old counterpart from the previous grid for any offset size.

                mirrored.X = position.X;
                mirrored.Y = position.Y;
                mirrored.Z = position.Z;
                
                if (offset.X != 0) mirrored.X = -position.X;
                if (offset.Z != 0) mirrored.Z = -position.Z;

                mirrored.X += previousChunk.X;
                mirrored.Z += previousChunk.Z;

                if (ChunkDictionnary.Remove(mirrored, out var outside))
                {
                    chunk = outside;
                    chunk.Status = ChunkStatus.Empty;
                    chunk.SetPosition(currentPosition); 
                }
                else
                {
                    chunk = new(this, currentPosition);
                }
                
                ChunkDictionnary.TryAdd(currentPosition, chunk);

                chunk.Status = ChunkStatus.QueuedToGenerate;
                ReuseQueue.Enqueue(chunk);
            }
            else
            {
                if (existingChunk.Status == ChunkStatus.QueuedToGenerate)
                {
                    _genQueue.Enqueue(existingChunk);
                    _generationSignal.Release();
                }
            }
        }

        Console.WriteLine(sw.Elapsed.TotalMilliseconds + " ms");
    }

    public void CheckChunkPositions2()
    {
        Vector3i currentChunk = CurrentChunk;
        Vector3i previousChunk = PreviousChunk;

        Vector3i currentPosition  = (0, 0, 0);
        Vector3i offset = currentChunk - previousChunk;

        int RD2 = RenderDistance * 2; // render distance x2
        int gridSize = RD2 + 1;
        int gridSizeSquared = gridSize * gridSize;

        for (int i = 0; i < _relativeChunkPositions.Length; i++)
        {
            ref var data = ref _relativeChunkPositions[i];

            ref var translation = ref data.Translation;

            int x = translation.X;
            int z = translation.Z;

            int oldX = x + offset.X;
            int oldZ = z + offset.Z;
            bool isValid = true;

            if (oldX < 0 || oldX >= gridSize)
            {
                oldX = RD2 - x;
                isValid = false;
            }
            if (oldZ < 0 || oldZ >= gridSize)
            {
                oldZ = RD2 - z;
                isValid = false;
            }

            int oldIndex = oldX + oldZ * gridSize + translation.Y * gridSizeSquared;
            
            ref var chunk = ref ChunkLookupArray1[data.Index];

            chunk.Chunk = ChunkLookupArray2[oldIndex].Chunk;
            chunk.IsValid = isValid;
        }  

        _genQueue.Clear();
        _renderingQueue.Clear();

        for (int i = 0; i < _relativeChunkPositions.Length; i++)
        {
            ref var data = ref _relativeChunkPositions[i];

            ref var position = ref data.Position;
            ref var index = ref data.Index;

            currentPosition.X = position.X + currentChunk.X;
            currentPosition.Y = position.Y;
            currentPosition.Z = position.Z + currentChunk.Z;

            ref var chunkInfo = ref ChunkLookupArray1[index];
            var existingChunk = chunkInfo.Chunk;

            // If the chunk at the current position doesn't exist, we need to create a new one
            if (existingChunk == null || !chunkInfo.IsValid)
            {
                // we can try to get it from a old position that is now out of the bounds of the render distance
                VoxelChunk chunk;

                if (existingChunk != null)
                {
                    ChunkDictionnary.Remove(existingChunk.RelativePosition, out var _);
                    ChunkGeneration.RemoveCache(existingChunk.RelativePosition.Xz);

                    chunk = existingChunk;
                    chunk.Status = ChunkStatus.Empty;
                    chunk.FreeAllocation();
                    chunk.SetPosition(currentPosition); 
                }
                else
                {
                    chunk = new(this, currentPosition);
                    
                }

                chunkInfo.Chunk = chunk;
                
                ChunkDictionnary.TryAdd(currentPosition, chunk);
                ChunkGeneration.TryAddCache(currentPosition.Xz);

                chunk.Status = ChunkStatus.QueuedToGenerate;
                _genQueue.Enqueue(chunk);
                _generationSignal.Release();
            }
            else
            {
                if (existingChunk.Status == ChunkStatus.QueuedToGenerate)
                {
                    _genQueue.Enqueue(existingChunk);
                    _generationSignal.Release();
                }

                if (existingChunk.Status == ChunkStatus.QueuedToMesh)
                {
                    _renderingQueue.Enqueue(existingChunk);
                    _renderingSignal.Release();
                }
            }
        }

        (ChunkLookupArray1, ChunkLookupArray2) = (ChunkLookupArray2, ChunkLookupArray1);
    }

    public void UpdateWireframeUniforms(Descriptor descriptor)
    {
        descriptor.Uniform(WireframeWorldViewLocation, Camera.ViewMatrix);
        descriptor.Uniform(WireframeWorldProjectionLocation, Camera.ProjectionMatrix);
    }

    private readonly SemaphoreSlim _workSignal = new(0);

    #region Generation
    public static Action<VoxelChunk> ChunkAction = BaseChunkTest;

    public AsyncVoxelGenerationBatch GenerationBatch;

    public int GenQueueCount { get => _genQueue.Count; }

    private ConcurrentQueue<VoxelChunk> _genQueue = new();
    private HashSet<VoxelChunk> _genSet = [];
    private readonly object _genLock = new();
    private readonly SemaphoreSlim _generationSignal = new(0);


    public int EnqueueGenerationUnsafeBase(VoxelChunk chunk)
    {
        if (_genSet.Add(chunk))
        {
            _genQueue.Enqueue(chunk);
            chunk.Status = ChunkStatus.QueuedToGenerate;
            return 1;
        }
        return 0;
    }

    public void EnqueueGeneration(VoxelChunk chunk)
    {
        _genQueue.Enqueue(chunk);
    }

    public bool TryDequeueGeneration([NotNullWhen(true)] out VoxelChunk? chunk)
    {
        if (!_genQueue.TryDequeue(out chunk))
            return false;

        chunk.Status = ChunkStatus.Generating;
        return true;
    }

    private void AwakeGeneration()
    {
        GenerationBatch = new(16, WorldGenerationShader);
    }

    private void ExitGeneration()
    {
        GenerationBatch.Dispose();
    }

    public void HandleGenerations()
    {
        /*
        GenerationBatch.Update();

        GenerationBatch.HandleCompletions();

        while (GenQueue.Count != 0)
        {
            var slot = GenerationBatch.TryGetAvailableSlot();
            if (slot == null || !TryDequeueGeneration(out var chunk))
                break;

            var job = new ChunkJob()
            {
                Chunk = chunk,
                Slot = slot
            };
            
            GenerationBatch.SubmitBatch(job);
        }
        */
    }

    private static void BaseChunkTest(VoxelChunk chunk)
    {
        if (chunk.WorldPosition.Y == 0)
        for (int x = 0; x < 32; x+=2)
        {
            for (int y = 0; y < 32; y+=2)
            {
                for (int z = 0; z < 32; z+=2)
                {
                    chunk[x, y, z] = new Block(BlockState.Solid, 1);
                }
            }
            
        }
    }

    private void GenerationWorkerLoop(int workerId)
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                _generationSignal.Wait(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }  
            
            HandleGeneration(workerId);
        }
    }
    
    private void HandleGeneration(int workerId)
    {
        if (!TryDequeueGeneration(out var chunk))
            return;

        long start = Stopwatch.GetTimestamp();

        Interlocked.Increment(ref WorldNodeEditor.GenerationCount);
        if (ChunkGeneration.GenerateChunk(chunk, workerId))
            chunk.EnqueueRendering();

        long end = Stopwatch.GetTimestamp();
        long elapsedTicks = end - start;
        Interlocked.Add(ref GenerationThreadTimes[workerId], elapsedTicks);
    }
    #endregion




    #region Rendering
    public int RenderingQueueCount { get => _renderingQueue.Count; }

    private ConcurrentQueue<VoxelChunk> _renderingQueue = new();

    /*
    private PriorityQueue<NewVoxelChunk, float> _renderingQueue = new();
    private HashSet<NewVoxelChunk> _renderSet = [];
    */

    private readonly object _renderingLock = new();
    private SemaphoreSlim _renderingSignal = new(0);

    /*
    public void EnqueueRendering(NewVoxelChunk chunk)
    {
        lock (_renderingLock)
        {
            _renderSet.Add(chunk);
            float priority = GetPriority(chunk);
            _renderingQueue.Enqueue(chunk, priority);
            chunk.Status = ChunkStatus.QueuedToMesh;
        }
        _renderingSignal.Release();
    }
    */

    public void EnqueueRendering(VoxelChunk chunk)
    {
        lock (_renderingLock)
        {
            chunk.NewGen();
            chunk.FreeAllocation();
            _renderingQueue.Enqueue(chunk);
            chunk.Status = ChunkStatus.QueuedToMesh;
        }
        _renderingSignal.Release();
    }


    public bool TryDequeueRendering([NotNullWhen(true)] out VoxelChunk? chunk)
    {
        chunk = null;

        lock (_renderingLock)
        {
            if (!_renderingQueue.TryDequeue(out chunk) || chunk.Status != ChunkStatus.QueuedToMesh  || chunk.Status == ChunkStatus.Meshing) 
                return false;

            chunk.Status = ChunkStatus.Meshing;
            return true;
        }
    }

    private void RenderingWorkerLoop(int workerId)
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    _renderingSignal.Wait(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                HandleRendering2(workerId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public void HandleRendering(int workerId)
    {
        long start = Stopwatch.GetTimestamp();
        if (TryDequeueRendering(out var chunk))
        {
            Interlocked.Increment(ref WorldNodeEditor.RenderingCount);
            Stopwatch sw = Stopwatch.StartNew();
            if (!NeedsNeighborsToRender || chunk.HasAllAndRenderNeighbours())
            {
                var result = ChunkMesher.MeshChunk(chunk, workerId, out var chunkData);

                if (result == ChunkMeshingStatus.Succeded && chunkData != null)
                {
                    chunk.Status = ChunkStatus.QueuedToUpload;
                    UploadQueue.Enqueue(chunkData);
                }
                else
                {
                    chunk.Status = ChunkStatus.FailedToMesh;
                }
            }
            else
            {
                chunk.Status = ChunkStatus.FailedToMesh;
            }
            WorldNodeEditor.ChunkRenderingTimer.AddSample(sw.Elapsed.TotalMilliseconds);
        }
        long end = Stopwatch.GetTimestamp();
        long elapsedTicks = end - start;
        Interlocked.Add(ref RenderingThreadTimes[workerId], elapsedTicks);
    }

    public void HandleRendering2(int workerId)
    {
        long start = Stopwatch.GetTimestamp();
        if (TryDequeueRendering(out var chunk))
        {
            Interlocked.Increment(ref WorldNodeEditor.RenderingCount);
            Stopwatch sw = Stopwatch.StartNew();
            if (!NeedsNeighborsToRender || chunk.HasAllAndRenderNeighbours())
            {
                var result = ChunkMesher.MeshChunk2(chunk, workerId);

                if (result == ChunkMeshingStatus.Succeded)
                {
                    chunk.Status = ChunkStatus.QueuedToUpload;
                }
                else
                {
                    chunk.Status = ChunkStatus.FailedToMesh;
                }
            }
            else
            {
                chunk.Status = ChunkStatus.FailedToMesh;
            }
            WorldNodeEditor.ChunkRenderingTimer.AddSample(sw.Elapsed.TotalMilliseconds);
        }
        long end = Stopwatch.GetTimestamp();
        long elapsedTicks = end - start;
        Interlocked.Add(ref RenderingThreadTimes[workerId], elapsedTicks);
    }
    #endregion


    #region Threads
    private CancellationTokenSource _cts = new();

    private Task _renderDistanceWorker = null!;
    private Task[] _generationWorkers = null!;
    private Task[] _renderingWorkers = null!;
    
    public void StartWorkers(int count = -1)
    {
        if (count <= 0)
            count = Math.Max(1, Environment.ProcessorCount - 1);

        _cts = new CancellationTokenSource();

        _generationWorkers = new Task[GenerationThreads];
        _renderingWorkers = new Task[RenderingThreads];

        for (int i = 0; i < GenerationThreads; i++)
        {
            int workerId = i;
            _generationWorkers[i] = Task.Factory.StartNew(() => GenerationWorkerLoop(workerId), _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        for (int i = 0; i < RenderingThreads; i++)
        {
            int workerId = i;
            _renderingWorkers[i] = Task.Factory.StartNew(() => RenderingWorkerLoop(workerId), _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }

    public void StopWorkers()
    {
        if (_generationWorkers == null && _renderingWorkers == null)
            return;

        _cts.Cancel();

        try
        {
            if (_generationWorkers != null)
                Task.WaitAll(_generationWorkers);

            if (_renderingWorkers != null)
                Task.WaitAll(_renderingWorkers);
        }
        catch (AggregateException)
        {
            
        }

        _generationWorkers = null!;
        _renderingWorkers = null!;

        _cts.Dispose();
    }
    #endregion




    #region Uploading
    public ConcurrentQueue<VoxelChunkData> UploadQueue = [];

    public void HandleUploads()
    {
        DataPool.HandleUploads();

        if (!UploadQueue.IsEmpty)
        {
            const double MaxUploadTimeMs = 2;

            int uploaded = 0;
            var sw = Stopwatch.StartNew();

            while (UploadQueue.TryDequeue(out var chunkData))
            {
                WorldNodeEditor.UploadCount++;
                ChunkMesher.UploadChunk(chunkData);
                
                uploaded++;

                if (sw.Elapsed.TotalMilliseconds > MaxUploadTimeMs)
                    break;
            }

            WorldNodeEditor.TotalUploadTimer.AddSample(sw.Elapsed.TotalMilliseconds);

            Info.ThreadPoolQueueCount = uploaded;
        }
    }
    #endregion


    #region Reuse
    public ConcurrentQueue<VoxelChunk> ReuseQueue = [];

    public void HandleReuses()
    {
        if (!ReuseQueue.IsEmpty)
        {
            const double MaxUploadTimeMs = 2;

            int uploaded = 0;
            var sw = Stopwatch.StartNew();

            while (ReuseQueue.TryDequeue(out var chunk))
            {
                chunk.FreeAllocation();
                _genQueue.Enqueue(chunk);
                _generationSignal.Release();

                if (sw.Elapsed.TotalMilliseconds > MaxUploadTimeMs)
                    break;
            }
        }
    }
    #endregion


    #region Deletion
    public ConcurrentQueue<VoxelChunk> DeletionQueue = [];

    public void HandleDeletions()
    {
        if (!DeletionQueue.IsEmpty)
        {
            const double MaxUploadTimeMs = 2;

            int uploaded = 0;
            var sw = Stopwatch.StartNew();

            while (DeletionQueue.TryDequeue(out var chunk))
            {
                chunk.FreeAllocation();
                
                uploaded++;

                if (sw.Elapsed.TotalMilliseconds > MaxUploadTimeMs)
                    break;
            }
        }
    }
    #endregion




    #region Initializing
    public static Shader TestPrePassShader = null!;

    public static int PrePassView = -1;
    public static int PrePassProjection = -1;




    // Wireframe world shader
    public static Shader WireframeWorldShader = null!;

    public static int WireframeWorldViewLocation = -1;
    public static int WireframeWorldProjectionLocation = -1;


    
    // Blank world shader
    public static Shader BlankWorldShader = null!;

    public static int BlankWorldViewLocation = -1;
    public static int BlankWorldProjectionLocation = -1;


    public static ComputeShader WorldGenerationShader = null!;
    public static int WorldGenerationChunkPositionLocation;

    public static void Init()
    {
        TestPrePassShader = new(new()
        {
            VertexShaderFile = "world_vulkan/indirect-world.vert"
        });
        TestPrePassShader.Compile();

        PrePassView = TestPrePassShader.GetLocation("ubo.view");
        PrePassProjection = TestPrePassShader.GetLocation("ubo.proj");


        {
            // Wireframe world shader
            ShaderInfo wireframeWorldShaderInfo = new()
            {
                VertexShaderFile = "world_vulkan_new/indirect-world-greedy-wireframe.vert", 
                FragmentShaderFile = "world_vulkan_new/indirect-world-greedy-wireframe.frag",
            };

            wireframeWorldShaderInfo.Rasterizer.PolygonMode = PolygonMode.Line;
            wireframeWorldShaderInfo.Rasterizer.CullMode = CullModeFlags.None;
            wireframeWorldShaderInfo.Rasterizer.FrontFace = FrontFace.CounterClockwise;
            
            WireframeWorldShader = new(wireframeWorldShaderInfo);

            WireframeWorldShader.Compile();

            WireframeWorldViewLocation = WireframeWorldShader.GetLocation("ubo.view");
            WireframeWorldProjectionLocation = WireframeWorldShader.GetLocation("ubo.proj");
        }



        // Blank world shader
        ShaderInfo blankInfo = new()
        {
            VertexShaderFile = "world_vulkan/indirect-world-blank.vert", 
            FragmentShaderFile = "world_vulkan/indirect-world-blank.frag",
        };
        blankInfo.Rasterizer.CullMode = CullModeFlags.FrontBit;
        BlankWorldShader = new(blankInfo);
        BlankWorldShader.Compile();

        BlankWorldViewLocation = BlankWorldShader.GetLocation("ubo.view");
        BlankWorldProjectionLocation = BlankWorldShader.GetLocation("ubo.proj");

        ComputeShaderInfo worldGenerationInfo = new()
        {
            ComputeShaderPath = Game.ShaderPath / "computeShaders" / "world_vulkan" / "newWorldFinal.comp"
        };

        WorldGenerationShader = new(worldGenerationInfo);

        WorldGenerationShader.Compile();

        WorldGenerationChunkPositionLocation = WorldGenerationShader.GetLocation("ubo.uChunkWorldPosition");

                /*
        _uiPlaneShader = new(new()
        {
            VertexShaderFile = "vulkan/fullScreen.vert",
            FragmentShaderFile = "vulkan/fullScreen.frag"
        });
        _uiPlaneShader.Compile();

        _uiPlaneDescriptor = _uiPlaneShader.GetDescriptorSet();
        _uiPlaneDescriptor.BindFramebufferColor(MiddleFBO, 0);
        

        
        debugModule = new();

        debugModule.AddGrid(new Vector3(0, 0, 0), Vector3.UnitX, Vector3.UnitY, new Vector2(32, 32), new Vector2(4, 4), (0, 0), new Vector3(1, 0, 0));
        debugModule.AddGrid(new Vector3(0, 32, 0), Vector3.UnitX, Vector3.UnitY, new Vector2(32, 32), new Vector2(4, 4), (0, 0), new Vector3(1, 0, 0));

        debugModule.AddGrid(new Vector3(0, 0, 0), Vector3.UnitZ, Vector3.UnitX, new Vector2(32, 32), new Vector2(4, 4), (0, 0), new Vector3(1, 0, 0), IncludedBorder.Left | IncludedBorder.Right);
        debugModule.AddGrid(new Vector3(32, 0, 0), Vector3.UnitZ, Vector3.UnitX, new Vector2(32, 32), new Vector2(4, 4), (0, 0), new Vector3(1, 0, 0), IncludedBorder.Left | IncludedBorder.Right);

        debugModule.AddGrid(new Vector3(0, 0, 0), Vector3.UnitX, Vector3.UnitZ, new Vector2(32, 32), new Vector2(4, 4), (0, 0), new Vector3(1, 0, 0), IncludedBorder.None);
        debugModule.AddGrid(new Vector3(0, 0, 32), Vector3.UnitX, Vector3.UnitZ, new Vector2(32, 32), new Vector2(4, 4), (0, 0), new Vector3(1, 0, 0), IncludedBorder.None);

        debugModule.AddGrid(new Vector3(0, 0, 0), Vector3.UnitX, Vector3.UnitY, new Vector2(32, 32), new Vector2(4, 4), (2, 2), (0, 1, 0), IncludedBorder.All);
        debugModule.AddGrid(new Vector3(0, 32, 0), Vector3.UnitX, Vector3.UnitY, new Vector2(32, 32), new Vector2(4, 4), (2, 2), (0, 1, 0), IncludedBorder.All);

        debugModule.AddGrid(new Vector3(0, 0, 0), Vector3.UnitZ, Vector3.UnitX, new Vector2(32, 32), new Vector2(4, 4), (2, 2), (0, 1, 0), IncludedBorder.All);
        debugModule.AddGrid(new Vector3(32, 0, 0), Vector3.UnitZ, Vector3.UnitX, new Vector2(32, 32), new Vector2(4, 4), (2, 2), (0, 1, 0), IncludedBorder.All);

        debugModule.AddGrid(new Vector3(0, 0, 0), Vector3.UnitX, Vector3.UnitZ, new Vector2(32, 32), new Vector2(4, 4), (2, 2), (0, 1, 0), IncludedBorder.All);
        debugModule.AddGrid(new Vector3(0, 0, 32), Vector3.UnitX, Vector3.UnitZ, new Vector2(32, 32), new Vector2(4, 4), (2, 2), (0, 1, 0), IncludedBorder.All);
        
        debugModule.Generate();
        */
        


        /*
        PBGConsole.AddCommand(new("info", [
            new("datapool", [
                new("count", null, c => {
                    var renderer = Scene.CurrentScene?.QueryComponent<NewVoxelRenderer>();
                    if (renderer != null)
                    {
                        return new(true, "There are " + renderer.DataPool.DataPool.Count + " datapools");
                    }
                    else
                    {
                        return new(false, "There doesn't seem to be an active renderer");
                    }
                })
            ])
        ]));
        
        PBGConsole.AddCommand(new("settings", [
            new("world", [
                new("render_distance", null, c => {
                    var renderer = Scene.CurrentScene?.QueryComponent<NewVoxelRenderer>();
                    if (renderer != null)
                    {
                        if (!c.HasToken())
                            return new(false, "Expected code after \"" + c.LastToken() + "\"");

                        var token = c.CurrentToken();
                        int value = Parse.Int.Parse(token);

                        renderer.RenderDistance = value;
                        renderer.GenerateChunkMap();
                        
                        return new(true, $"Set render distance to {value}");
                    }
                    else
                    {
                        return new(false, "There doesn't seem to be an active renderer");
                    }
                })
            ]),
            new("performance", [
                new("generation_count", null, c => {
                    var renderer = Scene.CurrentScene?.QueryComponent<NewVoxelRenderer>();
                    if (renderer != null)
                    {
                        if (!c.HasToken())
                            return new(false, "Expected code after \"" + c.LastToken() + "\"");

                        var token = c.CurrentToken();
                        int value = Parse.Int.Parse(token);

                        renderer.MaxChunkGenerationPerFrame = value;
                        
                        return new(true, $"Set max chunk generation count per frame to {value}");
                    }
                    else
                    {
                        return new(false, "There doesn't seem to be an active renderer");
                    }
                }),
                new("meshing_count", null, c => {
                    var renderer = Scene.CurrentScene?.QueryComponent<NewVoxelRenderer>();
                    if (renderer != null)
                    {
                        if (!c.HasToken())
                            return new(false, "Expected code after \"" + c.LastToken() + "\"");

                        var token = c.CurrentToken();
                        int value = Parse.Int.Parse(token);

                        renderer.MaxChunkBuildingPerFrame = value;
                        
                        return new(true, $"Set max chunk meshing queue count per frame to {value}");
                    }
                    else
                    {
                        return new(false, "There doesn't seem to be an active renderer");
                    }
                })
            ])
        ]));
        */
    }
    #endregion
}