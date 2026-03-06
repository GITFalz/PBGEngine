using PBG.MathLibrary;
using PBG.Threads;

public class Triangle
{
    public string ID = "ID";

    public Vertex A;
    public Vertex B;
    public Vertex C;

    public Vector2 UvA;
    public Vector2 UvB;
    public Vector2 UvC;

    public Edge AB;
    public Edge BC;
    public Edge CA;
    
    public Vector3 Normal;
    public Vector3 Color = new(0, 0, 0);

    public bool Inverted = false;

    public Triangle(Vertex a, Vertex b, Vertex c, Vector2 uvA, Vector2 uvB, Vector2 uvC, Edge ab, Edge bc, Edge ca)
    {
        A = a;
        B = b;
        C = c;

        UvA = uvA;
        UvB = uvB;
        UvC = uvC;

        AB = ab;
        BC = bc;
        CA = ca;

        A.AddParentTriangle(this);
        B.AddParentTriangle(this);
        C.AddParentTriangle(this);

        AB.AddParentTriangle(this);
        BC.AddParentTriangle(this);
        CA.AddParentTriangle(this);

        UpdateNormal();
    }

    public Triangle(Vertex a, Vertex b, Vertex c, Edge ab, Edge bc, Edge ca) : this(a, b, c, new(0, 0), new(0, 0), new(0, 0), ab, bc, ca) {}
    public Triangle(Vertex a, Vertex b, Vertex c) : this(a, b, c, new(a, b), new(b, c), new(c, a)) { }
    
    /*
    public override void RefreshValues(List<Vector3> transformedVerts, List<Vector2> uvs, List<Vector2i> textureIndices, List<Vector3> normals)
    {
        transformedVerts.AddRange(GetVerticesPosition());
        uvs.AddRange(GetUvs());
        textureIndices.AddRange([(0, 1), (0, 1), (0, 1)]);
        normals.AddRange(Normal, Normal, Normal);
    }
    */

    public void UpdateNormal() => Normal = CalculateNormal(A, B, C);
    
    public void Invert()
    {
        (A, B) = (B, A);
        Inverted = !Inverted;
    }
    
    public bool TwoVertSamePosition()
    {
        return A & B || A & C || B & C;
    }

    public void SetVertexTo(Vertex oldVertex, Vertex newVertex)
    {
        if (A == oldVertex)
            A = newVertex;
        else if (B == oldVertex)
            B = newVertex;
        else if (C == oldVertex)
            C = newVertex;

        AB.SetVertexTo(oldVertex, newVertex);
        BC.SetVertexTo(oldVertex, newVertex);
        CA.SetVertexTo(oldVertex, newVertex);

        oldVertex.ParentTriangles.Remove(this);
        newVertex.ParentTriangles.Add(this);
    }

    public void SetEdgeTo(Edge oldEdge, Edge newEdge)
    {
        if (AB == oldEdge)
            AB = newEdge;
        else if (BC == oldEdge)
            BC = newEdge;
        else if (CA == oldEdge)
            CA = newEdge;

        oldEdge.ParentTriangles.Remove(this);
        newEdge.ParentTriangles.Add(this);
    }

    public void SetUv(Vertex vertex, Vector2 uv)
    {
        if (A == vertex)
            UvA = uv;
        else if (B == vertex)
            UvB = uv;
        else if (C == vertex)
            UvC = uv;
    }

    public bool GetEdgeWithout(Vertex vertex, out Edge? edge)
    {
        edge = null;
        if (AB.HasNot(vertex))
        {
            edge = AB;
            return true;
        }
        if (BC.HasNot(vertex))
        {
            edge = BC;
            return true;
        }
        if (CA.HasNot(vertex))
        {
            edge = CA;
            return true;
        }
        return false;
    }

    public bool HasVertices(Vertex a, Vertex b, Vertex c)
    {
        return (A == a || A == b || A == c) && (B == a || B == b || B == c) && (C == a || C == b || C == c);
    }

    public bool HasVertices(Vertex a, Vertex b)
    {
        return (A == a && B == b) || (A == b && B == a) || (A == a && C == b) || (A == b && C == a) || (B == a && C == b) || (B == b && C == a);
    }

    public bool HasSharedVertexUv(Triangle triangle, out Vector2 uv1, out Vector2 uv2)
    {
        if (triangle.GetVertexUv(A, out uv2))
        {
            uv1 = UvA;
            return true;
        }
        if (triangle.GetVertexUv(B, out uv2))
        {
            uv1 = UvB;
            return true;
        }
        if (triangle.GetVertexUv(C, out uv2))
        {
            uv1 = UvC;
            return true;
        }

        uv1 = Vector2.Zero;
        uv2 = Vector2.Zero;
        return false;
    }

