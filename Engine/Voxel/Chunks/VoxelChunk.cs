
using System.Runtime.InteropServices;
using PBG.Core;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.Threads;

namespace PBG.Voxel
{
    public class VoxelChunk
    {
        private static int _idCounter = 0;

        public int ID;

        public ThreadProcess? Process;
        public bool Restart = false;
        public bool ToBeRemoved = false;

        public VoxelRenderer Renderer;

        public Vector3i RelativePosition;
        public System.Numerics.Vector3 CenterNum;
        public Vector3i Center;
        public Vector3i WorldPosition;
        public Matrix4 ModelMatrix;

        public ChunkStatus Status = ChunkStatus.Empty;

        public Texture? AmbientOcclusionTexture = null;

        public IndirectVoxelMesh ChunkMesh; //new(VertexDataType.Position | VertexDataType.Normal | VertexDataType.Uv | VertexDataType.TextureIndex);
        public Allocation Allocation;
        public int[] ChunkInfoSlot;

        public bool HasBlocks = false;
        public bool ForceDisabled = false;
        public bool Visible = false;

        public ChunkBlocks? Blocks;

        public VoxelChunk(VoxelRenderer renderer, Vector3i position)
        {
            ID = _idCounter;
            _idCounter++;

            Blocks = new(this);
            Renderer = renderer;
            RelativePosition = position;
            WorldPosition = position * 32;
            CenterNum = Mathf.Num(WorldPosition + (16, 16, 16));
            Center = WorldPosition + (16, 16, 16);
            ModelMatrix = Matrix4.CreateTranslation(WorldPosition);

            Allocation = new()
            {
                Chunk = this
            };

            ChunkMesh = new(this);
        }

        public Block Get(Vector3i position) => Blocks?.Get(position) ?? Block.Air;
        public Block Get(int index) => Blocks?.Get(index) ?? Block.Air;

        public Block GetInner(Vector3i position) => Blocks?.GetInner(position) ?? Block.Air;
        public Block GetInner(int index) => Blocks?.Get(index) ?? Block.Air;

        public void Set(Vector3i position, Block block) => Blocks?.Set(position, block);
        public void Set(int index, Block block) => Blocks?.Set(index, block);


        public bool InBounds(Vector3i pos) => InBounds(pos.X, pos.Y, pos.Z);
        public bool InBounds(int x, int y, int z)
        {
            int lx = x - WorldPosition.X;
            int ly = y - WorldPosition.Y;
            int lz = z - WorldPosition.Z;

            return (uint)lx < 32u && (uint)ly < 32u && (uint)lz < 32u;
        }

        /*
        public void GenerateChunkMesh(DefaultVoxelChunkHandlerNew handler)
        {
            if (_isDisposed || (Process != null && Process.Failed))
                return;

            ChunkMesh.Vertices = handler.Vertices;
            ChunkMesh.Normals = handler.Normals;
            ChunkMesh.Uvs = handler.Uvs;
            ChunkMesh.TextureIndices = handler.TextureIndices;

            var vertexCount = (handler.Vertices.Count >> 2) * 6;

            uint index = 0;

            for (int i = 0; i < vertexCount; i+=6)
            {
                ChunkMesh.Indices.Add(index);
                ChunkMesh.Indices.Add(index+1);
                ChunkMesh.Indices.Add(index+2);
                ChunkMesh.Indices.Add(index+2);
                ChunkMesh.Indices.Add(index+3);
                ChunkMesh.Indices.Add(index);
                index += 4;
            }

            ChunkMesh.GenerateMesh();
            HasBlocks = ChunkMesh.HasVertices();
        }
        */

        public void GenerateChunkMesh(List<Vector4i> vertexData)
        {
            if (_isDisposed || (Process != null && Process.Failed))
                return;

            ChunkMesh.VertexData = [..vertexData];

            ChunkMesh.GenerateMesh();
            /*
            if (_isDisposed || (Process != null && Process.Failed))
                return;

            ChunkMesh.Vertices = handler.Vertices;
            ChunkMesh.Normals = handler.Normals;
            ChunkMesh.Uvs = handler.Uvs;
            ChunkMesh.TextureIndices = handler.TextureIndices;

            var vertexCount = (handler.Vertices.Count >> 2) * 6;

            uint index = 0;

            for (int i = 0; i < vertexCount; i+=6)
            {
                ChunkMesh.Indices.Add(index);
                ChunkMesh.Indices.Add(index+1);
                ChunkMesh.Indices.Add(index+2);
                ChunkMesh.Indices.Add(index+2);
                ChunkMesh.Indices.Add(index+3);
                ChunkMesh.Indices.Add(index);
                index += 4;
            }

            ChunkMesh.GenerateMesh();
            HasBlocks = ChunkMesh.HasVertices();
            */
        }

        public bool IsAir(Vector3i blockPos) => Blocks?.Get(blockPos).IsAir() ?? true;
        public bool IsSolid(Vector3i blockPos) => Blocks?.Get(blockPos).IsSolid() ?? false;

        public void BreakProcess() 
        {
            Process?.Break();
        } 

        public void Render()
        {
            ChunkMesh.Render();
        }

        private bool _isDisposed = false;

        public void Dispose()
        {
            Process?.Break();
            AmbientOcclusionTexture?.Dispose();
            ChunkMesh?.Dispose();

            if (_isDisposed)
            {
                Console.WriteLine($"[WARNING] Double dispose attempted on chunk at {RelativePosition}");
                return;
            }

            //Allocation.DataPool?.Free(this);
            
            _isDisposed = true;

            if (ForceDisabled)
                return;
                
            ForceDisabled = true;
            Blocks = null;
        }

        public bool HasAllNeighbourChunks()
        {
            int yStart = RelativePosition.Y <= 0 ? 0 : -1;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = yStart; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && y == 0 && z == 0)
                            continue;

                        if (!Renderer.GetChunk(RelativePosition + (x, y, z), out var neighborChunk))
                        {
                            return false;
                        }

                        if (neighborChunk.Status < ChunkStatus.Generated)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}

public enum ChunkStatus
{
    Empty = 0,
    Generated = 1,
    Rendered = 2,
}

[StructLayout(LayoutKind.Sequential)]
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