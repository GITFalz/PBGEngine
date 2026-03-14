using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using PBG;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.Voxel;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public static class ItemDataManager
{
    public static Dictionary<string, ItemData> AllItems = [];
    public static List<ItemData> Items = [];

    public static int BlockCount = 0;
    public static int WeaponCount = 0;

    public static Shader BlocksShader;
    public static Descriptor BlocksDescriptor;

    public static int BlockViewLocation;
    public static int BlockProjectionLocation;
    public static int BlockModelLocation;

    public static int BlockLightDirectionLocation;

    private static ComputeShader? _textureArrayWrite;
    private static Descriptor _textureWriteDescriptor;

    private static int _textureArraySizeLocation = -1;
    private static int _textureArrayLayerLocation = -1;

    public static FBO FBO;

    public static TextureArray Image = null!;

    public static List<byte[]> Data = new List<byte[]>();

    public static int CubeModelLocation = -1;  
    public static int CubeProjectionLocation = -1;
    public static int CubeIndicesLocation = -1;

    private static bool _started = false;

    public static void Init()
    {
        if (_started)
            return;

        FBO = new FBO(128, 128);
    }

    static ItemDataManager()
    {
        
    }

    public static void GenerateIcons()
    {
        if (_started)
            return;
    
        _started = true;

        uint imageCount = 0;
        foreach (var (_, item) in AllItems)
        {
            if (item is BlockItemData blockItemData)
                imageCount++;
        }

        /*
        foreach (var (_, item) in AllItems)
        {
            if (item is WeaponItemData)
                imageCount++;
        }
        */

        Image = new(imageCount, new() { Width = 128, Height = 128 });

        _textureArrayWrite ??= new ComputeShader(new() { ComputeShaderPath = Game.ShaderPath / "computeShaders/textureArrayWrite.comp"});
        _textureArrayWrite.Compile();

        _textureArraySizeLocation = _textureArrayWrite.GetLocation("ubo.size");
        _textureArrayLayerLocation = _textureArrayWrite.GetLocation("ubo.layer");
        int _textureArrayOutlineRadiusLocation = _textureArrayWrite.GetLocation("ubo.outlineRadius");
        int _textureArrayOutlineColorLocation = _textureArrayWrite.GetLocation("ubo.outlineColor");

        _textureWriteDescriptor = _textureArrayWrite.GetDescriptorSet();
        _textureWriteDescriptor.BindFramebufferColor(FBO, 0);
        _textureWriteDescriptor.BindTextureArray(Image, 1, DescriptorType.StorageImage, ImageLayout.General);

        _textureWriteDescriptor.Uniform(_textureArrayOutlineRadiusLocation, 6);
        _textureWriteDescriptor.Uniform(_textureArrayOutlineColorLocation, new Vector4(0, 0, 0, 1));

        BlocksShader = new Shader(new(Game.ShaderPath / "world_vulkan/world_base.vert", Game.ShaderPath / "world_vulkan/world_base.frag"));
        BlocksShader.BindVertexBuffer<BlockVertexData>(0);
        BlocksShader.Compile();
        BlocksDescriptor = BlocksShader.GetDescriptorSet();

        BlockViewLocation = BlocksShader.GetLocation("ubo.uView");
        BlockProjectionLocation = BlocksShader.GetLocation("ubo.uProjection");
        BlockModelLocation = BlocksShader.GetLocation("ubo.uModel");

        BlockLightDirectionLocation = BlocksShader.GetLocation("fubo.lightDirection");

        Data.Clear();

        BlocksDescriptor.BindTextureArray(BlockData.BlockTextureArray, 2);

        GFX.TransitionImageArrayLayout(Image.TextureImage, Format.R8G8B8A8Unorm, ImageLayout.Undefined, ImageLayout.General, Image.LayerCount);

        try
        {    
            Matrix4 model = 
                Matrix4.CreateTranslation(64, 64, 0) * 
                Matrix4.CreateScale(64) * 
                Matrix4.CreateRotationX(Mathf.DegreesToRadians(45 + 90)) * 
                Matrix4.CreateRotationY(Mathf.DegreesToRadians(45 + 180));
                
            Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0, 128, 0, 128, -64, 64);
            Matrix4 view = Matrix4.Identity;

            BlocksDescriptor.UniformMatrix4(BlockModelLocation, model);
            BlocksDescriptor.UniformMatrix4(BlockViewLocation, view); 
            BlocksDescriptor.UniformMatrix4(BlockProjectionLocation, projection);
            BlocksDescriptor.Uniform3(BlockLightDirectionLocation, new Vector3(-1, 1, 1) * 2f);

            int i = 0;
            foreach (var (_, item) in AllItems)
            {
                if (item is BlockItemData blockItemData)
                {    
                    var cmd = GFX.BeginSingleTimeCommands();

                    BlocksShader.Bind(cmd);
                    BlocksDescriptor.Bind(cmd);

                    FBO.Reset();
                    FBO.Bind(cmd);
                    GFX.Viewport(cmd, 0, 0, 128, 128);

                    blockItemData.GenerateIcon(cmd);
                    
                    FBO.Unbind(cmd);

                    _textureArrayWrite.Bind(cmd);

                    _textureWriteDescriptor.Uniform(_textureArraySizeLocation, new Vector2i(128, 128));
                    _textureWriteDescriptor.Uniform(_textureArrayLayerLocation, i);

                    _textureWriteDescriptor.Bind(cmd, PipelineBindPoint.Compute);

                    _textureArrayWrite.DispatchBarrier(cmd, _textureWriteDescriptor, 16, 16, 1);

                    GFX.EndSingleTimeCommands(cmd);

                    i++;
                }
            }

            foreach (var (_, item) in AllItems)
            {
                if (item is WeaponItemData)
                {
                    /*
                    FBO.Bind();

                    item.GenerateIcon();

                    FBO.Unbind();

                    FBO.Clear();
                    */
                }   
            }

            //Image = new(Data, new() { Width = 128, Height = 128 });
        }

        catch (Exception ex)
        {
            Console.WriteLine($"[Critical Error] : Failed to generate icons: {ex.Message}");
            Console.WriteLine($"[Stack Trace] : {ex.StackTrace}");
            throw; // Re-throw to maintain original behavior
        }

        GFX.TransitionImageArrayLayout(Image.TextureImage, GFX.SwapChainFormat, ImageLayout.General, ImageLayout.ShaderReadOnlyOptimal, Image.LayerCount);
        
        GFX.Viewport(0, 0, Game.Width, Game.Height);
    }

    public static void ForeachBlockItems(Action<BlockItemData> action)
    {
        foreach (var (_, item) in AllItems)
        {
            if (item is BlockItemData blockItem)
            {
                action(blockItem);
            }
        }
    }
}
