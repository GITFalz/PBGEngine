using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;

namespace PBG.Graphics.Vulkan;

public unsafe sealed partial class VulkanDevice : IDisposable
{
    internal Vk Vk { get; private set; }
    internal Instance Instance { get; private set; }
    internal PhysicalDevice PhysicalDevice { get; private set; } = default;
    internal Device Device { get; private set; }

    // Surface
    internal SurfaceKHR Surface;
    internal KhrSurface KhrSurface;
    internal KhrSwapchain KhrSwapchain;

    public Queue GraphicsQueue;
    public Queue PresentQueue;

    private ExtDebugUtils? _debugUtils = null;
    private DebugUtilsMessengerEXT _debugMessenger;


    private readonly string[] _validationLayers = ["VK_LAYER_KHRONOS_validation"];
    private readonly string[] _deviceExtensions = [KhrSwapchain.ExtensionName, "VK_EXT_memory_budget"];

    public CommandPool CommandPool;


    private IWindow _window;
    private bool _enableValidation;

    public VulkanDevice(IWindow window, bool enableValidation = true)
    {
        Vk = Vk.GetApi();

        _window = window;
        _enableValidation = enableValidation;

        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
        PickPhysicalDevice();
        CreateLogicalDevice();
        CreateCommandPool();
    }


    public void Dispose()
    {
        Vk.DestroyCommandPool(Device, CommandPool, null);

        Vk.DestroyDevice(Device, null);
        if (_enableValidation)
            _debugUtils!.DestroyDebugUtilsMessenger(Instance, _debugMessenger, null);

        KhrSurface?.DestroySurface(Instance, Surface, null);
        Vk.DestroyInstance(Instance, null);
        Vk.Dispose();
    }
}