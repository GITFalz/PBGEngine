using PBG;
using PBG.Data;
using PBG.MathLibrary;
using PBG.UI;
using PBG.UI.FileManager;
using PBG.Graphics;
using PBG.Threads;
using Silk.NET.Input;

public class ModelingEditingMode : ModelingBase 
{
    public static RenderType selectionType = RenderType.Vertex;

    /*
    public static ShaderProgram selectionShader = new ShaderProgram("Selection/Selection.vert", "Selection/Selection.frag");
    public static int SelectionModelLocation = selectionShader.GetLocation("model");
    public static int SelectionProjectionLocation = selectionShader.GetLocation("projection");
    public static int SelectionSizeLocation = selectionShader.GetLocation("selectionSize");
    public static int SelectionColorLocation = selectionShader.GetLocation("color");

    public static VAO selectionVao = new();
    */

    public Action[] Selection = [ () => { }, () => { }, () => { } ];
    public Action[] Extrusion = [ () => { }, () => { }, () => { } ];
    public Func<bool>[] Deletion = [ () => false, () => false, () => false ];

    public Vector3 selectedCenter = Vector3.Zero;
    public float rotation = 0;
    public bool renderSelection = false;
    public Vector2 oldMousePos;

    public Vector2 _rotationAxis = Vector2.Zero;
    
    public ModelingEditingMode(ModelingEditor editor) : base(editor) 
    {
        Selection[0] = HandleVertexSelection;
        Selection[1] = HandleEdgeSelection;
        Selection[2] = HandleFaceSelection;

        Extrusion[0] = HandleVertexExtrusion;
        Extrusion[1] = HandleEdgeExtrusion;
        Extrusion[2] = HandleFaceExtrusion;

        Deletion[0] = HandleVertexDeletion;
        Deletion[1] = HandleEdgeDeletion;
        Deletion[2] = HandleTriangleDeletion;
    }

    public override void Start()
    {
        ModelSettings.WireframeVisible = true;
        Model?.UpdateVertexPosition();
        Regenerate = true;
    }

    public override void Resize()
    {

    }

    public override void Update()
    {
        renderSelection = false;

        if (Model == null)
            return;

        if (!FreeCamera)
        {
            if (!FileManager.IsHovering && Input.IsKeyDown(Key.ControlLeft))
            {
                // Undo
                if (Input.IsKeyPressed(Key.Z)) Handle_Undo();

                // Copy
                if (Input.IsKeyPressed(Key.C)) Handle_Copy();

                // Paste
                if (Input.IsKeyPressed(Key.V)) Handle_Paste();

                // Select all
                if (Input.IsKeyPressed(Key.A)) Handle_SelectAllVertices();

                // New Face
                if (Input.IsKeyPressed(Key.F)) Handle_GenerateNewFace();

                // Flip Selection
                if (Input.IsKeyPressed(Key.H)) Handle_FlipSelection();

                // Flipping triangle
                if (Input.IsKeyPressed(Key.I)) Handle_FlipTriangleNormal();

                // Deleting triangle
                if (Input.IsKeyPressed(Key.D)) Handle_TriangleDeletion();

                // Merging
                if (Input.IsKeyPressed(Key.K) && Model.SelectedVertices.Count >= 2) Handle_VertexMerging();

                // Split vertices
                if (Input.IsKeyPressed(Key.Q)) Handle_VertexSpliting();

                // Mapping
                if (Input.IsKeyPressed(Key.M)) Handle_Mapping();

                if (Input.IsKeyPressed(Key.T)) TestFunction();

                // Seperate selection
                if (Input.IsKeyPressed(Key.L)) Handle_SeperateSelection();

                // Combining Duplicate Vertices
                if (Input.IsKeyPressed(Key.G)) CombineDuplicateVertices();

                // Delete model
                if (Input.IsKeyPressed(Key.Delete)) Model.Delete();

                // Check for useless vertices
                if (Input.IsKeyPressed(Key.B)) Model.Mesh.CombineDuplicateVertices();
            }
            else if (!FileManager.IsHovering)
            {
                // Extrude
                if (Input.IsKeyPressed(Key.E)) Handle_Extrusion();

                // Rotation
                if (Input.IsKeyPressed(Key.R)) RotationInit();
                if (Input.IsKeyDown(Key.R)) Handle_RotateSelectedVertices();
                if (Input.IsKeyReleased(Key.R)) Model.UpdateVertexPosition();

                // Scaling
                if (Input.IsKeyPressed(Key.S)) ScalingInit();
                if (Input.IsKeyDown(Key.S)) Handle_ScalingSelectedVertices();
                if (Input.IsKeyReleased(Key.S))
                {
                    Model.UpdateVertexPosition();
                    _currentScalePositions = [];
                    _currentScale = 1f;
                }

                // Moving
                if (Input.IsKeyPressed(Key.G)) StashMesh();
                if (Input.IsKeyDown(Key.E) || Input.IsKeyDown(Key.G)) Handle_MovingSelectedVertices();
                if (Input.IsKeyReleased(Key.E) || Input.IsKeyReleased(Key.G))
                {
                    ModelSettings.SnappingOffset = Vector3.Zero;
                    Model.Mesh.CheckUselessEdges();
                    Model.Mesh.CheckUselessTriangles();
                    Regenerate = true;
                }
            }

            if (!FileManager.IsHovering && Input.IsMousePressed(MouseButton.Left) && Editor.Editor.UI.HoveringCenter)
            {
                Selection[(int)selectionType]();
            }

            if (Input.IsMousePressed(MouseButton.Left))
            {
                oldMousePos = Input.GetMousePosition();
            }

            if (!FileManager.IsHovering && Input.IsMouseDown(MouseButton.Left) && !Editor.blocked)
            {
                renderSelection = true;

                Vector2 mousePos = Input.GetMousePosition();
                Vector2 max = Mathf.Max(mousePos, oldMousePos);
                Vector2 min = Mathf.Min(mousePos, oldMousePos);
                float distance = Vector2.Distance(mousePos, oldMousePos);
                bool regenColor = false;

                if (distance < 5)
                    return;

                if (selectionType == RenderType.Vertex)
                {
                    for (int i = 0; i < Model.Mesh.VertexList.Count; i++)
                    {
                        var vertex = Model.Mesh.VertexList[i];
                        var position = vertex.Screen;
                        if (position.X >= min.X && position.X <= max.X && position.Y >= min.Y && position.Y <= max.Y)
                        {
                            if (!Model.SelectedVertices.Contains(vertex))
                            {
                                regenColor = true;
                                Model.SelectedVertices.Add(vertex);
                            }
                        }
                        else
                        {
                            if (!Input.IsKeyDown(Key.ShiftLeft) && Model.SelectedVertices.Contains(vertex))
                            {
                                regenColor = true;
                                Model.SelectedVertices.Remove(vertex);
                            }
                        }
                    }
                }
                else if (selectionType == RenderType.Face)
                {
                    for (int i = 0; i < Model.Triangles.Count; i++)
                    {
                        var (triangle, position, _) = Model.Triangles[i];
                        if (position.X >= min.X && position.X <= max.X && position.Y >= min.Y && position.Y <= max.Y)
                        {
                            if (!Model.SelectedTriangles.Contains(triangle))
                            {
                                regenColor = true;
                                Model.SelectedTriangles.Add(triangle);
                            }
                        }
                        else
                        {
                            if (!Input.IsKeyDown(Key.ShiftLeft) && Model.SelectedTriangles.Contains(triangle))
                            {
                                regenColor = true;
                                Model.SelectedTriangles.Remove(triangle);
                            }
                        }
                    }
                }

                if (regenColor)
                    Model.GenerateVertexColor();
            }
        }

        /*
        if (!FreeCamera)
        {
            if (TransformGizmo.Hover(out var triangle) && Input.IsMousePressed(MouseButton.Left))
            {
                HoldingTransform = true;
                Game.SetCursorState(CursorState.Grabbed);
            }

            if (HoldingTransform && Input.MouseDelta != Vector2.Zero && triangle != null)
            {
                //TransformGizmoAction(Model.Rotation, Input.MouseDelta, triangle.Value.Info, MoveSelectedModels);
            }

            if (RotationGizmo.Hover(out triangle) && Input.IsMousePressed(MouseButton.Left))
            {
                HoldingRotation = true;
                _rotationAxis = RotationGizmo.SliderDirection(triangle.Value.GetAxis());
                Game.SetCursorState(CursorState.Grabbed);
            }

            if (HoldingRotation && Input.MouseDelta != Vector2.Zero && triangle != null)
            {
                //RotationGizmoAction(Model.Rotation, Input.MouseDelta, _rotationAxis, triangle.Value.Info, RotateSelectedModels);
            }
        }

        if (Input.IsMouseReleased(MouseButton.Left))
        {
            if (HoldingTransform)
            {
                HoldingTransform = false;
                TransformGizmo.GenerateWorldSpacePoints();
            }
            
            if (HoldingRotation)
            {
                HoldingRotation = false;
                RotationGizmo.GenerateWorldSpacePoints();
            }

            Game.SetCursorState(CursorState.Normal);
        }
        */

        if (Regenerate)
        {
            Model.UpdateVertexPosition();
            Model.GenerateVertexColor();
            Regenerate = false;
        }
    }

