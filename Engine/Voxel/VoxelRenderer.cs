
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

    public class VoxelRenderer : ScriptingNode
    {
        private static bool _started = false;

        public static Shader TestPrePassShader = null!;

        public static int PrePassView = -1;
        public static int PrePassProjection = -1;


        public static Shader TestShader = null!;

        public static int View = -1;
        public static int Projection = -1;

        public static int LightDirectionLocation = -1;
        public static int DoAmbientOcclusion = -1;
        public static int CameraPosition = -1;


        public static ComputeShader IndirectCompute;

        public static int uPlanesLocation = -1;
        public static int uMaxSlotsLocation = -1;


        /*
        public readonly static ShaderProgram WorldShader = new ShaderProgram("world/world.vert", "world/world.frag");
        public readonly static ShaderProgram BaseShader = new ShaderProgram("world/world_base.vert", "world/world_base.frag");
        public readonly static ShaderProgram BlankShader = new ShaderProgram("world/world_blank.vert", "world/world_blank.frag");
        public readonly static ShaderProgram TestShader = new ShaderProgram("world/indirect-word.vert", "world/indirect-world.frag");

        

        public static readonly int View = TestShader.GetLocation("uView");
        public static readonly int Projection = TestShader.GetLocation("uProjection");
        public static readonly int Model = TestShader.GetLocation("uModel");

        public static readonly int Texture = TestShader.GetLocation("textureArray");
        public static readonly int LightDirectionLocation = TestShader.GetLocation("lightDirection");
        public static readonly int DoAmbientOcclusion = TestShader.GetLocation("uDoAmbientOcclusion");
        public static readonly int CameraPosition = TestShader.GetLocation("uCameraPosition");

        public static class WorldShaderLocation
        {
            public static readonly int View = WorldShader.GetLocation("uView");
            public static readonly int Projection = WorldShader.GetLocation("uProjection");
            public static readonly int Model = WorldShader.GetLocation("uModel");

            public static readonly int Texture = WorldShader.GetLocation("textureArray");
            public static readonly int LightDirection = WorldShader.GetLocation("lightDirection");

            public static readonly int CameraPosition = WorldShader.GetLocation("uCameraPosition");
            public static readonly int PlayerPosition = WorldShader.GetLocation("uPlayerPosition");
            public static readonly int AmbientOcclusionTexture = WorldShader.GetLocation("ambientOcclusionTexture");
            public static readonly int DoAmbientOcclusion = WorldShader.GetLocation("uDoAmbientOcclusion");

            public static readonly int CloseLightSpaceMatrix = WorldShader.GetLocation("uCloseLightSpaceMatrix");
            public static readonly int MiddleLightSpaceMatrix = WorldShader.GetLocation("uMiddleLightSpaceMatrix");
            public static readonly int CloseShadowMap = WorldShader.GetLocation("uCloseShadowMap");
            public static readonly int MiddleShadowMap = WorldShader.GetLocation("uMiddleShadowMap");
            public static readonly int DoRealtimeShadows = WorldShader.GetLocation("uDoRealtimeShadows");
        }

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
        public bool RealtimeShadows = false;
        public bool NeedsNeighborsToRender = true;

        private Vector3 _lightUp;
        public Vector3 LightDirection;

        //private FBO _closeFBO = new FBO(2000, 2000, FBOType.Depth);
        //private FBO _middleFBO = new FBO(2000, 2000, FBOType.Depth);

        private Matrix4 _closeLightSpaceMatrix;
        private Matrix4 _middleLightSpaceMatrix;

        private float _closeLightTimer = 0.1f;
        private float _middleLightTimer = 0.5f;


        public int RenderedChunks = 0;

        public int Counter = 0;

        public RollingAverageTimer RenderingTimer = new();

        public VoxelRenderer()
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

                TestShader = new(new()
                {
                    VertexShaderPath = Game.ShaderPath / "world_vulkan/indirect-world.vert", 
                    FragmentShaderPath = Game.ShaderPath / "world_vulkan/indirect-world.frag",
                });
                TestShader.Compile();

                View = TestShader.GetLocation("ubo.view");
                Projection = TestShader.GetLocation("ubo.proj");

                LightDirectionLocation = TestShader.GetLocation("data.lightDirection");
                DoAmbientOcclusion = TestShader.GetLocation("data.uDoAmbientOcclusion");
                CameraPosition = TestShader.GetLocation("data.uCameraPosition");

                IndirectCompute = new(new()
                {
                    ComputeShaderPath = Game.ShaderPath / "computeShaders/world_vulkan/renderLoop.comp"
                });
                IndirectCompute.Compile();

                uPlanesLocation = IndirectCompute.GetLocation("ubo.planes");
                uMaxSlotsLocation = IndirectCompute.GetLocation("ubo.uMaxSlots");

                _started = true;
            }

            _chunkOffsetAction = GenerateDistanceBasedChunkOffsets;
            _camera = new Camera(Game.Width, Game.Height, (0, 0, 0));
            _viewport = (0, 0, 0, 0);
            _width = Game.Width;
            _height = Game.Height;
        }

        public VoxelRenderer(VoxelRendererSettings settings)
        {
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
            _camera.GetProjectionMatrix();
        }

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
            _camera.GetProjectionMatrix();
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
            _camera.GetProjectionMatrix();
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

            float dayLengthSeconds = 600f; // 10 minutes per full day
            float angle = (float)(GameTime.TotalTime / dayLengthSeconds) * MathF.Tau;

            // Horizontal rotation (east → west)
            Vector3 horizontal = new Vector3(
                MathF.Cos(angle),
                0.0f,
                MathF.Sin(angle)
            );

            // Elevation (sun arc)
            float maxElevation = MathF.PI * 0.35f;
            float elevation = MathF.Sin(angle) * maxElevation;

            // Combine horizontal direction with elevation
            Vector3 front = Vector3.Normalize(new Vector3(
                horizontal.X,
                -MathF.Sin(elevation),   // negative = light points downward
                horizontal.Z
            ));
            
            var right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            _lightUp = Vector3.Normalize(Vector3.Cross(right, front));

            LightDirection = front;
        }

        void LateUpdate()
        {
            /*
            if (!Run) return;

            while (_closeLightTimer >= 0.1f)
            {
                _closeLightTimer -= 0.1f;
            }

            while (_middleLightTimer >= 0.5f)
            {
                _middleLightTimer -= 0.5f;
            }

            _closeLightTimer += GameTime.DeltaTime;
            _middleLightTimer += GameTime.DeltaTime;
            */

            //Info.SetChunkTotalCount(VoxelChunkInstances.Count);
        }

        private int _oldVisibleChunkCount = 0;
        private Vector3 _oldCameraPosition = Vector3.Zero;
        private int _chunkCount = 0;

        void Render()
        {
            if (!Run) return;

            /*
            if (RealtimeShadows)
            {
                if (_closeLightTimer >= 0.1f)
                {
                    RenderShadowMap(_closeFBO, 80, 80, 80, 64, 2000, ref _closeLightSpaceMatrix);
                }

                if (_middleLightTimer >= 0.5f)
                {
                    RenderShadowMap(_middleFBO, 320, 320, 400, 256, 2000, ref _middleLightSpaceMatrix);
                }
            }
            */  

            /*
            GL.UniformMatrix4(WorldShaderLocation.View, false, ref view);
            GL.UniformMatrix4(WorldShaderLocation.Projection, false, ref projection);

            GL.Uniform1(WorldShaderLocation.Texture, 0);
            GL.Uniform3(WorldShaderLocation.LightDirection, LightDirection);
            GL.Uniform3(WorldShaderLocation.CameraPosition, Camera.Position);
            GL.Uniform3(WorldShaderLocation.PlayerPosition, Transform.Position);
            GL.Uniform1(WorldShaderLocation.AmbientOcclusionTexture, 1);
            GL.Uniform1(WorldShaderLocation.DoAmbientOcclusion, 1);

            GL.UniformMatrix4(WorldShaderLocation.CloseLightSpaceMatrix, false, ref _closeLightSpaceMatrix);
            GL.Uniform1(WorldShaderLocation.CloseShadowMap, 2);
            GL.UniformMatrix4(WorldShaderLocation.MiddleLightSpaceMatrix, false, ref _middleLightSpaceMatrix);
            GL.Uniform1(WorldShaderLocation.MiddleShadowMap, 3);
            GL.Uniform1(WorldShaderLocation.DoRealtimeShadows, RealtimeShadows ? 1 : 0);
            */

            GFX.Viewport(_viewport.left, _viewport.bottom, _width, _height);
            
            if (Input.MouseDelta != Vector2.Zero || _oldCameraPosition != Camera.Position || VisibleChunks.Count != _oldVisibleChunkCount)
            {
                bool update = false;
                _chunkCount = 0;
                for (int i = 0; i < VisibleChunks.Count; i++)
                {
                    var chunk = VisibleChunks[i];
                    if (chunk.ForceDisabled || !Camera.FrustumIntersectsSphere(chunk.Center, 28))
                    {
                        update = update || chunk.Visible == true;
                        chunk.Visible = false;
                        continue;
                    }

                    update = update || chunk.Visible == false;
                    chunk.Visible = true;
                    chunk.Allocation.DataPool.UpdateDrawCommand(chunk, chunk.Allocation);
                    _chunkCount++;
                }

                if (update)
                {
                    ChunkDataPool.UpdateDrawCommands(this);
                }
                    
                Info.SetChunkRenderCount(_chunkCount);
            }

            _oldVisibleChunkCount = VisibleChunks.Count;
            _oldCameraPosition = Camera.Position;

            RenderedChunks = _chunkCount;

            TestShader.Bind();

            ChunkDataPool.Render(this);

            GFX.Viewport(0, 0, Game.Width, Game.Height);
        }

        private void RenderOrtho()
        {
            /*
            {
                float width  = 80f;
                float height = 80f;
                float depth = 80f;

                _closeFBO.Bind();

                GL.Viewport(0, 0, 2000, 2000);

                GL.Clear(ClearBufferMask.DepthBufferBit);

                Matrix4 view = Matrix4.LookAt(Transform.Position, Transform.Position + LightDirection, _lightUp);
                Matrix4 projection = Matrix4.CreateOrthographicOffCenter(-width / 2, width / 2, -height / 2, height / 2, -depth, depth);

                _closeLightSpaceMatrix = view * projection;

                GL.Enable(EnableCap.DepthTest);
                GL.DepthFunc(DepthFunction.Less);
                GL.Enable(EnableCap.CullFace);

                BlankShader.Bind();

                GL.UniformMatrix4(BlankShaderLocation.View, false, ref view);
                GL.UniformMatrix4(BlankShaderLocation.Projection, false, ref projection);

                float distance = 64f;

                for (int i = 0; i < VisibleChunks.Count; i++)
                {
                    var chunk = VisibleChunks[i];
                    if (!chunk.HasBlocks || Vector3.DistanceSquared(Transform.Position, chunk.Center) > distance * distance)
                        continue;

                    GL.UniformMatrix4(BlankShaderLocation.Model, false, ref chunk.ModelMatrix);
                    chunk.Render();
                }

                BlankShader.Unbind();

                _closeFBO.Unbind();
            }
            */
        }

        /*
        private void RenderShadowMap(FBO fbo, float width, float height, float depth, float distance, int size, ref Matrix4 lightSpaceMatrix)
        {
            fbo.Bind();

            GL.Viewport(0, 0, size, size);

            GL.Clear(ClearBufferMask.DepthBufferBit);

            Matrix4 view = Matrix4.LookAt(Transform.Position, Transform.Position + LightDirection, _lightUp);
            Matrix4 projection = Matrix4.CreateOrthographicOffCenter(-width / 2, width / 2, -height / 2, height / 2, -depth, depth);

            lightSpaceMatrix = view * projection;

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);
            GL.Disable(EnableCap.CullFace);

            BlankShader.Bind();

            GL.UniformMatrix4(BlankShaderLocation.View, false, ref view);
            GL.UniformMatrix4(BlankShaderLocation.Projection, false, ref projection);

            for (int i = 0; i < Chunks.Count; i++)
            {
                var chunk = Chunks[i];
                if (!chunk.HasBlocks || Vector3.DistanceSquared(Transform.Position, chunk.Center) > distance * distance)
                    continue;

                GL.UniformMatrix4(BlankShaderLocation.Model, false, ref chunk.ModelMatrix);
                chunk.Render();
            }

            BlankShader.Unbind();

            GL.Enable(EnableCap.CullFace);

            GL.Viewport(0, 0, Game.Width, Game.Height);

            fbo.Unbind();
        }
        */

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
            ChunkDataPool.Dispose();

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