using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;

namespace PBG.Voxel;

public class ChunkDataPool
{
    public List<GPUChunkDataPool> DataPool = [];
    public const uint CHUNK_COUNT_PER_POOL = 8196;
    public const uint SLOT_SIZE = 8196; // in vertex count (Vector4i * N)

    public readonly IVoxelRenderer Renderer;
    public bool Updated = false;

    public ChunkDataPool(IVoxelRenderer renderer)
    {
        Renderer = renderer;
    }

    public bool TryAllocate(uint size, out Allocation alloc)
    {
        if (DataPool.Count == 0)
            DataPool.Add(new(this, CHUNK_COUNT_PER_POOL, SLOT_SIZE));

        for (int i = 0; i < DataPool.Count; i++)
        {
            if (DataPool[i].TryAllocate(size, out alloc))
                return true;
        }

        DataPool.Add(new(this, CHUNK_COUNT_PER_POOL, SLOT_SIZE));
        if (DataPool[^1].TryAllocate(size, out alloc))
            return true;

        return false;
    }

    public void Reset()
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].Reset();
    }

    public void UpdateDrawCommands(int passIndex = 0)
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].UpdateDrawCommands(passIndex);
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

    public void Render(int passIndex = 0)
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].Render(Renderer, passIndex);
    }

    public void Dispose()
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].Dispose();
        DataPool = [];
    }
}

public class GPUChunkDataPool : IDisposable
{
    private ChunkDataPool _chunkDataPool;

    public SSBO<Vector4i> MeshSSBO;
    public ulong SizeInBytes;
    private uint _chunkSize;

    private Descriptor[] _descriptors;
    private Descriptor[] _blankDescriptors;
    private Descriptor[] _prePassDescriptors;

    private IDBO<DrawCommand>[][] _indirectSSBOs;
    private DrawCommand[][][] _drawCommands;

    private SSBO<Matrix4> _matrixSSBO;
    private Matrix4[] _matrices;

    private int[][] _chunkCounts;
    private int[] _visibleChunks = new int[PASS_COUNT];

    private bool _updateChunkData = false;
    private uint _updateStart = ChunkDataPool.CHUNK_COUNT_PER_POOL;
    private uint _updateEnd = 0;

    public List<Allocation> Allocations = [];

    public const int PASS_COUNT = 4;

    public static int vertexCount = 0;

    public GPUChunkDataPool(ChunkDataPool chunkDataPool, uint count, uint size)
    {
        _chunkDataPool = chunkDataPool;

        _chunkSize = size;
        SizeInBytes = count * size * (uint)Marshal.SizeOf<Vector4i>();

        MeshSSBO = new(count * size, true);

        Allocations.Add(new() { DataPool = this, Offset = 0, Size = count });

        _descriptors        = new Descriptor[PASS_COUNT];
        _blankDescriptors   = new Descriptor[PASS_COUNT];
        _prePassDescriptors = new Descriptor[PASS_COUNT];

        _indirectSSBOs      = new IDBO<DrawCommand>[GFX.MAX_FRAMES_IN_FLIGHT][];
        _drawCommands       = new DrawCommand[GFX.MAX_FRAMES_IN_FLIGHT][][];
        _chunkCounts        = new int[GFX.MAX_FRAMES_IN_FLIGHT][];

        _matrixSSBO         = new(count);
        _matrices           = new Matrix4[count];

        for (int i = 0; i < GFX.MAX_FRAMES_IN_FLIGHT; i++)
        {
            _indirectSSBOs[i] = new IDBO<DrawCommand>[PASS_COUNT];
            _drawCommands[i] = new DrawCommand[PASS_COUNT][];
            _chunkCounts[i] = new int[PASS_COUNT];

            for (int j = 0; j < PASS_COUNT; j++)
            {
                _indirectSSBOs[i][j] = new(count, true);
                _drawCommands[i][j] = new DrawCommand[count];
            }
        }

        for (int i = 0; i < PASS_COUNT; i++)
        {
            var descriptor = VoxelRenderer.WorldShader.GetDescriptorSet();  
            var blankDescriptor = VoxelRenderer.BlankWorldShader.GetDescriptorSet();  
            var prePassDescriptor = VoxelRenderer.TestPrePassShader.GetDescriptorSet();  

            _descriptors[i] = descriptor;
            _blankDescriptors[i] = blankDescriptor;
            _prePassDescriptors[i] = prePassDescriptor;

            descriptor.BindSSBO(BlockData.FaceGeometrySSBO, 0);
            descriptor.BindSSBO(MeshSSBO, 1);
            descriptor.BindSSBO(_matrixSSBO, 2);
            descriptor.BindTextureArray(BlockData.BlockTextureArray, 5);
            descriptor.BindFramebufferDepth(VoxelRenderer.CloseFBO, 6);
            descriptor.BindFramebufferDepth(VoxelRenderer.MiddleFBO, 7);
            descriptor.BindFramebufferDepth(VoxelRenderer.FarFBO, 8);
            
            blankDescriptor.BindSSBO(BlockData.FaceGeometrySSBO, 0);
            blankDescriptor.BindSSBO(MeshSSBO, 1);
            blankDescriptor.BindSSBO(_matrixSSBO, 2);
            blankDescriptor.BindTextureArray(BlockData.BlockTextureArray, 4);
            
            prePassDescriptor.BindSSBO(BlockData.FaceGeometrySSBO, 0);
            prePassDescriptor.BindSSBO(MeshSSBO, 1);
            prePassDescriptor.BindSSBO(_matrixSSBO, 2);
        }    
    }

