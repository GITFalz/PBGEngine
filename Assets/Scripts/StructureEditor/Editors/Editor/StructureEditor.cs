using PBG.MathLibrary;
using PBG.Noise;
using PBG;
using PBG.Data;
using PBG.Graphics;
using PBG.Rendering;
using PBG.UI;
using PBG.UI.Creator;
using PBG.Voxel;
using static PBG.UI.Styles;
using Silk.NET.Input;

public partial class StructureEditor : BaseStructureEditor
{
    /*
    public static ShaderProgram BoundingBoxShader = new ShaderProgram("StructureEditor/structure/boundingBox.vert","StructureEditor/structure/boundingBox.frag");
    public static ShaderProgram PlacementHelperShader = new ShaderProgram("world/placementHelper.vert","world/placementHelper.frag");
    public static SSBO<BoundingBoxData> SSBO;
    public static SSBO<BoundingBoxData> BlockSSBO = new(new List<BoundingBoxData>() { new()});
    public static VAO VAO = new();

    private static int _modelLocation = BoundingBoxShader.GetLocation("model");
    private static int _viewLocation = BoundingBoxShader.GetLocation("view");
    private static int _projectionLocation = BoundingBoxShader.GetLocation("projection");

    private static int _placementModelLocation = PlacementHelperShader.GetLocation("model");
    private static int _placementViewLocation = PlacementHelperShader.GetLocation("view");
    private static int _placementProjectionLocation = PlacementHelperShader.GetLocation("projection");
    private static int _placementSideLocation = PlacementHelperShader.GetLocation("uSide");
    private static int _placementBlockPosLocation = PlacementHelperShader.GetLocation("uBlockPos");
    private static int _placementCameraPosLocation = PlacementHelperShader.GetLocation("uCamPosition");
    private static int _placementRegionLocation = PlacementHelperShader.GetLocation("uRegion");
    */


    public UIController ScriptController = new();

    public Camera Camera => Editor.Camera;
    public VoxelRenderer Renderer => Editor.Parent.MeshRenderer.Renderer;
    public StructureNodeManager Editor;
    public RightPanel RightUIPanel;
    public LeftPanel LeftUIPanel;
    public StructureEditorScriptUI ScriptUI;

    public int BoundingBoxCount = 0;

    public Dictionary<Vector3i, VoxelChunk> AffectedChunks = [];

    public List<StructureData> BoundingBoxes = [];
    public StructureData? SelectedBoundingBox = null;

    public bool UpdateBoundingBox = true;
    public bool UpdateBlockData = true;

    public Vector3i BlockPlacementPosition
    {
        get => _blockPlacementPosition;
        set {
            if (_blockPlacementPosition != value)
            {
                UpdateBlockData = true;
                _blockPlacementPosition = value;
            }
        }
    }
    public Vector3i _blockPlacementPosition = Vector3i.Zero;
    public int SelectedRegion = -1;
    public int SelectedSide = 0;

    public bool RenderPlacementHelper = false;
    public bool RenderEmptyBlock = false;

    private CursorMode? _oldState = null;

    public bool ShowScript = false;
    public bool ShowBoundingBoxes = true;

    public BlockDefinition? CurrentBlock;


    public StructureEditor(StructureNodeManager editor)
    {
        Editor = editor;
        //SSBO = new(new List<BoundingBoxData>());
        BoundingBoxCount = 0;
        SelectedBoundingBox = new()
        {
            Size = (1, 1, 1),
            Blocks = [Block.Air]
        };
        BoundingBoxes.Add(SelectedBoundingBox);
        RightUIPanel = new(this);
        LeftUIPanel = new(this);
        ScriptUI = new(this);

        ScriptController.AddElement(ScriptUI);
        ScriptController.RemoveInputfieldOnEnter = false;
        
        //ScriptController.Update(); // Init once before

        StructureHeightShader.GenerateHeight(0, 0, 200);
    }

    public void Start()
    {
        foreach (var chunk in StructureTreeGenerationProcess.OldAffectedChunks)
        {
            chunk.Blocks?.Clear();
            Renderer.RerenderingQueue.AddLast(chunk);
        }
        StructureTreeGenerationProcess.OldAffectedChunks = [];
        CloseScript();
    }

    public void Rezise()
    {
        //ScriptController.Resize();
    }

