using PBG;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.Voxel;

[InternalSystemInit(InitPriority.Debug)]
public static class ChunkDebugger
{
    public static Shader ChunkShader = null!;
    public static Descriptor descriptor = null!;

    private static int _projectionLocation = -1;
    private static int _viewLocation = -1;
    private static int _modelLocation = -1;

    public static void Init()
    {
        ShaderInfo shaderInfo = new()
        {
            VertexShaderPath = Game.ShaderPath / "debug" / "chunk.vert",
            FragmentShaderPath = Game.ShaderPath / "debug" / "chunk.frag",
        };

        shaderInfo.Rasterizer.CullMode = Silk.NET.Vulkan.CullModeFlags.FrontBit;
        shaderInfo.InputAssembly.Topology = Silk.NET.Vulkan.PrimitiveTopology.LineList;

        ChunkShader = new(shaderInfo);

        ChunkShader.Compile();

        _projectionLocation = ChunkShader.GetLocation("ubo.proj");
        _viewLocation = ChunkShader.GetLocation("ubo.view");
        _modelLocation = ChunkShader.GetLocation("ubo.model");

        descriptor = ChunkShader.GetDescriptorSet();
    }

    public static void Render(Camera camera)
    {
        ChunkShader.Bind();
        descriptor.Bind();
        descriptor.UniformMatrix4(_projectionLocation, camera.ProjectionMatrix);
        descriptor.UniformMatrix4(_viewLocation, camera.ViewMatrix);
        
        Vector3i camPos = VoxelData.BlockToChunk(camera.Position.floorToInt());
        descriptor.UniformMatrix4(_modelLocation, Matrix4.CreateTranslation(camPos));

        GFX.Draw(36, 1, 0, 0);
    }
}