using PBG.Graphics;
using Silk.NET.Vulkan;

namespace PBG.UI
{
    public class UIData
    {
        public static UIData PixelPerfectUI;
        public static UIData LinearUI;
        private static bool _started = false;

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

        public static void Init()
        {
            if (!_started)
            {
                PixelPerfectUI = new UIData(TextureType.Nearest);
                LinearUI = new UIData(TextureType.Linear);
                _started = false;
            }
        }
        
        public UIData(TextureType textureType)
        {   
            ShaderInfo uiInfo = new() 
            { 
                VertexShaderPath = Path.Combine(Game.ShaderPath, "ui_vulkan/ui.vert"), 
                FragmentShaderPath = Path.Combine(Game.ShaderPath, "ui_vulkan/ui.frag") 
            };

            uiInfo.ColorBlendAttachment.BlendEnable = true;

            uiInfo.ColorBlendAttachment.SrcColorBlendFactor = BlendFactor.SrcAlpha;
            uiInfo.ColorBlendAttachment.DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha;
            uiInfo.ColorBlendAttachment.ColorBlendOp = BlendOp.Add;

            uiInfo.ColorBlendAttachment.SrcAlphaBlendFactor = BlendFactor.One;
            uiInfo.ColorBlendAttachment.DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha;
            uiInfo.ColorBlendAttachment.AlphaBlendOp = BlendOp.Add;

            UiShader = new Shader(uiInfo);
            UiShader.Compile();

            UiTexture = new TextureArray(new("UITextures.png", 64, 64) { Filter = Filter.Nearest});
            IconTexture = new TextureArray(new("Icons.png", 64, 64) { Filter = Filter.Nearest});
            ItemTexture = ItemDataManager.Image;

            ShaderInfo textInfo = new() 
            { 
                VertexShaderPath = Path.Combine(Game.ShaderPath, "text_vulkan/text.vert"), 
                FragmentShaderPath = Path.Combine(Game.ShaderPath, "text_vulkan/text.frag")
            };
            
            textInfo.ColorBlendAttachment.BlendEnable = true;

            textInfo.ColorBlendAttachment.SrcColorBlendFactor = BlendFactor.SrcAlpha;
            textInfo.ColorBlendAttachment.DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha;
            textInfo.ColorBlendAttachment.ColorBlendOp = BlendOp.Add;

            textInfo.ColorBlendAttachment.SrcAlphaBlendFactor = BlendFactor.One;
            textInfo.ColorBlendAttachment.DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha;
            textInfo.ColorBlendAttachment.AlphaBlendOp = BlendOp.Add;

            TextShader = new Shader(textInfo);
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
            descriptor.BindTextureArray(UiTexture, 4);
            descriptor.BindTextureArray(IconTexture, 5);
            descriptor.BindTextureArray(ItemTexture, 6);
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