using PBG.Graphics.Vulkan;
using Silk.NET.Shaderc;
using Silk.NET.Vulkan;
using static ShaderCompiler;

namespace PBG.Graphics;

public struct ComputeShaderInfo 
{
    public string ComputeShaderPath = "";
    public ComputeShaderInfo(string path)
    {
        ComputeShaderPath = path;
    }
}

public unsafe class ComputeShader : BufferBase, IShader
{
    private HashSet<Descriptor> _boundDescriptors = [];

    private Dictionary<string, int> _locations = [];
    private UniformBufferAttribute[] _uniformAttribues = [];
    private UniformBufferLayout[] _uniformBindings = [];
    public DescriptorSetLayout descriptorSetLayout;
    public PipelineLayout pipelineLayout;
    public Pipeline pipeline;


    public ComputeShaderInfo _shaderInfo;

    public ComputeShader(ComputeShaderInfo info)
    {
        _shaderInfo = info;
    }

    public string GetPath() => _shaderInfo.ComputeShaderPath;

    public int GetLocation(string name)
    {
        if (_locations.TryGetValue(name, out var index))
            return index;

        Console.WriteLine("[Warning] : Unknown location: " + name);
        return -1;
    }

    public void Bind() => Bind(GFX.CommandBuffer);
    public void Bind(CommandBuffer commandBuffer)
    {
        GFX.Vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Compute, pipeline);
    }

    public void Dispatch(uint groupsX, uint groupsY, uint groupsZ) => Dispatch(GFX.CommandBuffer, groupsX, groupsY, groupsZ);
    public void Dispatch(CommandBuffer commandBuffer, uint groupsX, uint groupsY, uint groupsZ)
    {
        GFX.Vk.CmdDispatch(commandBuffer, groupsX, groupsY, groupsZ);
    }

    public void DispatchBarrier(Descriptor descriptor, uint groupsX, uint groupsY, uint groupsZ) => DispatchBarrier(GFX.CommandBuffer, descriptor, groupsX, groupsY, groupsZ);
    public void DispatchBarrier(CommandBuffer commandBuffer, Descriptor descriptor, uint groupsX, uint groupsY, uint groupsZ)
    {
        var imageBarriers = descriptor.GetImageBarriers();
        var bufferBarriers = descriptor.GetBufferBarriers();

        GFX.Vk.CmdDispatch(commandBuffer, groupsX, groupsY, groupsZ);
        
        fixed (BufferMemoryBarrier* pBufferBarrier = bufferBarriers)
        fixed (ImageMemoryBarrier* pImageBarrier = imageBarriers)
        GFX.Vk.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.ComputeShaderBit, PipelineStageFlags.VertexShaderBit, DependencyFlags.None, 0, null, 
            (uint)bufferBarriers.Length, pBufferBarrier, (uint)imageBarriers.Length, pImageBarrier);
    }
    
    public void Compile()
    {
        _shaderCompiler.CompileComputeShader(_shaderInfo, out PBGComputeShaderModule module);

        _locations = module.Locations;
        _uniformAttribues = module.UniformAttribues;
        _uniformBindings = module.UniformBindings;
        descriptorSetLayout = module.DescriptorSetLayout;
        pipelineLayout = module.PipelineLayout;
        pipeline = module.Pipeline;
    }

    public void Renew()
    {
        Destroy();
        Compile();
    }

    public Descriptor GetDescriptorSet()
    {
        _shaderBuffer.AllocateDescriptorLayout(descriptorSetLayout, out var descriptorSets, out var descriptorPool);
        var descriptor = new Descriptor(this, pipelineLayout, descriptorPool, descriptorSets, _uniformBindings, _uniformAttribues);
        _boundDescriptors.Add(descriptor);
        return descriptor;
    }

    public void RenewDescriptors()
    {
        try
        {
            _shaderCompiler.CompileComputeShader(_shaderInfo, out PBGComputeShaderModule module);

            Destroy();
        
            _locations = module.Locations;
            _uniformAttribues = module.UniformAttribues;
            _uniformBindings = module.UniformBindings;
            descriptorSetLayout = module.DescriptorSetLayout;
            pipelineLayout = module.PipelineLayout;
            pipeline = module.Pipeline;

            RenewDescriptorSets();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] : Failed to reload shader {_shaderInfo.ComputeShaderPath}, {ex.Message}");
            return;
        }
    }

    private void RenewDescriptorSets()
    {
        foreach (var descriptor in _boundDescriptors)
        {
            descriptor.BaseDispose();
            _shaderBuffer.AllocateDescriptorLayout(descriptorSetLayout, out var descriptorSets, out var descriptorPool);
            descriptor.Create(pipelineLayout, descriptorPool, descriptorSets, _uniformBindings, _uniformAttribues);
            descriptor.RebindAll();
        }
    }

    public bool RemoveDescriptorSet(Descriptor descriptor)
    {
        return _boundDescriptors.Remove(descriptor);
    }

    public static ShaderModule CreateShaderModule(byte[] code)
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