using Silk.NET.Shaderc;
using Silk.NET.Vulkan;
using Silk.NET.SPIRV.Cross;
using ShadercCompiler = Silk.NET.Shaderc.Compiler;
using CrossCompiler = Silk.NET.SPIRV.Cross.Compiler;
using Silk.NET.SPIRV;

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

    public ShaderCompiler()
    {
        _shaderc = Shaderc.GetApi();
        _compiler = _shaderc.CompilerInitialize();
        _options = _shaderc.CompileOptionsInitialize();
        _cross = Cross.GetApi();

        Context* context;
        _cross.ContextCreate(&context);
        _context = context;
    }

    public struct ShaderData
    {
        public byte[] SpirV;
        public ShaderKind Kind;
        public VertexAttribute[] VertexAttributes;
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

    public ShaderData CompileToGlslAndReflect(string path, ShaderKind kind, out string glsl)
    {
        var spirV = Compile(path, kind);
        
        ParsedIr* ir;
        fixed (byte* pSpirV = spirV)
        {
            var result = _cross.ContextParseSpirv(_context, (uint*)pSpirV, (nuint)(spirV.Length / 4), &ir);
        }
        
        CrossCompiler* compiler;
        var compilerResult = _cross.ContextCreateCompiler(_context, Backend.None, ir, CaptureMode.Copy, &compiler);
        
        Resources* resources;
        var resourcesResult = _cross.CompilerCreateShaderResources(compiler, &resources);
        
        var vertexAttributes = GetVertexAttributes(compiler, resources);
        
        switch (kind)
        {
            case ShaderKind.VertexShader:
                ReflectUniformBufferBindings(compiler, resources, ShaderStageFlags.VertexBit);
                ReflectStorageBufferBindings(compiler, resources, ShaderStageFlags.VertexBit);
                break;
            case ShaderKind.FragmentShader:
                ReflectUniformBufferBindings(compiler, resources, ShaderStageFlags.FragmentBit);
                ReflectStorageBufferBindings(compiler, resources, ShaderStageFlags.FragmentBit);
                ReflectSampledImageBindings(compiler, resources, ShaderStageFlags.FragmentBit);
                break;
        }
        
        CrossCompiler* glslCompiler;
        var glslResult = _cross.ContextCreateCompiler(_context, Backend.Glsl, ir, CaptureMode.TakeOwnership, &glslCompiler);
        
        byte* source;
        var compileResult = _cross.CompilerCompile(glslCompiler, &source);
        
        glsl = new string((sbyte*)source);
                
        return new ShaderData
        {
            SpirV = spirV,
            Kind = kind,
            VertexAttributes = kind == ShaderKind.VertexShader ? vertexAttributes : []
        };
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

        var vertexAttributes = GetVertexAttributes(compiler, resources);

        switch (kind)
        {
            case ShaderKind.VertexShader:
                ReflectUniformBufferBindings(compiler, resources, ShaderStageFlags.VertexBit);
                ReflectStorageBufferBindings(compiler, resources, ShaderStageFlags.VertexBit);
                break;
            case ShaderKind.FragmentShader:
                ReflectUniformBufferBindings(compiler, resources, ShaderStageFlags.FragmentBit);
                ReflectStorageBufferBindings(compiler, resources, ShaderStageFlags.FragmentBit);
                ReflectSampledImageBindings(compiler, resources, ShaderStageFlags.FragmentBit);
                break;
        }

        return new ShaderData
        {
            SpirV = spirV,
            Kind = kind,
            VertexAttributes = kind == ShaderKind.VertexShader ? vertexAttributes : []
        };
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
            attributes[i].Format = SpvTypeToFormat(typeHandle, out var s);
            attributes[i].Offset = size;

            size += s * 4;
        }

        return attributes;
    }

    private Format SpvTypeToFormat(CrossType* type, out uint size)
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

    private void ReflectSampledImageBindings(CrossCompiler* compiler, Resources* resources, ShaderStageFlags stage)
    {
        ReflectedResource* samplers;
        nuint samplersCount;
        _cross.ResourcesGetResourceListForType(resources, ResourceType.SampledImage, &samplers, &samplersCount);

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
                        DescriptorType = DescriptorType.CombinedImageSampler,
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