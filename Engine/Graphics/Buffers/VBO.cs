using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics;

public unsafe class VBO<T> : BufferBase where T : unmanaged
{
    public Buffer Buffer;
    public DeviceMemory BufferMemory;
    public uint Size;
    public uint BindingPoint;
    public uint ElementCount;

    public VBO(T[] data)
    {
        ElementCount = (uint)data.Length;
        if (data.Length == 0)
        {
            Size = (uint)Marshal.SizeOf<T>();
            var dummy = new T[1];
            GFX.CreateBuffer(BufferUsageFlags.VertexBufferBit, dummy, out Buffer, out BufferMemory);
            return;
        }
        Size = (uint)Marshal.SizeOf<T>() * (uint)data.Length;
        GFX.CreateBuffer(BufferUsageFlags.VertexBufferBit, data, out Buffer, out BufferMemory);
    }

    public void Update(T[] data)
    {
        if (ElementCount == 0)
            return;

        uint size = (uint)Marshal.SizeOf<T>() * (uint)data.Length;
        if (size > Size)
        {
            Console.WriteLine($"[Warning] Updating VBO with more data than previously allocated - {size - Size} bytes will be lost!");
        }
        GFX.UpdateBuffer(data, Buffer);
    }

    public void Renew(T[] data)
    {
        Destroy();
        ElementCount = (uint)data.Length;
        if (data.Length == 0)
        {
            Size = (uint)Marshal.SizeOf<T>();
            var dummy = new T[1];
            GFX.CreateBuffer(BufferUsageFlags.VertexBufferBit, dummy, out Buffer, out BufferMemory);
            return;
        }
        Size = (uint)Marshal.SizeOf<T>() * (uint)data.Length;
        GFX.CreateBuffer(BufferUsageFlags.VertexBufferBit, data, out Buffer, out BufferMemory);
    }

    public void Bind()
    {
        ulong[] offsets = [0];
        fixed (ulong* pOffset = offsets)
        GFX.Vk.CmdBindVertexBuffers(GFX.CommandBuffer, 0, 1, ref Buffer, pOffset);
    }

    protected override void Destroy()
    {
        GFX.DestroyBuffer(Buffer);
        GFX.FreeMemory(BufferMemory);
    }
}