using System.Runtime.InteropServices;
using Silk.NET.Vulkan;

namespace PBG.Graphics;

public struct ShaderInfo 
{
    public PString VertexShaderFile = "";
    public PString? FragmentShaderFile = null;
    public RenderPass RenderPass = VulkanInstance.Instance.ClearRenderPass.RenderPass;

    public PipelineRasterizationStateCreateInfo Rasterizer = new()
    {
        SType = StructureType.PipelineRasterizationStateCreateInfo,
        DepthClampEnable = false,
        PolygonMode = PolygonMode.Fill,
        CullMode = CullModeFlags.BackBit,
        FrontFace = FrontFace.CounterClockwise,
        LineWidth = 1f,
        DepthBiasEnable = false,
        DepthBiasConstantFactor = 0.0f,
        DepthBiasClamp = 0.0f,
        DepthBiasSlopeFactor = 0.0f
    };

    public PipelineColorBlendAttachmentState ColorBlendAttachment = new()
    {
        ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
        BlendEnable = false,
        SrcColorBlendFactor = BlendFactor.One,
        DstColorBlendFactor = BlendFactor.Zero,
        ColorBlendOp = BlendOp.Add,
        SrcAlphaBlendFactor = BlendFactor.One,
        DstAlphaBlendFactor = BlendFactor.Zero,
        AlphaBlendOp = BlendOp.Add
    };

    public PipelineDepthStencilStateCreateInfo DepthStencil = new()
    {
        SType = StructureType.PipelineDepthStencilStateCreateInfo,
        DepthTestEnable = true,
        DepthWriteEnable = true,
        DepthCompareOp = CompareOp.Less,
        DepthBoundsTestEnable = false,
        MinDepthBounds = 0.0f, // Optional
        MaxDepthBounds = 1.0f, // Optional
        StencilTestEnable = false,
        Front = new(), // Optional
        Back = new() // Optional
    };

    public PipelineInputAssemblyStateCreateInfo InputAssembly = new()
    {
        SType = StructureType.PipelineInputAssemblyStateCreateInfo,
        Topology = PrimitiveTopology.TriangleList,
        PrimitiveRestartEnable = false
    };
    
    public ShaderInfo(string vertShader, string fragShader)
    {
        VertexShaderFile = vertShader;
        FragmentShaderFile = fragShader;
    }

    public ShaderInfo() {}


    public readonly string BaseVertexShaderPath() => ShaderHelper.BaseShaderPath(VertexShaderFile);
    public readonly string FixedVertexShaderPath() => ShaderHelper.FixedShaderPath(VertexShaderFile);

    public readonly string? BaseFragmentShaderPath() => FragmentShaderFile == null ? null : ShaderHelper.BaseShaderPath(FragmentShaderFile);
    public readonly string? FixedFragmentShaderPath() => FragmentShaderFile == null ? null : ShaderHelper.FixedShaderPath(FragmentShaderFile);
}

public unsafe class Shader : BufferBase, IShader
{
    public List<VertexInputBindingDescription> VertexBindings = [];
    private UniformBufferLayout[] _uniformBindings = [];

    private Dictionary<string, int> _locations = [];
    private UniformBufferAttribute[] _uniformAttribues = [];

    private HashSet<Descriptor> _boundDescriptors = [];

    public DescriptorSetLayout DescriptorSetLayout;
    public PipelineLayout PipelineLayout;
    public Pipeline GraphicsPipeline;

    private ShaderInfo _shaderInfo;
    public string Name = "";

    public Shader(ShaderInfo info)
    {
        _shaderInfo = info;
        Name = Path.GetRelativePath(Game.ShaderPath, info.VertexShaderFile + "-" + info.FragmentShaderFile);
    }

    public string GetPath() => _shaderInfo.VertexShaderFile;

    public void BindVertexBuffer(uint bindingPoint, uint stride)
    {
        VertexBindings.Add(new()
        {
            Binding = bindingPoint,
            Stride = stride,
            InputRate = VertexInputRate.Vertex
        });
    }

    public void BindVertexBuffer<T>(uint bindingPoint) where T : struct
    {
        VertexBindings.Add(new()
        {
            Binding = bindingPoint,
            Stride = (uint)Marshal.SizeOf<T>(),
            InputRate = VertexInputRate.Vertex
        });
    }

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
        GFX.Vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, GraphicsPipeline);
    }
    
    public void Compile()
    {
        _shaderCompiler.CompileShader(this, _shaderInfo, out PBGShaderModule module);

        _locations = module.Locations;
        _uniformAttribues = module.UniformAttribues;
        _uniformBindings = module.UniformBindings;
        DescriptorSetLayout = module.DescriptorSetLayout;
        PipelineLayout = module.PipelineLayout;
        GraphicsPipeline = module.Pipeline;
    }

    public Descriptor GetDescriptorSet()
    {
        _shaderBuffer.AllocateDescriptorLayout(DescriptorSetLayout, out var descriptorSets, out var descriptorPool);
        var descriptor = new Descriptor(this, PipelineLayout, descriptorPool, descriptorSets, _uniformBindings, _uniformAttribues);
        _boundDescriptors.Add(descriptor);
        return descriptor;
    }

    public void RenewDescriptors()
    {
        try
        {
            _shaderCompiler.CompileShader(this, _shaderInfo, out PBGShaderModule module);

            BaseDispose();
        
            _locations = module.Locations;
            _uniformAttribues = module.UniformAttribues;
            _uniformBindings = module.UniformBindings;
            DescriptorSetLayout = module.DescriptorSetLayout;
            PipelineLayout = module.PipelineLayout;
            GraphicsPipeline = module.Pipeline;

            RenewDescriptorSets();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] : Failed to reload shader {_shaderInfo.VertexShaderFile}, {ex.Message}");
            return;
        }
    }

    private void RenewDescriptorSets()
    {
        foreach (var descriptor in _boundDescriptors)
        {
            descriptor.BaseDispose();
            _shaderBuffer.AllocateDescriptorLayout(DescriptorSetLayout, out var descriptorSets, out var descriptorPool);
            descriptor.Create(PipelineLayout, descriptorPool, descriptorSets, _uniformBindings, _uniformAttribues);
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

    public void Renew()
    {
        BaseDispose();
        Compile();
    }

    public void Renew(ShaderInfo info)
    {
        _shaderInfo = info;
        BaseDispose();
        Compile();
    }

    public void BaseDispose()
    {
        _uniformBindings = [];
        _locations = [];
        _uniformAttribues = [];
        
        GFX.DestroyPipeline(GraphicsPipeline);
        GFX.DestroyPipelineLayout(PipelineLayout);

        GFX.DestroyDescriptorSetLayout(DescriptorSetLayout);
    }

    protected override void Destroy()
    {
        BaseDispose();
        VertexBindings = [];
    }
}