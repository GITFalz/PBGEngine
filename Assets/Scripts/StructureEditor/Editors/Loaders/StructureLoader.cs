using Compiler;
using Newtonsoft.Json;
using PBG.MathLibrary;
using PBG;
using PBG.Compiler;
using PBG.Compiler.Lines;
using PBG.Files;
using PBG.MathLibrary;
using PBG.Voxel;

public static class StructureLoader
{
    private static string _structurePath => Path.Combine(Game.CustomPath, "structures");

    private static JsonSerializerSettings _settings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto
    };

    public struct LoaderSettings
    {
        public bool LoadBlocks = true;
        public bool LoadScript = true;
        public LoaderSettings() {}
    }

    public struct StructureLoaderData
    {
        public List<StructureData> StructureBoundingBoxes = [];
        public StructureLoaderData() {}

        public void Add(StructureData structureData)
        {
            StructureBoundingBoxes.Add(structureData);
        }
    }

    private static bool DefaultCompile(List<string> lines, ref StructureData structureData) => StructureCompiler.CompileDefault(lines, ref structureData);
    
    // Loads the structure data into the default compiler used in the struture editor, try using the other load function with your own score variable and compiler if results seem incorrect
    public static bool Load(string basePath, out StructureLoaderData structureData) => Load(basePath, out structureData, new(), StructureCompiler.ScoreVariable, StructureCompiler.Compiler);
    public static bool Load(string basePath, out StructureLoaderData structureData, LoaderSettings loaderSettings, Variable scoreVariable, GameCompiler compiler)
    {
        StructureLoaderData structureLoaderData = new();
        structureData = structureLoaderData;

        if (!Directory.Exists(basePath))    
            return false;

        JsonSerializerSettings settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        string json = File.ReadAllText(Path.Combine(basePath, "config.json"));
        JSONBaseData? baseData = JsonConvert.DeserializeObject<JSONBaseData>(json, settings);
        if (baseData == null)
            return false;

        var dataPath = Path.Combine(basePath, "data");
        if (!Directory.Exists(dataPath))    
            return true;

        var directories = Directory.GetDirectories(dataPath);
        for (int k = 0; k < directories.Length; k++)
        {
            var directory = directories[k];
            json = File.ReadAllText(Path.Combine(directory, "config.json"));
            JSONStructureData? data = JsonConvert.DeserializeObject<JSONStructureData>(json, settings);
            if (data == null)
                continue;
                
            StructureData sbb = new()
            {
                Name = data.Name,
                ConnectionPoints = [],
                Size = Vector3i.One,
                SavePosition = Vector3i.Zero,
                Core = data.Core
            };

            for (int i = 0; i < Mathf.Min(data.Size.Count, 3); i++)
            {
                sbb.Size[i] = data.Size[i];
            }

            for (int i = 0; i < Mathf.Min(data.SavePosition.Count, 3); i++)
            {
                sbb.SavePosition[i] = data.SavePosition[i];
            }

            for (int i = 0; i < data.BoundingBoxes.Count; i++)
            {
                var boundingBox = data.BoundingBoxes[i];
                Vector3i position = Vector3i.Zero;
                Vector3i size = Vector3i.One;
                for (int j = 0; j < Mathf.Min(boundingBox.Position.Count, 3); j++)
                    position[j] = boundingBox.Position[j];
                
                for (int j = 0; j < Mathf.Min(boundingBox.Size.Count, 3); j++)
                    size[j] = boundingBox.Size[j];
                
                sbb.BoundingBoxes.Add(new(size, position));
            }

            for (int i = 0; i < data.Extenders.Count; i++)
            {
                var extender = data.Extenders[i];
                Vector3i position = Vector3i.Zero;
                Vector3i size = Vector3i.One;
                for (int j = 0; j < Mathf.Min(extender.Position.Count, 3); j++)
                    position[j] = extender.Position[j];
                
                for (int j = 0; j < Mathf.Min(extender.Size.Count, 3); j++)
                    size[j] = extender.Size[j];
                
                sbb.Extenders.Add(new(size, position));
            }

            foreach (var (name, connection) in data.ConnectionPoints)
            {
                Vector3 position = Vector3.Zero;
                for (int j = 0; j < Mathf.Min(connection.Position.Count, 3); j++)
                {
                    position[j] = connection.Position[j];
                }
                var c = new ConnectionPoint(position, connection.Yrotation, connection.Side);
                c.Categories = [..connection.Categories];
                c.Avoid = [..connection.Avoid];
                sbb.ConnectionPoints.Add(name, c);
            }

            foreach (var (name, rule) in data.Rulesets)
            {
                Vector3 position = Vector3.Zero;
                for (int j = 0; j < Mathf.Min(rule.Position.Count, 3); j++)
                {
                    position[j] = rule.Position[j];
                }
                sbb.RulesetPoints.Add(name, new(position));
            }

            if (loaderSettings.LoadBlocks)
            {
                sbb.Blocks = LoadBlocks(Path.Combine(directory, "blocks.dat"));
            }

            if (loaderSettings.LoadScript)
            {
                var scriptPath = Path.Combine(directory, "script.txt");
                if (File.Exists(scriptPath))
                {
                    var lines = File.ReadAllLines(scriptPath);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        sbb.Lines.Add(lines[i]);
                    }
                    
                    if (!StructureCompiler.Compile(scoreVariable, compiler, sbb.Lines, ref sbb))
                    {
                        var compileData = compiler.CompileData;
                        compileData.Print();
                    }
                    else
                    {
                        sbb.Executor.Lines = compiler.Lines;
                    }
                }
            } 

            structureLoaderData.Add(sbb);
        }

        structureData = structureLoaderData;
        return true;
    }



    public static void Save(string name, List<StructureData> structureBoundingBoxes)
    {
        var basePath = FileManager.CreatePath(_structurePath, name);
        var dataPath = FileManager.CreatePath(basePath, "data");

        JSONBaseData baseData = new()
        {
            Name = name
        };

        string json = JsonConvert.SerializeObject(baseData, Formatting.Indented, _settings);
        File.WriteAllText(Path.Combine(basePath, "config.json"), json);

        HashSet<string> directories = [..Directory.GetDirectories(dataPath)];

        for (int i = 0; i < structureBoundingBoxes.Count; i++)
        {
            var bb = structureBoundingBoxes[i];
            var path = FileManager.CreatePath(dataPath, bb.Name);

            directories.Remove(path);

            JSONStructureData data = new()
            {
                Name = bb.Name,
                Size = [bb.Size.X, bb.Size.Y, bb.Size.Z],
                SavePosition = [bb.SavePosition.X, bb.SavePosition.Y, bb.SavePosition.Z],
                Core = bb.Core,
                ConnectionPoints = [],
            };

            for (int j = 0; j < bb.BoundingBoxes.Count; j++)
            {
                var boundingBox = bb.BoundingBoxes[j];
                data.BoundingBoxes.Add(new()
                {
                    Position = [boundingBox.Position.X, boundingBox.Position.Y, boundingBox.Position.Z],
                    Size = [boundingBox.Size.X, boundingBox.Size.Y, boundingBox.Size.Z]
                });
            }

            for (int j = 0; j < bb.Extenders.Count; j++)
            {
                var extender = bb.Extenders[j];
                data.Extenders.Add(new()
                {
                    Position = [extender.Position.X, extender.Position.Y, extender.Position.Z],
                    Size = [extender.Size.X, extender.Size.Y, extender.Size.Z]
                });
            }

            foreach (var (n, connection) in bb.ConnectionPoints)
            {
                var c = new JSONConnectionData
                {
                    Position = [connection.Position.X, connection.Position.Y, connection.Position.Z],
                    Yrotation = connection.Yrotation,
                    Side = connection.Side,
                    Categories = [.. connection.Categories],
                    Avoid = [.. connection.Avoid]
                };
                data.ConnectionPoints.Add(n, c);
            }

            foreach (var (n, rule) in bb.RulesetPoints)
            {
                data.Rulesets.Add(n, new()
                {
                    Position = [rule.Position.X, rule.Position.Y, rule.Position.Z],
                });
            }

            json = JsonConvert.SerializeObject(data, Formatting.Indented, _settings);
            File.WriteAllText(Path.Combine(path, "config.json"), json);

            SaveBlocks(Path.Combine(path, "blocks.dat"), bb.Blocks);
            File.WriteAllLines(Path.Combine(path, "script.txt"), bb.Lines);
        }

        foreach (var dir in directories)
        {
            Directory.Delete(dir, true);
        }
    }

    public static void SaveBlocks(string path, Block[] blocks)
    {
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        bw.Write(blocks.Length);

        for (int i = 0; i < blocks.Length; i++)
            bw.Write(blocks[i].blockData);
    }

    public static Block[] LoadBlocks(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        int count = br.ReadInt32();

        var blocks = new Block[count];
        for (int i = 0; i < count; i++)
        {
            blocks[i] = new Block
            {
                blockData = br.ReadUInt32()
            };
        }

        return blocks;
    }

    public class JSONBaseData
    {
        public string Name { get; set; } = "Base";
    }

    public class JSONStructuresData
    {
        public List<JSONStructureData> Structures { get; set; } = [];
    }

    public class JSONStructureData
    {
        public string Name { get; set; } = "";
        public List<int> Size { get; set; } = [];
        public List<int> SavePosition { get; set; } = [];
        public bool Core { get; set; }
        public List<JSONBoundingBoxData> BoundingBoxes { get; set; } = [];
        public List<JSONExtenderData> Extenders { get; set; } = [];
        public Dictionary<string, JSONConnectionData> ConnectionPoints { get; set; } = [];
        public Dictionary<string, JSONRulesetData> Rulesets { get; set; } = [];
        public List<uint> Blocks { get; set; } = [];
        public string Script { get; set; } = "";
    }

    public class JSONBoundingBoxData
    {
        public List<int> Position { get; set; } = [];
        public List<int> Size { get; set; } = [];
    }

    public class JSONExtenderData
    {
        public List<int> Position { get; set; } = [];
        public List<int> Size { get; set; } = [];
    }

    public class JSONConnectionData
    {
        public List<float> Position { get; set; } = [];
        public List<string> Categories { get; set; } = [];
        public List<string> Avoid { get; set; } = [];
        public int Yrotation { get; set; }
        public int Side { get; set; }
    }

    public class JSONRulesetData
    {
        public List<float> Position { get; set; } = [];
    }
}