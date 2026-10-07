using System.Reflection;
using System.Runtime.Loader;

public sealed class ScriptContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    // Names of assemblies that must come from the host (default context)
    private static readonly HashSet<string> Shared = new()
    {
        "Engine.Core",
        "Engine.Main",
    };

    public ScriptContext(string assemblyPath) : base(name: "Scripts", isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(assemblyPath);
    }

    protected override Assembly? Load(AssemblyName name)
    {
        if (name.Name == null) return null;

        // Anything the host already has (Engine.Core, Silk.NET.*, etc.) is shared
        foreach (var a in AssemblyLoadContext.Default.Assemblies)
            if (a.GetName().Name == name.Name)
                return a;

        // Anything the host can resolve on its own
        try { return AssemblyLoadContext.Default.LoadFromAssemblyName(name); }
        catch { /* not available in the host, fall through */ }

        var path = _resolver.ResolveAssemblyToPath(name);
        return path != null ? LoadFromAssemblyPath(path) : null;
    }
}