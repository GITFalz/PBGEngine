
using PBG.Graphics;
using PBG.Core;
using PBG.Threads;
using System.Diagnostics.CodeAnalysis;
using PBG.Rendering;
using PBG.Data;
using PBG.MathLibrary;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Silk.NET.Vulkan;

namespace PBG.Voxel
{
    public abstract class VoxelRendererGenerator
    {
        public abstract void GenerateChunk(VoxelRenderer renderer);
    }

    public struct ChunkInfo 
    {
        public static readonly uint ByteSize = (uint)Marshal.SizeOf<ChunkInfo>();

        public Vector3 Center;
        public float Radius;
        public uint DataOffset;
        public uint VertexCount;
        public uint Active;
        public int SlotIndex;
    };

    public class VoxelRenderer : ScriptingNode, IVoxelRenderer
    {
        private static bool _started = false;

        public static Shader TestPrePassShader = null!;

        public static int PrePassView = -1;
        public static int PrePassProjection = -1;


        public static Shader WorldShader = null!;

        public static int WorldViewLocation = -1;
        public static int WorldProjectionLocation = -1;

        public static int WorldCloseLightSpaceMatrixLocation = -1;
        public static int WorldMiddleLightSpaceMatrixLocation = -1;
        public static int WorldFarLightSpaceMatrixLocation = -1;

        public static int WorldLightDirectionLocation = -1;
        
        public static int WorldCloseLightDirectionLocation = -1;
        public static int WorldMiddleLightDirectionLocation = -1;
        public static int WorldFarLightDirectionLocation = -1;

        public static int WorldDoRealtimeShadowsLocation = -1;
        public static int WorldDoAmbientOcclusionLocation = -1;
        public static int WorldPlayerPositionLocation = -1;
        public static int WorldTimeLocation = -1;

        float dayLengthSeconds = 60f; // 10 minutes per full day


        public static Shader BlankWorldShader = null!;

        public static int BlankWorldViewLocation = -1;
        public static int BlankWorldProjectionLocation = -1;

        public Skybox? skybox;


        private static Shader? _uiPlaneShader;
        private static Descriptor _uiPlaneDescriptor;

        public ChunkDataPool DataPool;
        

        /*
        public readonly static ShaderProgram WorldShader = new ShaderProgram("world/world.vert", "world/world.frag");
        public readonly static ShaderProgram BaseShader = new ShaderProgram("world/world_base.vert", "world/world_base.frag");
        
        public readonly static ShaderProgram TestShader = new ShaderProgram("world/indirect-word.vert", "world/indirect-world.frag");

        

        public static readonly int View = TestShader.GetLocation("uView");
        public static readonly int Projection = TestShader.GetLocation("uProjection");
        public static readonly int Model = TestShader.GetLocation("uModel");

        public static readonly int Texture = TestShader.GetLocation("textureArray");
        public static readonly int LightDirectionLocation = TestShader.GetLocation("lightDirection");
        public static readonly int DoAmbientOcclusion = TestShader.GetLocation("uDoAmbientOcclusion");
        public static readonly int CameraPosition = TestShader.GetLocation("uCameraPosition");

        

        public static class BaseShaderLocation
        {
            public readonly static int View = BaseShader.GetLocation("uView");
            public readonly static int Projection = BaseShader.GetLocation("uProjection");
            public readonly static int Model = BaseShader.GetLocation("uModel");

            public readonly static int Texture = BaseShader.GetLocation("textureArray");
            public readonly static int LightDirection = BaseShader.GetLocation("lightDirection");
        }

        public static class BlankShaderLocation
        {
            public readonly static int View = BlankShader.GetLocation("uView");
            public readonly static int Projection = BlankShader.GetLocation("uProjection");
            public readonly static int Model = BlankShader.GetLocation("uModel");
        }
        */
    

        public HashSet<VoxelChunk> VoxelChunkInstances = [];

        public Dictionary<Vector3i, VoxelChunk> ChunkDictionary = [];
        public HashSet<Vector3i> ChunkRelativePositions = [];
        public List<VoxelChunk> Chunks = [];
        public List<VoxelChunk> VisibleChunks = [];

        public HashSet<VoxelChunk> RerenderMap = [];
        public HashSet<VoxelChunk> FreedMap = [];

