using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using PBG;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;

public struct GizmoData(Vector3 position, Vector3 color)
{
    public Vector3 Position = position;
    public Vector3 Color = color;

    public static implicit operator GizmoData((Vector3 position, Vector3 color) data) => new(data.position, data.color);
}

public struct GizmoTriangle(Vector2 a, Vector2 b, Vector2 c, int info)
{
    Vector2 A = a;
    Vector2 B = b;
    Vector2 C = c;
    public int Info = info;

    public int GetAxis() => System.Numerics.BitOperations.TrailingZeroCount(Info >> 3);
    public bool InTriangle() => Mathf.PointInTriangle(Input.GetMousePosition(), A, B, C);
}

public abstract class Gizmo(Camera camera, Matrix4 projection)
{
    /*
    private static ShaderProgram _gizmoShader = new ShaderProgram("gizmo/gizmo.vert", "gizmo/gizmo.frag");
    private static int _gizmoModelLocation = _gizmoShader.GetLocation("model");
    private static int _gizmoViewLocation = _gizmoShader.GetLocation("view");
    private static int _gizmoProjectionLocation = _gizmoShader.GetLocation("projection");
    */

    protected static int stride = Marshal.SizeOf(typeof(GizmoData));

    public Vector3 Position = Vector3.Zero;
    public Quaternion Rotation = Quaternion.Identity;

    public List<GizmoTriangle> Triangles = [];
    public Camera Camera = camera;
    public Matrix4 Projection = projection;
    public Matrix4 ModelMatrix => Matrix4.CreateScale(Vector3.Distance(Camera.Position, Position) * 0.15f) * Matrix4.CreateFromQuaternion(Rotation) * Matrix4.CreateTranslation(Position);
    protected int _hoveringInfo = 0;

    public static Vector4[] GizmoColor = [
        (0.88f, 0.33f, 0.33f, 1.0f), // X - softer red
        (0.45f, 0.78f, 0.45f, 1.0f), // Y - soft green
        (0.38f, 0.55f, 0.88f, 1.0f)  // Z - soft blue
    ];

    public abstract void Bind();
    public abstract void Unbind();
    public abstract int Count();
    public abstract void UpdateColor();
    public abstract uint[] Indices();
    public abstract (Vector3 position, int info)[] Vertices();

    public bool Hover() => Hover(out _);
    public bool Hover([NotNullWhen(true)] out GizmoTriangle? tris)
    {
        tris = null;
        for (int i = 0; i < Triangles.Count; i++)
        {
            var triangle = Triangles[i];
            if (triangle.InTriangle())
            {
                tris = triangle;
                if (_hoveringInfo != triangle.Info)
                {
                    _hoveringInfo = triangle.Info;
                    UpdateColor();
                }
                return true;
            }
        }
        if (_hoveringInfo != 0)
        {
            _hoveringInfo = 0;
            UpdateColor();
        }
        return false;
    }

    public Vector3 GetColorMultiplier(int color) => (
        1 + (color & 1) * 0.2f,
        1 + ((color >> 1) & 1) * 0.2f,
        1 + ((color >> 2) & 1) * 0.2f
    );

    public void GenerateWorldSpacePoints()
    {
        Triangles = [];
        Matrix4 model = ModelMatrix;
        var proj = Mathf.Num(Projection);
        var view = Mathf.Num(Camera.ViewMatrix);

        var indices = Indices();
        var vertices = Vertices();
        for (int i = 0; i < indices.Length; i += 3)
        {
            var a = indices[i];
            var b = indices[i + 1];
            var c = indices[i + 2];

            (Vector3 position, int info) vertA;
            (Vector3 position, int info) vertB;
            (Vector3 position, int info) vertC;

            vertA = vertices[(int)a];
            vertB = vertices[(int)b];
            vertC = vertices[(int)c];
            
            var tvertA = Mathf.WorldToScreen((new Vector4(vertA.position, 1f) * model).Xyz, proj, view, Game.Width - 400, Game.Height - 50) ?? (0, 0);
            var tvertB = Mathf.WorldToScreen((new Vector4(vertB.position, 1f) * model).Xyz, proj, view, Game.Width - 400, Game.Height - 50) ?? (0, 0);
            var tvertC = Mathf.WorldToScreen((new Vector4(vertC.position, 1f) * model).Xyz, proj, view, Game.Width - 400, Game.Height - 50) ?? (0, 0);

            Triangles.Add(new(tvertA + (200, 50), tvertB + (200, 50), tvertC + (200, 50), vertA.info));
        }
    }

