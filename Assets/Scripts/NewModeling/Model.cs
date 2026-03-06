using PBG.Core;
using PBG.Graphics;
using PBG.MathLibrary;

namespace PBG.Modeling;

public class PBG_Model : ScriptingNode
{
    public Mesh Mesh = null!;

    public List<PBG_Vertex> VertexList = [];
    public List<PBG_Edge> EdgeList = [];
    public List<PBG_Triangle> TriangleList = [];

    public bool IsSelected = false;
    public bool IsVisible = true;

    void Start()
    {
        Mesh = Transform.GetComponent<Mesh>();
        GenerateMesh();
    }

    public int AddVertex(PBG_Vertex vertex)
    {
        int index = VertexList.Count;
        vertex.Index = index;
        VertexList.Add(vertex);
        return index;
    }

    public int AddEdge(PBG_Edge edge)
    {
        int index = EdgeList.Count;
        edge.Index = index;
        EdgeList.Add(edge);
        return index;
    }

    public int AddTriangle(PBG_Triangle triangle)
    {
        int index = TriangleList.Count;
        triangle.Index = index;
        TriangleList.Add(triangle);
        return index;
    }

    public void RemoveVertex(PBG_Vertex vertex)
    {
        TriangleList.RemoveAll(t => t.HasVertex(vertex));

        int lastIndex = VertexList.Count - 1;
        if (vertex.Index != lastIndex)
        {
            var lastVertex = VertexList[lastIndex];
            lastVertex.Index = vertex.Index;
            VertexList[vertex.Index] = lastVertex;

            for (int i = 0; i < EdgeList.Count; i++)
            {
                var e = EdgeList[i];
                if(e.VA == lastIndex) 
                    e.VA = vertex.Index;

                if(e.VB == lastIndex)   
                    e.VB = vertex.Index;

                EdgeList[i] = e;
            }

            for (int i = 0; i < TriangleList.Count; i++)
            {
                var t = TriangleList[i];
                if(t.VA == lastIndex) 
                    t.VA = vertex.Index;

                if(t.VB == lastIndex)   
                    t.VB = vertex.Index;

                if(t.VC == lastIndex) 
                    t.VC = vertex.Index;

                TriangleList[i] = t;
            }
        }

        VertexList.RemoveAt(lastIndex);
    }

    public void RemoveVertices(List<PBG_Vertex> vertices)
    {
        var toRemove = new HashSet<int>(vertices.Select(v => v.Index));
        var oldToNewIndex = new Dictionary<int, int>();
        var newVertexList = new List<PBG_Vertex>();

        for (int i = 0; i < VertexList.Count; i++)
        {
            if (toRemove.Contains(i)) continue;

            var v = VertexList[i];
            int newIndex = newVertexList.Count;
            oldToNewIndex[i] = newIndex;
            v.Index = newIndex;
            newVertexList.Add(v);
        }

        VertexList = newVertexList;

        for(int i = 0; i < TriangleList.Count; i++)
        {
            var t = TriangleList[i];
            t.VA = oldToNewIndex[t.VA];
            t.VB = oldToNewIndex[t.VB];
            t.VC = oldToNewIndex[t.VC];
            TriangleList[i] = t;
        }

        for(int i = 0; i < EdgeList.Count; i++)
        {
            var e = EdgeList[i];
            e.VA = oldToNewIndex[e.VA];
            e.VB = oldToNewIndex[e.VB];
            EdgeList[i] = e;
        }
    }

    public void GenerateMesh()
    {
        Dictionary<int, int> vertexHash = [];

        List<Vector3> vertices = [];
        List<Vector3> normals = [];
        List<Vector2> uvs = [];
        List<int> textureIndices = [];
        List<uint> indices = [];

        int index = 0;
        for (int i = 0; i < TriangleList.Count; i++)
        {
            var triangle = TriangleList[i];

            if (vertexHash.TryAdd(triangle.VA, index))
            {
                vertices.Add(VertexList[triangle.VA].Position);
                normals.Add(triangle.NA);
                uvs.Add(triangle.UvA);
                textureIndices.Add(0);
                indices.Add((uint)index);
                triangle.MIA = index;
                index++;
            }
            else
            {
                var ind = vertexHash[triangle.VA];
                triangle.MIA = ind;
                indices.Add((uint)ind);
            }
            
            if (vertexHash.TryAdd(triangle.VB, index))
            {
                vertices.Add(VertexList[triangle.VB].Position);
                normals.Add(triangle.NB);
                uvs.Add(triangle.UvB);
                textureIndices.Add(0);
                indices.Add((uint)index);
                index++;
            }
            else
            {
                var ind = vertexHash[triangle.VB];
                triangle.MIB = ind;
                indices.Add((uint)ind);
            }

            if (vertexHash.TryAdd(triangle.VC, index))
            {
                vertices.Add(VertexList[triangle.VC].Position);
                normals.Add(triangle.NC);
                uvs.Add(triangle.UvC);
                textureIndices.Add(0);
                indices.Add((uint)index);
                index++;
            }
            else
            {
                var ind = vertexHash[triangle.VC];
                triangle.MIC = ind;
                indices.Add((uint)ind);
            }

            TriangleList[i] = triangle;
        }

        Mesh.Vertices = [..vertices];
        Mesh.Normals = [..normals];
        Mesh.Uvs = [..uvs];
        Mesh.TextureIndices = [..textureIndices];
        Mesh.Indices = [..indices];

        Mesh.Generate();
    }

    public void UpdateVertices()
    {
        
    }

    public void UpdateVertex(PBG_Vertex vertex)
    {
        if (vertex.Index >= 0 && vertex.Index < VertexList.Count)
            VertexList[vertex.Index] = vertex;
    }


    public void Delete()
    {
        Transform.Delete();
    }

    void Dispose()
    {
        VertexList.Clear();
        EdgeList.Clear();
        TriangleList.Clear();
    }

    public static List<PBG_Model> SelectedModels = [];
    public static PBG_Model? SelectedModel = null;

    public static void Select(PBG_Model model)
    {
        if (!SelectedModels.Contains(model))
        {
            SelectedModels.Add(model);
        }

        if (SelectedModel != null)
        {
            /*
            SelectedModel.SelectedVertices.Clear();
            SelectedModel.GenerateVertexColor();
            */
        }

        SelectedModel = model;
        if (SelectedModel != null)
        {
            SelectedModel.IsSelected = true;
            //SelectedModel.UpdateVertexPosition();
        }
    }

    public static bool UnSelect(PBG_Model model)
    {
        model.IsSelected = false;
        SelectedModels.Remove(model);

        if (SelectedModel == model)
        {
            /*
            SelectedModel.SelectedVertices.Clear();
            SelectedModel.GenerateVertexColor();
            */
            SelectedModel = null;
            return true;
        }
        return false;
    }
}