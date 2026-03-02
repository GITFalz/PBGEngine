using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;

namespace PBG.Voxel;

public static class ChunkDataPool
{
    public static List<GPUChunkDataPool> DataPool = [];
    public const uint CHUNK_COUNT_PER_POOL = 6000;
    public const uint SLOT_SIZE = 2048; // in vertex count (Vector4i * N)

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

    public static void UpdateDrawCommands(int passIndex = 0)
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].UpdateDrawCommands(passIndex);
    }

    public static void Render(int passIndex = 0)
    {
        for (int i = 0; i < DataPool.Count; i++)
            DataPool[i].Render(passIndex);
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
    public int MeshID;
    public ulong SizeInBytes;
    private uint _chunkSize;

    private int[] _indirectIDs;
    private int[] _matrixIDs;

    private DrawCommand[][] _drawCommands;
    private Matrix4[][] _matrices;

    public int VisibleChunks = 0;
    private int _chunkCount = 0;

    public List<Allocation> Allocations = [];

    //private static VAO _vao = new();

    public const int PASS_COUNT = 4;

    public GPUChunkDataPool(uint count, uint size)
    {
        /*
        _chunkSize = size;
        SizeInBytes = count * size * (uint)Marshal.SizeOf<Vector4i>();

        MeshID = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, MeshID);
        GL.NamedBufferStorage(MeshID, (nint)SizeInBytes, IntPtr.Zero, BufferStorageFlags.DynamicStorageBit);

        Allocations.Add(new() { DataPool = this, Offset = 0, Size = count });

        _indirectIDs = new int[PASS_COUNT];
        _matrixIDs = new int[PASS_COUNT];

        _drawCommands = new DrawCommand[PASS_COUNT][];
        _matrices = new Matrix4[PASS_COUNT][];

        for (int i = 0; i < PASS_COUNT; i++)
        {
            _drawCommands[i] = new DrawCommand[count];
            _matrices[i] = new Matrix4[count];

            _indirectIDs[i] = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectIDs[i]);
            GL.BufferData(BufferTarget.DrawIndirectBuffer, (nint)(count * Marshal.SizeOf<DrawCommand>()), IntPtr.Zero, BufferUsageHint.DynamicDraw);

            _matrixIDs[i] = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _matrixIDs[i]);
            GL.NamedBufferStorage(_matrixIDs[i], (int)count * Marshal.SizeOf<Matrix4>(), IntPtr.Zero, BufferStorageFlags.DynamicStorageBit);
        }  
        */
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
        /*
        nint stride = Marshal.SizeOf<Vector4i>();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, MeshID);
        GL.NamedBufferSubData(MeshID, (nint)(chunk.Allocation.Offset * _chunkSize * stride), (int)(data.Length * stride), data);
        */
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
            _drawCommands[passIndex][VisibleChunks] = drawCommand;

            _matrices[passIndex][VisibleChunks] = chunk.ModelMatrix;

            vertexCount = (int)newVertexCount;
            VisibleChunks++;
        }
    }

    public void UpdateDrawCommands(int passIndex = 0)
    {
        if (VisibleChunks == 0)
        {
            _chunkCount = 0;
            return;
        }
        
        /*
        GL.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectIDs[passIndex]);
        GL.NamedBufferSubData(_indirectIDs[passIndex], 0, VisibleChunks * Marshal.SizeOf<DrawCommand>(), _drawCommands[passIndex]);

        GL.BindBuffer(BufferTarget.DrawIndirectBuffer, _matrixIDs[passIndex]);
        GL.NamedBufferSubData(_matrixIDs[passIndex], 0, VisibleChunks * Marshal.SizeOf<Matrix4>(), _matrices[passIndex]);
        */

        _chunkCount = VisibleChunks;
        VisibleChunks = 0;
    }

    public void Render(int passIndex = 0)
    {
        if (_chunkCount == 0)
            return;

        /*
        _vao.Bind();
        GL.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectIDs[passIndex]);

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, MeshID);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, MeshID);  

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _matrixIDs[passIndex]);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, _matrixIDs[passIndex]);

        GL.MultiDrawArraysIndirect(PrimitiveType.Triangles, IntPtr.Zero, _chunkCount, Marshal.SizeOf<DrawCommand>());
        Shader.Error("Indirect buffer error: ");

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        GL.BindBuffer(BufferTarget.DrawIndirectBuffer, 0);
        */
    }
    
    public void Dispose()
    {
        /*
        GL.DeleteBuffer(MeshID);
        
        for (int i = 0; i < PASS_COUNT; i++)
        {
            GL.DeleteBuffer(_indirectIDs[i]);
            GL.DeleteBuffer(_matrixIDs[i]);
        }  
        */
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