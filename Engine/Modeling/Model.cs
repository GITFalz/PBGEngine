using PBG.Core;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;
using Silk.NET.Vulkan;

namespace PBG.Modeling;

public class PBG_Model : ScriptingNode
{
    public static Shader VertexShader;
    private static StaticStarter starter = new();
    private static int _modelLocation = -1;
    private static int _viewLocation = -1;
    private static int _projectionLocation = -1;

    private Descriptor _descriptor;
    private VBO<VertexStruct> _vbo = new([]);
    private IBO _ibo = new([]);
    private int _vertexCount = 0;

    public MeshRenderer MeshRenderer = null!;

    public List<PBG_Vertex> VertexList = [];
    public List<PBG_Edge> EdgeList = [];
    public List<PBG_Triangle> TriangleList = [];

    public bool IsSelected = false;
    public bool IsVisible = true;

    void Start()
    {
        starter.Run(() =>
        {
            ShaderInfo info = new() { 
                VertexShaderPath = Game.ShaderPath / "model_vulkan/vertex.vert",
                FragmentShaderPath = Game.ShaderPath / "model_vulkan/vertex.frag"
            };
            info.InputAssembly.Topology = PrimitiveTopology.PointList;
            info.DepthStencil.DepthTestEnable = true;
            info.DepthStencil.DepthWriteEnable = true;

            info.ColorBlendAttachment.BlendEnable = true;

            info.ColorBlendAttachment.SrcColorBlendFactor = BlendFactor.SrcAlpha;
            info.ColorBlendAttachment.DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha;
            info.ColorBlendAttachment.ColorBlendOp = BlendOp.Add;

            info.ColorBlendAttachment.SrcAlphaBlendFactor = BlendFactor.One;
            info.ColorBlendAttachment.DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha;
            info.ColorBlendAttachment.AlphaBlendOp = BlendOp.Add;

            VertexShader = new(info);

            VertexShader.BindVertexBuffer<VertexStruct>(0);
            VertexShader.Compile();

            _modelLocation = VertexShader.GetLocation("ubo.model");
            _viewLocation = VertexShader.GetLocation("ubo.view");
            _projectionLocation = VertexShader.GetLocation("ubo.projection");
        });

        _descriptor = VertexShader.GetDescriptorSet();

        MeshRenderer = Transform.GetComponent<MeshRenderer>();
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
                var position = VertexList[triangle.VA].Position;
                vertices.Add(position);
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
                var position = VertexList[triangle.VB].Position;
                vertices.Add(position);
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
                var position = VertexList[triangle.VC].Position;
                vertices.Add(position);
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

        Mesh mesh = new Mesh
        {
            Vertices = [.. vertices],
            Normals = [.. normals],
            Uvs = [.. uvs],
            TextureIndices = [.. textureIndices],
            Indices = [.. indices]
        };

        mesh.Generate();

        MeshRenderer.Mesh = mesh;


        VertexStruct[] vertexStructs = new VertexStruct[VertexList.Count];
        uint[] vertexIndices = new uint[VertexList.Count];

        for (int i = 0; i < VertexList.Count; i++)
        {
            var vert = VertexList[i];
            vertexStructs[i] = new()
            {
                Position = vert.Position,
                Color = (0, 0, 0, 1),
                Size = 10
            };
            vertexIndices[i] = (uint)i;
        }

        _vbo.Renew(vertexStructs);
        _ibo.Renew(vertexIndices);

        _vertexCount = vertexIndices.Length;
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

    void Render()
    {
        if (_vertexCount == 0)
            return;

        VertexShader.Bind();
        _descriptor.Bind();

        _descriptor.Uniform(_modelLocation, Transform.GetModelMatrix());
        _descriptor.Uniform(_viewLocation, Camera.ViewMatrix);
        _descriptor.Uniform(_projectionLocation, Camera.ProjectionMatrix);

        _vbo.Bind();
        _ibo.Bind();

        GFX.DrawIndexed((uint)_vertexCount, 1, 0, 0, 0);
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

public struct VertexStruct
{
    public Vector3 Position;
    public Vector4 Color;
    public float Size;
}