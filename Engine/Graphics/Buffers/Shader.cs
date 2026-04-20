using System.Runtime.InteropServices;
using PBG.Graphics.Vulkan;
using Silk.NET.Shaderc;
using Silk.NET.Vulkan;
using static ShaderCompiler;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics;

public struct ShaderInfo 
{
    public string VertexShaderPath = "";
    public string? FragmentShaderPath = null;
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
        VertexShaderPath = vertShader;
        FragmentShaderPath = fragShader;
    }

    public ShaderInfo() {}
}

public unsafe class Shader : BufferBase, IShader
{
    private List<VertexInputBindingDescription> _vertexBindings = [];
    private UniformBufferLayout[] _uniformBindings = [];

    private Dictionary<string, int> _locations = [];
    private UniformBufferAttribute[] _uniformAttribues = [];

    public DescriptorSetLayout DescriptorSetLayout;
    public PipelineLayout PipelineLayout;
    public Pipeline GraphicsPipeline;

    private ShaderInfo _shaderInfo;

    public Shader(ShaderInfo info)
    {
        _shaderInfo = info;
    }

    public string GetPath() => _shaderInfo.VertexShaderPath;

    public void BindVertexBuffer(uint bindingPoint, uint stride)
    {
        _vertexBindings.Add(new()
        {
            Binding = bindingPoint,
            Stride = stride,
            InputRate = VertexInputRate.Vertex
        });
    }

