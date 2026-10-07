using Silk.NET.Vulkan;

namespace PBG.Graphics;

public abstract class ImageBuffer : BufferBase
{
    public ImageView ImageView;
    public Sampler Sampler;
}