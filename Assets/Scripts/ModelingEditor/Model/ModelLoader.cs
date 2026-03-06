using PBG.MathLibrary;
using PBG.Parse;

public static class ModelLoader
{
    public static void SaveModel(Model model, string path)
    {
        V2_SaveModel(model, path);
    }

    public static bool LoadModel(Model model, string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
            return false;

        string header = lines[0];
        if (header.Contains("model-"))
        {
            if (header.Contains("v2"))
                return V2_LoadModel(model, lines);
        }

        return V1_LoadModel(model, lines);
    }
    
    public static bool LoadModel(SimpleModel model, string path) => LoadModel(model, path);
    public static bool LoadModel(SimpleModel model, string path, out LoadInfo info)
    {
        info = new();
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
            return false;
        
        string header = lines[0];
        if (header.Contains("model-"))
        {
            if (header.Contains("v2"))
                return V2_LoadSimpleModel(model, lines, out info);
        }

        return false;
    }

    private static void V2_SaveModel(Model model, string path)
    {
        List<string> lines = ["model-v2"];

        lines.AddRange([
            "",
            "# Vertices"
        ]);
        for (int i = 0; i < model.Mesh.VertexList.Count; i++)
        {
            var vertex = model.Mesh.VertexList[i];
            vertex.Index = i;
            lines.Add($"v {Str(vertex)} {vertex.BoneName}");
        }

        lines.AddRange([
            "",
            "# Edges"
        ]);
        for (int i = 0; i < model.Mesh.EdgeList.Count; i++)
        {
            var edge = model.Mesh.EdgeList[i];
            lines.Add($"e {edge.A.Index}/{edge.B.Index}");
        }

        lines.AddRange([
            "",
            "# Uvs"
        ]);
        for (int i = 0; i < model.Mesh.TriangleList.Count; i++)
        {
            var triangle = model.Mesh.TriangleList[i];
            lines.Add($"uv {Str(triangle.UvA)}");
            lines.Add($"uv {Str(triangle.UvB)}");
            lines.Add($"uv {Str(triangle.UvC)}");
        }

        lines.AddRange([
            "",
            "# Faces"
        ]);
        for (int i = 0; i < model.Mesh.TriangleList.Count; i++)
        {
            var t = model.Mesh.TriangleList[i];
            lines.Add($"f {t.A.Index}/{t.B.Index}/{t.C.Index} {i * 3}/{i * 3 + 1}/{i * 3 + 2} {model.Mesh.EdgeList.IndexOf(t.AB)}/{model.Mesh.EdgeList.IndexOf(t.BC)}/{model.Mesh.EdgeList.IndexOf(t.CA)}");
        }

        lines.AddRange([
            "",
            "# Rig"
        ]);
        var bones = model.Rig?.RootBone.GetBones() ?? [];
        for (int i = 0; i < bones.Count; i++)
        {
            var bone = bones[i];
            string line = "b";
            line += $" {bone.Name} {bone.GetParentName()}";
            line += $" {Str(bone.Position)}";
            line += $" {Str(bone.Rotation)}";
            line += $" {Str(bone.Scale)}";
            lines.Add(line);
        }

        lines.AddRange([
            "",
            "# Animations"
        ]);
        foreach (var (_, animation) in model.Animations)
        {
            lines.Add($"a {animation.Name}");
            foreach (var (_, boneAnimation) in animation.BoneAnimations)
            {
                lines.Add($"ab {boneAnimation.BoneName}");
                for (int i = 0; i < boneAnimation.PositionKeyframes.Count; i++)
                {
                    var keyframe = boneAnimation.PositionKeyframes[i];
                    lines.Add($"akp {keyframe.Index} {Str(keyframe.Position)}");
                }
                for (int i = 0; i < boneAnimation.RotationKeyframes.Count; i++)
                {
                    var keyframe = boneAnimation.RotationKeyframes[i];
                    lines.Add($"akr {keyframe.Index} {Str(keyframe.Rotation)}");
                }
                for (int i = 0; i < boneAnimation.ScaleKeyframes.Count; i++)
                {
                    var keyframe = boneAnimation.ScaleKeyframes[i];
                    lines.Add($"aks {keyframe.Index} {Str(keyframe.Scale)}");
                }
            }
        }

        File.WriteAllLines(path, lines);
    }

