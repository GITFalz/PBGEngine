using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using PBG.Collections;
using PBG.Compiler.Lines;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.Threads;
using Silk.NET.Vulkan;

namespace PBG.NewVoxel;

public class NewChunkDataPool
{
    public List<GPUChunkDataPool> DataPool = [];
    public const uint CHUNK_COUNT_PER_POOL = 4096;
    public const uint SLOT_SIZE = 1024;

    public readonly VoxelRenderer Renderer;
    public bool Updated = false;

    private object _lock = new();
    private Task? _dataPoolTask = null;

    public NewChunkDataPool(VoxelRenderer renderer)
    {
        Renderer = renderer;
    }

    public bool TryAllocate(VoxelChunk chunk, uint size, out Allocation alloc)
    {
        if (DataPool.Count == 0)
            DataPool.Add(new(this, CHUNK_COUNT_PER_POOL, SLOT_SIZE));

        for (int i = 0; i < DataPool.Count; i++)
        {
            if (DataPool[i].TryAllocate(chunk, size, out alloc))
                return true;
        }

        DataPool.Add(new(this, CHUNK_COUNT_PER_POOL, SLOT_SIZE));
        if (DataPool[^1].TryAllocate(chunk, size, out alloc))
            return true;

        return false;
    }

    public bool TryAllocate2(VoxelChunk chunk, [NotNullWhen(true)] out Allocation? alloc)
    {
        // 1. Try all existing pools (fast path)
        for (int i = 0; i < DataPool.Count; i++)
        {
            if (DataPool[i].TryAllocate2(chunk, out alloc))
            {
                // We just used a slot → maybe we should start growing a spare
                EnsureSparePool();
                return true;
            }
        }

        // 2. No free slots. Try to force a new pool (this is the only place we may wait)
        lock (_lock)
        {
            // Re-check under lock in case another thread just added one
            for (int i = 0; i < DataPool.Count; i++)
            {
                if (DataPool[i].TryAllocate2(chunk, out alloc))
                    return true;
            }

            // Still nothing → we have to create one now (this is the rare case you want to avoid)
            if (_dataPoolTask != null)
            {
                _dataPoolTask.Wait();   // only wait if one was already in flight
                _dataPoolTask = null;
            }
            else
            {
                // Last resort: block and create
                Task.Run(() =>
                {
                    DataPool.Add(new GPUChunkDataPool(this, CHUNK_COUNT_PER_POOL, SLOT_SIZE));
                }).Wait();
            }

            // Now the last pool should have space
            return DataPool[^1].TryAllocate2(chunk, out alloc);
        }
    }

    // Keep one spare pool ready whenever possible
    private void EnsureSparePool()
    {
        lock (_lock)
        {
            // Already have an empty one? Nothing to do.
            if (DataPool.Any(p => p.AllocationCount == 0))
                return;

            // Already scheduled a creation? Don't schedule another.
            if (_dataPoolTask != null)
                return;

            /*
            _dataPoolTask = MainThreadDispatcher.Enqueue(() =>
            {
                var newPool = new GPUChunkDataPool(this, CHUNK_COUNT_PER_POOL, SLOT_SIZE);
                lock (_lock)
                {
                    DataPool.Add(newPool);
                    _dataPoolTask = null;
                }
            });
            */

            _dataPoolTask = Task.Run(() =>
            {
                var newPool = new GPUChunkDataPool(this, CHUNK_COUNT_PER_POOL, SLOT_SIZE);
                lock (_lock)
                {
                    DataPool.Add(newPool);
                    _dataPoolTask = null;
                }
            });
        }
    }

