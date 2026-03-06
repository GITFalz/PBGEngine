using System.Runtime.InteropServices;
using PBG.MathLibrary;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics;

public unsafe class Descriptor : BufferBase
{
    public readonly Guid ID = Guid.NewGuid();
    private Dictionary<Texture, uint> _boundTextures = [];
    private Dictionary<TextureArray, uint> _boundTextureArrays = [];
    private Dictionary<FBO, uint> _boundFramebuffers = [];
    
    private UniformBufferAttribute[] _uniformAttributes;
    private Buffer[] _uniformBuffers;
    private DeviceMemory[] _uniformBuffersMemory;
    private void*[] _uniformBuffersMapped;

    private DescriptorSet[] _descriptorSets;
    private DescriptorPool _descriptorPool;

    private PipelineLayout _pipelineLayout;

    private ImageMemoryBarrier[] _imageBarriers = [];
    private BufferMemoryBarrier[] _bufferBarriers = [];

    public Descriptor(PipelineLayout pipelineLayout, DescriptorPool descriptorPool, DescriptorSet[] descriptorSets, UniformBufferLayout[] uniformBindings, UniformBufferAttribute[] uniformAttributes)
    {
        _descriptorSets = descriptorSets;
        _descriptorPool = descriptorPool;
        _uniformAttributes = uniformAttributes;
        _pipelineLayout = pipelineLayout;

        _uniformBuffers = new Buffer[GraphicsContext.MAX_FRAMES_IN_FLIGHT * uniformBindings.Length];
        _uniformBuffersMemory = new DeviceMemory[GraphicsContext.MAX_FRAMES_IN_FLIGHT * uniformBindings.Length];
        _uniformBuffersMapped = new void*[GraphicsContext.MAX_FRAMES_IN_FLIGHT * uniformBindings.Length];

        for (int i = 0; i < uniformBindings.Length; i++)
        {
            var size = uniformBindings[i].Size;
            for (int j = 0; j < GraphicsContext.MAX_FRAMES_IN_FLIGHT; j++) 
            {
                int a = i * GraphicsContext.MAX_FRAMES_IN_FLIGHT + j;
                GFX.CreateBuffer(size, BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out _uniformBuffers[a], out _uniformBuffersMemory[a]);
                GFX.MapMemory(_uniformBuffersMemory[a], 0, size, 0, ref _uniformBuffersMapped[a]);
            }
        }

        for (int i = 0; i < uniformBindings.Length; i++)
        {
            var layout = uniformBindings[i];
            for (int j = 0; j < GraphicsContext.MAX_FRAMES_IN_FLIGHT; j++) 
            {
                DescriptorBufferInfo bufferInfo = new()
                {
                    Buffer = _uniformBuffers[j],
                    Offset = 0,
                    Range = layout.Size
                };

                WriteDescriptorSet descriptorWrite = new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = descriptorSets[j],
                    DstBinding = layout.LayoutBinding.Binding,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    PBufferInfo = &bufferInfo,
                    PImageInfo = null, // Optional
                    PTexelBufferView = null // Optional
                };

                GFX.UpdateDescriptorSets(1, &descriptorWrite, 0, null);
            }
        }
    }

    public void Uniform1(int location, float value) => Uniform(location, value);
    public void Uniform2(int location, Vector2 value) => Uniform(location, value);
    public void Uniform3(int location, Vector3 value) => Uniform(location, value);
    public void Uniform4(int location, Vector4 value) => Uniform(location, value);
    public void UniformMatrix4(int location, System.Numerics.Matrix4x4 value) => Uniform(location, value);
    public void UniformMatrix4(int location, Matrix4 value) => Uniform(location, value);
    
    public void Uniform<T>(int location, T value) where T : unmanaged
    {
        if (location >= 0 && location < _uniformAttributes.Length)
        {
            var attribute = _uniformAttributes[location];
            var bufferPtr = (byte*)_uniformBuffersMapped[attribute.Index * GraphicsContext.MAX_FRAMES_IN_FLIGHT + GFX.CurrentFrame];
            var dest = bufferPtr + attribute.Offset;
            *(T*)dest = value;
        }
        else
        {
            Console.WriteLine("Couldn't find uniform at location " + location);
        }
    }

    public void UniformArray<T>(int location, T[] values) where T : unmanaged
    {
        if (location < 0 || location >= _uniformAttributes.Length || values == null)
            return;

        var attr = _uniformAttributes[location];
        var bufferPtr = (byte*)_uniformBuffersMapped[attr.Index * GraphicsContext.MAX_FRAMES_IN_FLIGHT + GFX.CurrentFrame];
        var dest = bufferPtr + attr.Offset;
        HelperFunctions.MemCpyTo(values, dest, values.Length * Marshal.SizeOf<T>(), values.Length * Marshal.SizeOf<T>());
    }

    public void BindSSBO<T>(SSBO<T> ssbo, uint binding) where T : unmanaged
    {
        _bufferBarriers = [
            .._bufferBarriers, 
            ssbo.GetMemoryBarrier()
        ];

        for (int j = 0; j < GraphicsContext.MAX_FRAMES_IN_FLIGHT; j++) 
        {
            DescriptorBufferInfo bufferInfo = new()
            {
                Buffer = ssbo.Buffer,
                Offset = 0,
                Range = ssbo.Size
            };

            WriteDescriptorSet descriptorWrite = new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = _descriptorSets[j],
                DstBinding = binding,
                DstArrayElement = 0,
                DescriptorType = DescriptorType.StorageBuffer,
                DescriptorCount = 1,
                PBufferInfo = &bufferInfo,
                PImageInfo = null, // Optional
                PTexelBufferView = null // Optional
            };

            GFX.UpdateDescriptorSets(1, &descriptorWrite, 0, null);
        }
    }

    public void BindIDBO<T>(IDBO<T> idbo, uint binding) where T : unmanaged
    {
        _bufferBarriers = [
            .._bufferBarriers, 
            idbo.GetMemoryBarrier()
        ];

        for (int j = 0; j < GraphicsContext.MAX_FRAMES_IN_FLIGHT; j++) 
        {
            DescriptorBufferInfo bufferInfo = new()
            {
                Buffer = idbo.Buffer,
                Offset = 0,
                Range = idbo.Size
            };

            WriteDescriptorSet descriptorWrite = new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = _descriptorSets[j],
                DstBinding = binding,
                DstArrayElement = 0,
                DescriptorType = DescriptorType.StorageBuffer,
                DescriptorCount = 1,
                PBufferInfo = &bufferInfo,
                PImageInfo = null, // Optional
                PTexelBufferView = null // Optional
            };

            GFX.UpdateDescriptorSets(1, &descriptorWrite, 0, null);
        }
    }

    private void BindSampler(ImageView imageView, Sampler sampler, DescriptorType descriptorType, ImageLayout imageLayout, uint binding)
    {
        for (int j = 0; j < GraphicsContext.MAX_FRAMES_IN_FLIGHT; j++) 
        {
            DescriptorImageInfo imageInfo = new()
            {
                ImageLayout = imageLayout,
                ImageView = imageView,
                Sampler = sampler
            };

            WriteDescriptorSet descriptorWrite = new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = _descriptorSets[j],
                DstBinding = binding,
                DstArrayElement = 0,
                DescriptorType = descriptorType,
                DescriptorCount = 1,
                PBufferInfo = null,
                PImageInfo = &imageInfo, // Optional
                PTexelBufferView = null // Optional
            };

            GFX.UpdateDescriptorSets(1, &descriptorWrite, 0, null);
        }
    }

    public void BindTexture(Texture texture, uint binding)
    {
        
        if (_boundTextures.TryAdd(texture, binding))
        {
            _imageBarriers = [
                .._imageBarriers, 
                texture.GetMemoryBarrier()
            ];

            BindSampler(texture.textureImageView, texture.textureSampler, 
                texture.IsStorageImage ? DescriptorType.StorageImage : DescriptorType.CombinedImageSampler, 
                texture.IsStorageImage ? ImageLayout.General : ImageLayout.ShaderReadOnlyOptimal,
                binding);
        }
    }

    public void BindTextureArray(TextureArray texture, uint binding)
    {
        if (_boundTextureArrays.TryAdd(texture, binding))
        {
            _imageBarriers = [
                .._imageBarriers, 
                texture.GetMemoryBarrier()
            ];

            BindSampler(texture.textureImageView, texture.textureSampler, DescriptorType.CombinedImageSampler, ImageLayout.ShaderReadOnlyOptimal, binding); 
        }
    }

    public void BindFramebuffer(FBO framebuffer, uint binding)
    {
        if (_boundFramebuffers.TryAdd(framebuffer, binding))
            BindSampler(framebuffer.colorView, framebuffer.sampler, DescriptorType.CombinedImageSampler, ImageLayout.ShaderReadOnlyOptimal, binding);
    }

    public override void Resize(uint width, uint height)
    {
        foreach (var (texture, binding) in _boundTextures)
            BindSampler(texture.textureImageView, texture.textureSampler, 
                texture.IsStorageImage ? DescriptorType.StorageImage : DescriptorType.CombinedImageSampler, 
                texture.IsStorageImage ? ImageLayout.General : ImageLayout.ShaderReadOnlyOptimal,
                binding);

        foreach (var (texture, binding) in _boundTextureArrays)
            BindSampler(texture.textureImageView, texture.textureSampler, DescriptorType.CombinedImageSampler, ImageLayout.ShaderReadOnlyOptimal, binding);

        foreach (var (framebuffer, binding) in _boundFramebuffers)
            BindSampler(framebuffer.colorView, framebuffer.sampler, DescriptorType.CombinedImageSampler, ImageLayout.ShaderReadOnlyOptimal, binding);
    }

    public void Bind()
    {
        GFX.Vk.CmdBindDescriptorSets(GFX.CommandBuffer, PipelineBindPoint.Graphics, _pipelineLayout, 0, 1, ref _descriptorSets[GraphicsContext.graphicsContext.currentFrame], 0, null);
    }

    public void Bind(CommandBuffer commandBuffer, PipelineBindPoint pipelineBindPoint)
    {
        GFX.Vk.CmdBindDescriptorSets(commandBuffer, pipelineBindPoint, _pipelineLayout, 0, 1, ref _descriptorSets[GraphicsContext.graphicsContext.currentFrame], 0, null);
    }

    public ImageMemoryBarrier[] GetImageBarriers() => _imageBarriers;
    public BufferMemoryBarrier[] GetBufferBarriers() => _bufferBarriers;
    
    protected override void Destroy()
    {
        for (int i = 0; i < _uniformBuffers.Length; i++) 
        {
            GFX.DestroyBuffer(_uniformBuffers[i]);
            GFX.FreeMemory(_uniformBuffersMemory[i]);
        }

        fixed (DescriptorSet* pDescriptorSets = _descriptorSets)
        GFX.FreeDescriptorSets(_descriptorPool, (uint)_descriptorSets.Length, pDescriptorSets);
    }
}