    public void BindVertexBuffer<T>(uint bindingPoint) where T : struct
    {
        _vertexBindings.Add(new()
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
        ShaderData vertexData = _shaderCompiler.CompileAndReflect(_shaderInfo.VertexShaderPath, ShaderKind.VertexShader);
        ShaderData? fragmentData = null;
        if (_shaderInfo.FragmentShaderPath != null)
        {
            fragmentData = _shaderCompiler.CompileAndReflect(_shaderInfo.FragmentShaderPath, ShaderKind.FragmentShader);
        }

        ShaderModule vertModule = CreateShaderModule(vertexData.SpirV);
        ShaderModule? fragModule = null;
        if (fragmentData != null)
        {
            fragModule = CreateShaderModule(fragmentData.Value.SpirV);
        }

        // === Vertex buffer ===
        VertexInputAttributeDescription[] attributeDescriptions = new VertexInputAttributeDescription[vertexData.VertexAttributes.Length];
        VertexInputBindingDescription[] vertexBindings = [.._vertexBindings];
        for (int i = 0; i < vertexData.VertexAttributes.Length; i++)
        {
            var attribute = vertexData.VertexAttributes[i];
            //if (!_attributes.TryGetValue(attribute.Name, out var att))
                //throw new KeyNotFoundException($"[Error] : was not able to find attribute {attribute.Name} while creating the shader");

            attributeDescriptions[i].Location = (uint)attribute.Location;
            attributeDescriptions[i].Binding = 0;
            attributeDescriptions[i].Format = attribute.Format;
            attributeDescriptions[i].Offset = attribute.Offset;
        }
        // === End ===

        // === Unfiorm Mapping ===
        int uniformBindingsIndex = 0;
        Dictionary<uint, int> uniformBindingsMap = [];
        _uniformBindings = new UniformBufferLayout[_shaderCompiler.UniformBufferBindings.Count];
        List<UniformBufferAttribute> uniformBufferAttributes = [];

        for (int i = 0; i < _shaderCompiler.UniformBufferAttributes.Count; i++)
        {
            var attribute = _shaderCompiler.UniformBufferAttributes[i];
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
                var layout = _shaderCompiler.UniformBufferBindings[attribute.Binding];
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
        StorageBufferLayout[] storageBindings = new StorageBufferLayout[_shaderCompiler.StorageBufferBindings.Count];

        for (int i = 0; i < _shaderCompiler.StorageBufferAttributes.Count; i++)
        {
            var attribute = _shaderCompiler.StorageBufferAttributes[i];
            if (!storageBindingsMap.TryGetValue(attribute.Binding, out var index))
            {
                var layout = _shaderCompiler.StorageBufferBindings[attribute.Binding];
                storageBindings[storageBindingsIndex] = layout;
                storageBindingsMap.Add(attribute.Binding, storageBindingsIndex);
                storageBindingsIndex++;
            }
        }
        // === End ===

        // === Sampled Image Mapping ===
        int imageBindingsIndex = 0;
        Dictionary<uint, int> imageBindingsMap = [];
        SampledImageLayout[] imageBindings = new SampledImageLayout[_shaderCompiler.SampledImageBindings.Count];

        for (int i = 0; i < _shaderCompiler.SampledImageAttributes.Count; i++)
        {
            var attribute = _shaderCompiler.SampledImageAttributes[i];
            if (!imageBindingsMap.TryGetValue(attribute.Binding, out var index))
            {
                var layout = _shaderCompiler.SampledImageBindings[attribute.Binding];
                imageBindings[imageBindingsIndex] = layout;
                imageBindingsMap.Add(attribute.Binding, imageBindingsIndex);
                imageBindingsIndex++;
            }
        }
        // === End ===

        // === Storage Image Mapping ===
        int storageImageBindingsIndex = 0;
        Dictionary<uint, int> storageImageBindingsMap = [];
        SampledImageLayout[] storageImageBindings = new SampledImageLayout[_shaderCompiler.StorageImageBindings.Count];

        for (int i = 0; i < _shaderCompiler.StorageImageAttributes.Count; i++)
        {
            var attribute = _shaderCompiler.StorageImageAttributes[i];
            if (!storageImageBindingsMap.TryGetValue(attribute.Binding, out var index))
            {
                var layout = _shaderCompiler.StorageImageBindings[attribute.Binding];
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

        if (GFX.CreateDescriptorSetLayout(&layoutInfo, null, out DescriptorSetLayout) != Result.Success) {
            throw new InvalidOperationException("failed to create descriptor set layout!");
        }
        // === End ===
        
        _shaderCompiler.Clear();

        PipelineShaderStageCreateInfo vertShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertModule,
            PName = _mainPtr
        };

        List<PipelineShaderStageCreateInfo> preShaderStages = [vertShaderStageInfo];

        if (fragModule != null)
        {
            PipelineShaderStageCreateInfo fragShaderStageInfo = new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.FragmentBit,
                Module = fragModule.Value,
                PName = _mainPtr
            };

            preShaderStages.Add(fragShaderStageInfo);
        }

        PipelineShaderStageCreateInfo[] shaderStages = [.. preShaderStages];

        PipelineVertexInputStateCreateInfo vertexInputInfo = new()
        {
            SType = StructureType.PipelineVertexInputStateCreateInfo,
            VertexBindingDescriptionCount = 0,
            PVertexBindingDescriptions = null, // Optional
            VertexAttributeDescriptionCount = 0,
            PVertexAttributeDescriptions = null // Optional
        };

        if (vertexBindings.Length > 0)
        {
            vertexInputInfo.VertexBindingDescriptionCount = (uint)vertexBindings.Length;

            fixed (VertexInputBindingDescription* PBGertexBindings = vertexBindings)
            vertexInputInfo.PVertexBindingDescriptions = PBGertexBindings;
        }

        if (attributeDescriptions.Length > 0)
        {
            vertexInputInfo.VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length;

            fixed (VertexInputAttributeDescription* pAttributeDescriptions = attributeDescriptions)
            vertexInputInfo.PVertexAttributeDescriptions = pAttributeDescriptions;
        }

        var inputAssembly = _shaderInfo.InputAssembly;

        // == Viewport Settings ==
        Viewport viewport = new()
        {
            X = 0.0f,
            Y = 0.0f,
            Width = (float)GFX.SwapChainExtent.Width,
            Height = (float)GFX.SwapChainExtent.Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };

        Rect2D scissor = new()
        {
            Offset = new Offset2D(0, 0),
            Extent = GFX.SwapChainExtent
        };

        DynamicState[] dynamicStates = [
            DynamicState.Viewport,
            DynamicState.Scissor
        ];

        PipelineDynamicStateCreateInfo dynamicState = new()
        {
            SType = StructureType.PipelineDynamicStateCreateInfo,
            DynamicStateCount = (uint)dynamicStates.Length,    
        };

        fixed(DynamicState* pDynamicStates = dynamicStates)
        dynamicState.PDynamicStates = pDynamicStates;

        PipelineViewportStateCreateInfo viewportState = new()
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            ScissorCount = 1,
            PViewports = &viewport,
            PScissors = &scissor
        };

        var rasterizer = _shaderInfo.Rasterizer;

        PipelineMultisampleStateCreateInfo multisampling = new()
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            SampleShadingEnable = false,
            RasterizationSamples = SampleCountFlags.Count1Bit,
            MinSampleShading = 1.0f, // Optional
            PSampleMask = null, // Optional
            AlphaToCoverageEnable = false, // Optional
            AlphaToOneEnable = false // Optional
        };

        var colorBlendAttachment = _shaderInfo.ColorBlendAttachment;

        PipelineColorBlendStateCreateInfo colorBlending = new()
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = false,
            LogicOp = LogicOp.Copy, // Optional
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachment
        };
        colorBlending.BlendConstants[0] = 0.0f; // Optional
        colorBlending.BlendConstants[1] = 0.0f; // Optional
        colorBlending.BlendConstants[2] = 0.0f; // Optional
        colorBlending.BlendConstants[3] = 0.0f; // Optional