    public void Reset()
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].Reset();
    }

    public void FrustumPass(Camera camera, int passIndex)
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].FrustumPass(camera, passIndex);
    }

    public void OrderChunks(Vector3 center)
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].OrderChunks(center);
    }

    public void UpdateDrawCommands(int passIndex = 0)
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].UpdateDrawCommands(passIndex);
    }

    public void HandleUploads()
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].HandleUploads();
    }

    public void UpdateDescriptorUniform(Action<Descriptor> action, int passIndex)
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].UpdateDescriptorUniform(action, passIndex);
    }

    public void RenderPrePass(int passIndex = 0)
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].RenderPrePass(Renderer, passIndex);
    }

    public void RenderBlank(Matrix4 view, Matrix4 projection, int passIndex = 0)
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].RenderBlank(view, projection, passIndex);
    }

    public void Render(Camera camera, int passIndex = 0)
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].Render(camera, Renderer, passIndex);
    }

    public void RenderWireframe(int passIndex = 0)
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].RenderWireframe(Renderer, passIndex);
    }

    public void EmptyCheck()
    {
        bool canEmpty = false;

        for (int i = 0; i < DataPool.Count; i++)
        {
            if (canEmpty && DataPool[i].EmptyCheck())
            {
                DataPool.RemoveAt(i);
                i--;
            }

            canEmpty |= DataPool[i].AllocationCount == 0;
        }
    }

    public void Dispose()
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].Dispose();
        DataPool = [];
    }

    public void Remove(GPUChunkDataPool dataPool)
    {
        dataPool.Dispose();
        DataPool.Remove(dataPool);
    }

    public static int AllocationSize(int vertexCount) => Mathf.CeilToInt((float)vertexCount / (float)SLOT_SIZE);
}

public struct UploadData
{
    public VoxelChunk Chunk;
    public ulong Gen;
    public uint Index;
    public uint VertexCount;
    public ulong OffsetInBytes;
    public ulong SizeInBytes;

    public override string ToString()
    {
        return $"chunk: {Chunk.RelativePosition}, index: {Index}, vertex count: {VertexCount}, offset: {OffsetInBytes}, size: {SizeInBytes}";
    }   
}

[InternalSystemInit(InitPriority.Shader)]
public unsafe class GPUChunkDataPool : IDisposable
{
    private static int _counter = 0;

    public int ID { get; private set; } = _counter++;

    private BoundingBoxRenderer _boundingBoxDebug = new();
    private HashSet<VoxelChunk> _chunkMap = [];

    private NewChunkDataPool _chunkDataPool;

    public SSBO<Vector4i> MeshSSBO;
    public ulong SizeInBytes;
    private uint _chunkSize;

    public ConcurrentQueue<UploadData>[] UploadQueues = new ConcurrentQueue<UploadData>[GFX.MAX_FRAMES_IN_FLIGHT];

    private Descriptor[] _descriptors;
    private Descriptor[] _wireframeDescriptors;
    private Descriptor[] _blankDescriptors;
    private Descriptor[] _prePassDescriptors;
    private Descriptor[][] _cullingDescriptors;

    private IDBO<DrawCommand>[][] _indirectSSBOs;
    private IDBO<uint>[][] _indirectCountSSBOs;
    private DrawCommand[][][] _drawCommands;

    private SSBO<Matrix4> _matrixSSBO;
    private Matrix4[] _matrices;

    private SSBO<ChunkInfo> _chunkInfoSSBO;
    private ChunkInfo[] _chunkInfo = [];

    private int[][] _chunkCounts;
    private int[] _visibleChunks = new int[PASS_COUNT];

    private bool _updateChunkData = false;
    private uint _updateStart = NewChunkDataPool.CHUNK_COUNT_PER_POOL;
    private uint _updateEnd = 0;

    //public List<Allocation> Allocations = [];
    public BitArray AllocationArray = new((int)NewChunkDataPool.CHUNK_COUNT_PER_POOL);
    public int AllocationCount = 0;
    private object _allocationLock = new();

    public const int PASS_COUNT = 4;

    public static int vertexCount = 0;

    public bool Empty = true;
    public bool Full = false;
    private double _emptyTime = 0;