        public Queue<VoxelChunk> GenerationQueue = [];
        public Queue<VoxelChunk> PopulationQueue = [];
        public LinkedList<VoxelChunk> RenderingQueue = [];
        public LinkedList<VoxelChunk> RerenderingQueue = [];
        public Queue<VoxelChunk> ToBeFreedQueue = [];

        private bool _enableTerrainGeneration = true;

        public int RenderDistance = 16;
        public int MaxVerticalChunks = 8;

        public int MaxChunkGenerationPerFrame = 7;
        public int MaxChunkBuildingPerFrame = 7;

        public VoxelRendererGenerator ChunkGenerator = new BaseVoxelRendererGenerator();

        private Camera _camera;

        private Vector3i _currentChunk = Vector3i.Zero;
        private Action _chunkOffsetAction;
        private (int left, int right, int bottom, int top) _viewport;
        private int _width;
        private int _height;

        public bool Run = true;
        public bool GenerateChunks = true;

        public bool AmbientOcclusion = true;
        public bool RealtimeShadows = true;
        public bool NeedsNeighborsToRender = true;

        private Vector3 _lightUp;
        public Vector3 LightDirection;
        public float Time = 0;
        public Matrix4 ProjectionMatrix;

        public static FBO CloseFBO;
        public static FBO MiddleFBO;
        public static FBO FarFBO;

        private Matrix4 _closeLightSpaceMatrix;
        private Matrix4 _middleLightSpaceMatrix;
        private Matrix4 _farLightSpaceMatrix;

        private float _closeLightTimer = 0.1f;
        private float _middleLightTimer = 0.25f;
        private float _farLightTimer = 0.5f;


        public float closeTimer = 0.05f;
        public float middleTimer = 0.2f;
        public float farTimer = 0.5f;


        public int RenderedChunks = 0;

        public int Counter = 0;

        public RollingAverageTimer RenderingTimer = new();

        public VoxelRenderer()
        {
            Init();
            _chunkOffsetAction = GenerateDistanceBasedChunkOffsets;
            _viewport = (0, 0, 0, 0);
            _width = Game.Width;
            _height = Game.Height;
            _camera = new Camera(Game.Width, Game.Height, (0, 0, 0));
            ProjectionMatrix = _camera.GetProjectionMatrix();

            DataPool = new(this);
        }

        public VoxelRenderer(VoxelRendererSettings settings)
        {
            Init();
            _chunkOffsetAction = settings.GenerationType switch
            {
                VoxelRendererGenerationType.Distance => GenerateDistanceBasedChunkOffsets,
                VoxelRendererGenerationType.Cube => GenerateCubeChunkOffsets,
                _ => throw new ArgumentOutOfRangeException(settings.GenerationType + " doesn't exists")
            };

            _enableTerrainGeneration = settings.EnableTerrainGeneration;

            RenderDistance = settings.RenderDistance;
            MaxVerticalChunks = settings.MaxVerticalChunks;

            _viewport = settings.Viewport;

            _width = Game.Width - (_viewport.left + _viewport.right);
            _height = Game.Height - (_viewport.bottom + _viewport.top);
            _camera = new Camera(_width, _height, (0, 0, 0));
            ProjectionMatrix = _camera.GetProjectionMatrix();

            DataPool = new(this);
        }