    public override void Render()
    {
        /*
        if (renderSelection)
        {
            selectionShader.Bind();

            Matrix4 model = Matrix4.CreateTranslation((oldMousePos.X - 200, oldMousePos.Y - 50, 0));
            Matrix4 projection = Editor.WindowProjection;
            Vector2 selectionSize = Input.GetMousePosition() - oldMousePos;
            Vector3 color = new Vector3(1, 0.5f, 0.25f);

            GL.UniformMatrix4(SelectionModelLocation, true, ref model);
            GL.UniformMatrix4(SelectionProjectionLocation, true, ref projection);
            GL.Uniform2(SelectionSizeLocation, selectionSize);
            GL.Uniform3(SelectionColorLocation, color);

            selectionVao.Bind();

            GL.DrawArrays(PrimitiveType.Lines, 0, 8);

            selectionVao.Unbind();

            selectionShader.Unbind();
        }
        */

        /*
        if (Model != null && Model.SelectedVertices.Count > 0)
        {
            TransformGizmo.Render();
            GL.Disable(EnableCap.CullFace);
            RotationGizmo.Render();
            GL.Enable(EnableCap.CullFace);
        }
        */
    }

    public override void Exit()
    {
        
    }

    public void Handle_GetConnectedVertices()
    {
        Model?.GetConnectedVertices();
    }


    // Selection
    public void HandleVertexSelection()
    {
        if (Model == null)
            return;

        if (!Input.IsKeyDown(Key.ShiftLeft))
        {
            Editor.ResetGizmoRotation();
            Model.SelectedVertices.Clear();
        }

        Vector2 mousePos = Input.GetMousePosition();
        Vector2? closest = null;
        Vertex? closestVert = null;

        for (int i = 0; i < Model.Mesh.VertexList.Count; i++)
        {
            var vertex = Model.Mesh.VertexList[i];
            var position = vertex.Screen;
            float distance = Vector2.Distance(mousePos, position);
            float distanceClosest = closest == null ? 1000 : Vector2.Distance(mousePos, (Vector2)closest);

            if (distance < distanceClosest && distance < 10)
            {
                closest = position;
                closestVert = vertex;
            }
        }

        if (closestVert != null && !Model.SelectedVertices.Remove(closestVert))
            Model.SelectedVertices.Add(closestVert);

        Model.GenerateVertexColor();
    }

    public void HandleEdgeSelection()
    {
        
    }

    public void HandleFaceSelection()
    {
        if (Model == null)
            return;

        if (!Input.IsKeyDown(Key.ShiftLeft))
            Model.SelectedTriangles.Clear();

        Vector2 mousePos = Input.GetMousePosition();
        Vector2? closest = null;
        Triangle? closestTriangle = null;

        for (int i = 0; i < Model.Triangles.Count; i++)
        {
            var (triangle, position, _) = Model.Triangles[i];
            float distance = Vector2.Distance(mousePos, position);
            float distanceClosest = closest == null ? 1000 : Vector2.Distance(mousePos, (Vector2)closest);

            if (distance < distanceClosest && distance < 10)
            {
                closest = position;
                closestTriangle = triangle;
            }
        }

        if (closestTriangle != null && !Model.SelectedTriangles.Remove(closestTriangle))
            Model.SelectedTriangles.Add(closestTriangle);

        Model.GenerateVertexColor();
    }


    // Extrusion
    public void Handle_Extrusion()
    {
        if (Model == null)
            return;
            
        StashMesh();
        
        Console.WriteLine("Extruding");
        Extrusion[(int)selectionType]();

        if (!CanGenerateBuffers)
            return;

        Model.Mesh.RegenerateAll();

        Regenerate = true;
    }