    public GPUChunkDataPool(NewChunkDataPool chunkDataPool, uint count, uint size)
    {
        _chunkDataPool = chunkDataPool;

        _chunkSize = size;
        SizeInBytes = count * size * (uint)Marshal.SizeOf<Vector4i>();

        MeshSSBO = new(count * size, hostVisible: false, useStaging: true);

        //Allocations.Add(new() { DataPool = this, Offset = 0, Size = count });

        _descriptors            = new Descriptor[PASS_COUNT];
        _wireframeDescriptors   = new Descriptor[PASS_COUNT];
        _blankDescriptors       = new Descriptor[PASS_COUNT];
        _prePassDescriptors     = new Descriptor[PASS_COUNT];
        _cullingDescriptors     = new Descriptor[GFX.MAX_FRAMES_IN_FLIGHT][];

        _chunkInfoSSBO          = new SSBO<ChunkInfo>(count, true);

        _indirectSSBOs          = new IDBO<DrawCommand>[GFX.MAX_FRAMES_IN_FLIGHT][];
        _indirectCountSSBOs     = new IDBO<uint>[GFX.MAX_FRAMES_IN_FLIGHT][];
        _drawCommands           = new DrawCommand[GFX.MAX_FRAMES_IN_FLIGHT][][];
        _chunkCounts            = new int[GFX.MAX_FRAMES_IN_FLIGHT][];

        _matrixSSBO             = new(count);

        _matrices               = new Matrix4[count];
        _chunkInfo              = new ChunkInfo[count];

        for (int i = 0; i < GFX.MAX_FRAMES_IN_FLIGHT; i++)
        {
            UploadQueues[i] = [];

            _indirectSSBOs[i] = new IDBO<DrawCommand>[PASS_COUNT];
            _indirectCountSSBOs[i] = new IDBO<uint>[PASS_COUNT];
            _drawCommands[i] = new DrawCommand[PASS_COUNT][];
            _chunkCounts[i] = new int[PASS_COUNT];
            _cullingDescriptors[i] = new Descriptor[PASS_COUNT];

            for (int j = 0; j < PASS_COUNT; j++)
            {
                var indirectSSBO = new IDBO<DrawCommand>(count, true);
                var indirectCountSSBO = new IDBO<uint>([0], true);
                var cullingDescriptor = FrustumCullingCompute.GetDescriptorSet();  

                cullingDescriptor.BindSSBO(_chunkInfoSSBO, 0);
                cullingDescriptor.BindIDBO(indirectSSBO, 1);
                cullingDescriptor.BindIDBO(indirectCountSSBO, 2);

                _indirectSSBOs[i][j] = indirectSSBO;
                _indirectCountSSBOs[i][j] = indirectCountSSBO;
                _drawCommands[i][j] = new DrawCommand[count];
                _cullingDescriptors[i][j] = cullingDescriptor;
            }
        }

        for (int i = 0; i < PASS_COUNT; i++)
        {
            var descriptor = WorldShader.Shader.GetDescriptorSet();  
            var wireframeDescriptor = VoxelRenderer.WireframeWorldShader.GetDescriptorSet();  
            var blankDescriptor = VoxelRenderer.BlankWorldShader.GetDescriptorSet();  
            var prePassDescriptor = VoxelRenderer.TestPrePassShader.GetDescriptorSet();  
            
            _descriptors[i] = descriptor;
            _wireframeDescriptors[i] = wireframeDescriptor;
            _blankDescriptors[i] = blankDescriptor;
            _prePassDescriptors[i] = prePassDescriptor;

            descriptor.BindSSBO(BlockData.FaceGeometrySSBO, 0);
            descriptor.BindSSBO(MeshSSBO, 1);
            descriptor.BindSSBO(_matrixSSBO, 2);
            descriptor.BindTextureArray(BlockData.BlockTextureArray, 5);
            descriptor.BindFramebufferDepth(VoxelRenderer.CloseFBO, 6);
            descriptor.BindFramebufferDepth(VoxelRenderer.MiddleFBO, 7);
            descriptor.BindFramebufferDepth(VoxelRenderer.FarFBO, 8);

            wireframeDescriptor.BindSSBO(BlockData.FaceGeometrySSBO, 0);
            wireframeDescriptor.BindSSBO(MeshSSBO, 1);
            wireframeDescriptor.BindSSBO(_matrixSSBO, 2);
            
            blankDescriptor.BindSSBO(BlockData.FaceGeometrySSBO, 0);
            blankDescriptor.BindSSBO(MeshSSBO, 1);
            blankDescriptor.BindSSBO(_matrixSSBO, 2);
            blankDescriptor.BindTextureArray(BlockData.BlockTextureArray, 4);
            
            prePassDescriptor.BindSSBO(BlockData.FaceGeometrySSBO, 0);
            prePassDescriptor.BindSSBO(MeshSSBO, 1);
            prePassDescriptor.BindSSBO(_matrixSSBO, 2);
        }   

        _emptyTime = GameTime.TotalTime; 
    }

