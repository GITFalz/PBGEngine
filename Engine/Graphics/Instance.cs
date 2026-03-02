using System.Runtime.InteropServices;
using PBG.MathLibrary;
using Silk.NET.Core;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Image = Silk.NET.Vulkan.Image;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace PBG.Graphics;

public unsafe class VulkanInstance
{
    const bool enableValidationLayers = true;

    public GraphicsContext context;
    public GameWindow gameWindow;

    struct QueueFamilyIndices
    {
        public uint? GraphicsFamily;
        public uint? PresentFamily;
        public readonly bool IsComplete => GraphicsFamily.HasValue && PresentFamily.HasValue;
    }

    struct SwapChainSupportDetails {
        public SurfaceCapabilitiesKHR Capabilities;
        public SurfaceFormatKHR[] Formats = [];
        public PresentModeKHR[] PresentModes = [];
        public SwapChainSupportDetails() {}
    };

    private readonly string[] validationLayers = ["VK_LAYER_KHRONOS_validation"];
    private readonly string[] deviceExtensions = [KhrSwapchain.ExtensionName];

    private IInputContext? input = null;

    public VulkanInstance(GameWindow gameWindow, int width, int height)
    {
        this.gameWindow = gameWindow;
        context = new(width, height); 
    }

    public void Run()
    {
        context.window.Load   += OnLoad;
        context.window.Update += OnUpdate;
        context.window.Render += OnRender;
        context.window.Closing += OnClosing;
        context.window.FramebufferResize += OnResize;

        context.window.Run();
    } 

    private void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
        PickPhysicalDevice();
        CreateLogicalDevice();
        CreateSwapChain();
        CreateImageViews();

        var depthFormat = context.FindDepthFormat();
        CreateRenderPass(context.swapChainImageFormat, depthFormat, ImageLayout.Undefined, ImageLayout.PresentSrcKhr, AttachmentLoadOp.Clear, out context.renderPass);
        CreateRenderPass(context.swapChainImageFormat, depthFormat, ImageLayout.PresentSrcKhr, ImageLayout.PresentSrcKhr, AttachmentLoadOp.Load, out context.renderPassLoad);
        CreateRenderPass(context.swapChainImageFormat, depthFormat, ImageLayout.Undefined, ImageLayout.ShaderReadOnlyOptimal, AttachmentLoadOp.Clear, out context.framebufferRenderPass);
        CreateRenderPass(context.swapChainImageFormat, depthFormat, ImageLayout.PresentSrcKhr, ImageLayout.ShaderReadOnlyOptimal, AttachmentLoadOp.Load, out context.framebufferRenderPassLoad);

