using Silk.NET.Vulkan;

namespace PBG.Graphics;

public class SSBO<T> : GPUBuffer<T> where T : unmanaged
{
    public SSBO(T[] data, bool hostVisible = false) : base(data, new()
    {
        HostVisible = hostVisible,
        UsageFlags = BufferUsageFlags.StorageBufferBit
    }) {}

    public SSBO(uint count, bool hostVisible = false) : base(count, new()
    {
        HostVisible = hostVisible,
        UsageFlags = BufferUsageFlags.StorageBufferBit
    }) {}
}