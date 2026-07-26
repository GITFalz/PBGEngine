using Silk.NET.Shaderc;
using Silk.NET.Vulkan;
using Silk.NET.SPIRV.Cross;
using ShadercCompiler = Silk.NET.Shaderc.Compiler;
using CrossCompiler = Silk.NET.SPIRV.Cross.Compiler;
using Silk.NET.SPIRV;
using System.Runtime.InteropServices;
using PBG;
using PBG.Graphics;

public unsafe class ShaderCompiler
{
    private readonly Shaderc _shaderc;
    private readonly ShadercCompiler* _compiler;
    private readonly CompileOptions* _options;

    private readonly Cross _cross;
    private readonly Context* _context;

    public Dictionary<uint, UniformBufferLayout> UniformBufferBindings = [];
    public List<UniformBufferAttribute> UniformBufferAttributes = [];

    public Dictionary<uint, StorageBufferLayout> StorageBufferBindings = [];
    public List<StorageBufferAttribute> StorageBufferAttributes = [];

    public Dictionary<uint, SampledImageLayout> SampledImageBindings = [];
    public List<SampledImageAttribute> SampledImageAttributes = [];

    public Dictionary<uint, SampledImageLayout> StorageImageBindings = [];
    public List<SampledImageAttribute> StorageImageAttributes = [];

    private readonly byte* _mainPtr = (byte*)"main".ToPtr();

    public ShaderCompiler()
    {
        _shaderc = Shaderc.GetApi();
        _compiler = _shaderc.CompilerInitialize();
        _options = _shaderc.CompileOptionsInitialize();
        _cross = Cross.GetApi();

        Context* context;
        _cross.ContextCreate(&context);
        _context = context;

        _shaderc.CompileOptionsSetIncludeCallbacks(_options, PfnIncludeResolveFn.From(ResolveInclude), PfnIncludeResultReleaseFn.From(ReleaseInclude), null);
    }

    public struct ShaderData
    {
        public byte[] SpirV;
        public ShaderKind Kind;
        public VertexAttribute[] VertexAttributes;
    }


