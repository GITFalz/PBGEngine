using Silk.NET.Vulkan;

namespace PBG.Graphics.Vulkan;

public unsafe sealed class VulkanCommandBuffers : IDisposable
{
    private readonly VulkanDevice _vulkanDevice;
    public CommandPool CommandPool { get; private set; }
    public CommandBuffer[] CommandBuffers { get; private set; } = new CommandBuffer[GFX.MAX_FRAMES_IN_FLIGHT];

    public VulkanCommandBuffers(VulkanDevice vulkanDevice)
    {
        _vulkanDevice = vulkanDevice;

        CreateCommandPool();
        CreateCommandBuffer();
    }
    
    private void CreateCommandPool()
    {
        QueueFamilyIndices queueFamilyIndices = _vulkanDevice.FindQueueFamilies();

        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
            QueueFamilyIndex = queueFamilyIndices.GraphicsFamily!.Value
        };

        if (_vulkanDevice.Vk.CreateCommandPool(_vulkanDevice.Device, &poolInfo, null, out var commandPool) != Result.Success) {
            throw new InvalidOperationException("failed to create command pool!");
        }

        CommandPool = commandPool;
    }

    private void CreateCommandBuffer() 
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = CommandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1
        };

        for (int i = 0; i < GFX.MAX_FRAMES_IN_FLIGHT; i++)
        if (_vulkanDevice.Vk.AllocateCommandBuffers(_vulkanDevice.Device, &allocInfo, out CommandBuffers[i]) != Result.Success) {
            throw new InvalidOperationException("failed to allocate command buffers!");
        }
    }

    public void Dispose()
    {
        _vulkanDevice.Vk.FreeCommandBuffers(_vulkanDevice.Device, CommandPool, (uint)CommandBuffers.Length, CommandBuffers);
        _vulkanDevice.Vk.DestroyCommandPool(_vulkanDevice.Device, CommandPool, null);
    }
}