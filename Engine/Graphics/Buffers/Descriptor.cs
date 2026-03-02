using PBG.MathLibrary;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics;

public unsafe class Descriptor : BufferBase
{
    private Dictionary<Texture, uint> _boundTextures = [];
    private Dictionary<TextureArray, uint> _boundTextureArrays = [];
    private Dictionary<FBO, uint> _boundFramebuffers = [];
    
    private UniformBufferAttribute[] uniformAttributes;
    private Buffer[] uniformBuffers;
    private DeviceMemory[] uniformBuffersMemory;
    private void*[] uniformBuffersMapped;

    private DescriptorSet[] descriptorSets;

    private Shader _shader;

    public Descriptor(Shader shader, DescriptorSet[] descriptorSets, UniformBufferLayout[] uniformBindings, UniformBufferAttribute[] uniformAttributes)
    {
        //Console.WriteLine("Descriptor: " + shader._shaderInfo.VertexShaderPath + " " + descriptorSets.Length + " " + uniformBindings.Length + " " + uniformAttributes.Length);
        this.descriptorSets = descriptorSets;
        this.uniformAttributes = uniformAttributes;
        _shader = shader;

        uniformBuffers = new Buffer[GraphicsContext.MAX_FRAMES_IN_FLIGHT * uniformBindings.Length];
        uniformBuffersMemory = new DeviceMemory[GraphicsContext.MAX_FRAMES_IN_FLIGHT * uniformBindings.Length];
        uniformBuffersMapped = new void*[GraphicsContext.MAX_FRAMES_IN_FLIGHT * uniformBindings.Length];

        for (int i = 0; i < uniformBindings.Length; i++)
        {
            var size = uniformBindings[i].Size;
            for (int j = 0; j < GraphicsContext.MAX_FRAMES_IN_FLIGHT; j++) 
            {
                int a = i * GraphicsContext.MAX_FRAMES_IN_FLIGHT + j;
                GFX.CreateBuffer(size, BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out uniformBuffers[a], out uniformBuffersMemory[a]);
                GFX.MapMemory(uniformBuffersMemory[a], 0, size, 0, ref uniformBuffersMapped[a]);
            }
        }

        for (int i = 0; i < uniformBindings.Length; i++)
        {
            var layout = uniformBindings[i];
            for (int j = 0; j < GraphicsContext.MAX_FRAMES_IN_FLIGHT; j++) 
            {
                DescriptorBufferInfo bufferInfo = new()
                {
                    Buffer = uniformBuffers[j],
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
        if (location >= 0 && location < uniformAttributes.Length)
        {
            var attribute = uniformAttributes[location];
            var bufferPtr = (byte*)uniformBuffersMapped[attribute.Index * GraphicsContext.MAX_FRAMES_IN_FLIGHT + GFX.CurrentFrame];
            var dest = bufferPtr + attribute.Offset;
            *(T*)dest = value;
        }
        else
        {
            Console.WriteLine("Couldn't find uniform at location " + location);
        }
    }


    public void BindSSBO<T>(SSBO<T> ssbo, uint binding) where T : unmanaged
    {
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
                DstSet = descriptorSets[j],
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

    private void BindSampler(ImageView imageView, Sampler sampler, uint binding)
    {
        for (int j = 0; j < GraphicsContext.MAX_FRAMES_IN_FLIGHT; j++) 
        {
            DescriptorImageInfo imageInfo = new()
            {
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                ImageView = imageView,
                Sampler = sampler
            };

            WriteDescriptorSet descriptorWrite = new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = descriptorSets[j],
                DstBinding = binding,
                DstArrayElement = 0,
                DescriptorType = DescriptorType.CombinedImageSampler,
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
            BindSampler(texture.textureImageView, texture.textureSampler, binding);
    }

    public void BindTextureArray(TextureArray texture, uint binding)
    {
        if (_boundTextureArrays.TryAdd(texture, binding))
            BindSampler(texture.textureImageView, texture.textureSampler, binding); 
    }

    public void BindFramebuffer(FBO framebuffer, uint binding)
    {
        if (_boundFramebuffers.TryAdd(framebuffer, binding))
            BindSampler(framebuffer.colorView, framebuffer.sampler, binding);
    }

    public override void Resize(uint width, uint height)
    {
        foreach (var (texture, binding) in _boundTextures)
            BindSampler(texture.textureImageView, texture.textureSampler, binding);

        foreach (var (texture, binding) in _boundTextureArrays)
            BindSampler(texture.textureImageView, texture.textureSampler, binding);

        foreach (var (framebuffer, binding) in _boundFramebuffers)
            BindSampler(framebuffer.colorView, framebuffer.sampler, binding);
    }

    public void Bind()
    {
        GFX.Vk.CmdBindDescriptorSets(GFX.CommandBuffer, PipelineBindPoint.Graphics, _shader.pipelineLayout, 0, 1, ref descriptorSets[GraphicsContext.graphicsContext.currentFrame], 0, null);
    }
    
    protected override void Destroy()
    {
        for (int i = 0; i < uniformBuffers.Length; i++) 
        {
            GFX.DestroyBuffer(uniformBuffers[i]);
            GFX.FreeMemory(uniformBuffersMemory[i]);
        }
    }
}