using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Buffer = Silk.NET.Vulkan.Buffer;
using Framebuffer = Silk.NET.Vulkan.Framebuffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace PBG.Graphics;

public unsafe class GraphicsContext
{
    public const int MAX_FRAMES_IN_FLIGHT = 2;
    public static GraphicsContext graphicsContext;

    public int Width;
    public int Height;

    public IWindow window;

    public ShaderCompiler shaderCompiler;
    public ShaderBuffer shaderBuffer;

    public uint currentFrame = 0;
    public bool blockBinding = true;

    public Vk vk;

    public Instance instance;

    public ExtDebugUtils? debugUtils = null;
    public KhrSurface khrSurface = null!;
    public KhrSwapchain khrSwapchain = null!;

    public DebugUtilsMessengerEXT debugMessenger;

    public PhysicalDevice physicalDevice = default;

    public Device device;
    public Queue graphicsQueue;
    public Queue presentQueue;

    public SurfaceKHR surface;
    
    public SwapchainKHR swapChain;
    public Image[] swapChainImages = [];
    public Format swapChainImageFormat;
    public Extent2D swapChainExtent;

    public ImageView[] swapChainImageViews = [];

    public RenderPass renderPass;
    public RenderPass renderPassLoad;

    public RenderPass framebufferRenderPass;
    public RenderPass framebufferRenderPassLoad;

    public Framebuffer[] swapChainFramebuffers = [];
    public Framebuffer currentFramebuffer;

    public CommandPool commandPool;
    public CommandBuffer[] commandBuffers = new CommandBuffer[MAX_FRAMES_IN_FLIGHT];
    public CommandBuffer commandBuffer => commandBuffers[currentFrame];

    public Image depthImage;
    public DeviceMemory depthImageMemory;
    public ImageView depthImageView;

    public Semaphore[] imageAvailableSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
    public Semaphore[] renderFinishedSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
    public Fence[] inFlightFences = new Fence[MAX_FRAMES_IN_FLIGHT];
    public Fence[] imagesInFlight;

    public Semaphore imageSemaphore => imageAvailableSemaphores[currentFrame];
    public Semaphore renderSemaphore => renderFinishedSemaphores[currentFrame];

    public byte* mainPtr = (byte*)"main".ToPtr();

    public GraphicsContext(int width, int height)
    {
        _ = new GFX(this);

        graphicsContext = this;
        vk = Vk.GetApi();

        var options = WindowOptions.DefaultVulkan;

        options.Size   = new Vector2D<int>(width, height);
        options.Title  = "My First Silk.NET Window";
        options.VSync  = false;

        window = Window.Create(options);

        shaderCompiler = new();
        shaderBuffer = new(this);
    }

