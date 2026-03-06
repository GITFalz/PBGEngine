
using PBG.MathLibrary;

public class Uv
{
    public List<UvEdge> ParentEdges = [];
    public UvTriangle ParentTriangle = null!;

    public Vector2 _value;
    public Vector2 Value;
    public Vector3 Color = Vector3.Zero;
    public Vertex Vertex;

    public float X
    {
        get => Value.X;
        set
        {
            Value.X = value;
            _value = Value;
        }
    }
    public float Y
    {
        get => Value.Y;
        set
        {
            Value.Y = value; 
            _value = Value;
        }
    }

    public Uv(Vertex vertex, Vector2 v)
    {
        Vertex = vertex;
        Value = v;
        _value = Value;
    }

    public Uv(Vertex vertex, float x, float y)
    {
        Vertex = vertex;
        Value = (x, y);
        _value = Value;
    }

    public void Set(Vector2 v)
    {
        Value = v;
        _value = v;
    }

    public void MovePosition(Vector2 offset)
    {
        Value += offset;
        _value = Value;
    }

    public void SnapPosition(Vector2 offset, float snap)
    {
        _value += offset;
        Value = new Vector2(
            (float)Math.Round(_value.X / snap) * snap,
            (float)Math.Round(_value.Y / snap) * snap
        );
    }

    public bool AddParentEdge(UvEdge edge)
    {
        if (ParentEdges.Contains(edge))
            return false;

        ParentEdges.Add(edge);
        return true;
    }

    public static implicit operator Vector2(Uv uv) => uv.Value;
    public static implicit operator Uv((Vertex vertex, Vector2 v) data) => new(data.vertex, data.v);
    public static Vector2 operator -(Uv uv1, Uv uv2) => uv1 - uv2;
    public static Vector2 operator +(Uv uv1, Uv uv2) => uv1 + uv2;
    public static bool operator ==(Uv uv1, Uv uv2) => ReferenceEquals(uv1, uv2);
    public static bool operator !=(Uv uv1, Uv uv2) => !ReferenceEquals(uv1, uv2);
}