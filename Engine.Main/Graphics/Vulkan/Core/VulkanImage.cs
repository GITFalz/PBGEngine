using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics.Vulkan;

public unsafe sealed class VulkanImage
{
    private readonly VulkanDevice _vulkanDevice;

    public VulkanImage(VulkanDevice vulkanDevice)
    {
        _vulkanDevice = vulkanDevice;
    }

    public void CreateImage(uint width, uint height, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, out Image image, out DeviceMemory imageMemory)
    {
        ImageCreateInfo imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D
        };
        imageInfo.Extent.Width = width;
        imageInfo.Extent.Height = height;
        imageInfo.Extent.Depth = 1;
        imageInfo.MipLevels = 1;
        imageInfo.ArrayLayers = 1;
        imageInfo.Format = format;
        imageInfo.Tiling = tiling;
        imageInfo.InitialLayout = ImageLayout.Undefined;
        imageInfo.Usage = usage;
        imageInfo.SharingMode = SharingMode.Exclusive;
        imageInfo.Samples = SampleCountFlags.Count1Bit;
        imageInfo.Flags = 0; // Optional

        if (_vulkanDevice.Vk.CreateImage(_vulkanDevice.Device, &imageInfo, null, out image) != Result.Success) {
            throw new InvalidOperationException("failed to create image!");
        }

        MemoryRequirements memRequirements;
        _vulkanDevice.Vk.GetImageMemoryRequirements(_vulkanDevice.Device, image, &memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = _vulkanDevice.FindMemoryType(memRequirements.MemoryTypeBits, properties)
        };

        if (_vulkanDevice.Vk.AllocateMemory(_vulkanDevice.Device, &allocInfo, null, out imageMemory) != Result.Success) {
            throw new InvalidOperationException("failed to allocate image memory!");
        }

