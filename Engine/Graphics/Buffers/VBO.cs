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

    public VBO(T[] data)
    {
        Size = (uint)Marshal.SizeOf<T>() * (uint)data.Length;
        GFX.CreateBuffer(BufferUsageFlags.VertexBufferBit, data, out Buffer, out BufferMemory);
    }

    public void Update(T[] data)
    {
        uint size = (uint)Marshal.SizeOf<T>() * (uint)data.Length;
        if (size > Size)
        {
            Console.WriteLine($"[Warning] Updating VBO with more data than previously allocated - {size - Size} bytes will be lost!");
        }
        GFX.UpdateBuffer(data, Buffer);
    }

    public void Bind() {}
    public void Unbind() {}

    protected override void Destroy()
    {
        GFX.DestroyBuffer(Buffer);
        GFX.FreeMemory(BufferMemory);
    }
}