    public bool TryAllocate(VoxelChunk chunk, uint size, out Allocation alloc)
    {
        alloc = new(chunk) { DataPool = this, VertexCount = size };

        uint chunkCount = (uint)Mathf.CeilToInt((float)size / (float)_chunkSize);

        _chunkMap.Add(chunk);

        int bitSize = 0;

        for (int i = 0; i < AllocationArray.Length; i++)
        {
            ulong bit = AllocationArray.GetUnsafe(i);
            if (bit == 0ul)
            {
                bitSize++;
                if (bitSize == chunkCount)
                {
                    int index = i - (bitSize - 1);
                    AllocationArray.SetMap(index, bitSize);
                    alloc.Offset = (uint)index;
                    alloc.Size = (uint)bitSize;

                    chunk.Allocation.Set(alloc);
                    Empty = false;

                    return true;
                }
            }
            else
            {
                bitSize = 0;
            }
        }

        chunk.Allocation.Set(alloc);
        return false;
    }


    public bool TryAllocate2(VoxelChunk chunk, [NotNullWhen(true)] out Allocation? allocation)
    {
        allocation = null;

        uint frame = GFX.CurrentFrame;
        int index;

        lock (_allocationLock)
        {
            if (Full)
                return false;

            index = AllocationArray.GetEmptySlotIndex();

            if (index == -1)
            {
                Full = true;
                return false;
            }

            AllocationCount++;
        }
        
        Empty = false;

        allocation = new(chunk) 
        { 
            DataPool = this, 
            VertexCount = 0,
            Offset = (uint)index,
            Size = 1,
            Memory = ((Vector4i*)MeshSSBO.GetMappedPointer(frame)) + index * _chunkSize,
            FrameIndex = frame,
        };

        return true;
    }

    public void Update(VoxelChunk chunk, Vector4i[] data, int vertexCount)
    {
        _chunkDataPool.Updated = true;
        
        nint stride = Marshal.SizeOf<Vector4i>();
        MeshSSBO.Update(data, (ulong)(chunk.Allocation.Offset * _chunkSize * stride), (ulong)(vertexCount * stride));

        uint remaining = (uint)vertexCount;

        for (int i = 0; i < chunk.Allocation.Size; i++)
        {
            uint thisPageVerts = Math.Min(remaining, _chunkSize);
            long index = chunk.Allocation.Offset + i;

            _matrices[index] = chunk.ModelMatrix;
            _chunkInfo[index] = new() {
                Center      = chunk.Center,
                Radius      = 28.0f,
                VertexCount = thisPageVerts,
                SlotIndex   = (int)index,
                Active      = thisPageVerts > 0 ? 1u : 0u
            };

            remaining -= thisPageVerts;
        }

        _updateChunkData = true;
        if (chunk.Allocation.Start < _updateStart) _updateStart = chunk.Allocation.Start;
        if (chunk.Allocation.End > _updateEnd) _updateEnd = chunk.Allocation.End;
    }

