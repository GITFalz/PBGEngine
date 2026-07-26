using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Silk.NET.Core;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace PBG.Graphics.Vulkan;

public unsafe sealed partial class VulkanDevice
{
    private void CreateInstance()
    {
        if (_enableValidation && !Check_validationLayersupport()) {
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
                if (_enableValidation)
                {
                    createInfo.EnabledLayerCount = (uint)_validationLayers.Length;
                    createInfo.PpEnabledLayerNames = (byte**)_validationLayers.ToPtr(out var layerPtr);

                    PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
                    createInfo.PNext = &debugCreateInfo;
                    
                    Result result = Vk.CreateInstance(in createInfo, null, out var instance);
                    Instance = instance;
                    layerPtr.Free();
                    
                    if (result != Result.Success)
                        throw new Exception($"Failed to create Vulkan instance: {result}");
                }
                else
                {
                    Result result = Vk.CreateInstance(in createInfo, null, out var instance);
                    Instance = instance;
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

    private bool Check_validationLayersupport()
    {
        uint layerCount = 0;
        Vk.EnumerateInstanceLayerProperties(ref layerCount, null);

        if (layerCount == 0)
            return false;

        var availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* pAvailableLayers = availableLayers)
        Vk.EnumerateInstanceLayerProperties(ref layerCount, pAvailableLayers);

        foreach (var layerName in _validationLayers) 
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

        if (_enableValidation)
            return [.. extensions, ExtDebugUtils.ExtensionName];

        return extensions;
    }

    private void SetupDebugMessenger() 
    {
        if (!_enableValidation) return;

        if (!Vk.TryGetInstanceExtension(Instance, out _debugUtils))
            throw new Exception("Failed to load VK_EXT_debug_utils extension");

        DebugUtilsMessengerCreateInfoEXT createInfo = new();
        PopulateDebugMessengerCreateInfo(ref createInfo);

        if (_debugUtils!.CreateDebugUtilsMessenger(Instance, in createInfo, null, out _debugMessenger) != Result.Success)
            throw new Exception("Failed to create debug messenger");
    }

    private uint DebugCallback(
        DebugUtilsMessageSeverityFlagsEXT messageSeverity,
        DebugUtilsMessageTypeFlagsEXT messageType,
        DebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* pUserData)
    {
        string message = Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage) ?? "";
        if (message.Contains("INFO") || message.Contains("loader") || message.Contains("VkQueue"))
            return Vk.False;

        Console.ForegroundColor = messageSeverity switch
        {
            DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt   => ConsoleColor.Red,
            DebugUtilsMessageSeverityFlagsEXT.WarningBitExt => ConsoleColor.Yellow,
            DebugUtilsMessageSeverityFlagsEXT.InfoBitExt    => ConsoleColor.Cyan,
            _                                               => ConsoleColor.White
        };

        Console.WriteLine($"\n[{messageSeverity}]");
        Console.WriteLine(message);

        #if DEBUG
        // Vulkan messages contain handles like 0x000001A2B3C4D5E6
        foreach (Match match in Regex.Matches(message, @"0x([0-9a-fA-F]+)"))
        {
            if (ulong.TryParse(match.Groups[1].Value, NumberStyles.HexNumber, null, out ulong handle))
            {
                if (BufferBase.TryGetTrace(handle, out string? traceInfo))
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"\n  [DEBUG TRACE] {traceInfo}");
                    Console.ResetColor();
                }
            }
        }
        #endif

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

    private void PickPhysicalDevice()
    {
        uint deviceCount = 0;
        Vk.EnumeratePhysicalDevices(Instance, &deviceCount, null);

        if (deviceCount == 0) {
            throw new NotSupportedException("failed to find GPUs with Vulkan support!");
        }

        var devices = new PhysicalDevice[deviceCount];
        fixed (PhysicalDevice* pDevices = devices)
        Vk.EnumeratePhysicalDevices(Instance, &deviceCount, devices);

        foreach (var device in devices) {
            if (IsDeviceSuitable(device)) {
                PhysicalDevice = device;
                break;
            }
        }

        if (PhysicalDevice.Handle == 0) {
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
            swapChainAdequate = swapChainSupport.Formats.Length != 0 && swapChainSupport.PresentModes.Length != 0;
        }

        PhysicalDeviceFeatures supportedFeatures;
        Vk.GetPhysicalDeviceFeatures(device, &supportedFeatures);
        
        return indices.IsComplete && extensionsSupported && swapChainAdequate && supportedFeatures.SamplerAnisotropy;
    }