    public bool HasSameVertices()
    {
        return A == B || A == C || B == C;
    }

    public bool GetVertexUv(Vertex vertex, out Vector2 uv)
    {
        if (A == vertex)
        {
            uv = UvA;
            return true;
        }
        if (B == vertex)
        {
            uv = UvB;
            return true;
        }
        if (C == vertex)
        {
            uv = UvC;
            return true;
        }
        uv = Vector2.Zero;
        return false;
    }

    public void Not(Edge edge, out Edge A, out Edge B)
    {
        if (AB == edge)
        {
            A = BC;
            B = CA;
        }
        else if (BC == edge)
        {
            A = CA;
            B = AB;
        }
        else
        {
            A = AB;
            B = BC;
        }
    }

    public bool HasEdge(Edge edge)
    {
        return AB == edge || BC == edge || CA == edge;
    }

    public Triangle Delete()
    {
        A.ParentTriangles.Remove(this);
        B.ParentTriangles.Remove(this);
        C.ParentTriangles.Remove(this);
        
        AB.ParentTriangles.Remove(this);
        BC.ParentTriangles.Remove(this);
        CA.ParentTriangles.Remove(this);
        
        return this;
    }

    public Vector3[] GetVerticesPosition()
    {
        return [A, B, C];
    }

    public Vector3[] GetVerticesPosition(Vector3 offset)
    {
        return [A + offset, B + offset, C + offset];
    }

    public Vertex[] GetVertices()
    {
        return [A, B, C];
    }

    public Vector3 CalculateCenter() => new Vector3(A + B + C) / 3f;

    public Vector3 GetCenter()
    {
        return new Vector3(A + B + C) / 3f;
    }

    public Vector2[] GetUvs()
    {
        return [UvA, UvB, UvC];
    }

    public Triangle Copy(Vertex a, Vertex b, Vertex c, Vector2 uvA, Vector2 uvB, Vector2 uvC, Edge ab, Edge bc, Edge ca)
    {
        Triangle triangle = new Triangle(a, b, c, uvA, uvB, uvC, ab, bc, ca);
        triangle.ID = ID;
        return triangle;
    }

    public void InvertIfInverted()
    {
        foreach (Edge edge in new[] { AB, BC, CA })
        {
            foreach (Triangle adjTri in edge.ParentTriangles)
            {
                if (adjTri == this) continue;

                Vertex thisSingleVertex = edge.HasNot(A) ? A : edge.HasNot(B) ? B : C;
                Vertex adjSingleVertex = edge.HasNot(adjTri.A) ? adjTri.A : edge.HasNot(adjTri.B) ? adjTri.B : adjTri.C;

                Vector3 oldPosition = thisSingleVertex;
                thisSingleVertex.SetPosition(adjSingleVertex);
                UpdateNormal();
                float dot = Vector3.Dot(Normal, adjTri.Normal);
                thisSingleVertex.SetPosition(oldPosition);
                UpdateNormal();

                if (dot > 0)
                {
                    Normal = -Normal;
                    Invert();
                    return;
                }
            }
        }
    }

    public HashSet<Triangle> GetTriangleRegion(HashSet<Triangle> visited)
    {
        HashSet<Triangle> triangles = [this];
        visited.Add(this);
        
        // Loops trough all neighbouring triangles (connected by an edge at least).
        // Checks if the triangle has be visited before.
        // If not check if the current triangle and the neighbouring triangle are facing an opposite direction
        // If they are not add all the same neighbouring triangles of the neighbour triangle to this current one
        foreach (var sideTriangle in AB.ParentTriangles.Concat(BC.ParentTriangles).Concat(CA.ParentTriangles))
        {
            if (!visited.Contains(sideTriangle) && Mathf.RadiansToDegrees(Vector3.CalculateAngle(Normal, sideTriangle.Normal)) < 60)
            { 
                triangles.UnionWith(sideTriangle.GetTriangleRegion(visited));
            }
        }

        return triangles;
    }

