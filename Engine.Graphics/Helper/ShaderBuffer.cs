using PBG.Graphics.Vulkan;
using Silk.NET.Vulkan;
using Result = Silk.NET.Vulkan.Result;

public unsafe class ShaderBuffer
{
    private List<DescriptorPool> _descriptorPools = [];
    private bool _started = false;
    private bool _disposed = false;

    public void AllocateDescriptorLayout(DescriptorSetLayout descriptorSetLayout, out DescriptorSet[] descriptorSets, out DescriptorPool descriptorPool)
    {
        var layouts = new DescriptorSetLayout[GFX.MAX_FRAMES_IN_FLIGHT];
        Array.Fill(layouts, descriptorSetLayout);

        descriptorSets = new DescriptorSet[GFX.MAX_FRAMES_IN_FLIGHT];

        descriptorPool = GetDescriptorPool();

        DescriptorSetAllocateInfo allocInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = descriptorPool,
            DescriptorSetCount = GFX.MAX_FRAMES_IN_FLIGHT, 
        };

        fixed(DescriptorSetLayout* pLayouts = layouts)
        allocInfo.PSetLayouts = pLayouts;

        fixed(DescriptorSet* pDescriptorSets = descriptorSets)
        if (GFX.Vk.AllocateDescriptorSets(GFX.Device, &allocInfo, pDescriptorSets) == Result.Success) 
            return;

        // If it doesn't work, memory could be low, so create a new pool
        CreateDescriptorPool();

        descriptorPool = GetDescriptorPool();

        // try again
        allocInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = descriptorPool,
            DescriptorSetCount = GFX.MAX_FRAMES_IN_FLIGHT, 
        };

        fixed(DescriptorSetLayout* pLayouts = layouts)
        allocInfo.PSetLayouts = pLayouts;

        fixed(DescriptorSet* pDescriptorSets = descriptorSets)
        if (GFX.Vk.AllocateDescriptorSets(GFX.Device, &allocInfo, pDescriptorSets) != Result.Success) {
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
            new() { Type = DescriptorType.UniformBuffer,        DescriptorCount = GFX.MAX_FRAMES_IN_FLIGHT * 50 },
            new() { Type = DescriptorType.StorageBuffer,        DescriptorCount = GFX.MAX_FRAMES_IN_FLIGHT * 50 },
            new() { Type = DescriptorType.CombinedImageSampler, DescriptorCount = GFX.MAX_FRAMES_IN_FLIGHT * 50 },
            new() { Type = DescriptorType.StorageImage,         DescriptorCount = GFX.MAX_FRAMES_IN_FLIGHT * 50 },
        };

        DescriptorPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            Flags = DescriptorPoolCreateFlags.FreeDescriptorSetBit,
            PoolSizeCount = 4,
            PPoolSizes = poolSizes,
            MaxSets = GFX.MAX_FRAMES_IN_FLIGHT * 50
        };

        if (GFX.Vk.CreateDescriptorPool(GFX.Device, &poolInfo, null, out var descriptorPool) != Result.Success) {
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

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var descriptorPool in _descriptorPools)
            GFX.Vk.DestroyDescriptorPool(GFX.Device, descriptorPool, null);
        _disposed = false;
    }
}