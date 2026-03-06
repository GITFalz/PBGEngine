using PBG.MathLibrary;

public class Edge
{
    public static Edge Empty = new Edge(new Vertex(Vector3.Zero), new Vertex(Vector3.Zero));

    public Vertex A;
    public Vertex B;

    public HashSet<Triangle> ParentTriangles = new HashSet<Triangle>();

    public Edge(Vertex v1, Vertex v2)
    {
        A = v1;
        B = v2;

        A.AddParentEdge(this);
        B.AddParentEdge(this);
    }

    public Edge(Edge edge)
    {
        A = edge.A;
        B = edge.B;

        A.AddParentEdge(this);
        B.AddParentEdge(this);
    }

    private Edge()
    {
        A = new Vertex(Vector3.Zero);
        B = new Vertex(Vector3.Zero);
    }

    public void AddParentTriangle(Triangle triangle)
    {
        if (!ParentTriangles.Contains(triangle))
            ParentTriangles.Add(triangle);
    }

    public void SetVertexTo(Vertex oldVertex, Vertex newVertex)
    {
        if (A == oldVertex)
            A = newVertex;
        else if (B == oldVertex)
            B = newVertex;

        oldVertex.ParentEdges.Remove(this);
        newVertex.ParentEdges.Add(this);
    }

    public void ReplaceWith(Edge edge)
    {
        A.ParentEdges.Remove(this);
        B.ParentEdges.Remove(this);

        A = edge.A;
        B = edge.B;
        
        foreach (var triangle in ParentTriangles)
        {
            triangle.SetEdgeTo(this, edge);
        }
    }

    public bool Has(Vertex v)
    {
        return A == v || B == v;
    }

    public bool Is(Vertex a, Vertex b)
    {
        return (A == a && B == b) || (A == b && B == a);
    }

    public bool HasNot(Vertex v)
    {
        return !Has(v);
    }

    public Vector3 GetDirectionFrom(Vertex vertex)
    {
        return vertex == A ? B - A : A - B;
    }

    public Vector3 GetDirection()
    {
        return A - B;
    }

    public Vertex Not(Vertex v)
    {
        return A == v ? B : A;
    }

    public Edge SimpleCopy()
    {
        Edge edge = new Edge();
        edge.A = A;
        edge.B = B;
        return edge;
    }

    public Edge Delete()
    {
        A.ParentEdges.Remove(this);
        B.ParentEdges.Remove(this);
        ParentTriangles = [];
        return this;
    }

    public bool HasSameVertex(Edge e)
    {
        return (ReferenceEquals(A, e.A) && ReferenceEquals(B, e.B)) || (ReferenceEquals(A, e.B) && ReferenceEquals(B, e.A));
    }

    public static List<Edge> GetEdges(IEnumerable<Vertex> vertices)
    {
        List<Edge> edges = [];
        foreach (var vertex in vertices)
        {
            foreach (var edge in vertex.ParentEdges)
            {
                if (!edges.Contains(edge))
                    edges.Add(edge);
            }
        }
        return edges;
    }

    public override string ToString() => $"{A} - {B}";
}