    public HashSet<Triangle> GetTriangleRegion(HashSet<Triangle> visited, HashSet<Triangle> onlyInclude)
    {
        HashSet<Triangle> triangles = [];
        if (onlyInclude.Contains(this))
            triangles.Add(this);
        visited.Add(this);
        
        // Loops trough all neighbouring triangles (connected by an edge at least).
        // Checks if the triangle has be visited before.
        // If not check if the current triangle and the neighbouring triangle are facing an opposite direction
        // If they are not add all the same neighbouring triangles of the neighbour triangle to this current one
        foreach (var sideTriangle in AB.ParentTriangles.Concat(BC.ParentTriangles).Concat(CA.ParentTriangles))
        {
            if (!visited.Contains(sideTriangle) && Mathf.RadiansToDegrees(Vector3.CalculateAngle(Normal, sideTriangle.Normal)) < 60)
            { 
                triangles.UnionWith(sideTriangle.GetTriangleRegion(visited, onlyInclude));
            }
        }

        return triangles;
    }

    /// <summary>
    /// Flattens the triangles of a given region
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="region"></param>
    public void FlattenRegion(List<Triangle> region)
    {
        UvA = (A.X, A.Z);
        UvB = (B.X, B.Z);
        UvC = (C.X, C.Z);

        FlattenTriangle(region, []); 

        A.SetPosition((UvA.X, 0, UvA.Y));
        B.SetPosition((UvB.X, 0, UvB.Y));
        C.SetPosition((UvC.X, 0, UvC.Y));
    }

    /// <summary>
    /// Flattens the triangles of a given region
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="region"></param>
    /// <param name="visited"></param>
    /// <param name="updatePosition"></param>
    public void FlattenTriangle(List<Triangle> region, HashSet<Triangle> visited, bool updatePosition = true)
    {
        visited.Add(this);

        // Loops trough all neighbouring triangles (connected by an edge at least).
        foreach (var triangle in AB.ParentTriangles.Concat(BC.ParentTriangles).Concat(CA.ParentTriangles))
        {
            // Checks if the triangle is part of the region and has not been visited before.
            if (region.Contains(triangle) && !visited.Contains(triangle))
            {
                // Rotate the points of the triangle and store them in the uvs for the time being
                triangle.UpdateNormal();
                Vector3 rotationAxis = Vector3.Cross(triangle.Normal, (0, 1, 0));

                Vector3 a = triangle.A;
                Vector3 b = triangle.B;
                Vector3 c = triangle.C;

                if (rotationAxis.Length != 0)
                {
                    rotationAxis.Normalize();
                    float angle = Mathf.RadiansToDegrees(Vector3.CalculateAngle(triangle.Normal, (0, 1, 0)));
                    Vector3 center = triangle.GetCenter();
                    Vector3 rotatedNormal = Mathf.RotatePoint(triangle.Normal, Vector3.Zero, rotationAxis, angle);

                    if (Vector3.Dot(rotatedNormal, (0, 1, 0)) < 0)
                        angle += 180f;

                    a = Mathf.RotatePoint(a, center, rotationAxis, angle);
                    b = Mathf.RotatePoint(b, center, rotationAxis, angle);
                    c = Mathf.RotatePoint(c, center, rotationAxis, angle);
                }

                triangle.UvA = (a.X, a.Z);
                triangle.UvB = (b.X, b.Z);
                triangle.UvC = (c.X, c.Z);

                // if the triangle isn't the very first triangle update the position of the uv's so they line up with the parent triangle
                if (updatePosition && HasSharedVertexUv(triangle, out var uv1, out var uv2))
                {
                    Vector2 distance = uv1 - uv2;
                    triangle.UvA += distance;
                    triangle.UvB += distance;
                    triangle.UvC += distance;
                }
                // proceed to flatten the next triangles
                triangle.FlattenTriangle(region, visited);

                triangle.A.SetPosition((triangle.UvA.X, 0, triangle.UvA.Y));
                triangle.B.SetPosition((triangle.UvB.X, 0, triangle.UvB.Y));
                triangle.C.SetPosition((triangle.UvC.X, 0, triangle.UvC.Y));
            }
        }
    }
    
    public static Vector3 CalculateNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 edge1 = b - a;
        Vector3 edge2 = c - a;
        return Vector3.Cross(edge1, edge2).Normalized();
    }

    public override string ToString()
    {
        return $"Triangle:\nA: {A}\nB: {B}\nC: {C}";
    }
}

public static class Blocker
{
    private static volatile bool isBlocked = false;

    public static void Block()
    {
        if (!Thread.CurrentThread.IsBackground)
            return;

        isBlocked = true;
        while (isBlocked)
        {
            Thread.Sleep(1);
        }
    }

    public static void Unblock()
    {
        isBlocked = false;
    }

    public static void Reset()
    {
        isBlocked = true;
    }
}