    public void CompileShader(Shader shader, ShaderInfo shaderInfo, out PBGShaderModule module)
    {
        module = new();

        ShaderData vertexData = CompileAndReflect(shaderInfo.VertexShaderPath, ShaderKind.VertexShader);
        ShaderData? fragmentData = null;
        if (shaderInfo.FragmentShaderPath != null)
        {
            fragmentData = CompileAndReflect(shaderInfo.FragmentShaderPath, ShaderKind.FragmentShader);
        }

        ShaderModule vertModule = Shader.CreateShaderModule(vertexData.SpirV);
        ShaderModule? fragModule = null;
        if (fragmentData != null)
        {
            fragModule = Shader.CreateShaderModule(fragmentData.Value.SpirV);
        }

        // === Vertex buffer ===
        VertexInputAttributeDescription[] attributeDescriptions = new VertexInputAttributeDescription[vertexData.VertexAttributes.Length];
        VertexInputBindingDescription[] vertexBindings = [..shader.VertexBindings];

        vertexBindings = [.. vertexBindings.OrderBy(x => x.Binding)];

        if (vertexBindings.Length > 0)
        {
            uint currentBinding = 0;
            var currentVertexBinding = vertexBindings[0];
            uint currentStride = currentVertexBinding.Stride; 

            for (int i = 0; i < vertexData.VertexAttributes.Length; i++)
            {
                var attribute = vertexData.VertexAttributes[i];
                //if (!_attributes.TryGetValue(attribute.Name, out var att))
                    //throw new KeyNotFoundException($"[Error] : was not able to find attribute {attribute.Name} while creating the shader");

                if (attribute.Offset >= currentStride && currentBinding + 1 < vertexBindings.Length)
                {
                    currentBinding++;
                    currentVertexBinding = vertexBindings[currentBinding];
                    currentStride += currentVertexBinding.Stride;  
                }

                attributeDescriptions[i].Location = (uint)attribute.Location;
                attributeDescriptions[i].Binding = currentBinding;
                attributeDescriptions[i].Format = attribute.Format;
                attributeDescriptions[i].Offset = 0; //attribute.Offset;

                Console.WriteLine(attribute);
            }
        }  
        // === End ===

        // === Unfiorm Mapping ===
        int uniformBindingsIndex = 0;
        Dictionary<uint, int> uniformBindingsMap = [];
        module.UniformBindings = new UniformBufferLayout[UniformBufferBindings.Count];
        List<UniformBufferAttribute> uniformBufferAttributes = [];

        for (int i = 0; i < UniformBufferAttributes.Count; i++)
        {
            var attribute = UniformBufferAttributes[i];
            uint size;
            if (uniformBindingsMap.TryGetValue(attribute.Binding, out var index))
            {
                var layout = module.UniformBindings[index];
                attribute.Index = (uint)index;
                size = layout.Size;     

                module.Locations.Add(layout.Name + "." + attribute.Name, uniformBufferAttributes.Count);

                uniformBufferAttributes.Add(attribute);
                module.UniformBindings[index] = layout; 
            }
            else
            {
                var layout = UniformBufferBindings[attribute.Binding];
                attribute.Index = (uint)uniformBindingsIndex;
                size = layout.Size;

                module.Locations.Add(layout.Name + "." + attribute.Name, uniformBufferAttributes.Count);

                uniformBufferAttributes.Add(attribute);
                module.UniformBindings[uniformBindingsIndex] = layout;
                uniformBindingsMap.Add(attribute.Binding, uniformBindingsIndex);

                uniformBindingsIndex++;
            }
        }

        module.UniformAttribues = [.. uniformBufferAttributes];
        // === End ===

        
        // === Storage Mapping ===
        int storageBindingsIndex = 0;
        Dictionary<uint, int> storageBindingsMap = [];
        StorageBufferLayout[] storageBindings = new StorageBufferLayout[StorageBufferBindings.Count];

        for (int i = 0; i < StorageBufferAttributes.Count; i++)
        {
            var attribute = StorageBufferAttributes[i];
            if (!storageBindingsMap.TryGetValue(attribute.Binding, out var index))
            {
                var layout = StorageBufferBindings[attribute.Binding];
                storageBindings[storageBindingsIndex] = layout;
                storageBindingsMap.Add(attribute.Binding, storageBindingsIndex);
                storageBindingsIndex++;
            }
        }
        // === End ===

        // === Sampled Image Mapping ===
        int imageBindingsIndex = 0;
        Dictionary<uint, int> imageBindingsMap = [];
        SampledImageLayout[] imageBindings = new SampledImageLayout[SampledImageBindings.Count];

        for (int i = 0; i < SampledImageAttributes.Count; i++)
        {
            var attribute = SampledImageAttributes[i];
            if (!imageBindingsMap.TryGetValue(attribute.Binding, out var index))
            {
                var layout = SampledImageBindings[attribute.Binding];
                imageBindings[imageBindingsIndex] = layout;
                imageBindingsMap.Add(attribute.Binding, imageBindingsIndex);
                imageBindingsIndex++;
            }
        }
        // === End ===

        // === Storage Image Mapping ===
        int storageImageBindingsIndex = 0;
        Dictionary<uint, int> storageImageBindingsMap = [];
        SampledImageLayout[] storageImageBindings = new SampledImageLayout[StorageImageBindings.Count];

        for (int i = 0; i < StorageImageAttributes.Count; i++)
        {
            var attribute = StorageImageAttributes[i];
            if (!storageImageBindingsMap.TryGetValue(attribute.Binding, out var index))
            {
                var layout = StorageImageBindings[attribute.Binding];
                storageImageBindings[storageImageBindingsIndex] = layout;
                storageImageBindingsMap.Add(attribute.Binding, storageImageBindingsIndex);
                storageImageBindingsIndex++;
            }
        }
        // === End ===


        // === Create bindings ===
        DescriptorSetLayoutBinding[] layoutBindings = new DescriptorSetLayoutBinding[module.UniformBindings.Length + storageBindings.Length + imageBindings.Length + storageImageBindings.Length];
        
        for (int i = 0; i < module.UniformBindings.Length; i++)
        {
            var layout = module.UniformBindings[i];
            layoutBindings[i] = layout.LayoutBinding;
        }

        for (int i = 0; i < storageBindings.Length; i++)
        {
            var layout = storageBindings[i];
            layoutBindings[module.UniformBindings.Length + i] = layout.LayoutBinding;
        }

        for (int i = 0; i < imageBindings.Length; i++)
        {
            var layout = imageBindings[i];
            layoutBindings[module.UniformBindings.Length + storageBindings.Length + i] = layout.LayoutBinding;
        }

        for (int i = 0; i < storageImageBindings.Length; i++)
        {
            var layout = storageImageBindings[i];
            layoutBindings[module.UniformBindings.Length + storageBindings.Length + imageBindings.Length + i] = layout.LayoutBinding;
        }

        DescriptorSetLayoutCreateInfo layoutInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = (uint)layoutBindings.Length
        };