        PipelineLayoutCreateInfo pipelineLayoutInfo = new()
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PushConstantRangeCount = 0, // Optional
            PPushConstantRanges = null, // Optional
        };

        fixed (DescriptorSetLayout* pDescriptorSetLayout = &DescriptorSetLayout)
        pipelineLayoutInfo.PSetLayouts = pDescriptorSetLayout;

        if (GFX.CreatePipelineLayout(&pipelineLayoutInfo, null, out PipelineLayout) != Result.Success) {
            throw new InvalidOperationException("failed to create pipeline layout!");
        }

        var depthStencil = _shaderInfo.DepthStencil;

        GraphicsPipelineCreateInfo pipelineInfo = new()
        {
            SType = StructureType.GraphicsPipelineCreateInfo,
            StageCount = (uint)shaderStages.Length,
            PVertexInputState = &vertexInputInfo,
            PInputAssemblyState = &inputAssembly,
            PViewportState = &viewportState,
            PRasterizationState = &rasterizer,
            PMultisampleState = &multisampling,
            PDepthStencilState = &depthStencil,
            PColorBlendState = &colorBlending,
            PDynamicState = &dynamicState,
            Layout = PipelineLayout,
            RenderPass = _shaderInfo.RenderPass,
            Subpass = 0,
            BasePipelineHandle = default, // Optional
            BasePipelineIndex = -1 // Optional
        };

        fixed (PipelineShaderStageCreateInfo* pShaderStages = shaderStages)
        pipelineInfo.PStages = pShaderStages;

        if (GFX.CreateGraphicsPipelines(default, 1, &pipelineInfo, null, out GraphicsPipeline) != Result.Success) {
            throw new InvalidOperationException("failed to create graphics pipeline!");
        }

        GFX.DestroyShaderModule(vertModule);
        if (fragModule != null)
            GFX.DestroyShaderModule(fragModule.Value);
    }

    public Descriptor GetDescriptorSet()
    {
        _shaderBuffer.AllocateDescriptorLayout(DescriptorSetLayout, out var descriptorSets, out var descriptorPool);
        return new(this, PipelineLayout, descriptorPool, descriptorSets, _uniformBindings, _uniformAttribues);
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

    public void Renew()
    {
        Destroy();
        Compile();
    }

    public void Renew(ShaderInfo info)
    {
        _shaderInfo = info;
        Destroy();
        Compile();
    }

    protected override void Destroy()
    {
        _vertexBindings = [];
        _uniformBindings = [];
        _locations = [];
        _uniformAttribues = [];
        
        GFX.DestroyPipeline(GraphicsPipeline);
        GFX.DestroyPipelineLayout(PipelineLayout);

        GFX.DestroyDescriptorSetLayout(DescriptorSetLayout);
    }
}