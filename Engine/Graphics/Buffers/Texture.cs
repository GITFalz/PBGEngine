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

    private TextureInfo _info;
    private Format _format = Format.R8G8B8A8Srgb;
    
    public bool IsStorageImage => _info.IsStorageImage;

    public Texture(TextureInfo info)
    {
        _info = info;
        if (info.IsStorageImage)
        {
            _format = info.Format;
            GFX.CreateImage((uint)info.Width, (uint)info.Height, info.Format, ImageTiling.Optimal, ImageUsageFlags.StorageBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit, MemoryPropertyFlags.DeviceLocalBit, out textureImage, out textureImageMemory);
            GFX.TransitionImageLayout(textureImage, info.Format, ImageLayout.Undefined, ImageLayout.General);

            CreateStorageImageView();
            CreateTextureSampler();
            return;
        }
        
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
            HelperFunctions.MemCpyTo(texture.Data, data, imageSize, imageSize);
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
        else
        {
            _format = info.Format;
            GFX.CreateImage((uint)_info.Width, (uint)_info.Height, _info.Format, ImageTiling.Optimal, ImageUsageFlags.StorageBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit, MemoryPropertyFlags.DeviceLocalBit, out textureImage, out textureImageMemory);
            GFX.TransitionImageLayout(textureImage, _info.Format, ImageLayout.Undefined, ImageLayout.General);

            CreateTextureImageView();
            CreateTextureSampler();
        }
    }

    public void CreateStorageImageView()
    {
        textureImageView = GFX.CreateImageView(textureImage, _format, ImageAspectFlags.ColorBit);
    }

    public void CreateTextureImageView() 
    {
        textureImageView = GFX.CreateImageView(textureImage, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit);
    }

    public void CreateTextureSampler()
    {
        SamplerCreateInfo samplerInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Nearest,
            MinFilter = Filter.Nearest,

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

    public ImageMemoryBarrier GetMemoryBarrier()
    {
        return new ImageMemoryBarrier
        {
            SType         = StructureType.ImageMemoryBarrier,
            SrcAccessMask = AccessFlags.ShaderWriteBit,
            DstAccessMask = AccessFlags.ShaderReadBit,
            OldLayout     = ImageLayout.General,
            NewLayout     = ImageLayout.General,
            Image         = textureImage,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                LevelCount = 1,
                LayerCount = 1,
            }
        };
    }

    public float[] GetPixels()
    {
        uint imageSize = (uint)(_info.Width * _info.Height * 4 * sizeof(float));

        GFX.CreateBuffer(imageSize, BufferUsageFlags.TransferDstBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer stagingBuffer, out DeviceMemory stagingMemory);

        GFX.TransitionImageLayout(textureImage, _format, ImageLayout.General, ImageLayout.TransferSrcOptimal);

        GFX.CopyImageToBuffer(textureImage, stagingBuffer, (uint)_info.Width, (uint)_info.Height);

        GFX.TransitionImageLayout(textureImage, _format, ImageLayout.TransferSrcOptimal, ImageLayout.General);

        void* data;
        GFX.MapMemory(stagingMemory, 0, imageSize, 0, &data);
        float[] pixels = new float[_info.Width * _info.Height * 4];
        HelperFunctions.MemCpyFrom(data, pixels, imageSize, imageSize);
        GFX.UnmapMemory(stagingMemory);

        GFX.DestroyBuffer(stagingBuffer);
        GFX.FreeMemory(stagingMemory);

        return pixels;
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