        fixed (DescriptorSetLayoutBinding* pLayoutBindings = layoutBindings)
        layoutInfo.PBindings = pLayoutBindings;

        if (GFX.CreateDescriptorSetLayout(&layoutInfo, null, out var descriptorSetLayout) != Silk.NET.Vulkan.Result.Success) {
            throw new InvalidOperationException("failed to create descriptor set layout!");
        }
        // === End ===
        
        Clear();

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

        var inputAssembly = shaderInfo.InputAssembly;

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

        var rasterizer = shaderInfo.Rasterizer;

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

        var colorBlendAttachment = shaderInfo.ColorBlendAttachment;

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

        pipelineLayoutInfo.PSetLayouts = &descriptorSetLayout;

        if (GFX.CreatePipelineLayout(&pipelineLayoutInfo, null, out var pipelineLayout) != Silk.NET.Vulkan.Result.Success) {
            throw new InvalidOperationException("failed to create pipeline layout!");
        }

        var depthStencil = shaderInfo.DepthStencil;

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
            RenderPass = shaderInfo.RenderPass,
            Subpass = 0,
            BasePipelineHandle = default, // Optional
            BasePipelineIndex = -1 // Optional
        };

        fixed (PipelineShaderStageCreateInfo* pShaderStages = shaderStages)
        pipelineInfo.PStages = pShaderStages;

        if (GFX.CreateGraphicsPipelines(default, 1, &pipelineInfo, null, out var pipeline) != Silk.NET.Vulkan.Result.Success) {
            throw new InvalidOperationException("failed to create graphics pipeline!");
        }

        GFX.DestroyShaderModule(vertModule);
        if (fragModule != null)
            GFX.DestroyShaderModule(fragModule.Value);

        module.DescriptorSetLayout = descriptorSetLayout;
        module.PipelineLayout = pipelineLayout;
        module.Pipeline = pipeline;
    }



    public void CompileComputeShader(ComputeShaderInfo shaderInfo, out PBGComputeShaderModule module)
    {
        module = new();

        ShaderData computeData = CompileAndReflect(shaderInfo.ComputeShaderPath, ShaderKind.ComputeShader);

        ShaderModule computeModule = ComputeShader.CreateShaderModule(computeData.SpirV);

        // === Unfiorm Mapping ===
        int uniformBindingsIndex = 0;
        Dictionary<uint, int> uniformBindingsMap = [];
        module.UniformBindings = new UniformBufferLayout[UniformBufferBindings.Count];
        List<UniformBufferAttribute> uniformBufferAttributes = [];

        for (int i = 0; i < UniformBufferAttributes.Count; i++)
        {
            var attribute = UniformBufferAttributes[i];
            uint size;
            if (uniformBindingsMap.TryGetValue(attribute.Binding, out var index))
            {
                var layout = module.UniformBindings[index];
                attribute.Index = (uint)index;
                size = layout.Size;     

                module.Locations.Add(layout.Name + "." + attribute.Name, uniformBufferAttributes.Count);

                uniformBufferAttributes.Add(attribute);
                module.UniformBindings[index] = layout; 
            }
            else
            {
                var layout = UniformBufferBindings[attribute.Binding];
                attribute.Index = (uint)uniformBindingsIndex;
                size = layout.Size;

                module.Locations.Add(layout.Name + "." + attribute.Name, uniformBufferAttributes.Count);

                uniformBufferAttributes.Add(attribute);
                module.UniformBindings[uniformBindingsIndex] = layout;
                uniformBindingsMap.Add(attribute.Binding, uniformBindingsIndex);

                uniformBindingsIndex++;
            }
        }

        module.UniformAttribues = [.. uniformBufferAttributes];
        // === End ===

        
        // === Storage Mapping ===
        int storageBindingsIndex = 0;
        Dictionary<uint, int> storageBindingsMap = [];
        StorageBufferLayout[] storageBindings = new StorageBufferLayout[StorageBufferBindings.Count];

        for (int i = 0; i < StorageBufferAttributes.Count; i++)
        {
            var attribute = StorageBufferAttributes[i];
            if (!storageBindingsMap.TryGetValue(attribute.Binding, out var index))
            {
                var layout = StorageBufferBindings[attribute.Binding];
                storageBindings[storageBindingsIndex] = layout;
                storageBindingsMap.Add(attribute.Binding, storageBindingsIndex);
                storageBindingsIndex++;
            }
        }
        // === End ===

        // === Sampled Image Mapping ===
        int imageBindingsIndex = 0;
        Dictionary<uint, int> imageBindingsMap = [];
        SampledImageLayout[] imageBindings = new SampledImageLayout[SampledImageBindings.Count];

        for (int i = 0; i < SampledImageAttributes.Count; i++)
        {
            var attribute = SampledImageAttributes[i];
            if (!imageBindingsMap.TryGetValue(attribute.Binding, out var index))
            {
                var layout = SampledImageBindings[attribute.Binding];
                imageBindings[imageBindingsIndex] = layout;
                imageBindingsMap.Add(attribute.Binding, imageBindingsIndex);
                imageBindingsIndex++;
            }
        }
        // === End ===

        // === Storage Image Mapping ===
        int storageImageBindingsIndex = 0;
        Dictionary<uint, int> storageImageBindingsMap = [];
        SampledImageLayout[] storageImageBindings = new SampledImageLayout[StorageImageBindings.Count];

        for (int i = 0; i < StorageImageAttributes.Count; i++)
        {
            var attribute = StorageImageAttributes[i];
            if (!storageImageBindingsMap.TryGetValue(attribute.Binding, out var index))
            {
                var layout = StorageImageBindings[attribute.Binding];
                storageImageBindings[storageImageBindingsIndex] = layout;
                storageImageBindingsMap.Add(attribute.Binding, storageImageBindingsIndex);
                storageImageBindingsIndex++;
            }
        }
        // === End ===


        // === Create bindings ===
        DescriptorSetLayoutBinding[] layoutBindings = new DescriptorSetLayoutBinding[module.UniformBindings.Length + storageBindings.Length + imageBindings.Length + storageImageBindings.Length];
        
        for (int i = 0; i < module.UniformBindings.Length; i++)
        {
            var layout = module.UniformBindings[i];
            layoutBindings[i] = layout.LayoutBinding;
        }

        for (int i = 0; i < storageBindings.Length; i++)
        {
            var layout = storageBindings[i];
            layoutBindings[module.UniformBindings.Length + i] = layout.LayoutBinding;
        }

        for (int i = 0; i < imageBindings.Length; i++)
        {
            var layout = imageBindings[i];
            layoutBindings[module.UniformBindings.Length + storageBindings.Length + i] = layout.LayoutBinding;
        }

        for (int i = 0; i < storageImageBindings.Length; i++)
        {
            var layout = storageImageBindings[i];
            layoutBindings[module.UniformBindings.Length + storageBindings.Length + imageBindings.Length + i] = layout.LayoutBinding;
        }

        DescriptorSetLayoutCreateInfo layoutInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = (uint)layoutBindings.Length
        };

        fixed (DescriptorSetLayoutBinding* pLayoutBindings = layoutBindings)
        layoutInfo.PBindings = pLayoutBindings;

        if (GFX.CreateDescriptorSetLayout(&layoutInfo, null, out var descriptorSetLayout) != Silk.NET.Vulkan.Result.Success) {
            throw new InvalidOperationException("failed to create descriptor set layout!");
        }
        // === End ===
        
        UniformBufferAttributes = [];
        UniformBufferBindings = [];

        StorageBufferAttributes = [];
        StorageBufferBindings = [];

        SampledImageAttributes = [];
        SampledImageBindings = [];

        StorageImageAttributes = [];
        StorageImageBindings = [];

        PipelineLayoutCreateInfo pipelineLayoutInfo = new()
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PushConstantRangeCount = 0, // Optional
            PPushConstantRanges = null, // Optional
            PSetLayouts = &descriptorSetLayout
        };

        if (GFX.CreatePipelineLayout(&pipelineLayoutInfo, null, out var pipelineLayout) != Silk.NET.Vulkan.Result.Success) {
            throw new InvalidOperationException("failed to create pipeline layout!");
        }
        

        PipelineShaderStageCreateInfo computeShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.ComputeBit,
            Module = computeModule,
            PName = _mainPtr
        };

        var pipelineInfo = new ComputePipelineCreateInfo
        {
            SType  = StructureType.ComputePipelineCreateInfo,
            Stage  = computeShaderStageInfo,
            Layout = pipelineLayout
        };

        GFX.Vk.CreateComputePipelines(GFX.Device, default, 1, &pipelineInfo, null, out var pipeline);
        GFX.DestroyShaderModule(computeModule);

        module.DescriptorSetLayout = descriptorSetLayout;
        module.PipelineLayout = pipelineLayout;
        module.Pipeline = pipeline;
    }

    public byte[] Compile(string path, ShaderKind kind)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Shader at path '" + path + "' not found");

        var sourceCode = File.ReadAllText(path);
        
        var result = _shaderc.CompileIntoSpv(_compiler, sourceCode, (nuint)sourceCode.Length, kind, Path.GetFileName(path), "main", _options);

        if (_shaderc.ResultGetCompilationStatus(result) != CompilationStatus.Success)
            throw new Exception(_shaderc.ResultGetErrorMessageS(result));

        var length = (int)_shaderc.ResultGetLength(result);
        var bytePtr = _shaderc.ResultGetBytes(result);
        var array = new Span<byte>(bytePtr, length).ToArray();

        _shaderc.ResultRelease(result);

        return array;
    }

    public ShaderData CompileAndReflect(string path, ShaderKind kind)
    {
        var spirV = Compile(path, kind);

        ParsedIr* ir;
        fixed (byte* pSpirV = spirV)
            _cross.ContextParseSpirv(_context, (uint*)pSpirV, (nuint)(spirV.Length / 4), &ir);

        CrossCompiler* compiler;
        _cross.ContextCreateCompiler(_context, Backend.None, ir, CaptureMode.TakeOwnership, &compiler);

        Resources* resources;
        _cross.CompilerCreateShaderResources(compiler, &resources);

        var shaderData = new ShaderData
        {
            SpirV = spirV,
            Kind = kind,
            VertexAttributes = []
        };

        switch (kind)
        {
            case ShaderKind.VertexShader:
                ReflectUniformBufferBindings(compiler, resources, ShaderStageFlags.VertexBit);
                ReflectStorageBufferBindings(compiler, resources, ShaderStageFlags.VertexBit);
                shaderData.VertexAttributes = GetVertexAttributes(compiler, resources);
                break;
            case ShaderKind.FragmentShader:
                ReflectUniformBufferBindings(compiler, resources, ShaderStageFlags.FragmentBit);
                ReflectStorageBufferBindings(compiler, resources, ShaderStageFlags.FragmentBit);
                ReflectSampledImageBindings(SampledImageBindings, SampledImageAttributes, ResourceType.SampledImage, DescriptorType.CombinedImageSampler, compiler, resources, ShaderStageFlags.FragmentBit);
                ReflectSampledImageBindings(StorageImageBindings, StorageImageAttributes, ResourceType.StorageImage, DescriptorType.StorageImage, compiler, resources, ShaderStageFlags.FragmentBit);
                break;
            case ShaderKind.ComputeShader:
                ReflectUniformBufferBindings(compiler, resources, ShaderStageFlags.ComputeBit);
                ReflectStorageBufferBindings(compiler, resources, ShaderStageFlags.ComputeBit);
                ReflectSampledImageBindings(SampledImageBindings, SampledImageAttributes, ResourceType.SampledImage, DescriptorType.CombinedImageSampler, compiler, resources, ShaderStageFlags.ComputeBit);
                ReflectSampledImageBindings(StorageImageBindings, StorageImageAttributes, ResourceType.StorageImage, DescriptorType.StorageImage, compiler, resources, ShaderStageFlags.ComputeBit);
                break;
        }

        return shaderData;
    }

    private VertexAttribute[] GetVertexAttributes(CrossCompiler* compiler, Resources* resources)
    {
        ReflectedResource* inputs;
        nuint inputCount;
        _cross.ResourcesGetResourceListForType(resources, ResourceType.StageInput, &inputs, &inputCount); // get the vertex buffer information

        var attributes = new VertexAttribute[(int)inputCount];

        uint size = 0;

        for (int i = 0; i < (int)inputCount; i++)
        {
            var input = inputs[i];
            var location = _cross.CompilerGetDecoration(compiler, input.Id, Decoration.Location); // get the location
            var name = PtrExt.ToStr(input.Name) ?? throw new InvalidCastException($"[Error] : Unable to get name from input {i}");
            var typeHandle = _cross.CompilerGetTypeHandle(compiler, inputs[i].TypeId);

            attributes[i].Name = name;
            attributes[i].Location = (int)location;
            attributes[i].Format = SPBGTypeToFormat(typeHandle, out var s);
            attributes[i].Offset = s;
        }

        Array.Sort(attributes, (a, b) => a.Location.CompareTo(b.Location));

        for (int i = 0; i < (int)inputCount; i++)
        {
            var s = attributes[i].Offset;
            attributes[i].Offset = size;
            size += s * 4;
        }

        return attributes;
    }

    private Format SPBGTypeToFormat(CrossType* type, out uint size)
    {
        Basetype basetype = _cross.TypeGetBasetype(type);
        size = _cross.TypeGetVectorSize(type);
        
        return (basetype, size) switch
        {
            (Basetype.FP32, 1) => Format.R32Sfloat,
            (Basetype.FP32, 2) => Format.R32G32Sfloat,
            (Basetype.FP32, 3) => Format.R32G32B32Sfloat,
            (Basetype.FP32, 4) => Format.R32G32B32A32Sfloat,
            (Basetype.Int32,   1) => Format.R32Sint,
            (Basetype.Int32,   2) => Format.R32G32Sint,
            (Basetype.Int32,   3) => Format.R32G32B32Sint,
            (Basetype.Int32,   4) => Format.R32G32B32A32Sint,
            _ => throw new InvalidCastException($"Unknown SPIR-V type")
        };
    }

    private void ReflectUniformBufferBindings(CrossCompiler* compiler, Resources* resources, ShaderStageFlags stage)
    {
        ReflectedResource* ubos;
        nuint uboCount;
        _cross.ResourcesGetResourceListForType(resources, ResourceType.UniformBuffer, &ubos, &uboCount);

        for (int i = 0; i < (int)uboCount; i++)
        {
            var binding = _cross.CompilerGetDecoration(compiler, ubos[i].Id, Decoration.Binding);
            var bufferName    = PtrExt.ToStr(_cross.CompilerGetName(compiler, ubos[i].BaseTypeId)) ?? throw new InvalidCastException($"[Error] : Unable to get buffer name for uniform at binding {binding}");
            var name    = PtrExt.ToStr(_cross.CompilerGetName(compiler, ubos[i].Id)) ?? throw new InvalidCastException($"[Error] : Unable to get name for uniform at binding {binding}");

            if (UniformBufferBindings.TryGetValue(binding, out var existing))
            {
                existing.LayoutBinding.StageFlags |= stage;
                UniformBufferBindings[binding] = existing;
            }
            else
            {
                var layout = new UniformBufferLayout()
                {
                    BufferName = bufferName,
                    Name = name,
                    LayoutBinding = new DescriptorSetLayoutBinding()
                    {
                        Binding = binding,
                        DescriptorType = DescriptorType.UniformBuffer,
                        DescriptorCount = 1,
                        StageFlags = stage,
                        PImmutableSamplers = null
                    }
                };

                var typeId = ubos[i].BaseTypeId;
                CrossType* structType = _cross.CompilerGetTypeHandle(compiler, typeId);

                uint memberCount = _cross.TypeGetNumMemberTypes(structType);

                uint size = 0;
                for (uint m = 0; m < memberCount; m++)
                {
                    var memberName   = _cross.CompilerGetMemberName(compiler, typeId, m);
                    var memberOffset = _cross.CompilerGetMemberDecoration(compiler, typeId, m, Decoration.Offset);
                    var uniformName  = PtrExt.ToStr(memberName) ?? throw new InvalidCastException($"[Error] : Unable to get name for uniform value {m} at binding {binding}");

                    nuint memberSize;
                    _cross.CompilerGetDeclaredStructMemberSize(compiler, structType, m, &memberSize);

                    UniformBufferAttributes.Add(new()
                    {
                        Name    = uniformName,
                        Binding = binding,
                        Offset  = memberOffset,
                        Size    = (uint)memberSize
                    });

                    size += (uint)memberSize;
                }

                layout.Size = size;
                UniformBufferBindings[binding] = layout;
            }
        }
    }

    private void ReflectStorageBufferBindings(CrossCompiler* compiler, Resources* resources, ShaderStageFlags stage)
    {
        ReflectedResource* ssbos;
        nuint ssboCount;
        _cross.ResourcesGetResourceListForType(resources, ResourceType.StorageBuffer, &ssbos, &ssboCount);

        for (int i = 0; i < (int)ssboCount; i++)
        {
            var binding = _cross.CompilerGetDecoration(compiler, ssbos[i].Id, Decoration.Binding);
            var name    = PtrExt.ToStr(_cross.CompilerGetName(compiler, ssbos[i].Id)) ?? throw new InvalidCastException($"[Error] : Unable to get name for storage at binding {binding}");

            if (StorageBufferBindings.TryGetValue(binding, out var existing))
            {
                existing.LayoutBinding.StageFlags |= stage;
                StorageBufferBindings[binding] = existing;
            }
            else
            {
                var layout = new StorageBufferLayout()
                {
                    Name = name,
                    LayoutBinding = new DescriptorSetLayoutBinding()
                    {
                        Binding = binding,
                        DescriptorType = DescriptorType.StorageBuffer,
                        DescriptorCount = 1,
                        StageFlags = stage,
                        PImmutableSamplers = null
                    }
                };

                var typeId = ssbos[i].BaseTypeId;
                CrossType* structType = _cross.CompilerGetTypeHandle(compiler, typeId);

                uint memberCount = _cross.TypeGetNumMemberTypes(structType);

                uint size = 0;
                for (uint m = 0; m < memberCount; m++)
                {
                    var memberName   = _cross.CompilerGetMemberName(compiler, typeId, m);
                    var memberOffset = _cross.CompilerGetMemberDecoration(compiler, typeId, m, Decoration.Offset);
                    var uniformName  = PtrExt.ToStr(memberName) ?? throw new InvalidCastException($"[Error] : Unable to get name for storage value {m} at binding {binding}");

                    nuint memberSize;
                    _cross.CompilerGetDeclaredStructMemberSize(compiler, structType, m, &memberSize);

                    StorageBufferAttributes.Add(new()
                    {
                        Name    = uniformName,
                        Binding = binding,
                        Size    = (uint)memberSize
                    });

                    size += (uint)memberSize;
                }

                layout.Size = size;
                StorageBufferBindings[binding] = layout;
            }
        }
    }

    private void ReflectSampledImageBindings(Dictionary<uint, SampledImageLayout> map, List<SampledImageAttribute> attributes, ResourceType ressourceType, DescriptorType descriptorType, CrossCompiler* compiler, Resources* resources, ShaderStageFlags stage)
    {
        ReflectedResource* samplers;
        nuint samplersCount;
        _cross.ResourcesGetResourceListForType(resources, ressourceType, &samplers, &samplersCount);

        for (int i = 0; i < (int)samplersCount; i++)
        {
            
            var binding = _cross.CompilerGetDecoration(compiler, samplers[i].Id, Decoration.Binding);
            var name    = PtrExt.ToStr(_cross.CompilerGetName(compiler, samplers[i].Id)) ?? throw new InvalidCastException($"[Error] : Unable to get name for storage at binding {binding}");

            if (map.TryGetValue(binding, out var existing))
            {
                existing.LayoutBinding.StageFlags |= stage;
                map[binding] = existing;
            }
            else
            {
                var layout = new SampledImageLayout()
                {
                    Name = name,
                    LayoutBinding = new DescriptorSetLayoutBinding()
                    {
                        Binding = binding,
                        DescriptorType = descriptorType,
                        DescriptorCount = 1,
                        StageFlags = stage,
                        PImmutableSamplers = null
                    }
                };

                var typeId = samplers[i].BaseTypeId;
                CrossType* structType = _cross.CompilerGetTypeHandle(compiler, typeId);

                attributes.Add(new()
                {
                    Name    = name,
                    Binding = binding
                });

                map[binding] = layout;
            }
        }
    }

    private void ReflectStorageImageBindings(CrossCompiler* compiler, Resources* resources, ShaderStageFlags stage)
    {
        ReflectedResource* samplers;
        nuint samplersCount;
        _cross.ResourcesGetResourceListForType(resources, ResourceType.StorageImage, &samplers, &samplersCount);

        for (int i = 0; i < (int)samplersCount; i++)
        {
            
            var binding = _cross.CompilerGetDecoration(compiler, samplers[i].Id, Decoration.Binding);
            var name    = PtrExt.ToStr(_cross.CompilerGetName(compiler, samplers[i].Id)) ?? throw new InvalidCastException($"[Error] : Unable to get name for storage at binding {binding}");

            if (SampledImageBindings.TryGetValue(binding, out var existing))
            {
                existing.LayoutBinding.StageFlags |= stage;
                SampledImageBindings[binding] = existing;
            }
            else
            {
                var layout = new SampledImageLayout()
                {
                    Name = name,
                    LayoutBinding = new DescriptorSetLayoutBinding()
                    {
                        Binding = binding,
                        DescriptorType = DescriptorType.StorageImage,
                        DescriptorCount = 1,
                        StageFlags = stage,
                        PImmutableSamplers = null
                    }
                };

                var typeId = samplers[i].BaseTypeId;
                CrossType* structType = _cross.CompilerGetTypeHandle(compiler, typeId);

                SampledImageAttributes.Add(new()
                {
                    Name    = name,
                    Binding = binding
                });

                SampledImageBindings[binding] = layout;
            }
        }
    }

    private static IncludeResult* ResolveInclude(void* userData, byte* requestedSource, int type, byte* requestingSource, nuint includeDepth)
    {
        string requested  = Marshal.PtrToStringAnsi((nint)requestedSource)!;
        string requesting = Marshal.PtrToStringAnsi((nint)requestingSource)!;

        // resolve relative to the requesting shader's directory
        string dir     = Path.GetDirectoryName(requesting) ?? "";
        string fullPath = Path.Combine(dir, requested);

        // fallback to a global includes folder if not found relative
        if (!File.Exists(fullPath))
            fullPath = Path.Combine(Game.ShaderPath, requested);

        var result = (IncludeResult*)Marshal.AllocHGlobal(sizeof(IncludeResult));

        if (File.Exists(fullPath))
        {
            string content = File.ReadAllText(fullPath);
            result->SourceName        = (byte*)Marshal.StringToHGlobalAnsi(fullPath);
            result->SourceNameLength  = (nuint)fullPath.Length;
            result->Content           = (byte*)Marshal.StringToHGlobalAnsi(content);
            result->ContentLength     = (nuint)content.Length;
            result->UserData          = null;
        }
        else
        {
            // return error if not found
            string error = $"Include file not found: {fullPath}";
            result->SourceName        = (byte*)Marshal.StringToHGlobalAnsi("");
            result->SourceNameLength  = 0;
            result->Content           = (byte*)Marshal.StringToHGlobalAnsi(error);
            result->ContentLength     = (nuint)error.Length;
            result->UserData          = null;
        }

        return result;
    }

    private static void ReleaseInclude(void* userData, IncludeResult* result)
    {
        if (result == null) return;
        Marshal.FreeHGlobal((nint)result->SourceName);
        Marshal.FreeHGlobal((nint)result->Content);
        Marshal.FreeHGlobal((nint)result);
    }

    internal void Clear()
    {
        UniformBufferAttributes = [];
        UniformBufferBindings = [];

        StorageBufferAttributes = [];
        StorageBufferBindings = [];

        SampledImageAttributes = [];
        SampledImageBindings = [];

        StorageImageAttributes = [];
        StorageImageBindings = [];
    }

    internal void Dispose()
    {
        _shaderc.CompileOptionsRelease(_options);
        _shaderc.CompilerRelease(_compiler);
        _cross.ContextReleaseAllocations(_context);
    }
}

