using PBG.Core;
using PBG.Data;
using PBG.MathLibrary;
using Silk.NET.Vulkan;

namespace PBG.Graphics;

public class BoundingBoxRenderer : ScriptingNode
{
    public static Shader Shader;
    public static bool _started = false;

    private static int _modelLocation;
    private static int _viewLocation;
    private static int _projectionLocation;

    public Descriptor Descriptor;
    public SSBO<BoundingBoxData> SSBO;
    public uint ElementCount = 0;

    void Start()
    {
        if (!_started)
        {
            ShaderInfo bbinfo = new() { 
                VertexShaderPath = Game.ShaderPath / "StructureEditor_vulkan/structure/boundingBox.vert",
                FragmentShaderPath = Game.ShaderPath / "StructureEditor_vulkan/structure/boundingBox.frag" 
            };
            //bbinfo.DepthStencil.DepthTestEnable = false;
            bbinfo.DepthStencil.DepthWriteEnable = false;
            
            bbinfo.ColorBlendAttachment.BlendEnable = true;

            bbinfo.ColorBlendAttachment.SrcColorBlendFactor = BlendFactor.SrcAlpha;
            bbinfo.ColorBlendAttachment.DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha;
            bbinfo.ColorBlendAttachment.ColorBlendOp = BlendOp.Add;

            bbinfo.ColorBlendAttachment.SrcAlphaBlendFactor = BlendFactor.One;
            bbinfo.ColorBlendAttachment.DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha;
            bbinfo.ColorBlendAttachment.AlphaBlendOp = BlendOp.Add;

            Shader = new Shader(bbinfo);
            Shader.Compile();    

            _modelLocation = Shader.GetLocation("ubo.model");
            _viewLocation = Shader.GetLocation("ubo.view");
            _projectionLocation = Shader.GetLocation("ubo.projection");

            _started = true;
        }

        SSBO = new(0);

        Descriptor = Shader.GetDescriptorSet();
        Descriptor.BindSSBO(SSBO, 0);
        Descriptor.Uniform(_modelLocation, Matrix4.Identity);
        Descriptor.Uniform(_projectionLocation, Camera.ProjectionMatrix);
    }

    public void UpdateBoundingBoxes(BoundingBoxData[] boundingBoxes)
    {
        if (boundingBoxes.Length > SSBO.ElementCount || boundingBoxes.Length < SSBO.ElementCount * 0.5f)
        {
            SSBO.Renew(boundingBoxes);
            Descriptor.BindSSBO(SSBO, 0);
        }
        else
        {   
            SSBO.Update(boundingBoxes);
        }

        ElementCount = (uint)boundingBoxes.Length;
    }

    void Resize()
    {
        Descriptor.Uniform(_modelLocation, Matrix4.Identity);
        Descriptor.Uniform(_projectionLocation, Camera.ProjectionMatrix);
    }

    void Render()
    {
        if (ElementCount == 0)
            return;
            
        Shader.Bind();
        Descriptor.Bind();
        Descriptor.UniformMatrix4(_modelLocation, Matrix4.CreateTranslation(Transform.Position));
        Descriptor.UniformMatrix4(_projectionLocation, Camera.ProjectionMatrix);
        Descriptor.UniformMatrix4(_viewLocation, Camera.ViewMatrix);
        
        GFX.Draw(ElementCount * 36, 1, 0, 0);
    }
}

public struct BoundingBoxData
{
    public Vector3 Position;
    private readonly float p1;
    public Vector3 Size;
    private readonly float p2;
    public Vector4 Color;
}