using System.Runtime.InteropServices;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Parse;

public class UvMesh
{
    // Mesh
    //private VAO _vao = new VAO();
    //private IBO _ibo = new();
    //private VBO<Vector2> _uvVbo = new(new List<Vector2>());

    public List<Vector2> Uvs = new List<Vector2>();
    public List<uint> Indices = new List<uint>();

    // Vertex
    public struct VertexData
    {
        public Vector3 Position;
        public Vector4 Color;
        public float Size;
    }
    //private VAO _vertexVao = new VAO();
    //private VBO<VertexData> _vertexVbo = new(new List<VertexData>());
    public List<VertexData> Vertices = new List<VertexData>();

    // Edge
    //private VAO _edgeVao = new VAO();
    //private VBO<Vector3> _edgeVbo = new(new List<Vector3>());
    //private VBO<Vector3> _edgeColorVbo = new(new List<Vector3>());

    public List<Vector3> EdgeVertices = new List<Vector3>();
    public List<Vector3> EdgeColors = new List<Vector3>();


    public List<Uv> UvList = new List<Uv>();
    public List<UvEdge> EdgeList = new List<UvEdge>();
    public List<UvTriangle> TriangleList = new List<UvTriangle>();


    public void Init()
    {
        Uvs = [];
        for (int i = 0; i < TriangleList.Count; i++)
        {
            var triangle = TriangleList[i]; 
            Uvs.AddRange(triangle.GetUvPositions());
        }

        Vertices = [];
        for (int i = 0; i < UvList.Count; i++)
        {
            var uv = UvList[i];
            Vertices.Add(new VertexData{
                Position = (uv.X, uv.Y, 0),
                Color = new Vector4(uv.Color.X, uv.Color.Y, uv.Color.Z, 1f),
                Size = 10f
            });
        }

        EdgeVertices = [];
        EdgeColors = [];
        for (int i = 0; i < EdgeList.Count; i++)
        {
            var edge = EdgeList[i];
            EdgeVertices.AddRange((edge.A.X, edge.A.Y, 0), (edge.B.X, edge.B.Y, 0));
            EdgeColors.AddRange(edge.GetColors());
        }

        Indices = [];
        for (uint i = 0; i < TriangleList.Count * 3; i++)
        {
            Indices.Add(i);
        }
    }

    public void Update()
    {
        for (int i = 0; i < TriangleList.Count; i++)
        {
            var triangle = TriangleList[i];
            int index = i * 3;

            Uvs[index] = triangle.A;
            Uvs[index+1] = triangle.B;
            Uvs[index+2] = triangle.C;
        }

        for (int i = 0; i < UvList.Count; i++)
        {
            var uv = UvList[i];

            Vertices[i] = new VertexData{
                Position = (uv.X, uv.Y, 0),
                Color = new Vector4(uv.Color.X, uv.Color.Y, uv.Color.Z, 1f),
                Size = 10f
            };
        }

        for (int i = 0; i < EdgeList.Count; i++)
        {
            var edge = EdgeList[i];
            int index = i * 2;

            EdgeVertices[index] = (edge.A.X, edge.A.Y, 0);
            EdgeVertices[index+1] = (edge.B.X, edge.B.Y, 0);

            EdgeColors[index] = edge.A.Color;
            EdgeColors[index+1] = edge.B.Color;
        }

        UpdateMesh();
    }

    public UvEdge Add(UvEdge edge)
    {
        EdgeList.Add(edge);
        return edge;
    }

    public Uv Add(Uv uv)
    {
        UvList.Add(uv);
        return uv;
    }