        CreateCommandPool();
        CreateDepthResources();
        CreateFramebuffers();
        CreateCommandBuffer();
        CreateSyncObjects();
    }

    private void OnLoad()
    {
        Console.WriteLine($"Window loaded - {context.Width}x{context.Height}");

        input = context.window.CreateInput();

        // Keyboard
        foreach (var keyboard in input.Keyboards)
        {
            keyboard.KeyDown += gameWindow.OnKeyDown;
            keyboard.KeyUp += gameWindow.OnKeyUp;
            keyboard.KeyChar += gameWindow.OnKeyChar;
        }

        // Mouse
        foreach (var mouse in input.Mice)
        {
            mouse.MouseMove += (mouse, position) => gameWindow.OnMouseMove(mouse, position);
            mouse.MouseDown += gameWindow.OnMouseDown;
            mouse.MouseUp += gameWindow.OnMouseUp;
            mouse.Scroll += gameWindow.OnScroll;
        }

        gameWindow.Keyboard = input.Keyboards[0];
        gameWindow.Mouse = input.Mice[0];
         
        InitVulkan();
        gameWindow.OnLoad();
    }

    private void OnResize(Vector2D<int> vector2D)
    {
        context.Width = vector2D.X;
        context.Height = vector2D.Y;

        if (context.Width == 0 || context.Height == 0) 
            return;

        RecreateSwapChain();
        BufferBase.ResizeAll((uint)context.Width, (uint)context.Height);

        gameWindow.OnResize(context.Width, context.Height);
    }

    private void OnUpdate(double deltaSeconds)
    {
        gameWindow.OnUpdate(deltaSeconds);
    }

    #region Instance
    private void CreateInstance()
    {
        if (enableValidationLayers && !CheckValidationLayerSupport()) {
            throw new Exception("validation layers requested, but not available!");
        }

        var appName = "Hello Triangle"u8;
        var engineName = "No Engine"u8;

        fixed (byte* pAppName = appName)
        fixed (byte* pEngineName = engineName)
        {
            var appInfo = new ApplicationInfo
            {
                SType = StructureType.ApplicationInfo,
                PApplicationName   = pAppName,
                ApplicationVersion = Vk.MakeVersion(1, 3, 0),
                PEngineName        = pEngineName,
                EngineVersion      = Vk.MakeVersion(1, 3, 0),
                ApiVersion         = Vk.Version13
            };
            
            var createInfo = new InstanceCreateInfo
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo,
                // EnabledExtensionCount    = 0,
                // PpEnabledExtensionNames  = null,
                EnabledLayerCount        = 0,
                // PpEnabledLayerNames      = null,
                // Flags                    = 0,   // InstanceCreateFlags (rarely used)
                // pNext                    = null
            };
            
            var extensions = GetRequiredExtensions();
            createInfo.EnabledExtensionCount = (uint)extensions.Length;
            createInfo.PpEnabledExtensionNames = (byte**)extensions.ToPtr(out var extensionPtr);

            try
            {
                DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
                if (enableValidationLayers)
                {
                    createInfo.EnabledLayerCount = (uint)validationLayers.Length;
                    createInfo.PpEnabledLayerNames = (byte**)validationLayers.ToPtr(out var layerPtr);

                    PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
                    createInfo.PNext = &debugCreateInfo;
                    
                    Result result = context.vk.CreateInstance(in createInfo, null, out context.instance);
                    layerPtr.Free();
                    
                    if (result != Result.Success)
                        throw new Exception($"Failed to create Vulkan instance: {result}");
                }
                else
                {
                    Result result = context.vk.CreateInstance(in createInfo, null, out context.instance);
                    if (result != Result.Success)
                        throw new Exception($"Failed to create Vulkan instance: {result}");
                }
            }
            finally
            {
                extensionPtr.Free();
            }
        }
    }

    private bool CheckValidationLayerSupport()
    {
        uint layerCount = 0;
        context.vk.EnumerateInstanceLayerProperties(ref layerCount, null);

        if (layerCount == 0)
            return false;

        var availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* pAvailableLayers = availableLayers)
        context.vk.EnumerateInstanceLayerProperties(ref layerCount, pAvailableLayers);

        foreach (var layerName in validationLayers) 
        {
            bool layerFound = false;

            foreach (var layerProperties in availableLayers) 
            {
                string? availableLayerName = ((nint)layerProperties.LayerName).ToStr();
                if (layerName == availableLayerName) {
                    layerFound = true;
                    break;
                }
            }

            if (!layerFound) {
                return false;
            }
        }

        return true;
    }

    private string[] GetRequiredExtensions()
    {
        var glfw = Silk.NET.GLFW.GlfwProvider.GLFW.Value;
        var glfwExtensions = glfw.GetRequiredInstanceExtensions(out uint count);
        var extensions = ((nint)glfwExtensions).ToStrArray(count);

        if (enableValidationLayers)
            return [.. extensions, ExtDebugUtils.ExtensionName];

        return extensions;
    }
    #endregion

    #region DebugMessenger
    private void SetupDebugMessenger() 
    {
        if (!enableValidationLayers) return;

        if (!context.vk.TryGetInstanceExtension(context.instance, out context.debugUtils))
            throw new Exception("Failed to load VKcontext.EXTcontext.debugcontext.utils extension");

        DebugUtilsMessengerCreateInfoEXT createInfo = new();
        PopulateDebugMessengerCreateInfo(ref createInfo);

        if (context.debugUtils!.CreateDebugUtilsMessenger(context.instance, in createInfo, null, out context.debugMessenger) != Result.Success)
            throw new Exception("Failed to create debug messenger");
    }

    private uint DebugCallback(
        DebugUtilsMessageSeverityFlagsEXT messageSeverity,
        DebugUtilsMessageTypeFlagsEXT messageType,
        DebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* pUserData)
    {
        string message = Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage) ?? "";
    
        if (message.Contains("INFO") || message.Contains("loader")) 
            return Vk.False;

        Console.ForegroundColor = messageSeverity switch
        {
            DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt   => ConsoleColor.Red,
            DebugUtilsMessageSeverityFlagsEXT.WarningBitExt => ConsoleColor.Yellow,
            DebugUtilsMessageSeverityFlagsEXT.InfoBitExt    => ConsoleColor.Cyan,
            _                                                => ConsoleColor.White
        };

        Console.WriteLine($"\n[{messageSeverity}]");
        Console.WriteLine(message);
        Console.ResetColor();

        if (messageSeverity == DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt)
            System.Diagnostics.Debugger.Break();

        return Vk.False;
    }

    private void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo) 
    {
        createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
        createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt | DebugUtilsMessageSeverityFlagsEXT.WarningBitExt | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
        createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt;
        createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
    }
    #endregion

    #region PhysicalDevice
    private void PickPhysicalDevice()
    {
        uint deviceCount = 0;
        context.vk.EnumeratePhysicalDevices(context.instance, &deviceCount, null);

        if (deviceCount == 0) {
            throw new NotSupportedException("failed to find GPUs with Vulkan support!");
        }

        var devices = new PhysicalDevice[deviceCount];
        fixed (PhysicalDevice* pDevices = devices)
        context.vk.EnumeratePhysicalDevices(context.instance, &deviceCount, devices);

        foreach (var device in devices) {
            if (IsDeviceSuitable(device)) {
                context.physicalDevice = device;
                break;
            }
        }

        if (context.physicalDevice.Handle == 0) {
            throw new NotSupportedException("failed to find a suitable GPU!");
        }
    }

    private bool IsDeviceSuitable(PhysicalDevice device) 
    {
        QueueFamilyIndices indices = FindQueueFamilies(device);
        bool extensionsSupported = CheckDeviceExtensionSupport(device);
        bool swapChainAdequate = false;
        if (extensionsSupported) 
        {
            SwapChainSupportDetails swapChainSupport = QuerySwapChainSupport(device);
            swapChainAdequate = !swapChainSupport.Formats.Empty() && !swapChainSupport.PresentModes.Empty();
        }

        PhysicalDeviceFeatures supportedFeatures;
        context.vk.GetPhysicalDeviceFeatures(device, &supportedFeatures);
        
        return indices.IsComplete && extensionsSupported && swapChainAdequate && supportedFeatures.SamplerAnisotropy;
    }

    private bool CheckDeviceExtensionSupport(PhysicalDevice device)
    {
        uint extensionCount;
        context.vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &extensionCount, null);

        var availableExtensions = new ExtensionProperties[extensionCount];
        fixed (ExtensionProperties* pAvailableExtensions = availableExtensions)
        context.vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &extensionCount, pAvailableExtensions);

        var requiredExtensions = new HashSet<string>(deviceExtensions);

        foreach (var extension in availableExtensions) 
        {
            if (((nint)extension.ExtensionName).ToStr() is string value)
                requiredExtensions.Remove(value);
        }

        return requiredExtensions.Count == 0;
    }

    private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device) 
    {
        QueueFamilyIndices indices = new();

        uint queueFamilyCount = 0;
        context.vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);

        var queueFamilies = new QueueFamilyProperties[queueFamilyCount];
        fixed (QueueFamilyProperties* pQueueFamilies = queueFamilies)
        context.vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, pQueueFamilies);

        uint i = 0;
        foreach (var queueFamily in queueFamilies) 
        {
            if ((queueFamily.QueueFlags & QueueFlags.GraphicsBit) != 0) 
            {
                indices.GraphicsFamily = i;
            }

            Bool32 presentSupport = false;
            context.khrSurface.GetPhysicalDeviceSurfaceSupport(device, i, context.surface, out presentSupport);

            if (presentSupport)
            {
                indices.PresentFamily = i;
            }

            if (indices.IsComplete)
            {
                break;
            }

            i++;
        }

        return indices;
    }
    #endregion

    #region Logical Device
    private void CreateLogicalDevice()
    {
        QueueFamilyIndices indices = FindQueueFamilies(context.physicalDevice);

        HashSet<uint> uniqueQueueFamilies = [ indices.GraphicsFamily!.Value, indices.PresentFamily!.Value ];
        var queueCreateInfos = new DeviceQueueCreateInfo[uniqueQueueFamilies.Count];

        float queuePriority = 1.0f;
        int i = 0;
        foreach (var queueFamily in uniqueQueueFamilies) {
            DeviceQueueCreateInfo queueCreateInfo = new()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = queueFamily,
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };
            queueCreateInfos[i] = queueCreateInfo;
            i++;
        }

        PhysicalDeviceFeatures deviceFeatures = new()
        {
            SamplerAnisotropy = true
        };

        DeviceCreateInfo createInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = (uint)queueCreateInfos.Length,
            PEnabledFeatures = &deviceFeatures,
            EnabledExtensionCount = (uint)deviceExtensions.Length,
        };
        createInfo.PpEnabledExtensionNames = (byte**)deviceExtensions.ToPtr(out var pDeviceExtensions);

        try
        {
            
            fixed (DeviceQueueCreateInfo* pQueueCreateInfos = queueCreateInfos)
                createInfo.PQueueCreateInfos = pQueueCreateInfos;

            if (context.vk.CreateDevice(context.physicalDevice, &createInfo, null, out context.device) != Result.Success)
                throw new InvalidOperationException("failed to create logical device!");

            context.vk.GetDeviceQueue(context.device, indices.GraphicsFamily!.Value, 0, out context.graphicsQueue);
            context.vk.GetDeviceQueue(context.device, indices.PresentFamily!.Value, 0, out context.presentQueue);
        }
        finally
        {
            pDeviceExtensions.Free();
        }
    }
    #endregion

    #region Surface
    private void CreateSurface() 
    {
        if (context.window.VkSurface is null)
            throw new Exception("Windowing platform doesn't support Vulkan surface creation");
        
        context.surface = context.window.VkSurface.Create<AllocationCallbacks>(context.instance.ToHandle(), null).ToSurface();
        if (!context.vk.TryGetInstanceExtension(context.instance, out context.khrSurface))
            throw new Exception("Could not get KhrSurface extension");
    }
    #endregion

    #region Swap Chain
    private SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice device) 
    {
        SwapChainSupportDetails details = new();

        context.khrSurface.GetPhysicalDeviceSurfaceCapabilities(device, context.surface, out details.Capabilities);

        uint formatCount;
        context.khrSurface.GetPhysicalDeviceSurfaceFormats(device, context.surface, &formatCount, null);

        if (formatCount != 0) {
            details.Formats = new SurfaceFormatKHR[formatCount];
            fixed (SurfaceFormatKHR* pFormats = details.Formats)
            context.khrSurface.GetPhysicalDeviceSurfaceFormats(device, context.surface, &formatCount, pFormats);
        }

        uint presentModeCount;
        context.khrSurface.GetPhysicalDeviceSurfacePresentModes(device, context.surface, &presentModeCount, null);

        if (presentModeCount != 0) {
            details.PresentModes = new PresentModeKHR[presentModeCount];
            fixed (PresentModeKHR* pPresentModes = details.PresentModes)
            context.khrSurface.GetPhysicalDeviceSurfacePresentModes(device, context.surface, &presentModeCount, pPresentModes);
        }

        return details;
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
        foreach (var availablePresentMode in availablePresentModes) 
        {
            if (availablePresentMode == PresentModeKHR.ImmediateKhr) 
            {
                return availablePresentMode;
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
            Extent2D actualExtent = new((uint)context.Width, (uint)context.Height);

            actualExtent.Width = Mathf.Clampy(actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
            actualExtent.Height = Mathf.Clampy(actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

            return actualExtent;
        }
    }

    private void CreateSwapChain()
    {
        SwapChainSupportDetails swapChainSupport = QuerySwapChainSupport(context.physicalDevice);

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
            Surface = context.surface,
            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit
        };

        QueueFamilyIndices indices = FindQueueFamilies(context.physicalDevice);
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

        if (!context.vk.TryGetDeviceExtension(context.instance, context.device, out context.khrSwapchain))
            throw new Exception("Could not get KhrSwapchain extension");

        if (context.khrSwapchain.CreateSwapchain(context.device, &createInfo, null, out context.swapChain) != Result.Success)
            throw new InvalidOperationException("failed to create swap chain!");

        context.khrSwapchain.GetSwapchainImages(context.device, context.swapChain, &imageCount, null);
        context.swapChainImages = new Image[imageCount];
        fixed (Image* pSwapChainImages = context.swapChainImages)
        context.khrSwapchain.GetSwapchainImages(context.device, context.swapChain, &imageCount, pSwapChainImages);
        context.swapChainImageFormat = surfaceFormat.Format;
        context.swapChainExtent = extent;
    }

    private void CleanupSwapChain() 
    {
        context.vk.DestroyImageView(context.device, context.depthImageView, null);
        context.vk.DestroyImage(context.device, context.depthImage, null);
        context.vk.FreeMemory(context.device, context.depthImageMemory, null);
        
        foreach (var framebuffer in context.swapChainFramebuffers)
            context.vk.DestroyFramebuffer(context.device, framebuffer, null);
        
        foreach (var imageView in context.swapChainImageViews) 
            context.vk.DestroyImageView(context.device, imageView, null);
            
        context.khrSwapchain.DestroySwapchain(context.device, context.swapChain, null);
    }

    private void RecreateSwapChain() 
    {
        while (context.window.FramebufferSize.X == 0 || context.window.FramebufferSize.Y == 0)
        {
            context.window.DoEvents();
        }

        context.vk.DeviceWaitIdle(context.device);

        CleanupSwapChain();

        CreateSwapChain();
        CreateImageViews();
        CreateDepthResources();
        CreateFramebuffers();
    }
    #endregion

    #region Image Views
    private void CreateImageViews()
    {
        context.swapChainImageViews = new ImageView[context.swapChainImages.Length];

        for (int i = 0; i < context.swapChainImages.Length; i++) 
        {
            context.swapChainImageViews[i] = context.CreateImageView(context.swapChainImages[i], context.swapChainImageFormat, ImageAspectFlags.ColorBit, 1);
        }
    }
    #endregion

    #region Render Pass
    private static void CreateRenderPass(Format colorFormat, Format depthFormat, ImageLayout colorInitialLayout,ImageLayout colorFinalLayout, AttachmentLoadOp loadOp, out RenderPass renderPass)
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
            StoreOp            = AttachmentStoreOp.DontCare,
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

        if (GFX.Vk.CreateRenderPass(GFX.Device, &renderPassInfo, null, out renderPass) != Result.Success) {
            throw new InvalidOperationException("failed to create render pass!");
        }
    }
    #endregion

    #region Framebuffer
    private void CreateFramebuffers()
    {
        context.swapChainFramebuffers = new Framebuffer[context.swapChainImageViews.Length];

        for (int i = 0; i < context.swapChainImageViews.Length; i++) {
            ImageView[] attachments = [context.swapChainImageViews[i], context.depthImageView];

            FramebufferCreateInfo framebufferInfo = new()
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = context.renderPass,
                AttachmentCount = (uint)attachments.Length,
                Width = context.swapChainExtent.Width,
                Height = context.swapChainExtent.Height,
                Layers = 1
            };

            fixed (ImageView* pAttachments = attachments)
            framebufferInfo.PAttachments = pAttachments;

            if (context.vk.CreateFramebuffer(context.device, &framebufferInfo, null, out context.swapChainFramebuffers[i]) != Result.Success) {
                throw new InvalidOperationException("failed to create framebuffer!");
            }
        }
    }
    #endregion

    #region Command Pool
    private void CreateCommandPool()
    {
        QueueFamilyIndices queueFamilyIndices = FindQueueFamilies(context.physicalDevice);

        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
            QueueFamilyIndex = queueFamilyIndices.GraphicsFamily!.Value
        };

        if (context.vk.CreateCommandPool(context.device, &poolInfo, null, out context.commandPool) != Result.Success) {
            throw new InvalidOperationException("failed to create command pool!");
        }
    }

    private void CreateCommandBuffer() 
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = context.commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1
        };

        for (int i = 0; i < GraphicsContext.MAX_FRAMES_IN_FLIGHT; i++)
        if (context.vk.AllocateCommandBuffers(context.device, &allocInfo, out context.commandBuffers[i]) != Result.Success) {
            throw new InvalidOperationException("failed to allocate command buffers!");
        }
    }
    #endregion

    #region Depth
    private void CreateDepthResources()
    {
        Format depthFormat = context.FindDepthFormat();

        context.CreateImage(context.swapChainExtent.Width, context.swapChainExtent.Height, depthFormat, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, out context.depthImage, out context.depthImageMemory);
        context.depthImageView = context.CreateImageView(context.depthImage, depthFormat, ImageAspectFlags.DepthBit, 1);

        context.TransitionImageLayout(context.depthImage, depthFormat, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal);
    }
    
    #endregion

    #region Sync
    private void CreateSyncObjects()
    {
        context.imagesInFlight = new Fence[context.swapChainImages.Length];

        SemaphoreCreateInfo semaphoreInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo
        };

        FenceCreateInfo fenceInfo = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit
        };

        for (int i = 0; i < GraphicsContext.MAX_FRAMES_IN_FLIGHT; i++) 
        {
            if (context.vk.CreateSemaphore(context.device, &semaphoreInfo, null, out context.imageAvailableSemaphores[i]) != Result.Success ||
                context.vk.CreateSemaphore(context.device, &semaphoreInfo, null, out context.renderFinishedSemaphores[i]) != Result.Success ||
                context.vk.CreateFence(context.device, &fenceInfo, null, out context.inFlightFences[i]) != Result.Success) {
                throw new InvalidOperationException("failed to create semaphores!");
            }
        }
    }
    #endregion

    #region Vertex Buffer

    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector3 Position;
        public Vector2 Uv;
        //private Vector2 p1;

        public Vertex(Vector3 pos, Vector2 uv)
        {
            Position = pos;
            Uv = uv;
        }
    }

    #endregion


    private void OnRender(double deltaSeconds)
    {
        context.vk.WaitForFences(context.device, 1, ref context.inFlightFences[context.currentFrame], true, ulong.MaxValue);

        uint imageIndex;
        Result result = context.khrSwapchain.AcquireNextImage(context.device, context.swapChain, ulong.MaxValue, context.imageAvailableSemaphores[context.currentFrame], default, &imageIndex);
        if (result == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapChain();
            return;
        }
        else if (result != Result.Success && result != Result.SuboptimalKhr)
            throw new InvalidOperationException("failed to acquire swap chain image!");

        context.vk.ResetFences(context.device, 1, ref context.inFlightFences[context.currentFrame]);

        context.vk.ResetCommandBuffer(context.commandBuffers[context.currentFrame], 0);
        RecordCommandBuffer(context.commandBuffers[context.currentFrame], imageIndex);

        var waitStages = stackalloc PipelineStageFlags[] { PipelineStageFlags.ColorAttachmentOutputBit };

        var waitSemaphore = context.imageSemaphore;
        var commandBuffer = context.commandBuffer;
        var signalSemaphore = context.renderSemaphore;

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &waitSemaphore,
            PWaitDstStageMask = waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &signalSemaphore
        };

        if (context.vk.QueueSubmit(context.graphicsQueue, 1, &submitInfo, context.inFlightFences[context.currentFrame]) != Result.Success)
            throw new InvalidOperationException("failed to submit draw command buffer!");

        var swapChains = stackalloc SwapchainKHR[] { context.swapChain };
        PresentInfoKHR presentInfo = new()
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &signalSemaphore,
            SwapchainCount = 1,
            PSwapchains = swapChains,
            PImageIndices = &imageIndex,
            PResults = null
        };

        result = context.khrSwapchain.QueuePresent(context.presentQueue, &presentInfo);
        if (result == Result.ErrorOutOfDateKhr)
            RecreateSwapChain();
        else if (result != Result.Success && result != Result.SuboptimalKhr)
            throw new InvalidOperationException("failed to present swap chain image!");

        context.currentFrame = (context.currentFrame + 1) % GraphicsContext.MAX_FRAMES_IN_FLIGHT;
    }

    private void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex) 
    {
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = 0, // Optional
            PInheritanceInfo = null // Optional
        };

        if (context.vk.BeginCommandBuffer(commandBuffer, &beginInfo) != Result.Success) {
            throw new InvalidOperationException("failed to begin recording command buffer!");
        }

        context.currentFramebuffer = context.swapChainFramebuffers[imageIndex];

        RenderPassBeginInfo renderPassInfo = new()
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = context.renderPass,
            Framebuffer = context.swapChainFramebuffers[imageIndex]
        };
        renderPassInfo.RenderArea.Offset = new(0, 0);
        renderPassInfo.RenderArea.Extent = context.swapChainExtent;

        ClearValue[] clearValues = new ClearValue[2];
        clearValues[0].Color = new(0.0f, 0.0f, 0.0f, 1.0f);
        clearValues[1].DepthStencil = new(1.0f, 0);

        renderPassInfo.ClearValueCount = (uint)clearValues.Length;
        fixed (ClearValue* pClearValues = clearValues)
        renderPassInfo.PClearValues = pClearValues;

        context.vk.CmdBeginRenderPass(commandBuffer, &renderPassInfo, SubpassContents.Inline);

        Rect2D scissor = new()
        {
            Offset = new(0, 0),
            Extent = context.swapChainExtent
        };
        context.vk.CmdSetScissor(commandBuffer, 0, 1, &scissor);

        GFX.Viewport(0, 0, context.swapChainExtent.Width, context.swapChainExtent.Height);

        gameWindow.OnRender();

        context.vk.CmdEndRenderPass(commandBuffer);

        if (context.vk.EndCommandBuffer(commandBuffer) != Result.Success) {
            throw new InvalidOperationException("failed to record command buffer!");
        }
    }

    private void OnClosing()
    {
        Console.WriteLine("Window is closing...");

        context.vk.DeviceWaitIdle(context.device);

        BufferBase.DisposeAll();
        ShaderBuffer.Dispose();

        gameWindow.OnUnload();

        Dispose();
    }

    public void Dispose()
    {  
        CleanupSwapChain();

        context.vk.DestroyRenderPass(context.device, context.renderPass, null);
        context.vk.DestroyRenderPass(context.device, context.renderPassLoad, null);
        context.vk.DestroyRenderPass(context.device, context.framebufferRenderPass, null);
        context.vk.DestroyRenderPass(context.device, context.framebufferRenderPassLoad, null);
        
        for (int i = 0; i < GraphicsContext.MAX_FRAMES_IN_FLIGHT; i++) 
        {
            context.vk.DestroySemaphore(context.device, context.renderFinishedSemaphores[i], null);
            context.vk.DestroySemaphore(context.device, context.imageAvailableSemaphores[i], null);
            context.vk.DestroyFence(context.device, context.inFlightFences[i], null);
        }
        
        context.vk.DestroyCommandPool(context.device, context.commandPool, null);

        context.vk.DestroyDevice(context.device, null);
        if (enableValidationLayers)
            context.debugUtils!.DestroyDebugUtilsMessenger(context.instance, context.debugMessenger, null);

        context.khrSurface?.DestroySurface(context.instance, context.surface, null);
        context.vk.DestroyInstance(context.instance, null);

        ((nint)context.mainPtr).Free();
        
        context.window?.Dispose();
    }
}