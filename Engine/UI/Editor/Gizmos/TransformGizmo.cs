using PBG;
using PBG.Data;
using PBG.Editor;
using PBG.MathLibrary;
using PBG.Rendering;


public class TransformGizmo : Gizmo
{
    private float distance = 1;

    public TransformGizmo(Camera camera) : base(camera)
    {
        ibo = new(indices);
        GizmoData[] vertices = new GizmoData[_vertices.Length];
        for (int i = 0; i < _vertices.Length; i++)
        {
            var (position, move, color) = _vertices[i];
            vertices[i] = (position, GetColor(color));
        }   
        vbo = new(vertices);

        count = indices.Length;
    }

    public Vector2 SliderDirection(Axis axis)
    {
        var proj = Mathf.Num(Camera.ProjectionMatrix);
        var view = Mathf.Num(Camera.ViewMatrix);

        Vector3 worldAxis;
        if (EditorManager.WorldSpace)
        {
            worldAxis = axis.HasFlag(Axis.X) ? Vector3.UnitX
                    : axis.HasFlag(Axis.Y) ? Vector3.UnitY
                    :                        Vector3.UnitZ;
        }
        else
        {
            worldAxis = axis.HasFlag(Axis.X) ? Vector3.Transform(Vector3.UnitX, Rotation)
                    : axis.HasFlag(Axis.Y) ? Vector3.Transform(Vector3.UnitY, Rotation)
                    :                        Vector3.Transform(Vector3.UnitZ, Rotation);
        }

        var origin  = Mathf.WorldToScreen(Position, proj, view, Game.Width, Game.Height) ?? (0, 0);
        var tip     = Mathf.WorldToScreen(Position + worldAxis, proj, view, Game.Width, Game.Height) ?? (0, 0);

        return tip - origin;
    }

    public void Update()
    {
        
        if (Hover(out var tris) && Input.IsMousePressed(MouseButton.Left))
        {
            distance = Vector3.Distance(Camera.Position, Position);
            hoveringTriangle = tris.Value;
        }
        
        if (Input.IsMouseDown(MouseButton.Left) && hoveringTriangle != null)
        {
            var triangle = hoveringTriangle.Value;
            
            float sensitivity = 0.004f * distance; // tune this
            void Move(Axis axis, Vector3 dir)
            {
                if (triangle.MoveAxis.HasFlag(axis))
                {
                    Vector2 axisDir = Vector2.Normalize(SliderDirection(axis));
                    // raw dot instead of Sign — proportional to actual mouse speed
                    float movement = Vector2.Dot(Input.MouseDelta, axisDir);

                    if (!EditorManager.WorldSpace)
                        dir = Vector3.Transform(dir, Rotation);

                    Position += dir * movement * sensitivity;
                }
            }
            
            Move(Axis.X, (1, 0, 0));
            Move(Axis.Y, (0, 1, 0));
            Move(Axis.Z, (0, 0, 1));
        }

        if (Input.IsMouseReleased(MouseButton.Left))
        {
            hoveringTriangle = null;
            UpdateScreenSpacePositions = true;
        }
    }

    public override void Bind()
    {
        vbo.Bind();
        ibo.Bind();
    }

    public override void UpdateColor()
    {
        GizmoData[] vertices = new GizmoData[_vertices.Length];
        for (int i = 0; i < _vertices.Length; i++)
        {
            var (position, move, color) = _vertices[i];
            vertices[i] = new(position, GetColor(color) * (hoveringAxis.HasFlag(move) ? 1.2f : 1f));
        }   
        vbo.Update(vertices);
    }

    public override int Count() => count;
    public override uint[] Indices() => indices;
    public override (Vector3 position, Axis move, Axis color)[] Vertices() => _vertices;

