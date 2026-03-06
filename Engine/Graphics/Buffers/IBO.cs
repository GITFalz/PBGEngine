using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics;

public unsafe class IBO : BufferBase
{
    public Buffer Buffer;
    public DeviceMemory BufferMemory;
    public uint Size;
    public uint ElementCount;

    public IBO(uint[] data)
    {
        ElementCount = (uint)data.Length;
        if (data.Length == 0)
        {
            Size = (uint)Marshal.SizeOf<uint>();
            var dummy = new uint[1];
            GFX.CreateBuffer(BufferUsageFlags.IndexBufferBit, dummy, out Buffer, out BufferMemory);
            return;
        }
        Size = (uint)Marshal.SizeOf<uint>() * (uint)data.Length;
        GFX.CreateBuffer(BufferUsageFlags.IndexBufferBit, data, out Buffer, out BufferMemory);
    }

    public void Update(uint[] data)
    {
        if (ElementCount == 0)
            return;

        uint size = (uint)Marshal.SizeOf<uint>() * (uint)data.Length;
        if (size > Size)
        {
            Console.WriteLine($"[Warning] Updating IBO with more data than previously allocated - {size - Size} bytes will be lost!");
        }
        GFX.UpdateBuffer(data, Buffer);
    }

    public void Renew(uint[] data)
    {
        Destroy();
        ElementCount = (uint)data.Length;
        if (data.Length == 0)
        {
            Size = (uint)Marshal.SizeOf<uint>();
            var dummy = new uint[1];
            GFX.CreateBuffer(BufferUsageFlags.IndexBufferBit, dummy, out Buffer, out BufferMemory);
            return;
        }
        Size = (uint)Marshal.SizeOf<uint>() * (uint)data.Length;
        GFX.CreateBuffer(BufferUsageFlags.IndexBufferBit, data, out Buffer, out BufferMemory);
    }

    public void Bind()
    {
        GFX.Vk.CmdBindIndexBuffer(GFX.CommandBuffer, Buffer, 0, IndexType.Uint32);
    }

    protected override void Destroy()
    {
        GFX.DestroyBuffer(Buffer);
        GFX.FreeMemory(BufferMemory);
    }
}