    public bool LoadModel(Model model)
    {
        Unload();

        for (int i = 0; i < model.Mesh.TriangleList.Count; i++)
        {
            var tris = model.Mesh.TriangleList[i];

            Uv a = Add(new Uv(tris.A, tris.UvA));
            Uv b = Add(new Uv(tris.B, tris.UvB));
            Uv c = Add(new Uv(tris.C, tris.UvC));

            UvEdge ab = Add(new UvEdge(a, b));
            UvEdge bc = Add(new UvEdge(b, c));
            UvEdge ca = Add(new UvEdge(c, a));

            TriangleList.Add(new UvTriangle(a, b, c, ab, bc, ca));
        }

        Init();
        GenerateBuffers();

        return true;
    }

    public void UpdateVertexColors()
    {
        //_vertexVbo.Update(Vertices);
    }

    public void UpdateEdgeColors()
    {
        //_edgeColorVbo.Update(EdgeColors);
    }

    public void UpdateEdges()
    {
        //_edgeVbo.Update(EdgeVertices);
    }

    public void UpdatePosition()
    {
        //_uvVbo.Update(Uvs);
    }

    public void UpdateMesh()
    {
        UpdateVertexColors();
        UpdateEdgeColors();
        UpdateEdges();
        UpdatePosition();
    }

    public void GenerateBuffers()
    {
        /*
        _uvVbo.Renew(Uvs);

        _vertexVbo.Renew(Vertices);

        _edgeVbo.Renew(EdgeVertices);
        _edgeColorVbo.Renew(EdgeColors);

        _vao.Bind();
        
        _vao.LinkToVAO(0, 2, VertexAttribPointerType.Float, 0, 0, _uvVbo);

        _vao.Unbind();  

        _vertexVao.Bind();  

        _vertexVbo.Bind();

        int stride = Marshal.SizeOf<VertexData>();
        _vertexVao.Link(0, 3, VertexAttribPointerType.Float, stride, 0);
        _vertexVao.Link(1, 4, VertexAttribPointerType.Float, stride, 3 * sizeof(float));
        _vertexVao.Link(2, 1, VertexAttribPointerType.Float, stride, 7 * sizeof(float));

        _vertexVbo.Unbind();

        _vertexVao.Unbind();

        _edgeVao.Bind();

        _edgeVao.LinkToVAO(0, 3, VertexAttribPointerType.Float, 0, 0, _edgeVbo);
        _edgeVao.LinkToVAO(1, 3, VertexAttribPointerType.Float, 0, 0, _edgeColorVbo);

        _edgeVao.Unbind();
        
        _ibo.Renew(Indices);
        */
    }

    public void Render()
    {
        /*
        _vao.Bind();
        _ibo.Bind();

        GL.DrawElements(PrimitiveType.Triangles, Indices.Count, DrawElementsType.UnsignedInt, 0);

        _vao.Unbind();
        _ibo.Unbind();
        */
    }

    public void RenderVertices()
    {
        /*
        GL.Enable(EnableCap.ProgramPointSize);

        _vertexVao.Bind();

        GL.DrawArrays(PrimitiveType.Points, 0, Vertices.Count);

        _vertexVao.Unbind();

        GL.Disable(EnableCap.ProgramPointSize);
        */
    }

    public void RenderEdges()
    {
        /*
        _edgeVao.Bind();

        GL.DrawArrays(PrimitiveType.Lines, 0, EdgeVertices.Count);

        _edgeVao.Unbind();
        */
    }

    public void Unload()
    {
        UvList.Clear();
        EdgeList.Clear();
        TriangleList.Clear();

        Uvs.Clear();
        Indices.Clear();

        Vertices.Clear();

        EdgeVertices.Clear();
        EdgeColors.Clear();
    }
    
    public void Dispose()
    {
        /*
        _vao.DeleteBuffer();
        _ibo.DeleteBuffer();
        _uvVbo.DeleteBuffer();

        _vertexVao.DeleteBuffer();
        _vertexVbo.DeleteBuffer();

        _edgeVao.DeleteBuffer();
        _edgeVbo.DeleteBuffer();
        _edgeColorVbo.DeleteBuffer();
        */
    }
}