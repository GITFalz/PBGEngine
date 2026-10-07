using PBG.Core;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.UI;

namespace PBG.Nodes;

public class NodeManager : ScriptingNode
{
    private int _count = 0;

    private UIController _ui = null!;
    private NodeModule _nodeModule = null!;
    private NodeConnectionRenderer _connectionRenderer = new();

    void Start()
    {
        _ui = Transform.GetComponent<UIController>();
        _nodeModule = Transform.GetComponent<NodeModule>();

        NodeExpression a = NodeExpression.Constant(1.0f);
        NodeExpression b = NodeExpression.Constant(2.0f);
        NodeExpression c = NodeExpression.Constant(3.0f);

        NodeExpression final = NodeExpression.Multiply(a, NodeExpression.Add(b, c));

        NodeExpression block = NodeExpression.Block(
            final
        );

        NodeExpressionCompileContext context = new();

        block.Compile(context);

        Console.WriteLine("---- TEST GLSL COMPILE ----");
        Console.WriteLine(context.ToString());
        Console.WriteLine("---- END ----");
    }

    void Update()
    {
        if (Input.IsKeyPressed(Key.F))
        {
            NodeCompilation compilation = new(_nodeModule);
            compilation.Sort();
        }

        if (_nodeModule.DoRemakeConnections())
        {
            _connectionRenderer.GenerateLines(_nodeModule);
        }

        if (_nodeModule.DoUpdateConnections())
        {
            _connectionRenderer.UpdateLines(_nodeModule);
        }
    }

    void Render()
    {
        _connectionRenderer.Render(_ui);
    }
}

public static class NodeTypeColors
{
    // ── Core Categories ──────────────────────────────────

    public static readonly Color Default       = new Color("#89898a79");

    /// <summary> Add, Sub, Mult, Div, Power, Modulo, etc. </summary>
    public static readonly Color Math          = new Color("#60a5fa"); 

    /// <summary> Lerp, Clamp, Min, Max, Abs, Sign, Smoothstep, etc. </summary>
    public static readonly Color Utility       = new Color("#a78bfa");

    /// <summary> Perlin, Simplex, Worley, Value Noise, FBM, etc. </summary>
    public static readonly Color Noise         = new Color("#34d399");

    /// <summary> Constant, Seed, Parameter, Slider, etc. </summary>
    public static readonly Color Input         = new Color("#94a3b8");

    /// <summary> Vector Combine/Split, Normalize, Dot, Cross, Length </summary>
    public static readonly Color Vector        = new Color("#fb923c");

    /// <summary> Greater, Less, Equal, Switch, Branch, Boolean ops </summary>
    public static readonly Color Logic         = new Color("#fbbf24");

    /// <summary> Terrain Generator, Heightmap, Domain Warp, etc. </summary>
    public static readonly Color Generator     = new Color("#3b82f6");

    /// <summary> Erosion, Smooth, Blur, Terrace, Clamp Height, etc. </summary>
    public static readonly Color Filter        = new Color("#f472b6");

    /// <summary> Final Output, Heightmap Out, Density Out, Biome Out </summary>
    public static readonly Color Output        = new Color("#f87171");

    /// <summary> World Position, UV, Local Pos, Chunk Coord </summary>
    public static readonly Color Coordinate    = new Color("#22d3ee");

    /// <summary> SDF operations, Density Field, Union, Subtract, etc. </summary>
    public static readonly Color SDF           = new Color("#2ab8d1");
}