using Newtonsoft.Json;
using PBG.MathLibrary;
using PBG.Noise;
using PBG;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.Compiler;
using PBG.Compiler.Lines;
using PBG.MathLibrary;
using PBG.Threads;
using PBG.Voxel;

public static class StructureManager
{
    private static readonly int[] _heightGetters = new int[TaskPool.MAX_THREAD_COUNT];
    public static Dictionary<string, AStructureData> StructureDatas = [];
    public static Dictionary<Vector2i, List<AStructureInfo>> StructureChunks = [];

    static StructureManager()
    {
        Init();
    }

    public static int GetHeight(NoiseNodeManager manager)
    {
        return _heightGetters[manager.ThreadIndex];
    }

    public static void SetHeight(NoiseNodeManager manager, int height)
    {
        _heightGetters[manager.ThreadIndex] = height;
    }

    public static void Init()
    {
        InitStructure(new TreeStructureData("spruce"), 20);
        InitStructure(new BuildingStructureData("Base", (-100, -100, -100), (100, 100, 100)), 200);
    }

    private static void InitStructure(AStructureData structureData, int voronoiSize)
    {
        if (voronoiSize == 0 || !structureData.IsValid())
            return;

        StructureDatas.Add(structureData.Name, structureData);

        float gridMultiplier = 32f / (float)voronoiSize;
        float gridCount = gridMultiplier * 100;

        Vector2i start = (-50 * 32, -50 * 32);

        for (int x = 0; x < gridCount; x++)
        {
            for (int z = 0; z < gridCount; z++)
            {
                Vector2i structurePosition = start + (x * voronoiSize, z * voronoiSize);
                Vector2 voronoiPosition = VoronoiLib.VoronoiOrigin(new Vector2((float)structurePosition.X, (float)structurePosition.Y) / (float)voronoiSize);
                structurePosition = Mathf.FloorToInt(voronoiPosition * voronoiSize);
                var overlaps = structureData.GetFlatChunkOverlapList(structurePosition);
                var structureInfo = structureData.GetInfo(structurePosition);

                foreach (var overlap in overlaps)
                {
                    if (!StructureChunks.ContainsKey(overlap))
                        StructureChunks[overlap] = [];

                    StructureChunks[overlap].Add(structureInfo);
                }        
            }
        }
    }
}

public abstract class AStructureData(string name, Vector3i a, Vector3i b)
{
    public string Name = name;
    public (Vector3i A, Vector3i B) BoundingBox = GetBoundingBox(a, b);

    public HashSet<Vector2i> GetFlatChunkOverlapList(Vector2i origin)
    {
        var chunks = new HashSet<Vector2i>();

        int minX = origin.X + BoundingBox.A.X;
        int minZ = origin.Y + BoundingBox.A.Z;
        int maxX = origin.X + BoundingBox.B.X;
        int maxZ = origin.Y + BoundingBox.B.Z;

        var chunkMinWorld = VoxelData.BlockToChunk((minX, 0, minZ)).Xz;
        var chunkMaxWorld = VoxelData.BlockToChunk((maxX, 0, maxZ)).Xz;

        for (int cx = chunkMinWorld.X; cx <= chunkMaxWorld.X; cx += 32)
        {
            for (int cz = chunkMinWorld.Y; cz <= chunkMaxWorld.Y; cz += 32)
            {
                chunks.Add((cx, cz));
            }
        }

        return chunks;
    }

    public abstract bool IsValid();
    public abstract AStructureInfo GetInfo(Vector2i position);
    public abstract void Generate(NoiseNodeManager copiedManager, VoxelChunk chunk, AStructureInfo structureInfo);
    public abstract void Generate(NoiseNodeManager copiedManager, LODChunk chunk, AStructureInfo structureInfo);

    public static (Vector3i, Vector3i) GetBoundingBox(Vector3i a, Vector3i b) => (Mathf.Min(a, b), Mathf.Max(a, b));
}

public class TreeStructureData : AStructureData
{
    public TreeGenerationInfo? info = null;