        private void Init()
        {
            if (!_started)
            {
                TestPrePassShader = new(new()
                {
                    VertexShaderPath = Game.ShaderPath / "world_vulkan/indirect-world.vert"
                });
                TestPrePassShader.Compile();

                PrePassView = TestPrePassShader.GetLocation("ubo.view");
                PrePassProjection = TestPrePassShader.GetLocation("ubo.proj");

                WorldShader = new(new()
                {
                    VertexShaderPath = Game.ShaderPath / "world_vulkan/indirect-world.vert", 
                    FragmentShaderPath = Game.ShaderPath / "world_vulkan/indirect-world.frag",
                });
                WorldShader.Compile();

                WorldViewLocation = WorldShader.GetLocation("ubo.view");
                WorldProjectionLocation = WorldShader.GetLocation("ubo.proj");

                WorldCloseLightSpaceMatrixLocation = WorldShader.GetLocation("ubo.uCloseLightSpaceMatrix");
                WorldMiddleLightSpaceMatrixLocation = WorldShader.GetLocation("ubo.uMiddleLightSpaceMatrix");
                WorldFarLightSpaceMatrixLocation = WorldShader.GetLocation("ubo.uFarLightSpaceMatrix");

                WorldLightDirectionLocation = WorldShader.GetLocation("data.lightDirection");

                WorldCloseLightDirectionLocation = WorldShader.GetLocation("data.closeLightDirection");
                WorldMiddleLightDirectionLocation = WorldShader.GetLocation("data.middleLightDirection");
                WorldFarLightDirectionLocation = WorldShader.GetLocation("data.farLightDirection");

                WorldDoRealtimeShadowsLocation = WorldShader.GetLocation("data.uDoRealtimeShadows");
                WorldDoAmbientOcclusionLocation = WorldShader.GetLocation("data.uDoAmbientOcclusion");
                WorldPlayerPositionLocation = WorldShader.GetLocation("data.uPlayerPosition");
                WorldTimeLocation = WorldShader.GetLocation("data.time");


                ShaderInfo blankInfo = new()
                {
                    VertexShaderPath = Game.ShaderPath / "world_vulkan/indirect-world-blank.vert", 
                    FragmentShaderPath = Game.ShaderPath / "world_vulkan/indirect-world-blank.frag",
                };
                blankInfo.Rasterizer.CullMode = CullModeFlags.FrontBit;
                BlankWorldShader = new(blankInfo);
                BlankWorldShader.Compile();

                BlankWorldViewLocation = BlankWorldShader.GetLocation("ubo.view");
                BlankWorldProjectionLocation = BlankWorldShader.GetLocation("ubo.proj");

                CloseFBO = new FBO(4000, 4000);
                MiddleFBO = new FBO(3000, 3000);
                FarFBO = new FBO(2000, 2000);

                _uiPlaneShader = new(new()
                {
                    VertexShaderPath = Path.Combine(Game.ShaderPath, "vulkan/fullScreen.vert"),
                    FragmentShaderPath = Path.Combine(Game.ShaderPath, "vulkan/fullScreen.frag")
                });
                _uiPlaneShader.Compile();

                _uiPlaneDescriptor = _uiPlaneShader.GetDescriptorSet();
                _uiPlaneDescriptor.BindFramebufferColor(MiddleFBO, 0);

                _started = true;
            }
        }

        public Camera GetCamera() => Camera;

        public void GenerateDistanceBasedChunkOffsets()
        {
            ChunkRelativePositions = [];
            List<Vector3i> chunkPositions = [];

            for (int dx = -RenderDistance; dx <= RenderDistance; dx++)
            {
                for (int dz = -RenderDistance; dz <= RenderDistance; dz++)
                {
                    int distSq = dx * dx + dz * dz;
                    if (distSq > RenderDistance * RenderDistance) continue;

                    for (int dy = 0; dy <= MaxVerticalChunks; dy++)
                    {
                        chunkPositions.Add((dx, dy, dz));
                    }
                }
            }

            chunkPositions.Sort((a, b) => Vector2.DistanceSquared(a.Xz, Vector2.Zero).CompareTo(Vector2.DistanceSquared(b.Xz, Vector2.Zero)));

            for (int i = 0; i < chunkPositions.Count; i++)
            {
                ChunkRelativePositions.Add(chunkPositions[i]);
            }
        }

        public void GenerateCubeChunkOffsets()
        {
            ChunkRelativePositions = [];
            List<Vector3i> chunkPositions = [];

            for (int dx = -RenderDistance; dx <= RenderDistance; dx++)
            {
                for (int dz = -RenderDistance; dz <= RenderDistance; dz++)
                {
                    for (int dy = 0; dy <= MaxVerticalChunks; dy++)
                    {
                        chunkPositions.Add((dx, dy, dz));
                    }
                }
            }

            chunkPositions.Sort((a, b) => Vector2.DistanceSquared(a.Xz, Vector2.Zero).CompareTo(Vector2.DistanceSquared(b.Xz, Vector2.Zero)));

            for (int i = 0; i < chunkPositions.Count; i++)
            {
                ChunkRelativePositions.Add(chunkPositions[i]);
            }
        }

