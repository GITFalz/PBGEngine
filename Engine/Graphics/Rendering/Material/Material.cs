using PBG.Graphics;
using static PBG.Rendering.Mesh;

namespace PBG.Data;

public class Material
{
    private static Dictionary<string, Material> Materials = [];
    public static Material DefaultMaterial = null!;

    public Shader Shader = null!;
    public Descriptor Descriptor = null!;
    public int ModelLocation = -1;
    public int ViewLocation = -1;
    public int ProjectionLocation = -1;
    
    public void Bind()
    {
        Shader.Bind();
    }


    internal static void Init()
    {
        Material material = new();

        var shader = new Shader(new()
        {
            VertexShaderPath = Path.Combine(Game.ShaderPath, "mesh_vulkan", "mesh.vert"),
            FragmentShaderPath = Path.Combine(Game.ShaderPath, "mesh_vulkan", "mesh.frag")
        });
        shader.BindVertexBuffer<MeshVertex>(0);
        shader.Compile();

        material.Shader = shader;
        material.Descriptor = shader.GetDescriptorSet();
        material.ModelLocation = shader.GetLocation("ubo.model");
        material.ViewLocation = shader.GetLocation("ubo.view");
        material.ProjectionLocation = shader.GetLocation("ubo.projection");

        DefaultMaterial = material;
    }

    public Descriptor GetDescriptor() => Shader.GetDescriptorSet();
}