using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics;

public unsafe class SSBO<T> : BufferBase where T : unmanaged
{
    public Buffer Buffer;
    public DeviceMemory BufferMemory;
    public uint Size = 0;
    public uint ElementCount = 0;

    public SSBO(T[] data)
    {
        HelperFunctions.CheckAlignment<T>();
        ElementCount = (uint)data.Length;
        if (data.Length == 0)
        {
            Size = (uint)Marshal.SizeOf<T>();
            var dummy = new T[1];
            GFX.CreateBuffer(BufferUsageFlags.StorageBufferBit, dummy, out Buffer, out BufferMemory);
            return;
        }
        Size = (uint)Marshal.SizeOf<T>() * (uint)data.Length;
        GFX.CreateBuffer(BufferUsageFlags.StorageBufferBit, data, out Buffer, out BufferMemory);
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
        HelperFunctions.CheckAlignment<T>();
        ElementCount = (uint)data.Length;
        if (data.Length == 0)
        {
            Size = (uint)Marshal.SizeOf<T>();
            var dummy = new T[1];
            GFX.CreateBuffer(BufferUsageFlags.StorageBufferBit, dummy, out Buffer, out BufferMemory);
            return;
        }
        Size = (uint)Marshal.SizeOf<T>() * (uint)data.Length;
        GFX.CreateBuffer(BufferUsageFlags.StorageBufferBit, data, out Buffer, out BufferMemory);
    }

    protected override void Destroy()
    {
        GFX.DestroyBuffer(Buffer);
        GFX.FreeMemory(BufferMemory);
    }
}