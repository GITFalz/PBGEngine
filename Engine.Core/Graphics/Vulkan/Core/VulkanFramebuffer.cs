using Silk.NET.Vulkan;

namespace PBG.Graphics.Vulkan;

public sealed unsafe class VulkanFramebuffer : IDisposable
{
    private readonly VulkanDevice _vulkanDevice;
    private readonly VulkanSwapchain _vulkanSwapchain;
    private readonly VulkanImageViews _vulkanImageViews;
    private readonly VulkanDepthBuffer _vulkanDepthBuffer;
    private readonly VulkanRenderPass _vulkanRenderPass;

    public Framebuffer[] SwapChainFramebuffers { get; private set; } = [];

    public VulkanFramebuffer(VulkanDevice vulkanDevice, VulkanSwapchain vulkanSwapchain, VulkanImageViews vulkanImageViews, VulkanDepthBuffer depthBuffer, VulkanRenderPass renderPass)
    {
        _vulkanDevice = vulkanDevice;
        _vulkanSwapchain = vulkanSwapchain;
        _vulkanImageViews = vulkanImageViews;
        _vulkanDepthBuffer = depthBuffer;
        _vulkanRenderPass = renderPass;

        CreateFramebuffers();
    }

    private void CreateFramebuffers()
    {
        SwapChainFramebuffers = new Framebuffer[_vulkanImageViews.SwapChainImageViews.Length];

        for (int i = 0; i < _vulkanImageViews.SwapChainImageViews.Length; i++) {
            ImageView[] attachments = [_vulkanImageViews.SwapChainImageViews[i], _vulkanDepthBuffer.DepthImageView];

            FramebufferCreateInfo framebufferInfo = new()
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = _vulkanRenderPass.RenderPass,
                AttachmentCount = (uint)attachments.Length,
                Width = _vulkanSwapchain.SwapChainExtent.Width,
                Height = _vulkanSwapchain.SwapChainExtent.Height,
                Layers = 1
            };

            fixed (ImageView* pAttachments = attachments)
            framebufferInfo.PAttachments = pAttachments;

            if (_vulkanDevice.Vk.CreateFramebuffer(_vulkanDevice.Device, &framebufferInfo, null, out SwapChainFramebuffers[i]) != Result.Success) {
                throw new InvalidOperationException("failed to create framebuffer!");
            }
        }
    }

    public void Dispose()
    {
        foreach (var framebuffer in SwapChainFramebuffers)
            _vulkanDevice.Vk.DestroyFramebuffer(_vulkanDevice.Device, framebuffer, null);
        
    }
}