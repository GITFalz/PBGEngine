using PBG.Compiler.Lines;
using PBG.Core;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Parse;
using PBG.Threads;

namespace PBG.Modeling;

public static class ObjLoader
{
    public static void Load(string path, TransformNode hierarchy)
    {
        ObjLoadingProcess process = new(path, hierarchy);
        TaskPool.QueueAction(process, TaskPriority.Low);
    }

    private static bool Load(string path, out List<PBG_Model> models)
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
                    throw new Exception($"Face at line {i+1} is not written correctly, it must at least contain: f v/vt/vn v/vt/vn v/vt/vn, but found {line}");
        
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

    private class ObjLoadingProcess(string path, TransformNode hierarchy) : ThreadProcess
    {
        private List<PBG_Model> _models = [];

        public override bool Function()
        {
            try
            {
                var result = Load(path, out _models);
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
            Console.WriteLine("Loaded model " + Succeded);
            if (Succeded)
            {
                for (int i = 0; i < _models.Count; i++)
                {
                    var model = _models[i];

                    TransformNode transformNode = hierarchy.AddChild("Model");
                    transformNode.AddComponent(model, new Mesh());

                    float angle = Mathf.DegreesToRadians(-90f);
                    transformNode.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, angle);
                    transformNode.Scale = (0.001f, 0.001f, 0.001f);
                }

                GeneralModelingEditor.Instance.UpdateHierarchy();
            }
        }
    }
}