    public TreeStructureData(string name) : base(name, (0, 0, 0), (0, 0, 0))
    {
        var path = Path.Combine(Game.CustomPath, "trees", name + ".json");
        if (File.Exists(path))
        {
            JsonSerializerSettings settings = new()
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            string json = File.ReadAllText(path);
            info = JsonConvert.DeserializeObject<TreeGenerationInfo>(json, settings);
            if (info == null)
                return;

            BoundingBox = GetBoundingBox((info.MinX, info.MinY, info.MinZ), (info.MaxX, info.MaxY, info.MaxZ));
        }
    }

    public override bool IsValid() => info != null;

    public override AStructureInfo GetInfo(Vector2i position) =>
    new TreeStructureInfo(this)
    {
        Position = position
    };

    public override void Generate(NoiseNodeManager copiedManager, VoxelChunk chunk, AStructureInfo structureInfo)
    {
        var position = structureInfo.Position;
        copiedManager.Basic(position.X, position.Y);
        int height = StructureManager.GetHeight(copiedManager);
        if (height == 0)
            return;

        void SetBlock(Vector3i position, Block block, bool forceSet)
        {
            if (chunk.InBounds(position))
            {
                var currentBlock = chunk.Get(position);
                if (forceSet || currentBlock.IsAir())
                {
                    chunk.Set(position, block);
                }
            }
        }

        if (info == null)
            return;

        TreeGenerator.Run(info, (position.X, height, position.Y), SetBlock);
    }

    public override void Generate(NoiseNodeManager copiedManager, LODChunk chunk, AStructureInfo structureInfo)
    {
        var position = structureInfo.Position;
        copiedManager.Basic(position.X, position.Y);
        int height = StructureManager.GetHeight(copiedManager);
        if (height == 0)
            return;

        void SetBlock(Vector3i position, Block block, bool forceSet)
        {
            if (chunk.InBounds(position))
            {
                position -= chunk.WorldPosition;
                position = (position.X >> chunk.Level, position.Y >> chunk.Level, position.Z >> chunk.Level);
                int index = ChunkBlocks.GetIndex(position);
                var currentBlock = chunk.Blocks[index];
                if (forceSet || currentBlock.IsAir())
                {
                    chunk.Blocks[index] = block;
                }
            }
        }

        if (info == null)
            return;

        TreeGenerator.Run(info, (position.X, height, position.Y), SetBlock);
    }
}

public class BuildingStructureData(string name, Vector3i a, Vector3i b) : AStructureData(name, a, b)
{
    public override bool IsValid() => true;

    public override AStructureInfo GetInfo(Vector2i position) =>
    new BuildingStructureInfo(this)
    {
        Position = position
    };

    public override void Generate(NoiseNodeManager copiedManager, VoxelChunk chunk, AStructureInfo structureInfo)
    {
        Generate(copiedManager, structureInfo, chunk.WorldPosition, (position, block, forceSet) => 
        {
            if (chunk.InBounds(position))
            {
                var currentBlock = chunk.Get(position);
                if (forceSet || currentBlock.IsAir())
                {
                    chunk.Set(position, block);
                }
            }
        });
    }

    public override void Generate(NoiseNodeManager copiedManager, LODChunk chunk, AStructureInfo structureInfo)
    {
        Generate(copiedManager, structureInfo, chunk.WorldPosition, (position, block, forceSet) => 
        {
            if (chunk.InBounds(position))
            {
                position -= chunk.WorldPosition;
                position = (position.X >> chunk.Level, position.Y >> chunk.Level, position.Z >> chunk.Level);
                int index = ChunkBlocks.GetIndex(position);
                var currentBlock = chunk.Blocks[index];
                if (forceSet || currentBlock.IsAir())
                {
                    chunk.Blocks[index] = block;
                }
            }
        });
    }

