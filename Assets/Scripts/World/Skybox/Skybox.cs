using PBG.MathLibrary;
using PBG;
using PBG.Core;
using PBG.Graphics;

public class Skybox : ScriptingNode
{
    //private static ShaderProgram _skyboxShader = new ShaderProgram("skybox/skybox.vert", "skybox/skybox.frag");
    private static SkyboxMesh _skyboxMesh = new SkyboxMesh();

    private static int sml = -1;
    private static int svl = -1;
    private static int spl = -1;
    private static int sc = -1;
    private static int ld = -1;

    public Vector3 Color = new Vector3(0.2f, 0.2f, 0.2f);
    public Vector3 LightDirection = new Vector3(0, -1, 0);

    static Skybox()
    {
        /*
        _skyboxShader.Bind();

        sml = _skyboxShader.GetLocation("model");
        svl = _skyboxShader.GetLocation("view");
        spl = _skyboxShader.GetLocation("projection");
        sc = _skyboxShader.GetLocation("uColor");
        ld = _skyboxShader.GetLocation("uLightDirection");

        _skyboxShader.Unbind();
        */
    }

    void Render()
    {
        /*
        GL.Enable(EnableCap.CullFace);
        GL.DepthFunc(DepthFunction.Lequal);
        GL.DepthMask(false);

        _skyboxShader.Bind();

        Matrix4 model = Matrix4.CreateTranslation(Scene.DefaultCamera.Position);
        Matrix4 view = Scene.DefaultCamera.ViewMatrix;
        Matrix4 projection = Scene.DefaultCamera.ProjectionMatrix;

        GL.UniformMatrix4(sml, false, ref model);
        GL.UniformMatrix4(svl, false, ref view);
        GL.UniformMatrix4(spl, false, ref projection);
        GL.Uniform3(sc, Color);
        GL.Uniform3(ld, LightDirection);

        _skyboxMesh.Render();
        Shader.Error("Skybox rendering error: ");
        
        _skyboxShader.Unbind();

        GL.DepthMask(true);
        GL.DepthFunc(DepthFunction.Less);
        */
    }
}