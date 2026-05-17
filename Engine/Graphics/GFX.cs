using PBG.Graphics.Vulkan;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics;

public unsafe class GFX
{
    public const int MAX_FRAMES_IN_FLIGHT = 2;
    internal static int RenderCallCount = 0;
    public static uint CurrentFrame => _renderer.CurrentFrame;
    
    public static Vk Vk => _vulkanDevice.Vk;
    public static Device Device => _vulkanDevice.Device;

    public static CommandPool CommandPool => _vulkanCommandBuffers.CommandPool;
    public static CommandBuffer CommandBuffer => _vulkanCommandBuffers.CommandBuffers[CurrentFrame];

    public static Extent2D SwapChainExtent => _vulkanSwapchain.SwapChainExtent;
    public static Format SwapChainFormat => _vulkanSwapchain.SwapChainImageFormat;

    private static VulkanInstance _renderer = null!;

    private static VulkanDevice _vulkanDevice = null!;
    private static IWindow _window = null!;

    private static VulkanSwapchain _vulkanSwapchain = null!;

    private static VulkanImage _vulkanImage = null!;
    private static VulkanBuffer _vulkanBuffer = null!;

    private static VulkanImageViews _vulkanImageViews = null!;
    private static VulkanCommandBuffers _vulkanCommandBuffers = null!;
    private static VulkanDepthBuffer _vulkanDepthBuffer = null!;
    private static VulkanFramebuffer _vulkanFramebuffer = null!;
    private static VulkanSyncObject _vulkanSyncObject = null!;

    private static (int x, int y, uint width, uint height) _viewport;

    public GFX(
        VulkanInstance renderer,
        VulkanDevice vulkanDevice,
        IWindow window,
        VulkanSwapchain vulkanSwapchain, 
        VulkanImage vulkanImage, 
        VulkanBuffer vulkanBuffer, 
        VulkanImageViews vulkanImageViews, 
        VulkanCommandBuffers vulkanCommandBuffers,
        VulkanDepthBuffer vulkanDepthBuffer,
        VulkanFramebuffer vulkanFramebuffer,
        VulkanSyncObject vulkanSyncObject
    ) {
        _renderer = renderer;

        _vulkanDevice = vulkanDevice;
        _window = window;
        
        _vulkanSwapchain = vulkanSwapchain;
        _vulkanImage = vulkanImage;
        _vulkanBuffer = vulkanBuffer;
        _vulkanImageViews = vulkanImageViews;
        _vulkanCommandBuffers = vulkanCommandBuffers;
        _vulkanDepthBuffer = vulkanDepthBuffer;
        _vulkanFramebuffer = vulkanFramebuffer;
        _vulkanSyncObject = vulkanSyncObject;
    }

    #region Device
    public static void DeviceWaitIdle() => Vk.DeviceWaitIdle(Device);
    #endregion
       

    #region Physical device
    public static void GetPhysicalDeviceProperties(PhysicalDeviceProperties* pProperties)
    => Vk.GetPhysicalDeviceProperties(_vulkanDevice.PhysicalDevice, pProperties);
    
    public static void GetPhysicalDeviceProperties(out PhysicalDeviceProperties pProperties)
    => Vk.GetPhysicalDeviceProperties(_vulkanDevice.PhysicalDevice, out pProperties);
    #endregion
    
    #region Pipeline
    public static Result CreatePipelineLayout(PipelineLayoutCreateInfo* pCreateInfo, AllocationCallbacks* pAllocator, out PipelineLayout pPipelineLayout)
    {
        var result = Vk.CreatePipelineLayout(Device, pCreateInfo, pAllocator, out pPipelineLayout);
        #if DEBUG
        BufferBase.SetDebug(pPipelineLayout);
        #endif
        return result;
    }
    
    public static Result CreateDescriptorSetLayout(DescriptorSetLayoutCreateInfo* pCreateInfo, AllocationCallbacks* pAllocator, out DescriptorSetLayout pSetLayout)
    {
        var result = Vk.CreateDescriptorSetLayout(Device, pCreateInfo, pAllocator, out pSetLayout);
        #if DEBUG
        BufferBase.SetDebug(pSetLayout);
        #endif
        return result;
    }

    public static Result CreateDescriptorPool(DescriptorPoolCreateInfo* pCreateInfo, out DescriptorPool pDescriptorPool)
    {
        var result = Vk.CreateDescriptorPool(Device, pCreateInfo, null, out pDescriptorPool);
        #if DEBUG
        BufferBase.SetDebug(pDescriptorPool);
        #endif
        return result;
    }
    
