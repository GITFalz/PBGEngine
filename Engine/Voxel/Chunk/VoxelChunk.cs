using System.Runtime.CompilerServices;
using PBG.Mathematics;

namespace PBG.Voxel;

public class VoxelChunk
{
    public Block[] Blocks = [];
    private int _nonAirBlocks = 0;

    public Vector3i Position;
    public Vector3i Center;
    public Matrix4 ModelMatrix;

    public Allocation Allocation;

    public VoxelRenderer Renderer;

    private Vector4i[] _vertexData = [];
    public uint VertexCount = 0;

    public VoxelChunk(VoxelRenderer renderer, Vector3i position)
    {
        Renderer = renderer;
        Position = position;
        Center = position + 16;

        ModelMatrix = Matrix4.CreateTranslation(Position);
    }

    public bool HasMesh() => VertexCount > 0;

    public void SetVertexData(Vector4i[] vertexData)
    {
        _vertexData = vertexData;
        VertexCount = (uint)_vertexData.Length;
    }

    public Vector4i[] GetVertexData() => _vertexData;
    public void ClearVertexData() => _vertexData = [];

    public bool InBounds(Vector3i pos) => InBounds(pos.X, pos.Y, pos.Z);
    public bool InBounds(int x, int y, int z)
    {
        int lx = x - Position.X;
        int ly = y - Position.Y;
        int lz = z - Position.Z;

        return (uint)lx < 32u && (uint)ly < 32u && (uint)lz < 32u;
    }

    public Block Get(Vector3i position)
    {
        if (Blocks.Length == 0) 
            return Block.Air;
        return Get(GetIndex(position));
    }

    public Block GetInner(Vector3i position)
    {
        if (Blocks.Length == 0) 
            return Block.Air;
        return Get(GetIndexInner(position.X, position.Y, position.Z));
    }

    public Block Get(int index)
    {
        return Blocks[index];
    }

    public void Set(Block block, Vector3i position) => Set(block, position.X, position.Y, position.Z);
    public void Set(Block block, int x, int y, int z)
    {
        int index = GetIndex(x, y, z);
        if (block.IsAir())
        {
            if (Blocks.Length == 0 || Blocks[index].IsAir())
                return;

            Blocks[index] = block;
            _nonAirBlocks--;

            if (_nonAirBlocks == 0)
            {
                Blocks = [];
            }
        }
        else
        {
            if (Blocks.Length == 0)
            {
                Blocks = new Block[32768];
                _nonAirBlocks++;
            }
            else if (Blocks[index].IsAir())
            {
                _nonAirBlocks++;
            }

            Blocks[index] = block;
        }
    }
    
    public void Clear()
    {
        for (int i = 0; i < Blocks.Length; i++)
            Blocks[i] = Block.Air;
    }

    /// <summary>
    /// Used when you are certain the x, y and z values are all between 0 and 31
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetIndexInner(int x, int y, int z) => x + z * 32 + y * 1024;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetIndex(int x, int y, int z) => (x & 31) + (z & 31) * 32 + (y & 31) * 1024;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetIndex(Vector3i position) => (position.X & 31) + (position.Z & 31) * 32 + (position.Y & 31) * 1024;
}
