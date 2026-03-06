using PBG.Graphics;
using Silk.NET.SPIRV.Cross;
using Silk.NET.Vulkan;
using Result = Silk.NET.Vulkan.Result;

public unsafe class ShaderBuffer
{
    private List<DescriptorPool> _descriptorPools = [];
    private bool _started = false;
    private bool _disposed = false;

    public GraphicsContext _context;

    public ShaderBuffer(GraphicsContext context)
    {
        _context = context;
    }

    public void AllocateDescriptorLayout(DescriptorSetLayout descriptorSetLayout, out DescriptorSet[] descriptorSets, out DescriptorPool descriptorPool)
    {
        var layouts = new DescriptorSetLayout[GraphicsContext.MAX_FRAMES_IN_FLIGHT];
        Array.Fill(layouts, descriptorSetLayout);

        descriptorSets = new DescriptorSet[GraphicsContext.MAX_FRAMES_IN_FLIGHT];

        descriptorPool = GetDescriptorPool();

        DescriptorSetAllocateInfo allocInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = descriptorPool,
            DescriptorSetCount = GraphicsContext.MAX_FRAMES_IN_FLIGHT, 
        };

        fixed(DescriptorSetLayout* pLayouts = layouts)
        allocInfo.PSetLayouts = pLayouts;

        fixed(DescriptorSet* pDescriptorSets = descriptorSets)
        if (_context.vk.AllocateDescriptorSets(_context.device, &allocInfo, pDescriptorSets) == Result.Success) 
            return;

        // If it doesn't work, memory could be low, so create a new pool
        CreateDescriptorPool();

        descriptorPool = GetDescriptorPool();

        // try again
        allocInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = descriptorPool,
            DescriptorSetCount = GraphicsContext.MAX_FRAMES_IN_FLIGHT, 
        };

        fixed(DescriptorSetLayout* pLayouts = layouts)
        allocInfo.PSetLayouts = pLayouts;

        fixed(DescriptorSet* pDescriptorSets = descriptorSets)
        if (_context.vk.AllocateDescriptorSets(_context.device, &allocInfo, pDescriptorSets) != Result.Success) {
            // If that doesn't work idk bro :/
            throw new InvalidOperationException("failed to allocate descriptor sets! layout might be too big");
        }
    }

    private DescriptorPool GetDescriptorPool()
    {
        if (_descriptorPools.Count == 0)
            CreateDescriptorPool();

        return _descriptorPools[^1];
    }

    private void CreateDescriptorPool()
    {
        var poolSizes = stackalloc DescriptorPoolSize[]
        {
            new() { Type = DescriptorType.UniformBuffer,        DescriptorCount = GraphicsContext.MAX_FRAMES_IN_FLIGHT * 50 },
            new() { Type = DescriptorType.StorageBuffer,        DescriptorCount = GraphicsContext.MAX_FRAMES_IN_FLIGHT * 50 },
            new() { Type = DescriptorType.CombinedImageSampler, DescriptorCount = GraphicsContext.MAX_FRAMES_IN_FLIGHT * 50 },
            new() { Type = DescriptorType.StorageImage,         DescriptorCount = GraphicsContext.MAX_FRAMES_IN_FLIGHT * 50 },
        };

        DescriptorPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            Flags = DescriptorPoolCreateFlags.FreeDescriptorSetBit,
            PoolSizeCount = 4,
            PPoolSizes = poolSizes,
            MaxSets = GraphicsContext.MAX_FRAMES_IN_FLIGHT * 50
        };

        if (_context.vk.CreateDescriptorPool(_context.device, &poolInfo, null, out var descriptorPool) != Result.Success) {
            throw new InvalidOperationException("failed to create descriptor pool!");
        }

        _descriptorPools.Add(descriptorPool);
    }

    internal void Init()
    {
        if (_started) return;
        CreateDescriptorPool(); // Create the first Descriptor pool
        _started = true;
    }

    private void Destroy()
    {
        if (_disposed) return;
        foreach (var descriptorPool in _descriptorPools)
            _context.vk.DestroyDescriptorPool(_context.device, descriptorPool, null);
        _disposed = false;
    }

    internal static void Dispose()
    {
        GraphicsContext.graphicsContext.shaderBuffer.Destroy();
    }
}