    public void Render()
    {
        /*
        _gizmoShader.Bind();

        Matrix4 model = ModelMatrix;
        GL.UniformMatrix4(_gizmoModelLocation, false, ref model);
        GL.UniformMatrix4(_gizmoViewLocation, false, ref Camera.ViewMatrix);
        GL.UniformMatrix4(_gizmoProjectionLocation, false, ref Projection);

        Bind();

        GL.DrawElements(PrimitiveType.Triangles, Count(), DrawElementsType.UnsignedInt, 0);
        Shader.Error("Gizmo error: ");

        Unbind();

        _gizmoShader.Unbind();

        GL.Enable(EnableCap.DepthTest);
        */
    }


    public static Vector3 GetColor(int info)
    {
        int count = 0;
        Vector3 color = Vector3.Zero;

        if ((info & 1) == 1)
        {
            count++;
            color += GizmoColor[0].Xyz;
        }
        if (((info >> 1) & 1) == 1)
        {
            count++;
            color += GizmoColor[1].Xyz;
        }
        if (((info >> 2) & 1) == 1)
        {
            count++;
            color += GizmoColor[2].Xyz;
        }

        if (count == 0) return (0, 0, 0);
        if (count == 1) return color;
        return color / (float)count;
    }

    protected Vector3 GetCameraScaleVector() => new Vector3(Mathf.SignNo0(-Camera.front.X), Mathf.SignNo0(-Camera.front.Y), Mathf.SignNo0(-Camera.front.Z));
}

public class TransformGizmo(Camera camera, Matrix4 projection) : Gizmo(camera, projection)
{
    //private static VAO _vao = new VAO();
    private static IBO _ibo;
    private static VBO<GizmoData> _vbo;
    private static int _count = 0;

    static TransformGizmo()
    {
        /*
        _ibo = new(indices);
        List<GizmoData> vertices = [];
        for (int i = 0; i < _vertices.Length; i++)
        {
            var (position, info) = _vertices[i];
            vertices.Add((position, GetColor(info)));
        }   
        _vbo = new(vertices);

        _count = indices.Length;

        _vao.Bind();
        _vbo.Bind();

        _vao.Link(0, 3, VertexAttribPointerType.Float, stride, 0);
        _vao.IntLink(1, 3, VertexAttribIntegerType.Int, stride, 3 * sizeof(float));

        _vbo.Unbind();
        _vao.Unbind();
        */
    }

    public Vector2 SliderDirection(int axis)
    {
        Matrix4 model = ModelMatrix;
        Vector3 end = axis switch
        {
            0 => _vertices[4].position,
            1 => _vertices[9].position,
            2 => _vertices[14].position,
            _ => Vector3.Zero
        };

        var proj = Mathf.Num(Projection);
        var view = Mathf.Num(Camera.ViewMatrix);

        var tvertA = Mathf.WorldToScreen((new Vector4(0f, 0f, 0f, 1f) * model).Xyz, proj, view, Game.Width - 400, Game.Height - 50) ?? (0, 0);
        var tvertB = Mathf.WorldToScreen((new Vector4(end, 1f) * model).Xyz, proj, view, Game.Width - 400, Game.Height - 50) ?? (0, 0);

        return tvertB - tvertA;
    }

    public override void Bind()
    {
        //_vao.Bind();
        //_ibo.Bind();
    }
    public override void Unbind()
    {
        //_ibo.Unbind();
        //_vao.Unbind();
    }
    public override void UpdateColor()
    {
        List<GizmoData> vertices = [];
        for (int i = 0; i < _vertices.Length; i++)
        {
            var (position, info) = _vertices[i];
            var type = info >> 3;
            vertices.Add((position, GetColor(info) * ((type & (_hoveringInfo >> 3)) == type ? 1.2f : 1f)));
        }   
        //_vbo.Update(vertices);
    }
    public override int Count() => _count;
    public override uint[] Indices() => indices;
    public override (Vector3 position, int info)[] Vertices() => _vertices;

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

