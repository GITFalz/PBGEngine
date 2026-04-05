using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

    public VoxelChunk(VoxelRenderer renderer, Vector3i position)
    {
        Renderer = renderer;
        Position = position;
        Center = position + 16;

        ModelMatrix = Matrix4.CreateTranslation(Position);
    }

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

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BlockVertexData
{
    public float PX, PY, PZ;
    public float UX, UY;
    public float NX, NY, NZ;
    public int TextureIndex;

    public BlockVertexData(Vector3 position, Vector2 uvs, Vector3 normals, int texture)
    {
        PX = position.X; PY = position.Y; PZ = position.Z;
        UX = uvs.X; UY = uvs.Y;
        NX = normals.X; NY = normals.Y; NZ = normals.Z;
        TextureIndex = texture;
    }

    public BlockVertexData(Vector3 position, Vector2 uvs, Vector3 normals, int texture, int side, int corner)
    {
        PX = position.X; PY = position.Y; PZ = position.Z;
        UX = uvs.X; UY = uvs.Y;
        NX = normals.X; NY = normals.Y; NZ = normals.Z;
        TextureIndex = texture | (side << 16) | (corner << 20);
    }

    public override string ToString()
    {
        int texture = TextureIndex & 0xFFFF;
        int ao = TextureIndex >> 16;

        return $"P({PX},{PY},{PZ}) U({UX},{UY}) N({NX},{NY},{NZ}) T:{texture} AO:{ao}";
    }

    public void SetAmbientOcclusion(int ao)
    {
        TextureIndex = (TextureIndex & 0x0000FFFF) | (ao << 16);
    }
}