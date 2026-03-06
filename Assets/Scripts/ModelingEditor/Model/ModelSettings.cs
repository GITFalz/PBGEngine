using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;

public static class ModelSettings
{
    /*
    public static ShaderProgram VertexShader = new ShaderProgram("model/modelVertex.vert", "model/modelVertex.frag");
    public static int vertexModelLocation = VertexShader.GetLocation("model");
    public static int vertexViewLocation = VertexShader.GetLocation("view");
    public static int vertexProjectionLocation = VertexShader.GetLocation("projection");
    
    public static ShaderProgram EdgeShader = new ShaderProgram("model/modelEdge.vert", "model/modelEdge.frag");
    public static int edgeModelLocation = EdgeShader.GetLocation("model");
    public static int edgeViewLocation = EdgeShader.GetLocation("view");
    public static int edgeProjectionLocation = EdgeShader.GetLocation("projection");
    */

    public static Camera? Camera;

    public static RenderType RenderType = RenderType.Vertex;

    // Debug
    public static bool RenderScreenSpacePositions = false;

    // Ui values
    public static float MeshAlpha = 1.0f;
    public static bool WireframeVisible = true;
    public static bool BackfaceCulling = true;
    public static bool Snapping = false;
    public static bool GridAligned = false;
    public static float SnappingFactor = 1;
    public static int SnappingFactorIndex = 0;
    public static Vector3 SnappingOffset = new Vector3(0, 0, 0);
    public static bool IsLocalMode = false;

    public static Vector3i Mirror
    {
        get => _mirror;
        set
        {
            _mirror = value;
            _mirrorIndex = _mirror.X + _mirror.Y * 2 + _mirror.Z * 4;
        }
    }
    private static Vector3i _mirror = (0, 0, 0);
    private static int _mirrorIndex = 0;

    public static Vector3i Axis = (1, 1, 1);
    public static Vector3[] Mirrors => Mirroring[_mirrorIndex];
    public static bool[] Swaps => Swapping[Mirror];
    public static readonly Vector3[][] Mirroring =
    [
        [(1, 1, 1)],
        [(1, 1, 1), (-1, 1, 1)],
        [(1, 1, 1), (1, -1, 1)],
        [(1, 1, 1), (-1, 1, 1), (-1, -1, 1), (1, -1, 1)],
        [(1, 1, 1), (1, 1, -1)],
        [(1, 1, 1), (-1, 1, 1), (-1, 1, -1), (1, 1, -1)],
        [(1, 1, 1), (1, -1, 1), (1, -1, -1), (1, 1, -1)],
        [(1, 1, 1), (-1, 1, 1), (-1, -1, 1), (1, -1, 1), (-1, 1, -1), (1, 1, -1), (1, -1, -1), (-1, -1, -1)]
    ];

    public static readonly Dictionary<Vector3i, bool[]> Swapping = new Dictionary<Vector3i, bool[]>()
    {
        { (0, 0, 0), [false] },
        { (1, 0, 0), [false, true] },
        { (0, 1, 0), [false, true] },
        { (0, 0, 1), [false, true] },
        { (1, 1, 0), [false, true, false, true] },
        { (1, 0, 1), [false, true, false, true] },
        { (0, 1, 1), [false, true, false, true] },
        { (1, 1, 1), [false, true, false, true, false, true, false, true] }
    };
}