    private static (Vector3 position, int info)[] _vertices =
    [
        ((1, -0.03f, 0.03f), 0b001001), ((1, 0.03f, 0.03f), 0b001001), ((1, 0.03f, -0.03f), 0b001001), ((1, -0.03f, -0.03f), 0b001001),
        ((1.2f, 0, 0), 0b001001),

        ((-0.03f, 1, 0.03f), 0b010010), ((0.03f, 1, 0.03f), 0b010010), ((0.03f, 1, -0.03f), 0b010010), ((-0.03f, 1, -0.03f), 0b010010),
        ((0, 1.2f, 0), 0b010010),

        ((-0.03f, 0.03f, 1), 0b100100), ((0.03f, 0.03f, 1), 0b100100), ((0.03f, -0.03f, 1), 0b100100), ((-0.03f, -0.03f, 1), 0b100100),
        ((0, 0, 1.2f), 0b100100),

        ((0.35f, 0, 0.35f), 0b101010), ((0.35f, 0, 0.55f), 0b101010), ((0.55f, 0, 0.55f), 0b101010), ((0.55f, 0, 0.35f), 0b101010),
        ((0.35f, 0, 0.35f), 0b101010), ((0.55f, 0, 0.35f), 0b101010), ((0.55f, 0, 0.55f), 0b101010), ((0.35f, 0, 0.55f), 0b101010),

        ((0.35f, 0.35f, 0), 0b011100), ((0.35f, 0.55f, 0), 0b011100), ((0.55f, 0.55f, 0), 0b011100), ((0.55f, 0.35f, 0), 0b011100),
        ((0.35f, 0.35f, 0), 0b011100), ((0.55f, 0.35f, 0), 0b011100), ((0.55f, 0.55f, 0), 0b011100), ((0.35f, 0.55f, 0), 0b011100),

        ((0, 0.35f, 0.35f), 0b110001), ((0, 0.35f, 0.55f), 0b110001), ((0, 0.55f, 0.55f), 0b110001), ((0, 0.55f, 0.35f), 0b110001),
        ((0, 0.35f, 0.35f), 0b110001), ((0, 0.55f, 0.35f), 0b110001), ((0, 0.55f, 0.55f), 0b110001), ((0, 0.35f, 0.55f), 0b110001),
    ];
}

public class RotationGizmo(Camera camera, Matrix4 projection) : Gizmo(camera, projection)
{
    //private static VAO _vao = new VAO();
    private static IBO _ibo;
    private static VBO<GizmoData> _vbo;
    private static int _count = 0;

    static RotationGizmo()
    {
        /*
        GenerateArc();

        _ibo = new(_indices);
        List<GizmoData> vertices = [];
        for (int i = 0; i < _vertices.Length; i++)
        {
            var (position, info) = _vertices[i];
            vertices.Add((position, GetColor(info)));
        }
        _vbo = new(vertices);

        _count = _indices.Length;

        _vao.Bind();
        _vbo.Bind();

        _vao.Link(0, 3, VertexAttribPointerType.Float, stride, 0);
        _vao.IntLink(1, 3, VertexAttribIntegerType.Int, stride, 3 * sizeof(float));

        _vbo.Unbind();
        _vao.Unbind();
        */
    }
    
