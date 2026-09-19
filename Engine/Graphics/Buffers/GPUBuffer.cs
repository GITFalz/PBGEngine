using System.Drawing;
using System.Runtime.InteropServices;
using PBG.Graphics.Vulkan;
using PBG.MathLibrary;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics;

public unsafe class GPUBufferBase : BufferBase
{
    public Buffer Buffer;
    public DeviceMemory BufferMemory;
    public uint SizeInBytes = 0;
    public uint ElementCount = 0;

    protected List<(ulong Offset, ulong Size)>[] DirtyRegions = [.. Enumerable.Range(0, GFX.MAX_FRAMES_IN_FLIGHT).Select(_ => new List<(ulong, ulong)>())];
    protected Buffer[] StagingBuffers = new Buffer[GFX.MAX_FRAMES_IN_FLIGHT];
    protected DeviceMemory[] StagingMemories = new DeviceMemory[GFX.MAX_FRAMES_IN_FLIGHT];
    protected void*[] StagingMapped = new void*[GFX.MAX_FRAMES_IN_FLIGHT];


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
        else if (_settings.UseStaging)
        {
            for (int i = 0; i < GFX.MAX_FRAMES_IN_FLIGHT; i++)
            {
                GFX.UnmapMemory(StagingMemories[i]);
                StagingMapped[i] = null;

                GFX.DestroyBuffer(StagingBuffers[i]);
                GFX.FreeMemory(StagingMemories[i]);
            }
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
        else if (_settings.UseStaging)
        {
            HelperFunctions.MemCpyTo<T>(data, (byte*)StagingMapped[GFX.CurrentFrame] + offsetInBytes, sizeInBytes, sizeInBytes);
            MarkDirty(offsetInBytes, sizeInBytes);
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
        else if (_settings.UseStaging)
        {
            fixed (T* pData = data)
            {
                byte* src = (byte*)pData + offsetInBytes;
                HelperFunctions.MemCpyTo<T>(src, (byte*)StagingMapped[GFX.CurrentFrame] + offsetInBytes, sizeInBytes, sizeInBytes);
                MarkDirty(offsetInBytes, sizeInBytes);
            }
        }
        else
        {
            GFX.UpdateBufferRange(data, Buffer, offsetInBytes, sizeInBytes);
        }
    }

    public void MarkDirty(ulong offsetInBytes, ulong sizeInBytes)
    {
        DirtyRegions[GFX.CurrentFrame].Add((offsetInBytes, sizeInBytes));
    }

    public void MarkDirty(int frameIndex, ulong offsetInBytes, ulong sizeInBytes)
    {
        DirtyRegions[frameIndex].Add((offsetInBytes, sizeInBytes));
    }

    public void* GetMappedPointer(uint frameIndex) => StagingMapped[frameIndex];
    public void* GetCurrentMappedPointer() => StagingMapped[GFX.CurrentFrame];


    public void FlushStaging()
    {
        var regions = DirtyRegions[GFX.CurrentFrame];
        if (regions.Count == 0) return;

        regions.Sort((a, b) => a.Offset.CompareTo(b.Offset));

        var merged = new List<(ulong Offset, ulong Size)>(regions.Count);
        ulong curStart = regions[0].Offset;
        ulong curEnd   = regions[0].Offset + regions[0].Size;

        for (int i = 1; i < regions.Count; i++)
        {
            ulong start = regions[i].Offset;
            ulong end   = regions[i].Offset + regions[i].Size;

            if (start <= curEnd)
                curEnd = Math.Max(curEnd, end);
            else
            {
                merged.Add((curStart, curEnd - curStart));
                curStart = start;
                curEnd   = end;
            }
        }
        merged.Add((curStart, curEnd - curStart));

        GFX.RecordUpload(StagingBuffers[GFX.CurrentFrame], Buffer, merged);

        ulong totalStart = merged[0].Offset;
        ulong totalEnd   = merged[^1].Offset + merged[^1].Size;
        GFX.ShaderBarrier(Buffer, totalStart, totalEnd - totalStart);

        regions.Clear();
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
        else if (_settings.UseStaging)
        {
            for (int i = 0; i < GFX.MAX_FRAMES_IN_FLIGHT; i++)
            {
                // create the staging buffer
                GFX.CreateBuffer(data, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out StagingBuffers[i], out StagingMemories[i]);
                void* ptr;
                GFX.MapMemory(StagingMemories[i], 0, SizeInBytes, 0, &ptr);
                StagingMapped[i] = ptr;
            }

            // create the device local buffer
            GFX.CreateBuffer(data, BufferUsageFlags.TransferDstBit | _settings.UsageFlags, MemoryPropertyFlags.DeviceLocalBit, out Buffer, out BufferMemory);
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
            GFX.CreateBuffer(SizeInBytes, _settings.UsageFlags, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit | MemoryPropertyFlags.HostCachedBit, out Buffer, out BufferMemory);
            void* ptr;
            GFX.MapMemory(BufferMemory, 0, SizeInBytes, 0, &ptr);
            _mapped = ptr;
        }
        else if (_settings.UseStaging)
        {
            for (int i = 0; i < GFX.MAX_FRAMES_IN_FLIGHT; i++)
            {
                // create the staging buffer
                GFX.CreateBuffer(SizeInBytes, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out StagingBuffers[i], out StagingMemories[i]);
                void* ptr;
                GFX.MapMemory(StagingMemories[i], 0, SizeInBytes, 0, &ptr);
                StagingMapped[i] = ptr;
            }

            // create the device local buffer
            GFX.CreateBuffer(SizeInBytes, BufferUsageFlags.TransferDstBit | _settings.UsageFlags, MemoryPropertyFlags.DeviceLocalBit, out Buffer, out BufferMemory);
        }
        else
        {
            GFX.CreateBuffer(SizeInBytes, BufferUsageFlags.TransferDstBit | _settings.UsageFlags, MemoryPropertyFlags.DeviceLocalBit, out Buffer, out BufferMemory);
        }
    }

    public T[] ReadBack(int count)
    {
        if (!_settings.HostVisible)
            throw new InvalidOperationException("ReadBack (mapped) called on a non-host-visible buffer — use ReadBackStaging instead.");

        T[] result = new T[count];
        fixed (T* pResult = result)
        {
            HelperFunctions.MemCpyTo<T>((byte*)_mapped, (byte*)pResult, (ulong)(count * Marshal.SizeOf<T>()), (ulong)(count * Marshal.SizeOf<T>()));
        }
        return result;
    }

    public void Map(T[] result) => InternalMap(result, 0, 0, result.Length);
    public void Map(T[] result, int count) => InternalMap(result, 0, 0, count);
    public void Map(T[] result, int sourceStart, int destinationStart, int count) => InternalMap(result, sourceStart, destinationStart, count);

    private void InternalMap(T[] result, int sourceStart, int destinationStart, int count)
    {
        if (!_settings.HostVisible)
            throw new InvalidOperationException("ReadBack requires a host-visible buffer.");

        if ((uint)sourceStart > (uint)result.Length || (uint)destinationStart > (uint)result.Length || count < 0 || sourceStart + count > result.Length || destinationStart + count > result.Length)
            throw new ArgumentOutOfRangeException();

        int size = sizeof(T);
        ulong bytes = (ulong)(count * size);

        fixed (T* pResult = result)
        {
            byte* source = (byte*)_mapped + (sourceStart * size);
            byte* destination = (byte*)pResult + (destinationStart * size);

            HelperFunctions.MemCpyTo<T>(source, destination, bytes, bytes);
        }
    }


    public void Map(T* result, uint arrayLength) => InternalMap(result, arrayLength, 0, 0, (int)arrayLength);
    public void Map(T* result, uint arrayLength, int count) => InternalMap(result, arrayLength, 0, 0, count);
    public void Map(T* result, uint arrayLength, int sourceStart, int destinationStart, int count) => InternalMap(result, arrayLength, sourceStart, destinationStart, count);

    private void InternalMap(T* result, uint resultCount, int sourceStart, int destinationStart, int count)
    {
        if (!_settings.HostVisible)
            throw new InvalidOperationException("ReadBack requires a host-visible buffer.");

        if ((uint)sourceStart > resultCount || (uint)destinationStart > resultCount || count < 0 || sourceStart + count > resultCount || destinationStart + count > resultCount)
            throw new ArgumentOutOfRangeException();

        int size = sizeof(T);
        ulong bytes = (ulong)(count * size);

        byte* source = (byte*)_mapped + (sourceStart * size);
        byte* destination = (byte*)result + (destinationStart * size);

        HelperFunctions.MemCpyTo<T>(source, destination, bytes, bytes);
    }
}

public struct GPUBufferSettings
{
    public bool UseStaging = false;
    public bool HostVisible = false;
    public BufferUsageFlags UsageFlags = BufferUsageFlags.None;

    public GPUBufferSettings() {}

    public GPUBufferSettings Set(BufferUsageFlags usageFlags)
    {
        UsageFlags = usageFlags;
        return this;
    }
}