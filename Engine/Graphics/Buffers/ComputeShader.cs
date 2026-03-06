using System.Runtime.InteropServices;
using PBG.MathLibrary;
using Silk.NET.Shaderc;
using Silk.NET.Vulkan;
using static ShaderCompiler;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics;

public struct ComputeShaderInfo 
{
    public string ComputeShaderPath = "";
    public ComputeShaderInfo(string path)
    {
        ComputeShaderPath = path;
    }
}

public unsafe class ComputeShader : BufferBase
{
    private UniformBufferLayout[] _uniformBindings = [];

    public DescriptorSetLayout descriptorSetLayout;

    public PipelineLayout pipelineLayout;
    public Pipeline pipeline;

    public ComputeShaderInfo _shaderInfo;

    public ComputeShader(ComputeShaderInfo info)
    {
        _shaderInfo = info;
    }

    private Dictionary<string, int> _locations = [];
    private UniformBufferAttribute[] _uniformAttribues = [];

    public int GetLocation(string name)
    {
        if (_locations.TryGetValue(name, out var index))
            return index;

        Console.WriteLine("[Warning] : Unknown location: " + name);
        return -1;
    }


    private CommandBuffer _commandBuffer;
    public void Bind(out CommandBuffer commandBuffer)
    {
        commandBuffer = GraphicsContext.graphicsContext.BeginSingleTimeCommands();
        GFX.Vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Compute, pipeline);
        _commandBuffer = commandBuffer;
    }

    public void Dispatch(uint groupsX, uint groupsY, uint groupsZ)
    {
        GFX.Vk.CmdDispatch(_commandBuffer, groupsX, groupsY, groupsZ);
        GraphicsContext.graphicsContext.EndSingleTimeCommands(_commandBuffer);
    }

    public void DispatchBarrier(Descriptor descriptor, uint groupsX, uint groupsY, uint groupsZ)
    {
        var imageBarriers = descriptor.GetImageBarriers();
        var bufferBarriers = descriptor.GetBufferBarriers();

        GFX.Vk.CmdDispatch(_commandBuffer, groupsX, groupsY, groupsZ);
        
        fixed (BufferMemoryBarrier* pBufferBarrier = bufferBarriers)
        fixed (ImageMemoryBarrier* pImageBarrier = imageBarriers)
        GFX.Vk.CmdPipelineBarrier(_commandBuffer, PipelineStageFlags.ComputeShaderBit, PipelineStageFlags.VertexShaderBit, DependencyFlags.None, 0, null, 
            (uint)bufferBarriers.Length, pBufferBarrier, (uint)imageBarriers.Length, pImageBarrier);
            
        GraphicsContext.graphicsContext.EndSingleTimeCommands(_commandBuffer);
    }
    
    public void Compile()
    {
        ShaderData computeData = GraphicsContext.graphicsContext.shaderCompiler.CompileAndReflect(_shaderInfo.ComputeShaderPath, ShaderKind.ComputeShader);

        ShaderModule computeModule = CreateShaderModule(computeData.SpirV);

        // === Unfiorm Mapping ===
        int uniformBindingsIndex = 0;
        Dictionary<uint, int> uniformBindingsMap = [];
        _uniformBindings = new UniformBufferLayout[GraphicsContext.graphicsContext.shaderCompiler.UniformBufferBindings.Count];
        List<UniformBufferAttribute> uniformBufferAttributes = [];

        for (int i = 0; i < GraphicsContext.graphicsContext.shaderCompiler.UniformBufferAttributes.Count; i++)
        {
            var attribute = GraphicsContext.graphicsContext.shaderCompiler.UniformBufferAttributes[i];
            uint size;
            if (uniformBindingsMap.TryGetValue(attribute.Binding, out var index))
            {
                var layout = _uniformBindings[index];
                attribute.Index = (uint)index;
                size = layout.Size;     

                _locations.Add(layout.Name + "." + attribute.Name, uniformBufferAttributes.Count);

                uniformBufferAttributes.Add(attribute);
                _uniformBindings[index] = layout; 
            }
            else
            {
                var layout = GraphicsContext.graphicsContext.shaderCompiler.UniformBufferBindings[attribute.Binding];
                attribute.Index = (uint)uniformBindingsIndex;
                size = layout.Size;

                _locations.Add(layout.Name + "." + attribute.Name, uniformBufferAttributes.Count);

                uniformBufferAttributes.Add(attribute);
                _uniformBindings[uniformBindingsIndex] = layout;
                uniformBindingsMap.Add(attribute.Binding, uniformBindingsIndex);

                uniformBindingsIndex++;
            }
        }

        _uniformAttribues = [.. uniformBufferAttributes];
        // === End ===

        
        // === Storage Mapping ===
        int storageBindingsIndex = 0;
        Dictionary<uint, int> storageBindingsMap = [];
        StorageBufferLayout[] storageBindings = new StorageBufferLayout[GraphicsContext.graphicsContext.shaderCompiler.StorageBufferBindings.Count];

        for (int i = 0; i < GraphicsContext.graphicsContext.shaderCompiler.StorageBufferAttributes.Count; i++)
        {
            var attribute = GraphicsContext.graphicsContext.shaderCompiler.StorageBufferAttributes[i];
            if (!storageBindingsMap.TryGetValue(attribute.Binding, out var index))
            {
                var layout = GraphicsContext.graphicsContext.shaderCompiler.StorageBufferBindings[attribute.Binding];
                storageBindings[storageBindingsIndex] = layout;
                storageBindingsMap.Add(attribute.Binding, storageBindingsIndex);
                storageBindingsIndex++;
            }
        }
        // === End ===

        // === Sampled Image Mapping ===
        int imageBindingsIndex = 0;
        Dictionary<uint, int> imageBindingsMap = [];
        SampledImageLayout[] imageBindings = new SampledImageLayout[GraphicsContext.graphicsContext.shaderCompiler.SampledImageBindings.Count];

        for (int i = 0; i < GraphicsContext.graphicsContext.shaderCompiler.SampledImageAttributes.Count; i++)
        {
            var attribute = GraphicsContext.graphicsContext.shaderCompiler.SampledImageAttributes[i];
            if (!imageBindingsMap.TryGetValue(attribute.Binding, out var index))
            {
                var layout = GraphicsContext.graphicsContext.shaderCompiler.SampledImageBindings[attribute.Binding];
                imageBindings[imageBindingsIndex] = layout;
                imageBindingsMap.Add(attribute.Binding, imageBindingsIndex);
                imageBindingsIndex++;
            }
        }
        // === End ===

        // === Storage Image Mapping ===
        int storageImageBindingsIndex = 0;
        Dictionary<uint, int> storageImageBindingsMap = [];
        SampledImageLayout[] storageImageBindings = new SampledImageLayout[GraphicsContext.graphicsContext.shaderCompiler.StorageImageBindings.Count];

        for (int i = 0; i < GraphicsContext.graphicsContext.shaderCompiler.StorageImageAttributes.Count; i++)
        {
            var attribute = GraphicsContext.graphicsContext.shaderCompiler.StorageImageAttributes[i];
            if (!storageImageBindingsMap.TryGetValue(attribute.Binding, out var index))
            {
                var layout = GraphicsContext.graphicsContext.shaderCompiler.StorageImageBindings[attribute.Binding];
                storageImageBindings[storageImageBindingsIndex] = layout;
                storageImageBindingsMap.Add(attribute.Binding, storageImageBindingsIndex);
                storageImageBindingsIndex++;
            }
        }
        // === End ===


        // === Create bindings ===
        DescriptorSetLayoutBinding[] layoutBindings = new DescriptorSetLayoutBinding[_uniformBindings.Length + storageBindings.Length + imageBindings.Length + storageImageBindings.Length];
        
        for (int i = 0; i < _uniformBindings.Length; i++)
        {
            var layout = _uniformBindings[i];
            layoutBindings[i] = layout.LayoutBinding;
        }

        for (int i = 0; i < storageBindings.Length; i++)
        {
            var layout = storageBindings[i];
            layoutBindings[_uniformBindings.Length + i] = layout.LayoutBinding;
        }

        for (int i = 0; i < imageBindings.Length; i++)
        {
            var layout = imageBindings[i];
            layoutBindings[_uniformBindings.Length + storageBindings.Length + i] = layout.LayoutBinding;
        }

        for (int i = 0; i < storageImageBindings.Length; i++)
        {
            var layout = storageImageBindings[i];
            layoutBindings[_uniformBindings.Length + storageBindings.Length + imageBindings.Length + i] = layout.LayoutBinding;
        }

        DescriptorSetLayoutCreateInfo layoutInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = (uint)layoutBindings.Length
        };

        fixed (DescriptorSetLayoutBinding* pLayoutBindings = layoutBindings)
        layoutInfo.PBindings = pLayoutBindings;

        if (GFX.CreateDescriptorSetLayout(&layoutInfo, null, out descriptorSetLayout) != Result.Success) {
            throw new InvalidOperationException("failed to create descriptor set layout!");
        }
        // === End ===
        
        GraphicsContext.graphicsContext.shaderCompiler.UniformBufferAttributes = [];
        GraphicsContext.graphicsContext.shaderCompiler.UniformBufferBindings = [];

        GraphicsContext.graphicsContext.shaderCompiler.StorageBufferAttributes = [];
        GraphicsContext.graphicsContext.shaderCompiler.StorageBufferBindings = [];

        GraphicsContext.graphicsContext.shaderCompiler.SampledImageAttributes = [];
        GraphicsContext.graphicsContext.shaderCompiler.SampledImageBindings = [];

        GraphicsContext.graphicsContext.shaderCompiler.StorageImageAttributes = [];
        GraphicsContext.graphicsContext.shaderCompiler.StorageImageBindings = [];

        PipelineLayoutCreateInfo pipelineLayoutInfo = new()
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PushConstantRangeCount = 0, // Optional
            PPushConstantRanges = null, // Optional
        };

        fixed (DescriptorSetLayout* pDescriptorSetLayout = &descriptorSetLayout)
        pipelineLayoutInfo.PSetLayouts = pDescriptorSetLayout;

        if (GFX.CreatePipelineLayout(&pipelineLayoutInfo, null, out pipelineLayout) != Result.Success) {
            throw new InvalidOperationException("failed to create pipeline layout!");
        }
        

        PipelineShaderStageCreateInfo computeShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.ComputeBit,
            Module = computeModule,
            PName = GraphicsContext.graphicsContext.mainPtr
        };

        var pipelineInfo = new ComputePipelineCreateInfo
        {
            SType  = StructureType.ComputePipelineCreateInfo,
            Stage  = computeShaderStageInfo,
            Layout = pipelineLayout
        };

        GFX.Vk.CreateComputePipelines(GFX.Device, default, 1, &pipelineInfo, null, out pipeline);
        GFX.DestroyShaderModule(computeModule);
    }

    public void Renew()
    {
        Destroy();
        Compile();
    }

    public Descriptor GetDescriptorSet()
    {
        GraphicsContext.graphicsContext.shaderBuffer.AllocateDescriptorLayout(descriptorSetLayout, out var descriptorSets, out var descriptorPool);
        return new(pipelineLayout, descriptorPool, descriptorSets, _uniformBindings, _uniformAttribues);
    }

    private ShaderModule CreateShaderModule(byte[] code)
    {
        ShaderModuleCreateInfo createInfo = new()
        {
            SType = StructureType.ShaderModuleCreateInfo,
            CodeSize = (nuint)code.Length,
        };

        fixed (byte* codePtr = code)
        {
            createInfo.PCode = (uint*)codePtr;
        }

        if (GFX.CreateShaderModule(&createInfo, null, out ShaderModule shaderModule) != Result.Success)
            throw new Exception("Failed to create shader module!");

        return shaderModule;
    }

    protected override void Destroy()
    {
        GFX.DestroyPipeline(pipeline);
        GFX.DestroyPipelineLayout(pipelineLayout);

        GFX.DestroyDescriptorSetLayout(descriptorSetLayout);
    }
}