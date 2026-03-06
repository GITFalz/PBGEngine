using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using PBG.Data;
using PBG.Files;
using PBG.Graphics;
using PBG.MathLibrary;
using Silk.NET.Vulkan;
using StbImageResizeSharp;
using StbImageSharp;

namespace PBG.Voxel
{
    public static class BlockData
    {
        public static string CoreBlockDataPath = FileManager.CreatePath(Game.MainPath, "data", "core", "blocks");
        public static string BlockTexturesPath = FileManager.CreatePath(Game.TexturePath, "blocks");
        public static TextureArray BlockTextureArray = null!;
        
        public static Dictionary<string, BlockJSON> BlockJsonDictionary = [];
        public static Dictionary<string, (int index, string filePath)[]> RealTextureIndices = [];

        public static Dictionary<string, uint> BlockNames = [];

        public static int BLOCK_COUNT = 0;
        public static BlockDefinition[] BlockDefinitions = [];

        public static BlockPalette Palette = new();

        public static List<FaceGeometry> FaceGeometries = [];
        public static SSBO<FaceGeometry> FaceGeometrySSBO = null!;

        public static void Init()
        {
            LoadBlocks();
            LoadTextures();
            LoadModels();
        }

        public static void LoadBlocks()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var registryPath = Path.Combine(Game.EditorRegistryPath, "block_registry.json");
            
            BlockRegistry? registry = null;
            if (File.Exists(registryPath))
            {
                string registryJson = File.ReadAllText(registryPath);
                registry = JsonSerializer.Deserialize<BlockRegistry>(registryJson, options);
            }
            registry ??= new();

            var palettePath = Path.Combine(Game.EditorPalettePath, "block_palette.json");

            BlockPalette? palette = null;
            if (File.Exists(palettePath))
            {
                string paletteJson = File.ReadAllText(palettePath);
                palette = JsonSerializer.Deserialize<BlockPalette>(paletteJson, options);
            }
            palette ??= new();
            Palette = palette;

            var blockFiles = Directory.GetFiles(CoreBlockDataPath, "*.json");
            int count = 0;
            for (int i = 0; i < blockFiles.Length; i++)
            {
                string blockFile = blockFiles[i]; 
                if (!BlockDataLoader.LoadFromPath(blockFile, out var blockJson))
                {
                    Console.WriteLine("[Error] : [BlockLoader] : An error occured when deserializing the block in file: " + blockFile);
                    continue;
                }

                if (!registry.Ids.TryGetValue(blockJson.Name, out uint id))
                {
                    id = registry.NextId++;
                    registry.Ids.Add(blockJson.Name, id);
                }

                if (!Palette.Ids.ContainsKey(blockJson.Name))
                    Palette.Ids.Add(blockJson.Name, (uint)Palette.Ids.Count);

                blockJson.ID = id;

                BlockJsonDictionary[blockJson.Name] = blockJson;
                RealTextureIndices[blockJson.Name] = new(int,string)[blockJson.Textures.Length];

                for (int j = 0; j < blockJson.Textures.Length; j++)
                {
                    string name = blockJson.Textures[j];
                    string texturePath = Path.Combine(BlockTexturesPath, name + ".png");
                    if (File.Exists(texturePath))
                    {
                        RealTextureIndices[blockJson.Name][j] = (count, texturePath);
                        count++;
                    }
                }

                Console.WriteLine($"[Loaded block] : '{blockJson.Name}'");
            }
            
            var json = JsonSerializer.Serialize(registry, options);
            File.WriteAllText(registryPath, json);

            json = JsonSerializer.Serialize(Palette, options);
            File.WriteAllText(palettePath, json);
        }

