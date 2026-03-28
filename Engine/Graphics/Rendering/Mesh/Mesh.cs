using System.Runtime.InteropServices;
using PBG.Asset;
using PBG.Graphics;
using PBG.MathLibrary;

namespace PBG.Rendering;

public unsafe class Mesh : IDisposable
{
    public string LocalPath = "";
    public string GlobalPath => Game.CurrentProjectPath / LocalPath;

    private VBO<MeshVertex>? _vertexBuffer = null;
    private SubMesh[] _subMeshes;
    private SubMeshInfo[] _subMeshInfos;

    public Vector3[] Vertices = [];
    public Vector3[] Normals = [];
    public Vector2[] Uvs = [];
    public int[] TextureIndices = [];
    public uint[] Indices = [];

    public bool IsCached => MeshCache.MeshesHash.Contains(this);

    public Mesh()
    {
        _subMeshes = [];
        _subMeshInfos = [];
    }
    public Mesh(string path) : this()
    {
        LocalPath = path;
    }

    public static implicit operator Asset<Mesh>(Mesh mesh) => new(mesh);

    public void ClearMesh()
    {
        Vertices = [];
        Normals = [];
        Uvs = [];
        TextureIndices = [];
        Indices = [];

        _vertexBuffer?.Dispose();
        for (int i = 0; i < _subMeshes.Length; i++)
            _subMeshes[i].Dispose();
    }

    private MeshVertex[] BuildVertexBuffer()
    {
        var vertices = new MeshVertex[Vertices.Length];
        for (int i = 0; i < Vertices.Length; i++)
        {
            vertices[i] = new MeshVertex
            {
                Position     = Vertices[i],
                Normal       = i < Normals.Length        ? Normals[i]        : Vector3.Zero,
                Uv           = i < Uvs.Length            ? Uvs[i]            : Vector2.Zero,
                TextureIndex = i < TextureIndices.Length ? TextureIndices[i] : 0,
            };
        }
        return vertices;
    }

    public void Generate()
    {
        if (_subMeshInfos.Length == 0)
        {
            return;
        }

        var _vertices = BuildVertexBuffer();

        if (_vertexBuffer == null)
            _vertexBuffer = new(_vertices);
        else
            _vertexBuffer.Renew(_vertices);

        if (_subMeshInfos.Length == 1)
        {
            _subMeshInfos[0].Start = 0;
            _subMeshInfos[0].Count = (uint)Indices.Length;
        }
        
        for (int i = 0; i < _subMeshes.Length; i++)
        {
            _subMeshes[i].Dispose();
        }

        _subMeshes = new SubMesh[_subMeshInfos.Length];

        for (int i = 0; i < _subMeshInfos.Length; i++)
        {
            _subMeshes[i] = new(this, _subMeshInfos[i]);
            _subMeshes[i].Generate();
        }
    
        Vertices = [];
        Normals = [];
        Uvs = [];
        TextureIndices = [];
        Indices = [];
    }

    public void SetSubMeshes(SubMeshInfo[] subMeshInfos)
    {
        _subMeshInfos = subMeshInfos;
    }

    public void Render(MeshRenderer meshRenderer)
    {
        if (_subMeshInfos.Length == 0)
            return;

        _vertexBuffer?.Bind();
        for (int i = 0; i < _subMeshes.Length; i++)
            _subMeshes[i].Render(meshRenderer);
    }

    public void Dispose()
    {
        ClearMesh();
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct MeshVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Uv;
        public int     TextureIndex;
    }
}