using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace PBG.Graphics.Vulkan;

public sealed unsafe class VulkanSyncObject : IDisposable
{
    private readonly VulkanDevice _vulkanDevice;
    private readonly VulkanSwapchain _vulkanSwapchain;

    public Semaphore[] ImageAvailableSemaphores = new Semaphore[GFX.MAX_FRAMES_IN_FLIGHT];
    public Semaphore[] RenderFinishedSemaphores = new Semaphore[GFX.MAX_FRAMES_IN_FLIGHT];
    public Fence[] InFlightFences = new Fence[GFX.MAX_FRAMES_IN_FLIGHT];
    public Fence[] ImagesInFlight = [];

    public VulkanSyncObject(VulkanDevice vulkanDevice, VulkanSwapchain vulkanSwapchain)
    {
        _vulkanDevice = vulkanDevice;
        _vulkanSwapchain = vulkanSwapchain;

        CreateSyncObjects();
    }

    private void CreateSyncObjects()
    {
        ImagesInFlight = new Fence[_vulkanSwapchain.SwapChainImages.Length];

        SemaphoreCreateInfo semaphoreInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo
        };

        FenceCreateInfo fenceInfo = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit
        };

        for (int i = 0; i < GFX.MAX_FRAMES_IN_FLIGHT; i++) 
        {
            if (_vulkanDevice.Vk.CreateSemaphore(_vulkanDevice.Device, &semaphoreInfo, null, out ImageAvailableSemaphores[i]) != Result.Success ||
                _vulkanDevice.Vk.CreateSemaphore(_vulkanDevice.Device, &semaphoreInfo, null, out RenderFinishedSemaphores[i]) != Result.Success ||
                _vulkanDevice.Vk.CreateFence(_vulkanDevice.Device, &fenceInfo, null, out InFlightFences[i]) != Result.Success) {
                throw new InvalidOperationException("failed to create semaphores!");
            }
        }
    }

    public void Dispose()
    {
        for (int i = 0; i < GFX.MAX_FRAMES_IN_FLIGHT; i++) 
        {
            _vulkanDevice.Vk.DestroySemaphore(_vulkanDevice.Device, RenderFinishedSemaphores[i], null);
            _vulkanDevice.Vk.DestroySemaphore(_vulkanDevice.Device, ImageAvailableSemaphores[i], null);
            _vulkanDevice.Vk.DestroyFence(_vulkanDevice.Device, InFlightFences[i], null);
        }
    }
}