using System.Diagnostics;
using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.MathLibrary;
using PBG.Voxel;

namespace PBG.Assets.Scripts.NoiseNodes.Nodes;

public class StructurePlacementNode(GetterValue[] getters, SetterValue[] setters, string type) : NoiseNode(getters, setters, type)
{
    public GetterValue Height = getters[0];

    public override void Basic(int x, int y) { }
    public override void Run(NoiseNodeManager manager, VoxelChunk chunk, int x, int y)
    {
        /*
        Stopwatch stopwatch = Stopwatch.StartNew();

        Vector3i relative = VoxelData.BlockToRelative((x, 0, y));
        if (relative != Vector3i.Zero)
            return;

        if (StructureManager.StructureChunks.TryGetValue(chunk.WorldPosition.Xz, out var structures))
        {
            if (manager.PersonalCopy == null)
                throw new Exception("Personal copy is null in StructurePlacementNode");

            for (int i = 0; i < structures.Count; i++)
            {
                var structure = structures[i];
                var getter = Getters[0].Copy(manager.PersonalCopy.Variables); 
                structure.StructureData.Generate(manager.PersonalCopy, getter, chunk, structure);
            }
        }

        stopwatch.Stop();
        //Console.WriteLine("structure generation: " + stopwatch.ElapsedMilliseconds + " ms");
        */
    }
    
    public override void LOD(NoiseNodeManager manager, LODChunk chunk, int level, int x, int y)
    {
        /*
        Vector3i relative = VoxelData.BlockToRelative((x, 0, y));
        if (relative != Vector3i.Zero)
            return;

        if (StructureManager.StructureChunks.TryGetValue(chunk.WorldPosition.Xz, out var structures))
        {
            if (manager.PersonalCopy == null)
                throw new Exception("Personal copy is null in StructurePlacementNode");

            for (int i = 0; i < structures.Count; i++)
            {
                var structure = structures[i];
                var getter = Getters[0].Copy(manager.PersonalCopy.Variables);
                structure.StructureData.Generate(manager.PersonalCopy, getter, chunk, structure);
            }
        }
        */
    }
    protected override Expression BuildBasicExpression() 
    { 
        return CallManager(GetType(), "GenerateBasic", [I], Height.Int());
    }
    protected override Expression BuildTerrainExpression()
    {
        return CallMain(GetType(), "GenerateTerrain");
    }

    public static void GenerateBasic(NoiseNodeManager manager, int height)
    {
        StructureManager.SetHeight(manager, height);
    }

    public static void GenerateTerrain(NoiseNodeManager manager, VoxelChunk chunk, Vector2i position)
    {
        Vector3i relative = VoxelData.BlockToRelative((position.X, 0, position.Y));
        if (relative != Vector3i.Zero)
            return;

        if (StructureManager.StructureChunks.TryGetValue(chunk.WorldPosition.Xz, out var structures))
        {
            for (int i = 0; i < structures.Count; i++)
            {
                var structure = structures[i];
                structure.StructureData.Generate(manager, chunk, structure);
            }
        }
    }
}