    public void HandleUploads()
    {
        var uploadQueue = UploadQueues[GFX.CurrentFrame];
        if (uploadQueue.IsEmpty)
            return;

        const double MaxUploadTimeMs = 2;

        int uploaded = 0;
        var sw = Stopwatch.StartNew();

        while (uploadQueue.TryDequeue(out var uploadData))
        {
            //Console.WriteLine(uploadData);
            WorldNodeEditor.UploadCount++;

            var chunk = uploadData.Chunk;
            var gen = uploadData.Gen;
            var index = uploadData.Index;
            var vertexCount = uploadData.VertexCount;

            if (gen != chunk.Gen)
            {
                RemoveAllocation(index);
                continue;
            }

            MeshSSBO.MarkDirty(uploadData.OffsetInBytes, uploadData.SizeInBytes);

            _matrices[index] = chunk.ModelMatrix;
            _chunkInfo[index] = new() {
                Center      = chunk.Center,
                Radius      = 28.0f,
                VertexCount = vertexCount,
                SlotIndex   = (int)index,
                Active      = vertexCount > 0 ? 1u : 0u
            };

            _updateChunkData = true;
            _updateStart = Math.Min(_updateStart, index);
            _updateEnd = Mathf.Max(_updateEnd, index + 1);
            
            uploaded++;

            chunk.Allocations.Add(new(chunk)
            {
                DataPool = this,
                VertexCount = vertexCount,
                Offset = index,
                Size = 1
            });

            if (sw.Elapsed.TotalMilliseconds > MaxUploadTimeMs)
                break;
        }

        WorldNodeEditor.TotalUploadTimer.AddSample(sw.Elapsed.TotalMilliseconds);
    }



    public void UpdateDescriptorUniform(Action<Descriptor> action, int passIndex)
    {
        action.Invoke(_descriptors[passIndex]);
    }

    public void RemoveAllocation(Allocation allocation) => RemoveAllocation(allocation.Offset);
    public void RemoveAllocation(uint index)
    {
        lock (_allocationLock)
        {
            Full = false;
            if (AllocationArray.Remove((int)index))
                AllocationCount--;
        }
    }

    public void Free(VoxelChunk chunk)
    {
        _chunkMap.Remove(chunk);

        _chunkDataPool.Updated = true;
        var alloc = chunk.Allocation;

        lock (_allocationLock)
        {
            AllocationArray.RemoveMap((int)alloc.Offset, (int)alloc.Size);

            for (int i = 0; i < chunk.Allocation.Size; i++)
            {
                long index = chunk.Allocation.Offset + i;
                _chunkInfo[index].Active = 0;
            }

            Empty = AllocationArray.IsEmpty;
            if (Empty)
            {
                _emptyTime = GameTime.TotalTime;
            }
        }

        _updateChunkData = true;
        if (chunk.Allocation.Start < _updateStart) _updateStart = chunk.Allocation.Start;
        if (chunk.Allocation.End > _updateEnd) _updateEnd = chunk.Allocation.End;
    }

    public void Free(ref Allocation allocation)
    {
        _chunkMap.Remove(allocation.Chunk);

        _chunkDataPool.Updated = true;

        lock (_allocationLock)
        {
            AllocationArray.RemoveMap((int)allocation.Offset, (int)allocation.Size);

            for (int i = 0; i < allocation.Size; i++)
            {
                long index = allocation.Offset + i;
                _chunkInfo[index].Active = 0;
            }

            Empty = AllocationArray.IsEmpty;
            if (Empty)
            {
                _emptyTime = GameTime.TotalTime;
            }
        }

        _updateChunkData = true;
        if (allocation.Start < _updateStart) _updateStart = allocation.Start;
        if (allocation.End > _updateEnd) _updateEnd = allocation.End;
    }

