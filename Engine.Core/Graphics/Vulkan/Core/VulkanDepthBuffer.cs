using Silk.NET.Vulkan;

namespace PBG.Graphics.Vulkan;

public unsafe sealed class VulkanDepthBuffer : IDisposable
{
    private readonly VulkanDevice _vulkanDevice;
    private readonly VulkanSwapchain _vulkanSwapchain;
    private readonly VulkanImage _vulkanImage;

    public Image DepthImage { get; private set; }
    public DeviceMemory DepthImageMemory { get; private set; }
    public ImageView DepthImageView { get; private set; }
    public Format DepthImageFormat { get; private set; }

    public VulkanDepthBuffer(VulkanDevice vulkanDevice, VulkanSwapchain vulkanSwapchain, VulkanImage vulkanImage)
    {
        _vulkanDevice = vulkanDevice;
        _vulkanSwapchain = vulkanSwapchain;
        _vulkanImage = vulkanImage;

        CreateDepthResources();
    }

    private void CreateDepthResources()
    {
        DepthImageFormat = FindDepthFormat();

        _vulkanImage.CreateImage(_vulkanSwapchain.SwapChainExtent.Width, _vulkanSwapchain.SwapChainExtent.Height, DepthImageFormat, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, out var depthImage, out var depthImageMemory);
        DepthImage = depthImage;
        DepthImageMemory = depthImageMemory;

        DepthImageView = _vulkanImage.CreateImageView(DepthImage, DepthImageFormat, ImageAspectFlags.DepthBit, 1);

        _vulkanImage.TransitionImageLayout(DepthImage, DepthImageFormat, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal);
    }

    private Format FindSupportedFormat(List<Format> candidates, ImageTiling tiling, FormatFeatureFlags features) 
    {
        foreach (Format format in candidates) 
        {
            FormatProperties props;
            _vulkanDevice.Vk.GetPhysicalDeviceFormatProperties(_vulkanDevice.PhysicalDevice, format, &props);

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

    public void Dispose()
    {
        _vulkanDevice.Vk.DestroyImageView(_vulkanDevice.Device, DepthImageView, null);
        _vulkanDevice.Vk.DestroyImage(_vulkanDevice.Device, DepthImage, null);
        _vulkanDevice.Vk.FreeMemory(_vulkanDevice.Device, DepthImageMemory, null);
    }
}