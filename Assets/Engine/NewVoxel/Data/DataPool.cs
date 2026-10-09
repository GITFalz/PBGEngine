using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using PBG.Collections;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;
using Silk.NET.Vulkan;

namespace PBG.NewVoxel;

[InternalSystemInit(InitPriority.Shader)]
public class NewChunkDataPool
{
    public static ComputeShader FrustumCullingCompute;
    public static int PlanesLocation = -1;
    public static int MaxSlotsLocation = -1;
    public static int PlayerPositionLocation = -1;
    public static int MaxDistanceLocation = -1;

    public static void Init()
    {
        FrustumCullingCompute = new(new()
        {
            ComputeShaderPath = Files.FileManager.ShaderPath / "computeShaders" / "world_vulkan" / "renderLoop.comp"
        });

        FrustumCullingCompute.Compile();

        PlanesLocation = FrustumCullingCompute.GetLocation("ubo.planes");
        MaxSlotsLocation = FrustumCullingCompute.GetLocation("ubo.uMaxSlots");
        PlayerPositionLocation = FrustumCullingCompute.GetLocation("ubo.playerPosition");
        MaxDistanceLocation = FrustumCullingCompute.GetLocation("ubo.maxDistance");
    }



    public List<GPUChunkData> DataPool = [];
    public const uint CHUNK_COUNT_PER_POOL = 4096;
    public const uint SLOT_SIZE = 1024;

    public readonly VoxelRenderer Renderer;
    public readonly int LODLevel;
    public bool Updated = false;

    private object _lock = new();
    private Task? _dataPoolTask = null;
    private int _generating = 0;

    public NewChunkDataPool(VoxelRenderer renderer, int lodLevel)
    {
        Renderer = renderer;
        LODLevel = lodLevel;
    }

    public bool TryAllocate(VoxelChunk chunk, uint size, out Allocation alloc)
    {
        if (DataPool.Count == 0)
            DataPool.Add(new(this, CHUNK_COUNT_PER_POOL, SLOT_SIZE, LODLevel));

        for (int i = 0; i < DataPool.Count; i++)
        {
            if (DataPool[i].TryAllocate(chunk, size, out alloc))
                return true;
        }

        DataPool.Add(new(this, CHUNK_COUNT_PER_POOL, SLOT_SIZE, LODLevel));
        if (DataPool[^1].TryAllocate(chunk, size, out alloc))
            return true;

        return false;
    }

    public bool TryAllocate2(VoxelChunk chunk, [NotNullWhen(true)] out Allocation? alloc)
    {
        // Fast path
        for (int i = 0; i < DataPool.Count; i++)
        {
            if (DataPool[i].TryAllocate2(chunk, out alloc))
            {
                EnsureSparePool();
                return true;
            }
        }

        // Slow path
        Task? task;

        lock (_lock)
        {
            // Someone may have added a pool while we were searching.
            for (int i = 0; i < DataPool.Count; i++)
            {
                if (DataPool[i].TryAllocate2(chunk, out alloc))
                    return true;
            }

            task = _dataPoolTask;

            if (task == null)
            {
                // Nobody is generating one.
                // We become the thread that does it.
                AddDataPool();
                
                return DataPool[^1].TryAllocate2(chunk, out alloc);
            }
        }

        // IMPORTANT: lock is released before waiting.
        task.GetAwaiter().GetResult();

        // The pool has now been added.
        return DataPool[^1].TryAllocate2(chunk, out alloc);
    }

    // Keep one spare pool ready whenever possible
    private void EnsureSparePool()
    {
        lock (_lock)
        {
            // Already have an empty one? Nothing to do.
            try
            {
                bool empty = false;
                for (int i = 0; i < DataPool.Count; i++)
                {
                    empty |= DataPool[i].AllocationCount == 0;
                }
                if (empty)
                    return;
            }
            catch
            {
                DebugLog.Warning("Race condition when looping over data pools");
            }

            // Already scheduled a creation? Don't schedule another.
            if (_dataPoolTask != null)
                return;

            _dataPoolTask = Task.Run(() =>
            {
                try
                {
                    AddDataPool();
                }
                catch (Exception ex)
                {
                    DebugLog.Error(ex.Message);
                    throw;
                }
                finally
                {
                    lock (_lock)
                    {
                        _dataPoolTask = null;
                    }
                }
            });
        }
    }

