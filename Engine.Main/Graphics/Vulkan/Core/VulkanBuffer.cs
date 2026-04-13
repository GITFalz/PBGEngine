using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics.Vulkan;

public unsafe sealed class VulkanBuffer
{
    private readonly VulkanDevice _vulkanDevice;

    public VulkanBuffer(VulkanDevice vulkanDevice)
    {
        _vulkanDevice = vulkanDevice;
    }

    public void CreateBuffer<T>(T[] array, BufferUsageFlags bufferType, MemoryPropertyFlags properties, out Buffer buffer, out DeviceMemory bufferMemory) where T : unmanaged
    {
        var bufferSize = (ulong)(Marshal.SizeOf<T>() * array.Length);

        if (properties.HasFlag(MemoryPropertyFlags.HostVisibleBit))
        {
            CreateBuffer(bufferSize, bufferType, properties, out buffer, out bufferMemory);
            void* data;
            _vulkanDevice.Vk.MapMemory(_vulkanDevice.Device, bufferMemory, 0, bufferSize, 0, &data);
            HelperFunctions.MemCpyTo(array, data, bufferSize, bufferSize);
            _vulkanDevice.Vk.UnmapMemory(_vulkanDevice.Device, bufferMemory);
        }
        else
        {
            CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer stagingBuffer, out DeviceMemory stagingBufferMemory);
            void* data;
            _vulkanDevice.Vk.MapMemory(_vulkanDevice.Device, stagingBufferMemory, 0, bufferSize, 0, &data);
            HelperFunctions.MemCpyTo(array, data, bufferSize, bufferSize);
            _vulkanDevice.Vk.UnmapMemory(_vulkanDevice.Device, stagingBufferMemory);
            CreateBuffer(bufferSize, BufferUsageFlags.TransferDstBit | bufferType, properties, out buffer, out bufferMemory);
            CopyBuffer(stagingBuffer, buffer, bufferSize);
            _vulkanDevice.Vk.DestroyBuffer(_vulkanDevice.Device, stagingBuffer, null);
            _vulkanDevice.Vk.FreeMemory(_vulkanDevice.Device, stagingBufferMemory, null);
        }
    }

    public void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, out Buffer buffer, out DeviceMemory bufferMemory) 
    {
        BufferCreateInfo bufferInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };

        if (_vulkanDevice.Vk.CreateBuffer(_vulkanDevice.Device, &bufferInfo, null, out buffer) != Result.Success) {
            throw new InvalidOperationException("failed to create buffer!");
        }

        MemoryRequirements memRequirements;
        _vulkanDevice.Vk.GetBufferMemoryRequirements(_vulkanDevice.Device, buffer, &memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = _vulkanDevice.FindMemoryType(memRequirements.MemoryTypeBits, properties)
        };

        if (_vulkanDevice.Vk.AllocateMemory(_vulkanDevice.Device, &allocInfo, null, out bufferMemory) != Result.Success) {
            _vulkanDevice.Vk.DestroyBuffer(_vulkanDevice.Device, buffer, null);
            buffer = default;
            throw new InvalidOperationException("failed to allocate buffer memory!");
        }

        _vulkanDevice.Vk.BindBufferMemory(_vulkanDevice.Device, buffer, bufferMemory, 0);
    }

    public void UpdateBuffer<T>(T[] array, Buffer buffer) where T : unmanaged
    {
        var bufferSize = (ulong)(Marshal.SizeOf<T>() * array.Length);

        CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer stagingBuffer, out DeviceMemory stagingBufferMemory);

        void* data; 
        _vulkanDevice.Vk.MapMemory(_vulkanDevice.Device, stagingBufferMemory, 0, bufferSize, 0, &data);
        HelperFunctions.MemCpyTo(array, data, bufferSize, bufferSize);
        _vulkanDevice.Vk.UnmapMemory(_vulkanDevice.Device, stagingBufferMemory);
    
        CopyBuffer(stagingBuffer, buffer, bufferSize);

        _vulkanDevice.Vk.DestroyBuffer(_vulkanDevice.Device, stagingBuffer, null);
        _vulkanDevice.Vk.FreeMemory(_vulkanDevice.Device, stagingBufferMemory, null);
    }

    public void UpdateBuffer<T>(T[] array, Buffer buffer, ulong offsetBytes = 0, ulong sizeBytes = 0) where T : unmanaged
    {
        ulong elementSize = (ulong)Marshal.SizeOf<T>();
        ulong totalSize   = elementSize * (ulong)array.Length;
        ulong copySize    = sizeBytes == 0 ? totalSize : sizeBytes;

        CreateBuffer(copySize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer stagingBuffer, out DeviceMemory stagingBufferMemory);

        void* data;
        _vulkanDevice.Vk.MapMemory(_vulkanDevice.Device, stagingBufferMemory, 0, copySize, 0, &data);
        HelperFunctions.MemCpyTo(array, data, copySize, copySize);
        _vulkanDevice.Vk.UnmapMemory(_vulkanDevice.Device, stagingBufferMemory);

        CopyBuffer(stagingBuffer, buffer, copySize, 0, offsetBytes);

        _vulkanDevice.Vk.DestroyBuffer(_vulkanDevice.Device, stagingBuffer, null);
        _vulkanDevice.Vk.FreeMemory(_vulkanDevice.Device, stagingBufferMemory, null);
    }

    public void UpdateBufferRange<T>(T[] array, Buffer buffer, ulong offsetBytes = 0, ulong sizeBytes = 0) where T : unmanaged
    {
        ulong elementSize = (ulong)Marshal.SizeOf<T>();
        ulong totalSize   = elementSize * (ulong)array.Length;
        ulong copySize    = sizeBytes == 0 ? totalSize : sizeBytes;

        CreateBuffer(copySize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer stagingBuffer, out DeviceMemory stagingBufferMemory);

        void* data;
        _vulkanDevice.Vk.MapMemory(_vulkanDevice.Device, stagingBufferMemory, 0, copySize, 0, &data);

        fixed (T* pArray = array)
        {
            byte* src = (byte*)pArray + offsetBytes;
            HelperFunctions.MemCpyTo<T>(src, data, copySize, copySize);
        }

        _vulkanDevice.Vk.UnmapMemory(_vulkanDevice.Device, stagingBufferMemory);
        CopyBuffer(stagingBuffer, buffer, copySize, 0, offsetBytes);

        _vulkanDevice.Vk.DestroyBuffer(_vulkanDevice.Device, stagingBuffer, null);
        _vulkanDevice.Vk.FreeMemory(_vulkanDevice.Device, stagingBufferMemory, null);
    }

    public void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size, ulong srcOffset = 0, ulong dstOffset = 0) 
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = _vulkanDevice.CommandPool,
            CommandBufferCount = 1
        };

        CommandBuffer commandBuffer;
        _vulkanDevice.Vk.AllocateCommandBuffers(_vulkanDevice.Device, &allocInfo, &commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        _vulkanDevice.Vk.BeginCommandBuffer(commandBuffer, &beginInfo);

        BufferCopy copyRegion = new()
        {
            SrcOffset = srcOffset, // Optional
            DstOffset = dstOffset, // Optional
            Size = size
        };

        _vulkanDevice.Vk.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, &copyRegion);

        _vulkanDevice.Vk.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };

        _vulkanDevice.Vk.QueueSubmit(_vulkanDevice.GraphicsQueue, 1, &submitInfo, default);
        _vulkanDevice.Vk.QueueWaitIdle(_vulkanDevice.GraphicsQueue);

        _vulkanDevice.Vk.FreeCommandBuffers(_vulkanDevice.Device, _vulkanDevice.CommandPool, 1, &commandBuffer);
    }
}