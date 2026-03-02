using PBG.Core;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;

namespace PBG.Voxel;

public class VoxelMesh
{
    //private VertexDataType Type;
    // Mesh buffers
    private IBO? _ibo = null!;

    private VBO<Vector3>? _vertexVbo = null;
    private VBO<Vector3>? _normalVbo = null;
    private VBO<Vector2>? _uvVbo = null;
    private VBO<Vector3>? _uv3Vbo = null;
    private VBO<int>? _textureIndexVbo = null;

    //private VAO? _vao = null!;

    public List<Vector3> Vertices = [];
    public List<Vector3> Normals = [];
    public List<Vector2> Uvs = [];
    public List<Vector3> Uv3s = [];
    public List<int> TextureIndices = [];
    public List<uint> Indices = [];

    private int _vertexCount = 0;

    /*
    public VoxelMesh(VertexDataType type)
    {
        Type = type;
    }
    */

    /*
    private void HandleBuffer<T>(ref VBO<T>? vbo, VertexDataType type, List<T> data, ref int count) where T : struct
    {
        if (vbo != null)
        {
            if (count == -1 || count > data.Count)
            {
                count = data.Count;
            }
            vbo.Renew(data);
        }
        else if ((Type & type) == type)
        {
            if (count == -1 || count > data.Count)
            {
                count = data.Count;
            }
            vbo = new(data);
        }
    }
    */

    public void GenerateMesh()
    {
        /*
        _vertexCount = 0;

        if (Indices.Count == 0)
        {
            _vao?.DeleteBuffer();
            _ibo?.DeleteBuffer();

            Vertices = [];
            Normals = [];
            Uvs = [];
            Uv3s = [];
            TextureIndices = [];
            Indices = [];

            return;
        }

        int count = -1;

        HandleBuffer(ref _vertexVbo, VertexDataType.Position, Vertices, ref count);
        HandleBuffer(ref _normalVbo, VertexDataType.Normal, Normals, ref count);
        HandleBuffer(ref _uvVbo, VertexDataType.Uv, Uvs, ref count);
        HandleBuffer(ref _uv3Vbo, VertexDataType.Uv3, Uv3s, ref count);
        HandleBuffer(ref _textureIndexVbo, VertexDataType.TextureIndex, TextureIndices, ref count);

        if (count == -1)
        {
            _vertexCount = 0;
            return;
        }

        _vertexCount = Indices.Count;

        if (_vao == null)
            _vao = new();
        else
            _vao.Renew();

        _vao.Bind();

        if (_ibo == null)
            _ibo = new(Indices);
        else
            _ibo.Renew(Indices);

        int buffer = 0;

        if (_vertexVbo != null)
        {
            _vao.LinkToVAO(buffer, 3, VertexAttribPointerType.Float, 3 * sizeof(float), 0, _vertexVbo);
            buffer++;
        }

        if (_normalVbo != null)
        {
            _vao.LinkToVAO(buffer, 3, VertexAttribPointerType.Float, 3 * sizeof(float), 0, _normalVbo);
            buffer++;
        }

        if (_uvVbo != null)
        {
            _vao.LinkToVAO(buffer, 2, VertexAttribPointerType.Float, 2 * sizeof(float), 0, _uvVbo);
            buffer++;
        }

        if (_uv3Vbo != null)
        {
            _vao.LinkToVAO(buffer, 3, VertexAttribPointerType.Float, 3 * sizeof(float), 0, _uv3Vbo);
            buffer++;
        }

        if (_textureIndexVbo != null)
        {
            _vao.IntLinkToVAO(buffer, 1, VertexAttribIntegerType.Int, sizeof(int), 0, _textureIndexVbo);
        }

        _vao.Unbind();

        Vertices = [];
        Normals = [];
        Uvs = [];
        Uv3s = [];
        TextureIndices = [];
        Indices = [];
        */
    }

    public bool HasVertices() => _vertexCount > 0;

    public void Render()
    {
        /*
        if (_vao == null) return;

        _vao.Bind();
        _ibo!.Bind();

        GL.DrawElements(PrimitiveType.Triangles, _vertexCount, DrawElementsType.UnsignedInt, 0);

        _ibo.Unbind();
        _vao.Unbind();
        */
    }

    public void Dispose()
    {
        /*
        _vertexVbo?.DeleteBuffer();
        _normalVbo?.DeleteBuffer();
        _uvVbo?.DeleteBuffer();
        _uv3Vbo?.DeleteBuffer();
        _textureIndexVbo?.DeleteBuffer();
        _ibo?.DeleteBuffer();
        _vao?.DeleteBuffer();

        _vertexVbo = null;
        _normalVbo = null;
        _uvVbo = null;
        _uv3Vbo = null;
        _textureIndexVbo = null;
        _ibo = null;
        _vao = null;

        Vertices = [];
        Normals = [];
        Uvs = [];
        Uv3s = [];
        TextureIndices = [];
        Indices = [];
        */
    }
}