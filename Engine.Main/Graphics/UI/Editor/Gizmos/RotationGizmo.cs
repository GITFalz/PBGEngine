using PBG;
using PBG.Data;
using PBG.Mathematics;
using PBG.Rendering;


public class RotationGizmo : Gizmo
{
    public RotationGizmo(Camera camera) : base(camera)
    {
        GenerateArc();

        ibo = new(indices);
        GizmoData[] vertices = new GizmoData[_vertices.Length];
        for (int i = 0; i < _vertices.Length; i++)
        {
            var (position, _, color) = _vertices[i];
            vertices[i] = (position, GetColor(color));
        }   
        vbo = new([..vertices]);

        count = indices.Length;
    }
    
    public Vector2 SliderDirection(Axis axis)
    {
        Vector2i[] a = [(1, 2), (0, 2), (0, 1)];
        Matrix4 model = ModelMatrix * Matrix4.CreateScale(GetCameraScaleVector());
        Vector3 start = Vector3.Zero;
        Vector3 end = Vector3.Zero;

        int i = 0;
        if (axis.HasFlag(Axis.X))
        {
            start = (0, 1, 0);
            end = (0, 0, 1);
        }
        else if (axis.HasFlag(Axis.Y))
        {
            start = (0, 0, 1);
            end = (1, 0, 0);
            i = 1;
        }
        else if (axis.HasFlag(Axis.Z))
        {
            start = (1, 0, 0);
            end = (0, 1, 0);
            i = 2;
        }

        if ((Camera.front[a[i].X] * Camera.front[a[i].Y]) < 0)
        {
            (end, start) = (start, end);
        }

        var proj = Mathf.Num(Camera.ProjectionMatrix);
        var view = Mathf.Num(Camera.ViewMatrix);

        var tvertA = Mathf.WorldToScreen((new Vector4(start, 1f) * model).Xyz, proj, view, VoxelEngine.Width, VoxelEngine.Height) ?? (0, 0);
        var tvertB = Mathf.WorldToScreen((new Vector4(end, 1f) * model).Xyz, proj, view, VoxelEngine.Width, VoxelEngine.Height) ?? (0, 0);

        return tvertB - tvertA;
    }

    public override void Bind()
    {
        vbo.Bind();
        ibo.Bind();
    }

