public class ModelCopy
{
    public List<Vertex> selectedVertices = [];
    public List<Edge> selectedEdges = [];
    public List<Triangle> selectedTriangles = [];

    public List<Vertex> newSelectedVertices = [];
    public List<Edge> newSelectedEdges = [];
    public List<Triangle> newSelectedTriangles = [];

    public Dictionary<Vertex, Vertex> vertexMap = [];
    public Dictionary<Edge, Edge> edgeMap = [];
    public Dictionary<Triangle, Triangle> triangleMap = [];

    public ModelCopy() { }
    public ModelCopy(IEnumerable<Vertex> vertices) { CopyInto(this, vertices); }

    public void Clear()
    {
        selectedVertices.Clear();
        selectedEdges.Clear();
        selectedTriangles.Clear();

        newSelectedVertices.Clear();
        newSelectedEdges.Clear();
        newSelectedTriangles.Clear();

        vertexMap.Clear();
        edgeMap.Clear();
        triangleMap.Clear();
    }

    public void Add(Vertex vert) { if (!selectedVertices.Contains(vert)) selectedVertices.Add(vert); }
    public void Add(Edge edge) { if (!selectedEdges.Contains(edge)) selectedEdges.Add(edge); }
    public void Add(Triangle triangle) { if (!selectedTriangles.Contains(triangle)) selectedTriangles.Add(triangle); }

    // Vertex
    public int GetOldIndex(Vertex vert) { return selectedVertices.IndexOf(vert); }
    public Vertex GetOldVertex(int index) { return selectedVertices[index]; }
    public int GetNewIndex(Vertex vert) { return newSelectedVertices.IndexOf(vert); }
    public Vertex GetNewVertex(int index) { return newSelectedVertices[index]; }
    public Vertex GetNewVertex(Vertex oldVertex) { return newSelectedVertices[GetOldIndex(oldVertex)]; }

    // Edge
    public int GetOldIndex(Edge edge) { return selectedEdges.IndexOf(edge); }
    public Edge GetOldEdge(int index) { return selectedEdges[index]; }
    public int GetNewIndex(Edge edge) { return newSelectedEdges.IndexOf(edge); }
    public Edge GetNewEdge(int index) { return newSelectedEdges[index]; }
    public Edge GetNewEdge(Edge oldEdge) { return newSelectedEdges[GetOldIndex(oldEdge)]; }

    public void CreateCopy()
    {
        foreach (var triangle in selectedTriangles)
        {
            Add(triangle.A);
            Add(triangle.B);
            Add(triangle.C);

            Add(triangle.AB);
            Add(triangle.BC);
            Add(triangle.CA);
        }

        foreach (var vert in selectedVertices)
        {
            Vertex newVert = vert.Copy();
            vertexMap.Add(newVert, vert);
            newSelectedVertices.Add(newVert);
        }

        foreach (var edge in selectedEdges)
        {
            Edge newEdge = new(GetNewVertex(edge.A), GetNewVertex(edge.B));
            edgeMap.Add(newEdge, edge);
            newSelectedEdges.Add(newEdge);
        }

        foreach (var triangle in selectedTriangles)
        {
            Triangle newTriangle = triangle.Copy(
                GetNewVertex(triangle.A), 
                GetNewVertex(triangle.B), 
                GetNewVertex(triangle.C), 
                triangle.UvA,
                triangle.UvB,
                triangle.UvC,
                GetNewEdge(triangle.AB), 
                GetNewEdge(triangle.BC), 
                GetNewEdge(triangle.CA)
            );
            triangleMap.Add(newTriangle, triangle);
            newSelectedTriangles.Add(
                newTriangle
            );  
        }
    }

    public ModelCopy Copy()
    {
        ModelCopy copy = new();

        foreach (var vert in newSelectedVertices)
        {
            Vertex newVert = vert.Copy();
            copy.vertexMap.Add(newVert, vertexMap[vert]);
            copy.newSelectedVertices.Add(vert.Copy());
        }

        foreach (var edge in newSelectedEdges)
        {
            Edge newEdge = new(copy.GetNewVertex(GetNewIndex(edge.A)), copy.GetNewVertex(GetNewIndex(edge.B)));
            copy.edgeMap.Add(newEdge, edgeMap[edge]);
            copy.newSelectedEdges.Add(newEdge);
        }

        foreach (var triangle in newSelectedTriangles)
        {
            Triangle newTriangle = triangle.Copy(
                copy.GetNewVertex(GetNewIndex(triangle.A)), 
                copy.GetNewVertex(GetNewIndex(triangle.B)), 
                copy.GetNewVertex(GetNewIndex(triangle.C)), 
                triangle.UvA,
                triangle.UvB,
                triangle.UvC,
                copy.GetNewEdge(GetNewIndex(triangle.AB)), 
                copy.GetNewEdge(GetNewIndex(triangle.BC)), 
                copy.GetNewEdge(GetNewIndex(triangle.CA))
            );
            copy.triangleMap.Add(newTriangle, triangleMap[triangle]);
            copy.newSelectedTriangles.Add(
                newTriangle
            );  
        }

        return copy;
    }

    public static void CopyInto(ModelCopy copy, IEnumerable<Vertex> vertices)
    {
        copy.Clear();
        copy.selectedVertices = [.. vertices];
        copy.selectedEdges = [.. Model.GetFullSelectedEdges(vertices)];
        copy.selectedTriangles = [.. Model.GetFullSelectedTriangles(vertices)];
        copy.CreateCopy();
    }
}