    public void HandleVertexExtrusion()
    {
        if (Model == null)
            return;
            
        HashSet<Vertex> newVertices = [];

        foreach (var vertex in Model.SelectedVertices)
        {
            Vertex newVertex = vertex.Copy();
            newVertices.Add(newVertex);

            Model.Mesh.EdgeList.Add(new Edge(vertex, newVertex));
        }

        Model.SelectedVertices = [];
        Model.SelectedVertices = newVertices;

        Model.GenerateVertexColor();

        Model.Mesh.AddVertices(newVertices);
    }

    public void HandleEdgeExtrusion()
    {

    }

    public void HandleFaceExtrusion()
    {

    }


    // Deletion
    public void Handle_TriangleDeletion(bool stash = true)
    {
        if (Model == null)
            return;
            
        if (stash)
            StashMesh();

        if (!Deletion[(int)selectionType]() || !CanGenerateBuffers)
            return;
            
        Model.Mesh.RegenerateAll();

        Regenerate = true;
    }
    public bool HandleVertexDeletion()
    {
        if (Model == null)
            return false;
            
        if (Model.SelectedVertices.Count == 0)
            return false;

        foreach (var vert in Model.SelectedVertices)
        {
            Model.Mesh.RemoveVertex(vert);
        }
        Model.SelectedVertices.Clear();

        return true;
    }

    public bool HandleEdgeDeletion()
    {
        if (Model == null)
            return false;
            
        if (Model.SelectedVertices.Count == 0)
            return false;

        List<Edge> edges = Model.GetFullSelectedEdges(Model.SelectedVertices);
        foreach (var edge in edges)
        {
            Model.Mesh.RemoveEdge(edge);
        }

        Model.SelectedVertices.Clear();
        Model.SelectedEdges.Clear();

        return true;
    }

    public bool HandleTriangleDeletion()
    {
        if (Model == null)
            return false;
            
        if (Model.SelectedTriangles.Count > 0)
        {
            foreach (var triangle in Model.SelectedTriangles)
            {
                Model.Mesh.RemoveTriangle(triangle);
            }
            Model.SelectedVertices.Clear();

            return true;
        }
        return false;
    }   

    public static bool TriangleDeletion(ModelMesh mesh, List<Vertex> selectedVertices)
    {
        var triangles = Model.GetFullSelectedTriangles(selectedVertices);

        if (triangles.Count > 0)
        {
            foreach (var triangle in triangles)
            {
                mesh.RemoveTriangle(triangle);
            }
            selectedVertices.Clear();

            return true;
        }
        return false;
    }



    public void Handle_FlipTriangleNormal()
    {
        if (Model == null)
            return;

        StashMesh();

        var triangles = Model.GetFullSelectedTriangles(Model.SelectedVertices);

        if (triangles.Count > 0)
        {
            foreach (var triangle in triangles)
            {
                Vertex A = triangle.A;
                Vertex B = triangle.B;
                if (Model.Mesh.SwaPBGertices(A, B))
                {
                    triangle.Invert();
                    triangle.UpdateNormal();
                }
            }

            Model.Mesh.Init();
            Model.Mesh.UpdateMesh();
        }
    }

