using PBG.Graphics.Vulkan;
using Silk.NET.Vulkan;

namespace PBG.Graphics;

public unsafe class FBO : BufferBase, IResizeable
{
    private static List<FBO> _fbos = [];

    public enum RenderPassState { None, Main, FBO }
    public static RenderPassState currentRenderPassState = RenderPassState.None;

    public Framebuffer framebuffer;
    public Image colorImage;
    public DeviceMemory colorMemory;
    public ImageView colorView;
    public Image depthImage;
    public DeviceMemory depthMemory;
    public ImageView depthView;
    public Sampler sampler;
    private Func<uint> _widthAction;
    private Func<uint> _heightAction;
    public uint Width;
    public uint Height;

    private bool _started = false;
    private ImageLayout _currentLayout = ImageLayout.ShaderReadOnlyOptimal;

    public FBO(int width, int height) : this((uint)width, (uint)height) {}
    public FBO(uint width, uint height) : this(() => width, () => height) {}
    public FBO(Func<uint> widthAction, Func<uint> heightAction)
    {
        _widthAction = widthAction;
        _heightAction = heightAction;

        Width = _widthAction();
        Height = _heightAction();

        CreateFramebuffer();
        CreateSampler();

        _fbos.Add(this);
    }

    public void Resize(uint width, uint height)
    {
        Width = _widthAction();
        Height = _heightAction();

        DestroyBase();
        CreateFramebuffer();
    }

    private void CreateFramebuffer()
    {
        RenderPass renderPass = VulkanInstance.Instance.FramebufferClearRenderPass.RenderPass;
        GFX.CreateImage(Width, Height, GFX.SwapChainFormat, ImageTiling.Optimal,
            ImageUsageFlags.TransferDstBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.SampledBit,
            MemoryPropertyFlags.DeviceLocalBit,
            out colorImage, out colorMemory);
        colorView = GFX.CreateImageView(colorImage, GFX.SwapChainFormat, ImageAspectFlags.ColorBit, 1);

        var depthFormat = VulkanInstance.Instance.VulkanDepthBuffer.FindDepthFormat();
        GFX.CreateImage(Width, Height, depthFormat, ImageTiling.Optimal,
            ImageUsageFlags.TransferDstBit | ImageUsageFlags.DepthStencilAttachmentBit | ImageUsageFlags.SampledBit,
            MemoryPropertyFlags.DeviceLocalBit,
            out depthImage, out depthMemory);
        depthView = GFX.CreateImageView(depthImage, depthFormat, ImageAspectFlags.DepthBit, 1);

        ImageView[] attachments = [colorView, depthView];
        var framebufferInfo = new FramebufferCreateInfo
        {
            SType           = StructureType.FramebufferCreateInfo,
            RenderPass      = renderPass,
            AttachmentCount = (uint)attachments.Length,
            Width           = Width,
            Height          = Height,
            Layers          = 1,
        };

        fixed (ImageView* pAttachments = attachments)
            framebufferInfo.PAttachments = pAttachments;

        if (GFX.CreateFramebuffer(&framebufferInfo, null, out framebuffer) != Result.Success)
            throw new InvalidOperationException("Failed to create offscreen framebuffer!");

        var cmd = GFX.BeginSingleTimeCommands();

        ImageMemoryBarrier colorBarrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = ImageLayout.Undefined,
            NewLayout = ImageLayout.ShaderReadOnlyOptimal,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = colorImage,
            SubresourceRange = new(ImageAspectFlags.ColorBit, 0, 1, 0, 1),
            SrcAccessMask = AccessFlags.None,
            DstAccessMask = AccessFlags.ShaderReadBit
        };

