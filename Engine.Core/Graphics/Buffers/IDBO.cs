using Silk.NET.Vulkan;

namespace PBG.Graphics;

public class IDBO<T> : GPUBuffer<T> where T : unmanaged
{
    public IDBO(T[] data, bool hostVisible = false) : base(data, new()
    {
        HostVisible = hostVisible,
        UsageFlags = BufferUsageFlags.IndirectBufferBit | BufferUsageFlags.StorageBufferBit
    }) {}

    public IDBO(uint count, bool hostVisible = false) : base(count, new()
    {
        HostVisible = hostVisible,
        UsageFlags = BufferUsageFlags.IndirectBufferBit | BufferUsageFlags.StorageBufferBit
    }) {}
}