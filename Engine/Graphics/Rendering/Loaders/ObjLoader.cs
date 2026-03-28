using PBG.Asset;
using PBG.Core;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Parse;
using PBG.Rendering;
using PBG.Threads;

namespace PBG.Modeling;

public static class ObjLoader
{

    public static int Counter = 0;

    public static void LoadModel(string path, TransformNode hierarchy)
    {
        ModelObjLoadingProcess process = new(path, hierarchy);
        TaskPool.QueueAction(process, TaskPriority.Low);
    }

    public static void LoadMesh(string path, MeshRenderer renderer)
    {
        Console.WriteLine("Loading mesh at path: " + path);
        if (MeshCache.Meshes.TryGetValue(path, out var mesh))
        {
            renderer.Mesh = mesh;
        }
        else if (MeshCache.Loading.TryGetValue(path, out var loader))
        {
            loader.Add(renderer);
        }
        else
        {
            var process = new MeshObjLoadingProcess(path);
            loader = new AssetLoader<MeshRenderer>();
            loader.Add(renderer);

            MeshCache.Loading.Add(path, loader);
            TaskPool.QueueAction(process); 
        }
    }

    private static bool LoadMesh(string path, out Mesh? mesh)
    {
        if (!Path.Exists(path))
        {
            Console.WriteLine($"[Warning] : obj file not found at path: '{path}'");
            mesh = null;
            return false;
        }

        var lines = File.ReadAllLines(path);
        List<Vector3> extractedVertices = [];
        List<Vector2> extractedUvs = [];
        List<Vector3> extractedNormals = [];

        Vector3 min = new Vector3(float.MaxValue);
        Vector3 max = new Vector3(float.MinValue);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.StartsWith('#')) // it is a comment
                continue;

            if (line.StartsWith("v ")) // Handle vertex
            {
                var data = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Vector3 position = Vector3.Zero;

                for (int j = 1; j < 4.Min(data.Length); j++)
                {
                    position[j-1] = Float.Parse(data[j]);
                }

                min.MinSet(position);
                max.MaxSet(position);

                extractedVertices.Add(position);
            }

            if (line.StartsWith("vt ")) // Handle uvs
            {
                var data = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Vector2 uv = Vector2.Zero;

                for (int j = 1; j < 3.Min(data.Length); j++)
                {
                    uv[j-1] = Float.Parse(data[j]);
                }

                extractedUvs.Add(uv);
            }