    public void Handle_Mapping()
    {
#if MYDEBUG
        Console.WriteLine("=== Handle_Mapping Started ===");
#endif

        if (Model == null)
        {
#if MYDEBUG
            Console.WriteLine("Model is null, returning early");
#endif
            return;
        }

        if (Model.SelectedVertices.Count == 0)
            return;

        StashMesh();

        var triangles = Model.GetFullSelectedTriangles(Model.SelectedVertices);
        if (triangles.Count == 0)
            return;
        
        CanStash = false;
        CanGenerateBuffers = false;

        List<BoundingBoxRegion> boundingBoxes = [];

        Vector3 offset = (0, 0, 0);

        Vector3 min = Vector3.Zero;
        Vector3 max = Vector3.Zero;

        List<(Triangle original, Triangle copy)> copiedTriangles = [];
        List<Vertex> copiedVertices = [];

        List<(Vector3, Vector3, List<Vertex>)> minMax = [];
        float approximateVolume = 0;

        // Basic flattening and packing
        int regionCount = 0;
        while (triangles.Count > 0)
        {
            regionCount++;

            // Get a region of triangles from a mesh to flatten out
            var tris = triangles.First();
            var selectedTriangles = tris.GetTriangleRegion([], triangles);
            triangles.RemoveWhere(selectedTriangles.Contains);

            Dictionary<Vertex, Vertex> copyVertex = [];
            Dictionary<Edge, Edge> copyEdge = [];
            List<Triangle> triangleCopies = [];

            foreach (var t in selectedTriangles)
            {
                void EnsureVertex(Vertex v) 
                {
                    if (copyVertex.TryAdd(v, v.Copy()))
                        copiedVertices.Add(copyVertex[v]);
                }

                void EnsureEdge(Edge e, Vertex v1, Vertex v2)
                {
                    if (copyEdge.ContainsKey(e)) 
                        return;

                    EnsureVertex(v1);
                    EnsureVertex(v2);
                    copyEdge[e] = new(copyVertex[v1], copyVertex[v2]);
                }

                // Ensure edges and vertices exist
                EnsureEdge(t.AB, t.A, t.B);
                EnsureEdge(t.BC, t.B, t.C);
                EnsureEdge(t.CA, t.C, t.A);

                // Create the copied triangle
                var newT = new Triangle(
                    copyVertex[t.A], 
                    copyVertex[t.B], 
                    copyVertex[t.C], 
                    copyEdge[t.AB], 
                    copyEdge[t.BC], 
                    copyEdge[t.CA]
                );

                triangleCopies.Add(newT);
                copiedTriangles.Add((t, newT));
            }
            Model.Handle_Flattening(triangleCopies);

            // if any vertices are present:
            // 1. Flip the region if the normal of the first triangle is facing down
            // 2. Get the smallest possible bounding box of the selected region and rotate it if needed before moving it next to the last one
            var newVerts = Model.GetVertices(triangleCopies);
            if (newVerts.Count != 0)
            {
                // Flip the region if the normal of the first triangle is facing down
                if (triangleCopies.Count > 0)
                {
                    Triangle first = triangleCopies[0];
                    first.UpdateNormal();

                    if (Vector3.Dot(first.Normal, (0, 1, 0)) < 0 && newVerts.Count > 0)
                    {
                        Vector3 center = Vector3.Zero;
                        foreach (var vert in newVerts)
                        {
                            center += vert;
                        }
                        center /= newVerts.Count;

                        foreach (var vert in newVerts)
                        {
                            vert.SetPosition(Mathf.RotatePoint(vert, center, (1, 0, 0), 180f));
                        }
                    }
                }

                // Get the smallest possible bounding box of the selected region and rotate it if needed before moving it next to the last one
                Mathf.GetSmallestBoundingBox(newVerts, out min, out max);

                minMax.Add((min, max, [.. newVerts]));

                Vector2 size = max.Xz - min.Xz;
                float volume = size.X * size.Y;
                approximateVolume += volume;
            }
        }

        double sideLength = Math.Sqrt(approximateVolume);
        float skip = (float)sideLength / 50;

        int vertexMoveCount = 0;
        foreach (var (Min, Max, vertices) in minMax)
        {
            Vector3 size = Max - Min;
            Vector3 vOffset = (offset.X - Min.X, 0, -Min.Z);

            BoundingBoxRegion region = new BoundingBoxRegion(Min + vOffset, Max + vOffset, vertices);
            boundingBoxes.Add(region);

            foreach (var vert in vertices)
            {
                vert.SetPosition(vert + vOffset);
                vertexMoveCount++;
            }

            offset.X += size.X + skip;
        }

        // Better packing algorithm

        // Packing algorithm
        bool packed = false;
        int packingIteration = 0;
        while (!packed)
        {
            packingIteration++;

            bool needsPacking = false;

            // Get the last region and remove it from the list so we can test it against the others
            BoundingBoxRegion last = boundingBoxes[^1];
            boundingBoxes.RemoveAt(boundingBoxes.Count - 1);
            Vector3 lastSize = last.Size;

            Vector3 bestMin = Vector3.Zero;
            int bestIndex = 0;
            bool foundAtLeastOne = false;

            for (int i = 0; i < boundingBoxes.Count; i++)
            {
                min = boundingBoxes[i].Min;
                max = boundingBoxes[i].Max;

                if (min.X >= sideLength) // If the region is too far to the right, skip it
                {
                    needsPacking = true;
                    continue;
                }

                Vector3 testMinLeft = (min.X, 0, max.Z + skip); // Tesing the region above the current one on the left side
                Vector3 testMinRight = (max.X - lastSize.X, 0, max.Z + skip); // Tesing the region above the current one on the right side

                bool collidingLeft = false;
                for (int j = 0; j < boundingBoxes.Count; j++)
                {
                    if (i == j)
                        continue;

                    last.SetMin(testMinLeft);
                    var bB = boundingBoxes[j];

                    if (last & bB)
                    {
                        collidingLeft = true;
                        break;
                    }
                }

                if (!collidingLeft)
                {
                    bestIndex = i;

                    if (!foundAtLeastOne || testMinLeft.Z < bestMin.Z) // Only set the best index if it is the first one or if it is smaller than the current best
                    {
                        bestMin = testMinLeft;
                        foundAtLeastOne = true;
                    }
                    continue;
                }

                if (max.X < sideLength) // Only test the right side if the region is not too far to the right
                {
                    bool collidingRight = false;
                    for (int j = 0; j < boundingBoxes.Count; j++)
                    {
                        if (i == j)
                            continue;

                        last.SetMin(testMinRight);
                        var bB = boundingBoxes[j];

                        if (last & bB)
                        {
                            collidingRight = true;
                            break;
                        }
                    }

                    if (!collidingRight)
                    {
                        bestIndex = i;
                        if (!foundAtLeastOne || testMinRight.Z < bestMin.Z)
                        {
                            bestMin = testMinRight;
                            foundAtLeastOne = true;
                        }
                        continue;
                    }
                }
            }

            last.SetMin(bestMin);
            boundingBoxes.Insert(bestIndex, last);

            if (!needsPacking)
            {
                packed = true;
                break;
            }
        }

        // Move the regions to their new positions
        foreach (var region in boundingBoxes)
        {
            Vector3 o = region.Min - region.OriginalMin;
            foreach (var vert in region.Vertices)
            {
                vert.SetPosition(vert + o);
            }
        }

        // Get the current bounding box
        min = (float.MaxValue, float.MaxValue, float.MaxValue);
        max = (float.MinValue, float.MinValue, float.MinValue);

        foreach (var vert in copiedVertices)
        {
            min = Mathf.Min(min, vert);
            max = Mathf.Max(max, vert);
        }

        // Move the mesh to 0,0,0
        foreach (var vert in copiedVertices)
        {
            vert.SetPosition(vert - min);
        }

        min = Vector3.Zero;
        max -= min;

        Vector3 bSize = max - min;
        float largestSide = Mathf.Max(bSize.X, bSize.Z);

        Vector2 uvMin = Vector2.One;
        Vector2 uvMax = Vector2.Zero;

        for (int i = 0; i < copiedTriangles.Count; i++)
        {
            var (original, copy) = copiedTriangles[i];

            Vector2 uvA = (copy.A.X / largestSide, copy.A.Z / largestSide);
            Vector2 uvB = (copy.B.X / largestSide, copy.B.Z / largestSide);
            Vector2 uvC = (copy.C.X / largestSide, copy.C.Z / largestSide);

            uvMin = Mathf.Min(uvA, uvB, uvC, uvMin);
            uvMax = Mathf.Max(uvA, uvB, uvC, uvMax);

            original.UvA = uvA;
            original.UvB = uvB;
            original.UvC = uvC;
        }

        Vector2 uvSize = uvMax - uvMin;
        float smallest = Mathf.Max(uvSize.X, uvSize.Y);
        float multiplier = smallest == 0 ? 1 : 1 / smallest;

        for (int i = 0; i < copiedTriangles.Count; i++)
        {
            var (original, _) = copiedTriangles[i];
            original.UvA = (original.UvA - uvMin) * multiplier;
            original.UvB = (original.UvB - uvMin) * multiplier;
            original.UvC = (original.UvC - uvMin) * multiplier;
        }

        CanStash = true;
        CanGenerateBuffers = true;
        Regenerate = true;

        Model?.Mesh.CheckUselessVertices();
        Model?.Mesh.UpdateAndRegenerateAll();
    }

