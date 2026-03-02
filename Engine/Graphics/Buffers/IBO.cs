using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics;

public unsafe class IBO : BufferBase
{
    public Buffer Buffer;
    public DeviceMemory BufferMemory;
    public uint Size;

    public IBO(uint[] data)
    {
        Size = (uint)Marshal.SizeOf<uint>() * (uint)data.Length;
        GFX.CreateBuffer(BufferUsageFlags.IndexBufferBit, data, out Buffer, out BufferMemory);
    }

    public void Update(uint[] data)
    {
        uint size = (uint)Marshal.SizeOf<uint>() * (uint)data.Length;
        if (size > Size)
        {
            Console.WriteLine($"[Warning] Updating VBO with more data than previously allocated - {size - Size} bytes will be lost!");
        }
        GFX.UpdateBuffer(data, Buffer);
    }

    public void Bind()
    {
        if (!GraphicsContext.graphicsContext.blockBinding)
            GFX.Vk.CmdBindIndexBuffer(GFX.CommandBuffer, Buffer, 0, IndexType.Uint32);
    }

    protected override void Destroy()
    {
        GFX.DestroyBuffer(Buffer);
        GFX.FreeMemory(BufferMemory);
    }
}