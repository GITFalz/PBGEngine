using PBG;
using PBG.Graphics;
using BenchmarkDotNet.Running;

class Program
{
    static void Main(string[] args)
    {
        bool debug = true;

        if (debug && args.Length > 0 && args[0] == "--profile-mesh")
        {
            int iterations = args.Length > 1 ? int.Parse(args[1]) : 10_000;
            PBG.NewVoxel.GreedyMesher9YByte.MeshBench.Profile(iterations);
            return;
        }

        if (debug && args.Contains("--filter", StringComparer.Ordinal) || args.Contains("-f", StringComparer.Ordinal))
        {
            BenchmarkSwitcher.FromAssembly(typeof(PBG.NewVoxel.GreedyMesher9YByte).Assembly).Run(args);
            return;
        }

        GameWindow.New(new Game(), 1200, 800);
    }
}