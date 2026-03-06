using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;

namespace PBG.Voxel;

public static class ChunkDataPool
{
    public static List<GPUChunkDataPool> DataPool = [];
    public const uint CHUNK_COUNT_PER_POOL = 8196;
    public const uint SLOT_SIZE = 8196; // in vertex count (Vector4i * N)

    public static bool TryAllocate(uint size, out Allocation alloc)
    {
        if (DataPool.Count == 0)
            DataPool.Add(new(CHUNK_COUNT_PER_POOL, SLOT_SIZE));

        for (int i = 0; i < DataPool.Count; i++)
        {
            if (DataPool[i].TryAllocate(size, out alloc))
                return true;
        }

        DataPool.Add(new(CHUNK_COUNT_PER_POOL, SLOT_SIZE));
        if (DataPool[^1].TryAllocate(size, out alloc))
            return true;

        return false;
    }

    public static void UpdateDrawCommands(VoxelRenderer renderer, int passIndex = 0)
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].UpdateDrawCommands(renderer, passIndex);
    }

    public static void RenderPrePass(VoxelRenderer renderer, int passIndex = 0)
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].RenderPrePass(renderer, passIndex);
    }

    public static void Render(VoxelRenderer renderer, int passIndex = 0)
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].Render(renderer, passIndex);
    }

    public static void Dispose()
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].Dispose();
        DataPool = [];
    }
}

public class GPUChunkDataPool : IDisposable
{
    public SSBO<Vector4i> MeshSSBO;
    public ulong SizeInBytes;
    private uint _chunkSize;

    private Descriptor[] _descriptors;
    private Descriptor[] _prePassDescriptors;

    private IDBO<DrawCommand>[] _indirectSSBOs;
    private DrawCommand[][] _drawCommands;

    private SSBO<Matrix4> _matrixSSBO;
    private Matrix4[] _matrices;

    private bool _updateChunkData = false;
    private uint _updateStart = ChunkDataPool.CHUNK_COUNT_PER_POOL;
    private uint _updateEnd = 0;


    public int VisibleChunks = 0;
    private int _chunkCount = 0;

    public List<Allocation> Allocations = [];

    public const int PASS_COUNT = 4;

    public static int vertexCount = 0;

    public GPUChunkDataPool(uint count, uint size)
    {
        _chunkSize = size;
        SizeInBytes = count * size * (uint)Marshal.SizeOf<Vector4i>();

        MeshSSBO = new(count * size, false);

        Allocations.Add(new() { DataPool = this, Offset = 0, Size = count });

        _descriptors        = new Descriptor[PASS_COUNT];
        _prePassDescriptors = new Descriptor[PASS_COUNT];

        _indirectSSBOs      = new IDBO<DrawCommand>[PASS_COUNT];
        _drawCommands       = new DrawCommand[PASS_COUNT][];

        _matrixSSBO         = new(count);
        _matrices           = new Matrix4[count];

        for (int i = 0; i < PASS_COUNT; i++)
        {
            var descriptor = VoxelRenderer.TestShader.GetDescriptorSet();  
            var prePassDescriptor = VoxelRenderer.TestPrePassShader.GetDescriptorSet();  

            _descriptors[i] = descriptor;
            _prePassDescriptors[i] = prePassDescriptor;

            _indirectSSBOs[i] = new(count, true);
            _drawCommands[i] = new DrawCommand[count];

            descriptor.BindTextureArray(BlockData.BlockTextureArray, 5);
            descriptor.BindSSBO(BlockData.FaceGeometrySSBO, 0);
            descriptor.BindSSBO(MeshSSBO, 1);
            descriptor.BindSSBO(_matrixSSBO, 2);

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

    public void UpdateDrawCommand(VoxelChunk chunk, Allocation alloc, int passIndex = 0)
    {
        int vertexCount = (int)alloc.VertexCount;
        for (int i = 0; i < alloc.Size; i++)
        {
            if (vertexCount <= 0)
                return;

            var newVertexCount = Mathf.Max(vertexCount - _chunkSize, 0);
            
            var drawCommand = _drawCommands[passIndex][VisibleChunks];
            drawCommand.InstanceCount = 1;
            drawCommand.Count = ((uint)vertexCount - (uint)newVertexCount) * 6;
            drawCommand.First = (uint)(alloc.Offset + i) * _chunkSize * 6;
            drawCommand.BaseInstance = alloc.Offset + (uint)i;
            _drawCommands[passIndex][VisibleChunks] = drawCommand;

            vertexCount = (int)newVertexCount;
            VisibleChunks++;
        }
    }

    public void UpdateDrawCommands(VoxelRenderer renderer, int passIndex = 0)
    {
        if (VisibleChunks == 0)
        {
            _chunkCount = 0;
            return;
        }
        
        _indirectSSBOs[passIndex].Update(_drawCommands[passIndex], 0, (uint)VisibleChunks * (uint)Marshal.SizeOf<DrawCommand>());
        if (_updateChunkData && _updateEnd > _updateStart)
        {
            _matrixSSBO.Update(_matrices, _updateStart * Matrix4.ByteSize, (_updateEnd - _updateStart) * Matrix4.ByteSize, true);

            _updateChunkData = false;
            _updateStart = ChunkDataPool.CHUNK_COUNT_PER_POOL;
            _updateEnd = 0;
        }

        _chunkCount = VisibleChunks;
    }

    public void RenderPrePass(VoxelRenderer renderer, int passIndex = 0)
    {
        if (_chunkCount == 0)
            return;

        var prePassDescriptor = _prePassDescriptors[passIndex];
        
        prePassDescriptor.Bind();
        prePassDescriptor.Uniform(VoxelRenderer.PrePassView, renderer.Camera.ViewMatrix);
        prePassDescriptor.Uniform(VoxelRenderer.PrePassProjection, renderer.Camera.ProjectionMatrix);

        GFX.Vk.CmdDrawIndirect(GFX.CommandBuffer, _indirectSSBOs[passIndex].Buffer, 0, (uint)_chunkCount, (uint)Marshal.SizeOf<DrawCommand>());

        VisibleChunks = 0;
    }

    public void Render(VoxelRenderer renderer, int passIndex = 0)
    {
        if (_chunkCount == 0)
            return;

        var descriptor = _descriptors[passIndex];
        
        descriptor.Bind();
        descriptor.Uniform(VoxelRenderer.View, renderer.Camera.ViewMatrix);
        descriptor.Uniform(VoxelRenderer.Projection, renderer.Camera.ProjectionMatrix);
        descriptor.Uniform(VoxelRenderer.LightDirectionLocation, renderer.LightDirection);
        descriptor.Uniform(VoxelRenderer.CameraPosition, renderer.Camera.Position);
        descriptor.Uniform(VoxelRenderer.DoAmbientOcclusion, renderer.AmbientOcclusion ? 1 : 0);

        GFX.Vk.CmdDrawIndirect(GFX.CommandBuffer, _indirectSSBOs[passIndex].Buffer, 0, (uint)_chunkCount, (uint)Marshal.SizeOf<DrawCommand>());

        VisibleChunks = 0;
    }
    
    public void Dispose()
    {
        MeshSSBO.Dispose();
        _matrixSSBO.Dispose();
        
        for (int i = 0; i < PASS_COUNT; i++)
        {
            _indirectSSBOs[i].Dispose();
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