using System.Reflection;
using System.Runtime.CompilerServices;

public sealed class ScriptLoader
{
    private ScriptContext? _ctx;
    public Assembly? Assembly { get; private set; }

    public Assembly Load(string dllPath)
    {
        _ctx = new ScriptContext(dllPath);

        // Read into memory so the file isn't locked and can be rebuilt while running
        var dll = File.ReadAllBytes(dllPath);
        var pdbPath = Path.ChangeExtension(dllPath, ".pdb");

        using var dllStream = new MemoryStream(dll);
        if (File.Exists(pdbPath))
        {
            using var pdbStream = new MemoryStream(File.ReadAllBytes(pdbPath));
            Assembly = _ctx.LoadFromStream(dllStream, pdbStream);
        }
        else
        {
            Assembly = _ctx.LoadFromStream(dllStream);
        }

        return Assembly;
    }

    // NoInlining so the JIT doesn't keep locals alive and block the unload
    [MethodImpl(MethodImplOptions.NoInlining)]
    public WeakReference Unload()
    {
        var weak = new WeakReference(_ctx, trackResurrection: true);
        Assembly = null;
        _ctx?.Unload();
        _ctx = null;
        return weak;
    }
}