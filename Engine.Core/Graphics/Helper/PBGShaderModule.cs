using Silk.NET.Vulkan;

namespace PBG.Graphics;

public struct PBGShaderModule
{
    public Dictionary<string, int> Locations = [];
    public UniformBufferAttribute[] UniformAttribues = [];
    public UniformBufferLayout[] UniformBindings = [];
    public DescriptorSetLayout DescriptorSetLayout;
    public PipelineLayout PipelineLayout;
    public Pipeline Pipeline;
    
    public PBGShaderModule() {}
}