    public bool TryAllocate(uint size, out Allocation alloc)
    {
        alloc = new() { DataPool = this, VertexCount = size };
        uint chunkCount = (uint)Mathf.CeilToInt((float)size / (float)_chunkSize);

        for (int i = 0; i < Allocations.Count; i++)
        {
            var a = Allocations[i];
            if (a.Size >= chunkCount)
            {
                alloc.Offset = a.Offset;
                alloc.Size = chunkCount;

                if (a.Size == chunkCount)
                {
                    Allocations.RemoveAt(i);
                }
                else
                {
                    a.Offset += chunkCount;
                    a.Size -= chunkCount;
                    Allocations[i] = a;
                }

                return true;
            }
        }

        return false;
    }

    public void Update(VoxelChunk chunk, Vector4i[] data)
    {
        _chunkDataPool.Updated = true;
        
        nint stride = Marshal.SizeOf<Vector4i>();
        MeshSSBO.Update(data, (ulong)(chunk.Allocation.Offset * _chunkSize * stride), 0);
        for (int i = 0; i < chunk.Allocation.Size; i++)
        {
            long index = chunk.Allocation.Offset + i;
            _matrices[index] = chunk.ModelMatrix;
        }

        _updateChunkData = true;
        if (chunk.Allocation.Start < _updateStart) _updateStart = chunk.Allocation.Start;
        if (chunk.Allocation.End > _updateEnd) _updateEnd = chunk.Allocation.End;
    }

    public void Free(VoxelChunk chunk)
    {
        _chunkDataPool.Updated = true;
        
        var alloc = chunk.Allocation;
        for (int i = 0; i < Allocations.Count; i++)
        {
            var a = Allocations[i];

            if (alloc.Offset < a.Offset)
            {
                Allocations.Insert(i, alloc);
                MergeAround(i);
                return;
            }
        }

        Allocations.Add(alloc);
        MergeAround(Allocations.Count - 1);
    }

    private void MergeAround(int index)
    {
        var current = Allocations[index];
        if (index > 0)
        {
            var prev = Allocations[index - 1];
            if (prev.End == current.Offset)
            {
                prev.Size += current.Size;
                Allocations[index - 1] = prev;
                Allocations.RemoveAt(index);
                index--;
                current = prev;
            }
        }

        if (index < Allocations.Count - 1)
        {
            var next = Allocations[index + 1];
            if (current.End == next.Offset)
            {
                current.Size += next.Size;
                Allocations[index] = current;
                Allocations.RemoveAt(index + 1);
            }
        }
    }

