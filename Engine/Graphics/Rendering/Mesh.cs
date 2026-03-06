using System.Runtime.InteropServices;
using PBG.Core;
using PBG.MathLibrary;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics;

public unsafe class Mesh : ScriptingNode, IDisposable
{
    private static bool _generated = false;
    private static Shader _shader = null!;
    private static int _modelLocation;
    private static int _viewLocation;
    private static int _projectionLocation;
    private static int _useVertexColorLocation;

    private Descriptor _descriptor;

    private GraphicsContext _context;

    private VBO<MeshVertex> _vertexBuffer = new([]);
    private IBO _indexBuffer = new([]);
    private uint _indexCount = 0;

    private CommandBuffer _commandBuffer;


    public Vector3[] Vertices = [];
    public Vector3[] Normals = [];
    public Vector2[] Uvs = [];
    public int[] TextureIndices = [];
    public uint[] Indices = [];

    public Mesh()
    {
        _context = GraphicsContext.graphicsContext;

        if (!_generated)
        {
            _shader = new Shader(new()
            {
                VertexShaderPath = Path.Combine(Game.ShaderPath, "mesh_vulkan", "mesh.vert"),
                FragmentShaderPath = Path.Combine(Game.ShaderPath, "mesh_vulkan", "mesh.frag")
            });

            _shader.BindVertexBuffer<MeshVertex>(0);

            _shader.Compile();

            _modelLocation = _shader.GetLocation("ubo.model");
            _viewLocation = _shader.GetLocation("ubo.view");
            _projectionLocation = _shader.GetLocation("ubo.projection");
            _useVertexColorLocation = _shader.GetLocation("uvc.useVertexColor");

            _generated = true;
        }

        _descriptor = _shader.GetDescriptorSet();
    }

    public void ClearMesh()
    {
        Vertices = [];
        Normals = [];
        Uvs = [];
        TextureIndices = [];
        Indices = [];

        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();

        _context.vk.FreeCommandBuffers(_context.device, _context.commandPool, 1, ref _commandBuffer);
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
        var _vertices = BuildVertexBuffer();
        _vertexBuffer.Renew(_vertices);
        _indexBuffer.Renew(Indices);

        _indexCount = (uint)Indices.Length;

        //_commandBuffer = _context.CreateCommandBuffer();
        //_context.RecordMeshCommandBuffer(shader, _commandBuffer, _vertexBuffers, _indexBuffer, _offsets, _indexCount);

        Vertices = [];
        Normals = [];
        Uvs = [];
        TextureIndices = [];
        Indices = [];
    }

    public void Render()
    {   
        _shader.Bind();
        _descriptor.Bind();

        _descriptor.Uniform(_modelLocation, Matrix4.CreateTranslation(Transform.Position) * Matrix4.CreateRotationX(Mathf.DegToRad(90))* Matrix4.CreateScale(0.001f));
        _descriptor.Uniform(_viewLocation, Camera.ViewMatrix);
        _descriptor.Uniform(_projectionLocation, Camera.ProjectionMatrix);
        _descriptor.Uniform(_useVertexColorLocation, 1);

        _vertexBuffer.Bind();
        _indexBuffer.Bind();

        GFX.DrawIndexed(_indexCount, 1, 0, 0, 0);
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