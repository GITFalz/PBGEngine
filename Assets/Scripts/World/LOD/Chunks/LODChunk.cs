using System.Runtime.InteropServices;
using PBG.MathLibrary;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Threads;
using PBG.Voxel;

public class LODChunk : LODBaseChunk
{
    public ThreadProcess? Process;
    public bool Restart = false;
    public bool ToBeRemoved = false;

    public LODVoxelRenderer Renderer;
    public System.Numerics.Vector3 CenterNum;
    public Vector3i Center;
    public Vector3i WorldPosition;
    public Matrix4 ModelMatrix;

    //public VAO ChunkVAO = new();
    //public IBO ChunkIBO = new();        
    public List<uint> ChunkIndices = [];

    public ChunkStatus Status = ChunkStatus.Empty;

    //public VBO<BlockVertexData> ChunkVertexVBO = new();
    public List<BlockVertexData> ChunkVertices = [];
    //public Texture? AmbientOcclusionTexture = null;

    public int VertexCount = 0;
    private uint _indexOffset = 0;
    public int Level = 0;

    public bool HasBlocks = false;
    public bool IsDisabled = false;
    public bool ForceDisabled = false;

    public Block[] Blocks = [];

    public object _lock = new object();

    public LODChunk(LODVoxelRenderer renderer, Vector3i position, int level)
    {
        Level = level;
        Renderer = renderer;
        WorldPosition = position;
        int p = 1 << level;
        CenterNum = Mathf.Num(WorldPosition + new Vector3i(16, 16, 16) * p);
        Center = WorldPosition + new Vector3i(16, 16, 16) * p;
        ModelMatrix = Matrix4.CreateScale(p) * Matrix4.CreateTranslation(WorldPosition);
    }

    public bool InBounds(Vector3i pos) => InBounds(pos.X, pos.Y, pos.Z);
    public bool InBounds(int x, int y, int z)
    {
        int p = 32 * (1 << Level);
        int lx = x - WorldPosition.X;
        int ly = y - WorldPosition.Y;
        int lz = z - WorldPosition.Z;

        return (uint)lx < p && (uint)ly < p && (uint)lz < p;
    }

    public void GenerateChunkMesh()
    {
        /*
        if (_isDisposed || (Process != null && Process.Failed))
            return;
            
        if (!ChunkVAO.IsValid)
        {
            throw new Exception("VAO is not valid for chunk at " + WorldPosition);
        }

        ChunkVAO.Renew();
        ChunkVAO.Bind();

        ChunkVertexVBO.Renew(ChunkVertices);
        ChunkIBO.Renew(ChunkIndices);

        ChunkVertexVBO.Bind();
        int stride = Marshal.SizeOf<BlockVertexData>();

        ChunkVAO.Link(0, 3, VertexAttribPointerType.Float, stride, 0);
        ChunkVAO.Link(1, 3, VertexAttribPointerType.Float, stride, sizeof(float) * 5);
        ChunkVAO.Link(2, 2, VertexAttribPointerType.Float, stride, sizeof(float) * 3);
        ChunkVAO.IntLink(3, 1, VertexAttribIntegerType.Int, stride, sizeof(float) * 8);

        ChunkVertexVBO.Unbind();

        ChunkVAO.Unbind();

        VertexCount = ChunkIndices.Count;
        _indexOffset = 0;
        HasBlocks = VertexCount > 0;

        //Console.WriteLine("Vertices: " + VertexCount);

        ChunkIndices = [];
        ChunkVertices = [];
        */
    }

    public void BindBuffers()
    {
        /*
        ChunkVAO.Renew();
        ChunkVAO.Bind();

        ChunkVertexVBO.Bind();
        int stride = Marshal.SizeOf<BlockVertexData>();

        ChunkVAO.Link(0, 3, VertexAttribPointerType.Float, stride, 0);
        ChunkVAO.Link(1, 2, VertexAttribPointerType.Float, stride, sizeof(float) * 3);
        ChunkVAO.Link(2, 3, VertexAttribPointerType.Float, stride, sizeof(float) * 5);
        ChunkVAO.IntLink(3, 1, VertexAttribIntegerType.Int, stride, sizeof(float) * 8);

        ChunkVertexVBO.Unbind();

        ChunkVAO.Unbind();
        */
    }

    /// <summary>
    /// Check if the block at the given position is air
    /// IMPORTANT: blockPos has to be relative to the chunk
    /// </summary>
    /// <param name="blockPos"></param>
    /// <returns></returns>
    public bool IsAir(Vector3i blockPos) => Blocks[ChunkBlocks.GetIndex(blockPos.X, blockPos.Y, blockPos.Z)].IsAir();

    public void BreakProcess() 
    {
        Process?.Break();
    } 

    public bool HasSolidBlocks()
    {
        for (int i = 0; i < Blocks.Length; i++)
        {
            if (!Blocks[i].IsAir())
                return true;
        }
        return false;
    }

    public void Render()
    {
        /*
        try
        {
            ChunkVAO.Bind();
            ChunkIBO.Bind();

            GL.DrawElements(PrimitiveType.Triangles, VertexCount, DrawElementsType.UnsignedInt, 0);
            Shader.Error("Chunk rendering error: ");

            ChunkIBO.Unbind();
            ChunkVAO.Unbind();
        }
        catch (Exception ex)
        {
            throw new Exception($"[Error] Was vao deleted?: {ForceDisabled} ", ex);
        }
        */
    }

    private bool _isDisposed = false;

    public void Dispose()
    {
        Process?.Break();

        ChunkIndices = [];
        ChunkVertices = [];

        if (_isDisposed)
        {
            Console.WriteLine($"[WARNING] Double dispose attempted on chunk at {WorldPosition}");
            return;
        }
        
        _isDisposed = true;

        if (ForceDisabled)
            return;
            
        ForceDisabled = true;
        Blocks = [];

        /*
        try
        {
            if (ChunkVAO != null && ChunkVAO.IsValid)
            {
                ChunkVAO.DeleteBuffer();
            }
            
            ChunkIBO?.DeleteBuffer();
            ChunkVertexVBO?.DeleteBuffer();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARNING] Error disposing chunk at {WorldPosition}: {ex.Message}");
        }
        */
    }
}