    /*
    public void Handle_Mapping()
    {
        Console.WriteLine("=== Handle_Mapping Started ===");

        if (Model == null)
        {
            Console.WriteLine("Model is null, returning early");
            return;
        }

        StashMesh();
        Console.WriteLine("Mesh stashed");

        Handle_SelectAllVertices();
        Console.WriteLine("All vertices selected");

        CanStash = false;
        CanGenerateBuffers = false;
        Console.WriteLine("CanStash and CanGenerateBuffers set to false");

        List<Triangle> triangles = Model.GetFullSelectedTriangles(Model.SelectedVertices);
        Console.WriteLine($"Initial triangles count: {triangles.Count}");

        Dictionary<string, Triangle> trianglesDict = [];
        List<BoundingBoxRegion> boundingBoxes = [];

        for (int i = 0; i < triangles.Count; i++) { triangles[i].ID = i.ToString(); trianglesDict.Add(i.ToString(), triangles[i]); }
        Console.WriteLine($"Triangles dictionary created with {trianglesDict.Count} entries");

        ModelMesh tempMesh = new(Model);

        Vector3 offset = (0, 0, 0);

        Vector3 min = Vector3.Zero;
        Vector3 max = Vector3.Zero;

        ModelCopy copy = new ModelCopy(Model.SelectedVertices);
        Console.WriteLine($"ModelCopy created with {Model.SelectedVertices.Count} vertices");

        List<(Vector3, Vector3, List<Vertex>)> minMax = [];
        float approximateVolume = 0;

        // Basic flattening and packing
        Console.WriteLine("\n--- Starting Basic Flattening and Packing ---");
        int regionCount = 0;
        while (triangles.Count > 0)
        {
            regionCount++;
            Console.WriteLine($"\nProcessing region {regionCount}, remaining triangles: {triangles.Count}");

            // Get a region of triangles from a mesh to flatten out
            Model.SelectedTriangles = triangles[0].GetTriangleRegion([]).ToList();
            Console.WriteLine($"Selected triangle region with {Model.SelectedTriangles.Count} triangles");

            Model.SelectedVertices = Model.GetVertices(Model.SelectedTriangles);
            Console.WriteLine($"Selected {Model.SelectedVertices.Count} vertices for region");

            triangles.RemoveAll(t => Model.SelectedTriangles.Contains(t));
            Console.WriteLine($"Triangles remaining after removal: {triangles.Count}");

            // Remove region from the original mesh and flatten the region
            Handle_SeperateSelection();
            Console.WriteLine("Selection separated");

            MoveSelectedVertices(offset);
            Console.WriteLine($"Vertices moved by offset: {offset}");

            Handle_Flattening();
            Console.WriteLine("Flattening applied");

            // if any vertices are present:
            // 1. Flip the region if the normal of the first triangle is facing down
            // 2. Get the smallest possible bounding box of the selected region and rotate it if needed before moving it next to the last one
            if (Model.SelectedVertices.Count != 0)
            {
                Console.WriteLine($"Processing {Model.SelectedVertices.Count} vertices in region");

                // Flip the region if the normal of the first triangle is facing down
                List<Triangle> tris = Model.GetFullSelectedTriangles(Model.SelectedVertices);
                Console.WriteLine($"Got {tris.Count} triangles for flip check");

                if (tris.Count > 0)
                {
                    Triangle first = tris[0];
                    first.UpdateNormal();
                    Console.WriteLine($"First triangle normal: {first.Normal}");

                    if (Vector3.Dot(first.Normal, (0, 1, 0)) < 0)
                    {
                        Console.WriteLine("Normal is facing down, calculating center for flip");
                        Vector3 center = Model.SelectedVertices[0];
                        for (int i = 1; i < Model.SelectedVertices.Count; i++)
                        {
                            center += Model.SelectedVertices[i];
                        }
                        center /= Model.SelectedVertices.Count;
                        Console.WriteLine($"Center calculated: {center}");

                        foreach (var vert in Model.SelectedVertices)
                        {
                            vert.SetPosition(Mathf.RotatePoint(vert, center, (1, 0, 0), 180f));
                        }
                        Console.WriteLine("Flip code is commented out");
                    }
                    else
                    {
                        Console.WriteLine("Normal is facing up, no flip needed");
                    }
                }

                // Get the smallest possible bounding box of the selected region and rotate it if needed before moving it next to the last one
                Mathf.GetSmallestBoundingBox(Model.SelectedVertices, out min, out max);
                Console.WriteLine("GetSmallestBoundingBox is commented out");

                minMax.Add((min, max, [.. Model.SelectedVertices]));
                Console.WriteLine($"Added to minMax list. Min: {min}, Max: {max}");

                Vector2 size = max.Xz - min.Xz;
                float volume = size.X * size.Y;
                approximateVolume += volume;
                Console.WriteLine($"Region size: {size}, volume: {volume}, total approximate volume: {approximateVolume}");
            }
        }

        Console.WriteLine($"\n--- Basic Flattening Complete. Processed {regionCount} regions ---");

        double sideLength = Math.Sqrt(approximateVolume);
        float skip = (float)sideLength / 50;
        Console.WriteLine($"Side length: {sideLength}, skip distance: {skip}");

        Console.WriteLine("\n--- Applying Initial Offsets ---");
        int vertexMoveCount = 0;
        foreach (var (Min, Max, vertices) in minMax)
        {
            Vector3 size = Max - Min;
            Vector3 vOffset = (offset.X - Min.X, 0, -Min.Z);
            Console.WriteLine($"Processing region with {vertices.Count} vertices. Size: {size}, Offset: {vOffset}");

            BoundingBoxRegion region = new BoundingBoxRegion(Min + vOffset, Max + vOffset, vertices);
            boundingBoxes.Add(region);

            foreach (var vert in vertices)
            {
                vert.MovePosition(vOffset);
                vertexMoveCount++;
            }

            offset.X += size.X + skip;
            Console.WriteLine($"New offset.X: {offset.X}");
        }
        Console.WriteLine($"Total vertices moved: {vertexMoveCount}");
        Console.WriteLine($"Total bounding boxes: {boundingBoxes.Count}");

        Handle_SelectAllVertices(false);
        Console.WriteLine("All vertices selected (false parameter)");

        foreach (var tris in Model.GetFullSelectedTriangles(Model.SelectedVertices))
            trianglesDict[tris.ID] = tris;
        Console.WriteLine("Triangles dictionary updated");

        // Better packing algorithm
        Console.WriteLine("\n--- Starting Better Packing Algorithm ---");

        // Packing algorithm
        bool packed = false;
        int packingIteration = 0;
        while (!packed)
        {
            packingIteration++;
            Console.WriteLine($"\nPacking iteration {packingIteration}");

            bool needsPacking = false;

            // Get the last region and remove it from the list so we can test it against the others
            BoundingBoxRegion last = boundingBoxes[^1];
            boundingBoxes.RemoveAt(boundingBoxes.Count - 1);
            Vector3 lastSize = last.Size;
            Console.WriteLine($"Testing last region. Size: {lastSize}, Remaining regions: {boundingBoxes.Count}");

            Vector3 bestMin = Vector3.Zero;
            int bestIndex = 0;
            bool foundAtLeastOne = false;

            for (int i = 0; i < boundingBoxes.Count; i++)
            {
                min = boundingBoxes[i].Min;
                max = boundingBoxes[i].Max;
                //Console.WriteLine($"  Testing against region {i}. Min: {min}, Max: {max}");

                if (min.X >= sideLength) // If the region is too far to the right, skip it
                {
                    //Console.WriteLine($"  Region {i} is too far right (min.X: {min.X} >= sideLength: {sideLength}), needs packing");
                    needsPacking = true;
                    continue;
                }

                Vector3 testMinLeft = (min.X, 0, max.Z + skip); // Tesing the region above the current one on the left side
                Vector3 testMinRight = (max.X - lastSize.X, 0, max.Z + skip); // Tesing the region above the current one on the right side
                //Console.WriteLine($"  Test positions - Left: {testMinLeft}, Right: {testMinRight}");

                bool collidingLeft = false;
                for (int j = 0; j < boundingBoxes.Count; j++)
                {
                    if (i == j)
                        continue;

                    last.SetMin(testMinLeft);
                    var bB = boundingBoxes[j];

                    if (last & bB)
                    {
                        collidingLeft = true;
                        //Console.WriteLine($"  Left position collides with region {j}");
                        break;
                    }
                }

                if (!collidingLeft)
                {
                    //Console.WriteLine($"  Left position is valid");
                    bestIndex = i;

                    if (!foundAtLeastOne || testMinLeft.Z < bestMin.Z) // Only set the best index if it is the first one or if it is smaller than the current best
                    {
                        bestMin = testMinLeft;
                        foundAtLeastOne = true;
                        //Console.WriteLine($"  New best position found at left: {bestMin}");
                    }
                    continue;
                }

                if (max.X < sideLength) // Only test the right side if the region is not too far to the right
                {
                    //Console.WriteLine($"  Testing right side (max.X: {max.X} < sideLength: {sideLength})");
                    bool collidingRight = false;
                    for (int j = 0; j < boundingBoxes.Count; j++)
                    {
                        if (i == j)
                            continue;

                        last.SetMin(testMinRight);
                        var bB = boundingBoxes[j];

                        if (last & bB)
                        {
                            collidingRight = true;
                            //Console.WriteLine($"  Right position collides with region {j}");
                            break;
                        }
                    }

                    if (!collidingRight)
                    {
                        //Console.WriteLine($"  Right position is valid");
                        bestIndex = i;
                        if (!foundAtLeastOne || testMinRight.Z < bestMin.Z)
                        {
                            bestMin = testMinRight;
                            foundAtLeastOne = true;
                            //Console.WriteLine($"  New best position found at right: {bestMin}");
                        }
                        continue;
                    }
                }
            }

            last.SetMin(bestMin);
            boundingBoxes.Insert(bestIndex, last);
            Console.WriteLine($"Region inserted at index {bestIndex} with position {bestMin}");

            if (!needsPacking)
            {
                packed = true;
                Console.WriteLine("Packing complete, no more regions need packing");
                break;
            }
        }
        Console.WriteLine($"Packing algorithm finished after {packingIteration} iterations");

        // Move the regions to their new positions
        Console.WriteLine("\n--- Moving Regions to Final Positions ---");
        foreach (var region in boundingBoxes)
        {
            Vector3 o = region.Min - region.OriginalMin;
            Console.WriteLine($"Moving region by offset: {o}, affecting {region.Vertices.Count} vertices");
            foreach (var vert in region.Vertices)
            {
                vert.MovePosition(o);
            }
        }

        // Get the current bounding box
        Console.WriteLine("\n--- Calculating Final Bounding Box ---");
        min = (float.MaxValue, float.MaxValue, float.MaxValue);
        max = (float.MinValue, float.MinValue, float.MinValue);

        Handle_SelectAllVertices(false);
        Console.WriteLine($"All vertices selected: {Model.SelectedVertices.Count} vertices");

        foreach (var vert in Model.SelectedVertices)
        {
            min = Mathf.Min(min, vert);
            max = Mathf.Max(max, vert);
        }
        Console.WriteLine($"Final bounding box - Min: {min}, Max: {max}");

        // Move the mesh to 0,0,0
        Console.WriteLine("Moving mesh to origin (0,0,0)");
        foreach (var vert in Model.SelectedVertices)
        {
            vert.MovePosition(-min);
        }

        min = Vector3.Zero;
        max -= min;
        Console.WriteLine($"Adjusted bounding box - Min: {min}, Max: {max}");

        Vector3 bSize = max - min;
        float largestSide = Mathf.Max(bSize.X, bSize.Z);
        Console.WriteLine($"Bounding box size: {bSize}, largest side: {largestSide}");

        Model.Mesh.Unload();
        Console.WriteLine("Mesh unloaded");

        Model.Mesh.AddCopy(copy.Copy());
        Console.WriteLine("Model copy added back to mesh");

        Handle_SelectAllVertices(false);
        triangles = Model.GetFullSelectedTriangles(Model.SelectedVertices);
        Console.WriteLine($"Final triangles count: {triangles.Count}");

        Vector2 uvMin = Vector2.One;
        Vector2 uvMax = Vector2.Zero;

        Console.WriteLine("\n--- Calculating UV Coordinates ---");
        foreach (var triangle in triangles)
        {
            Triangle oldTriangle = trianglesDict[triangle.ID];

            Vector2 uvA = (oldTriangle.A.X / largestSide, oldTriangle.A.Z / largestSide);
            Vector2 uvB = (oldTriangle.B.X / largestSide, oldTriangle.B.Z / largestSide);
            Vector2 uvC = (oldTriangle.C.X / largestSide, oldTriangle.C.Z / largestSide);

            uvMin = Mathf.Min(uvA, uvB, uvC, uvMin);
            uvMax = Mathf.Max(uvA, uvB, uvC, uvMax);

            triangle.UvA = uvA;
            triangle.UvB = uvB;
            triangle.UvC = uvC;
        }
        Console.WriteLine($"UV bounds - Min: {uvMin}, Max: {uvMax}");

        Vector2 uvSize = uvMax - uvMin;
        float smallest = Mathf.Max(uvSize.X, uvSize.Y);
        float multiplier = smallest == 0 ? 1 : 1 / smallest;
        Console.WriteLine($"UV size: {uvSize}, smallest: {smallest}, multiplier: {multiplier}");

        Console.WriteLine("Normalizing UV coordinates");
        foreach (var triangle in triangles)
        {
            triangle.UvA = (triangle.UvA - uvMin) * multiplier;
            triangle.UvB = (triangle.UvB - uvMin) * multiplier;
            triangle.UvC = (triangle.UvC - uvMin) * multiplier;
        }

        CanStash = true;
        CanGenerateBuffers = true;
        Console.WriteLine("CanStash and CanGenerateBuffers set to true");

        Model.Mesh.CheckUselessVertices();
        Console.WriteLine("Checked for useless vertices");

        Model.Mesh.UpdateAndRegenerateAll();
        Console.WriteLine("Mesh updated and regenerated");

        Regenerate = true;
        Console.WriteLine("Regenerate set to true");

        Console.WriteLine("\n=== Handle_Mapping Completed ===");
    }
    */
    