    public SwapChainSupportDetails QuerySwapChainSupport() => QuerySwapChainSupport(PhysicalDevice);
    private SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice device) 
    {
        SwapChainSupportDetails details = new();

        KhrSurface.GetPhysicalDeviceSurfaceCapabilities(device, Surface, out details.Capabilities);
        
        uint formatCount;
        KhrSurface.GetPhysicalDeviceSurfaceFormats(device, Surface, &formatCount, null);

        if (formatCount != 0) {
            details.Formats = new SurfaceFormatKHR[formatCount];
            fixed (SurfaceFormatKHR* pFormats = details.Formats)
            KhrSurface.GetPhysicalDeviceSurfaceFormats(device, Surface, &formatCount, pFormats);
        }

        uint presentModeCount;
        KhrSurface.GetPhysicalDeviceSurfacePresentModes(device, Surface, &presentModeCount, null);

        if (presentModeCount != 0) {
            details.PresentModes = new PresentModeKHR[presentModeCount];
            fixed (PresentModeKHR* pPresentModes = details.PresentModes)
            KhrSurface.GetPhysicalDeviceSurfacePresentModes(device, Surface, &presentModeCount, pPresentModes);
        }

        return details;
    }

    private bool CheckDeviceExtensionSupport(PhysicalDevice device)
    {
        uint extensionCount;
        Vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &extensionCount, null);

        var availableExtensions = new ExtensionProperties[extensionCount];
        fixed (ExtensionProperties* pAvailableExtensions = availableExtensions)
        Vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &extensionCount, pAvailableExtensions);

        var requiredExtensions = new HashSet<string>(_deviceExtensions);

        foreach (var extension in availableExtensions) 
        {
            if (((nint)extension.ExtensionName).ToStr() is string value)
                requiredExtensions.Remove(value);
        }

        return requiredExtensions.Count == 0;
    }

    public QueueFamilyIndices FindQueueFamilies() => FindQueueFamilies(PhysicalDevice);
    private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device) 
    {
        QueueFamilyIndices indices = new();

        uint queueFamilyCount = 0;
        Vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);

        var queueFamilies = new QueueFamilyProperties[queueFamilyCount];
        fixed (QueueFamilyProperties* pQueueFamilies = queueFamilies)
        Vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, pQueueFamilies);

        uint i = 0;
        foreach (var queueFamily in queueFamilies) 
        {
            if ((queueFamily.QueueFlags & QueueFlags.GraphicsBit) != 0) 
            {
                indices.GraphicsFamily = i;
            }

            KhrSurface.GetPhysicalDeviceSurfaceSupport(device, i, Surface, out Bool32 presentSupport);

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


    private void CreateLogicalDevice()
    {
        PhysicalDeviceFeatures supportedFeatures;
        Vk.GetPhysicalDeviceFeatures(PhysicalDevice, &supportedFeatures);

        if (!supportedFeatures.MultiDrawIndirect)
            throw new Exception("GPU does not support MultiDrawIndirect!");
            
        QueueFamilyIndices indices = FindQueueFamilies(PhysicalDevice);

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

        var vulkan11Features = new PhysicalDeviceVulkan11Features
        {
            SType                = StructureType.PhysicalDeviceVulkan11Features,
            ShaderDrawParameters = true
        };

        var vulkan12Features = new PhysicalDeviceVulkan12Features
        {
            SType             = StructureType.PhysicalDeviceVulkan12Features,
            DrawIndirectCount = true,
            PNext             = &vulkan11Features   // chain 1.1 features after 1.2 features
        };

        var deviceFeatures2 = new PhysicalDeviceFeatures2
        {
            SType = StructureType.PhysicalDeviceFeatures2,
            PNext = &vulkan12Features
        };

        PhysicalDeviceFeatures deviceFeatures = new()
        {
            SamplerAnisotropy = true,
            MultiDrawIndirect  = true
        };

        DeviceCreateInfo createInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = (uint)queueCreateInfos.Length,
            PEnabledFeatures = &deviceFeatures,
            EnabledExtensionCount = (uint)_deviceExtensions.Length,
            PNext = &deviceFeatures2,
            PpEnabledExtensionNames = (byte**)_deviceExtensions.ToPtr(out var pDeviceExtensions)
        };

        try
        {
            fixed (DeviceQueueCreateInfo* pQueueCreateInfos = queueCreateInfos)
                createInfo.PQueueCreateInfos = pQueueCreateInfos;

            if (Vk.CreateDevice(PhysicalDevice, &createInfo, null, out var device) != Result.Success)
                throw new InvalidOperationException("failed to create logical device!");

            Device = device;

            Vk.GetDeviceQueue(Device, indices.GraphicsFamily!.Value, 0, out GraphicsQueue);
            Vk.GetDeviceQueue(Device, indices.PresentFamily!.Value, 0, out PresentQueue);
        }
        finally
        {
            pDeviceExtensions.Free();
        }
    }

    private void CreateSurface() 
    {
        if (_window.VkSurface is null)
            throw new Exception("Windowing platform doesn't support Vulkan surface creation");
        
        Surface = _window.VkSurface.Create<AllocationCallbacks>(Instance.ToHandle(), null).ToSurface();
        if (!Vk.TryGetInstanceExtension(Instance, out KhrSurface))
            throw new Exception("Could not get KhrSurface extension");
            
    }
}