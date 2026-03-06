using PBG.Graphics;
using Silk.NET.Vulkan;

namespace PBG.UI
{
    public class UIData
    {
        public static UIData PixelPerfectUI = new UIData(TextureType.Nearest);
        public static UIData LinearUI = new UIData(TextureType.Linear);

        public Shader UiShader;
        public TextureArray UiTexture;
        public TextureArray IconTexture;
        public TextureArray ItemTexture;
        public Shader TextShader;
        public TextureArray TextTexture;

        public int modelLoc = -1;
        public int projectionLoc = -1;

        public int textModelLoc = -1;
        public int textProjectionLoc = -1;
        public int textTimeLoc = -1;
        
        public UIData(TextureType textureType)
        {
            
            UiShader = new Shader(new() { VertexShaderPath = Path.Combine(Game.ShaderPath, "ui_vulkan/ui.vert"), FragmentShaderPath = Path.Combine(Game.ShaderPath, "ui_vulkan/ui.frag") });
            UiShader.Compile();

            UiTexture = new TextureArray(new("UITextures.png", 64, 64) { Filter = Filter.Nearest});
            IconTexture = new TextureArray(new("Icons.png", 64, 64) { Filter = Filter.Nearest});
            ItemTexture = new TextureArray(new("Icons.png", 64, 64) { Filter = Filter.Nearest});

            //ItemTexture = ItemDataManager.Image;
            TextShader = new Shader(new() { VertexShaderPath = Path.Combine(Game.ShaderPath, "text_vulkan/text.vert"), FragmentShaderPath = Path.Combine(Game.ShaderPath, "text_vulkan/text.frag") });
            TextShader.Compile();

            TextTexture = new TextureArray(new("TextAtlas.png", 14, 18) { Filter = Filter.Linear, SamplerMode = SamplerAddressMode.ClampToEdge }); 

            modelLoc = UiShader.GetLocation("ubo.model");
            projectionLoc = UiShader.GetLocation("ubo.projection");

            textModelLoc = TextShader.GetLocation("ubo.model");
            textProjectionLoc = TextShader.GetLocation("ubo.projection");
            textTimeLoc = TextShader.GetLocation("ubo.time");
        }

        public Descriptor GetUiDescriptor()
        {
            var descriptor = UiShader.GetDescriptorSet();
            descriptor.BindTextureArray(UiTexture, 3);
            descriptor.BindTextureArray(IconTexture, 4);
            descriptor.BindTextureArray(ItemTexture, 5);
            return descriptor;
        }

        public Descriptor GetTextDescriptor()
        {
            var descriptor = TextShader.GetDescriptorSet();
            descriptor.BindTextureArray(TextTexture, 4);     
            return descriptor;
        }
    }
}