    public bool EmptyCheck()
    {
        if (Empty && GameTime.TotalTime - _emptyTime >= 60)
        {
            Dispose();
            return true;
        }

        return false;
    }


    public void OrderChunks(Vector3 center)
    {
        var list = _chunkMap.Select(chunk => (chunk, distance: chunk.DistanceSquaredTo(center))).ToList();
        list.Sort((a, b) => a.distance.CompareTo(b.distance));

        ChunkInfo[] newChunkInfo = new ChunkInfo[NewChunkDataPool.CHUNK_COUNT_PER_POOL];

        uint newIndex = 0;
        for (int i = 0; i < list.Count; i++)
        {
            var chunk = list[i].chunk;
            uint newOffset = newIndex;

            for (int j = 0; j < chunk.Allocation.Size; j++)
            {
                long index = chunk.Allocation.Offset + j;
                newChunkInfo[newIndex] = _chunkInfo[index];
                _matrices[newIndex] = Matrix4.CreateTranslation(chunk.WorldPosition);
                newIndex++;
            }

            chunk.Allocation.Offset = newOffset;
        }

        _chunkInfo = newChunkInfo;

        _updateChunkData = true;
        _updateStart = 0;
        _updateEnd = NewChunkDataPool.CHUNK_COUNT_PER_POOL;
    }

    public void Reset()
    {
        /*
        for (int i = 0; i < PASS_COUNT; i++)
        {
            _visibleChunks[i] = 0;
        }
        */

        for (int j = 0; j < PASS_COUNT; j++)
        {
            _indirectCountSSBOs[GFX.CurrentFrame][j].Update([0]);
        }
    }


    public static ComputeShader FrustumCullingCompute;
    public static int PlanesLocation = -1;
    public static int MaxSlotsLocation = -1;


    public static void Init()
    {
        FrustumCullingCompute = new(new()
        {
            ComputeShaderPath = Game.ShaderPath / "computeShaders" / "world_vulkan" / "renderLoop.comp"
        });

        FrustumCullingCompute.Compile();

        PlanesLocation = FrustumCullingCompute.GetLocation("ubo.planes");
        MaxSlotsLocation = FrustumCullingCompute.GetLocation("ubo.uMaxSlots");
    }

    public unsafe void FrustumPass(Camera camera, int passIndex)
    {
        var descriptor = _cullingDescriptors[GFX.CurrentFrame][passIndex];

        var cmd = GFX.CommandBuffer;

        FrustumCullingCompute.Bind(cmd);
        descriptor.Bind(cmd, Silk.NET.Vulkan.PipelineBindPoint.Compute);

        descriptor.UniformArray(PlanesLocation, camera.GpuPlanes);
        descriptor.Uniform(MaxSlotsLocation, NewChunkDataPool.CHUNK_COUNT_PER_POOL);
        
        FrustumCullingCompute.DispatchBarrier(cmd, descriptor, (uint)((NewChunkDataPool.CHUNK_COUNT_PER_POOL + 255) / 256), 1, 1);

        MemoryBarrier barrier = new()
        {
            SType = StructureType.MemoryBarrier,
            SrcAccessMask = AccessFlags.ShaderWriteBit,
            DstAccessMask = AccessFlags.IndirectCommandReadBit
        };

        GFX.Vk.CmdPipelineBarrier(
            cmd,
            PipelineStageFlags.ComputeShaderBit,
            PipelineStageFlags.DrawIndirectBit,
            0,
            1, &barrier,
            0, null,
            0, null);
    }