        public static void LoadTextures()
        {
            int maxWidth = 0;
            int maxHeight = 0;

            List<(int width, int height)> textureSizes = [];
            List<byte[]> textureData = [];

            foreach (var (_, textures) in RealTextureIndices)
            {
                foreach (var (_, filePath) in textures)
                {
                    ImageResult texture;

                    using (var stream = File.OpenRead(filePath))
                    {
                        texture = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                    }

                    maxWidth = Mathf.Max(maxWidth, texture.Width);
                    maxHeight = Mathf.Max(maxHeight, texture.Height);

                    textureSizes.Add((texture.Width, texture.Height));
                    textureData.Add(texture.Data);
                }
            }

            List<byte[]> fullTextureData = [];

            for (int i = 0; i < textureData.Count; i++)
            {
                var texture = textureData[i];
                var (width, height) = textureSizes[i];

                if (width < maxWidth || height < maxHeight)
                {
                    byte[] resized = new byte[maxWidth * maxHeight * 4];
                    StbImageResize.stbir_resize_uint8(
                        texture, width, height, 0,
                        resized, maxWidth, maxHeight, 0, 4
                    );
                    texture = resized;
                }

                fullTextureData.Add(texture);
            }

            BlockTextureArray = new(fullTextureData, new("", maxWidth, maxHeight) { SamplerMode = SamplerAddressMode.ClampToEdge, Filter = Filter.Nearest });
        }

        public static void LoadModels()
        {
            BlockDefinitions = new BlockDefinition[Palette.Ids.Count];
            BLOCK_COUNT = Palette.Ids.Count;

            uint i = 0;
            foreach (var (name, id) in Palette.Ids)
            {
                var definition = new BlockDefinition();
                if (BlockJsonDictionary.TryGetValue(name, out var blockJson))
                {
                    definition.Type = BlockDefinitionType.Present;
                    definition.Name = name;
                    definition.Block = new(BlockState.Solid, blockJson.ID);

                    blockJson.Generate(definition);
                    BlockNames.Add(name, blockJson.ID);
                    _ = new BlockItemData(definition, i);
                }
                BlockDefinitions[i] = definition;
                i++;
            }

            FaceGeometrySSBO = new([..FaceGeometries]);
            FaceGeometries = [];
        }

        public class BlockPalette
        {
            public Dictionary<string, uint> Ids { get; set; } = [];
        }

        public class BlockRegistry
        {
            public uint NextId { get; set; } = 0;
            public Dictionary<string, uint> Ids { get; set; } = [];
        }

        public static bool GetBlock(string name, [NotNullWhen(true)] out uint id)
        {
            return BlockNames.TryGetValue(name, out id);
        }