    private Format FindSupportedFormat(List<Format> candidates, ImageTiling tiling, FormatFeatureFlags features) 
    {
        foreach (Format format in candidates) 
        {
            FormatProperties props;
            vk.GetPhysicalDeviceFormatProperties(physicalDevice, format, &props);

            if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features) {
                return format;
            } else if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features) {
                return format;
            }
        }

        throw new InvalidOperationException("failed to find supported format!");
    }

    public Format FindDepthFormat() {
        return FindSupportedFormat(
            [Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint],
            ImageTiling.Optimal,
            FormatFeatureFlags.DepthStencilAttachmentBit
        );
    }

    public CommandBuffer CreateCommandBuffer() 
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = commandPool,
            Level = CommandBufferLevel.Secondary,
            CommandBufferCount = 1
        };

        if (vk.AllocateCommandBuffers(device, &allocInfo, out CommandBuffer cb) != Result.Success) {
            throw new InvalidOperationException("failed to allocate command buffers!");
        }

        return cb;
    }
    
    public void CreateBuffer<T>(T[] array, BufferUsageFlags bufferType, MemoryPropertyFlags properties, out Buffer buffer, out DeviceMemory bufferMemory) where T : unmanaged
    {
        var bufferSize = (ulong)(Marshal.SizeOf<T>() * array.Length);

        CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer stagingBuffer, out DeviceMemory stagingBufferMemory);

        void* data; 
        vk.MapMemory(device, stagingBufferMemory, 0, bufferSize, 0, &data);
        HelperFunctions.MemCpyTo(array, data, bufferSize, bufferSize);
        vk.UnmapMemory(device, stagingBufferMemory);

        CreateBuffer(bufferSize, BufferUsageFlags.TransferDstBit | bufferType, properties, out buffer, out bufferMemory);
    
        CopyBuffer(stagingBuffer, buffer, bufferSize);

        vk.DestroyBuffer(device, stagingBuffer, null);
        vk.FreeMemory(device, stagingBufferMemory, null);
    }

    public void CreateBuffer<T>(BufferUsageFlags bufferType, T[] array, out Buffer buffer, out DeviceMemory bufferMemory) where T : unmanaged
    {
        var bufferSize = (ulong)(Marshal.SizeOf<T>() * array.Length);

        CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer stagingBuffer, out DeviceMemory stagingBufferMemory);

        void* data; 
        vk.MapMemory(device, stagingBufferMemory, 0, bufferSize, 0, &data);
        HelperFunctions.MemCpyTo(array, data, bufferSize, bufferSize);
        vk.UnmapMemory(device, stagingBufferMemory);

        CreateBuffer(bufferSize, BufferUsageFlags.TransferDstBit | bufferType, MemoryPropertyFlags.DeviceLocalBit, out buffer, out bufferMemory);
    
        CopyBuffer(stagingBuffer, buffer, bufferSize);

        vk.DestroyBuffer(device, stagingBuffer, null);
        vk.FreeMemory(device, stagingBufferMemory, null);
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

        if (vk.CreateBuffer(device, &bufferInfo, null, out buffer) != Result.Success) {
            throw new InvalidOperationException("failed to create buffer!");
        }

        MemoryRequirements memRequirements;
        vk.GetBufferMemoryRequirements(device, buffer, &memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties)
        };

        if (vk.AllocateMemory(device, &allocInfo, null, out bufferMemory) != Result.Success) {
            vk.DestroyBuffer(device, buffer, null);
            buffer = default;
            throw new InvalidOperationException("failed to allocate buffer memory!");
        }

        vk.BindBufferMemory(device, buffer, bufferMemory, 0);
    }

    public void UpdateBuffer<T>(T[] array, Buffer buffer) where T : unmanaged
    {
        var bufferSize = (ulong)(Marshal.SizeOf<T>() * array.Length);

        CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer stagingBuffer, out DeviceMemory stagingBufferMemory);

        void* data; 
        vk.MapMemory(device, stagingBufferMemory, 0, bufferSize, 0, &data);
        HelperFunctions.MemCpyTo(array, data, bufferSize, bufferSize);
        vk.UnmapMemory(device, stagingBufferMemory);
    
        CopyBuffer(stagingBuffer, buffer, bufferSize);

        vk.DestroyBuffer(device, stagingBuffer, null);
        vk.FreeMemory(device, stagingBufferMemory, null);
    }

    public void UpdateBuffer<T>(T[] array, Buffer buffer, ulong offsetBytes = 0, ulong sizeBytes = 0) where T : unmanaged
    {
        ulong elementSize = (ulong)Marshal.SizeOf<T>();
        ulong totalSize   = elementSize * (ulong)array.Length;
        ulong copySize    = sizeBytes == 0 ? totalSize : sizeBytes;

        CreateBuffer(copySize, BufferUsageFlags.TransferSrcBit, 
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, 
            out Buffer stagingBuffer, out DeviceMemory stagingBufferMemory);

        void* data;
        vk.MapMemory(device, stagingBufferMemory, 0, copySize, 0, &data);
        HelperFunctions.MemCpyTo(array, data, copySize, copySize);
        vk.UnmapMemory(device, stagingBufferMemory);

        CopyBuffer(stagingBuffer, buffer, copySize, 0, offsetBytes);

        vk.DestroyBuffer(device, stagingBuffer, null);
        vk.FreeMemory(device, stagingBufferMemory, null);
    }

    public void UpdateBufferFull<T>(T[] array, Buffer buffer, ulong offsetBytes = 0, ulong sizeBytes = 0) where T : unmanaged
    {
        ulong elementSize = (ulong)Marshal.SizeOf<T>();
        ulong totalSize   = elementSize * (ulong)array.Length;
        ulong copySize    = sizeBytes == 0 ? totalSize : sizeBytes;

        CreateBuffer(copySize, BufferUsageFlags.TransferSrcBit, 
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, 
            out Buffer stagingBuffer, out DeviceMemory stagingBufferMemory);

        void* data;
        vk.MapMemory(device, stagingBufferMemory, 0, copySize, 0, &data);

        // offset into the source array too!
        fixed (T* pArray = array)
        {
            byte* src = (byte*)pArray + offsetBytes; // <-- offset source
            HelperFunctions.MemCpyTo<T>(src, data, copySize, copySize);
        }

        vk.UnmapMemory(device, stagingBufferMemory);
        CopyBuffer(stagingBuffer, buffer, copySize, 0, offsetBytes); // dst offset on GPU

        vk.DestroyBuffer(device, stagingBuffer, null);
        vk.FreeMemory(device, stagingBufferMemory, null);
    }

    public void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size, ulong srcOffset = 0, ulong dstOffset = 0) 
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = commandPool,
            CommandBufferCount = 1
        };

        CommandBuffer commandBuffer;
        vk.AllocateCommandBuffers(device, &allocInfo, &commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        vk.BeginCommandBuffer(commandBuffer, &beginInfo);

        BufferCopy copyRegion = new()
        {
            SrcOffset = srcOffset, // Optional
            DstOffset = dstOffset, // Optional
            Size = size
        };

        vk.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, &copyRegion);

        vk.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };

        vk.QueueSubmit(graphicsQueue, 1, &submitInfo, default);
        vk.QueueWaitIdle(graphicsQueue);

        vk.FreeCommandBuffers(device, commandPool, 1, &commandBuffer);
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

        if (vk.CreateImage(device, &imageInfo, null, out image) != Result.Success) {
            throw new InvalidOperationException("failed to create image!");
        }

        MemoryRequirements memRequirements;
        vk.GetImageMemoryRequirements(device, image, &memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties)
        };

        if (vk.AllocateMemory(device, &allocInfo, null, out imageMemory) != Result.Success) {
            throw new InvalidOperationException("failed to allocate image memory!");
        }

        vk.BindImageMemory(device, image, imageMemory, 0);
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

        if (vk.CreateImage(device, &imageInfo, null, out image) != Result.Success) {
            throw new InvalidOperationException("failed to create image!");
        }

        MemoryRequirements memRequirements;
        vk.GetImageMemoryRequirements(device, image, &memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties)
        };

        if (vk.AllocateMemory(device, &allocInfo, null, out imageMemory) != Result.Success) {
            throw new InvalidOperationException("failed to allocate image memory!");
        }

        vk.BindImageMemory(device, image, imageMemory, 0);
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

        if (vk.CreateImageView(device, &viewInfo, null, out ImageView imageView) != Result.Success)
        {
            throw new InvalidOperationException("failed to create texture image view!");
        }

        return imageView;
    }

    public CommandBuffer BeginSingleTimeCommands() 
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = commandPool,
            CommandBufferCount = 1
        };

        CommandBuffer commandBuffer;
        vk.AllocateCommandBuffers(device, &allocInfo, &commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        vk.BeginCommandBuffer(commandBuffer, &beginInfo);

        return commandBuffer;
    }

    public void EndSingleTimeCommands(CommandBuffer commandBuffer) 
    {
        vk.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };

        vk.QueueSubmit(graphicsQueue, 1, &submitInfo, default);
        vk.QueueWaitIdle(graphicsQueue);

        vk.FreeCommandBuffers(device, commandPool, 1, &commandBuffer);
    }

    public void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout) 
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

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

        vk.CmdPipelineBarrier(
            commandBuffer,
            sourceStage, destinationStage,
            0,
            0, null,
            0, null,
            1, &barrier
        );

        EndSingleTimeCommands(commandBuffer);
    }

    public void TransitionImageArrayLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout, uint layers) 
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

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
        else
        {
            throw new ArgumentException("Unsupported layout transition!");
        }

        vk.CmdPipelineBarrier(
            commandBuffer,
            sourceStage, destinationStage,
            0,
            0, null,
            0, null,
            1, &barrier
        );

        EndSingleTimeCommands(commandBuffer);
    }

    private bool HasStencilComponent(Format format) {
        return format == Format.D32SfloatS8Uint || format == Format.D24UnormS8Uint;
    }

    public void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height) 
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

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

        vk.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, 1, &region);

        EndSingleTimeCommands(commandBuffer);
    }

    public void CopyImageToBuffer(Image image, Buffer buffer, uint width, uint height) 
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

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

        vk.CmdCopyImageToBuffer(commandBuffer, image, ImageLayout.TransferSrcOptimal, buffer, 1, &region);

        EndSingleTimeCommands(commandBuffer);
    }

    public void CopyBufferToImageArray(Buffer buffer, Image image, uint width, uint height, uint layers) 
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

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
        vk.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, layers, pRegions);

        EndSingleTimeCommands(commandBuffer);
    }

    public uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties) 
    {
        PhysicalDeviceMemoryProperties memProperties;
        vk.GetPhysicalDeviceMemoryProperties(physicalDevice, &memProperties);

        for (int i = 0; i < memProperties.MemoryTypeCount; i++) {
            if ((typeFilter & (1 << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties) {
                return (uint)i;
            }
        }

        throw new InvalidOperationException("failed to find suitable memory type!");
    }



    public void RecordMeshCommandBuffer(Shader shader, CommandBuffer cmd, Buffer[] vertexBuffers, Buffer indexBuffer, ulong[] offsets, uint indexCount)
    {
        CommandBufferInheritanceInfo inheritanceInfo = new()
        {
            SType = StructureType.CommandBufferInheritanceInfo,
            RenderPass = renderPass,
            Subpass = 0
        };

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.RenderPassContinueBit,
            PInheritanceInfo = &inheritanceInfo
        };

        vk.BeginCommandBuffer(cmd, &beginInfo);

        Viewport viewport = new()
        {
            X = 0.0f,
            Y = 0.0f,
            Width = (float)swapChainExtent.Width,
            Height = (float)swapChainExtent.Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        vk.CmdSetViewport(cmd, 0, 1, &viewport);

        Rect2D scissor = new()
        {
            Offset = new(0, 0),
            Extent = swapChainExtent
        };
        vk.CmdSetScissor(cmd, 0, 1, &scissor);

        vk.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, shader.graphicsPipeline);
        //vk.CmdBindDescriptorSets(cmd, PipelineBindPoint.Graphics, shader.pipelineLayout, 0, 1, ref shader.descriptorSets[currentFrame], 0, null);

        fixed (Buffer* PBGert = vertexBuffers)
        fixed (ulong* pOffset = offsets)
        vk.CmdBindVertexBuffers(cmd, 0, (uint)vertexBuffers.Length, PBGert, pOffset);

        vk.CmdBindIndexBuffer(cmd, indexBuffer, 0, IndexType.Uint32);

        vk.CmdDrawIndexed(cmd, indexCount, 1, 0, 0, 0);

        vk.EndCommandBuffer(cmd);
    }
}