public struct VertexAttribute
{
    public string Name;
    public int Location;
    public Format Format;
    public uint Offset;
    public VertexAttributeType Type;

    public override string ToString()
    {
        return $"Name: {Name}, Location: {Location}, Format: {Format}, Offset: {Offset}, Type: {Type}";
    }
}

public enum VertexAttributeType
{
    Int, Float, Vec2, iVec2, Vec3, iVec3, Vec4, iVec4
}

public struct UniformBufferLayout
{
    public string BufferName = "";
    public string Name = "";
    public uint Size;
    public DescriptorSetLayoutBinding LayoutBinding;

    public UniformBufferLayout() {}

    public override string ToString()
    {
        return $"BufferName: {BufferName}, Name: {Name}, Size: {Size}, LayoutBinding: {LayoutBinding.Binding}";
    }
}

public struct StorageBufferLayout
{
    public string Name = "";
    public uint Size;
    public DescriptorSetLayoutBinding LayoutBinding;
    public StorageBufferLayout() {}

    public override string ToString()
    {
        return $"Name: {Name}, Size: {Size}, LayoutBinding: {LayoutBinding.Binding}";
    }
}

public struct SampledImageLayout
{
    public string Name = "";
    public uint Size;
    public DescriptorSetLayoutBinding LayoutBinding;
    public SampledImageLayout() {}
}

public struct UniformBufferAttribute
{
    public string Name;
    public uint Binding;
    public uint Index;
    public uint Offset;
    public uint Size;
    public ShaderStageFlags Stage;

    public override string ToString()
    {
        return $"Name: {Name}, Size: {Size}, Binding: {Binding}, Offset: {Offset}, Size: {Size}";
    }
}

public struct StorageBufferAttribute
{
    public string Name;
    public uint Binding;
    public uint Size;
    public ShaderStageFlags Stage;
}

public struct SampledImageAttribute
{
    public string Name;
    public uint Binding;
    public uint Size;
    public ShaderStageFlags Stage;
}