    public void MoveSelectedVertices(Vector3 move)
    {
        if (Model == null)
            return;
            
        Model.MoveSelectedVertices(Model, move, Model.SelectedVertices);
    }


    // Utility

    public void Handle_MovingSelectedVertices()
    {
        if (Model == null)
            return;
            
        Vector3 move = Editor.GetSnappingMovement();
        MoveSelectedVertices(move);

        if (!CanGenerateBuffers)
            return;

        Model.Mesh.UpdateNormals();
        Model.Mesh.UpdateAll();
    }

    public void RotationInit()
    {
        if (Model == null)
            return;
            
        StashMesh();
        rotation = 0;
        selectedCenter = Model.GetSelectedCenter(Model.SelectedVertices);
    }

    public void Handle_RotateSelectedVertices()
    {
        if (Model == null)
            return;
            
        if (Model.SelectedVertices.Count == 0)
            return;

        Vector3 axis = Camera.front * ModelSettings.Axis;
        if (axis.Length == 0) return;
        axis.Normalize();

        float mouseDelta = Input.GetMouseDelta().X;
        rotation += mouseDelta;

        if (ModelSettings.Snapping)
        {
            if (Mathf.Abs(rotation) >= ModelSettings.SnappingFactor)
                rotation = ModelSettings.SnappingFactor * Mathf.Sign(rotation);
            else    
                return;
        }

        var rotate = Model.Rotation != Quaternion.Identity;
        foreach (var vert in Model.SelectedVertices)
        {
            if (rotate)
            {
                var position = Vector3.Transform(vert.Position, Model.Rotation);
                position = Mathf.RotatePoint(position, selectedCenter, axis, rotation);
                vert.SetPosition(Vector3.Transform(position, Model.Rotation.Inverted()));
            }
            else
            {
                vert.SetPosition(Mathf.RotatePoint(vert, selectedCenter, axis, rotation));
            }
        }

        rotation = 0;

        if (!CanGenerateBuffers)
            return;

        Model.Mesh.UpdateNormals();
        Model.Mesh.UpdateAll();

        Regenerate = true;
    }