    public void Update()
    {
        if (!Editor.Parent.MeshRenderer.Rendering && ShowScript)
        {
            //ScriptController.Update();
            ScriptUI.Update();
            return;
        }

        if (StructureEngineManager.CanRotate)
        {
            if (Input.IsKeyPressed(Key.Escape))
            {
                StructureEngineManager.CanRotate = false;
                Game.SetCursorState(CursorMode.Normal);
                Camera.Lock();
            }
        }

        SelectedRegion = -1;
        RenderPlacementHelper = false;
        RenderEmptyBlock = false;
        if (_oldState != CursorMode.Normal && Game.IsCursorState(CursorMode.Disabled))
        {
            bool raycast = VoxelData.Raycast(Renderer, Camera.Position, Camera.front, 100, out Hit hit);
            if (raycast)
            {
                Vector3i position = hit.BlockPosition + hit.Normal;
                if (Input.IsMousePressed(MouseButton.Right) && CurrentBlock != null)
                {
                    var block = CurrentBlock.Block;

                    int rotationIndex = 0;
                    //if (CurrentBlock.Placements != null)
                        //rotationIndex = CurrentBlock.Placements.Get(hit.Side, hit.Region, Camera.Cardinal);
                    
                    block.SetRotation((uint)rotationIndex);
                    Renderer.SetBlock(position, block);
                    Vector3i worldPosition = VoxelData.BlockToChunkRelative(position);
                    if (!AffectedChunks.ContainsKey(worldPosition) && Renderer.GetChunk(worldPosition, out var chunk))
                        AffectedChunks.Add(worldPosition, chunk);

                    Console.WriteLine(block);
                }
                BlockPlacementPosition = hit.BlockPosition;
                SelectedRegion = hit.Region;
                RenderPlacementHelper = true;
                SelectedSide = hit.Side;
            }
            else
            {
                Vector3i position = Mathf.FloorToInt(Camera.Position + Camera.front * 10f);
                if (Input.IsMousePressed(MouseButton.Right) && CurrentBlock != null)
                {
                    var block = CurrentBlock.Block;
                    block.ClearRotation();
                    Renderer.SetBlock(position, block);
                    Vector3i worldPosition = VoxelData.BlockToChunkRelative(position);
                    if (!AffectedChunks.ContainsKey(worldPosition) && Renderer.GetChunk(worldPosition, out var chunk))
                        AffectedChunks.Add(worldPosition, chunk);
                }
                BlockPlacementPosition = position;
                RenderEmptyBlock = true;
            }

            if (Input.IsMousePressed(MouseButton.Left))
            {
                if (raycast)
                {
                    Vector3i position = hit.BlockPosition;
                    Renderer.SetBlock(position, Block.Air);
                    Vector3i worldPosition = VoxelData.BlockToChunkRelative(position);
                    if (!AffectedChunks.ContainsKey(worldPosition) && Renderer.GetChunk(worldPosition, out var chunk))
                        AffectedChunks.Add(worldPosition, chunk);
                }
            }

            if (Input.IsKeyPressed(Key.I))
            {
                var rotation = hit.Block.Rotation();
                var definition = BlockData.BlockDefinitions[hit.Block.ID];
                //BlockPlacement.Key((int)rotation, out var side, out var region, out var facing);
                //Console.WriteLine($"[Info] : Looking at block '{definition.Name}' that is placed on side {side}, region {region} and facing {(BlockFacing)facing}");
            }
        }
        
        if (UpdateBoundingBox && SelectedBoundingBox != null)
        {
            SelectedBoundingBox.SavePosition = LeftUIPanel.Position;
            SelectedBoundingBox.Size = LeftUIPanel.Size;

            if (RightUIPanel.SelectedConnection != null)
                RightUIPanel.SelectedConnection.Position = RightUIPanel.ConnectionPosition;

            if (RightUIPanel.SelectedRuleset != null)
                RightUIPanel.SelectedRuleset.Position = RightUIPanel.RulesetPosition;

            List<BoundingBoxData> boxes = [];
            boxes.Add(new()
            {
                Position = LeftUIPanel.Position - new Vector3(0.01f, 0.01f, 0.01f),
                Size = LeftUIPanel.Size + new Vector3(0.02f, 0.02f, 0.02f),
                Color = (0.2f, 0.8f, 0.2f, 0.25f)
            });

            for (int i = 0; i < SelectedBoundingBox.BoundingBoxes.Count; i++)
            {
                var boundingBox = SelectedBoundingBox.BoundingBoxes[i];
                boxes.Add(new()
                {
                    Position = boundingBox.Position - (new Vector3(0.005f, 0.005f, 0.005f)),
                    Size = boundingBox.Size + (new Vector3(0.01f, 0.01f, 0.01f)),
                    Color = RightUIPanel.SelectedBoundingBox == boundingBox ? (1.0f, 0.75f, 0.35f, 0.25f) : (1.0f, 0.6f, 0.1f, 0.25f)
                });
            }

            for (int i = 0; i < SelectedBoundingBox.Extenders.Count; i++)
            {
                var extender = SelectedBoundingBox.Extenders[i];
                boxes.Add(new()
                {
                    Position = extender.Position - (new Vector3(0.0075f, 0.1f, 0.0075f)),
                    Size = extender.Size + (new Vector3(0.015f, 0.2f, 0.015f)),
                    Color = RightUIPanel.SelectedExtender == extender ? (0.35f, 1.0f, 0.9f, 0.25f) : (0.0f, 0.85f, 0.75f, 0.25f)
                });
            }

            void AddConnectionBox(ConnectionPoint connection, Vector3 position, Vector3 size)
            {
                boxes.Add(new()
                {
                    Position = position + SelectedBoundingBox.SavePosition,
                    Size = size,
                    Color = RightUIPanel.SelectedConnection == connection ? (0.45f, 0.75f, 1.0f, 0.25f) : (0.2f, 0.6f, 1.0f, 0.25f)
                });
            }
            foreach (var (_, connection) in SelectedBoundingBox.ConnectionPoints)
            {
                switch (connection.Side)
                {
                    case 0: AddConnectionBox(connection, connection.Position - (0.3f, 0.3f, 0.15f), (0.6f, 0.6f, 0.3f)); break; 
                    case 1: AddConnectionBox(connection, connection.Position - (0.15f, 0.3f, 0.3f), (0.3f, 0.6f, 0.6f)); break; 
                    case 2: AddConnectionBox(connection, connection.Position - (0.3f, 0.15f, 0.3f), (0.6f, 0.3f, 0.6f)); break; 
                    case 3: AddConnectionBox(connection, connection.Position - (0.15f, 0.3f, 0.3f), (0.3f, 0.6f, 0.6f)); break; 
                    case 4: AddConnectionBox(connection, connection.Position - (0.3f, 0.15f, 0.3f), (0.6f, 0.3f, 0.6f)); break; 
                    case 5: AddConnectionBox(connection, connection.Position - (0.3f, 0.3f, 0.15f), (0.6f, 0.6f, 0.3f)); break; 
                }
            }

            foreach (var (_, rule) in SelectedBoundingBox.RulesetPoints)
            {
                boxes.Add(new()
                {
                    Position = rule.Position + SelectedBoundingBox.SavePosition - (0.2f, 0.2f, 0.2f),
                    Size = (1.4f, 1.4f, 1.4f),
                    Color = RightUIPanel.SelectedRuleset == rule ? (0.9f, 0.55f, 1.0f, 0.25f) : (0.8f, 0.3f, 1.0f, 0.25f)
                });
            }
            //SSBO.Renew(boxes);
            BoundingBoxCount = boxes.Count;
            UpdateBoundingBox = false;
        }


        if (UpdateBlockData && RenderEmptyBlock)
        {
            /*
            BlockSSBO.Update(new List<BoundingBoxData>() { new()
            {
                Position = BlockPlacementPosition,
                Size = (1, 1, 1),
                Color = (0.4f, 0.4f, 0.4f, 0.4f)
            }}, 0);
            */
        }

        StructureHeightShader.Update(Camera);

        _oldState = Game.GetCursorState();
    }

