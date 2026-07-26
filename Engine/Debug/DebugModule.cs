

using System.Diagnostics;
using PBG;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;
using Buffer = Silk.NET.Vulkan.Buffer;

[InternalSystemInit(InitPriority.Shader)]
public class DebugModule
{
    public static Shader GridShader = null!;
    public static int GridModelLocation = -1;
    public static int GridViewLocation = -1;
    public static int GridProjectionLocation = -1;

    public Descriptor GridDescriptor = null!;

    public List<Vector3> GridVertices = [];
    public List<Vector3> GridColors = [];
    public List<uint> GridIndices = [];

    public VBO<Vector3> GridVertexVBO;
    public VBO<Vector3> GridColorVBO;
    public IBO GridIBO;

    public Buffer[] GridBuffers = [];
    public ulong[] GridOffsets = [];
    public uint GridIndexCount = 0;

    public DebugModule()
    {
        GridDescriptor = GridShader.GetDescriptorSet();
    }

    public static void Init()
    {
        ShaderInfo gridShaderInfo = new()
        {
            VertexShaderPath = Game.ShaderPath / "debug" / "lines.vert",
            FragmentShaderPath = Game.ShaderPath / "debug" / "lines.frag"
        };

        gridShaderInfo.InputAssembly.Topology = Silk.NET.Vulkan.PrimitiveTopology.LineList;

        GridShader = new(gridShaderInfo);

        GridShader.BindVertexBuffer<Vector3>(0);
        GridShader.BindVertexBuffer<Vector3>(1);

        GridShader.Compile();

        GridModelLocation = GridShader.GetLocation("ubo.model");
        GridViewLocation = GridShader.GetLocation("ubo.view");
        GridProjectionLocation = GridShader.GetLocation("ubo.proj");
    }

    private Vector3 To3D(Vector2 p, DebugAxis axis)
    {
        return axis switch
        {
            DebugAxis.X => new Vector3(0, p.X, p.Y),
            DebugAxis.Y => new Vector3(p.X, 0, p.Y),
            DebugAxis.Z => new Vector3(p.X, p.Y, 0),
            _ => new Vector3(p.X, p.Y, 0)
        };
    }

    public void AddGrid(Vector3 origin, Vector3 direction, Vector3 normal, Vector2 size, Vector2 gridCellSize, Vector2 offset, Vector3 color, IncludedBorder includedBorder = IncludedBorder.All)
{
    Vector3 n = Vector3.Normalize(normal);

    Vector3 u = direction - n * Vector3.Dot(direction, n);
    if (u.LengthSquared < 1e-8f)
    {
        Vector3 fallback = MathF.Abs(Vector3.Dot(n, Vector3.UnitX)) < 0.99f ? Vector3.UnitX : Vector3.UnitY;
        u = fallback - n * Vector3.Dot(fallback, n);
    }
    u = Vector3.Normalize(u);
    Vector3 v = Vector3.Normalize(Vector3.Cross(n, u));
    if (Vector3.Dot(v, Vector3.UnitY) < 0)
        v.Y = -v.Y;

    if (Vector3.Dot(v, Vector3.UnitZ) < 0)
        v.Z = -v.Z;

    Vector3 To3D(Vector2 p) => origin + u * p.X + v * p.Y;

    Vector2 min = Vector2.Zero;
    Vector2 max = size; // exact local rectangle, no projection distortion

    Vector2 offsetA = (0, 0);
    Vector2 offsetB = (0, 0);

    if (includedBorder == IncludedBorder.None)
    {
        offsetA.X = gridCellSize.X;
        offsetA.Y = gridCellSize.X;
        offsetB.X = gridCellSize.Y;
        offsetB.Y = gridCellSize.Y;
    }
    else
    {
        if (!includedBorder.HasFlag(IncludedBorder.Left))
            offsetA.X = gridCellSize.X;

        if (!includedBorder.HasFlag(IncludedBorder.Right))
            offsetA.Y = gridCellSize.X;

        if (!includedBorder.HasFlag(IncludedBorder.Top))
            offsetB.X = gridCellSize.Y;

        if (!includedBorder.HasFlag(IncludedBorder.Bottom))
            offsetB.Y = gridCellSize.Y;
    }

    Console.WriteLine(offsetA + " " + offsetB);

    for (float x = min.X + offsetA.X + offset.X; x <= max.X - offsetA.Y; x += gridCellSize.X)
    {
        Vector3 p1 = To3D(new Vector2(x, min.Y));
        Vector3 p2 = To3D(new Vector2(x, max.Y));

        uint idx1 = (uint)GridVertices.Count;
        GridVertices.Add(p1);
        GridVertices.Add(p2);
        GridColors.Add(color);
        GridColors.Add(color);
        GridIndices.Add(idx1);
        GridIndices.Add(idx1 + 1);
    }

    for (float y = min.Y + offsetB.X + offset.Y; y <= max.Y - offsetB.Y; y += gridCellSize.Y)
    {
        Vector3 p1 = To3D(new Vector2(min.X, y));
        Vector3 p2 = To3D(new Vector2(max.X, y));

        uint idx1 = (uint)GridVertices.Count;
        GridVertices.Add(p1);
        GridVertices.Add(p2);
        GridColors.Add(color);
        GridColors.Add(color);
        GridIndices.Add(idx1);
        GridIndices.Add(idx1 + 1);
    }
}

    public void Generate()
    {
        GridVertexVBO = new([.. GridVertices]);
        GridColorVBO = new([.. GridColors]);

        Console.WriteLine(GridVertices.Count + " " + GridColors.Count + " sgefsfsefsefssfefsfesfesf");
        GridIBO = new([.. GridIndices]);

        GridBuffers = [GridVertexVBO.Buffer, GridColorVBO.Buffer];
        GridOffsets = [0, 0];

        GridIndexCount = (uint)GridIndices.Count;

        GridVertices = [];
        GridColors = [];
        GridIndices = [];
    }

    public void Render(Camera camera, Vector3 position)
    {
        GridShader.Bind();
        GridDescriptor.Bind();

        GridDescriptor.Uniform(GridModelLocation, Matrix4.CreateTranslation(position));
        GridDescriptor.Uniform(GridViewLocation, camera.ViewMatrix);
        GridDescriptor.Uniform(GridProjectionLocation, camera.ProjectionMatrix);

        VBOBase.Bind(GridBuffers, GridOffsets);
        GridIBO.Bind();
        GFX.DrawIndexed(GridIndexCount, 1, 0, 0, 0);
    }
}

public enum DebugAxis
{
    X,
    Y,
    Z
}

[Flags]
public enum IncludedBorder
{
    None   = 0,       // 0000  - no borders
    Left   = 1 << 0,  // 0001
    Right  = 1 << 1,  // 0010
    Top    = 1 << 2,  // 0100
    Bottom = 1 << 3,  // 1000
    All    = Left | Right | Top | Bottom // 1111
}