        _vulkanDevice.Vk.BindImageMemory(_vulkanDevice.Device, image, imageMemory, 0);
    }

    public void CreateImageArray(uint width, uint height, uint layers, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, out Image image, out DeviceMemory imageMemory)
    {
        ImageCreateInfo imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D
        };
        imageInfo.Extent.Width = width;
        imageInfo.Extent.Height = height;
        imageInfo.Extent.Depth = 1;
        imageInfo.MipLevels = 1;
        imageInfo.ArrayLayers = layers;
        imageInfo.Format = format;
        imageInfo.Tiling = tiling;
        imageInfo.InitialLayout = ImageLayout.Undefined;
        imageInfo.Usage = usage;
        imageInfo.SharingMode = SharingMode.Exclusive;
        imageInfo.Samples = SampleCountFlags.Count1Bit;
        imageInfo.Flags = 0; // Optional

        if (_vulkanDevice.Vk.CreateImage(_vulkanDevice.Device, &imageInfo, null, out image) != Result.Success) {
            throw new InvalidOperationException("failed to create image!");
        }

        MemoryRequirements memRequirements;
        _vulkanDevice.Vk.GetImageMemoryRequirements(_vulkanDevice.Device, image, &memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = _vulkanDevice.FindMemoryType(memRequirements.MemoryTypeBits, properties)
        };

        if (_vulkanDevice.Vk.AllocateMemory(_vulkanDevice.Device, &allocInfo, null, out imageMemory) != Result.Success) {
            throw new InvalidOperationException("failed to allocate image memory!");
        }

        _vulkanDevice.Vk.BindImageMemory(_vulkanDevice.Device, image, imageMemory, 0);
    }

    public ImageView CreateImageView(Image image, Format format, ImageAspectFlags aspectFlags, uint layers) 
    {
        ImageViewCreateInfo viewInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = image,
            ViewType = layers == 1 ? ImageViewType.Type2D : ImageViewType.Type2DArray,
            Format = format
        };
        viewInfo.SubresourceRange.AspectMask = aspectFlags;
        viewInfo.SubresourceRange.BaseMipLevel = 0;
        viewInfo.SubresourceRange.LevelCount = 1;
        viewInfo.SubresourceRange.BaseArrayLayer = 0;
        viewInfo.SubresourceRange.LayerCount = layers;

        if (_vulkanDevice.Vk.CreateImageView(_vulkanDevice.Device, &viewInfo, null, out ImageView imageView) != Result.Success)
        {
            throw new InvalidOperationException("failed to create texture image view!");
        }

        return imageView;
    }

    public void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout) 
    {
        CommandBuffer commandBuffer = _vulkanDevice.BeginSingleTimeCommands();

        ImageMemoryBarrier barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image
        };
        barrier.SubresourceRange.BaseMipLevel = 0;
        barrier.SubresourceRange.LevelCount = 1;
        barrier.SubresourceRange.BaseArrayLayer = 0;
        barrier.SubresourceRange.LayerCount = 1;

        if (newLayout == ImageLayout.DepthStencilAttachmentOptimal) {
            barrier.SubresourceRange.AspectMask = ImageAspectFlags.DepthBit;

            if (HasStencilComponent(format)) {
                barrier.SubresourceRange.AspectMask |= ImageAspectFlags.StencilBit;
            }
        } else {
            barrier.SubresourceRange.AspectMask = ImageAspectFlags.ColorBit;
        }

        PipelineStageFlags sourceStage;
        PipelineStageFlags destinationStage;

        if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.TransferWriteBit;

            sourceStage = PipelineStageFlags.TopOfPipeBit;
            destinationStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;

            sourceStage = PipelineStageFlags.TransferBit;
            destinationStage = PipelineStageFlags.FragmentShaderBit;
        }
        else if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.DepthStencilAttachmentOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask =
                AccessFlags.DepthStencilAttachmentReadBit |
                AccessFlags.DepthStencilAttachmentWriteBit;

            sourceStage = PipelineStageFlags.TopOfPipeBit;
            destinationStage = PipelineStageFlags.EarlyFragmentTestsBit;
        }
        else if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.General)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit | AccessFlags.ShaderWriteBit;
            sourceStage      = PipelineStageFlags.TopOfPipeBit;
            destinationStage = PipelineStageFlags.ComputeShaderBit;
        }
        else if (oldLayout == ImageLayout.General && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.ShaderWriteBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;
            sourceStage      = PipelineStageFlags.ComputeShaderBit;
            destinationStage = PipelineStageFlags.FragmentShaderBit;
        }
        else if (oldLayout == ImageLayout.ShaderReadOnlyOptimal && newLayout == ImageLayout.General)
        {
            barrier.SrcAccessMask = AccessFlags.ShaderReadBit;
            barrier.DstAccessMask = AccessFlags.ShaderWriteBit;
            sourceStage      = PipelineStageFlags.FragmentShaderBit;
            destinationStage = PipelineStageFlags.ComputeShaderBit;
        }
        else if (oldLayout == ImageLayout.General && newLayout == ImageLayout.TransferSrcOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.ShaderWriteBit;
            barrier.DstAccessMask = AccessFlags.TransferReadBit;
            sourceStage      = PipelineStageFlags.ComputeShaderBit;
            destinationStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferSrcOptimal && newLayout == ImageLayout.General)
        {
            barrier.SrcAccessMask = AccessFlags.TransferReadBit;
            barrier.DstAccessMask = AccessFlags.ShaderWriteBit;
            sourceStage      = PipelineStageFlags.TransferBit;
            destinationStage = PipelineStageFlags.ComputeShaderBit;
        }
        else
        {
            throw new ArgumentException("Unsupported layout transition!");
        }

     
        _vulkanDevice.Vk.CmdPipelineBarrier(
            commandBuffer,
            sourceStage, destinationStage,
            0,
            0, null,
            0, null,
            1, &barrier
        );

        _vulkanDevice.EndSingleTimeCommands(commandBuffer);
    }

    public void TransitionImageArrayLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout, uint layers) 
    {
        CommandBuffer commandBuffer = _vulkanDevice.BeginSingleTimeCommands();

        ImageMemoryBarrier barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image
        };
        barrier.SubresourceRange.BaseMipLevel = 0;
        barrier.SubresourceRange.LevelCount = 1;
        barrier.SubresourceRange.BaseArrayLayer = 0;
        barrier.SubresourceRange.LayerCount = layers;

        if (newLayout == ImageLayout.DepthStencilAttachmentOptimal) {
            barrier.SubresourceRange.AspectMask = ImageAspectFlags.DepthBit;

            if (HasStencilComponent(format)) {
                barrier.SubresourceRange.AspectMask |= ImageAspectFlags.StencilBit;
            }
        } else {
            barrier.SubresourceRange.AspectMask = ImageAspectFlags.ColorBit;
        }

        PipelineStageFlags sourceStage;
        PipelineStageFlags destinationStage;

        if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.TransferWriteBit;

            sourceStage = PipelineStageFlags.TopOfPipeBit;
            destinationStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;

            sourceStage = PipelineStageFlags.TransferBit;
            destinationStage = PipelineStageFlags.FragmentShaderBit;
        }
        else if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.DepthStencilAttachmentOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask =
                AccessFlags.DepthStencilAttachmentReadBit |
                AccessFlags.DepthStencilAttachmentWriteBit;

            sourceStage = PipelineStageFlags.TopOfPipeBit;
            destinationStage = PipelineStageFlags.EarlyFragmentTestsBit;
        }
        else if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.General)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit | AccessFlags.ShaderWriteBit;
            sourceStage      = PipelineStageFlags.TopOfPipeBit;
            destinationStage = PipelineStageFlags.ComputeShaderBit;
        }
        else if (oldLayout == ImageLayout.General && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.ShaderWriteBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;
            sourceStage      = PipelineStageFlags.ComputeShaderBit;
            destinationStage = PipelineStageFlags.FragmentShaderBit;
        }
        else if (oldLayout == ImageLayout.ShaderReadOnlyOptimal && newLayout == ImageLayout.General)
        {
            barrier.SrcAccessMask = AccessFlags.ShaderReadBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit | AccessFlags.ShaderWriteBit;
            sourceStage      = PipelineStageFlags.FragmentShaderBit;
            destinationStage = PipelineStageFlags.ComputeShaderBit;
        }
        else
        {
            throw new ArgumentException("Unsupported layout transition!");
        }

        _vulkanDevice.Vk.CmdPipelineBarrier(
            commandBuffer,
            sourceStage, destinationStage,
            0,
            0, null,
            0, null,
            1, &barrier
        );

        _vulkanDevice.EndSingleTimeCommands(commandBuffer);
    }

    private bool HasStencilComponent(Format format) {
        return format == Format.D32SfloatS8Uint || format == Format.D24UnormS8Uint;
    }

    public void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height) 
    {
        CommandBuffer commandBuffer = _vulkanDevice.BeginSingleTimeCommands();

        BufferImageCopy region = new()
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0
        };

        region.ImageSubresource.AspectMask = ImageAspectFlags.ColorBit;
        region.ImageSubresource.MipLevel = 0;
        region.ImageSubresource.BaseArrayLayer = 0;
        region.ImageSubresource.LayerCount = 1;

        region.ImageOffset = new(0, 0, 0);
        region.ImageExtent = new(width, height, 1);

        _vulkanDevice.Vk.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, 1, &region);

        _vulkanDevice.EndSingleTimeCommands(commandBuffer);
    }

    public void CopyImageToBuffer(Image image, Buffer buffer, uint width, uint height) 
    {
        CommandBuffer commandBuffer = _vulkanDevice.BeginSingleTimeCommands();

        BufferImageCopy region = new()
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0
        };

        region.ImageSubresource.AspectMask = ImageAspectFlags.ColorBit;
        region.ImageSubresource.MipLevel = 0;
        region.ImageSubresource.BaseArrayLayer = 0;
        region.ImageSubresource.LayerCount = 1;

        region.ImageOffset = new(0, 0, 0);
        region.ImageExtent = new(width, height, 1);

        _vulkanDevice.Vk.CmdCopyImageToBuffer(commandBuffer, image, ImageLayout.TransferSrcOptimal, buffer, 1, &region);

        _vulkanDevice.EndSingleTimeCommands(commandBuffer);
    }

    public void CopyBufferToImageArray(Buffer buffer, Image image, uint width, uint height, uint layers) 
    {
        CommandBuffer commandBuffer = _vulkanDevice.BeginSingleTimeCommands();

        var regions = new BufferImageCopy[layers];
        for (uint i = 0; i < layers; i++)
        {
            BufferImageCopy region = new()
            {
                BufferOffset = (ulong)(width * height * 4) * i,
                BufferRowLength = 0,
                BufferImageHeight = 0
            };

            region.ImageSubresource.AspectMask = ImageAspectFlags.ColorBit;
            region.ImageSubresource.MipLevel = 0;
            region.ImageSubresource.BaseArrayLayer = i;
            region.ImageSubresource.LayerCount = 1;

            region.ImageOffset = new(0, 0, 0);
            region.ImageExtent = new(width, height, 1);

            regions[i] = region;
        }

        fixed (BufferImageCopy* pRegions = regions)
        _vulkanDevice.Vk.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, layers, pRegions);

        _vulkanDevice.EndSingleTimeCommands(commandBuffer);
    }
}