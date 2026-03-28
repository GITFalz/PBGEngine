using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics;

public unsafe class GPUBufferBase : BufferBase
{
    public Buffer Buffer;
    public DeviceMemory BufferMemory;
    public uint SizeInBytes = 0;
    public uint ElementCount = 0;

    protected GPUBufferSettings _settings;
    protected void* _mapped;

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
        OnDispose?.Invoke(this);
        OnDispose = null;

        GFX.Vk.DeviceWaitIdle(GFX.Device);
        if (_settings.HostVisible && _mapped != null)
        {
            GFX.UnmapMemory(BufferMemory);
            _mapped = null;
        }

        GFX.DestroyBuffer(Buffer);
        GFX.FreeMemory(BufferMemory);
    }
}
public unsafe class GPUBuffer<T> : GPUBufferBase where T : unmanaged
{
    public GPUBuffer(T[] data, GPUBufferSettings settings)
    {
        if (settings.UsageFlags == BufferUsageFlags.None)
            throw new InvalidDataException("[Error] : Generating buffer of unknown type");

        _settings = settings;
        Create(data);
    }

    public GPUBuffer(uint count) : this(count, new()) {}
    public GPUBuffer(uint count, GPUBufferSettings settings)
    {
        if (settings.UsageFlags == BufferUsageFlags.None)
            throw new InvalidDataException("[Error] : Generating buffer of unknown type");

        _settings = settings;
        Create(count);
    }

    public void Update(T[] data) => Update(data, 0, (uint)Marshal.SizeOf<T>() * (uint)data.Length);
    public void Update(T[] data, ulong offsetInBytes, ulong sizeInBytes)
    {
        if (ElementCount == 0 || data.Length == 0) return;
        if (sizeInBytes == 0) sizeInBytes = (ulong)(Marshal.SizeOf<T>() * data.Length);
        if ((offsetInBytes + sizeInBytes) > SizeInBytes)
        {
            Console.WriteLine($"[Warning] Updating SSBO with more data than previously allocated - {(offsetInBytes + sizeInBytes) - SizeInBytes} bytes will be lost!");
            sizeInBytes = SizeInBytes - offsetInBytes;
        }

        if (_settings.HostVisible)
        {
            HelperFunctions.MemCpyTo<T>(data, (byte*)_mapped + offsetInBytes, sizeInBytes, sizeInBytes);
        }
        else
        {
            GFX.UpdateBuffer(data, Buffer, offsetInBytes, sizeInBytes);
        }
    }

    public void UpdateSlice(T[] data, ulong offsetInBytes, ulong sizeInBytes)
    {
        if (ElementCount == 0 || data.Length == 0) return;
        if (sizeInBytes == 0) sizeInBytes = (ulong)(Marshal.SizeOf<T>() * data.Length);

        ulong dataSize = (ulong)(Marshal.SizeOf<T>() * data.Length);
        if (offsetInBytes + sizeInBytes > dataSize)
        {
            Console.WriteLine($"[Warning] UpdateRange source out of bounds, clamping!");
            sizeInBytes = dataSize - offsetInBytes;
        }

        if (offsetInBytes + sizeInBytes > SizeInBytes)
        {
            Console.WriteLine($"[Warning] UpdateRange destination out of bounds, clamping!");
            sizeInBytes = SizeInBytes - offsetInBytes;
        }

        if (_settings.HostVisible)
        {
            fixed (T* pData = data)
            {
                byte* src = (byte*)pData + offsetInBytes;
                HelperFunctions.MemCpyTo<T>(src, (byte*)_mapped + offsetInBytes, sizeInBytes, sizeInBytes);
            }
        }
        else
            GFX.UpdateBufferRange(data, Buffer, offsetInBytes, sizeInBytes);
    }

    public void Renew(T[] data, bool hostVisible)
    {
        Destroy();
        _settings.HostVisible = hostVisible;
        Create(data);
    }

    public void Renew(uint count, bool hostVisible)
    {
        Destroy();
        _settings.HostVisible = hostVisible;
        Create(count);
    }

    public void Renew(T[] data)
    {
        Destroy();
        Create(data);
    }

    public void Renew(uint count)
    {
        Destroy();
        Create(count);
    }

    private void Create(T[] data)
    {
        HelperFunctions.CheckAlignment<T>();
        uint allocCount = data.Length == 0 ? 1 : (uint)data.Length;
        if (data.Length == 0) data = new T[1];
        ElementCount = (uint)data.Length;
        SizeInBytes = (uint)Marshal.SizeOf<T>() * allocCount;

        if (_settings.HostVisible)
        {
            GFX.CreateBuffer(data, _settings.UsageFlags, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer, out BufferMemory);
            void* ptr;
            GFX.MapMemory(BufferMemory, 0, SizeInBytes, 0, &ptr);
            _mapped = ptr;
        }
        else
        {
            GFX.CreateBuffer(data, BufferUsageFlags.TransferDstBit | _settings.UsageFlags, MemoryPropertyFlags.DeviceLocalBit, out Buffer, out BufferMemory);
        }
    }

    private void Create(uint count)
    {
        HelperFunctions.CheckAlignment<T>();
        uint allocCount = count == 0 ? 1 : count;
        ElementCount = count;
        SizeInBytes = (uint)Marshal.SizeOf<T>() * allocCount;

        if (_settings.HostVisible)
        {
            GFX.CreateBuffer(SizeInBytes, _settings.UsageFlags, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer, out BufferMemory);
            void* ptr;
            GFX.MapMemory(BufferMemory, 0, SizeInBytes, 0, &ptr);
            _mapped = ptr;
        }
        else
        {
            GFX.CreateBuffer(SizeInBytes, BufferUsageFlags.TransferDstBit | _settings.UsageFlags, MemoryPropertyFlags.DeviceLocalBit, out Buffer, out BufferMemory);
        }
    }
}

public struct GPUBufferSettings
{
    public bool HostVisible = false;
    public BufferUsageFlags UsageFlags = BufferUsageFlags.None;

    public GPUBufferSettings() {}
}