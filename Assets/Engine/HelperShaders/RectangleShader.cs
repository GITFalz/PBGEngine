using PBG.Graphics;

namespace PBG.Helper;

[Shader("Utils_vulkan/Rectangle.vert", "editor/Image.frag")]
public static class ImageShader
{
    public static Shader Shader;

    [Uniform("ubo.model")] public static int Model;
    [Uniform("ubo.projection")] public static int Projection;
    [Uniform("ubo.size")] public static int Size;
    [Uniform("fubo.saturation")] public static int Saturation;
}