    public void Generate(NoiseNodeManager manager, AStructureInfo structureInfo, Vector3i worldPosition, Action<Vector3i, Block, bool> setBlock)
    {
        var position = structureInfo.Position;
        manager.Basic(position.X, position.Y);
        int height = StructureManager.GetHeight(manager);

        if (height == 0 || structureInfo is not BuildingStructureInfo info)
            return;
        
        info.GenerateStructure(manager);

        if (info.Placements.TryGetValue(worldPosition, out var placements))
        {
            Dictionary<string, Block[]> LoadedStructures = [];

            foreach (var placement in placements)
            {
                if (LoadedStructures.ContainsKey(placement.Data.Name))
                    continue;

                LoadedStructures[placement.Data.Name] = StructureLoader.LoadBlocks(Path.Combine(Game.CustomPath, "structures", Name, "data", placement.Data.Name, "blocks.dat"));
            }

            foreach (var placement in placements)
            {
                var blocks = LoadedStructures[placement.Data.Name];
                for (int x = 0; x < placement.Data.Size.X; x++)
                for (int y = 0; y < placement.Data.Size.Y; y++)
                for (int z = 0; z < placement.Data.Size.Z; z++)
                {
                    var block = blocks[x + y * placement.Data.Size.X + z * placement.Data.Size.X * placement.Data.Size.Y];
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

                    setBlock(worldPos, block, true);
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

                            setBlock(worldPos, new Block(BlockState.Solid, 3), true);
                        }
                    }  
                }
            }
        }
    }
}

public abstract class AStructureInfo(AStructureData structureData)
{
    public Vector2i Position;
    public AStructureData StructureData = structureData;
}

public class TreeStructureInfo(AStructureData structureData) : AStructureInfo(structureData)
{

}

public class BuildingStructureInfo : AStructureInfo
{
    public Dictionary<Vector3i, List<StructureBoundingBoxPlacement>> Placements = [];
    public GameCompiler Compiler = new();
    public Variable ScoreVariable;

    private string _basePath => Path.Combine(Game.CustomPath, "structures", StructureData.Name);

    private bool _generated = false;
    private object _lock = new();

    public BuildingStructureInfo(AStructureData structureData) : base(structureData)
    {
        ScoreVariable = new(Compiler, new(), "SCORE")
        {
            Type = "float",
        };
        ScoreVariable.Result.SetFloat(1);
    }

    public void AddPlacement(StructureBoundingBoxPlacement placement)
    {
        var position = placement.RotatedPosition;
        position.Y = position.Y.Min(placement.LowestY);
        var chunkMinWorld = VoxelData.BlockToChunk(position);
        var chunkMaxWorld = VoxelData.BlockToChunk(placement.RotatedPosition + placement.RotatedSize);

        for (int cx = chunkMinWorld.X; cx <= chunkMaxWorld.X; cx += 32)
        {
            for (int cy = chunkMinWorld.Y; cy <= chunkMaxWorld.Y; cy += 32)
            {
                for (int cz = chunkMinWorld.Z; cz <= chunkMaxWorld.Z; cz += 32)
                {
                    var chunkPosition = (cx, cy, cz);
                    Placements.TryAdd(chunkPosition, []);
                    Placements[chunkPosition].Add(placement);
                }
            }
        }
    }

    public void GenerateStructure(NoiseNodeManager heightManager)
    {
        lock(_lock)
        {
            if (_generated)
                return;

            if (!StructureLoader.Load(_basePath, out var structureData, new() { LoadBlocks = false }, ScoreVariable, Compiler))
                return;

            if (structureData.StructureBoundingBoxes.Count == 0)
                return;
            
            WorldStructureGenerator generator = new(ScoreVariable, heightManager);

            int maxChain = 6;
            int seed = 4;
            List<StructureData> boundingBoxes = [..structureData.StructureBoundingBoxes];
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

            heightManager.Basic(Position.X, Position.Y);
            var result = StructureManager.GetHeight(heightManager);

            List<StructureBoundingBoxPlacement> placements = [];
            StructureData first = core ?? boundingBoxes[0];
            StructureBoundingBoxPlacement firstPlacement = new()
            {
                Data = first,
                Position = (Position.X, result, Position.Y)
            };

            firstPlacement.SetRotation(generator, 0);
            placements.Add(firstPlacement);

            StructureModule module = first.GetModule(firstPlacement);

            module.GenerateRandom(generator, boundingBoxes, placements, maxChain, seed);

            for (int i = 0; i < placements.Count; i++)
            {
                AddPlacement(placements[i]);
                for (int j = 0; j < placements[i].boundingBoxes.Count; j++)
                {
                    var b = placements[i].boundingBoxes[j];
                }
            }

            _generated = true;
        }
    }
}