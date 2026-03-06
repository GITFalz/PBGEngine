using System.Runtime.InteropServices;
using PBG.Data;
using PBG.MathLibrary;
using Silk.NET.Shaderc;
using Silk.NET.Vulkan;
using static ShaderCompiler;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PBG.Graphics;

public struct ShaderInfo 
{
    public string VertexShaderPath = "";
    public string? FragmentShaderPath = null;
    public RenderPass RenderPass = GFX.RenderPass;
    public CompareOp DepthCompare = CompareOp.Less;
    public ShaderInfo() {}
}

public unsafe class Shader : BufferBase
{
    private List<VertexInputBindingDescription> _vertexBindings = [];
    private UniformBufferLayout[] _uniformBindings = [];

    public DescriptorSetLayout descriptorSetLayout;

    public PipelineLayout pipelineLayout;
    public Pipeline graphicsPipeline;

    public ShaderInfo _shaderInfo;

    public Shader(ShaderInfo info)
    {
        _shaderInfo = info;
    }

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

    private Dictionary<string, int> _locations = [];
    private UniformBufferAttribute[] _uniformAttribues = [];

    public int GetLocation(string name)
    {
        if (_locations.TryGetValue(name, out var index))
            return index;

        Console.WriteLine("[Warning] : Unknown location: " + name);
        return -1;
    }

    public void Bind()
    {
        GFX.Vk.CmdBindPipeline(GFX.CommandBuffer, PipelineBindPoint.Graphics, graphicsPipeline);
    }
    
    public void Compile()
    {
        ShaderData vertexData = GraphicsContext.graphicsContext.shaderCompiler.CompileAndReflect(_shaderInfo.VertexShaderPath, ShaderKind.VertexShader);
        ShaderData? fragmentData = null;
        if (_shaderInfo.FragmentShaderPath != null)
        {
            fragmentData = GraphicsContext.graphicsContext.shaderCompiler.CompileAndReflect(_shaderInfo.FragmentShaderPath, ShaderKind.FragmentShader);
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

            Console.WriteLine(attribute);
        }
        // === End ===

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
        

        PipelineShaderStageCreateInfo vertShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertModule,
            PName = GraphicsContext.graphicsContext.mainPtr
        };

        List<PipelineShaderStageCreateInfo> preShaderStages = [vertShaderStageInfo];

        if (fragModule != null)
        {
            PipelineShaderStageCreateInfo fragShaderStageInfo = new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.FragmentBit,
                Module = fragModule.Value,
                PName = GraphicsContext.graphicsContext.mainPtr
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

        PipelineInputAssemblyStateCreateInfo inputAssembly = new()
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = false
        };


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

        PipelineRasterizationStateCreateInfo rasterizer = new()
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            DepthClampEnable = false,
            PolygonMode = PolygonMode.Fill,
            CullMode = CullModeFlags.BackBit,
            FrontFace = FrontFace.CounterClockwise,
            LineWidth = 1f,
            DepthBiasEnable = false,
            DepthBiasConstantFactor = 0.0f, // Optional
            DepthBiasClamp = 0.0f, // Optional
            DepthBiasSlopeFactor = 0.0f // Optional
        };

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

        PipelineColorBlendAttachmentState colorBlendAttachment = new()
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            BlendEnable = false,
            SrcColorBlendFactor = BlendFactor.One, // Optional
            DstColorBlendFactor = BlendFactor.Zero, // Optional
            ColorBlendOp = BlendOp.Add, // Optional
            SrcAlphaBlendFactor = BlendFactor.One, // Optional
            DstAlphaBlendFactor = BlendFactor.Zero, // Optional
            AlphaBlendOp = BlendOp.Add // Optional
        };

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

        fixed (DescriptorSetLayout* pDescriptorSetLayout = &descriptorSetLayout)
        pipelineLayoutInfo.PSetLayouts = pDescriptorSetLayout;

        if (GFX.CreatePipelineLayout(&pipelineLayoutInfo, null, out pipelineLayout) != Result.Success) {
            throw new InvalidOperationException("failed to create pipeline layout!");
        }

        Console.WriteLine("Shader: " + Path.GetFileName(_shaderInfo.VertexShaderPath) + " " + pipelineLayout.Handle + " " + descriptorSetLayout.Handle);

        PipelineDepthStencilStateCreateInfo depthStencil = new()
        {
            SType = StructureType.PipelineDepthStencilStateCreateInfo,
            DepthTestEnable = true,
            DepthWriteEnable = true,
            DepthCompareOp = _shaderInfo.DepthCompare,
            DepthBoundsTestEnable = false,
            MinDepthBounds = 0.0f, // Optional
            MaxDepthBounds = 1.0f, // Optional
            StencilTestEnable = false,
            Front = new(), // Optional
            Back = new() // Optional
        };


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
            Layout = pipelineLayout,
            RenderPass = _shaderInfo.RenderPass.Handle == 0 ? GFX.RenderPass : _shaderInfo.RenderPass,
            Subpass = 0,
            BasePipelineHandle = default, // Optional
            BasePipelineIndex = -1 // Optional
        };

        fixed (PipelineShaderStageCreateInfo* pShaderStages = shaderStages)
        pipelineInfo.PStages = pShaderStages;

        if (GFX.CreateGraphicsPipelines(default, 1, &pipelineInfo, null, out graphicsPipeline) != Result.Success) {
            throw new InvalidOperationException("failed to create graphics pipeline!");
        }

        GFX.DestroyShaderModule(vertModule);
        if (fragModule != null)
            GFX.DestroyShaderModule(fragModule.Value);
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
        GFX.DestroyPipeline(graphicsPipeline);
        GFX.DestroyPipelineLayout(pipelineLayout);

        GFX.DestroyDescriptorSetLayout(descriptorSetLayout);
    }
}