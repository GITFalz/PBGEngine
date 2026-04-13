using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using PBG.Data;
using PBG.Files;

public static class BlockDataLoader
{
    static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public static bool LoadFromPath(string path, [NotNullWhen(true)] out BlockJSON? blockJson)
    {
        
        blockJson = null;
        if (!File.Exists(path))
            return false;

        var json = File.ReadAllText(path);
        blockJson = JsonSerializer.Deserialize<BlockJSON>(json, _options);
        return blockJson != null;
    }
}