using PBG.Mathematics;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace PBG.Graphics.Vulkan;

public unsafe sealed class VulkanSwapchain : IDisposable
{
    private readonly VulkanDevice _vulkanDevice;
    private readonly IWindow _window;

    public SwapchainKHR SwapChain { get; private set; }
    public Image[] SwapChainImages { get; private set; } = [];
    public Format SwapChainImageFormat { get; private set; }
    public Extent2D SwapChainExtent { get; private set; }

    public VulkanSwapchain(VulkanDevice vulkanDevice, IWindow window)
    {
        _vulkanDevice = vulkanDevice;
        _window = window;

        CreateSwapChain();
    }

    private SurfaceFormatKHR ChooseSwapSurfaceFormat(SurfaceFormatKHR[] availableFormats) 
    {
        foreach (var availableFormat in availableFormats) 
        {
            if (availableFormat.Format == Format.B8G8R8A8Srgb && availableFormat.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr) 
            {
                return availableFormat;
            }
        }

        return availableFormats[0];
    }

    private PresentModeKHR ChooseSwapPresentMode(PresentModeKHR[] availablePresentModes) 
    {
        if (!_window.VSync)
        {
            foreach (var availablePresentMode in availablePresentModes) 
            {
                if (availablePresentMode == PresentModeKHR.ImmediateKhr)
                {
                    return availablePresentMode;
                }
            }
        }
    
        return PresentModeKHR.FifoKhr;
    }

    private Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities) 
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue) 
        {
            return capabilities.CurrentExtent;
        } 
        else 
        {
            Extent2D actualExtent = new((uint)VoxelEngine.Width, (uint)VoxelEngine.Height);

            actualExtent.Width = Mathf.Clampy(actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
            actualExtent.Height = Mathf.Clampy(actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

            return actualExtent;
        }
    }

    private void CreateSwapChain()
    {
        SwapChainSupportDetails swapChainSupport = _vulkanDevice.QuerySwapChainSupport();

        SurfaceFormatKHR surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
        PresentModeKHR presentMode = ChooseSwapPresentMode(swapChainSupport.PresentModes);
        Extent2D extent = ChooseSwapExtent(swapChainSupport.Capabilities);

        uint imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
        if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount) 
        {
            imageCount = swapChainSupport.Capabilities.MaxImageCount;
        }

        SwapchainCreateInfoKHR createInfo = new()
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = _vulkanDevice.Surface,
            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit
        };

        QueueFamilyIndices indices = _vulkanDevice.FindQueueFamilies();
        uint[] queueFamilyIndices = [indices.GraphicsFamily!.Value, indices.PresentFamily!.Value];

        if (indices.GraphicsFamily != indices.PresentFamily) {
            createInfo.ImageSharingMode = SharingMode.Concurrent;
            createInfo.QueueFamilyIndexCount = 2;
            fixed (uint* pQueueFamiluIndices = queueFamilyIndices)
            createInfo.PQueueFamilyIndices = pQueueFamiluIndices;
        } else {
            createInfo.ImageSharingMode = SharingMode.Exclusive;
            createInfo.QueueFamilyIndexCount = 0;
            createInfo.PQueueFamilyIndices = null;
        }

        createInfo.PreTransform = swapChainSupport.Capabilities.CurrentTransform;
        createInfo.CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr;
        createInfo.PresentMode = presentMode;
        createInfo.Clipped = true;
        createInfo.OldSwapchain = default;

        if (!_vulkanDevice.Vk.TryGetDeviceExtension(_vulkanDevice.Instance, _vulkanDevice.Device, out _vulkanDevice.KhrSwapchain))
            throw new Exception("Could not get KhrSwapchain extension");

        if (_vulkanDevice.KhrSwapchain.CreateSwapchain(_vulkanDevice.Device, &createInfo, null, out var swapchain) != Result.Success)
            throw new InvalidOperationException("failed to create swap chain!");

        SwapChain = swapchain;

        _vulkanDevice.KhrSwapchain.GetSwapchainImages(_vulkanDevice.Device, SwapChain, &imageCount, null);
        SwapChainImages = new Image[imageCount];
        fixed (Image* pSwapChainImages = SwapChainImages)
        _vulkanDevice.KhrSwapchain.GetSwapchainImages(_vulkanDevice.Device, SwapChain, &imageCount, pSwapChainImages);
        SwapChainImageFormat = surfaceFormat.Format;
        SwapChainExtent = extent;
    }

    public void Dispose()
    {
        _vulkanDevice.KhrSwapchain.DestroySwapchain(_vulkanDevice.Device, SwapChain, null);
    }
}