    public void Reset()
    {
        for (int i = 0; i < PASS_COUNT; i++)
        {
            _visibleChunks[i] = 0;
        }
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
        var visibleChunks = _visibleChunks[passIndex];
        if (visibleChunks == 0)
        {
            _chunkCounts[GFX.CurrentFrame][passIndex] = 0;
            return;
        }
        
        _indirectSSBOs[GFX.CurrentFrame][passIndex].Update(_drawCommands[GFX.CurrentFrame][passIndex], 0, (uint)visibleChunks * (uint)Marshal.SizeOf<DrawCommand>());
        if (_updateChunkData && _updateEnd > _updateStart)
        {
            _matrixSSBO.UpdateSlice(_matrices, _updateStart * Matrix4.ByteSize, (_updateEnd - _updateStart) * Matrix4.ByteSize);

            _updateChunkData = false;
            _updateStart = ChunkDataPool.CHUNK_COUNT_PER_POOL;
            _updateEnd = 0;
        }

        _chunkCounts[GFX.CurrentFrame][passIndex] = visibleChunks;
        _visibleChunks[passIndex] = 0;
    }

    public void UpdateDescriptorUniform(Action<Descriptor> action, int passIndex)
    {
        action.Invoke(_descriptors[passIndex]);
    }

    public void RenderPrePass(IVoxelRenderer renderer, int passIndex = 0)
    {
        if (_chunkCounts[GFX.CurrentFrame][passIndex] == 0)
            return;

        var prePassDescriptor = _prePassDescriptors[passIndex];
        var cam = renderer.GetCamera();
        
        prePassDescriptor.Bind();
        prePassDescriptor.Uniform(VoxelRenderer.PrePassView, cam.ViewMatrix);
        prePassDescriptor.Uniform(VoxelRenderer.PrePassProjection, cam.ProjectionMatrix);

        GFX.DrawIndirect(_indirectSSBOs[GFX.CurrentFrame][passIndex].Buffer, 0, (uint)_chunkCounts[GFX.CurrentFrame][passIndex], (uint)Marshal.SizeOf<DrawCommand>());
    }

    public void Render(IVoxelRenderer renderer, int passIndex = 0)
    {
        if (_chunkCounts[GFX.CurrentFrame][passIndex] == 0)
            return;

        var descriptor = _descriptors[passIndex];
        
        descriptor.Bind();
        renderer.UpdateUniforms(descriptor);

        GFX.Vk.CmdDrawIndirect(GFX.CommandBuffer, _indirectSSBOs[GFX.CurrentFrame][passIndex].Buffer, 0, (uint)_chunkCounts[GFX.CurrentFrame][passIndex], (uint)Marshal.SizeOf<DrawCommand>());
    }


    public void RenderBlank(Matrix4 view, Matrix4 projection, int passIndex = 0)
    {
        if (_chunkCounts[GFX.CurrentFrame][passIndex] == 0)
            return;

        var descriptor = _blankDescriptors[passIndex];

        descriptor.Bind();
        descriptor.UniformMatrix4(VoxelRenderer.BlankWorldViewLocation, view);
        descriptor.UniformMatrix4(VoxelRenderer.BlankWorldProjectionLocation, projection);

        GFX.Vk.CmdDrawIndirect(GFX.CommandBuffer, _indirectSSBOs[GFX.CurrentFrame][passIndex].Buffer, 0, (uint)_chunkCounts[GFX.CurrentFrame][passIndex], (uint)Marshal.SizeOf<DrawCommand>());
    }
    
    public void Dispose()
    {
        MeshSSBO.Dispose();
        _matrixSSBO.Dispose();

        for (int i = 0; i < GFX.MAX_FRAMES_IN_FLIGHT; i++)
        for (int j = 0; j < PASS_COUNT; j++)
        {
            _indirectSSBOs[i][j].Dispose();
        }
        
        for (int i = 0; i < PASS_COUNT; i++)
        {
            _descriptors[i].Dispose();
            _prePassDescriptors[i].Dispose();
        }  

        _descriptors = [];
        _prePassDescriptors = [];
    }
}

public struct Allocation
{
    public GPUChunkDataPool DataPool;
    public VoxelChunk Chunk;
    public uint VertexCount;
    public uint Offset;
    public uint Size;

    public readonly uint Start => Offset;
    public readonly uint End => Offset + Size;

    public void Set(Allocation allocation)
    {
        DataPool = allocation.DataPool;
        VertexCount = allocation.VertexCount;
        Offset = allocation.Offset;
        Size = allocation.Size;
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