    public void Render()
    {
        /*
        GL.Viewport(240, 0, Game.Width - 480, Game.Height - 60);

        Matrix4 orthoProjection = Matrix4.CreateOrthographicOffCenter(240, Game.Width - 240, Game.Height, 60, -2, 2);

        if (!Editor.Parent.MeshRenderer.Rendering && ShowScript)
        {
            //ScriptController.RenderDepthTest(orthoProjection);
        }
        else if (ShowBoundingBoxes)
        {
            GL.DepthMask(false);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            StructureHeightShader.Render(Camera);

            BoundingBoxShader.Bind();

            Matrix4 model = Matrix4.Identity;
            Matrix4 view = Camera.ViewMatrix;
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Camera.FOV), (float)(Game.Width - 480) / (float)(Game.Height - 60), 0.1f, 10000f);

            GL.UniformMatrix4(_modelLocation, true, ref model);
            GL.UniformMatrix4(_viewLocation, true, ref view);
            GL.UniformMatrix4(_projectionLocation, true, ref projection);

            VAO.Bind();
            SSBO.Bind(0);

            GL.DrawArrays(PrimitiveType.Triangles, 0, BoundingBoxCount * 36);
            Shader.Error("Structure editor render error: ");

            SSBO.Unbind();
            
            if (RenderEmptyBlock)
            {
                BlockSSBO.Bind(0);

                GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
                Shader.Error("Structure editor render error: ");

                BlockSSBO.Unbind();
            }

            VAO.Unbind();

            BoundingBoxShader.Unbind();

            GL.DepthMask(true);

            if (RenderPlacementHelper)
            {
                PlacementHelperShader.Bind();

                model = Matrix4.CreateTranslation(BlockPlacementPosition);

                GL.UniformMatrix4(_placementModelLocation, false, ref model);
                GL.UniformMatrix4(_placementViewLocation, false, ref view);
                GL.UniformMatrix4(_placementProjectionLocation, false, ref projection);
                GL.Uniform1(_placementSideLocation, (int)SelectedSide);
                GL.Uniform1(_placementRegionLocation, (int)SelectedRegion);
                GL.Uniform3(_placementBlockPosLocation, new Vector3(BlockPlacementPosition));
                GL.Uniform3(_placementCameraPosLocation, Camera.Position);

                VAO.Bind();

                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
                Shader.Error("Structure editor render error: ");

                VAO.Unbind();

                PlacementHelperShader.Unbind();
            }
    
            GL.Disable(EnableCap.Blend);
            GL.Clear(ClearBufferMask.DepthBufferBit);
        }

        GL.Viewport(0, 0, Game.Width, Game.Height);
        */
    }

