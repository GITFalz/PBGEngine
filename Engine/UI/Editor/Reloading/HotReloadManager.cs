using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PBG.Core;

namespace PBG.Editor;

public class HotReloadManager
{
    private static AssemblyLoadContext? _context;
    private static Dictionary<string, HotReloadManager> _oldHotReloaders = [];
    private static Dictionary<string, HotReloadManager> _currentHotReloaders = [];

    public readonly string Name;
    public int InUse = 0;
    public Type Type;

    public HotReloadManager(string name, Type type)
    {
        Name = name;
        Type = type;
    }

    public static bool Get(string name, [NotNullWhen(true)] out HotReloadManager? hotReloadManager) => _currentHotReloaders.TryGetValue(name.ToLowerInvariant(), out hotReloadManager);
    public static bool IsOld(string name, [NotNullWhen(true)] out HotReloadManager? hotReloadManager) => _oldHotReloaders.TryGetValue(name.ToLowerInvariant(), out hotReloadManager);

    public ScriptingNode? CreateInstance()
    {
        InUse++;
        return (ScriptingNode?)Activator.CreateInstance(Type);
    }

    public static bool Compile(string path, string outputPath)
    {
        var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
        var syntaxTrees = files.Select(file => CSharpSyntaxTree.ParseText(ReadFile(file))
        ).ToList();

        var references = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location) && !(a.FullName ?? "").Contains("HotReload")).Select(a => MetadataReference.CreateFromFile(a.Location));

        var compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(outputPath),
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var stream = File.Create(outputPath);
        var result = compilation.Emit(stream);

        if (!result.Success)
        {
            foreach (var diag in result.Diagnostics)
                Console.WriteLine(diag.ToString());
            return false;
        }
        return true;
    }

    static string ReadFile(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static bool Load(string path)
    {
        Unload();

        if (!WaitForFile(path))
        {
            Log("Timed out waiting for file to be ready");
            return false;
        }

        _context = new AssemblyLoadContext("HotReload", true);
        AttachDiagnosticListeners(_context);

        if (!TryReadAssemblyBytes(path, out var assemblyBytes))
            return false;

        Assembly assembly;
        using (var stream = new MemoryStream(assemblyBytes))
            assembly = _context.LoadFromStream(stream);

        var name = AssemblyName.GetAssemblyName(path);
        Log($"Loaded: {name.FullName} (v{name.Version})");

        if (!TryInspectTypes(assembly, out var types))
            return false;

        var matches = types.Where(t => typeof(ScriptingNode).IsAssignableFrom(t) && !t.IsAbstract).ToList();

        Dictionary<string, HotReloadManager> newHotReloaders = [];

        foreach (var match in matches)
        {
            var typeName = match.Name.ToLowerInvariant();
            Console.WriteLine("Loading type " + match);
            if (_currentHotReloaders.TryGetValue(typeName, out var oldReloader))
            {
                newHotReloaders.Add(typeName, oldReloader);
                oldReloader.InUse = 0;
                oldReloader.Type = match;
                _currentHotReloaders.Remove(typeName);
            }
            else
            {
                var reloader = new HotReloadManager(typeName, match);
                newHotReloaders.TryAdd(typeName, reloader);
            }
        }

        _currentHotReloaders = newHotReloaders;

        _oldHotReloaders = [];
        foreach (var (typeName, oldReloader) in _currentHotReloaders)
        {
            _oldHotReloaders.TryAdd(typeName, oldReloader);
            oldReloader.InUse = 0;
        }

        return true;
    }

    private static bool TryReadAssemblyBytes(string path, out byte[]? bytes)
    {
        bytes = null;
        try
        {
            bytes = File.ReadAllBytes(path);
            return true;
        }
        catch (IOException ex)
        {
            Log($"File read failed: {ex.Message}");
            return false;
        }
    }

    private static void AttachDiagnosticListeners(AssemblyLoadContext context)
    {
        context.Resolving += (ctx, assemblyName) =>
        {
            Log($"[Resolving] {assemblyName.FullName}");
            return null;
        };

        context.ResolvingUnmanagedDll += (assembly, dllName) =>
        {
            Log($"[ResolvingUnmanaged] {dllName} from {assembly.GetName().Name}");
            return IntPtr.Zero;
        };
    }

    private static bool TryInspectTypes(Assembly assembly, [NotNullWhen(true)] out Type[]? types)
    {
        types = null;
        try
        {
            types = assembly.GetTypes();
            Log($"Types loaded successfully: {types.Length}");
            return true;
        }
        catch (ReflectionTypeLoadException ex)
        {
            var loaded  = ex.Types.Where(t => t != null).ToList();
            var failed  = ex.Types.Select((t, i) => (t, ex.LoaderExceptions[i]))
                                .Where(x => x.t == null)
                                .ToList();

            Log($"Partial load — succeeded: {loaded.Count}, failed: {failed.Count}");

            LogLoaderExceptions(ex.LoaderExceptions);
            LogReferenceSkew(assembly);

            return false;
        }
    }

    private static void LogLoaderExceptions(IEnumerable<Exception?> exceptions)
    {
        foreach (var ex in exceptions)
        {
            if (ex == null) continue;

            Log($"[LoaderException] {ex.GetType().Name} 0x{ex.HResult:X8}: {ex.Message}");

            if (ex is TypeLoadException tle && tle.TypeName != null)
                Log($"  FailedType: {tle.TypeName}");

            if (ex.StackTrace != null)
                Log($"  Stack: {ex.StackTrace}");

            for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
                Log($"  Inner: {inner.Message}");
        }
    }

    private static void LogReferenceSkew(Assembly assembly)
    {
        Log("Checking referenced assemblies for version skew...");
        foreach (var refName in assembly.GetReferencedAssemblies())
        {
            try
            {
                var resolved = Assembly.Load(refName);
                var skewed   = resolved.FullName != refName.FullName;
                var tag      = skewed ? "SKEW" : "OK  ";

                Log($"  [{tag}] {refName.FullName}");
                if (skewed)
                    Log($"         Got: {resolved.FullName}");
            }
            catch (Exception ex)
            {
                Log($"  [MISS] {refName.FullName} → {ex.Message}");
            }
        }
    }

    private static void Log(string message) => Console.WriteLine($"[HotReload] {message}");

    private static bool WaitForFile(string path, int timeoutMs = 3000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (IsFileReady(path)) return true;
            Thread.Sleep(100);
        }
        return false;
    }

    private static bool IsFileReady(string path)
    {
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
            return stream.Length > 0;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public static void CleanUp()
    {
        Dictionary<string, HotReloadManager> newReloaders = [];
        foreach (var (name, reloader) in _oldHotReloaders)
        {
            if (reloader.InUse > 0)
                newReloaders.Add(name, reloader);
        }
        _oldHotReloaders = newReloaders;
    }

    public static void Unload()
    {
        _context?.Unload();
        _context = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}