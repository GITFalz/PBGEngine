using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics;

public unsafe class GFX
{
    private static GraphicsContext _graphicsContext = null!;
    public static uint CurrentFrame => _graphicsContext.currentFrame;
    
    public static Vk Vk => _graphicsContext.vk;
    public static Device Device => _graphicsContext.device;
    public static RenderPass RenderPass => _graphicsContext.renderPass;
    public static CommandBuffer CommandBuffer => _graphicsContext.commandBuffer;

    public static Extent2D SwapChainExtent => GraphicsContext.graphicsContext.swapChainExtent;
    public static Format SwapChainFormat => GraphicsContext.graphicsContext.swapChainImageFormat;

    private static (int x, int y, uint width, uint height) _viewport;

    public GFX(GraphicsContext graphicsContext)
    {
        _graphicsContext ??= graphicsContext;
    }
       

    #region Physical device
    public static void GetPhysicalDeviceProperties(PhysicalDeviceProperties* pProperties)
    => Vk.GetPhysicalDeviceProperties(_graphicsContext.physicalDevice, pProperties);
    
    public static void GetPhysicalDeviceProperties(out PhysicalDeviceProperties pProperties)
    => Vk.GetPhysicalDeviceProperties(_graphicsContext.physicalDevice, out pProperties);
    #endregion
    
    #region Pipeline
    public static Result CreatePipelineLayout(PipelineLayoutCreateInfo* pCreateInfo, AllocationCallbacks* pAllocator, out PipelineLayout pPipelineLayout)
    => Vk.CreatePipelineLayout(Device, pCreateInfo, pAllocator, out pPipelineLayout);
    
    public static Result CreateDescriptorSetLayout(DescriptorSetLayoutCreateInfo* pCreateInfo, AllocationCallbacks* pAllocator, out DescriptorSetLayout pSetLayout)
    => Vk.CreateDescriptorSetLayout(Device, pCreateInfo, pAllocator, out pSetLayout);

    public static Result CreateGraphicsPipelines(PipelineCache pipelineCache, uint createInfoCount, GraphicsPipelineCreateInfo* pCreateInfos, AllocationCallbacks* pAllocator, out Pipeline pPipelines)
    => Vk.CreateGraphicsPipelines(Device, pipelineCache, createInfoCount, pCreateInfos, pAllocator, out pPipelines);

    public static Result CreateShaderModule(ShaderModuleCreateInfo* pCreateInfo, AllocationCallbacks* pAllocator, out ShaderModule pShaderModule)
    => Vk.CreateShaderModule(Device, pCreateInfo, pAllocator, out pShaderModule);

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
    => _graphicsContext.CreateImage(width, height, format, tiling, usage, properties, out image, out imageMemory);
    
