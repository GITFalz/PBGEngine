namespace PBG.Files
{
    public static class FileManager
    {
        public static PString MainPath = FileManager.CreatePath(GetRealUserAppData(), ".projectVoxel");
        public static PString AssetsPath = FileManager.CreatePath(MainPath, "assets");
        public static PString ShaderPath = FileManager.CreatePath(AssetsPath, "shaders");
        public static PString FixedShaderPath = FileManager.CreatePath(AssetsPath, "fixedShaders");
        public static PString TexturePath = FileManager.CreatePath(AssetsPath, "textures");
        public static PString SettingsPath = FileManager.CreatePath(AssetsPath, "settings");

        public static PString DataPath = FileManager.CreatePath(MainPath, "data");
        public static PString ModelPath = FileManager.CreatePath(DataPath, "models");
        public static PString UndoModelPath = FileManager.CreatePath(ModelPath, "undo");
        public static PString EditorRegistryPath = FileManager.CreatePath(DataPath, "registry");
        public static PString EditorPalettePath = FileManager.CreatePath(DataPath, "palette");

        public static PString CustomPath = FileManager.CreatePath(MainPath, "custom");
        public static PString CustomTempPath = FileManager.CreatePath(CustomPath, "temp");
        
        public static string CreatePath(params string[] paths)
        {
            string path = Path.Combine(paths);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public static void FileOverwrite(string shaderFromPath, string identifier, string shaderToPath, List<string> lines)
        {
            if (!File.Exists(shaderFromPath))
                throw new FileNotFoundException("[Error] : Shader from not found at path: " + shaderFromPath);

            List<string> newLines = [];
            string[] baseLines = File.ReadAllLines(shaderFromPath);
            foreach (var line in baseLines)
            {
                if (line.Contains(identifier))
                {
                    newLines.AddRange(lines);
                    continue;
                }
                newLines.Add(line);
            }
            File.WriteAllLines(shaderToPath, newLines);
        }

        public static void CheckDirectory(string path)
        {
            List<string> parts = [.. path.Split(['\\', '/'])];
            parts.Insert(0, MainPath);
            path = Path.Combine([.. parts]);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        static string GetRealUserAppData()
        {
            // When running under sudo, SUDO_USER contains the original username
            string? user = Environment.GetEnvironmentVariable("SUDO_USER");
            
            if (!string.IsNullOrEmpty(user))
            {
                // Linux
                return $"/home/{user}/.config";
            }

            // Normal case (not running as root)
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
    }   
}