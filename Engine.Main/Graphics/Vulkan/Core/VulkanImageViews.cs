using Silk.NET.Vulkan;

namespace PBG.Graphics.Vulkan;

public unsafe sealed class VulkanImageViews : IDisposable
{
    private readonly VulkanDevice _vulkanDevice;
    private readonly VulkanImage _vulkanImage;
    private readonly VulkanSwapchain _vulkanSwapchain;

    public ImageView[] SwapChainImageViews = [];

    public VulkanImageViews(VulkanDevice vulkanDevice, VulkanImage vulkanImage, VulkanSwapchain vulkanSwapchain)
    {
        _vulkanDevice = vulkanDevice;
        _vulkanImage = vulkanImage;
        _vulkanSwapchain = vulkanSwapchain;

        CreateImageViews();
    }

    private void CreateImageViews()
    {
        SwapChainImageViews = new ImageView[_vulkanSwapchain.SwapChainImages.Length];

        for (int i = 0; i < _vulkanSwapchain.SwapChainImages.Length; i++) 
        {
            SwapChainImageViews[i] = _vulkanImage.CreateImageView(_vulkanSwapchain.SwapChainImages[i], _vulkanSwapchain.SwapChainImageFormat, ImageAspectFlags.ColorBit, 1);
        }
    }

    public void Dispose()
    {
        foreach (var imageView in SwapChainImageViews) 
            _vulkanDevice.Vk.DestroyImageView(_vulkanDevice.Device, imageView, null);
    }
}