    public static void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout)
    => _graphicsContext.TransitionImageLayout(image, format, oldLayout, newLayout);
    
    public static void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height)
    => _graphicsContext.CopyBufferToImage(buffer, image, width, height);

    public static void CopyImageToBuffer(Image image, Buffer buffer, uint width, uint height)
    => _graphicsContext.CopyImageToBuffer(image, buffer, width, height);
    
    public static void CreateImageArray(uint width, uint height, uint layerCount, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, out Image image, out DeviceMemory imageMemory)
    => _graphicsContext.CreateImageArray(width, height, layerCount, format, tiling, usage, properties, out image, out imageMemory);
    
    public static void TransitionImageArrayLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout, uint layerCount)
    => _graphicsContext.TransitionImageArrayLayout(image, format, oldLayout, newLayout, layerCount);
    
    public static void CopyBufferToImageArray(Buffer buffer, Image image, uint width, uint height, uint layerCount)
    => _graphicsContext.CopyBufferToImageArray(buffer, image, width, height, layerCount);
    
    public static ImageView CreateImageView(Image image, Format format, ImageAspectFlags aspectFlags, uint layerCount = 1)
    => _graphicsContext.CreateImageView(image, format, aspectFlags, layerCount);
    
    public static Result CreateFramebuffer(FramebufferCreateInfo* pCreateInfo, AllocationCallbacks* pAllocator, out Silk.NET.Vulkan.Framebuffer pFramebuffer)
    => Vk.CreateFramebuffer(Device, pCreateInfo, pAllocator, out pFramebuffer);

    public static Result CreateSampler(SamplerCreateInfo* pCreateInfo, AllocationCallbacks* pAllocator, out Silk.NET.Vulkan.Sampler pSampler)
    => Vk.CreateSampler(Device, pCreateInfo, pAllocator, out pSampler);
    #endregion

    public static void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, out Buffer buffer, out DeviceMemory bufferMemory)
    => _graphicsContext.CreateBuffer(size, usage, properties, out buffer, out bufferMemory);
    
    public static void CreateBuffer<T>(T[] array, BufferUsageFlags bufferType, MemoryPropertyFlags properties, out Buffer buffer, out DeviceMemory bufferMemory) where T : unmanaged
    => _graphicsContext.CreateBuffer(array, bufferType, properties, out buffer, out bufferMemory);
        
    public static void CreateBuffer<T>(BufferUsageFlags bufferType, T[] array, out Buffer buffer, out DeviceMemory bufferMemory) where T : unmanaged
    => _graphicsContext.CreateBuffer(bufferType, array, out buffer, out bufferMemory);
    
    public static void UpdateBuffer<T>(T[] array, Buffer buffer) where T : unmanaged
    => _graphicsContext.UpdateBuffer(array, buffer);

    public static void UpdateBuffer<T>(T[] array, Buffer buffer, ulong offsetBytes, ulong sizeBytes) where T : unmanaged
    => _graphicsContext.UpdateBuffer(array, buffer, offsetBytes, sizeBytes);

    public static void UpdateBufferRange<T>(T[] array, Buffer buffer, ulong offsetBytes, ulong sizeBytes) where T : unmanaged
    => _graphicsContext.UpdateBufferFull(array, buffer, offsetBytes, sizeBytes);
    

    #region Clean up
    // === Destroy ===
    public static void DestroyBuffer(Buffer buffer)
    => Vk.DestroyBuffer(Device, buffer, null);

    public static void DestroyFramebuffer(Silk.NET.Vulkan.Framebuffer framebuffer)
    => Vk.DestroyFramebuffer(Device, framebuffer, null);

    public static void DestroySampler(Silk.NET.Vulkan.Sampler sampler)
    => Vk.DestroySampler(Device, sampler, null);
    
    public static void DestroyImageView(ImageView imageView)
    => Vk.DestroyImageView(Device, imageView, null);
    
    public static void DestroyImage(Image image)
    => Vk.DestroyImage(Device, image, null);
       
    public static void FreeMemory(DeviceMemory deviceMemory)
    => Vk.FreeMemory(Device, deviceMemory, null);  

    public static void DestroyShaderModule(ShaderModule module)
    => Vk.DestroyShaderModule(Device, module, null);

    public static void DestroyPipeline(Pipeline pipeline)
    => Vk.DestroyPipeline(Device, pipeline, null);

    public static void DestroyPipelineLayout(PipelineLayout pipelineLayout)
    => Vk.DestroyPipelineLayout(Device, pipelineLayout, null);
    
    public static void FreeDescriptorSets(DescriptorPool descriptorPool, uint descriptorSetCount, DescriptorSet* pDescriptorSets)
    => Vk.FreeDescriptorSets(Device, descriptorPool, descriptorSetCount, pDescriptorSets);

    public static void DestroyDescriptorSetLayout(DescriptorSetLayout descriptorSetLayout)
    => Vk.DestroyDescriptorSetLayout(Device, descriptorSetLayout, null);
    #endregion


    #region Rendering
    public static void Viewport(int x, int y, int width, int height)
    => Viewport(x, y, (uint)width, (uint)height);

    public static void Viewport(int x, int y, uint width, uint height)
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
        Vk.CmdSetViewport(_graphicsContext.commandBuffer, 0, 1, &viewport);
    }

    public static (int x, int y, uint width, uint height) GetViewport() => _viewport;


    public static void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance)
    => Vk.CmdDraw(CommandBuffer, vertexCount, instanceCount, firstVertex, firstInstance);

    public static void DrawIndexed(uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
    => Vk.CmdDrawIndexed(CommandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
    #endregion

}
public enum BufferType
{
    VertexBuffer,
    StorageBuffer
}
