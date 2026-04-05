using System.Runtime.InteropServices;
using PBG.Graphics.Vulkan;
using PBG.Mathematics;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics;

public unsafe class Descriptor : BufferBase, IResizeable
{
    public readonly Guid ID = Guid.NewGuid();

    private Dictionary<Texture, uint> _boundTextures = [];
    private Dictionary<TextureArray, uint> _boundTextureArrays = [];
    private Dictionary<FBO, (uint? colorBinding, uint? depthBinding)> _boundFramebuffers = [];
    
    private UniformBufferAttribute[] _uniformAttributes;
    private Buffer[] _uniformBuffers;
    private DeviceMemory[] _uniformBuffersMemory;
    private void*[] _uniformBuffersMapped;

    private DescriptorSet[] _descriptorSets;
    private DescriptorPool _descriptorPool;

    private PipelineLayout _pipelineLayout;

    private Dictionary<ImageBuffer, (int index, DescriptorType type, ImageLayout layout)> _imageIndices = [];
    private Dictionary<int, ImageBuffer> _indexImages = [];
    private ImageMemoryBarrier[] _imageBarriers = [];

    private Dictionary<GPUBufferBase, int> _bufferIndices = [];
    private Dictionary<int, GPUBufferBase> _indexBuffers = [];
    private BufferMemoryBarrier[] _bufferBarriers = [];

    private IShader _shader;

