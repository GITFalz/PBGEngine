using Silk.NET.Vulkan;

namespace PBG.Graphics.Vulkan;

public unsafe sealed partial class VulkanDevice
{   
    private void CreateCommandPool()
    {
        QueueFamilyIndices queueFamilyIndices = FindQueueFamilies(PhysicalDevice);

        {
            CommandPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.CommandPoolCreateInfo,
                Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
                QueueFamilyIndex = queueFamilyIndices.GraphicsFamily!.Value
            };

            if (Vk.CreateCommandPool(Device, &poolInfo, null, out CommandPool) != Result.Success) {
                throw new InvalidOperationException("failed to create command pool!");
            }
        }

        Console.WriteLine("compute queue: " + queueFamilyIndices.ComputeFamily);

        if (queueFamilyIndices.ComputeFamily != null)
        {
            CommandPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.CommandPoolCreateInfo,
                Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
                QueueFamilyIndex = queueFamilyIndices.ComputeFamily.Value
            };

            if (Vk.CreateCommandPool(Device, &poolInfo, null, out ComputeCommandPool) != Result.Success) {
                throw new InvalidOperationException("failed to create compute command pool!");
            }

            Console.WriteLine("success");
        }
        else
        {
            ComputeCommandPool = CommandPool;
            Console.WriteLine("fail");
        }
    }

    public CommandBuffer BeginSingleTimeCommands() 
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = CommandPool,
            CommandBufferCount = 1
        };

        CommandBuffer commandBuffer;
        Vk.AllocateCommandBuffers(Device, &allocInfo, &commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        Vk.BeginCommandBuffer(commandBuffer, &beginInfo);

        return commandBuffer;
    }

    public void EndSingleTimeCommands(CommandBuffer commandBuffer) 
    {
        Vk.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };

        Vk.QueueSubmit(GraphicsQueue, 1, &submitInfo, default);
        Vk.QueueWaitIdle(GraphicsQueue);

        Vk.FreeCommandBuffers(Device, CommandPool, 1, &commandBuffer);
    }

    public CommandBuffer BeginSingleTimeComputeCommands() 
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = ComputeCommandPool,
            CommandBufferCount = 1
        };

        CommandBuffer commandBuffer;
        Vk.AllocateCommandBuffers(Device, &allocInfo, &commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        Vk.BeginCommandBuffer(commandBuffer, &beginInfo);

        return commandBuffer;
    }

    public void EndSingleTimeComputeCommands(CommandBuffer commandBuffer) 
    {
        Vk.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };

        Vk.QueueSubmit(ComputeQueue, 1, &submitInfo, default);
        Vk.QueueWaitIdle(ComputeQueue);

        Vk.FreeCommandBuffers(Device, ComputeCommandPool, 1, &commandBuffer);
    }

    public uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties) 
    {
        PhysicalDeviceMemoryProperties memProperties;
        Vk.GetPhysicalDeviceMemoryProperties(PhysicalDevice, &memProperties);

        for (int i = 0; i < memProperties.MemoryTypeCount; i++) {
            if ((typeFilter & (1 << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties) {
                return (uint)i;
            }
        }

        throw new InvalidOperationException("failed to find suitable memory type!");
    }
}