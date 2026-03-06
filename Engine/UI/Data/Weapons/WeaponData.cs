using System.Text.Json;
using PBG.Files;
using PBG.Graphics;

namespace PBG.Data;

public class WeaponData
{
    public static string WeaponDataPath = FileManager.CreatePath(Game.MainPath, "data", "weapons");
    public static string WeaponModelPath = FileManager.CreatePath(Game.AssetsPath, "models", "weapons");

    public static Dictionary<string, WeaponDefinition> WeaponDefinitions = [];

    public static Dictionary<string, SimpleModel> Weapons = [];

    public string Name = "";
    public uint Index = 0;

    public WeaponData(uint index, string name)
    {
    }

    public static bool TryGet(string name, out SimpleModel? model) => Weapons.TryGetValue(name, out model);

    public static void Init()
    {
        LoadWeapons();
        LoadModels();
    }

    public static void LoadWeapons()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var weaponFiles = Directory.GetFiles(WeaponDataPath, "*.json");
        for (int i = 0; i < weaponFiles.Length; i++)
        {
            string weaponFile = weaponFiles[i];
            string json = File.ReadAllText(weaponFile);

            var definition = JsonSerializer.Deserialize<WeaponDefinition>(json, options);

            if (definition == null)
            {
                Console.WriteLine("[Error] : [BlockLoader] : An error occured when deserializing the block in file: " + weaponFile);
                continue;
            }

            WeaponDefinitions[definition.Name] = definition;

            Console.WriteLine($"[Loaded weapon] : {definition}");
        }
    }

    public static void LoadModels()
    {
        foreach (var (name, definition) in WeaponDefinitions)
        {
            var path = Path.Combine(WeaponModelPath, name + ".model");
            if (File.Exists(path))
            {
                /*
                var model = new SimpleModel(path);
                Weapons.Add(name, model);
                _ = new WeaponItemData(definition.ID, model);
                */
            }
        }
    }

    public class WeaponDefinition
    {
        public uint ID { get; set; } = 0;
        public string Name { get; set; } = "";

        public override string ToString() => $"Name: {Name}";
    }
}