    public Descriptor(IShader shader, PipelineLayout pipelineLayout, DescriptorPool descriptorPool, DescriptorSet[] descriptorSets, UniformBufferLayout[] uniformBindings, UniformBufferAttribute[] uniformAttributes)
    {
        _shader = shader;
        _descriptorSets = descriptorSets;
        _descriptorPool = descriptorPool;
        _uniformAttributes = uniformAttributes;
        _pipelineLayout = pipelineLayout;

        _uniformBuffers = new Buffer[GFX.MAX_FRAMES_IN_FLIGHT * uniformBindings.Length];
        _uniformBuffersMemory = new DeviceMemory[GFX.MAX_FRAMES_IN_FLIGHT * uniformBindings.Length];
        _uniformBuffersMapped = new void*[GFX.MAX_FRAMES_IN_FLIGHT * uniformBindings.Length];

        for (int i = 0; i < uniformBindings.Length; i++)
        {
            var size = uniformBindings[i].Size;
            for (int j = 0; j < GFX.MAX_FRAMES_IN_FLIGHT; j++) 
            {
                int a = i * GFX.MAX_FRAMES_IN_FLIGHT + j;
                GFX.CreateBuffer(size, BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out _uniformBuffers[a], out _uniformBuffersMemory[a]);
                GFX.MapMemory(_uniformBuffersMemory[a], 0, size, 0, ref _uniformBuffersMapped[a]);
            }
        }

        for (int i = 0; i < uniformBindings.Length; i++)
        {
            var layout = uniformBindings[i];
            for (int j = 0; j < GFX.MAX_FRAMES_IN_FLIGHT; j++) 
            {
                DescriptorBufferInfo bufferInfo = new()
                {
                    Buffer = _uniformBuffers[i * GFX.MAX_FRAMES_IN_FLIGHT + j],
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
            var bufferPtr = (byte*)_uniformBuffersMapped[attribute.Index * GFX.MAX_FRAMES_IN_FLIGHT + GFX.CurrentFrame];
            var dest = bufferPtr + attribute.Offset;
            *(T*)dest = value;
        }
        #if DEBUG
        else
        {
            throw new Exception("Couldn't find uniform at location " + location);
        }
        #endif   
    }

    public void UniformArray<T>(int location, T[] values) where T : unmanaged
    {
        if (location < 0 || location >= _uniformAttributes.Length || values == null)
            return;

        var attr = _uniformAttributes[location];
        var bufferPtr = (byte*)_uniformBuffersMapped[attr.Index * GFX.MAX_FRAMES_IN_FLIGHT + GFX.CurrentFrame];
        var dest = bufferPtr + attr.Offset;
        HelperFunctions.MemCpyTo(values, dest, values.Length * Marshal.SizeOf<T>(), values.Length * Marshal.SizeOf<T>());
    }

    private void DisposeBuffer(GPUBufferBase buffer)
    {
        if (!_bufferIndices.TryGetValue(buffer, out int index))
            return;

        int lastIdx = _bufferBarriers.Length - 1;

        if (index != lastIdx)
        {
            var lastBuffer = _indexBuffers[lastIdx];

            _bufferBarriers[index] = _bufferBarriers[lastIdx];
            _bufferIndices[lastBuffer] = index;
            _indexBuffers[index] = lastBuffer;
        }

        _bufferBarriers = _bufferBarriers[..lastIdx];
        _bufferIndices.Remove(buffer);
        _indexBuffers.Remove(lastIdx);
    }

    private void DisposeImage(ImageBuffer image)
    {
        if (!_imageIndices.TryGetValue(image, out var data))
            return;

        int lastIdx = _imageBarriers.Length - 1;

        if (data.index != lastIdx)
        {
            var lastImage = _indexImages[lastIdx];

            _imageBarriers[data.index] = _imageBarriers[lastIdx];
            _imageIndices[lastImage] = (data.index, data.type, data.layout);
            _indexImages[data.index] = lastImage;
        }

        _imageBarriers = _imageBarriers[..lastIdx];
        _imageIndices.Remove(image);
        _indexImages.Remove(lastIdx);
    }

    public void BindBuffer(GPUBufferBase buffer, uint binding)
    {
        if (_bufferIndices.TryGetValue(buffer, out var index))
        {
            _bufferBarriers[index] = buffer.GetMemoryBarrier();
        }
        else
        {
            buffer.OnDispose += _ => DisposeBuffer(buffer);
            _bufferIndices.Add(buffer, _bufferBarriers.Length);
            _indexBuffers.Add(_bufferBarriers.Length, buffer);
            _bufferBarriers = [.._bufferBarriers, buffer.GetMemoryBarrier()];
        }

        for (int j = 0; j < GFX.MAX_FRAMES_IN_FLIGHT; j++) 
        {
            DescriptorBufferInfo bufferInfo = new()
            {
                Buffer = buffer.Buffer,
                Offset = 0,
                Range = buffer.SizeInBytes
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
                PImageInfo = null,
                PTexelBufferView = null
            };

            GFX.UpdateDescriptorSets(1, &descriptorWrite, 0, null);
        }
    }

    public void BindSSBO<T>(SSBO<T> ssbo, uint binding) where T : unmanaged => BindBuffer(ssbo, binding);
    public void BindIDBO<T>(IDBO<T> idbo, uint binding) where T : unmanaged => BindBuffer(idbo, binding);

    private void BindSampler(ImageView imageView, Sampler sampler, DescriptorType descriptorType, ImageLayout imageLayout, uint binding)
    {
        for (int j = 0; j < GFX.MAX_FRAMES_IN_FLIGHT; j++) 
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

    public void BindTexture(Texture texture, uint binding) => BindTexture(texture, binding, 
        texture.IsStorageImage ? DescriptorType.StorageImage : DescriptorType.CombinedImageSampler, 
        texture.IsStorageImage ? ImageLayout.General : ImageLayout.ShaderReadOnlyOptimal);
    public void BindTexture(Texture texture, uint binding, DescriptorType type, ImageLayout layout)
    {
        if (_imageIndices.TryGetValue(texture, out var data))
        {
            _imageBarriers[data.index] = texture.GetMemoryBarrier();
            type = data.type;
            layout = data.layout;
        }
        else
        {
            texture.OnDispose += _ =>
            {
                DisposeImage(texture);
                _boundTextures.Remove(texture);
            };
            _boundTextures.Add(texture, binding);
            _imageIndices.Add(texture, (_imageBarriers.Length, type, layout));
            _indexImages.Add(_imageBarriers.Length, texture);
            _imageBarriers = [.._imageBarriers, texture.GetMemoryBarrier()];
        }

        BindSampler(texture.ImageView, texture.Sampler, type, layout, binding);
    }

    public void BindTextureArray(TextureArray texture, uint binding) => BindTextureArray(texture, binding, DescriptorType.CombinedImageSampler, ImageLayout.ShaderReadOnlyOptimal);
    public void BindTextureArray(TextureArray textureArray, uint binding, DescriptorType type, ImageLayout layout)
    {
        if (_imageIndices.TryGetValue(textureArray, out var data))
        {
            _imageBarriers[data.index] = textureArray.GetMemoryBarrier();
            type = data.type;
            layout = data.layout;
        }
        else
        {
            textureArray.OnDispose += _ =>
            {
                DisposeImage(textureArray);
                _boundTextureArrays.Remove(textureArray);
            };
            _boundTextureArrays.Add(textureArray, binding);
            _imageIndices.Add(textureArray, (_imageBarriers.Length, type, layout));
            _indexImages.Add(_imageBarriers.Length, textureArray);
            _imageBarriers = [.._imageBarriers, textureArray.GetMemoryBarrier()];
        }

        BindSampler(textureArray.ImageView, textureArray.Sampler, type, layout, binding); 
    }

    public void BindFramebufferColor(FBO framebuffer, uint binding)
    {
        if (_boundFramebuffers.TryGetValue(framebuffer, out var bindings))
        {
            bindings.colorBinding = binding;
            _boundFramebuffers[framebuffer] = bindings;
        }
        else
        {
            _boundFramebuffers.Add(framebuffer, (binding, null));
            framebuffer.OnDispose = _ => _boundFramebuffers.Remove(framebuffer);
        }

        BindSampler(framebuffer.colorView, framebuffer.sampler, DescriptorType.CombinedImageSampler, ImageLayout.ShaderReadOnlyOptimal, binding);
    }

    public void BindFramebufferDepth(FBO framebuffer, uint binding)
    {
        if (_boundFramebuffers.TryGetValue(framebuffer, out var bindings))
        {
            bindings.depthBinding = binding;
            _boundFramebuffers[framebuffer] = bindings;
        }
        else
        {
            _boundFramebuffers.Add(framebuffer, (null, binding));
            framebuffer.OnDispose = _ => _boundFramebuffers.Remove(framebuffer);
        }

        BindSampler(framebuffer.depthView, framebuffer.sampler, DescriptorType.CombinedImageSampler, ImageLayout.ShaderReadOnlyOptimal, binding);
    }

    public void UnbindTexture(Texture texture)
    {
        DisposeImage(texture);
        _boundTextures.Remove(texture);
    }
    public void UnbindTextureArray(TextureArray texture)
    {
        DisposeImage(texture);
        _boundTextureArrays.Remove(texture);
    }
    public void UnbindFramebuffer(FBO framebuffer)
    {
        _boundFramebuffers.Remove(framebuffer);
    }


    public void Resize(uint width, uint height)
    {
        foreach (var (texture, binding) in _boundTextures)
            BindTexture(texture, binding);

        foreach (var (texture, binding) in _boundTextureArrays)
            BindTextureArray(texture, binding);

        foreach (var (framebuffer, bindings) in _boundFramebuffers)
        {
            if (bindings.colorBinding != null) BindFramebufferColor(framebuffer, bindings.colorBinding.Value);
            if (bindings.depthBinding != null) BindFramebufferDepth(framebuffer, bindings.depthBinding.Value);
        }
    }

    public void Bind()
    {
        GFX.Vk.CmdBindDescriptorSets(GFX.CommandBuffer, PipelineBindPoint.Graphics, _pipelineLayout, 0, 1, ref _descriptorSets[VoxelEngine.Instance.Renderer.CurrentFrame], 0, null);
    }

    public void Bind(CommandBuffer commandBuffer) => Bind(commandBuffer, PipelineBindPoint.Graphics);
    public void Bind(CommandBuffer commandBuffer, PipelineBindPoint pipelineBindPoint)
    {
        GFX.Vk.CmdBindDescriptorSets(commandBuffer, pipelineBindPoint, _pipelineLayout, 0, 1, ref _descriptorSets[VoxelEngine.Instance.Renderer.CurrentFrame], 0, null);
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