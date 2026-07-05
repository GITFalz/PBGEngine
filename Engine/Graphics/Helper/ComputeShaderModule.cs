using Silk.NET.Vulkan;

namespace PBG.Graphics;

public struct PBGComputeShaderModule
{
    public Dictionary<string, int> Locations = [];
    public UniformBufferAttribute[] UniformAttribues = [];
    public UniformBufferLayout[] UniformBindings = [];
    public DescriptorSetLayout DescriptorSetLayout;
    public PipelineLayout PipelineLayout;
    public Pipeline Pipeline;
    
    public PBGComputeShaderModule() {}
}