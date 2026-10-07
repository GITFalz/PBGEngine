using Silk.NET.Vulkan;

namespace PBG.Graphics;

public class SSBO<T> : GPUBuffer<T> where T : unmanaged
{
    public SSBO(T[] data, bool hostVisible = false, bool useStaging = false) : base(data, new()
    {
        HostVisible = hostVisible,
        UseStaging = useStaging,
        UsageFlags = BufferUsageFlags.StorageBufferBit
    }) {}

    public SSBO(uint count, bool hostVisible = false, bool useStaging = false) : base(count, new()
    {
        HostVisible = hostVisible,
        UseStaging = useStaging,
        UsageFlags = BufferUsageFlags.StorageBufferBit
    }) {}
}