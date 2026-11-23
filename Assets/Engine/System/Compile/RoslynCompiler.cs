using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace PBG.Roslyn;

public sealed class CompiledContext<TDelegate> : IDisposable where TDelegate : Delegate
{
    public TDelegate Invoke { get; }
    private readonly CollectibleLoadContext _alc;

    internal CompiledContext(TDelegate fn, CollectibleLoadContext alc) { Invoke = fn; _alc = alc; }
    public void Dispose() => _alc.Unload();
}

sealed class CollectibleLoadContext : AssemblyLoadContext { public CollectibleLoadContext() : base(isCollectible: true) { } }

public static class Compiler
{
    static readonly MetadataReference[] DefaultRefs =
        [.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))];

    public static CompiledContext<TDelegate> Compile<TDelegate>(
        string source, string typeName, string methodName,
        IEnumerable<MetadataReference>? extraRefs = null) where TDelegate : Delegate
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));

        var compilation = CSharpCompilation.Create(
            "Graph_" + Guid.NewGuid().ToString("N"), [tree],
            extraRefs == null ? DefaultRefs : DefaultRefs.Concat(extraRefs),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString());
            throw new InvalidOperationException(string.Join("\n", errors));
        }

        ms.Position = 0;
        var alc = new CollectibleLoadContext();
        var asm = alc.LoadFromStream(ms);

        var method = asm.GetType(typeName)!.GetMethod(methodName)!;
        var del = (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), method);
        return new CompiledContext<TDelegate>(del, alc);
    }
}