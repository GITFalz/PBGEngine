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

public struct GizmoTriangle(Vector2 a, Vector2 b, Vector2 c, Gizmo.Axis move, Gizmo.Axis color)
{
    Vector2 A = a;
    Vector2 B = b;
    Vector2 C = c;
    public Gizmo.Axis MoveAxis = move;
    public Gizmo.Axis ColorAxis = color;

    public bool InTriangle() => Mathf.PointInTriangle(Input.GetMousePosition(), A, B, C);
}

public abstract class Gizmo
{
    private static Shader _gizmoShader = null!;
    private static StaticStarter _staticStarter = new();
    private static int _gizmoModelLocation = -1;
    private static int _gizmoViewLocation = -1;
    private static int _gizmoProjectionLocation = -1;

    private Descriptor _descriptor;

    protected IBO ibo;
    protected VBO<GizmoData> vbo;
    protected int count = 0;

    protected static int stride = Marshal.SizeOf(typeof(GizmoData));

    public Vector3 Position = Vector3.Zero;
    public Quaternion Rotation = Quaternion.Identity;
    public Quaternion ChangedRotation = Quaternion.Identity;
    public bool UpdateScreenSpacePositions = false;

    public List<GizmoTriangle> Triangles = [];
    public Camera Camera;
    public Matrix4 ModelMatrix => Matrix4.CreateTranslation(Position) * Matrix4.CreateFromQuaternion(Rotation) * Matrix4.CreateScale(Vector3.Distance(Camera.Position, Position) * 0.15f);
    protected Axis hoveringAxis = 0;
    protected GizmoTriangle? hoveringTriangle = null;

    public static Vector4[] GizmoColor = [
        (0.88f, 0.33f, 0.33f, 1.0f), // X - softer red
        (0.45f, 0.78f, 0.45f, 1.0f), // Y - soft green
        (0.38f, 0.55f, 0.88f, 1.0f)  // Z - soft blue
    ];

    public Gizmo(Camera camera)
    {
        Camera = camera;

        _staticStarter.Run(() =>
        {
            var info = new ShaderInfo(Game.ShaderPath / "gizmo_vulkan/gizmo.vert", Game.ShaderPath / "gizmo_vulkan/gizmo.frag");
            info.Rasterizer.CullMode = Silk.NET.Vulkan.CullModeFlags.None;
            
            _gizmoShader = new Shader(info);
            _gizmoShader.BindVertexBuffer<GizmoData>(0);
            _gizmoShader.Compile();

            _gizmoModelLocation = _gizmoShader.GetLocation("ubo.model");
            _gizmoViewLocation = _gizmoShader.GetLocation("ubo.view");
            _gizmoProjectionLocation = _gizmoShader.GetLocation("ubo.projection");
        });

        _descriptor = _gizmoShader.GetDescriptorSet();
    }

    public abstract void Bind();
    public abstract int Count();

    public abstract void UpdateColor();

    public abstract uint[] Indices();
    public abstract (Vector3 position, Axis move, Axis color)[] Vertices();

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
                if (!hoveringAxis.HasFlag(triangle.MoveAxis))
                {
                    hoveringAxis = triangle.MoveAxis;
                    UpdateColor();
                }
                return true;
            }
        }
        if (hoveringAxis != 0)
        {
            hoveringAxis = 0;
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
        UpdateScreenSpacePositions = false;
        Triangles = [];
        Matrix4 model = ModelMatrix;
        var proj = Mathf.Num(Camera.ProjectionMatrix);
        var view = Mathf.Num(Camera.ViewMatrix);

        var indices = Indices();
        var vertices = Vertices();
        for (int i = 0; i < indices.Length; i += 3)
        {
            var a = indices[i];
            var b = indices[i + 1];
            var c = indices[i + 2];

            (Vector3 position, Axis move, Axis color) vertA;
            (Vector3 position, Axis move, Axis color) vertB;
            (Vector3 position, Axis move, Axis color) vertC;

            vertA = vertices[(int)a];
            vertB = vertices[(int)b];
            vertC = vertices[(int)c];
            
            var tvertA = Mathf.WorldToScreen((model * new Vector4(vertA.position, 1f)).Xyz, proj, view, Game.Width, Game.Height) ?? (0, 0);
            var tvertB = Mathf.WorldToScreen((model * new Vector4(vertB.position, 1f)).Xyz, proj, view, Game.Width, Game.Height) ?? (0, 0);
            var tvertC = Mathf.WorldToScreen((model * new Vector4(vertC.position, 1f)).Xyz, proj, view, Game.Width, Game.Height) ?? (0, 0);

            Triangles.Add(new(tvertA, tvertB, tvertC, vertA.move, vertA.color));
        }
    }

    public void Render()
    {
        _gizmoShader.Bind();
        _descriptor.Bind();

        _descriptor.UniformMatrix4(_gizmoModelLocation, ModelMatrix);
        _descriptor.UniformMatrix4(_gizmoViewLocation, Camera.ViewMatrix);
        _descriptor.UniformMatrix4(_gizmoProjectionLocation, Camera.ProjectionMatrix);

        Bind();

        GFX.DrawIndexed((uint)Count(), 1, 0, 0, 0);
    }


    public static Vector3 GetColor(Axis axis)
    {
        int count = 0;
        Vector3 color = Vector3.Zero;

        if (axis.HasFlag(Axis.X))
        {
            count++;
            color += GizmoColor[0].Xyz;
        }
        if (axis.HasFlag(Axis.Y))
        {
            count++;
            color += GizmoColor[1].Xyz;
        }
        if (axis.HasFlag(Axis.Z))
        {
            count++;
            color += GizmoColor[2].Xyz;
        }

        if (count == 0) return (0, 0, 0);
        if (count == 1) return color;
        return color / (float)count;
    }

    protected Vector3 GetCameraScaleVector() => new Vector3(Mathf.SignNo0(-Camera.front.X), Mathf.SignNo0(-Camera.front.Y), Mathf.SignNo0(-Camera.front.Z));

    [Flags]
    public enum Axis
    {
        X = 0b1,
        Y = 0b10,
        Z = 0b100
    }
}
