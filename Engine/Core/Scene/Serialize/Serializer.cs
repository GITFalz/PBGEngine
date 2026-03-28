using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using PBG.Editor;

namespace PBG.Core;

public static class SceneSerializer
{
    public static bool Deserialize(string path)
    {
        if (!File.Exists(path))
            return false;

        if (Path.GetExtension(path) != ".json")
            return false;

        SceneBlueprint.Clear();

        JsonSerializerSettings settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
        };

        string json = File.ReadAllText(path);
        SceneBaseJson? sceneConfig = JsonConvert.DeserializeObject<SceneBaseJson>(json, settings);
        if (sceneConfig != null)
        {
            sceneConfig.Load();
            SceneBlueprint.CurrentPath = path;
        }   
        return true;
    }

    public static bool Serialize(string path, SceneBaseJson sceneJson)
    {
        if (Path.GetExtension(path) != ".json")
            return false;

        JsonSerializerSettings settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        string json = JsonConvert.SerializeObject(sceneJson, settings);
        File.WriteAllText(path, json);
        return true;
    }

    private enum SerializeStage
    {
        None,
        Node,
        Component
    }
}