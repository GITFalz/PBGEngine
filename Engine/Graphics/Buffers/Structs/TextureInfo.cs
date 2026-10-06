using Silk.NET.Vulkan;

namespace PBG.Graphics;

public struct TextureInfo
{
    public string FilePath = "";
    public bool IsStorageImage = false;
    public int Width;
    public int Height;
    public bool UseMipMaps = false;
    public Format Format = Format.R32G32B32A32Sfloat;
    public Filter Filter {
        set {
            MagFilter = value;
            MinFilter = value;
        }
    }
    public Filter MagFilter = Filter.Linear;
    public Filter MinFilter = Filter.Linear;
    public SamplerAddressMode SamplerMode = SamplerAddressMode.Repeat;
    
    public TextureInfo(string filePath, int width, int height)
    {
        FilePath = filePath;
        Width = width;
        Height = height;
    }
}