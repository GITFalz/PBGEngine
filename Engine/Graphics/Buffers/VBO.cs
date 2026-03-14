using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics;

public unsafe class VBOBase : BufferBase
{
    public Buffer Buffer;
    public DeviceMemory BufferMemory;
    public uint Size;
    public uint BindingPoint;
    public uint ElementCount;

    protected override void Destroy()
    {
        GFX.DestroyBuffer(Buffer);
        GFX.FreeMemory(BufferMemory);
    }

    public static void Bind(Buffer[] buffers, ulong[] offsets)
    {
        fixed (ulong* pOffset = offsets)
        fixed (Buffer* pBuffers = buffers)
        GFX.Vk.CmdBindVertexBuffers(GFX.CommandBuffer, 0, 1, pBuffers, pOffset);
    }

    public void Bind() => Bind(GFX.CommandBuffer);
    public void Bind(CommandBuffer commandBuffer)
    {
        ulong[] offsets = [0];
        fixed (ulong* pOffset = offsets)
        GFX.Vk.CmdBindVertexBuffers(commandBuffer, 0, 1, ref Buffer, pOffset);
    }
}

public unsafe class VBO<T> : VBOBase where T : unmanaged
{
    

    public VBO(T[] data)
    {
        ElementCount = (uint)data.Length;
        if (data.Length == 0)
        {
            Size = (uint)Marshal.SizeOf<T>();
            var dummy = new T[1];
            GFX.CreateBuffer(dummy, BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.DeviceLocalBit, out Buffer, out BufferMemory);
            return;
        }
        Size = (uint)Marshal.SizeOf<T>() * (uint)data.Length;
        GFX.CreateBuffer(data, BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.DeviceLocalBit, out Buffer, out BufferMemory);
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
            GFX.CreateBuffer(dummy, BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.DeviceLocalBit, out Buffer, out BufferMemory);
            return;
        }
        Size = (uint)Marshal.SizeOf<T>() * (uint)data.Length;
        GFX.CreateBuffer(data, BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.DeviceLocalBit, out Buffer, out BufferMemory);
    }
}