    public void ScalingInit()
    {
        if (Model == null)
            return;
            
        StashMesh();
        _currentScale = 1;
        selectedCenter = Model.GetSelectedCenter(Model.SelectedVertices);

        _currentScalePositions = [];
        foreach( var vert in Model.SelectedVertices)
        {
            _currentScalePositions.Add(vert);
        }
    }

    private float _currentScale = 1f;
    private List<Vector3> _currentScalePositions = [];

    public void Handle_ScalingSelectedVertices()
    {
        if (Model == null || Model.SelectedVertices.Count < 2)
            return;

        float mouseDelta = Input.GetMouseDelta().X * 0.1f;
        _currentScale += mouseDelta;

        if (ModelSettings.Snapping)
        {
            _currentScale = Mathf.Round(_currentScale / ModelSettings.SnappingFactor) * ModelSettings.SnappingFactor;
        }

        int i = 0;
        foreach( var vert in Model.SelectedVertices)
        { 
            Vector3 direction = _currentScalePositions[i] - selectedCenter; // store original positions before scaling starts
            Vector3 newPosition = selectedCenter + direction * _currentScale;

            if (ModelSettings.Axis.X == 0)
                newPosition.X = vert.Position.X;
            if (ModelSettings.Axis.Y == 0)
                newPosition.Y = vert.Position.Y;
            if (ModelSettings.Axis.Z == 0)
                newPosition.Z = vert.Position.Z;

            vert.SetPosition(newPosition);
            i++;
        }

        if (!CanGenerateBuffers)
            return;

        Model.Mesh.UpdateNormals();
        Model.Mesh.UpdateAll();
    }

    public void Handle_VertexMerging()
    {        
        if (Model == null || Model.SelectedVertices.Count < 2)
            return;

        StashMesh();

        Model.Mesh.MergeVertices(Model.SelectedVertices);
                
        Vertex first = Model.SelectedVertices.First();
        Model.SelectedVertices = [first];
                
        Regenerate = true;
    }

    public void Handle_VertexSpliting()
    {
        if (Model == null || Model.SelectedVertices.Count == 0)
            return;

        StashMesh();

        foreach (var vert in Model.SelectedVertices)
        {
            SplitVertex(vert);
        }

        if (!CanGenerateBuffers)
            return;

        Model.Mesh.UpdateAndRegenerateAll();

        Regenerate = true;
    }   

    public void Handle_SeperateSelection()
    {
        StashMesh();

        Handle_Copy();
        HandleTriangleDeletion();
        Handle_Paste(false);
    }

    public static void SeperateSelection(ModelCopy copy, List<Vertex> vertices, ModelMesh mesh)
    {
        ModelCopy.CopyInto(copy, vertices);
        TriangleDeletion(mesh, vertices);
        ModelingEditor.Paste(copy, mesh);
    }

