using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using PBG.MathLibrary;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Diagnosers;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

[DisassemblyDiagnoser(
    printSource: true,
    maxDepth: 5,                          // 3 is often enough, 5 is safer
    exportCombinedDisassemblyReport: true, // side-by-side HTML for both methods
    filters: new[] { "*TestNative*", "*TestWrapper*", "*Avx2Float*" }
)]
[MemoryDiagnoser]
public class SimdBenchmark
{
    private Vector256<float> a;
    private Vector256<float> b;
    private Vector256<float> c;

    private Avx2Float wa;
    private Avx2Float wb;
    private Avx2Float wc;

    [GlobalSetup]
    public void Setup()
    {
        a = Vector256.Create(2f);
        b = Vector256.Create(3f);
        c = Vector256.Create(4f);

        wa = new Avx2Float(a);
        wb = new Avx2Float(b);
        wc = new Avx2Float(c);
    }

    [Benchmark(Baseline = true)]
    public Vector256<float> Native()
        => Avx2Test.TestNative(a, b, c);

    [Benchmark]
    public Avx2Float Wrapper()
        => Avx2Test.TestWrapper(wa, wb, wc);
}

public static class Avx2Test
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector256<float> TestNative(
        Vector256<float> a,
        Vector256<float> b,
        Vector256<float> c)
    {
        var x = a + b;
        var y = x * c;
        var z = y - a;
        var w = z / b;

        return w * c + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Avx2Float TestWrapper(
        Avx2Float a,
        Avx2Float b,
        Avx2Float c)
    {
        var x = a + b;
        var y = x * c;
        var z = y - a;
        var w = z / b;

        return w * c + x;
    }
}