        public VoxelChunk? GetChunk(Vector3i position)
        {
            if (ChunkDictionary.TryGetValue(position, out var chunk))
                return chunk;
            return null;
        }

        public bool GetChunk(Vector3i position, [NotNullWhen(true)] out VoxelChunk? chunk)
        {
            if (!ChunkDictionary.TryGetValue(position, out chunk))
                return false;

            return chunk != null;
        }

        public bool GetBlockState(Vector3i blockPosition, out Block block)
        {
            block = Block.Air;
            Vector3i chunkPosition = VoxelData.BlockToChunkRelative(blockPosition);

            if (!GetChunk(chunkPosition, out VoxelChunk? chunk))
                return false;

            block = chunk.GetInner(VoxelData.BlockToRelative(blockPosition));
            return true;
        }

        public bool IsAir_Fast(Vector3i blockPosition) => IsAir_Fast(blockPosition, true);
        public bool IsAir_Fast(Vector3i blockPosition, bool ifNoChunk)
        {
            if (!GetChunk(VoxelData.BlockToChunkRelative(blockPosition), out VoxelChunk? chunk))
                return ifNoChunk;

            return chunk.IsAir(VoxelData.BlockToRelative(blockPosition));
        }

        public bool IsSolid_Fast(Vector3i blockPosition) => IsSolid_Fast(blockPosition, true);
        public bool IsSolid_Fast(Vector3i blockPosition, bool ifNoChunk)
        {
            if (!GetChunk(VoxelData.BlockToChunkRelative(blockPosition), out VoxelChunk? chunk))
                return ifNoChunk;

            return chunk.IsSolid(VoxelData.BlockToRelative(blockPosition));
        }

        public bool GetBlock(Vector3i blockPosition, out Block block)
        {
            GetBlockState(blockPosition, out block);
            return block.IsSolid();
        }

        public Block GetBlock(Vector3i blockPosition)
        {
            GetBlockState(blockPosition, out var block);
            return block;
        }

        public bool SetBlock(int x, int y, int z, Block block) => SetBlock((x, y, z), block, out _);
        public bool SetBlock(int x, int y, int z, Block block, [NotNullWhen(true)] out VoxelChunk? chunk) => SetBlock((x, y, z), block, out chunk);
        public bool SetBlock(Vector3i blockPosition, Block block) => SetBlock(blockPosition, block, out _);
        public bool SetBlock(Vector3i blockPosition, Block block, [NotNullWhen(true)] out VoxelChunk? chunk)
        {
            Vector3i chunkPosition = VoxelData.BlockToChunkRelative(blockPosition);
            if (!GetChunk(chunkPosition, out chunk))
                return false;

            Vector3i relative = VoxelData.BlockToRelative(blockPosition);
            chunk.Set(relative, block);

            //Console.WriteLine("Set block at " + blockPosition + " to " + block + " in chunk " + chunk.RelativePosition + " at relative " + relative);

            int oX = ((relative.X & 3) >> 1) * 2 - 1;
            int oY = ((relative.Y & 3) >> 1) * 2 - 1;
            int oZ = ((relative.Z & 3) >> 1) * 2 - 1;

            HashSet<VoxelChunk> updatedChunks = [];

            void Update(Vector3i blockPos)
            {
                var pos = VoxelData.BlockToChunkRelative(blockPos);
                if (GetChunk(pos, out var chunk) && updatedChunks.Add(chunk))
                {
                    if (chunk.Process != null)
                    {
                        chunk.Restart = true;
                        chunk.Process.Break();
                    }
                    else if (RerenderMap.Add(chunk)) 
                    {
                        RerenderingQueue.AddLast(chunk);
                    }
                }
            }

            Update(blockPosition);
            Update(blockPosition + (oX, 0, 0));
            Update(blockPosition + (0, oY, 0));
            Update(blockPosition + (oX, oY, 0));
            Update(blockPosition + (0, 0, oZ));
            Update(blockPosition + (oX, 0, oZ));
            Update(blockPosition + (0, oY, oZ));
            Update(blockPosition + (oX, oY, oZ));

            return true;
        }

