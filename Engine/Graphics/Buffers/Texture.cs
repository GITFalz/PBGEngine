using PBG.Graphics;
using Silk.NET.Vulkan;
using StbImageSharp;
using Buffer = Silk.NET.Vulkan.Buffer;

public unsafe class Texture : BufferBase
{
    private Image textureImage;
    private DeviceMemory textureImageMemory;

    public ImageView textureImageView;
    public Sampler textureSampler;

    public Texture(TextureInfo info)
    {
        if (info.FilePath != null)
        {
            StbImage.stbi_set_flip_vertically_on_load(1);

            ImageResult texture;

            using (var stream = File.OpenRead(info.FilePath))
            {
                texture = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            }

            ulong imageSize = (ulong)(texture.Width * texture.Height * 4);
            GFX.CreateBuffer(imageSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer stagingBuffer, out DeviceMemory stagingBufferMemory);

            void* data; 
            GFX.MapMemory(stagingBufferMemory, 0, imageSize, 0, &data);
            HelperFunctions.MemCpy(texture.Data, data, imageSize, imageSize);
            GFX.UnmapMemory(stagingBufferMemory);

            GFX.CreateImage((uint)texture.Width, (uint)texture.Height, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, out textureImage, out textureImageMemory);
        
            GFX.TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
            GFX.CopyBufferToImage(stagingBuffer, textureImage, (uint)texture.Width, (uint)texture.Height);

            GFX.TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

            GFX.DestroyBuffer(stagingBuffer);
            GFX.FreeMemory(stagingBufferMemory);

            CreateTextureImageView();
            CreateTextureSampler();
        }   
    }

    public void CreateTextureImageView() 
    {
        textureImageView = GFX.CreateImageView(textureImage, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit, 1);
    }

    public void CreateTextureSampler()
    {
        SamplerCreateInfo samplerInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,

            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,

            AnisotropyEnable = true,
        };

        PhysicalDeviceProperties properties = new();
        GFX.GetPhysicalDeviceProperties(&properties);

        samplerInfo.MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy;

        samplerInfo.BorderColor = BorderColor.IntOpaqueBlack;
        samplerInfo.UnnormalizedCoordinates = false;

        samplerInfo.CompareEnable = false;
        samplerInfo.CompareOp = CompareOp.Always;

        samplerInfo.MipmapMode = SamplerMipmapMode.Linear;
        samplerInfo.MipLodBias = 0.0f;
        samplerInfo.MinLod = 0.0f;
        samplerInfo.MaxLod = 0.0f;

        if (GFX.CreateSampler(&samplerInfo, null, out textureSampler) != Result.Success) {
            throw new InvalidOperationException("failed to create texture sampler!");
        }
    }

    protected override void Destroy()
    {
        GFX.DestroySampler(textureSampler);
        GFX.DestroyImageView(textureImageView);

        GFX.DestroyImage(textureImage);
        GFX.FreeMemory(textureImageMemory);
    }
}

public enum TextureType
{
    MipMap,           // For 3D world textures (uses mipmaps)
    Linear,              // For UI elements (linear filtering, clamp to edge)
    Nearest   // For pixel art UI (nearest filtering)
}