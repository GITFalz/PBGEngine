using Newtonsoft.Json;

namespace PBG.Editor;

public static class EditorLoader
{
    private static string _path = "";
    public static EditorFile Files = new();

    public static void Load(string path)
    {
        if (!File.Exists(path))
            return;

        _path = path;
        JsonSerializerSettings settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        string json = File.ReadAllText(path);
        EditorFile? nodeConfig = JsonConvert.DeserializeObject<EditorFile>(json, settings);
        if (nodeConfig != null)
        {
            Files = nodeConfig;
            Files.Init();
        }
    }

    public static void Save() => Save(_path);
    public static void Save(string path)
    {
        JsonSerializerSettings settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };
        var json = JsonConvert.SerializeObject(Files, settings);
        File.WriteAllText(path, json);
    }
}

public class EditorFile
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public List<EditorFile> Files { get; set; } = [];

    [JsonIgnore]
    public EditorFile? Parent = null;

    [JsonIgnore]
    public Dictionary<string, EditorFile> FileMap = [];

    [JsonIgnore]
    public PString GlobalPath => Game.CurrentProjectPath / Path;

    [JsonIgnore]
    public bool IsDirectory => Type == "folder";

    public void AddFile(EditorFile file)
    {
        file.Parent = this;
        Files.Add(file);
        FileMap[file.Name] = file;
    }

    public void Init(EditorFile? parent = null)
    {
        Console.WriteLine(Name + " " + Path);
        Parent = parent;
        for (int i = 0; i < Files.Count; i++)
        {
            var file = Files[i];
            file.Init(this);
            FileMap[file.Name] = file;
        }
    }

    public string[] GetDirectories()
    {
        int count = 0;
        for (int i = 0; i < Files.Count; i++)
        {
            var file = Files[i];
            if (file.IsDirectory)
                count++;
        }
        string[] directories = new string[count];
        count = 0;
        for (int i = 0; i < Files.Count; i++)
        {
            var file = Files[i];
            if (file.IsDirectory)
            {
                directories[count] = file.GlobalPath;
                count++;
            }
        }
        return directories;
    }

    public string[] GetFiles()
    {
        int count = 0;
        for (int i = 0; i < Files.Count; i++)
        {
            var file = Files[i];
            if (!file.IsDirectory)
                count++;
        }
        string[] files = new string[count];
        count = 0;
        for (int i = 0; i < Files.Count; i++)
        {
            var file = Files[i];
            if (!file.IsDirectory)
            {
                files[count] = file.GlobalPath;
                count++;
            }
        }
        return files;
    }

    public EditorFile? GetFile(string name)
    {
        if (FileMap.TryGetValue(name, out var file))
            return file;
        return null;
    }

    public void CreateDirectory(string folderName)
    {
        var newFolderPath = GlobalPath / folderName;
        var newFolderName = folderName;
        int i = 0;
        while (Directory.Exists(newFolderPath))
        {
            newFolderName = $"{folderName}_copy" + (i == 0 ? "" : i);
            newFolderPath = GlobalPath / newFolderName;
            i++;
        }
        Directory.CreateDirectory(newFolderPath);
        EditorFile file = new()
        {
            Type = "folder",
            Name = newFolderName,
            Path = System.IO.Path.Combine(Path, newFolderName)
        };
        AddFile(file);
        EditorLoader.Save();
    }

    public void CreateFile(string fileName)
    {
        var ext = System.IO.Path.GetExtension(fileName);
        if (ext == "") ext = ".txt";
        fileName = System.IO.Path.GetFileNameWithoutExtension(fileName);

        var newFilePath = GlobalPath / (fileName + ext);
        var newFileName = fileName;
        int i = 0;
        while (File.Exists(newFilePath))
        {
            newFileName = $"{fileName}_copy" + (i == 0 ? "" : i);
            newFilePath = GlobalPath / (newFileName + ext);
            i++;
        }
        File.Create(newFilePath).Dispose();;
        var type = "file";
        if (_fileTypes.TryGetValue(ext, out var t))
            type = t;

        EditorFile file = new()
        {
            Type = type,
            Name = newFileName,
            Path = System.IO.Path.Combine(Path, newFileName + ext)
        };
        AddFile(file);
        EditorLoader.Save();
    }

    public void DeleteFile(string fileName)
    {
        if (FileMap.TryGetValue(fileName, out var file) && File.Exists(file.GlobalPath))
        {
            File.Delete(file.GlobalPath);
            Files.Remove(file);
            FileMap.Remove(fileName);
        }
    }

    public void DeleteFolder(string fileName)
    {
        if (FileMap.TryGetValue(fileName, out var file) && Directory.Exists(file.GlobalPath))
        {
            Directory.Delete(file.GlobalPath);
            Files.Remove(file);
            FileMap.Remove(fileName);
        }
    }

    private static readonly Dictionary<string, string> _fileTypes = new()
    {
        { ".cs", "script" },
        { ".obj", "obj" },
        { ".pbgscene", "scene" }
    };
}