    public bool Update()
    {
        bool updated = false;
        if (Hover(out var tris) && Input.IsMousePressed(MouseButton.Left) && hoveringTriangle == null)
        {
            hoveringTriangle = tris.Value;
        }
        
        if (Input.IsMouseDown(MouseButton.Left) && hoveringTriangle != null && Input.MouseDelta != Vector2.Zero)
        {
            var triangle = hoveringTriangle.Value;
            var proj = Mathf.Num(Camera.ProjectionMatrix);
            var view = Mathf.Num(Camera.ViewMatrix);
            var center = Mathf.WorldToScreen(
                (new Vector4(Position, 1f) * ModelMatrix).Xyz, proj, view, VoxelEngine.Width, VoxelEngine.Height
            ) ?? (0, 0);

            Vector2 currentDir = Vector2.Normalize(Input.MousePosition - center);
            Vector2 prevDir    = Vector2.Normalize((Input.MousePosition - Input.MouseDelta) - center);

            float angle = MathF.Atan2(currentDir.Y, currentDir.X)
                        - MathF.Atan2(prevDir.Y, prevDir.X);

            if (angle >  MathF.PI) angle -= 2f * MathF.PI;
            if (angle < -MathF.PI) angle += 2f * MathF.PI;

            float sensitivity = 1.0f;
            Vector3 camDir = Vector3.Normalize(Camera.Position - Position);

            if (WorldSpace)
            {
                float signX = Mathf.Sign(-Vector3.Dot(Vector3.UnitX, camDir));
                float signY = Mathf.Sign(-Vector3.Dot(Vector3.UnitY, camDir));
                float signZ = Mathf.Sign(-Vector3.Dot(Vector3.UnitZ, camDir));

                if (triangle.MoveAxis.HasFlag(Axis.X))
                {
                    ChangedRotation = Quaternion.FromAxisAngle(Vector3.UnitX, signX * angle * sensitivity);
                    Rotation = ChangedRotation * Rotation;
                }
                if (triangle.MoveAxis.HasFlag(Axis.Y))
                {
                    ChangedRotation = Quaternion.FromAxisAngle(Vector3.UnitY, signY * angle * sensitivity);
                    Rotation = ChangedRotation * Rotation;
                }              
                if (triangle.MoveAxis.HasFlag(Axis.Z))
                {
                    ChangedRotation = Quaternion.FromAxisAngle(Vector3.UnitZ, signZ * angle * sensitivity);
                    Rotation = ChangedRotation * Rotation;
                }          
            }
            else
            {
                Vector3 localX = Vector3.Transform(Vector3.UnitX, Rotation);
                Vector3 localY = Vector3.Transform(Vector3.UnitY, Rotation);
                Vector3 localZ = Vector3.Transform(Vector3.UnitZ, Rotation);

                float signX = Mathf.Sign(-Vector3.Dot(localX, camDir));
                float signY = Mathf.Sign(-Vector3.Dot(localY, camDir));
                float signZ = Mathf.Sign(-Vector3.Dot(localZ, camDir));

                if (triangle.MoveAxis.HasFlag(Axis.X))
                    Rotation *= Quaternion.FromAxisAngle(localX, signX * angle * sensitivity);
                if (triangle.MoveAxis.HasFlag(Axis.Y))
                    Rotation *= Quaternion.FromAxisAngle(localY, signY * angle * sensitivity);
                if (triangle.MoveAxis.HasFlag(Axis.Z))
                    Rotation *= Quaternion.FromAxisAngle(localZ, signZ * angle * sensitivity);
            }

            updated = true;
        }

        if (Input.IsMouseReleased(MouseButton.Left))
        {
            if (WorldSpace)
            {
                Rotation = Quaternion.Identity;
            }

            hoveringTriangle = null;
            UpdateScreenSpacePositions = true;
        }
        return updated;
    }

    public override void UpdateColor()
    {
        GizmoData[] vertices = new GizmoData[_vertices.Length];
        for (int i = 0; i < _vertices.Length; i++)
        {
            var (position, move, color) = _vertices[i];
            vertices[i] = (position, GetColor(color) * (hoveringAxis.HasFlag(move) ? 1.2f : 1f));
        }   
        vbo.Update(vertices);
    }

    public override int Count() => count;
    public override uint[] Indices() => indices;
    public override (Vector3 position, Axis move, Axis color)[] Vertices() => _vertices;

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
        (Axis move, Axis color)[] infos = [(Axis.Z, Axis.Z), (Axis.X, Axis.X), (Axis.Y, Axis.Y)];

        List<uint> indices = [];
        List<(Vector3 position, Axis move, Axis color)> vertices = [];

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
            (Axis move, Axis color) info = infos[l];

            for (int i = 0; i < 21; i++)
            {
                Vector3 posA = (0, 0, 0), posB = (0, 0, 0);

                Vector2 mult = (i < 5 ? -1 : 1, i > 15 ? -1 : 1);
                Vector2 a = _arcPositions[positionIndices[i]] * multiplier.X * mult;
                Vector2 b = _arcPositions[positionIndices[i]] * multiplier.Y * mult;

                posA[ind.X] = a.X; posA[ind.Y] = a.Y; posA[ind.Z] = offset.X;
                posB[ind.X] = b.X; posB[ind.Y] = b.Y; posB[ind.Z] = offset.Y;

                vertices.AddRange((posA, info.move, info.color), (posB, info.move, info.color));
            }
        }

        RotationGizmo.indices = [.. indices];
        _vertices = [.. vertices];
    }

    private static uint[] indices = [];
    private static (Vector3 position, Axis move, Axis color)[] _vertices = [];
}