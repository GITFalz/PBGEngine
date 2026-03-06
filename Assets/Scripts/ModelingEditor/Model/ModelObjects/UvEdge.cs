
using PBG.MathLibrary;

public class UvEdge
{
    public List<UvTriangle> ParentTriangles = [];

    public Uv A;
    public Uv B;

    public UvEdge(Uv uv1, Uv uv2)
    {
        A = uv1;
        B = uv2;

        A.AddParentEdge(this);
        B.AddParentEdge(this);
    }

    public bool AddParentTriangle(UvTriangle triangle)
    {
        if (ParentTriangles.Contains(triangle))
            return false;
        
        ParentTriangles.Add(triangle);
        return true;
    }

    public Vector2[] GetVetices()
    {
        return [A, B];
    }
    public Vector3[] GetColors()
    {
        return [A.Color, B.Color];
    }

    public bool HasSameUvs(UvEdge e)
    {
        return (ReferenceEquals(A, e.A) && ReferenceEquals(B, e.B)) || (ReferenceEquals(A, e.B) && ReferenceEquals(B, e.A));
    }

    public static bool operator ==(UvEdge uv1, UvEdge uv2) => ReferenceEquals(uv1, uv2) || uv1.HasSameUvs(uv2);
    public static bool operator !=(UvEdge uv1, UvEdge uv2) => !ReferenceEquals(uv1, uv2) && !uv1.HasSameUvs(uv2);
}