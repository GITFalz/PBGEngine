using Silk.NET.Vulkan;

namespace PBG.Graphics.Vulkan;

public struct QueueFamilyIndices
{
    public uint? GraphicsFamily;
    public uint? PresentFamily;
    public readonly bool IsComplete => GraphicsFamily.HasValue && PresentFamily.HasValue;
}

public struct SwapChainSupportDetails 
{
    public SurfaceCapabilitiesKHR Capabilities;
    public SurfaceFormatKHR[] Formats = [];
    public PresentModeKHR[] PresentModes = [];
    public SwapChainSupportDetails() {}
};