    public void UpdateDrawCommand(VoxelChunk chunk, Allocation alloc, int passIndex = 0)
    {
        int vertexCount = (int)alloc.VertexCount;
        for (int i = 0; i < alloc.Size; i++)
        {
            if (vertexCount <= 0)
                return;

            var newVertexCount = Mathf.Max(vertexCount - _chunkSize, 0);
            
            var visibleChunks = _visibleChunks[passIndex];
            var drawCommand = _drawCommands[GFX.CurrentFrame][passIndex][visibleChunks];
            drawCommand.InstanceCount = 1;
            drawCommand.Count = ((uint)vertexCount - (uint)newVertexCount) * 6;
            drawCommand.First = (uint)(alloc.Offset + i) * _chunkSize * 6;
            drawCommand.BaseInstance = alloc.Offset + (uint)i;
            _drawCommands[GFX.CurrentFrame][passIndex][visibleChunks] = drawCommand;

            vertexCount = (int)newVertexCount;
            _visibleChunks[passIndex]++;
        }
    }

    public void UpdateDrawCommands(int passIndex = 0)
    {
        /* 
        var visibleChunks = _visibleChunks[passIndex];
        if (visibleChunks == 0)
        {
            _chunkCounts[GFX.CurrentFrame][passIndex] = 0;
            return;
        }
        
        _indirectSSBOs[GFX.CurrentFrame][passIndex].Update(_drawCommands[GFX.CurrentFrame][passIndex], 0, (uint)visibleChunks * (uint)Marshal.SizeOf<DrawCommand>());
        */

        if (_updateChunkData && _updateEnd > _updateStart)
        {
            MeshSSBO.FlushStaging();

            //Console.WriteLine("update arrays: " + _updateStart + " " + _updateEnd);
            _matrixSSBO.UpdateSlice(_matrices, _updateStart * Matrix4.ByteSize, (_updateEnd - _updateStart) * Matrix4.ByteSize);
            _chunkInfoSSBO.UpdateSlice(_chunkInfo, _updateStart * ChunkInfo.ByteSize, (_updateEnd - _updateStart) * ChunkInfo.ByteSize);

            _updateChunkData = false;
            _updateStart = NewChunkDataPool.CHUNK_COUNT_PER_POOL;
            _updateEnd = 0;
        }

        /*
        _chunkCounts[GFX.CurrentFrame][passIndex] = visibleChunks;
        _visibleChunks[passIndex] = 0;
        */
    }

    public void RenderPrePass(VoxelRenderer renderer, int passIndex = 0)
    {
        if (_chunkCounts[GFX.CurrentFrame][passIndex] == 0)
            return;

        var prePassDescriptor = _prePassDescriptors[passIndex];
        var cam = renderer.Camera;
        
        prePassDescriptor.Bind();
        prePassDescriptor.Uniform(VoxelRenderer.PrePassView, cam.ViewMatrix);
        prePassDescriptor.Uniform(VoxelRenderer.PrePassProjection, cam.ProjectionMatrix);

        GFX.DrawIndirect(_indirectSSBOs[GFX.CurrentFrame][passIndex].Buffer, 0, (uint)_chunkCounts[GFX.CurrentFrame][passIndex], (uint)Marshal.SizeOf<DrawCommand>());
    }

    public void Render(Camera camera, VoxelRenderer renderer, int passIndex = 0)
    {
        var descriptor = _descriptors[passIndex];
        
        descriptor.Bind();
        renderer.UpdateUniforms(camera, descriptor);

        var indrectBuffer = _indirectSSBOs[GFX.CurrentFrame][passIndex].Buffer;
        var countBuffer = _indirectCountSSBOs[GFX.CurrentFrame][passIndex].Buffer;
        GFX.Vk.CmdDrawIndirectCount(GFX.CommandBuffer, indrectBuffer, 0, countBuffer, 0, NewChunkDataPool.CHUNK_COUNT_PER_POOL, (uint)Marshal.SizeOf<DrawCommand>());
    }

