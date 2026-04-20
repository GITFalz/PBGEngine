using PBG;
using PBG.Graphics;
using PBG.Graphics.Vulkan;
using Silk.NET.Vulkan;
using StbImageSharp;
using Buffer = Silk.NET.Vulkan.Buffer;

public unsafe class TextureArray : ImageBuffer
{
    public Image TextureImage;
    public DeviceMemory TextureImageMemory;

    public uint LayerCount { get; private set; }

    public TextureArray(TextureInfo info)
    {
        StbImage.stbi_set_flip_vertically_on_load(1);

        var pixelData = TextureData.SplitTextureAtlasCellSize(Path.Combine(Game.TexturePath, info.FilePath), info.Width, info.Height, true);

        LayerCount = (uint)pixelData.Count;

        ulong layerSize = (ulong)(info.Width * info.Height * 4);
        ulong totalSize = layerSize * LayerCount;

        GFX.CreateBuffer(totalSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer stagingBuffer, out DeviceMemory stagingBufferMemory);

        void* data; 
        GFX.MapMemory(stagingBufferMemory, 0, totalSize, 0, &data);
        for (int i = 0; i < LayerCount; i++)
        {
            ulong offset = layerSize * (ulong)i;
            void* dst = (byte*)data + offset;
            HelperFunctions.MemCpyTo(pixelData[i], dst, layerSize, layerSize);
        }
        GFX.UnmapMemory(stagingBufferMemory);

        GFX.CreateImageArray((uint)info.Width, (uint)info.Height, LayerCount, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, out TextureImage, out TextureImageMemory);
    
        GFX.TransitionImageArrayLayout(TextureImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, LayerCount);
        GFX.CopyBufferToImageArray(stagingBuffer, TextureImage, (uint)info.Width, (uint)info.Height, LayerCount);

        GFX.TransitionImageArrayLayout(TextureImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, LayerCount);

        GFX.DestroyBuffer(stagingBuffer);
        GFX.FreeMemory(stagingBufferMemory);

        CreateTextureImageView(Format.R8G8B8A8Srgb);
        CreateTextureSampler(info);
    }

    public TextureArray(List<byte[]> pixelData, TextureInfo info)
    {
        StbImage.stbi_set_flip_vertically_on_load(1);

        LayerCount = (uint)pixelData.Count;

        ulong layerSize = (ulong)(info.Width * info.Height * 4);
        ulong totalSize = layerSize * LayerCount;

        GFX.CreateBuffer(totalSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer stagingBuffer, out DeviceMemory stagingBufferMemory);

        void* data; 
        GFX.MapMemory(stagingBufferMemory, 0, totalSize, 0, &data);
        for (int i = 0; i < LayerCount; i++)
        {
            ulong offset = layerSize * (ulong)i;
            void* dst = (byte*)data + offset;
            HelperFunctions.MemCpyTo(pixelData[i], dst, layerSize, layerSize);
        }
        GFX.UnmapMemory(stagingBufferMemory);

        GFX.CreateImageArray((uint)info.Width, (uint)info.Height, LayerCount, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, out TextureImage, out TextureImageMemory);
    
        GFX.TransitionImageArrayLayout(TextureImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, LayerCount);
        GFX.CopyBufferToImageArray(stagingBuffer, TextureImage, (uint)info.Width, (uint)info.Height, LayerCount);

        GFX.TransitionImageArrayLayout(TextureImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, LayerCount);

        GFX.DestroyBuffer(stagingBuffer);
        GFX.FreeMemory(stagingBufferMemory);

        CreateTextureImageView(Format.R8G8B8A8Srgb);
        CreateTextureSampler(info);
    }
    
    public TextureArray(uint layerCount, TextureInfo info)
    {
        StbImage.stbi_set_flip_vertically_on_load(1);

        LayerCount = (uint)layerCount;

        GFX.CreateImageArray((uint)info.Width, (uint)info.Height, LayerCount, Format.R8G8B8A8Unorm, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit | ImageUsageFlags.StorageBit, MemoryPropertyFlags.DeviceLocalBit, out TextureImage, out TextureImageMemory);

        CreateTextureImageView(Format.R8G8B8A8Unorm);
        CreateTextureSampler(info);
    }
    

    public void CreateTextureImageView(Format format) 
    {
        ImageView = GFX.CreateImageView(TextureImage, format, ImageAspectFlags.ColorBit, LayerCount);
    }

    public void CreateTextureSampler(TextureInfo info)
    {
        SamplerCreateInfo samplerInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = info.Filter,
            MinFilter = info.Filter,

            AddressModeU = info.SamplerMode,
            AddressModeV = info.SamplerMode,
            AddressModeW = info.SamplerMode,

            AnisotropyEnable = false,
        };

        PhysicalDeviceProperties properties = new();
        GFX.GetPhysicalDeviceProperties(&properties);

        samplerInfo.MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy;

        samplerInfo.BorderColor = BorderColor.FloatTransparentBlack;
        samplerInfo.UnnormalizedCoordinates = false;

        samplerInfo.CompareEnable = false;
        samplerInfo.CompareOp = CompareOp.Always;

        samplerInfo.MipmapMode = SamplerMipmapMode.Linear;
        samplerInfo.MipLodBias = 0.0f;
        samplerInfo.MinLod = 0.0f;
        samplerInfo.MaxLod = 0.0f;

        if (GFX.CreateSampler(&samplerInfo, null, out Sampler) != Result.Success) {
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
            Image         = TextureImage,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                LevelCount = 1,
                LayerCount = 1,
            }
        };
    }

    protected override void Destroy()
    {
        OnDispose?.Invoke(this);
        OnDispose = null;
        
        GFX.DestroySampler(Sampler);
        GFX.DestroyImageView(ImageView);

        GFX.DestroyImage(TextureImage);
        GFX.FreeMemory(TextureImageMemory);
    }
}