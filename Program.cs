using PBG;

class Program
{
    static void Main(string[] args)
    {
        using var game = new VoxelEngine(1500, 1000);
        game.Run();
    }
}