using System.Diagnostics;
using PBG.Data;
using Silk.NET.Vulkan;

namespace PBG.Graphics;

public unsafe class FBO : BufferBase
{
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
    public uint Width;
    public uint Height;

    private ImageLayout currentLayout = ImageLayout.Undefined;
    private int _currentFrame = -1;

    public FBO(int width, int height) : this((uint)width, (uint)height) {}
    public FBO(uint width, uint height)
    {
        Width = width;
        Height = height;

        CreateFramebuffer();
        CreateSampler();
    }

    public override void Resize(uint width, uint height)
    {
        Width = width;
        Height = height;
        currentLayout = ImageLayout.Undefined;

        DestroyBase();
        CreateFramebuffer();
    }

    private void CreateFramebuffer()
    {
        RenderPass renderPass = GraphicsContext.graphicsContext.framebufferRenderPass;
        GFX.CreateImage(Width, Height, GFX.SwapChainFormat, ImageTiling.Optimal,
            ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.SampledBit,
            MemoryPropertyFlags.DeviceLocalBit,
            out colorImage, out colorMemory);
        colorView = GFX.CreateImageView(colorImage, GFX.SwapChainFormat, ImageAspectFlags.ColorBit, 1);

        var depthFormat = GraphicsContext.graphicsContext.FindDepthFormat();
        GFX.CreateImage(Width, Height, depthFormat, ImageTiling.Optimal,
            ImageUsageFlags.DepthStencilAttachmentBit,
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
        GFX.DestroySampler(sampler);
        DestroyBase();
    }

    public void Bind()
    {
        currentRenderPassState = RenderPassState.FBO;
        GFX.Vk.CmdEndRenderPass(GFX.CommandBuffer);
        
        
        var barrier = new ImageMemoryBarrier
        {
            SType            = StructureType.ImageMemoryBarrier,
            OldLayout        = ImageLayout.Undefined,
            NewLayout        = ImageLayout.ColorAttachmentOptimal,
            SrcAccessMask    = currentLayout == ImageLayout.Undefined ? AccessFlags.None : AccessFlags.ShaderReadBit,
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
        GFX.Vk.CmdPipelineBarrier(GFX.CommandBuffer,
            PipelineStageFlags.FragmentShaderBit,
            PipelineStageFlags.ColorAttachmentOutputBit,
            DependencyFlags.None,
            0, null, 0, null,
            1, &barrier);

        var ctx = GraphicsContext.graphicsContext;
        var renderPassInfo = new RenderPassBeginInfo
        {
            SType       = StructureType.RenderPassBeginInfo,
            Framebuffer = framebuffer,
            RenderArea  = new Rect2D(new Offset2D(0, 0), new Extent2D(Width, Height)),
        };

        if (_currentFrame != GraphicsContext.graphicsContext.currentFrame)
        {
            _currentFrame = (int)GraphicsContext.graphicsContext.currentFrame;
            renderPassInfo.RenderPass  = ctx.framebufferRenderPass;

            ClearValue[] clearValues = new ClearValue[2];
            clearValues[0].Color        = new(0.0f, 0.0f, 0.0f, 0.0f);
            clearValues[1].DepthStencil = new(1.0f, 0);
            renderPassInfo.ClearValueCount = (uint)clearValues.Length;

            fixed (ClearValue* pClearValues = clearValues)
                renderPassInfo.PClearValues = pClearValues;
        }
        else
        {
            renderPassInfo.RenderPass  = ctx.framebufferRenderPassLoad;
        }

        GFX.Vk.CmdBeginRenderPass(GFX.CommandBuffer, &renderPassInfo, SubpassContents.Inline);
        currentLayout = ImageLayout.ColorAttachmentOptimal;
    }

    public void Unbind()
    {

        currentRenderPassState = RenderPassState.Main;
        GFX.Vk.CmdEndRenderPass(GFX.CommandBuffer);

        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = ImageLayout.Undefined,
            NewLayout = ImageLayout.ShaderReadOnlyOptimal,
            SrcAccessMask = AccessFlags.ColorAttachmentWriteBit,
            DstAccessMask = AccessFlags.None,  // Or AccessFlags.None if no immediate read
            Image = colorImage,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1,
            }
        };
        GFX.Vk.CmdPipelineBarrier(GFX.CommandBuffer,
            PipelineStageFlags.ColorAttachmentOutputBit,
            PipelineStageFlags.FragmentShaderBit,  // Or PipelineStageFlags.TopOfPipeBit if no immediate read
            DependencyFlags.None,
            0, null, 0, null,
            1, &barrier);

        var ctx = GraphicsContext.graphicsContext;
        var renderPassInfo = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = ctx.renderPassLoad,
            Framebuffer = ctx.currentFramebuffer,
            RenderArea = new Rect2D(new Offset2D(0, 0), ctx.swapChainExtent),
            ClearValueCount = 0,
            PClearValues = null
        };

        GFX.Vk.CmdBeginRenderPass(GFX.CommandBuffer, &renderPassInfo, SubpassContents.Inline);

        var viewport = new Viewport
        {
            X = 0.0f,
            Y = 0.0f,
            Width = ctx.swapChainExtent.Width,
            Height = ctx.swapChainExtent.Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        GFX.Vk.CmdSetViewport(GFX.CommandBuffer, 0, 1, &viewport);

        var scissor = new Rect2D
        {
            Offset = new Offset2D(0, 0),
            Extent = ctx.swapChainExtent
        };
        GFX.Vk.CmdSetScissor(GFX.CommandBuffer, 0, 1, &scissor);

        currentLayout = ImageLayout.ShaderReadOnlyOptimal;
    }
}