    public void RenderWireframe(VoxelRenderer renderer, int passIndex = 0)
    {
        var descriptor = _wireframeDescriptors[passIndex];
        
        descriptor.Bind();
        renderer.UpdateWireframeUniforms(descriptor);

        var indrectBuffer = _indirectSSBOs[GFX.CurrentFrame][passIndex].Buffer;
        var countBuffer = _indirectCountSSBOs[GFX.CurrentFrame][passIndex].Buffer;
        GFX.Vk.CmdDrawIndirectCount(GFX.CommandBuffer, indrectBuffer, 0, countBuffer, 0, NewChunkDataPool.CHUNK_COUNT_PER_POOL, (uint)Marshal.SizeOf<DrawCommand>());
    }

    public void RenderBlank(Matrix4 view, Matrix4 projection, int passIndex = 0)
    {
        var descriptor = _blankDescriptors[passIndex];
        
        descriptor.Bind();
        descriptor.UniformMatrix4(VoxelRenderer.BlankWorldViewLocation, view);
        descriptor.UniformMatrix4(VoxelRenderer.BlankWorldProjectionLocation, projection);

        var indrectBuffer = _indirectSSBOs[GFX.CurrentFrame][passIndex].Buffer;
        var countBuffer = _indirectCountSSBOs[GFX.CurrentFrame][passIndex].Buffer;
        GFX.Vk.CmdDrawIndirectCount(GFX.CommandBuffer, indrectBuffer, 0, countBuffer, 0, NewChunkDataPool.CHUNK_COUNT_PER_POOL, (uint)Marshal.SizeOf<DrawCommand>());
    }
    
    public void Dispose()
    {
        MeshSSBO.Dispose();
        _matrixSSBO.Dispose();
        _chunkInfoSSBO.Dispose();

        for (int i = 0; i < GFX.MAX_FRAMES_IN_FLIGHT; i++)
        for (int j = 0; j < PASS_COUNT; j++)
        {
            _indirectSSBOs[i][j].Dispose();
            _indirectCountSSBOs[i][j].Dispose();
            _cullingDescriptors[i][j].Dispose();
        }
        
        for (int i = 0; i < PASS_COUNT; i++)
        {
            _descriptors[i].Dispose();
            _wireframeDescriptors[i].Dispose();
            _prePassDescriptors[i].Dispose();
        } 
        
        _descriptors = [];
        _wireframeDescriptors = [];
        _prePassDescriptors = [];
        _chunkMap = [];

        GC.SuppressFinalize(this);
    }
}

public unsafe struct Allocation(VoxelChunk chunk)
{
    public VoxelChunk Chunk = chunk;
    public GPUChunkDataPool DataPool;
    public uint VertexCount;
    public uint Offset;
    public uint Size;
    public Vector4i* Memory = null;
    public uint FrameIndex;

    public readonly uint Start => Offset;
    public readonly uint End => Offset + Size;

    public void Set(Allocation allocation)
    {
        VoxelRenderer.TotalVertexCount -= VertexCount;

        DataPool = allocation.DataPool;
        VertexCount = allocation.VertexCount;
        Offset = allocation.Offset;
        Size = allocation.Size;

        VoxelRenderer.TotalVertexCount += VertexCount;
    }

    public void RemoveAllocation()
    {
        if (Size == 0)
            return;

        DataPool.RemoveAllocation(this);
        Memory = null;
        Size = 0;
        Offset = 0;
        VertexCount = 0;
    }

    public void Free()
    {
        if (Size == 0)
            return;

        VoxelRenderer.TotalVertexCount -= VertexCount;
        
        DataPool.RemoveAllocation(this);
        DataPool?.Free(ref this);
        Memory = null;
        Size = 0;
        Offset = 0;
        VertexCount = 0;
    }

    public override string ToString()
    {
        return $"[Allocation] : Chunk: {Chunk.WorldPosition}, Count: {VertexCount}, Offset: {Offset}, Size: {Size}";
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct DrawCommand
{
    public uint Count;
    public uint InstanceCount;
    public uint First;
    public uint BaseInstance;
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