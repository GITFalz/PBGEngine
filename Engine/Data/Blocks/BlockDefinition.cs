using PBG.MathLibrary;
using PBG.Physics;
using PBG.Voxel;
using static PBG.Voxel.VoxelChunkGenerator;

public class BlockDefinition
{
    public BlockDefinitionType Type = BlockDefinitionType.Missing;
    public bool CanRotate = false;
    public Block Block;
    public string Name = "";

    public int CacheIndex = 0;
    public int VariantCount = 1;

    public Collider[][] Colliders = [];
    
    public NewBlockFaces[] NewBlockFaces = [];

    public BlockPlacement? Placements = null;

    public void GenerateFullBlock(BaseVoxelChunkHandler blockSetter, Vector3 position, int rotation = 0)
    {
        try
        {
            var blockFaces = NewBlockFaces[rotation];
            GenerateFaces(blockSetter, blockFaces.InternalFaces, position);

            for (int side = 0; side < 6; side++)
            {
                for (int i = 0; i < blockFaces.Faces[side].Length; i++)
                {
                    blockSetter.AddFace(blockFaces.Faces[side][i], position);
                }
            }
        }
        catch
        {
            Console.WriteLine($"[Error] : Block '{Name}' has a problem generating full block with index: '{rotation}'");
        }  
    }

    public static void GenerateFaces(BaseVoxelChunkHandler blockGetter, VoxelFace[] faces, Vector3 position)
    {
        for (int i = 0; i < faces.Length; i++)
        {
            blockGetter.AddFace(faces[i], position);
        }
    }

    public static void AddFace(List<BlockVertexData> vertexData, VoxelFace face, Vector3 position)
    {
        var bottomLeft = new BlockVertexData(face.A + position , face.UvA, face.Normal, face.TextureIndex, face.Side, 0);
        var topLeft = new BlockVertexData(face.B + position, face.UvB, face.Normal, face.TextureIndex, face.Side, 1);
        var topRight = new BlockVertexData(face.C + position, face.UvC, face.Normal, face.TextureIndex, face.Side, 2);
        var bottomRight = new BlockVertexData(face.D + position, face.UvD, face.Normal, face.TextureIndex, face.Side, 3);
        vertexData.AddRange(bottomLeft, topLeft, topRight, bottomRight);
    }
}
