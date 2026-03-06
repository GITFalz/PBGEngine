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
    private void* _mapped;
    private bool _hostVisible;

    public SSBO(T[] data, bool hostVisible = false)
    {
        HelperFunctions.CheckAlignment<T>();
        uint allocCount = (uint)data.Length == 0 ? 1 : (uint)data.Length;
        ElementCount    = (uint)data.Length;
        Size            = (uint)Marshal.SizeOf<T>() * allocCount;
        _hostVisible    = hostVisible;

        if (hostVisible)
        {
            GFX.CreateBuffer(data.Length == 0 ? new T[1] : data, BufferUsageFlags.StorageBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer, out BufferMemory);
            void* ptr;
            GFX.MapMemory(BufferMemory, 0, Size, 0, &ptr);
            _mapped = ptr;
        }
        else
        {
            GFX.CreateBuffer(data.Length == 0 ? new T[1] : data, BufferUsageFlags.TransferDstBit | BufferUsageFlags.StorageBufferBit, MemoryPropertyFlags.DeviceLocalBit, out Buffer, out BufferMemory);
        }
    }

    public SSBO(uint count, bool hostVisible = false)
    {
        HelperFunctions.CheckAlignment<T>();
        uint allocCount = count == 0 ? 1 : count;
        ElementCount    = count;
        Size            = (uint)Marshal.SizeOf<T>() * allocCount;
        _hostVisible    = hostVisible;

        if (hostVisible)
        {
            GFX.CreateBuffer(Size, BufferUsageFlags.StorageBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer, out BufferMemory);
            void* ptr;
            GFX.MapMemory(BufferMemory, 0, Size, 0, &ptr);
            _mapped = ptr;
        }
        else
        {
            GFX.CreateBuffer(Size, BufferUsageFlags.TransferDstBit | BufferUsageFlags.StorageBufferBit, MemoryPropertyFlags.DeviceLocalBit, out Buffer, out BufferMemory);
        }
    }

    public void Update(T[] data)
    {
        if (ElementCount == 0)
            return;

        uint size = (uint)Marshal.SizeOf<T>() * (uint)data.Length;
        if (size > Size)
        {
            Console.WriteLine($"[Warning] Updating SSBO with more data than previously allocated - {size - Size} bytes will be lost!");
        }
        GFX.UpdateBuffer(data, Buffer);
    }

    public void Update(T[] data, ulong offsetInBytes, ulong sizeInBytes, bool fullArray = false)
    {
        if (ElementCount == 0) return;

        if (sizeInBytes == 0)
            sizeInBytes = (ulong)(Marshal.SizeOf<T>() * data.Length);

        if (!fullArray)
            GFX.UpdateBuffer(data, Buffer, offsetInBytes, sizeInBytes);
        else
            GFX.UpdateBufferRange(data, Buffer, offsetInBytes, sizeInBytes);
    }

    public void Renew(T[] data, bool hostVisible = false)
    {
        Destroy();
        HelperFunctions.CheckAlignment<T>();
        uint allocCount = (uint)data.Length == 0 ? 1 : (uint)data.Length;
        ElementCount    = (uint)data.Length;
        Size            = (uint)Marshal.SizeOf<T>() * allocCount;
        _hostVisible    = hostVisible;

        if (hostVisible)
        {
            GFX.CreateBuffer(data.Length == 0 ? new T[1] : data, BufferUsageFlags.StorageBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer, out BufferMemory);
            void* ptr;
            GFX.MapMemory(BufferMemory, 0, Size, 0, &ptr);
            _mapped = ptr;
        }
        else
        {
            GFX.CreateBuffer(data.Length == 0 ? new T[1] : data, BufferUsageFlags.TransferDstBit | BufferUsageFlags.StorageBufferBit, MemoryPropertyFlags.DeviceLocalBit, out Buffer, out BufferMemory);
        }
    }

    public BufferMemoryBarrier GetMemoryBarrier()
    {
        return new BufferMemoryBarrier
        {
            SType         = StructureType.BufferMemoryBarrier,
            SrcAccessMask = AccessFlags.ShaderWriteBit,
            DstAccessMask = AccessFlags.ShaderReadBit,
            Buffer        = Buffer,
            Size          = Vk.WholeSize
        };
    }

    protected override void Destroy()
    {
        GFX.DestroyBuffer(Buffer);
        GFX.FreeMemory(BufferMemory);
    }
}