        public void ChunkCheck(Vector3i playerChunkPosition)
        {
            for (int i = 0; i < Chunks.Count; i++)
            {
                var chunk = Chunks[i];
                if (ChunkRelativePositions.Contains(chunk.RelativePosition - playerChunkPosition))
                    continue;

                if (RemoveChunk(chunk.RelativePosition))
                    i--;

            }
            foreach (var c in ChunkRelativePositions)
            {
                var position = c + playerChunkPosition;
                if (ChunkDictionary.ContainsKey(position))
                    continue;

                AddChunk(position);
            }
        }

        public void AddChunk(Vector3i relativePosition)
        {
            VoxelChunk chunk = new(this, relativePosition);

            VoxelChunkInstances.Add(chunk);
            if (!ChunkDictionary.TryAdd(relativePosition, chunk))
                return;

            Chunks.Add(chunk);
            if (!chunk.ToBeRemoved)
                GenerationQueue.Enqueue(chunk);
        }

        public bool RemoveChunk(Vector3i relativePosition)
        {
            if (!ChunkDictionary.TryGetValue(relativePosition, out var chunk))
                return false;

            chunk.ToBeRemoved = true;
            chunk.BreakProcess();
            ChunkDictionary.Remove(relativePosition);
            Chunks.Remove(chunk);
            VisibleChunks.Remove(chunk);

            RenderingQueue.Remove(chunk);
            RerenderingQueue.Remove(chunk);
            RerenderMap.Remove(chunk);

            if (FreedMap.Add(chunk))
                ToBeFreedQueue.Enqueue(chunk);

            return true;
        }

        void Start()
        {
            skybox = Transform.TryGetComponent<Skybox>();
        }

        void Awake()
        {
            Vector3i newPosition = VoxelData.BlockToChunkRelative(Mathf.FloorToInt(Transform.Position));
            _currentChunk.Xz = Mathf.FloorToInt(newPosition.Xz);

            _chunkOffsetAction.Invoke();
            ChunkCheck(_currentChunk);

            _width = Game.Width - (_viewport.left + _viewport.right);
            _height = Game.Height - (_viewport.bottom + _viewport.top);
            _camera = new Camera(_width, _height, (0, 0, 0));
            ProjectionMatrix = _camera.GetProjectionMatrix();
        }

        public void Restart()
        {
            GenerateChunks = true;
            Awake();
        }

        void Resize()
        {
            _width = Game.Width - (_viewport.left + _viewport.right);
            _height = Game.Height - (_viewport.bottom + _viewport.top);
            _camera = new Camera(_width, _height, (0, 0, 0));
            ProjectionMatrix = _camera.GetProjectionMatrix();
        }

        private float _oldGameTime = 0f;