    public Vector2 SliderDirection(int axis)
    {
        Vector2i[] a = [(1, 2), (0, 2), (0, 1)];
        Matrix4 model = Matrix4.CreateScale(GetCameraScaleVector()) * ModelMatrix;
        Vector3 start = axis switch
        {
            0 => (0, 1, 0),
            1 => (0, 0, 1),
            2 => (1, 0, 0),
            _ => Vector3.Zero
        };
        Vector3 end = axis switch
        {
            0 => (0, 0, 1),
            1 => (1, 0, 0),
            2 => (0, 1, 0),
            _ => Vector3.Zero
        };

        if ((Camera.front[a[axis].X] * Camera.front[a[axis].Y]) < 0)
        {
            (end, start) = (start, end);
        }

        var proj = Mathf.Num(Projection);
        var view = Mathf.Num(Camera.ViewMatrix);

        var tvertA = Mathf.WorldToScreen((new Vector4(start, 1f) * model).Xyz, proj, view, Game.Width - 400, Game.Height - 50) ?? (0, 0);
        var tvertB = Mathf.WorldToScreen((new Vector4(end, 1f) * model).Xyz, proj, view, Game.Width - 400, Game.Height - 50) ?? (0, 0);

        return tvertB - tvertA;
    }

    public override void Bind()
    {
        //_vao.Bind();
        //_ibo.Bind();
    }
    public override void Unbind()
    {
        //_ibo.Unbind();
        //_vao.Unbind();
    }

    public override void UpdateColor()
    {
        List<GizmoData> vertices = [];
        for (int i = 0; i < _vertices.Length; i++)
        {
            var (position, info) = _vertices[i];
            var type = info >> 3;
            vertices.Add((position, GetColor(info) * ((type & _hoveringInfo) == type ? 1.2f : 1f)));
        }   
        //_vbo.Update(vertices);
    }

    public override int Count() => _count;
    public override uint[] Indices() => _indices;
    public override (Vector3 position, int info)[] Vertices() => _vertices;

    private static Vector2[] _arcPositions = [
        (1.0000f, 0.0000f), // 0
        (0.9877f, 0.1564f), // 1
        (0.9511f, 0.3090f), // 2
        (0.8910f, 0.4539f), // 3
        (0.8090f, 0.5878f), // 4
        (0.7071f, 0.7071f), // 5
        (0.5878f, 0.8090f), // 6
        (0.4539f, 0.8910f), // 7
        (0.3090f, 0.9511f), // 8
        (0.1564f, 0.9877f), // 9
        (0.0000f, 1.0000f)  // 10
    ];

    private static void GenerateArc()
    {
        int[] positionIndices = [5, 6, 7, 8, 9, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 1, 2, 3, 4, 5];
        Vector2[] multipliers = [(0.89f, 0.91f), (0.9f, 0.9f)];
        Vector2[] offsets = [(0, 0), (-0.015f, 0.015f)];
        Vector3i[] inds = [(0, 1, 2), (2, 1, 0), (0, 2, 1)];
        int[] infos = [0b100100, 0b1001, 0b10010];

        List<uint> indices = [];
        List<(Vector3 position, int info)> vertices = [];

        for (uint i = 0; i < 6; i++)
        {
            for (uint j = 0; j < 40; j += 2)
            {
                uint o = j + i * 42;
                indices.AddRange([o, o + 1, o + 3, o + 3, o + 2, o]);
            }
        }

        for (int w = 0; w < 6; w++)
        {
            int m = w % 2;
            int l = Mathf.FloorToInt(w / 2);

            Vector2 multiplier = multipliers[m];
            Vector2 offset = offsets[m];
            Vector3i ind = inds[l];
            int info = infos[l];

            for (int i = 0; i < 21; i++)
            {
                Vector3 posA = (0, 0, 0), posB = (0, 0, 0);

                Vector2 mult = (i < 5 ? -1 : 1, i > 15 ? -1 : 1);
                Vector2 a = _arcPositions[positionIndices[i]] * multiplier.X * mult;
                Vector2 b = _arcPositions[positionIndices[i]] * multiplier.Y * mult;

                posA[ind.X] = a.X; posA[ind.Y] = a.Y; posA[ind.Z] = offset.X;
                posB[ind.X] = b.X; posB[ind.Y] = b.Y; posB[ind.Z] = offset.Y;

                vertices.AddRange((posA, info), (posB, info));
            }
        }

        _indices = [.. indices];
        _vertices = [.. vertices];
    }

    private static uint[] _indices = [];
    private static (Vector3 position, int info)[] _vertices = [];
}