    public static Result AllocateDescriptorSets(DescriptorSetAllocateInfo* allocInfo, DescriptorSet[] descriptorSets)
    {
        Result result;
        fixed(DescriptorSet* pDescriptorSets = descriptorSets)
        result = Vk.AllocateDescriptorSets(Device, allocInfo, pDescriptorSets);
        #if DEBUG
        BufferBase.SetDebug(descriptorSets);
        #endif
        return result;
    }

    public static Result CreateGraphicsPipelines(PipelineCache pipelineCache, uint createInfoCount, GraphicsPipelineCreateInfo* pCreateInfos, AllocationCallbacks* pAllocator, out Pipeline pPipelines)
    {
        var result = Vk.CreateGraphicsPipelines(Device, pipelineCache, createInfoCount, pCreateInfos, pAllocator, out pPipelines);
        #if DEBUG
        BufferBase.SetDebug(pPipelines);
        #endif
        return result;
    }

    public static Result CreateShaderModule(ShaderModuleCreateInfo* pCreateInfo, AllocationCallbacks* pAllocator, out ShaderModule pShaderModule)
    {
        var result = Vk.CreateShaderModule(Device, pCreateInfo, pAllocator, out pShaderModule);
        #if DEBUG
        BufferBase.SetDebug(pShaderModule);
        #endif
        return result;
    }

    public static void UpdateDescriptorSets(uint descriptorWriteCount, WriteDescriptorSet* pDescriptorWrites, uint descriptorCopyCount, CopyDescriptorSet* pDescriptorCopies)
    => Vk.UpdateDescriptorSets(Device, descriptorWriteCount, pDescriptorWrites, descriptorCopyCount, pDescriptorCopies);
    #endregion

    #region Memory
    public static Result MapMemory(DeviceMemory memory, ulong offset, ulong size, MemoryMapFlags flags, void** ppData)
    => Vk.MapMemory(Device, memory, offset, size, flags, ppData);
    
    public static Result MapMemory(DeviceMemory memory, ulong offset, ulong size, MemoryMapFlags flags, ref void* pData)
    => Vk.MapMemory(Device, memory, offset, size, flags, ref pData);
    
    public static void UnmapMemory(DeviceMemory deviceMemory)
    => Vk.UnmapMemory(Device, deviceMemory);
    #endregion
    
    #region Image
    public static void CreateImage(uint width, uint height, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, out Image image, out DeviceMemory imageMemory)
    {
        _vulkanImage.CreateImage(width, height, format, tiling, usage, properties, out image, out imageMemory);
        #if DEBUG
        BufferBase.SetDebug(image);
        BufferBase.SetDebug(imageMemory);
        #endif
    }
    
