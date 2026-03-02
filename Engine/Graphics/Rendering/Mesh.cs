using PBG.MathLibrary;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics;

public unsafe class Mesh : BufferBase
{
    private GraphicsContext _context;

    private List<Buffer> _buffers = [];
    private List<DeviceMemory> _deviceMemories = [];
    
    private Buffer _indexBuffer;
    private DeviceMemory _indexBufferMemory;
    private uint _indexCount = 0;

    private Buffer[] _vertexBuffers = [];
    private ulong[] _offsets = [];

    private CommandBuffer _commandBuffer;


    public Vector3[] Vertices = [];
    public Vector3[] Colors = [];
    public uint[] Indices = [];

    public Mesh()
    {
        _context = GraphicsContext.graphicsContext;
    }

    public void ClearMesh()
    {
        Vertices = [];
        Colors = [];
        Indices = [];

        _context.vk.DestroyBuffer(_context.device, _indexBuffer, null);
        _context.vk.FreeMemory(_context.device, _indexBufferMemory, null);

        for (int i = 0; i < _buffers.Count; i++)
        {
            _context.vk.DestroyBuffer(_context.device, _buffers[i], null);
            _context.vk.FreeMemory(_context.device, _deviceMemories[i], null);
        }

        _context.vk.FreeCommandBuffers(_context.device, _context.commandPool, 1, ref _commandBuffer);

        _deviceMemories.Clear();
        _buffers.Clear();
    }


    public void Generate(Shader shader)
    {
        _context.CreateBuffer(BufferUsageFlags.VertexBufferBit, Vertices, out Buffer vertexBuffer, out DeviceMemory vertexBufferMemory);
        _context.CreateBuffer(BufferUsageFlags.VertexBufferBit, Colors, out Buffer colorBuffer, out DeviceMemory colorBufferMemory);
        _context.CreateBuffer(BufferUsageFlags.IndexBufferBit, Indices, out _indexBuffer, out _indexBufferMemory);

        _buffers.Add(vertexBuffer);
        _buffers.Add(colorBuffer);

        _deviceMemories.Add(vertexBufferMemory);
        _deviceMemories.Add(colorBufferMemory);

        _vertexBuffers = [.._buffers];
        _offsets = new ulong[_buffers.Count];
        _indexCount = (uint)Indices.Length;

        //_commandBuffer = _context.CreateCommandBuffer();
        //_context.RecordMeshCommandBuffer(shader, _commandBuffer, _vertexBuffers, _indexBuffer, _offsets, _indexCount);

        Vertices = [];
        Colors = [];
        Indices = [];
    }
    
    public void Bind()
    {
        fixed (Buffer* pVert = _vertexBuffers)
        fixed (ulong* pOffset = _offsets)
        _context.vk.CmdBindVertexBuffers(_context.commandBuffer, 0, (uint)_vertexBuffers.Length, pVert, pOffset);

        _context.vk.CmdBindIndexBuffer(_context.commandBuffer, _indexBuffer, 0, IndexType.Uint32);
    }

    public void Draw()
    {   
        _context.vk.CmdDrawIndexed(_context.commandBuffer, _indexCount, 1, 0, 0, 0);
    }

    protected override void Destroy()
    {
        ClearMesh();
    }
}