        void Update()
        {
            if (!Run) return;
            
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
            
            //Info.SetGenerationQueueCount(GenerationQueue.Count);

            if (RenderingQueue.Count > 0)
            {
                for (int i = 0; i < MaxChunkBuildingPerFrame.Min(RenderingQueue.Count); i++)
                {
                    var chunk = RenderingQueue.First;
                    if (chunk != null)
                    {
                        if (!ChunkDictionary.ContainsKey(chunk.Value.RelativePosition))
                        {
                            RenderingQueue.Remove(chunk);
                            continue;
                        }

                        if (!chunk.Value.ToBeRemoved && (!NeedsNeighborsToRender || chunk.Value.HasAllNeighbourChunks()))
                        {
                            //RenderingTimer.Start();
                            DefaultChunkRenderingProcess renderingProcess = new DefaultChunkRenderingProcess(chunk.Value);
                            //renderingProcess.Function();
                            //renderingProcess.OnCompleteBase();
                            //RenderingTimer.End();
                            TaskPool.QueueAction(renderingProcess, TaskPriority.High);
                            RenderingQueue.Remove(chunk);
                            
                            Counter++;
                        }
                        else
                        {
                            RenderingQueue.Remove(chunk);
                            RenderingQueue.AddLast(chunk);
                        }
                    }
                }
                
                //Info.SetRenderingQueueCount(RenderingQueue.Count);
                //Info.AverageChunkRenderingSpeed(DefaultChunkRenderingProcess.Timer.GetAverageMs());
            }

            if (RerenderingQueue.Count > 0)
            {
                for (int i = 0; i < 2.Min(RerenderingQueue.Count); i++)
                {
                    var chunk = RerenderingQueue.First;
                    if (chunk != null)
                    {
                        if (!ChunkDictionary.ContainsKey(chunk.Value.RelativePosition))
                        {
                            RerenderingQueue.Remove(chunk);
                            continue;
                        }

                        if (!chunk.Value.ToBeRemoved && (!NeedsNeighborsToRender || chunk.Value.HasAllNeighbourChunks()))
                        {
                            /*
                            DefaultChunkRenderingProcess renderingProcess = new DefaultChunkRenderingProcess(chunk.Value);
                            TaskPool.QueueAction(renderingProcess, TaskPriority.Urgent);  
                            */

                            chunk.Value.Allocation.DataPool?.Free(chunk.Value);
                            DefaultChunkRenderingProcess renderingProcess = new DefaultChunkRenderingProcess(chunk.Value);
                            renderingProcess.SetThreadIndex(TaskPool.ThreadCount);
                            renderingProcess.Function();
                            renderingProcess.OnCompleteBase();

                            RerenderingQueue.Remove(chunk);

                            _oldVisibleChunkCount--;
                        }
                        else
                        {
                            RerenderingQueue.Remove(chunk);
                            RerenderingQueue.AddLast(chunk);
                        }
                    }
                }
            }

            if (_oldGameTime + 1f < GameTime.TotalTime)
            {
                _oldGameTime = GameTime.TotalTime;
            }

            if (ToBeFreedQueue.Count > 0)
            {
                for (int i = 0; i < 4.Min(ToBeFreedQueue.Count); i++)
                {
                    var chunk = ToBeFreedQueue.Dequeue();
                    VoxelChunkInstances.Remove(chunk);
                    RenderingQueue.Remove(chunk);
                    RerenderingQueue.Remove(chunk);
                    chunk.Status = ChunkStatus.Empty;
                    chunk.Dispose();
                    FreedMap.Remove(chunk);
                    CacheManager.RemoveChunk(chunk.WorldPosition.Xz);
                }
            }

            Time = Mathf.Fraction(GameTime.TotalTime / dayLengthSeconds);
            float angle = Time * 360f;
            LightDirection = Mathf.RotatePoint((0, 1, 0), (0, 0, 0), (0, 0, 1), angle);
            var right = Vector3.Normalize(Vector3.Cross(LightDirection, Vector3.UnitY));
            _lightUp = Vector3.Normalize(Vector3.Cross(right, LightDirection));

            if (skybox != null)
            {
                skybox.LightDirection = LightDirection;
                skybox.Time = Time;
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

        private int _oldVisibleChunkCount = 0;
        private Vector3 _oldCameraPosition = Vector3.Zero;
        private int _chunkCount = 0;

        void Render()
        {
            if (!Run) return;
            
            DataPool.Reset();
            if (RealtimeShadows)
            {
                if (_closeLightTimer >= 0.01f)
                {
                    RenderShadowMap(CloseFBO, 140, 140, 140, 1, out _closeLightSpaceMatrix);
                }

                if (_middleLightTimer >= 0.1f)
                {
                    RenderShadowMap(MiddleFBO, 640, 500, 500, 2, out _middleLightSpaceMatrix);
                }

                if (_farLightTimer >= 0.5f)
                {
                    RenderShadowMap(FarFBO, 2560, 2500, 1600, 3, out _farLightSpaceMatrix);
                }
            }

            GFX.Viewport(_viewport.left, _viewport.top, _width, _height);

            /*
            _uiPlaneShader.Bind();
            _uiPlaneDescriptor.Bind();

            GFX.Draw(3, 1, 0, 0);
            */
            
            if (DataPool.Updated || Input.MouseDelta != Vector2.Zero || _oldCameraPosition != Camera.Position || VisibleChunks.Count != _oldVisibleChunkCount)
            {
                DataPool.Updated = false;

                _chunkCount = 0;
                for (int i = 0; i < VisibleChunks.Count; i++)
                {
                    var chunk = VisibleChunks[i];
                    if (chunk.ForceDisabled || !Camera.FrustumIntersectsSphere(chunk.Center, 28))
                    {
                        chunk.Visible = false;
                        continue;
                    }

                    chunk.Visible = true;
                    chunk.Allocation.DataPool.UpdateDrawCommand(chunk, chunk.Allocation);
                    _chunkCount++;
                }

                DataPool.UpdateDrawCommands();
                    
                Info.SetChunkRenderCount(_chunkCount);
            }

            _oldVisibleChunkCount = VisibleChunks.Count;
            _oldCameraPosition = Camera.Position;

            RenderedChunks = _chunkCount;

            WorldShader.Bind();

            DataPool.Render();

            GFX.Viewport(0, 0, Game.Width, Game.Height);
        }

        public void UpdateUniforms(Descriptor descriptor)
        {
            descriptor.Uniform(WorldViewLocation, Camera.ViewMatrix);
            descriptor.Uniform(WorldProjectionLocation, ProjectionMatrix);
            descriptor.Uniform(WorldCloseLightSpaceMatrixLocation, _closeLightSpaceMatrix);
            descriptor.Uniform(WorldMiddleLightSpaceMatrixLocation, _middleLightSpaceMatrix);
            descriptor.Uniform(WorldFarLightSpaceMatrixLocation, _farLightSpaceMatrix);

            descriptor.Uniform(WorldLightDirectionLocation, LightDirection);
            descriptor.Uniform(WorldDoRealtimeShadowsLocation, RealtimeShadows ? 1 : 0);
            descriptor.Uniform(WorldDoAmbientOcclusionLocation, AmbientOcclusion ? 1 : 0);
            descriptor.Uniform(WorldPlayerPositionLocation, Camera.Position);
            descriptor.Uniform(WorldTimeLocation, Time);
        }

        public void RenderShadowMap(FBO fbo, float halfSize, float depth, float distance, int passIndex, out Matrix4 matrix)
        {
            fbo.Bind();

            Matrix4 view = Matrix4.CreateLookAt(Camera.Position, Camera.Position + LightDirection, _lightUp);
            Matrix4 projection = Matrix4.CreateOrthographicOffCenter(-halfSize / 2, halfSize / 2, halfSize / 2, -halfSize / 2, -depth, depth);

            for (int i = 0; i < VisibleChunks.Count; i++)
            {
                var chunk = VisibleChunks[i];
                if (!chunk.HasBlocks || Vector3.DistanceSquared(Camera.Position, chunk.Center) > distance * distance)
                    continue;

                chunk.Allocation.DataPool.UpdateDrawCommand(chunk, chunk.Allocation, passIndex);
            }

            BlankWorldShader.Bind();

            matrix = projection * view;
            DataPool.UpdateDescriptorUniform(descriptor => descriptor.Uniform3(WorldCloseLightDirectionLocation, LightDirection), passIndex);
            DataPool.UpdateDrawCommands(passIndex);  
            
            DataPool.RenderBlank(view, projection, passIndex);

            fbo.Unbind();
        }

        void Exit()
        {
            Clear();
        }

        void Dispose()
        {
            Clear();
        }

        public void Clear()
        {
            Console.WriteLine("Clearing voxel renderer");
            Console.WriteLine("--- Before ---");
            //BufferBase.PrintBufferCount();

            TaskPool.Clear();
            foreach (var (_, chunk) in ChunkDictionary)
            {
                chunk.Dispose();
            }

            ChunkDictionary = [];
            ChunkRelativePositions = [];
            Chunks = [];

            VoxelChunkInstances = [];
            GenerationQueue = [];
            PopulationQueue = [];
            RenderingQueue = [];
            RerenderingQueue = [];
            ToBeFreedQueue = [];

            RerenderMap = [];
            FreedMap = [];

            CacheManager.Clear();
            DataPool.Dispose();

            Console.WriteLine("--- After ---");
            //BufferBase.PrintBufferCount();
        }

        private class BaseVoxelRendererGenerator : VoxelRendererGenerator
        {
            public override void GenerateChunk(VoxelRenderer renderer) { }
        }
    }

    public struct VoxelRendererSettings
    {
        public VoxelRendererGenerationType GenerationType = VoxelRendererGenerationType.Distance;
        public bool EnableTerrainGeneration = true;
        public int RenderDistance = 10;
        public int MaxVerticalChunks = 16;
        public (int left, int right, int bottom, int top) Viewport;

        public VoxelRendererSettings()
        {
            Viewport = (0, 0, 0, 0);
        }
    }

    public enum VoxelRendererGenerationType
    {
        Distance,
        Cube
    }
}