    private static uint[] indices =
    [
        0, 1, 2, 2, 3, 0,
        1, 4, 2, 2, 4, 3,
        3, 4, 0, 0, 4, 1,

        5, 6, 7, 7, 8, 5,
        6, 9, 7, 7, 9, 8,
        8, 9, 5, 5, 9, 6,

        10, 11, 12, 12, 13, 10,
        11, 14, 12, 12, 14, 13,
        13, 14, 10, 10, 14, 11,

        15, 16, 17, 17, 18, 15,
        19, 20, 21, 21, 22, 19,

        23, 24, 25, 25, 26, 23,
        27, 28, 29, 29, 30, 27,

        31, 32, 33, 33, 34, 31,
        35, 36, 37, 37, 38, 35
    ];

    private static (Vector3 position, Axis move, Axis color)[] _vertices =
    [
        ((1, -0.03f, 0.03f), Axis.X, Axis.X), ((1, 0.03f, 0.03f), Axis.X, Axis.X), ((1, 0.03f, -0.03f), Axis.X, Axis.X), ((1, -0.03f, -0.03f), Axis.X, Axis.X),
        ((1.2f, 0, 0), Axis.X, Axis.X),

        ((-0.03f, 1, 0.03f), Axis.Y, Axis.Y), ((0.03f, 1, 0.03f), Axis.Y, Axis.Y), ((0.03f, 1, -0.03f), Axis.Y, Axis.Y), ((-0.03f, 1, -0.03f), Axis.Y, Axis.Y),
        ((0, 1.2f, 0), Axis.Y, Axis.Y),

        ((-0.03f, 0.03f, 1), Axis.Z, Axis.Z), ((0.03f, 0.03f, 1), Axis.Z, Axis.Z), ((0.03f, -0.03f, 1), Axis.Z, Axis.Z), ((-0.03f, -0.03f, 1), Axis.Z, Axis.Z),
        ((0, 0, 1.2f), Axis.Z, Axis.Z),

        ((0.35f, 0, 0.35f), Axis.X | Axis.Z, Axis.Y), ((0.35f, 0, 0.55f), Axis.X | Axis.Z, Axis.Y), ((0.55f, 0, 0.55f), Axis.X | Axis.Z, Axis.Y), ((0.55f, 0, 0.35f), Axis.X | Axis.Z, Axis.Y),
        ((0.35f, 0, 0.35f), Axis.X | Axis.Z, Axis.Y), ((0.55f, 0, 0.35f), Axis.X | Axis.Z, Axis.Y), ((0.55f, 0, 0.55f), Axis.X | Axis.Z, Axis.Y), ((0.35f, 0, 0.55f), Axis.X | Axis.Z, Axis.Y),

        ((0.35f, 0.35f, 0), Axis.X | Axis.Y, Axis.Z), ((0.35f, 0.55f, 0), Axis.X | Axis.Y, Axis.Z), ((0.55f, 0.55f, 0), Axis.X | Axis.Y, Axis.Z), ((0.55f, 0.35f, 0), Axis.X | Axis.Y, Axis.Z),
        ((0.35f, 0.35f, 0), Axis.X | Axis.Y, Axis.Z), ((0.55f, 0.35f, 0), Axis.X | Axis.Y, Axis.Z), ((0.55f, 0.55f, 0), Axis.X | Axis.Y, Axis.Z), ((0.35f, 0.55f, 0), Axis.X | Axis.Y, Axis.Z),

        ((0, 0.35f, 0.35f), Axis.Y | Axis.Z, Axis.X), ((0, 0.35f, 0.55f), Axis.Y | Axis.Z, Axis.X), ((0, 0.55f, 0.55f), Axis.Y | Axis.Z, Axis.X), ((0, 0.55f, 0.35f), Axis.Y | Axis.Z, Axis.X),
        ((0, 0.35f, 0.35f), Axis.Y | Axis.Z, Axis.X), ((0, 0.55f, 0.35f), Axis.Y | Axis.Z, Axis.X), ((0, 0.55f, 0.55f), Axis.Y | Axis.Z, Axis.X), ((0, 0.35f, 0.55f), Axis.Y | Axis.Z, Axis.X),
    ];
}
