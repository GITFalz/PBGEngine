using PBG.MathLibrary;

public struct BoundingBoxRegion
{
    public Vector3 Min;
    public Vector3 Max;
    public Vector3 OriginalMin;
    public Vector3 Size;
    public List<Vertex> Vertices;

    public BoundingBoxRegion(Vector3 min, Vector3 max, List<Vertex> vertices)
    {
        Min = min;
        Max = max;
        OriginalMin = min;
        Size = max - min;
        Vertices = vertices;
    }

    public void SetMin(Vector3 min)
    {
        Min = min;
        Max = Min + Size;
    }

    public bool Intersects(BoundingBoxRegion other)
    {
        return (Mathf.Min(Max.X, other.Max.X) >= Mathf.Max(Min.X, other.Min.X)) &&
            (Mathf.Min(Max.Y, other.Max.Y) >= Mathf.Max(Min.Y, other.Min.Y)) &&
            (Mathf.Min(Max.Z, other.Max.Z) >= Mathf.Max(Min.Z, other.Min.Z));
    }

    /// Check if two bounding boxes intersect
    public static bool operator &(BoundingBoxRegion a, BoundingBoxRegion b)
    {
        return a.Intersects(b);
    }
}