    public void SplitVertex(Vertex vertex)
    {
        if (Model == null)
            return;
            
        List<Triangle> triangles = [.. vertex.ParentTriangles];
        foreach (var tris in triangles)
        {
            bool replace = false;
            Vertex replacement = new Vertex(vertex + ((tris.GetCenter() - vertex).Normalized() * 0.1f));

            if (tris.AB.Has(vertex) && tris.AB.ParentTriangles.Count > 1)
            {
                Edge ab = new(tris.AB);
                tris.AB = ab;
                Model.Mesh.EdgeList.Add(ab);
                replace = true;
            }
            if (tris.BC.Has(vertex) && tris.BC.ParentTriangles.Count > 1)
            {
                Edge bc = new(tris.BC);
                tris.BC = bc;
                Model.Mesh.EdgeList.Add(bc);
                replace = true;
            }
            if (tris.CA.Has(vertex) && tris.CA.ParentTriangles.Count > 1)
            {
                Edge ca = new(tris.CA);
                tris.CA = ca;
                Model.Mesh.EdgeList.Add(ca);
                replace = true;
            }

            if (replace)
            {
                if (tris.A == vertex)
                    tris.A = replacement;
                else if (tris.B == vertex)
                    tris.B = replacement;
                else if (tris.C == vertex)
                    tris.C = replacement;

                tris.SetVertexTo(vertex, replacement);
                Model.Mesh.AddVertex(replacement, false);
            }
        }

        Model.Mesh.RemoveVertex(vertex);

        if (!CanGenerateBuffers)
            return;

        Model.Mesh.UpdateAndRegenerateAll();

        Regenerate = true;
    }

    public void Handle_GenerateNewFace()
    {
        if (Model == null || selectionType != RenderType.Vertex || Model.SelectedVertices.Count > 4)
            return;

        StashMesh();

        var selectedVertices = Model.SelectedVertices.ToList();
        if (selectedVertices.Count == 2)
        {
            if (ModelingHelper.Generate_2_Selected(selectedVertices))
            {
                HashSet<Vertex> nextVertices = [selectedVertices[2], selectedVertices[3]];
                ModelingHelper.Generate_4_Selected(Model, selectedVertices, Model.Mesh);
                Model.SelectedVertices = nextVertices;
            }            
        }
        else if (selectedVertices.Count == 3)
        { 
            ModelingHelper.Generate_3_Selected(selectedVertices, Model.Mesh);
        }
        else if (selectedVertices.Count == 4)
        { 
            ModelingHelper.Generate_4_Selected(Model, selectedVertices, Model.Mesh);    
        }

        if (!CanGenerateBuffers)
            return;

        Model.Mesh.RegenerateAll();

        Model.GenerateVertexColor();
    }

    public void Handle_FlipSelection()
    {
        if (Model == null)
            return;
            
        StashMesh();
        Vector3 center = Model.GetSelectedCenter(Model.SelectedVertices);

        foreach (var vert in Model.SelectedVertices)
        {
            Vector3 centeredPosition = vert - center;
            centeredPosition.X *= ModelSettings.Axis.X == 1 ? -1 : 1;
            centeredPosition.Y *= ModelSettings.Axis.Y == 1 ? -1 : 1;
            centeredPosition.Z *= ModelSettings.Axis.Z == 1 ? -1 : 1;
            vert.SetPosition(center + centeredPosition);
        }

        if (!CanGenerateBuffers)
            return;

        Model.Mesh.RecalculateNormals();
        Model.Mesh.Init();
        Model.Mesh.UpdateMesh();

        Regenerate = true;
    }

    public void Handle_SelectAllVertices(bool generateColors = true)
    {
        if (Model == null)
            return;

        if (Input.IsKeyDown(Key.ShiftLeft))
        {
            Handle_GetConnectedVertices();
        }
        else
        {
            Model.SelectedVertices.Clear();

            foreach (var vert in Model.Mesh.VertexList)
            {
                Model.SelectedVertices.Add(vert);
            }
        }     
        if (generateColors)     
            Model.GenerateVertexColor();
    }

    public void CombineDuplicateVertices()
    {
        if (Model == null)
            return;

        StashMesh();
        Model.Mesh.CombineDuplicateVertices();

        if (!CanGenerateBuffers)
            return;

        Model.Mesh.UpdateAndRegenerateAll();

        Regenerate = true;
    }

    public void TestFunction()
    {
        if (Model == null)
            return;

        Vector3 min = (float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3  max = (float.MinValue, float.MinValue, float.MinValue);
        
        foreach (var vert in Model.Mesh.VertexList)
        {
            min = Mathf.Min(min, vert);
            max = Mathf.Max(max, vert); 
        }

        Vector3 bSize = max - min;
        float largestSide = Mathf.Max(bSize.X, bSize.Z);

        ModelCopy.CopyInto(Model.randomCopy, Model.Mesh.VertexList);

        // Set the uvs to the new positions / largestSide

        // To apply the uvs we need to:
        // 1. Store the uvs in a list, delete all the triangles we have now, and paste the model we copied at the start of the function
        // 2. Set the uvs in the pasted triangles
        // This is possible because the order of the triangles hasn't changed inside the mesh so the setting of the uvs will be correct

        List<(Vector2, Vector2, Vector2)> triangleUvs = [];

        foreach (var triangle in Model.Mesh.TriangleList)
        {
            Vector2 uvA = (triangle.A.X / largestSide, triangle.A.Z / largestSide);
            Vector2 uvB = (triangle.B.X / largestSide, triangle.B.Z / largestSide);
            Vector2 uvC = (triangle.C.X / largestSide, triangle.C.Z / largestSide);

            triangleUvs.Add((uvA, uvB, uvC));
        }

        Model.Mesh.Unload();

        Model.Mesh.AddCopy(Model.randomCopy);

        for (int i = 0; i < Model.Mesh.TriangleList.Count; i++)
        {
            Triangle triangle = Model.Mesh.TriangleList[i];
            triangle.UvA = triangleUvs[i].Item1;
            triangle.UvB = triangleUvs[i].Item2;
            triangle.UvC = triangleUvs[i].Item3;
        }

        Model.Mesh.CheckUselessVertices();

        Model.Mesh.UpdateAndRegenerateAll();

        Regenerate = true;
    }


    private void StashMesh() => Editor.StashMesh();
    private void Handle_Copy() => Editor.Handle_Copy();
    private void Handle_Paste(bool stash = true) => Editor.Handle_Paste(stash);
    private void Handle_Undo() => Editor.Handle_Undo();
}   