    private void AddDataPool()
    {
        try
        {
            var newPool = new GPUChunkData(this, CHUNK_COUNT_PER_POOL, SLOT_SIZE, LODLevel);
            DataPool.Add(newPool);
        }
        catch (Exception ex)
        {
            DebugLog.Error(ex.Message);
            throw;
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
            DataPool[i].UpdateBuffers(passIndex);
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

    public void Remove(GPUChunkData dataPool)
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

    public override string ToString()
    {
        return $"chunk: {Chunk.RelativePosition}, index: {Index}, vertex count: {VertexCount}";
    }   
}

public unsafe class GPUChunkData : IDisposable
{
    private static int _counter = 0;

    public static RollingAverageDoubleTimer TotalUploadTimer = new(2000);
    public static int UploadCount = 0;

    public int ID { get; private set; } = _counter++;

    private HashSet<VoxelChunk> _chunkMap = [];

    private NewChunkDataPool _chunkDataPool;

    public SSBO<Vector2u> MeshSSBO;
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
    public ChunkInfo[] ChunkInfo = [];

    private int[][] _chunkCounts;

    private bool _updateChunkData = false;
    private uint _updateStart = NewChunkDataPool.CHUNK_COUNT_PER_POOL;
    private uint _updateEnd = 0;

    public BitArray AllocationArray = new((int)NewChunkDataPool.CHUNK_COUNT_PER_POOL);
    public int AllocationCount = 0;
    private object _allocationLock = new();

    public const int PASS_COUNT = 4;

    public static int vertexCount = 0;

    public bool Empty = true;
    public bool Full = false;
    private double _emptyTime = 0;

    public GPUChunkData(NewChunkDataPool chunkDataPool, uint count, uint size, int lodLevel)
    {
        _chunkDataPool = chunkDataPool;

        _chunkSize = size;

        SizeInBytes = count * size * (uint)Marshal.SizeOf<Vector2u>();

        MeshSSBO = new(count * size, hostVisible: false, useStaging: true);

        _descriptors            = new Descriptor[PASS_COUNT];
        _wireframeDescriptors   = new Descriptor[PASS_COUNT];
        _blankDescriptors       = new Descriptor[PASS_COUNT];
        _prePassDescriptors     = new Descriptor[PASS_COUNT];
        _cullingDescriptors     = new Descriptor[GFX.MAX_FRAMES_IN_FLIGHT][];

        _chunkInfoSSBO = new SSBO<ChunkInfo>(count, true);

        _indirectSSBOs      = new IDBO<DrawCommand>[GFX.MAX_FRAMES_IN_FLIGHT][];
        _indirectCountSSBOs = new IDBO<uint>[GFX.MAX_FRAMES_IN_FLIGHT][];
        _drawCommands       = new DrawCommand[GFX.MAX_FRAMES_IN_FLIGHT][][];
        _chunkCounts        = new int[GFX.MAX_FRAMES_IN_FLIGHT][];

        _matrixSSBO = new(count);

        _matrices  = new Matrix4[count];
        ChunkInfo = new ChunkInfo[count];

        for (int i = 0; i < GFX.MAX_FRAMES_IN_FLIGHT; i++)
        {
            UploadQueues[i] = [];

            _indirectSSBOs[i]      = new IDBO<DrawCommand>[PASS_COUNT];
            _indirectCountSSBOs[i] = new IDBO<uint>[PASS_COUNT];
            _drawCommands[i]       = new DrawCommand[PASS_COUNT][];
            _chunkCounts[i]        = new int[PASS_COUNT];
            _cullingDescriptors[i] = new Descriptor[PASS_COUNT];

            for (int j = 0; j < PASS_COUNT; j++)
            {
                var indirectSSBO = new IDBO<DrawCommand>(count, true);
                var indirectCountSSBO = new IDBO<uint>([0], true);
                var cullingDescriptor = NewChunkDataPool.FrustumCullingCompute.GetDescriptorSet();

                cullingDescriptor.BindSSBO(_chunkInfoSSBO, 0);
                cullingDescriptor.BindIDBO(indirectSSBO, 1);
                cullingDescriptor.BindIDBO(indirectCountSSBO, 2);

                _indirectSSBOs[i][j]      = indirectSSBO;
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

            _descriptors[i]          = descriptor;
            _wireframeDescriptors[i] = wireframeDescriptor;
            _blankDescriptors[i]     = blankDescriptor;
            _prePassDescriptors[i]   = prePassDescriptor;

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

        //chunk.AddStatusChange((VoxelChunk.sw.Elapsed.TotalMilliseconds, "index: " + index + " value: " + AllocationArray.GetInt(index)));
        
        Empty = false;

        allocation = new(chunk) 
        { 
            DataPool = this, 
            VertexCount = 0,
            Offset = (uint)index,
            Size = 1,
            Memory = ((Vector2u*)MeshSSBO.GetMappedPointer(frame)) + index * _chunkSize,
            FrameIndex = frame,
        };

        return true;
    }

    public void Update(VoxelChunk chunk, Vector2u[] data, int vertexCount)
    {
        _chunkDataPool.Updated = true;
        
        nint stride = Marshal.SizeOf<Vector2u>();
        MeshSSBO.Update(data, (ulong)(chunk.Allocation.Offset * _chunkSize * stride), (ulong)(vertexCount * stride));

        uint remaining = (uint)vertexCount;

        for (int i = 0; i < chunk.Allocation.Size; i++)
        {
            uint thisPageVerts = Math.Min(remaining, _chunkSize);
            long index = chunk.Allocation.Offset + i;

            _matrices[index] = chunk.ModelMatrix;
            ChunkInfo[index] = new() {
                Center      = chunk.Center,
                Radius      = 28.0f * chunk.LodMult,
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
            UploadCount++;

            var chunk = uploadData.Chunk;
            var gen = uploadData.Gen;
            var index = uploadData.Index;
            var vertexCount = uploadData.VertexCount;
            var offsetInBytes = index * NewChunkDataPool.SLOT_SIZE * Vector2u.ByteSize;
            var sizeInBytes = vertexCount * Vector2u.ByteSize;

            if (gen != chunk.Gen)
            {
                //chunk.AddStatusChange((VoxelChunk.sw.Elapsed.TotalMilliseconds, "upload free index: " + index + " value: " + AllocationArray.GetInt((int)index)));
                RemoveAllocation(index);
                continue;
            }

            MeshSSBO.MarkDirty(offsetInBytes, sizeInBytes);

            //chunk.AddStatusChange((VoxelChunk.sw.Elapsed.TotalMilliseconds, "uploaded"));

            var oldInfo = ChunkInfo[index];
            if (oldInfo.Active == 1)
                DebugLog.Warning($"Chunk: {(oldInfo.Center - 16)} was already there");

            _matrices[index] = chunk.ModelMatrix;
            ChunkInfo[index] = new() {
                Center      = chunk.Center,
                Radius      = 28.0f * chunk.LodMult,
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

        TotalUploadTimer.AddSample(sw.Elapsed.TotalMilliseconds);
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

    public void Free(ref Allocation allocation)
    {
        _chunkMap.Remove(allocation.Chunk);

        _chunkDataPool.Updated = true;

        lock (_allocationLock)
        {
            //AllocationArray.RemoveMap((int)allocation.Offset, (int)allocation.Size);

            for (int i = 0; i < allocation.Size; i++)
            {
                long index = allocation.Offset + i;
                ChunkInfo[index].Active = 0;
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
                newChunkInfo[newIndex] = ChunkInfo[index];
                _matrices[newIndex] = Matrix4.CreateTranslation(chunk.WorldPosition);
                newIndex++;
            }

            chunk.Allocation.Offset = newOffset;
        }

        ChunkInfo = newChunkInfo;

        _updateChunkData = true;
        _updateStart = 0;
        _updateEnd = NewChunkDataPool.CHUNK_COUNT_PER_POOL;
    }

    public void Reset()
    {
        for (int j = 0; j < PASS_COUNT; j++)
        {
            _indirectCountSSBOs[GFX.CurrentFrame][j].Update([0]);
        }
    }

    public unsafe void FrustumPass(Camera camera, int passIndex)
    {
        var descriptor = _cullingDescriptors[GFX.CurrentFrame][passIndex];

        var cmd = GFX.CommandBuffer;

        NewChunkDataPool.FrustumCullingCompute.Bind(cmd);
        descriptor.Bind(cmd, Silk.NET.Vulkan.PipelineBindPoint.Compute);

        descriptor.UniformArray(NewChunkDataPool.PlanesLocation, camera.GpuPlanes);
        descriptor.Uniform(NewChunkDataPool.MaxSlotsLocation, NewChunkDataPool.CHUNK_COUNT_PER_POOL);
        descriptor.Uniform(NewChunkDataPool.PlayerPositionLocation, camera.Position);
        descriptor.Uniform(NewChunkDataPool.MaxDistanceLocation, camera.SCREEN_FAR);
        
        NewChunkDataPool.FrustumCullingCompute.DispatchBarrier(cmd, descriptor, (uint)((NewChunkDataPool.CHUNK_COUNT_PER_POOL + 255) / 256), 1, 1);

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

    public void UpdateBuffers(int passIndex = 0)
    {
        if (_updateChunkData && _updateEnd > _updateStart)
        {
            MeshSSBO.FlushStaging();

            _matrixSSBO.UpdateSlice(_matrices, _updateStart * Matrix4.ByteSize, (_updateEnd - _updateStart) * Matrix4.ByteSize);
            _chunkInfoSSBO.UpdateSlice(ChunkInfo, _updateStart * NewVoxel.ChunkInfo.ByteSize, (_updateEnd - _updateStart) * NewVoxel.ChunkInfo.ByteSize);

            _updateChunkData = false;
            _updateStart = NewChunkDataPool.CHUNK_COUNT_PER_POOL;
            _updateEnd = 0;
        }
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

        AllocationArray.Dispose();

        GC.SuppressFinalize(this);
    }
}
