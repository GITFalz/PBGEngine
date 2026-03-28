using PBG.Asset;

namespace PBG.Rendering;

public static class MeshCache
{
    public static HashSet<Mesh> MeshesHash = [];
    public static Dictionary<string, Mesh> Meshes = [];
    public static Dictionary<string, AssetLoader<MeshRenderer>> Loading = [];

    public static void Cache(string path, Mesh mesh)
    {
        Meshes[path] = mesh;
        MeshesHash.Add(mesh);
    }
}