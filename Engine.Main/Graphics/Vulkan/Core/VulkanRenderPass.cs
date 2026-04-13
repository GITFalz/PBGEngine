using Silk.NET.Vulkan;

namespace PBG.Graphics.Vulkan;

public sealed unsafe class VulkanRenderPass : IDisposable
{
    private readonly VulkanDevice _vulkanDevice;

    public RenderPass RenderPass { get; private set; }

    public VulkanRenderPass(VulkanDevice vulkanDevice, Format colorFormat, Format depthFormat, ImageLayout colorInitialLayout,ImageLayout colorFinalLayout, AttachmentLoadOp loadOp)
    {
        _vulkanDevice = vulkanDevice;

        CreateRenderPass(colorFormat, depthFormat, colorInitialLayout, colorFinalLayout, loadOp, out var renderPass);
        RenderPass = renderPass;
    }

    private void CreateRenderPass(Format colorFormat, Format depthFormat, ImageLayout colorInitialLayout,ImageLayout colorFinalLayout, AttachmentLoadOp loadOp, out RenderPass renderPass)
    {
        AttachmentDescription colorAttachment = new()
        {
            Format        = colorFormat,
            Samples       = SampleCountFlags.Count1Bit,
            LoadOp        = loadOp,
            StoreOp       = AttachmentStoreOp.Store,
            InitialLayout = colorInitialLayout,
            FinalLayout   = colorFinalLayout
        };

        AttachmentDescription depthAttachment = new()
        {
            Format             = depthFormat,
            Samples            = SampleCountFlags.Count1Bit,
            LoadOp             = loadOp,
            StoreOp            = AttachmentStoreOp.Store,
            StencilLoadOp      = AttachmentLoadOp.DontCare,
            StencilStoreOp     = AttachmentStoreOp.DontCare,
            InitialLayout      = loadOp == AttachmentLoadOp.Clear ? ImageLayout.Undefined : ImageLayout.DepthStencilAttachmentOptimal,
            FinalLayout        = ImageLayout.DepthStencilAttachmentOptimal
        };

        AttachmentReference colorAttachmentRef = new()
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal
        };

        AttachmentReference depthAttachmentRef = new()
        {
            Attachment = 1,
            Layout = ImageLayout.DepthStencilAttachmentOptimal
        };

        SubpassDescription subpass = new()
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef,
            PDepthStencilAttachment = &depthAttachmentRef
        };

        SubpassDependency dependency = new()
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.LateFragmentTestsBit,
            SrcAccessMask = AccessFlags.DepthStencilAttachmentWriteBit,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
        };

        AttachmentDescription[] attachments = [colorAttachment, depthAttachment];
        RenderPassCreateInfo renderPassInfo = new()
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = (uint)attachments.Length,
            SubpassCount = 1,
            PSubpasses = &subpass,
            DependencyCount = 1
        };

        fixed (AttachmentDescription* pAttachments = attachments)
        renderPassInfo.PAttachments = pAttachments;

        var dependencies = stackalloc SubpassDependency[] { dependency };
        renderPassInfo.PDependencies = dependencies;

        if (_vulkanDevice.Vk.CreateRenderPass(_vulkanDevice.Device, &renderPassInfo, null, out renderPass) != Result.Success) {
            throw new InvalidOperationException("failed to create render pass!");
        }
    }

    public void Dispose()
    {
        _vulkanDevice.Vk.DestroyRenderPass(_vulkanDevice.Device, RenderPass, null);
    }
}