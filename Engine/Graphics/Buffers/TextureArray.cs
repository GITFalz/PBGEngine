using PBG;
using PBG.Graphics;
using Silk.NET.Vulkan;
using StbImageSharp;
using Buffer = Silk.NET.Vulkan.Buffer;

public unsafe class TextureArray : BufferBase
{
    private Image textureImage;
    private DeviceMemory textureImageMemory;

    public ImageView textureImageView;
    public Sampler textureSampler;

    public uint LayerCount { get; private set; }

    public TextureArray(string filePath, int width, int height)
    {
        StbImage.stbi_set_flip_vertically_on_load(1);

        var pixelData = TextureData.SplitTextureAtlasCellSize(Path.Combine(Game.TexturePath, filePath), width, height, true);

        LayerCount = (uint)pixelData.Count;

        ulong layerSize = (ulong)(width * height * 4);
        ulong totalSize = layerSize * LayerCount;

        GFX.CreateBuffer(totalSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer stagingBuffer, out DeviceMemory stagingBufferMemory);

        void* data; 
        GFX.MapMemory(stagingBufferMemory, 0, totalSize, 0, &data);
        for (int i = 0; i < LayerCount; i++)
        {
            ulong offset = layerSize * (ulong)i;
            void* dst = (byte*)data + offset;
            HelperFunctions.MemCpy(pixelData[i], dst, layerSize, layerSize);
        }
        GFX.UnmapMemory(stagingBufferMemory);

        GFX.CreateImageArray((uint)width, (uint)height, LayerCount, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, out textureImage, out textureImageMemory);
    
        GFX.TransitionImageArrayLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, LayerCount);
        GFX.CopyBufferToImageArray(stagingBuffer, textureImage, (uint)width, (uint)height, LayerCount);

        GFX.TransitionImageArrayLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, LayerCount);

        GFX.DestroyBuffer(stagingBuffer);
        GFX.FreeMemory(stagingBufferMemory);

        CreateTextureImageView();
        CreateTextureSampler();
    }

    public TextureArray(List<byte[]> pixelData, int width, int height)
    {
        StbImage.stbi_set_flip_vertically_on_load(1);

        LayerCount = (uint)pixelData.Count;

        ulong layerSize = (ulong)(width * height * 4);
        ulong totalSize = layerSize * LayerCount;

        GFX.CreateBuffer(totalSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out Buffer stagingBuffer, out DeviceMemory stagingBufferMemory);

        void* data; 
        GFX.MapMemory(stagingBufferMemory, 0, totalSize, 0, &data);
        for (int i = 0; i < LayerCount; i++)
        {
            ulong offset = layerSize * (ulong)i;
            void* dst = (byte*)data + offset;
            HelperFunctions.MemCpy(pixelData[i], dst, layerSize, layerSize);
        }
        GFX.UnmapMemory(stagingBufferMemory);

        GFX.CreateImageArray((uint)width, (uint)height, LayerCount, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, out textureImage, out textureImageMemory);
    
        GFX.TransitionImageArrayLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, LayerCount);
        GFX.CopyBufferToImageArray(stagingBuffer, textureImage, (uint)width, (uint)height, LayerCount);

        GFX.TransitionImageArrayLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, LayerCount);

        GFX.DestroyBuffer(stagingBuffer);
        GFX.FreeMemory(stagingBufferMemory);

        CreateTextureImageView();
        CreateTextureSampler();
    }

    public void CreateTextureImageView() 
    {
        textureImageView = GFX.CreateImageView(textureImage, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit, LayerCount);
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