        ImageMemoryBarrier depthBarrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = ImageLayout.Undefined,
            NewLayout = ImageLayout.ShaderReadOnlyOptimal,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = depthImage,
            SubresourceRange = new(ImageAspectFlags.DepthBit, 0, 1, 0, 1),
            SrcAccessMask = AccessFlags.None,
            DstAccessMask = AccessFlags.DepthStencilAttachmentWriteBit
        };

        ImageMemoryBarrier* barriers = stackalloc ImageMemoryBarrier[] { colorBarrier, depthBarrier };

        GFX.Vk.CmdPipelineBarrier(cmd,
            PipelineStageFlags.TopOfPipeBit,
            PipelineStageFlags.FragmentShaderBit | PipelineStageFlags.EarlyFragmentTestsBit,
            0, 0, null, 0, null, 2, barriers);

        GFX.EndSingleTimeCommands(cmd);
    }

    private void CreateSampler()
    {
        var samplerInfo = new SamplerCreateInfo
        {
            SType        = StructureType.SamplerCreateInfo,
            MagFilter    = Filter.Linear,
            MinFilter    = Filter.Linear,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            MipmapMode   = SamplerMipmapMode.Linear,
            MinLod       = 0,
            MaxLod       = 1,
        };

        if (GFX.CreateSampler(&samplerInfo, null, out sampler) != Result.Success)
            throw new InvalidOperationException("Failed to create offscreen sampler!");
    }

    private void DestroyBase()
    {
        GFX.DestroyFramebuffer(framebuffer);
        GFX.DestroyImageView(colorView);
        GFX.DestroyImageView(depthView);
        GFX.DestroyImage(colorImage);
        GFX.DestroyImage(depthImage);
        GFX.FreeMemory(colorMemory);
        GFX.FreeMemory(depthMemory);
    }

    protected override void Destroy()
    {
        OnDispose?.Invoke(this);
        OnDispose = null;
        
        GFX.DestroySampler(sampler);
        DestroyBase();

        _fbos.Remove(this);
    }

    public void Clear()
    {
        var cmd = GFX.BeginSingleTimeCommands();

        ImageMemoryBarrier colorBarrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = ImageLayout.ShaderReadOnlyOptimal,
            NewLayout = ImageLayout.TransferDstOptimal,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = colorImage,
            SubresourceRange = new(ImageAspectFlags.ColorBit, 0, 1, 0, 1),
            SrcAccessMask = AccessFlags.None,
            DstAccessMask = AccessFlags.TransferWriteBit
        };

        GFX.Vk.CmdPipelineBarrier(cmd,
            PipelineStageFlags.TopOfPipeBit,
            PipelineStageFlags.TransferBit,
            0, 0, null, 0, null, 1, &colorBarrier);

        ClearColorValue clearColor = new(0f, 0f, 0f, 0f);
        ImageSubresourceRange range = new(ImageAspectFlags.ColorBit, 0, 1, 0, 1);
        GFX.Vk.CmdClearColorImage(cmd, colorImage, ImageLayout.TransferDstOptimal, &clearColor, 1, &range);
        
        colorBarrier.OldLayout = ImageLayout.TransferDstOptimal;
        colorBarrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
        colorBarrier.SrcAccessMask = AccessFlags.TransferWriteBit;
        colorBarrier.DstAccessMask = AccessFlags.ColorAttachmentWriteBit;
        
        GFX.Vk.CmdPipelineBarrier(cmd,
            PipelineStageFlags.TransferBit,
            PipelineStageFlags.ColorAttachmentOutputBit,
            0, 0, null, 0, null, 1, &colorBarrier);
        _currentLayout = ImageLayout.ShaderReadOnlyOptimal;

        ClearDepthStencilValue clearValue = new(1f, 0);
        ImageSubresourceRange depthRange = new(ImageAspectFlags.DepthBit, 0, 1, 0, 1);

        ImageMemoryBarrier depthBarrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = ImageLayout.ShaderReadOnlyOptimal,
            NewLayout = ImageLayout.TransferDstOptimal,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = depthImage,
            SubresourceRange = new(ImageAspectFlags.DepthBit, 0, 1, 0, 1),
            SrcAccessMask = AccessFlags.None,
            DstAccessMask = AccessFlags.TransferWriteBit
        };

        GFX.Vk.CmdPipelineBarrier(cmd,
            PipelineStageFlags.TopOfPipeBit,
            PipelineStageFlags.TransferBit,
            0, 0, null, 0, null, 1, &depthBarrier);

        GFX.Vk.CmdClearDepthStencilImage(cmd, depthImage, ImageLayout.TransferDstOptimal, &clearValue, 1, &depthRange);

        depthBarrier.OldLayout = ImageLayout.TransferDstOptimal;
        depthBarrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
        depthBarrier.SrcAccessMask = AccessFlags.TransferWriteBit;
        depthBarrier.DstAccessMask = AccessFlags.DepthStencilAttachmentWriteBit;
        
        GFX.Vk.CmdPipelineBarrier(cmd,
            PipelineStageFlags.TransferBit,
            PipelineStageFlags.EarlyFragmentTestsBit,
            0, 0, null, 0, null, 1, &depthBarrier);

        GFX.EndSingleTimeCommands(cmd);
    }

    public void Bind()
    {
        currentRenderPassState = RenderPassState.FBO;
        GFX.Vk.CmdEndRenderPass(GFX.CommandBuffer);
        Bind(GFX.CommandBuffer);
    }

    public void Bind(CommandBuffer commandBuffer)
    {
        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = ImageLayout.ShaderReadOnlyOptimal,
            NewLayout = ImageLayout.ColorAttachmentOptimal,
            SrcAccessMask    = AccessFlags.ShaderReadBit,
            DstAccessMask    = AccessFlags.ColorAttachmentWriteBit,
            Image            = colorImage,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask     = ImageAspectFlags.ColorBit,
                BaseMipLevel   = 0,
                LevelCount     = 1,
                BaseArrayLayer = 0,
                LayerCount     = 1,
            }
        };
        
        GFX.Vk.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.FragmentShaderBit, PipelineStageFlags.ColorAttachmentOutputBit, DependencyFlags.None, 0, null, 0, null, 1, &barrier);
        _currentLayout = ImageLayout.ColorAttachmentOptimal;

        var depthBarrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = ImageLayout.ShaderReadOnlyOptimal,
            NewLayout = ImageLayout.DepthStencilAttachmentOptimal,
            SrcAccessMask = AccessFlags.ShaderReadBit,
            DstAccessMask = AccessFlags.DepthStencilAttachmentWriteBit,
            Image = depthImage,
            SubresourceRange = new(ImageAspectFlags.DepthBit, 0, 1, 0, 1)
        };
        GFX.Vk.CmdPipelineBarrier(commandBuffer,
            PipelineStageFlags.FragmentShaderBit,
            PipelineStageFlags.EarlyFragmentTestsBit,
            DependencyFlags.None,
            0, null, 0, null, 1, &depthBarrier);

        var renderPassInfo = new RenderPassBeginInfo
        {
            SType       = StructureType.RenderPassBeginInfo,
            Framebuffer = framebuffer,
            RenderArea  = new Rect2D(new Offset2D(0, 0), new Extent2D(Width, Height)),
        };

        ClearValue* pClearValues = stackalloc ClearValue[2];
        pClearValues[0].Color        = new(0.0f, 0.0f, 0.0f, 0f);
        pClearValues[1].DepthStencil = new(1.0f, 0);

        var renderer = VulkanInstance.Instance;
        if (!_started)
        {
            _started = true;
            renderPassInfo.RenderPass      = renderer.FramebufferClearRenderPass.RenderPass;
            renderPassInfo.ClearValueCount = 2;
            renderPassInfo.PClearValues    = pClearValues;
        }
        else
        {
            renderPassInfo.RenderPass      = renderer.FramebufferLoadRenderPass.RenderPass;
            renderPassInfo.ClearValueCount = 0;
            renderPassInfo.PClearValues    = null;
        }

        GFX.Vk.CmdBeginRenderPass(commandBuffer, &renderPassInfo, SubpassContents.Inline);

        var viewport = new Viewport
        {
            X = 0.0f,
            Y = 0.0f,
            Width = Width,
            Height = Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        GFX.Vk.CmdSetViewport(commandBuffer, 0, 1, &viewport);

        var scissor = new Rect2D
        {
            Offset = new Offset2D(0, 0),
            Extent = new Extent2D(Width, Height)
        };
        GFX.Vk.CmdSetScissor(commandBuffer, 0, 1, &scissor);
        _currentLayout = ImageLayout.ShaderReadOnlyOptimal;
    }

    public void Unbind()
    {
        Unbind(GFX.CommandBuffer);   

        var renderer = VulkanInstance.Instance;
        var renderPassInfo = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = renderer.LoadRenderPass.RenderPass,
            Framebuffer = renderer.CurrentFramebuffer,
            RenderArea = new Rect2D(new Offset2D(0, 0), renderer.VulkanSwapchain.SwapChainExtent),
            ClearValueCount = 0,
            PClearValues = null
        };

        GFX.Vk.CmdBeginRenderPass(GFX.CommandBuffer, &renderPassInfo, SubpassContents.Inline);

        var viewport = new Viewport
        {
            X = 0.0f,
            Y = 0.0f,
            Width = renderer.VulkanSwapchain.SwapChainExtent.Width,
            Height = renderer.VulkanSwapchain.SwapChainExtent.Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        GFX.Vk.CmdSetViewport(GFX.CommandBuffer, 0, 1, &viewport);

        var scissor = new Rect2D
        {
            Offset = new Offset2D(0, 0),
            Extent = renderer.VulkanSwapchain.SwapChainExtent
        };
        GFX.Vk.CmdSetScissor(GFX.CommandBuffer, 0, 1, &scissor);
        _currentLayout = ImageLayout.ShaderReadOnlyOptimal;
    }

    public void Unbind(CommandBuffer commandBuffer)
    {
        GFX.Vk.CmdEndRenderPass(commandBuffer);

        var depthBarrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = ImageLayout.DepthStencilAttachmentOptimal,
            NewLayout = ImageLayout.ShaderReadOnlyOptimal,
            SrcAccessMask = AccessFlags.DepthStencilAttachmentWriteBit,
            DstAccessMask = AccessFlags.ShaderReadBit,
            Image = depthImage,
            SubresourceRange = new(ImageAspectFlags.DepthBit, 0, 1, 0, 1)
        };
        GFX.Vk.CmdPipelineBarrier(commandBuffer,
            PipelineStageFlags.LateFragmentTestsBit,
            PipelineStageFlags.FragmentShaderBit,
            DependencyFlags.None,
            0, null, 0, null, 1, &depthBarrier);
        _currentLayout = ImageLayout.ShaderReadOnlyOptimal;
    }
    
    public byte[] GetPixels()
    {
        GFX.Vk.DeviceWaitIdle(GFX.Device);

        uint imageSize = (uint)(Width * Height * 4);
        GFX.CreateBuffer(imageSize, BufferUsageFlags.TransferDstBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            out Silk.NET.Vulkan.Buffer stagingBuffer, out DeviceMemory stagingMemory);

        var cmd = GFX.BeginSingleTimeCommands();

        var toTransfer = new ImageMemoryBarrier
        {
            SType               = StructureType.ImageMemoryBarrier,
            OldLayout           = _currentLayout,
            NewLayout           = ImageLayout.TransferSrcOptimal,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image               = colorImage,
            SubresourceRange    = new(ImageAspectFlags.ColorBit, 0, 1, 0, 1),
            SrcAccessMask       = AccessFlags.ShaderReadBit,
            DstAccessMask       = AccessFlags.TransferReadBit
        };
        GFX.Vk.CmdPipelineBarrier(cmd,
            PipelineStageFlags.FragmentShaderBit,
            PipelineStageFlags.TransferBit,
            0, 0, null, 0, null, 1, &toTransfer);

        // copy to buffer
        var region = new BufferImageCopy
        {
            BufferOffset      = 0,
            BufferRowLength   = 0,
            BufferImageHeight = 0,
            ImageSubresource  = new ImageSubresourceLayers
            {
                AspectMask     = ImageAspectFlags.ColorBit,
                MipLevel       = 0,
                BaseArrayLayer = 0,
                LayerCount     = 1,
            },
            ImageOffset = new Offset3D(0, 0, 0),
            ImageExtent = new Extent3D((uint)Width, (uint)Height, 1),
        };
        GFX.Vk.CmdCopyImageToBuffer(cmd, colorImage,
            ImageLayout.TransferSrcOptimal, stagingBuffer, 1, &region);

        var toOriginal = new ImageMemoryBarrier
        {
            SType               = StructureType.ImageMemoryBarrier,
            OldLayout           = ImageLayout.TransferSrcOptimal,
            NewLayout           = _currentLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image               = colorImage,
            SubresourceRange    = new(ImageAspectFlags.ColorBit, 0, 1, 0, 1),
            SrcAccessMask       = AccessFlags.TransferReadBit,
            DstAccessMask       = AccessFlags.ShaderReadBit
        };
        GFX.Vk.CmdPipelineBarrier(cmd,
            PipelineStageFlags.TransferBit,
            PipelineStageFlags.FragmentShaderBit,
            0, 0, null, 0, null, 1, &toOriginal);

        GFX.EndSingleTimeCommands(cmd);

        void* data;
        GFX.MapMemory(stagingMemory, 0, imageSize, 0, &data);
        byte[] pixels = new byte[Width * Height * 4];
        HelperFunctions.MemCpyFrom(data, pixels, imageSize, imageSize);
        GFX.UnmapMemory(stagingMemory);

        GFX.DestroyBuffer(stagingBuffer);
        GFX.FreeMemory(stagingMemory);

        // swap B and R channels
        for (int i = 0; i < pixels.Length; i += 4)
        {
            byte b = pixels[i + 0];
            byte r = pixels[i + 2];
            pixels[i + 0] = r;
            pixels[i + 2] = b;
        }

        return pixels;
    }

    public void Reset()
    {
        _started = false;
    }

    public static void ResetAll()
    {
        for (int i = 0; i < _fbos.Count; i++)
            _fbos[i].Reset();
    }
}