    public static void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout)
    => _vulkanImage.TransitionImageLayout(image, format, oldLayout, newLayout);
    
    public static void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height)
    => _vulkanImage.CopyBufferToImage(buffer, image, width, height);

    public static void CopyImageToBuffer(Image image, Buffer buffer, uint width, uint height)
    => _vulkanImage.CopyImageToBuffer(image, buffer, width, height);
    
    public static void CreateImageArray(uint width, uint height, uint layerCount, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, out Image image, out DeviceMemory imageMemory)
    {  
        _vulkanImage.CreateImageArray(width, height, layerCount, format, tiling, usage, properties, out image, out imageMemory);
        #if DEBUG
        BufferBase.SetDebug(image);
        BufferBase.SetDebug(imageMemory);
        #endif
    }
    
    public static void TransitionImageArrayLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout, uint layerCount)
    => _vulkanImage.TransitionImageArrayLayout(image, format, oldLayout, newLayout, layerCount);
    
    public static void CopyBufferToImageArray(Buffer buffer, Image image, uint width, uint height, uint layerCount)
    => _vulkanImage.CopyBufferToImageArray(buffer, image, width, height, layerCount);
    
    public static ImageView CreateImageView(Image image, Format format, ImageAspectFlags aspectFlags, uint layerCount = 1)
    {  
        var imageView = _vulkanImage.CreateImageView(image, format, aspectFlags, layerCount);
        #if DEBUG
        BufferBase.SetDebug(imageView);
        #endif
        return imageView;
    }
    
    public static Result CreateFramebuffer(FramebufferCreateInfo* pCreateInfo, AllocationCallbacks* pAllocator, out Silk.NET.Vulkan.Framebuffer pFramebuffer)
    {  
        var result = Vk.CreateFramebuffer(Device, pCreateInfo, pAllocator, out pFramebuffer);
        #if DEBUG
        BufferBase.SetDebug(pFramebuffer);
        #endif
        return result;
    }

    public static Result CreateSampler(SamplerCreateInfo* pCreateInfo, AllocationCallbacks* pAllocator, out Silk.NET.Vulkan.Sampler pSampler)
    {  
        var result = Vk.CreateSampler(Device, pCreateInfo, pAllocator, out pSampler);
        #if DEBUG
        BufferBase.SetDebug(pSampler);
        #endif
        return result;
    }
    #endregion

    public static void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, out Buffer buffer, out DeviceMemory bufferMemory)
    {  
        _vulkanBuffer.CreateBuffer(size, usage, properties, out buffer, out bufferMemory);
        #if DEBUG
        BufferBase.SetDebug(buffer);
        BufferBase.SetDebug(bufferMemory);
        #endif
    }
    
    public static void CreateBuffer<T>(T[] array, BufferUsageFlags bufferType, MemoryPropertyFlags properties, out Buffer buffer, out DeviceMemory bufferMemory) where T : unmanaged
    {  
        _vulkanBuffer.CreateBuffer(array, bufferType, properties, out buffer, out bufferMemory);
        #if DEBUG
        BufferBase.SetDebug(buffer);
        BufferBase.SetDebug(bufferMemory);
        #endif
    }
    
    public static void UpdateBuffer<T>(T[] array, Buffer buffer) where T : unmanaged
    => _vulkanBuffer.UpdateBuffer(array, buffer);

    public static void UpdateBuffer<T>(T[] array, Buffer buffer, ulong offsetBytes, ulong sizeBytes) where T : unmanaged
    => _vulkanBuffer.UpdateBuffer(array, buffer, offsetBytes, sizeBytes);

    public static void UpdateBufferRange<T>(T[] array, Buffer buffer, ulong offsetBytes, ulong sizeBytes) where T : unmanaged
    => _vulkanBuffer.UpdateBufferRange(array, buffer, offsetBytes, sizeBytes);
    

    #region Clean up
    // === Destroy ===
    public static void DestroyBuffer(Buffer buffer)
    { 
        Vk.DestroyBuffer(Device, buffer, null);
        #if DEBUG
        BufferBase.RemoveDebug(buffer);
        #endif
    }

    public static void DestroyFramebuffer(Silk.NET.Vulkan.Framebuffer framebuffer)
    { 
        Vk.DestroyFramebuffer(Device, framebuffer, null);
        #if DEBUG
        BufferBase.RemoveDebug(framebuffer);
        #endif
    }

    public static void DestroySampler(Silk.NET.Vulkan.Sampler sampler)
    { 
        Vk.DestroySampler(Device, sampler, null);
        #if DEBUG
        BufferBase.RemoveDebug(sampler);
        #endif
    }
    
    public static void DestroyImageView(ImageView imageView)
    { 
        Vk.DestroyImageView(Device, imageView, null);
        #if DEBUG
        BufferBase.RemoveDebug(imageView);
        #endif
    }
    
    public static void DestroyImage(Image image)
    { 
        Vk.DestroyImage(Device, image, null);
        #if DEBUG
        BufferBase.RemoveDebug(image);
        #endif
    }
       
    public static void FreeMemory(DeviceMemory deviceMemory)
    { 
        Vk.FreeMemory(Device, deviceMemory, null);  
        #if DEBUG
        BufferBase.RemoveDebug(deviceMemory);
        #endif
    }

    public static void DestroyShaderModule(ShaderModule module)
    { 
        Vk.DestroyShaderModule(Device, module, null);
        #if DEBUG
        BufferBase.RemoveDebug(module);
        #endif
    }

    public static void DestroyPipeline(Pipeline pipeline)
    { 
        Vk.DestroyPipeline(Device, pipeline, null);
        #if DEBUG
        BufferBase.RemoveDebug(pipeline);
        #endif
    }

    public static void DestroyPipelineLayout(PipelineLayout pipelineLayout)
    { 
        Vk.DestroyPipelineLayout(Device, pipelineLayout, null);
        #if DEBUG
        BufferBase.RemoveDebug(pipelineLayout);
        #endif
    }
    
    public static void FreeDescriptorSets(DescriptorPool descriptorPool, uint descriptorSetCount, DescriptorSet* pDescriptorSets)
    { 
        Vk.FreeDescriptorSets(Device, descriptorPool, descriptorSetCount, pDescriptorSets);
        #if DEBUG
        BufferBase.RemoveDebug(descriptorSetCount, pDescriptorSets);
        #endif
    }

    public static void FreeDescriptorSets(DescriptorPool descriptorPool, DescriptorSet[] descriptorSets)
    { 
        Vk.FreeDescriptorSets(Device, descriptorPool, (uint)descriptorSets.Length, descriptorSets);
        #if DEBUG
        BufferBase.RemoveDebug(descriptorSets);
        #endif
    }

    public static void DestroyDescriptorSetLayout(DescriptorSetLayout descriptorSetLayout)
    { 
        Vk.DestroyDescriptorSetLayout(Device, descriptorSetLayout, null);
        #if DEBUG
        BufferBase.RemoveDebug(descriptorSetLayout);
        #endif
    }
     
    public static void DestroyDescriptorPool(DescriptorPool descriptorPool)
    {
        Vk.DestroyDescriptorPool(Device, descriptorPool, null);
        #if DEBUG
        BufferBase.RemoveDebug(descriptorPool);
        #endif
    }
    #endregion


    #region Command buffer
    public static CommandBuffer BeginSingleTimeCommands() => _vulkanDevice.BeginSingleTimeCommands();
    public static void EndSingleTimeCommands(CommandBuffer commandBuffer) => _vulkanDevice.EndSingleTimeCommands(commandBuffer);
    #endregion


    #region Rendering
    /// <summary>
    /// Default Viewport that is the size of the screen
    /// </summary>
    public static void Viewport() => Viewport(CommandBuffer, 0, 0, (uint)Game.Width, (uint)Game.Height);
    public static void Viewport(int x, int y, int width, int height) => Viewport(CommandBuffer, x, y, (uint)width, (uint)height);
    public static void Viewport(int x, int y, uint width, uint height) => Viewport(CommandBuffer, x, y, width, height);
    public static void Viewport(CommandBuffer commandBuffer, int x, int y, uint width, uint height)
    {
        _viewport = (x, y, width, height);
        Viewport viewport = new()
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        Vk.CmdSetViewport(commandBuffer, 0, 1, &viewport);
    }

    public static (int x, int y, uint width, uint height) GetViewport() => _viewport;

    
    public static void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance)
    {
        Vk.CmdDraw(CommandBuffer, vertexCount, instanceCount, firstVertex, firstInstance);
        RenderCallCount++;
    }

    public static void Draw(CommandBuffer commandBuffer, uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance)
    {
        Vk.CmdDraw(commandBuffer, vertexCount, instanceCount, firstVertex, firstInstance);
        RenderCallCount++;
    }

    public static void DrawIndexed(uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
    {
        Vk.CmdDrawIndexed(CommandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
        RenderCallCount++;
    }

    public static void DrawIndexed(CommandBuffer commandBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
    {
        Vk.CmdDrawIndexed(commandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
        RenderCallCount++;
    }

    public static void DrawIndirect(Buffer buffer, ulong offset, uint drawCount, uint stride)
    {
        Vk.CmdDrawIndirect(CommandBuffer, buffer, offset, drawCount, stride);
        RenderCallCount++;
    }

    public static void DrawIndirect(CommandBuffer commandBuffer, Buffer buffer, ulong offset, uint drawCount, uint stride)
    {
        Vk.CmdDrawIndirect(commandBuffer, buffer, offset, drawCount, stride);
        RenderCallCount++;
    }

    public static void DrawIndirectCount(Buffer buffer, ulong offset, Buffer countBuffer, ulong countOffset, uint maxDrawCount, uint stride)
    {
        Vk.CmdDrawIndirectCount(CommandBuffer, buffer, offset, countBuffer, countOffset, maxDrawCount, stride);
        RenderCallCount++;
    }

    public static void DrawIndirectCount(CommandBuffer commandBuffer, Buffer buffer, ulong offset, Buffer countBuffer, ulong countOffset, uint maxDrawCount, uint stride)
    {
        Vk.CmdDrawIndirectCount(commandBuffer, buffer, offset, countBuffer, countOffset, maxDrawCount, stride);
        RenderCallCount++;
    }
    #endregion

}
public enum BufferType
{
    VertexBuffer,
    StorageBuffer
}
