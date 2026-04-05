using Silk.NET.Vulkan;

namespace PBG.Graphics;

public unsafe class MappedBuffer<T> : BufferBase where T : unmanaged
{
    private Silk.NET.Vulkan.Buffer[] _uniformBuffers = new Silk.NET.Vulkan.Buffer[GraphicsContext.MAX_FRAMES_IN_FLIGHT];
    private DeviceMemory[] _uniformBuffersMemory = new DeviceMemory[GraphicsContext.MAX_FRAMES_IN_FLIGHT];
    private void*[] _uniformBuffersMapped = new void*[GraphicsContext.MAX_FRAMES_IN_FLIGHT];

    public ulong Size = 0;

    public MappedBuffer(BufferUsageFlags bufferUsageFlags)
    {
        Size = (ulong)System.Runtime.InteropServices.Marshal.SizeOf<T>();
        for (int i = 0; i < GraphicsContext.MAX_FRAMES_IN_FLIGHT; i++) 
        {
            GFX.CreateBuffer(Size, bufferUsageFlags, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out _uniformBuffers[i], out _uniformBuffersMemory[i]);
            GFX.MapMemory(_uniformBuffersMemory[i], 0, Size, 0, ref _uniformBuffersMapped[i]);
        }
    }

    public void Update(T data)
    {
        var bufferPtr = (byte*)_uniformBuffersMapped[GraphicsContext.MAX_FRAMES_IN_FLIGHT + GFX.CurrentFrame];
        var dest = bufferPtr;
        *(T*)dest = data;
    }

    protected override void Destroy()
    {
        for (int i = 0; i < _uniformBuffers.Length; i++) 
        {
            GFX.DestroyBuffer(_uniformBuffers[i]);
            GFX.FreeMemory(_uniformBuffersMemory[i]);
        }
    }
}