            if (line.StartsWith("vn ")) // Handle normals
            {
                var data = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Vector3 normal = Vector3.Zero;

                for (int j = 1; j < 4.Min(data.Length); j++)
                {
                    normal[j-1] = Float.Parse(data[j]);
                }

                extractedNormals.Add(normal);
            }
        }
        Vector3 center = (min + max) * 0.5f;
        for (int i = 0; i < extractedVertices.Count; i++)
        {
            extractedVertices[i] = extractedVertices[i] - center;
        }

        Dictionary<(int v, int vt, int vn), uint> vertexMap = [];

        bool wasNotFace = true;

        mesh = new(Path.GetRelativePath(Game.CurrentProjectPath, path));
        
        List<SubMeshInfo> subMeshes = [];

        List<Vector3> vertices = [];
        List<Vector2> uvs = [];
        List<Vector3> normals = [];
        List<uint> indices = [];

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.StartsWith('#')) // it is a comment
                continue;

            if (line.StartsWith("f ")) // Handle vertex
            {
                if (wasNotFace)
                {
                    subMeshes.Add(new() {Start = (uint)indices.Count, Count = 0});
                    wasNotFace = false;
                }

                var data = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (data.Length < 4) // has to be at least: f v/vt/vn v/vt/vn v/vt/vn
                {
                    Console.WriteLine($"Face at line {i+1} is not written correctly, it must at least contain: f v/vt/vn v/vt/vn v/vt/vn, but found {line}");
                    throw new Exception($"Face at line {i+1} is not written correctly, it must at least contain: f v/vt/vn v/vt/vn v/vt/vn, but found {line}");
                }

                static bool Index(int i, string[] data, int count, out int index)
                {
                    index = -1;
                    if (data.Length > i)
                    {
                        var Index = Int.Parse(data[i], 0);
                        if (Index > 0 && Index <= count)
                        {
                            index = Index;
                            return true;
                        }
                    }
                    return false;
                }

                bool Vertex(string[] data, out int index) => Index(0, data, extractedVertices.Count, out index);
                bool Uv(string[] data, out int index) => Index(1, data, extractedUvs.Count, out index);
                bool Normal(string[] data, out int index) => Index(2, data, extractedNormals.Count, out index);

                for (int v = 1; v < 4; v++)
                {
                    var vData = data[v].Split('/');
                    if (Vertex(vData, out var vI) && Uv(vData, out var uI) && Normal(vData, out var nI))
                    {
                        (int v, int vt, int vn) face = (vI, uI, nI);
                        if (!vertexMap.TryGetValue(face, out var index))
                        {
                            index = (uint)vertices.Count;
                            vertexMap.Add(face, index);
                            vertices.Add(extractedVertices[face.v-1]);
                            uvs.Add(extractedUvs[face.vt-1]);
                            normals.Add(extractedNormals[face.vn-1]);
                        }
                        
                        indices.Add(index);
                        var info = subMeshes[^1];
                        info.Count++;
                        subMeshes[^1] = info;
                    }
                }
            }
            else
            {
                wasNotFace = true;
            }
        }

        mesh.SetSubMeshes([..subMeshes]);
        mesh.Vertices = [..vertices];
        mesh.Uvs = [..uvs];
        mesh.Normals = [..normals];
        mesh.Indices = [..indices];

        return true;
    }

    private static bool LoadModel(string path, out List<PBG_Model> models)
    {
        models = [];
        if (!Path.Exists(path))
        {
            Console.WriteLine($"[Warning] : obj file not found at path: '{path}'");
            return false;
        }

        var lines = File.ReadAllLines(path);
        List<Vector3> vertices = [];
        List<Vector2> uvs = [];
        List<Vector3> normals = [];

        Vector3 min = new Vector3(float.MaxValue);
        Vector3 max = new Vector3(float.MinValue);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.StartsWith('#')) // it is a comment
                continue;

            if (line.StartsWith("v ")) // Handle vertex
            {
                var data = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Vector3 position = Vector3.Zero;

                for (int j = 1; j < 4.Min(data.Length); j++)
                {
                    position[j-1] = Float.Parse(data[j]);
                }

                min.MinSet(position);
                max.MaxSet(position);

                vertices.Add(position);
            }

            if (line.StartsWith("vt ")) // Handle uvs
            {
                var data = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Vector2 uv = Vector2.Zero;

                for (int j = 1; j < 3.Min(data.Length); j++)
                {
                    uv[j-1] = Float.Parse(data[j]);
                }

                uvs.Add(uv);
            }

            if (line.StartsWith("vn ")) // Handle normals
            {
                var data = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Vector3 normal = Vector3.Zero;

                for (int j = 1; j < 4.Min(data.Length); j++)
                {
                    normal[j-1] = Float.Parse(data[j]);
                }

                normals.Add(normal);
            }
        }
        Vector3 center = (min + max) * 0.5f;
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = vertices[i] - center;
        }

        Dictionary<int, int> viewedVertices = [];
        HashSet<Vector2i> edgePairs = [];

        bool wasNotFace = true;

        PBG_Model? currentModel = null;
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.StartsWith('#')) // it is a comment
                continue;

            if (line.StartsWith("f ")) // Handle vertex
            {
                if (wasNotFace)
                {
                    if (currentModel != null)
                    {
                        for (int j = 0; j < currentModel.TriangleList.Count; j++)
                        {
                            var tris = currentModel.TriangleList[j];

                            Vector2i ab = (tris.VA, tris.VB);
                            Vector2i bc = (tris.VB, tris.VC);
                            Vector2i ca = (tris.VC, tris.VA);

                            if (!edgePairs.Contains(ab) && !edgePairs.Contains(ab.Flip()))
                                tris.EAB = currentModel.AddEdge(new PBG_Edge(ab));

                            if (!edgePairs.Contains(bc) && !edgePairs.Contains(bc.Flip()))
                                tris.EBC = currentModel.AddEdge(new PBG_Edge(bc));

                            if (!edgePairs.Contains(ca) && !edgePairs.Contains(ca.Flip()))
                                tris.ECA = currentModel.AddEdge(new PBG_Edge(ca));

                            currentModel.TriangleList[j] = tris;
                        }
                    }

                    currentModel = new();
                    models.Add(currentModel);

                    viewedVertices = [];

                    wasNotFace = false;
                }

                var data = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (data.Length < 4) // has to be at least: f v/vt/vn v/vt/vn v/vt/vn
                {
                    Console.WriteLine($"Face at line {i+1} is not written correctly, it must at least contain: f v/vt/vn v/vt/vn v/vt/vn, but found {line}");
                    throw new Exception($"Face at line {i+1} is not written correctly, it must at least contain: f v/vt/vn v/vt/vn v/vt/vn, but found {line}");
                }
        
                if (currentModel == null)
                    continue;

                PBG_Triangle triangle = new();

                // handle default data
                var vaData = data[1].Split('/');
                var vbData = data[2].Split('/');
                var vcData = data[3].Split('/');

                bool HandleVertex(string[] data, out int index)
                {
                    index = -1;
                    if (data.Length > 0)
                    {
                        var vIndex = Int.Parse(data[0], 0) - 1;
                        if (vIndex >= 0 && vIndex < vertices.Count)
                        {
                            if (!viewedVertices.TryGetValue(vIndex, out index))
                            {
                                var vertex = new PBG_Vertex(vertices[vIndex]);
                                index = currentModel.AddVertex(vertex);
                                viewedVertices[vIndex] = index;
                            }
                            return true;
                        }
                    }
                    return false;
                }

                bool HandleUv(string[] data, out Vector2 uv)
                {
                    uv = Vector2.Zero;
                    if (data.Length > 1)
                    {
                        var index = Int.Parse(data[1], 0) - 1;
                        if (index >= 0 && index < uvs.Count)
                        {
                            uv = uvs[index];
                            return true;
                        }
                    }
                    return false;
                }

                bool HandleNormal(string[] data, out Vector3 normal)
                {
                    normal = (0, 1, 0);
                    if (data.Length > 2)
                    {
                        var index = Int.Parse(data[2], 0) - 1;
                        if (index >= 0 && index < normals.Count)
                        {
                            normal = normals[index];
                            return true;
                        }
                    }
                    return false;
                }

                if (HandleVertex(vaData, out var index)) triangle.VA = index;
                if (HandleVertex(vbData, out index)) triangle.VB = index;
                if (HandleVertex(vcData, out index)) triangle.VC = index;
                
                if (HandleUv(vaData, out var uv)) triangle.UvA = uv;
                if (HandleUv(vbData, out uv)) triangle.UvB = uv;
                if (HandleUv(vcData, out uv)) triangle.UvC = uv;

                if (HandleNormal(vaData, out var normal)) triangle.NA = normal;
                if (HandleNormal(vbData, out normal)) triangle.NB = normal;
                if (HandleNormal(vcData, out normal)) triangle.NC = normal;

                currentModel.AddTriangle(triangle);
            }
            else
            {
                wasNotFace = true;
            }
        }
        if (!wasNotFace)
        {
            if (currentModel != null)
            {
                for (int j = 0; j < currentModel.TriangleList.Count; j++)
                {
                    var tris = currentModel.TriangleList[j];

                    Vector2i ab = (tris.VA, tris.VB);
                    Vector2i bc = (tris.VB, tris.VC);
                    Vector2i ca = (tris.VC, tris.VA);

                    if (!edgePairs.Contains(ab) && !edgePairs.Contains(ab.Flip()))
                        tris.EAB = currentModel.AddEdge(new PBG_Edge(ab));

                    if (!edgePairs.Contains(bc) && !edgePairs.Contains(bc.Flip()))
                        tris.EBC = currentModel.AddEdge(new PBG_Edge(bc));

                    if (!edgePairs.Contains(ca) && !edgePairs.Contains(ca.Flip()))
                        tris.ECA = currentModel.AddEdge(new PBG_Edge(ca));

                    currentModel.TriangleList[j] = tris;
                }
            }

            currentModel = new();
            models.Add(currentModel);
        }
        return true;
    }

    private class ModelObjLoadingProcess(string path, TransformNode hierarchy) : ThreadProcess
    {
        private List<PBG_Model> _models = [];

        public override bool Function()
        {
            try
            {
                var result = LoadModel(path, out _models);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Error] : " + ex.Message);
                return false;
            }
        }

        public override void OnCompleteBase()
        {
            if (Succeded)
            {   
                for (int i = 0; i < _models.Count; i++)
                {
                    var model = _models[i];

                    TransformNode transformNode = hierarchy.AddChild("Model");
                    transformNode.AddComponent(model, new MeshRenderer());

                    float angle = Mathf.DegreesToRadians(-90f);
                    transformNode.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, angle);
                    transformNode.Scale = (0.001f, 0.001f, 0.001f);
                }
            }
        }
    }

    private class MeshObjLoadingProcess(string path) : ThreadProcess
    {
        private Mesh? _mesh;

        public override bool Function()
        {
            try
            {
                var result = LoadMesh(path, out _mesh);
                return result;
            }
            catch (Exception ex)
            {
                throw;
                Console.WriteLine("[Error] : " + ex.Message);
                return false;
            }
        }

        public override void OnCompleteBase()
        {
            if (_mesh != null)
            {
                Console.WriteLine("Loaded mesh at path: " + path);
                _mesh.Generate();
                MeshCache.Cache(path, _mesh);
                if (MeshCache.Loading.TryGetValue(path, out var loader))
                {
                    Console.WriteLine("renderers: " + loader.Datas.Count);
                    while (loader.Datas.Count > 0)
                    {
                        var renderer = loader.Datas.Dequeue();
                        renderer.Mesh = _mesh;
                    }
                    MeshCache.Loading.Remove(path);
                }
            }
        }
    }
}