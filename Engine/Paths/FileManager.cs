namespace PBG.Files
{
    public static class FileManager
    {
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
            parts.Insert(0, Game.MainPath);
            path = Path.Combine([.. parts]);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}