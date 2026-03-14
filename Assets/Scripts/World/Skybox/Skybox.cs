using PBG.MathLibrary;
using PBG;
using PBG.Core;
using PBG.Graphics;

public class Skybox : ScriptingNode
{
    private static Shader _skyboxShader;
    private static SkyboxMesh _skyboxMesh = new SkyboxMesh();
    private static bool _started = false;

    private static int sml = -1;
    private static int svl = -1;
    private static int spl = -1;
    private static int sd = -1;
    private static int sn = -1;
    private static int ld = -1;
    private static int tl = -1;

    private Descriptor _descriptor;
    public Vector3 Day = new Vector3(0.2f, 0.2f, 0.2f);
    public Vector3 Night = new Vector3(0.2f, 0.2f, 0.2f);
    public Vector3 LightDirection = new Vector3(0, -1, 0);
    public float Time = 0f;

    static Skybox()
    {

    }

    void Start()
    {
        if (!_started)
        {
            ShaderInfo info = new()
            {
                VertexShaderPath = Game.ShaderPath / "skybox_vulkan/skybox.vert", 
                FragmentShaderPath = Game.ShaderPath / "skybox_vulkan/skybox.frag"
            };
            info.DepthStencil.DepthWriteEnable = false;
            info.DepthStencil.DepthCompareOp = Silk.NET.Vulkan.CompareOp.LessOrEqual;

            _skyboxShader = new(info);
            _skyboxShader.BindVertexBuffer<Vector3>(0);
            _skyboxShader.BindVertexBuffer<Vector2>(1);
            _skyboxShader.BindVertexBuffer<int>(2);
            _skyboxShader.Compile();

            sml = _skyboxShader.GetLocation("ubo.model");
            svl = _skyboxShader.GetLocation("ubo.view");
            spl = _skyboxShader.GetLocation("ubo.projection");
            sd = _skyboxShader.GetLocation("fubo.uDay");
            sn = _skyboxShader.GetLocation("fubo.uNight");
            ld = _skyboxShader.GetLocation("fubo.uLightDirection");
            tl = _skyboxShader.GetLocation("fubo.time");
        }

        _descriptor = _skyboxShader.GetDescriptorSet();
    }

    void Render()
    {
        _skyboxShader.Bind();
        _descriptor.Bind();

        Matrix4 model = Matrix4.CreateTranslation(Camera.Position);
        Matrix4 view = Camera.GetViewMatrix();
        Matrix4 projection = Camera.ProjectionMatrix;

        _descriptor.UniformMatrix4(sml, model);
        _descriptor.UniformMatrix4(svl, view);
        _descriptor.UniformMatrix4(spl, projection);
        _descriptor.Uniform3(sd, Day);
        _descriptor.Uniform3(sn, Night);
        _descriptor.Uniform3(ld, LightDirection);
        _descriptor.Uniform1(tl, Time);

        _skyboxMesh.Render();
    }
}