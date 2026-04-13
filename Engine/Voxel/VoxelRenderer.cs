
using System.Diagnostics.CodeAnalysis;
using PBG;
using PBG.Core;
using PBG.Data;
using PBG.Graphics;
using PBG.Mathematics;
using PBG.Rendering;
using PBG.Voxel;

[SystemInit(InitPriority.Data)]
public class VoxelRenderer : ScriptingNode, IVoxelRenderer
{
    public static Shader VoxelShader = null!;
    public static int ViewLocation = -1;
    public static int ProjectionLocation = -1;

    public Dictionary<Vector3i, VoxelChunk> ActiveChunks = [];
    public List<VoxelChunk> Chunks = [];

    public List<VoxelMesh> VoxelMeshes = [];

    public Scene Scene;

    public VoxelRenderer(Scene scene)
    {
        Scene = scene;
    }

    public static void Init()
    {
        ShaderInfo info = new()
        {
            VertexShaderPath = VoxelEngine.ShaderPath / "voxel" / "voxel.vert",
            FragmentShaderPath = VoxelEngine.ShaderPath / "voxel" / "voxel.frag"
        };
        VoxelShader = new(info);
        VoxelShader.Compile();

        ViewLocation = VoxelShader.GetLocation("ubo.view");
        ProjectionLocation = VoxelShader.GetLocation("ubo.proj");
    }

    public bool GetChunk(Vector3i position, [NotNullWhen(true)] out VoxelChunk? chunk) => ActiveChunks.TryGetValue(position, out chunk);
    public Block GetBlock(Vector3i position) => GetBlock(position, out var block) ? block.Value : Block.Air;
    public bool GetBlock(Vector3i position, [NotNullWhen(true)] out Block? block)
    {
        block = null;
        if (!GetChunk(VoxelData.BlockToChunk(position), out var chunk))
            return false;

        block = chunk.Get(VoxelData.BlockToRelative(position));
        return true;
    }

    public void UpdateUniforms(Descriptor descriptor)
    {
        descriptor.Uniform(ViewLocation, Scene.Camera.ViewMatrix);
        descriptor.Uniform(ProjectionLocation, Scene.Camera.ProjectionMatrix);
    }

    public Camera GetCamera() => Scene.Camera;

    public void Awake()
    {
        Test();
    }

    public void Test()
    {
        VoxelChunk chunk = new(this, (0, 0, 0));
        for (int x = 0; x < 32; x++)
        {
            for (int z = 0; z < 32; z++)
            {
                chunk.Set(new Block(BlockState.Solid, 0), x, 0, z);
            }
        }

        Chunks.Add(chunk);
        ActiveChunks.Add(chunk.Position, chunk);

        VoxelChunkGenerator.GenerateIndirectMesh(chunk);
        if (!chunk.HasMesh())
            return;

        if (VoxelMeshes.Count == 0)
            VoxelMeshes.Add(new());

        VoxelMesh? voxelMesh = null;
        for (int i = 0; i < VoxelMeshes.Count; i++)
        {
            var mesh = VoxelMeshes[i];
            if (mesh.TryAllocate(chunk))
            {
                voxelMesh = mesh;
                break;
            }
        }

        if (voxelMesh == null)
        {
            voxelMesh = new();
            VoxelMeshes.Add(voxelMesh);
            if (!voxelMesh.TryAllocate(chunk))
                throw new Exception("[Error] : Vertex data to large for any vertex mesh with a size of " + chunk.VertexCount);
        }

        voxelMesh.Update(chunk);
    }

    public void Render()
    {
        for (int i = 0; i < Chunks.Count; i++)
        {
            var chunk = Chunks[i];
            if (!Scene.Camera.FrustumIntersectsSphere(chunk.Center, 28))
                continue;

            chunk.Allocation.Mesh.UpdateDrawCommand(chunk, chunk.Allocation);
        }
        
        for (int i = 0; i < VoxelMeshes.Count; i++)
        {
            VoxelMeshes[i].UpdateDrawCommands();
        }

        VoxelShader.Bind();
        
        for (int i = 0; i < VoxelMeshes.Count; i++)
        {
            VoxelMeshes[i].Render(this);
        }
    }

    public void Dispose()
    {
        for (int i = 0; i < VoxelMeshes.Count; i++)
        {
            VoxelMeshes[i].Dispose();
        }
    }
}