using System.Runtime.CompilerServices;
using PBG.MathLibrary;

namespace PBG.Voxel;

public abstract class BaseVoxelChunkHandler(Vector3i worldPosition, Block[] blocks)
{
    public Vector3i WorldPosition = worldPosition;
    public Block[] Blocks = blocks;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract Block GetBlock(Vector3i position);
    public abstract void AddFace(VoxelFace face, Vector3 position);
}

public class DefaultVoxelChunkHandler(VoxelChunk chunk) : BaseVoxelChunkHandler(chunk.WorldPosition, chunk.Blocks!.GetBlocks())
{
    public VoxelChunk Chunk = chunk;
    public List<Vector3> Vertices = [];
    public List<Vector3> Normals = [];
    public List<Vector2> Uvs = [];
    public List<int> TextureIndices = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Block GetBlock(Vector3i position)
    {
        int lx = position.X - Chunk.WorldPosition.X;
        int ly = position.Y - Chunk.WorldPosition.Y;
        int lz = position.Z - Chunk.WorldPosition.Z;

        if ((uint)lx < 32u & (uint)ly < 32u & (uint)lz < 32u)
            return Chunk.Blocks?.Get(ChunkBlocks.GetIndex(lx, ly, lz)) ?? Block.Air;

        return Chunk.Renderer.GetChunk(VoxelData.BlockToChunkRelative(position), out var c) ? c.Blocks?.Get(ChunkBlocks.GetIndex(lx, ly, lz)) ?? Block.Air : Block.Air;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void AddFace(VoxelFace face, Vector3 position)
    {
        Vertices.Add(face.A + position);
        Vertices.Add(face.B + position);
        Vertices.Add(face.C + position);
        Vertices.Add(face.D + position);

        Normals.Add(face.Normal);
        Normals.Add(face.Normal);
        Normals.Add(face.Normal);
        Normals.Add(face.Normal);

        Uvs.Add(face.UvA);
        Uvs.Add(face.UvB);
        Uvs.Add(face.UvC);
        Uvs.Add(face.UvD);

        var baseTexture = face.TextureIndex | (face.Side << 16);
        TextureIndices.Add(baseTexture);
        TextureIndices.Add(baseTexture | (1 << 20));
        TextureIndices.Add(baseTexture | (2 << 20));
        TextureIndices.Add(baseTexture | (3 << 20));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddFaceInternal(VoxelFace face, Vector3 position)
    {
        Vertices.Add(face.A + position);
        Vertices.Add(face.B + position);
        Vertices.Add(face.C + position);
        Vertices.Add(face.D + position);

        Normals.Add(face.Normal);
        Normals.Add(face.Normal);
        Normals.Add(face.Normal);
        Normals.Add(face.Normal);

        Uvs.Add(face.UvA);
        Uvs.Add(face.UvB);
        Uvs.Add(face.UvC);
        Uvs.Add(face.UvD);

        var baseTexture = face.TextureIndex | (face.Side << 16);
        TextureIndices.Add(baseTexture);
        TextureIndices.Add(baseTexture | (1 << 20));
        TextureIndices.Add(baseTexture | (2 << 20));
        TextureIndices.Add(baseTexture | (3 << 20));
    }
}


public class DefaultVoxelChunkHandlerNew(VoxelChunk chunk)
{
    public VoxelChunk Chunk = chunk;
    public List<Vector3> Vertices = [];
    public List<Vector3> Normals = [];
    public List<Vector2> Uvs = [];
    public List<int> TextureIndices = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Block GetBlock(Vector3i position)
    {
        int lx = position.X - Chunk.WorldPosition.X;
        int ly = position.Y - Chunk.WorldPosition.Y;
        int lz = position.Z - Chunk.WorldPosition.Z;

        if ((uint)lx < 32u & (uint)ly < 32u & (uint)lz < 32u)
            return Chunk.Blocks?.Get(ChunkBlocks.GetIndex(lx, ly, lz)) ?? Block.Air;

        return Chunk.Renderer.GetChunk(VoxelData.BlockToChunkRelative(position), out var c) ? c.Blocks?.Get(ChunkBlocks.GetIndex(lx, ly, lz)) ?? Block.Air : Block.Air;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddFace(VoxelFace face, Vector3 position)
    {
        Vertices.Add(face.A + position);
        Vertices.Add(face.B + position);
        Vertices.Add(face.C + position);
        Vertices.Add(face.D + position);

        Normals.Add(face.Normal);
        Normals.Add(face.Normal);
        Normals.Add(face.Normal);
        Normals.Add(face.Normal);

        Uvs.Add(face.UvA);
        Uvs.Add(face.UvB);
        Uvs.Add(face.UvC);
        Uvs.Add(face.UvD);

        var baseTexture = face.TextureIndex | (face.Side << 16);
        TextureIndices.Add(baseTexture);
        TextureIndices.Add(baseTexture | (1 << 20));
        TextureIndices.Add(baseTexture | (2 << 20));
        TextureIndices.Add(baseTexture | (3 << 20));
    }
}