    public void Dispose()
    {
        
    }

    public void SetBlock(string name)
    {
        if (!ItemDataManager.AllItems.TryGetValue(name, out var item) || item is not BlockItemData data)
            return;

        RightUIPanel.CurrentBlockImg.UpdateItem(name);
        CurrentBlock = data.Block;
    }

    public void SaveSelectedBoundingBox()
    {
        if (SelectedBoundingBox == null)
            return;

        var size = LeftUIPanel.Size;
        var position = LeftUIPanel.Position;

        int length = size.X * size.Y * size.Z;
        Block[] blocks = new Block[length];
        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                for (int z = 0; z < size.Z; z++)
                {
                    Vector3i worldPos = position + (x, y, z);
                    if (Renderer.GetBlock(worldPos, out Block block))
                    {
                        blocks[x + y * size.X + z * size.X * size.Y] = block;
                    }
                    else
                    {
                        blocks[x + y * size.X + z * size.X * size.Y] = Block.Air;
                    }
                }
            }
        }

        SelectedBoundingBox.Size = size;
        SelectedBoundingBox.SavePosition = position;
        SelectedBoundingBox.Blocks = blocks;
    }

    public void DeleteBoundingBox(StructureData data)
    {
        if (SelectedBoundingBox == data)
            SelectedBoundingBox = null;

        BoundingBoxes.Remove(data);

        data.Element.Delete();

        RightUIPanel.RegenerateBoundingBoxes();
        RightUIPanel.RegenerateConnectionPoints();
        RightUIPanel.RegenerateExtenders();
        RightUIPanel.RegenerateRulesetPoints();
    }

    public void LoadSelectedBoundingBox()
    {
        if (SelectedBoundingBox == null)
            return;

        foreach (var (_, chunk) in AffectedChunks)
        {
            chunk.Blocks?.Clear();
        }

        var size = SelectedBoundingBox.Size;
        var position = SelectedBoundingBox.SavePosition;

        Dictionary<Vector3i, VoxelChunk> affectChunks = [];

        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                for (int z = 0; z < size.Z; z++)
                {
                    Vector3i worldPos = position + (x, y, z);
                    var block = SelectedBoundingBox.Blocks[x + y * size.X + z * size.X * size.Y];

                    var chunkPos = VoxelData.BlockToChunkRelative(worldPos);
                    if (AffectedChunks.TryGetValue(chunkPos, out var chunk))
                    {
                        var blockPos = VoxelData.BlockToRelative(worldPos);
                        chunk.Set(blockPos, block);
                        affectChunks.TryAdd(chunkPos, chunk);
                    }
                    else if (Renderer.GetChunk(chunkPos, out chunk))
                    {
                        AffectedChunks.TryAdd(chunkPos, chunk);
                        affectChunks.TryAdd(chunkPos, chunk);
                        var blockPos = VoxelData.BlockToRelative(worldPos);
                        chunk.Set(blockPos, block);
                    }
                }
            }
        }

        foreach (var (_, chunk) in AffectedChunks)
        {
            Renderer.RerenderingQueue.AddLast(chunk);
        }

        AffectedChunks = affectChunks;
    }

    public void OpenScript()
    {
        if (SelectedBoundingBox == null)
            return;

        ShowScript = true;
        Editor.Parent.MeshRenderer.Rendering = false;
        Editor.Parent.MeshRenderer.Renderer.Run = false;
        Editor.structureNodeUI.CenterPanel.SetVisible(false);
        RightUIPanel.BlockSelectionPanel.SetVisible(false);
        ScriptUI.SetLines(SelectedBoundingBox.Lines);
    }

    public void CloseScript()
    {
        ShowScript = false;
        Editor.Parent.MeshRenderer.Rendering = true;
        Editor.Parent.MeshRenderer.Renderer.Run = true;
        Editor.structureNodeUI.CenterPanel.SetVisible(true);
        RightUIPanel.BlockSelectionPanel.SetVisible(true);
    }

    public void ClearTerrain()
    {

    }

    public void GenerateTerrain()
    {
    }

    public static int SampleTerrain(int x, int z)
    {
        Vector2 position = (x + 0.001f, z + 0.001f) * new Vector2(0.01f, 0.01f);
        float result = (NoiseLib.Noise(position) + 1) * 0.5f;
        return Mathf.Lerp(0, 60, result).Fti();
    }

    public void GenerateStructure()
    {
        Console.WriteLine("Generating structure");
        if (BoundingBoxes.Count == 0)
            return;

        var generator = new DefaultStructureGenerator();

        foreach (var (_, chunk) in AffectedChunks)
        {
            chunk.Blocks?.Clear();
        }

        int maxChain = 20;
        int seed = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        List<StructureData> boundingBoxes = [..BoundingBoxes];
        StructureData? core = null;

        for (int i = 0; i < boundingBoxes.Count; i++)
        {
            var bb = boundingBoxes[i];
            foreach (var (_, connection) in bb.ConnectionPoints)
            {
                connection.HashedCategories = [..connection.Categories];
                connection.HashedAvoid = [..connection.Avoid];
            }
        }

        for (int i = 0; i < boundingBoxes.Count; i++)
        {
            var bb = boundingBoxes[i];
            if (bb.Core)
            {
                core = bb;
                boundingBoxes.RemoveAt(i);
                break;
            }
        }

        if (boundingBoxes.Count == 0)
            return;
        
        Shuffle(boundingBoxes, seed);

        var result = SampleTerrain(0, 0);

        List<StructureBoundingBoxPlacement> Placements = [];
        StructureData first = core ?? boundingBoxes[0];
        StructureBoundingBoxPlacement firstPlacement = new()
        {
            Data = first,
            Position = (0, result, 0)
        };
        firstPlacement.SetRotation(generator, 0);
        Placements.Add(firstPlacement);
        Queue<StructureBoundingBoxPlacement> placementQueue = [];
        StructureBoundingBoxPlacement currentPlacement = firstPlacement;
        StructureModule module = first.GetModule(firstPlacement);

        module.GenerateRandom(generator, boundingBoxes, Placements, maxChain, seed);

        Dictionary<Vector3i, VoxelChunk> affectChunks = [];
        for (int i = 0; i < Placements.Count; i++)
        {
            var placement = Placements[i];

            for (int x = 0; x < placement.Data.Size.X; x++)
            for (int y = 0; y < placement.Data.Size.Y; y++)
            for (int z = 0; z < placement.Data.Size.Z; z++)
            {
                var block = placement.Data.Blocks[x + y * placement.Data.Size.X + z * placement.Data.Size.X * placement.Data.Size.Y];
                if (block.IsAir()) continue;

                var rotated = Mathf.RotatePoint((x, y, z) + new Vector3(0.5f) , placement.Data.Center, (0, 1, 0), placement.Yrotation * 90f);
                Vector3i worldPos = Mathf.FloorToInt(placement.Position + rotated);

                var definition = BlockData.BlockDefinitions[block.ID];
                if (definition.CanRotate)
                {
                    var rotation = block.Rotation();
                    //var rotationId = definition.Placements?.RotatedKey((int)rotation, placement.Yrotation) ?? (int)rotation;
                    block.SetRotation(0);
                }

                var chunkPos = VoxelData.BlockToChunkRelative(worldPos);
                if (AffectedChunks.TryGetValue(chunkPos, out var chunk))
                {
                    var blockPos = VoxelData.BlockToRelative(worldPos);
                    chunk.Set(blockPos, block);
                    affectChunks.TryAdd(chunkPos, chunk);
                }
                else if (Renderer.GetChunk(chunkPos, out chunk))
                {
                    AffectedChunks.TryAdd(chunkPos, chunk);
                    affectChunks.TryAdd(chunkPos, chunk);
                    var blockPos = VoxelData.BlockToRelative(worldPos);
                    chunk.Set(blockPos, block);
                }
            }

            for (int j = 0; j < placement.ExtenderDatas.Count; j++)
            {
                var data = placement.ExtenderDatas[j];
                for (int x = 0; x < data.Size.X; x++)
                for (int z = 0; z < data.Size.Z; z++)
                {
                    int index = z + x * data.Size.Z;
                    for (int y = data.Heights[index]; y < data.Top; y++)
                    {
                        Vector3i worldPos = (x + data.Position.X, y, z + data.Position.Z);

                        var chunkPos = VoxelData.BlockToChunkRelative(worldPos);
                        if (AffectedChunks.TryGetValue(chunkPos, out var chunk))
                        {
                            var blockPos = VoxelData.BlockToRelative(worldPos);
                            chunk.Set(blockPos, new Block(BlockState.Solid, 3));
                            affectChunks.TryAdd(chunkPos, chunk);
                        }
                        else if (Renderer.GetChunk(chunkPos, out chunk))
                        {
                            AffectedChunks.TryAdd(chunkPos, chunk);
                            affectChunks.TryAdd(chunkPos, chunk);
                            var blockPos = VoxelData.BlockToRelative(worldPos);
                            chunk.Set(blockPos, new Block(BlockState.Solid, 3));
                        }
                    }
                }  
            }
        }

        foreach (var (_, chunk) in AffectedChunks)
        {
            Renderer.RerenderingQueue.AddLast(chunk);
        }

        AffectedChunks = affectChunks;
    }

    public static void Shuffle<T>(IList<T> list, int seed)
    {
        Random rng = new Random(seed);

        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }

    public UIElementBase GetRightPanel() => RightUIPanel;
    public UIElementBase GetLeftPanel() => LeftUIPanel;

    private void Increment(UIField field, int min, int max, Action updateAction) => Change(field, min, max, 1, updateAction);
    private void Decrement(UIField field, int min, int max, Action updateAction) => Change(field, min, max, -1, updateAction);
    private void Change(UIField field, int min, int max, int change, Action updateAction)
    {
        float value = field.GetFloat();
        float oldValue = value;
        value += change;
        value = Mathf.Clampy(value, min, max);
        if (value != oldValue)
        {
            field.UpdateText(value.ToString());
            UpdateBoundingBox = true;
            updateAction();
        }
    }

    public class ChangeElement(StructureEditor editor, UIField field, int min, int max, Action updateAction) : UIScript { public override UIElementBase Script() =>
    new UIHCol(Class(w_[55], h_full, spacing_[5], top_right, right_[5]), Sub(
        new UICol(Class(w_half, h_full, blank_sharp_g_[25], hover_scale_easeout_[1.2f, 0.2f]), OnClickCol(_ => editor.Decrement(field, min, max, updateAction)), Sub(
            new UIImg(Class(w_[20], h_[20], middle_center, icon_[17], bg_white))
        )),
        new UICol(Class(w_half, h_full, blank_sharp_g_[25], hover_scale_easeout_[1.2f, 0.2f]), OnClickCol(_ => editor.Increment(field, min, max, updateAction)), Sub(
            new UIImg(Class(w_[20], h_[20], middle_center, icon_[16], bg_white))
        ))
    ));}
}