    private static bool V2_LoadModel(Model model, string[] lines)
    {
        ModelMesh mesh = model.Mesh;
        mesh.Unload();

        List<(Vector3 position, string? boneName)> vertexData = [];
        List<Vector2i> edgeData = [];
        List<Vector2> uvData = [];
        List<(Vector3i vertices, Vector3i? uvs, Vector3i? edges)> faceData = [];
        List<(string name, string parent, Vector3 position, Quaternion rotation, Vector3 scale)> boneData = [];
        Dictionary<string, Dictionary<string, (List<(int index, Vector3 position)> positions, List<(int index, Quaternion rotation)> rotations, List<(int index, Vector3 scale)> scales)>> animationData = [];
        
        string currentAnimation = "";
        string currentBoneAnimation = "";

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line) || line.Contains('#'))
                continue;

            var sections = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (sections.Length == 0)
                continue;

            switch (sections[0].Trim())
            {
                case "v":
                    if (sections.Length > 1)
                    {
                        var pos = sections[1].Split('/');
                        Vector3 position = (
                            pos.Length >= 1 ? Float.Parse(pos[0]) : 0,
                            pos.Length >= 2 ? Float.Parse(pos[1]) : 0,
                            pos.Length >= 3 ? Float.Parse(pos[2]) : 0
                        );
                        string? boneName = sections.Length > 2 ? sections[2] : null;
                        vertexData.Add((position, boneName));
                    }
                    break;
                case "e":
                    if (sections.Length > 1)
                    {
                        var ind = sections[1].Split('/');
                        Vector2i indices = (
                            ind.Length >= 1 ? Int.Parse(ind[0]) : 0,
                            ind.Length >= 2 ? Int.Parse(ind[1]) : 0
                        );
                        edgeData.Add(indices);
                    }
                    break;
                case "uv":
                    if (sections.Length > 1)
                    {
                        var pos = sections[1].Split('/');
                        Vector2 uv = (
                            pos.Length >= 1 ? Float.Parse(pos[0]) : 0,
                            pos.Length >= 2 ? Float.Parse(pos[1]) : 0
                        );
                        uvData.Add(uv);
                    }
                    break;
                case "f":
                    if (sections.Length > 1)
                    {
                        var vert = sections[1].Split('/');
                        Vector3i vertices = (
                            vert.Length >= 1 ? Int.Parse(vert[0]) : 0,
                            vert.Length >= 2 ? Int.Parse(vert[1]) : 0,
                            vert.Length >= 3 ? Int.Parse(vert[2]) : 0
                        );
                        Vector3i? uvs = null;
                        if (sections.Length > 2)
                        {
                            var uv = sections[2].Split('/');
                            uvs = (
                                uv.Length >= 1 ? Int.Parse(uv[0]) : 0,
                                uv.Length >= 2 ? Int.Parse(uv[1]) : 0,
                                uv.Length >= 3 ? Int.Parse(uv[2]) : 0
                            );
                        }
                        Vector3i? edges = null;
                        if (sections.Length > 3)
                        {
                            var edge = sections[3].Split('/');
                            edges = (
                                edge.Length >= 1 ? Int.Parse(edge[0]) : 0,
                                edge.Length >= 2 ? Int.Parse(edge[1]) : 0,
                                edge.Length >= 3 ? Int.Parse(edge[2]) : 0
                            );
                        }
                        faceData.Add((vertices, uvs, edges));
                    }
                    break;
                case "b":
                    if (sections.Length > 5)
                    {
                        string name = sections[1].Trim();
                        string parent = sections[2].Trim();
                        var pos = sections[3].Split('/');
                        Vector3 position = new(
                            pos.Length >= 1 ? Float.Parse(pos[0]) : 0,
                            pos.Length >= 2 ? Float.Parse(pos[1]) : 0,
                            pos.Length >= 3 ? Float.Parse(pos[2]) : 0
                        );
                        var rot = sections[4].Split('/');
                        Quaternion rotation = new(
                            rot.Length >= 1 ? Float.Parse(rot[0]) : 0,
                            rot.Length >= 2 ? Float.Parse(rot[1]) : 0,
                            rot.Length >= 3 ? Float.Parse(rot[2]) : 0,
                            rot.Length >= 4 ? Float.Parse(rot[3]) : 1
                        );
                        var sc = sections[5].Split('/');
                        Vector3 scale = new(
                            sc.Length >= 1 ? Float.Parse(sc[0]) : 1,
                            sc.Length >= 2 ? Float.Parse(sc[1]) : 1,
                            sc.Length >= 3 ? Float.Parse(sc[2]) : 1
                        );
                        boneData.Add((name, parent, position, rotation, scale));
                    }
                    break;
                case "a":
                    if (sections.Length > 1)
                    {
                        currentAnimation = sections[1];
                        animationData[currentAnimation] = [];
                    }
                    break;
                case "ab":
                    if (sections.Length > 1)
                    {
                        currentBoneAnimation = sections[1];
                        animationData[currentAnimation][currentBoneAnimation] = ([], [], []);
                    }
                    break;
                case "akp":
                    if (sections.Length > 2)
                    {
                        int index = Int.Parse(sections[1].Trim());
                        var pos = sections[2].Split('/');
                        Vector3 position = new(
                            pos.Length >= 1 ? Float.Parse(pos[0]) : 0,
                            pos.Length >= 2 ? Float.Parse(pos[1]) : 0,
                            pos.Length >= 3 ? Float.Parse(pos[2]) : 0
                        );
                        animationData[currentAnimation][currentBoneAnimation].positions.Add((index, position));
                    }
                    break;
                case "akr":
                    if (sections.Length > 2)
                    {
                        int index = Int.Parse(sections[1].Trim());
                        var rot = sections[2].Split('/');
                        Quaternion rotation = new(
                            rot.Length >= 1 ? Float.Parse(rot[0]) : 0,
                            rot.Length >= 2 ? Float.Parse(rot[1]) : 0,
                            rot.Length >= 3 ? Float.Parse(rot[2]) : 0,
                            rot.Length >= 4 ? Float.Parse(rot[3]) : 1
                        );
                        animationData[currentAnimation][currentBoneAnimation].rotations.Add((index, rotation));
                    }
                    break;
                case "aks":
                    if (sections.Length > 2)
                    {
                        int index = Int.Parse(sections[1].Trim());
                        var sc = sections[2].Split('/');
                        Vector3 scale = new(
                            sc.Length >= 1 ? Float.Parse(sc[0]) : 1,
                            sc.Length >= 2 ? Float.Parse(sc[1]) : 1,
                            sc.Length >= 3 ? Float.Parse(sc[2]) : 1
                        );
                        animationData[currentAnimation][currentBoneAnimation].scales.Add((index, scale));
                    }
                    break;    
            }
        }

        Vertex getVertex(int index) => (index >= 0 && index < mesh.VertexList.Count) ? mesh.VertexList[index] : new((0, 0, 0));
        Vector2 getUv(int index) => (index >= 0 && index < mesh.Uvs.Count) ? mesh.Uvs[index] : (0, 0);
        Edge getEdge(int index) => (index >= 0 && index < mesh.EdgeList.Count) ? mesh.EdgeList[index] : new(new((0, 0, 0)), new((0, 0, 0)));

        for (int i = 0; i < vertexData.Count; i++)
        {
            var (position, boneName) = vertexData[i];
            var vertex = new Vertex(position);
            vertex.BoneName = boneName ?? "RootBone";
            mesh.VertexList.Add(vertex);
        }

        for (int i = 0; i < edgeData.Count; i++)
        {
            var data = edgeData[i];
            mesh.EdgeList.Add(new Edge(getVertex(data.X), getVertex(data.Y)));
        }

        for (int i = 0; i < uvData.Count; i++)
        {
            var data = uvData[i];
            mesh.Uvs.Add(data);
        }

        for (int i = 0; i < faceData.Count; i++)
        {
            var (vertices, uvs, edges) = faceData[i];

            Vertex a = getVertex(vertices.X);
            Vertex b = getVertex(vertices.Y);
            Vertex c = getVertex(vertices.Z);

            Vector2 uvA = Vector2.Zero;
            Vector2 uvB = Vector2.Zero;
            Vector2 uvC = Vector2.Zero;

            Edge edgeAB;
            Edge edgeBC;
            Edge edgeCA;

            if (uvs != null)
            {
                uvA = getUv(uvs.Value.X);
                uvB = getUv(uvs.Value.Y);
                uvC = getUv(uvs.Value.Z);
            }

            if (edges != null)
            {
                edgeAB = getEdge(edges.Value.X);
                edgeBC = getEdge(edges.Value.Y);
                edgeCA = getEdge(edges.Value.Z);
            }
            else
            {
                edgeAB = new(a, b);
                edgeBC = new(b, c);
                edgeCA = new(c, a);
            }

            mesh.TriangleList.Add(new(a, b, c, uvA, uvB, uvC, edgeAB, edgeBC, edgeCA));
        }

        foreach (var (name, anim) in animationData)
        {
            NewAnimation animation = model.AddAnimation(name);
            foreach (var (bone, boneAnim) in anim)
            {
                NewBoneAnimation boneAnimation = new(bone);
                for (int i = 0; i < boneAnim.positions.Count; i++)
                {
                    var (index, position) = boneAnim.positions[i];
                    PositionKeyframe keyframe = new(index, position);
                    boneAnimation.AddOrUpdateKeyframe(keyframe);
                }
                for (int i = 0; i < boneAnim.rotations.Count; i++)
                {
                    var (index, rotation) = boneAnim.rotations[i];
                    RotationKeyframe keyframe = new(index, rotation);
                    boneAnimation.AddOrUpdateKeyframe(keyframe);
                }
                for (int i = 0; i < boneAnim.scales.Count; i++)
                {
                    var (index, scale) = boneAnim.scales[i];
                    ScaleKeyframe keyframe = new(index, scale);
                    boneAnimation.AddOrUpdateKeyframe(keyframe);
                }
                animation.AddBoneAnimation(boneAnimation);
            }
        }

        Dictionary<string, Bone> bones = [];
        model.Rig?.Delete();
        model.Rig = new(model.Name + "_rig");
        for (int i = 0; i < boneData.Count; i++)
        {
            var data = boneData[i];
            if (data.name == data.parent)
            {
                var bone = new RootBone(data.name);
                bone.Set(data.position, data.rotation, data.scale);
                model.Rig.RootBone = bone;
                bones.Add(data.name, bone);
            }
            else if (bones.TryGetValue(data.parent, out var parent))
            {
                var bone = new ChildBone(data.name, parent);
                bone.Set(data.position, data.rotation, data.scale);
                bones.Add(data.name, bone);
            }
        }
        model.Rig.Create();
        model.Rig.Initialize();
        model.Rig.RootBone.UpdateGlobalTransformation();

        model.BoneMatricesList.Clear();
        foreach (var bone in model.Rig.BonesList)
        {
            model.BoneMatricesList.Add(bone.GlobalAnimatedMatrix);
        }
        //model.BoneMatrices.Renew(model.BoneMatricesList);
        model.Mesh.InitRig();

        mesh.CheckUselessEdges();
        mesh.CheckUselessTriangles();

        mesh.RegenerateAll();

        return true;
    }
    
    private static bool V2_LoadSimpleModel(SimpleModel model, string[] lines, out LoadInfo info)
    {
        SimpleModelMesh mesh = model.Mesh;

        List<(Vector3 position, string? boneName)> vertexData = [];
        List<Vector2> uvData = [];
        List<(Vector3i vertices, Vector3i? uvs)> faceData = [];
        List<(string name, string parent, Vector3 position, Quaternion rotation, Vector3 scale)> boneData = [];
        Dictionary<string, Dictionary<string, (List<(int index, Vector3 position)> positions, List<(int index, Quaternion rotation)> rotations, List<(int index, Vector3 scale)> scales)>> animationData = [];

        string currentAnimation = "";
        string currentBoneAnimation = "";

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line) || line.Contains('#'))
                continue;

            var sections = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (sections.Length == 0)
                continue;

            switch (sections[0].Trim())
            {
                case "v":
                    if (sections.Length > 1)
                    {
                        var pos = sections[1].Split('/');
                        Vector3 position = (
                            pos.Length >= 1 ? Float.Parse(pos[0]) : 0,
                            pos.Length >= 2 ? Float.Parse(pos[1]) : 0,
                            pos.Length >= 3 ? Float.Parse(pos[2]) : 0
                        );
                        string? boneName = sections.Length > 2 ? sections[2] : null;
                        vertexData.Add((position, boneName));
                    }
                    break;
                case "uv":
                    if (sections.Length > 1)
                    {
                        var pos = sections[1].Split('/');
                        Vector2 uv = (
                            pos.Length >= 1 ? Float.Parse(pos[0]) : 0,
                            pos.Length >= 2 ? Float.Parse(pos[1]) : 0
                        );
                        uvData.Add(uv);
                    }
                    break;
                case "f":
                    if (sections.Length > 1)
                    {
                        var vert = sections[1].Split('/');
                        Vector3i vertices = (
                            vert.Length >= 1 ? Int.Parse(vert[0]) : 0,
                            vert.Length >= 2 ? Int.Parse(vert[1]) : 0,
                            vert.Length >= 3 ? Int.Parse(vert[2]) : 0
                        );
                        Vector3i? uvs = null;
                        if (sections.Length > 2)
                        {
                            var uv = sections[2].Split('/');
                            uvs = (
                                uv.Length >= 1 ? Int.Parse(uv[0]) : 0,
                                uv.Length >= 2 ? Int.Parse(uv[1]) : 0,
                                uv.Length >= 3 ? Int.Parse(uv[2]) : 0
                            );
                        }
                        faceData.Add((vertices, uvs));
                    }
                    break;
                case "b":
                    if (sections.Length > 5)
                    {
                        string name = sections[1].Trim();
                        string parent = sections[2].Trim();
                        var pos = sections[3].Split('/');
                        Vector3 position = new(
                            pos.Length >= 1 ? Float.Parse(pos[0]) : 0,
                            pos.Length >= 2 ? Float.Parse(pos[1]) : 0,
                            pos.Length >= 3 ? Float.Parse(pos[2]) : 0
                        );
                        var rot = sections[4].Split('/');
                        Quaternion rotation = new(
                            rot.Length >= 1 ? Float.Parse(rot[0]) : 0,
                            rot.Length >= 2 ? Float.Parse(rot[1]) : 0,
                            rot.Length >= 3 ? Float.Parse(rot[2]) : 0,
                            rot.Length >= 4 ? Float.Parse(rot[3]) : 0
                        );
                        var sc = sections[5].Split('/');
                        Vector3 scale = new(
                            sc.Length >= 1 ? Float.Parse(sc[0]) : 0,
                            sc.Length >= 2 ? Float.Parse(sc[1]) : 0,
                            sc.Length >= 3 ? Float.Parse(sc[2]) : 0
                        );
                        boneData.Add((name, parent, position, rotation, scale));
                    }
                    break;
                case "a":
                    if (sections.Length > 1)
                    {
                        currentAnimation = sections[1];
                        animationData[currentAnimation] = [];
                    }
                    break;
                case "ab":
                    if (sections.Length > 1)
                    {
                        currentBoneAnimation = sections[1];
                        animationData[currentAnimation][currentBoneAnimation] = ([], [], []);
                    }
                    break;
                case "akp":
                    if (sections.Length > 2)
                    {
                        int index = Int.Parse(sections[1].Trim());
                        var pos = sections[2].Split('/');
                        Vector3 position = new(
                            pos.Length >= 1 ? Float.Parse(pos[0]) : 0,
                            pos.Length >= 2 ? Float.Parse(pos[1]) : 0,
                            pos.Length >= 3 ? Float.Parse(pos[2]) : 0
                        );
                        animationData[currentAnimation][currentBoneAnimation].positions.Add((index, position));
                    }
                    break;
                case "akr":
                    if (sections.Length > 2)
                    {
                        int index = Int.Parse(sections[1].Trim());
                        var rot = sections[2].Split('/');
                        Quaternion rotation = new(
                            rot.Length >= 1 ? Float.Parse(rot[0]) : 0,
                            rot.Length >= 2 ? Float.Parse(rot[1]) : 0,
                            rot.Length >= 3 ? Float.Parse(rot[2]) : 0,
                            rot.Length >= 4 ? Float.Parse(rot[3]) : 1
                        );
                        animationData[currentAnimation][currentBoneAnimation].rotations.Add((index, rotation));
                    }
                    break;
                case "aks":
                    if (sections.Length > 2)
                    {
                        int index = Int.Parse(sections[1].Trim());
                        var sc = sections[2].Split('/');
                        Vector3 scale = new(
                            sc.Length >= 1 ? Float.Parse(sc[0]) : 1,
                            sc.Length >= 2 ? Float.Parse(sc[1]) : 1,
                            sc.Length >= 3 ? Float.Parse(sc[2]) : 1
                        );
                        animationData[currentAnimation][currentBoneAnimation].scales.Add((index, scale));
                    }
                    break;   
            }
        }

        List<Vertex> vertexList = [];
        List<Vector2> uvList = [];
        List<Triangle> triangleList = [];

        Vertex getVertex(int index) => (index >= 0 && index < vertexList.Count) ? vertexList[index] : new((0, 0, 0));
        Vector2 getUv(int index) => (index >= 0 && index < uvList.Count) ? uvList[index] : (0, 0);

        Vector3 min = new Vector3(float.MaxValue);
        Vector3 max = new Vector3(float.MinValue);

        for (int i = 0; i < vertexData.Count; i++)
        {
            var data = vertexData[i];
            var vertex = new Vertex(data.position);
            vertex.BoneName = data.boneName ?? "RootBone";
            vertexList.Add(vertex);

            min = Mathf.Min(min, data.position);
            max = Mathf.Max(max, data.position);
        }

        for (int i = 0; i < uvData.Count; i++)
        {
            var data = uvData[i];
            uvList.Add(data);
        }

        for (int i = 0; i < faceData.Count; i++)
        {
            var (vertices, uvs) = faceData[i];

            Vertex a = getVertex(vertices.X);
            Vertex b = getVertex(vertices.Y);
            Vertex c = getVertex(vertices.Z);

            Vector2 uvA = Vector2.Zero;
            Vector2 uvB = Vector2.Zero;
            Vector2 uvC = Vector2.Zero;

            Edge edgeAB = new(a, b);
            Edge edgeBC = new(b, c);
            Edge edgeCA = new(c, a);

            if (uvs != null)
            {
                uvA = getUv(uvs.Value.X);
                uvB = getUv(uvs.Value.Y);
                uvC = getUv(uvs.Value.Z);
            }

            triangleList.Add(new(a, b, c, uvA, uvB, uvC, edgeAB, edgeBC, edgeCA));
        }
        
        List<NewAnimation> newAnimations = [];
        foreach (var (name, anim) in animationData)
        {
            NewAnimation animation = new(name, 0);
            foreach (var (bone, boneAnim) in anim)
            {
                NewBoneAnimation boneAnimation = new(bone);
                for (int i = 0; i < boneAnim.positions.Count; i++)
                {
                    var (index, position) = boneAnim.positions[i];
                    PositionKeyframe keyframe = new(index, position);
                    boneAnimation.AddOrUpdateKeyframe(keyframe);
                }
                for (int i = 0; i < boneAnim.rotations.Count; i++)
                {
                    var (index, rotation) = boneAnim.rotations[i];
                    RotationKeyframe keyframe = new(index, rotation);
                    boneAnimation.AddOrUpdateKeyframe(keyframe);
                }
                for (int i = 0; i < boneAnim.scales.Count; i++)
                {
                    var (index, scale) = boneAnim.scales[i];
                    ScaleKeyframe keyframe = new(index, scale);
                    boneAnimation.AddOrUpdateKeyframe(keyframe);
                }
                animation.AddBoneAnimation(boneAnimation);
            }
            newAnimations.Add(animation);
        }

        Dictionary<string, Bone> bones = [];
        model.Rig?.Delete();
        model.Rig = new(model.Name + "_rig");
        for (int i = 0; i < boneData.Count; i++)
        {
            var data = boneData[i];
            if (data.name == data.parent)
            {
                var bone = new RootBone(data.name);
                bone.Set(data.position, data.rotation, data.scale);
                model.Rig.RootBone = bone;
                bones.Add(data.name, bone);
            }
            else if (bones.TryGetValue(data.parent, out var parent))
            {
                var bone = new ChildBone(data.name, parent);
                bone.Set(data.position, data.rotation, data.scale);
                bones.Add(data.name, bone);
            }
        }

        List<Vector3> transformedVerts = [];
        List<Vector2> meshUvs = [];
        List<Vector2i> textureIndices = [];
        List<Vector3> normals = [];

        model.Rig.Create();
        model.Rig.Initialize();
        model.Rig.RootBone.UpdateGlobalTransformation();

        model.BoneMatricesList.Clear();
        foreach (var bone in model.Rig.BonesList)
        {
            model.BoneMatricesList.Add(bone.GlobalAnimatedMatrix);
        }
        //model.BoneMatrices.Renew(model.BoneMatricesList);

        for (int i = 0; i < newAnimations.Count; i++)
        {
            var animation = newAnimations[i];
            model.Animations.Add(animation.Name, new(model.Rig, animation));
        }

        for (int i = 0; i < vertexList.Count; i++)
        {
            var vertex = vertexList[i];
            if (model.Rig.GetBone(vertex.BoneName, out var boneA))
            {
                vertex.Bone = boneA;
                vertex.Position = (boneA.TransposedInverseGlobalAnimatedMatrix * vertex.V4).Xyz;
            }
            else
            {
                vertex.Bone = null;
            }    
        }

        for (int i = 0; i < triangleList.Count; i++)
        {
            var t = triangleList[i];
            transformedVerts.AddRange(t.GetVerticesPosition());
            meshUvs.AddRange(t.GetUvs());
            textureIndices.AddRange([(0, t.A.Bone?.Index ?? 0), (0, t.B.Bone?.Index ?? 0), (0, t.C.Bone?.Index ?? 0)]);
            normals.AddRange(t.Normal, t.Normal, t.Normal);
        }

        //mesh.GenerateMesh(transformedVerts, meshUvs, textureIndices, normals);

        info = new() { Min = min, Max = max };

        return true;
    }

    // Old saving system
    private static void V1_SaveModel(ModelMesh mesh, string path)
    {
        mesh.CheckUselessVertices();
        mesh.CheckUselessEdges();
        mesh.CheckUselessTriangles();
        
        if (!File.Exists(path)) File.WriteAllText(path, "0\n0\n0\n0\n0");
        List<string> oldLines = [.. File.ReadAllLines(path)];
        List<string> newLines = new List<string>();

        int oldVertexCount = Int.Parse(oldLines[0]);
        int oldEdgeCount = Int.Parse(oldLines[oldVertexCount + 1]);
        int oldUvCount = Int.Parse(oldLines[oldVertexCount + oldEdgeCount + 2]);
        int oldTriangleCount = Int.Parse(oldLines[oldVertexCount + oldEdgeCount + oldUvCount + 3]);
        int oldNormalCount = Int.Parse(oldLines[oldVertexCount + oldEdgeCount + oldUvCount + oldTriangleCount + 4]);
        int rigStart = oldVertexCount + oldEdgeCount + oldUvCount + oldTriangleCount + oldNormalCount + 5;

        newLines.Add(mesh.VertexList.Count.ToString());
        foreach (var vertex in mesh.VertexList)
        {
            newLines.Add($"v {Float.Str(vertex.X)} {Float.Str(vertex.Y)} {Float.Str(vertex.Z)} {vertex.Index} {vertex.BoneName}");
        }

        newLines.Add(mesh.EdgeList.Count.ToString());
        foreach (var edge in mesh.EdgeList)
        {
            newLines.Add($"e {mesh.VertexList.IndexOf(edge.A)} {mesh.VertexList.IndexOf(edge.B)}");
        }

        newLines.Add((mesh.TriangleList.Count * 3).ToString());
        foreach (var triangle in mesh.TriangleList)
        {
            newLines.Add($"uv {Float.Str(triangle.UvA.X)} {Float.Str(triangle.UvA.Y)}");
            newLines.Add($"uv {Float.Str(triangle.UvB.X)} {Float.Str(triangle.UvB.Y)}");
            newLines.Add($"uv {Float.Str(triangle.UvC.X)} {Float.Str(triangle.UvC.Y)}");
        }

        newLines.Add(mesh.TriangleList.Count.ToString());
        foreach (var triangle in mesh.TriangleList)
        {
            newLines.Add($"f {mesh.VertexList.IndexOf(triangle.A)} {mesh.VertexList.IndexOf(triangle.B)} {mesh.VertexList.IndexOf(triangle.C)} {mesh.EdgeList.IndexOf(triangle.AB)} {mesh.EdgeList.IndexOf(triangle.BC)} {mesh.EdgeList.IndexOf(triangle.CA)}");
        }

        newLines.Add(mesh.Normals.Count.ToString());
        foreach (var normal in mesh.Normals)
        {
            newLines.Add($"n {Float.Str(normal.X)} {Float.Str(normal.Y)} {Float.Str(normal.Z)}");
        }

        for (int i = rigStart; i < oldLines.Count; i++)
        {
            newLines.Add(oldLines[i]);
        }
        
        File.WriteAllLines(path, newLines);
    }

    private static bool V1_LoadModel(Model model, string[] lines)
    {
        ModelMesh mesh = model.Mesh;
        mesh.Unload();

        int vertexCount = Int.Parse(lines[0]);

        int edgeIndex = vertexCount + 1;
        int edgeCount = Int.Parse(lines[edgeIndex]);

        int uvIndex = vertexCount + edgeCount + 2;
        int uvCount = Int.Parse(lines[uvIndex]);

        int triangleIndex = vertexCount + edgeCount + uvCount + 3;
        int triangleCount = Int.Parse(lines[triangleIndex]);

        for (int i = 1; i <= vertexCount; i++)
        {
            string[] values = lines[i].Trim().Trim().Split(' ');
            Vertex vertex = new Vertex(new Vector3(Float.Parse(values[1]), Float.Parse(values[2]), Float.Parse(values[3])));
            vertex.Name = "Vertex " + i;
            if (values.Length > 4)
                vertex.Index = Int.Parse(values[4]);

            if (values.Length > 5)
                vertex.BoneName = values[5].Trim();

            mesh.VertexList.Add(vertex);
        }

        for (int i = vertexCount + 2; i <= vertexCount + edgeCount + 1; i++)
        {
            string[] values = lines[i].Trim().Split(' ');
            mesh.EdgeList.Add(new Edge(mesh.VertexList[Int.Parse(values[1])], mesh.VertexList[Int.Parse(values[2])]));
        }

        for (int i = vertexCount + edgeCount + 3; i <= vertexCount + edgeCount + uvCount + 2; i++)
        {
            string[] values = lines[i].Trim().Split(' ');
            mesh.Uvs.Add(new Vector2(Float.Parse(values[1]), Float.Parse(values[2])));
        }

        int index = 0;
        for (int i = vertexCount + edgeCount + uvCount + 4; i <= vertexCount + edgeCount + uvCount + triangleCount + 3; i++)
        {
            string[] values = lines[i].Trim().Split(' ');

            Vertex a, b, c;

            try
            {
                a = mesh.VertexList[Int.Parse(values[1])];
                b = mesh.VertexList[Int.Parse(values[2])];
                c = mesh.VertexList[Int.Parse(values[3])];
            }
            catch (Exception)
            {
                Console.WriteLine("An error happened when loading the model: Getting vertices for the faces");
                mesh.Unload();
                return false;
            }

            Uv uvA = (a, mesh.Uvs.ElementAtOrDefault(index + 0));
            Uv uvB = (b, mesh.Uvs.ElementAtOrDefault(index + 1));
            Uv uvC = (c, mesh.Uvs.ElementAtOrDefault(index + 2));

            Edge ab, bc, ca;

            try
            {
                ab = mesh.EdgeList[Int.Parse(values[4])];
                bc = mesh.EdgeList[Int.Parse(values[5])];
                ca = mesh.EdgeList[Int.Parse(values[6])];
            }
            catch (Exception)
            {
                Console.WriteLine("An error happened when loading the model: Getting edges for the faces");
                mesh.Unload();
                return false;
            }

            Triangle triangle = new Triangle(a, b, c, uvA, uvB, uvC, ab, bc, ca);
            mesh.AddTriangleSimple(triangle);
            index += 3;
        }

        mesh.CheckUselessVertices();
        mesh.CheckUselessEdges();
        mesh.CheckUselessTriangles();

        mesh.RegenerateAll();

        return true;
    }

    private static string Str(Vector2 v) => $"{Float.Str(v.X)}/{Float.Str(v.Y)}";
    private static string Str(Vector3 v) => $"{Float.Str(v.X)}/{Float.Str(v.Y)}/{Float.Str(v.Z)}";
    private static string Str(Vector4 v) => $"{Float.Str(v.X)}/{Float.Str(v.Y)}/{Float.Str(v.Z)}/{Float.Str(v.W)}";
    private static string Str(Quaternion q) => $"{Float.Str(q.X)}/{Float.Str(q.Y)}/{Float.Str(q.Z)}/{Float.Str(q.W)}";

    public struct LoadInfo
    {
        public Vector3 Min;
        public Vector3 Max;
    }
}