        public static bool GetBlock(string name, out Block block)
        {
            block = Block.Air;
            if (!GetBlock(name, out uint id))
                return false;
            
            block = new Block(BlockState.Solid, id);
            return true;
        }
    }

    public class NewBlockFaces
    {
        public VoxelFace[][] Faces = [];
        public ulong[] BitMasks = [];
        public VoxelFace[] InternalFaces = [];

        public VoxelFace[] GetFaces(int side) => Faces[side];
        public ulong GetBitMask(int side) => BitMasks[side];

        public bool IsOccluded(NewBlockFaces faces, int sideA, int sideB) => !IsNotOccluded(faces, sideA, sideB);
        public bool IsNotOccluded(NewBlockFaces faces, int sideA, int sideB) => (GetBitMask(sideA) & ~faces.GetBitMask(sideB)) != 0;
    }

    public abstract class BaseBlockFaces
    {
        public abstract bool GenerateFaces(BaseVoxelChunkHandler blockGetter, Vector3 position, BaseBlockFaces faces, int sideA, int sideB);
        public abstract void GenerateFaces(BaseVoxelChunkHandler blockGetter, Vector3 position, int side);
        public abstract VoxelFace[] GetFaces(int side);
        public abstract ulong GetBitMask(int side);

        public bool IsOccluded(BaseBlockFaces faces, int sideA, int sideB) => !IsNotOccluded(faces, sideA, sideB);
        public bool IsNotOccluded(BaseBlockFaces faces, int sideA, int sideB) => (GetBitMask(sideA) & ~faces.GetBitMask(sideB)) != 0;
    }

    public class FullBlockFaces : BaseBlockFaces
    {
        public VoxelFace[] Faces = [];

        public override bool GenerateFaces(BaseVoxelChunkHandler blockGetter, Vector3 position, BaseBlockFaces faces, int sideA, int sideB)
        {
            if (IsNotOccluded(faces, sideA, sideB))
            {
                blockGetter.AddFace(Faces[sideA], position);
                return true;
            }
            return false;
        }

        public override void GenerateFaces(BaseVoxelChunkHandler blockGetter, Vector3 position, int side)
        {
            blockGetter.AddFace(Faces[side], position);
        }

        public override VoxelFace[] GetFaces(int side) => [Faces[side]];

        public override ulong GetBitMask(int side) => Faces[side].BitMask;
    }

    public class PartialBlockFaces : BaseBlockFaces
    {
        public VoxelFace[][] Faces = [];
        public ulong[] BitMasks = [];
        public VoxelFace[] InternalFaces = [];

        public override bool GenerateFaces(BaseVoxelChunkHandler blockGetter, Vector3 position, BaseBlockFaces faces, int sideA, int sideB)
        {
            if (IsNotOccluded(faces, sideA, sideB))
            {
                for (int i = 0; i < Faces[sideA].Length; i++)
                {
                    blockGetter.AddFace(Faces[sideA][i], position);
                }
                return true;
            }
            return false;
        }
        
        public override void GenerateFaces(BaseVoxelChunkHandler blockGetter, Vector3 position, int side)
        {
            for (int i = 0; i < Faces[side].Length; i++)
            {
                blockGetter.AddFace(Faces[side][i], position);
            }
        }

        public override VoxelFace[] GetFaces(int side) => Faces[side];

        public override ulong GetBitMask(int side) => BitMasks[side];
    }

    public abstract class BaseBlockFace
    {
        
    }

    public class FullBlockFace : BaseBlockFace
    {
        public VoxelFace Face;
    }

    public class PartialBlockFace : BaseBlockFace
    {
        public VoxelFace Face;
    }

    public class InternalBlockFace : BaseBlockFace
    {
        public VoxelFace Face;
    }

    public struct VoxelFace
    {
        public Vector3 A = Vector3.Zero;
        public Vector3 B = Vector3.Zero;
        public Vector3 C = Vector3.Zero;
        public Vector3 D = Vector3.Zero;

        public Vector2 UvA = (0, 0);
        public Vector2 UvB = (0, 1);
        public Vector2 UvC = (1, 1);
        public Vector2 UvD = (1, 0);

        public Vector3 Normal = (0, 1, 0);

        public int TextureIndex = 0;
        public int GeometryIndex = 0;

        public bool CanBeOccluded = false;
        public int Side = 0;

        public readonly Vector2 AABB_a => (A[_sideXY[Side].X], A[_sideXY[Side].Y]);
        public readonly Vector2 AABB_c => (C[_sideXY[Side].X], C[_sideXY[Side].Y]);

        public ulong BitMask = 0;

        public bool MergeX = false;
        public bool MergeY = false;

        public VoxelFace() { }

        public Vector3[] Vertices => [A, D, B, C];
        public Vector3 Center => (A + B + C + D) / 4f;

        private static readonly Vector2i[] _sideXY = 
        [
            (0, 1),
            (2, 1),
            (0, 2),
            (2, 1),
            (0, 2),
            (0, 1)
        ];
    }
}

public struct FaceGeometry
{
    public Vector3 A;
    float P1;
    public Vector3 B;
    float P2;
    public Vector3 C;
    float P3;
    public Vector3 D;
    float P4;

    public Vector2 UvA;
    public Vector2 UvB;
    public Vector2 UvC;
    public Vector2 UvD;

    public Vector3 Normal;

    public int TextureIndex;
}

public class BlockPlacement
{
    public int[,,] Placements = new int[6, 5, 4];

    public void Set(int side, int region, int dir, int index)
    {
        Placements[side, region, dir] = index;
    }

    public int Get(int side, int region, int dir)
    {
        return Placements[side, region, dir];
    }
}

public enum BlockDefinitionType
{
    Present,
    Missing
}