using PBG.MathLibrary;
using PBG.Physics;
using PBG.Voxel;

namespace PBG.Data;

public partial class BlockJSON
{
    private static int _cacheIndex = 0;
    public static int GeometryIndex = 0;

    public static List<List<VoxelFace>> FrontFaces = [];
    public static List<List<VoxelFace>> RightFaces = [];
    public static List<List<VoxelFace>> TopFaces = [];
    public static List<List<VoxelFace>> LeftFaces = [];
    public static List<List<VoxelFace>> BottomFaces = [];
    public static List<List<VoxelFace>> BackFaces = [];
    public static List<List<VoxelFace>> InternalFaces = [];

    public uint ID { get; set; } = 0;
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Rotation { get; set; } = "fixed";
    public FaceDefaultsJson DefaultFace { get; set; } = new();
    public uint ParserVersion { get; set; } = 0;
    public VariantJSON[] Variants { get; set; } = [];
    public string[] Textures { get; set; } = [];
    public BoxJSON[]? Collision { get; set; } = null;
    public BoxJSON[]? Geometry { get; set; } = null;

    public void Generate(BlockDefinition definition)
    {
        int blockCount = 1 + Variants.Length;
        List<VariantJSON> variants = [new(), ..Variants];

        FrontFaces = [];
        RightFaces = [];
        TopFaces = [];
        LeftFaces = [];
        BottomFaces = [];
        BackFaces = [];
        InternalFaces = [];

        definition.CacheIndex = _cacheIndex;
        definition.VariantCount = blockCount;

        _cacheIndex += blockCount;
        
        Collision ??= [new()];
        Geometry ??= [new()];

        definition.Colliders = new Collider[blockCount][];

        if (Variants.Length > 0)
        {
            definition.Placements = new();
        }

        for (int i = 0; i < blockCount; i++)
        {
            FrontFaces.Add([]);
            RightFaces.Add([]);
            TopFaces.Add([]);
            LeftFaces.Add([]);
            BottomFaces.Add([]);
            BackFaces.Add([]);
            InternalFaces.Add([]);

            var variant = variants[i];

            variant.GenerateGeometry(definition, this, Geometry, i);
            variant.PopulatePlacements(definition, i);
            variant.RotateBoxes(Collision, out var boxes, out var _);

            definition.Colliders[i] = new Collider[Collision.Length];
            for (int j = 0; j < Collision.Length.Min(boxes.Count); j++)
                definition.Colliders[i][j] = boxes[j].GetCollider();
        }

        if (Geometry.Length == 1 && Type == "solid")
        {
            definition.NewBlockFaces = new NewBlockFaces[blockCount];

            for (int i = 0; i < blockCount; i++)
            {
                var newBlockFaces = new NewBlockFaces
                {
                    Faces = new VoxelFace[6][],
                    BitMasks = new ulong[6]
                };

                void Set(List<List<VoxelFace>> faces, int side)
                {
                    if (faces[i].Count > 0) 
                    {
                        newBlockFaces.Faces[side] = [faces[i][0]];
                        newBlockFaces.BitMasks[side] = faces[i][0].BitMask;
                    }
                    else
                    {
                        newBlockFaces.Faces[side] = [];
                    }
                }

                Set(FrontFaces, 0);
                Set(RightFaces, 1);
                Set(TopFaces, 2);
                Set(LeftFaces, 3);
                Set(BottomFaces, 4);
                Set(BackFaces, 5);

                definition.NewBlockFaces[i] = newBlockFaces;
            }
        } 
        else
        {
            definition.NewBlockFaces = new NewBlockFaces[blockCount];
                  
            for (int i = 0; i < blockCount; i++)
            {
                var newBlockFaces = new NewBlockFaces
                {
                    Faces = new VoxelFace[6][],
                    BitMasks = new ulong[6]
                };

                void Set(List<List<VoxelFace>> faces, int side)
                {
                    if (faces[i].Count > 0) 
                    {
                        newBlockFaces.Faces[side] = [..faces[i]];
                        newBlockFaces.BitMasks[side] = GetCombinedBitMask(faces[i]);
                    }
                    else
                    {
                        newBlockFaces.Faces[side] = [];
                    }
                }

                Set(FrontFaces, 0);
                Set(RightFaces, 1);
                Set(TopFaces, 2);
                Set(LeftFaces, 3);
                Set(BottomFaces, 4);
                Set(BackFaces, 5);
                
                newBlockFaces.InternalFaces = [..InternalFaces[i]];

                definition.NewBlockFaces[i] = newBlockFaces;
            }
        }
    }

    static ulong GetCombinedBitMask(List<VoxelFace> faces)
    {
        ulong mask = 0;
        for (int i = 0; i < faces.Count; i++)
        {
            var face = faces[i];
            mask |= face.BitMask;
        }
        return mask;
    }
}

public class VariantJSON
{
    public int X { get; set; } = 0;
    public int Y { get; set; } = 0;
    public int Z { get; set; } = 0;
    public bool Flip { get; set; } = false;
    public string Order { get; set; } = "xyz";
    public VariantPlacementsJSON? Placements { get; set; } = null;

    public void RotateBoxes(BoxJSON[] boxesJson, out List<Box> boxes, out List<Faces> faces)
    {
        boxes = [];
        faces = [];

        foreach (var g in boxesJson)
        {
            boxes.Add(g.GetBox());
            faces.Add(g.GetFaces());
        }

        foreach (var c in Order.ToCharArray())
        {
            for (int i = 0; i < boxesJson.Length; i++)
            {
                var box = boxes[i];
                var face = faces[i];

                if (c == 'x')
                {
                    int x = Mathf.Rti((float)X / 90f);
                    if (x > 0)
                    {
                        for (int j = 0; j < x; j++)
                        {
                            box = box.RotatePositiveX();
                            face = face.RotatePositiveX();
                        }
                    }
                    else if (x < 0)
                    {
                        for (int j = 0; j < -x; j++)
                        {
                            box = box.RotateNegativeX();
                            face = face.RotateNegativeX();
                        }
                    }
                }
                else if (c == 'y')
                {
                    int y = Mathf.Rti((float)Y / 90f);
                    if (y > 0)
                    {
                        for (int j = 0; j < y; j++)
                        {
                            box = box.RotatePositiveY();
                            face = face.RotatePositiveY();
                        }
                    }
                    else if (y < 0)
                    {
                        for (int j = 0; j < -y; j++)
                        {
                            box = box.RotateNegativeY();
                            face = face.RotateNegativeY();
                        }
                    }
                }
                else if (c == 'z')
                {
                    int z = Mathf.Rti((float)Z / 90f);
                    if (z > 0)
                    {
                        for (int j = 0; j < z; j++)
                        {
                            box = box.RotatePositiveZ();
                            face = face.RotatePositiveZ();
                        }
                    }
                    else if (z < 0)
                    {
                        for (int j = 0; j < -z; j++)
                        {
                            box = box.RotateNegativeZ();
                            face = face.RotateNegativeZ();
                        }
                    }
                }

                boxes[i] = box;
                faces[i] = face;
            }

            if (c == 'x') X = 0;
            else if (c == 'y') Y = 0;
            else if (c == 'z') Z = 0;
        }

        for (int i = 0; i < boxesJson.Length; i++)
        {
            if (Flip)
            {
                boxes[i] = boxes[i].Flip();
                faces[i] = faces[i].Flip();
            }
        }
    }

    public void GenerateGeometry(BlockDefinition definition, BlockJSON json, BoxJSON[] geometry, int index)
    {
        RotateBoxes(geometry, out var boxes, out var faces);

        for (int i = 0; i < geometry.Length; i++)
        {
            var boxJson = geometry[i];
            var box = boxes[i];
            var face = faces[i];

            if (!face.Contains("front"))
                GenerateFace(definition, json, boxJson, box, BlockJSON.FrontFaces[index], (0, 0, -1),    box.From.Z == 0,    0,  0, 1,   index);

            if (!face.Contains("right"))
                GenerateFace(definition, json, boxJson, box, BlockJSON.RightFaces[index], (1, 0, 0),     box.To.X == 1,      1,  2, 1,   index);

            if (!face.Contains("top"))
                GenerateFace(definition, json, boxJson, box, BlockJSON.TopFaces[index], (0, 1, 0),     box.To.Y == 1,      2,  0, 2,   index);

            if (!face.Contains("left"))
                GenerateFace(definition, json, boxJson, box, BlockJSON.LeftFaces[index], (-1, 0, 0),    box.From.X == 0,    3,  2, 1,   index);

            if (!face.Contains("bottom"))
                GenerateFace(definition, json, boxJson, box, BlockJSON.BottomFaces[index], (0, -1, 0),    box.From.Y == 0,    4,  0, 2,   index);

            if (!face.Contains("back"))
                GenerateFace(definition, json, boxJson, box, BlockJSON.BackFaces[index], (0, 0, 1),     box.To.Z == 1,      5,  0, 1,   index);
        }
    }

    public void PopulatePlacements(BlockDefinition definition, int index)
    {
        Placements?.PopulatePlacements(definition, index);
    }

    public static void GenerateFace(BlockDefinition definition, BlockJSON json, BoxJSON boxJson, Box box, List<VoxelFace> voxelFaces, Vector3 normal, bool canBeOccluded, int side, int x, int y, int index)
    {
        int textureIndex = json.DefaultFace.Texture;
        var faces = boxJson.Faces;
        if (faces != null)
        {
            var over = faces.GetFace(side);
            if (over != null)
            {
                textureIndex = over.Texture;
            }
        }

        var faceData = GetFaceCoords(box.From, box.To, side);
        var uvData = GetFaceUvs(json, faceData, x, y);

        Vector3 A = (faceData.a.a.X, faceData.a.b.Y, faceData.a.c.Z);
        Vector3 B = (faceData.b.a.X, faceData.b.b.Y, faceData.b.c.Z);
        Vector3 C = (faceData.c.a.X, faceData.c.b.Y, faceData.c.c.Z);
        Vector3 D = (faceData.d.a.X, faceData.d.b.Y, faceData.d.c.Z);

        Vector2 uvA = uvData[0];
        Vector2 uvB = uvData[1];
        Vector2 uvC = uvData[2];
        Vector2 uvD = uvData[3];

        var voxelFace = new VoxelFace()
        {
            A = A,
            B = B,
            C = C,
            D = D,

            UvA = uvA,
            UvB = uvB,
            UvC = uvC,
            UvD = uvD,

            Normal = normal,
            TextureIndex = BlockData.RealTextureIndices[json.Name][textureIndex].index,
            GeometryIndex = BlockJSON.GeometryIndex,

            CanBeOccluded = canBeOccluded && json.Type == "solid",
            Side = side
        };

        BlockData.FaceGeometries.Add(new()
        {
            A = A,
            B = B,
            C = C,
            D = D,

            UvA = uvA,
            UvB = uvB,
            UvC = uvC,
            UvD = uvD,

            Normal = normal,

            TextureIndex = BlockData.RealTextureIndices[json.Name][textureIndex].index,
        });

        BlockJSON.GeometryIndex++;

        GetBitMask(ref voxelFace);

        if (voxelFace.CanBeOccluded)
            voxelFaces.Add(voxelFace);
        else
            BlockJSON.InternalFaces[index].Add(voxelFace);
    }

    public static (
        (Vector3 a, Vector3 b, Vector3 c) a, 
        (Vector3 a, Vector3 b, Vector3 c) b, 
        (Vector3 a, Vector3 b, Vector3 c) c, 
        (Vector3 a, Vector3 b, Vector3 c) d
    ) GetFaceCoords(Vector3 from, Vector3 to, int side)
    {
        if (side == 1) return ((to, from, from), (to, to, from), (to, to, to), (to, from, to));
        else if (side == 2) return ((from, to, from), (from, to, to), (to, to, to), (to, to, from));
        else if (side == 3) return ((from, from, to), (from, to, to), (from, to, from), (from, from, from));
        else if (side == 4) return ((to, from, from), (to, from, to), (from, from, to), (from, from, from));
        else if (side == 5) return ((to, from, to), (to, to, to), (from, to, to), (from, from, to));
        return ((from, from, from), (from, to, from), (to, to, from), (to, from, from));
    }

    public static Vector2[] GetFaceUvs(
        BlockJSON json, (
        (Vector3 a, Vector3 b, Vector3 c) a, 
        (Vector3 a, Vector3 b, Vector3 c) b, 
        (Vector3 a, Vector3 b, Vector3 c) c, 
        (Vector3 a, Vector3 b, Vector3 c) d) 
        data, int x, int y)
    {
        Vector3 A = (data.a.a.X, data.a.b.Y, data.a.c.Z);
        Vector3 B = (data.b.a.X, data.b.b.Y, data.b.c.Z);
        Vector3 C = (data.c.a.X, data.c.b.Y, data.c.c.Z);
        Vector3 D = (data.d.a.X, data.d.b.Y, data.d.c.Z);
        
        Vector2 uvA = (0, 0);
        Vector2 uvB = (0, 1);
        Vector2 uvC = (1, 1);
        Vector2 uvD = (1, 0);

        if (json.DefaultFace.Uv == "auto")
        {
            uvA = (A[x], A[y]);
            uvB = (B[x], B[y]);
            uvC = (C[x], C[y]);
            uvD = (D[x], D[y]);
        }
        
        return [uvA, uvB, uvC, uvD];
    }

    public static ulong GetBitMask(ref VoxelFace face)
    {
        var aabb_a = face.AABB_a;
        var aabb_c = face.AABB_c;

        Vector2i min = Mathf.Max(Mathf.FloorToInt(aabb_a.Min(aabb_c) * 8), (0, 0));
        Vector2i max = Mathf.Min(Mathf.CeilToInt(aabb_a.Max(aabb_c) * 8), (8, 8));

        Vector2i size = max - min;

        face.MergeX = size.X >= 8;
        face.MergeY = size.Y >= 8;

        if (size.X != 0 && size.Y != 0)
        {
            ulong mask = 0;
            ulong xMask = ((1u << size.X) - 1) << min.X;
            for (int y = min.Y; y < max.Y; y++)
            {
                mask |= xMask << (y * 8);
            }
            face.BitMask = mask;
        }

        return face.BitMask;
    }
}

public class VariantPlacementsJSON
{
    public VariantPlacementJSON? Front { get; set; } = null;
    public VariantPlacementJSON? Right { get; set; } = null;
    public VariantPlacementJSON? Top { get; set; } = null;
    public VariantPlacementJSON? Left { get; set; } = null;
    public VariantPlacementJSON? Bottom { get; set; } = null;
    public VariantPlacementJSON? Back { get; set; } = null;

    public void PopulatePlacements(BlockDefinition definition, int index)
    {
        Front?.PopulatePlacements(definition, 0, index);
    }
}

public class VariantPlacementJSON
{
    public string? Center { get; set; } = null;
    public string? Left { get; set; } = null;
    public string? Right { get; set; } = null;
    public string? Top { get; set; } = null;
    public string? Bottom { get; set; } = null;

    public void PopulatePlacements(BlockDefinition definition, int side, int index)
    {
        Populate(definition, Center, side, 0, index);
        Populate(definition, Left, side, 1, index);
        Populate(definition, Top, side, 2, index);
        Populate(definition, Right, side, 3, index);
        Populate(definition, Bottom, side, 4, index);
    }

    private void Populate(BlockDefinition definition, string? regionText, int side, int region, int index)
    {
        if (regionText == null)
            return;

        if (regionText == "any")
        {
            definition.Placements?.Set(side, region, 0, index);
            definition.Placements?.Set(side, region, 1, index);
            definition.Placements?.Set(side, region, 2, index);
            definition.Placements?.Set(side, region, 3, index);
        }
        if (regionText == "front")
        {
            definition.Placements?.Set(side, region, 0, index);
        }
        if (regionText == "right")
        {
            definition.Placements?.Set(side, region, 1, index);
        }
        if (regionText == "back")
        {
            definition.Placements?.Set(side, region, 2, index);
        }
        if (regionText == "left")
        {
            definition.Placements?.Set(side, region, 3, index);
        }
    }
}

public class BoxJSON
{
    public float[] From { get; set; } = [];
    public float[] To { get; set; } = [];
    public string[] DisabledFaces { get; set; } = [];
    public FacesJson? Faces { get; set; } = null;

    public Box GetBox()
    {
        Box box = new();

        for (int i = 0; i < 3.Min(From.Length); i++)
            box.From[i] = From[i];
        
        for (int i = 0; i < 3.Min(To.Length); i++)
            box.To[i] = To[i];

        return box;
    }

    public Faces GetFaces()
    {
        Faces faces = new(DisabledFaces);
        return faces;
    }
}

public struct Box
{
    public Vector3 From = Vector3.Zero;
    public Vector3 To = Vector3.One;

    public Box() {}

    public Collider GetCollider() => new(From, To);

    public Box RotatePositiveX()
    {
        Box box = new()
        {
            From = (From.X, 1 - To.Z, From.Y),
            To = (To.X, 1 - From.Z, To.Y)
        };
        return box;
    }

    public Box RotateNegativeX()
    {
        Box box = new()
        {
            From = (From.X, From.Z, 1 - To.Y),
            To = (To.X, To.Z, 1 - From.Y)
        };
        return box;
    }

    public Box RotatePositiveY()
    {
        Box box = new()
        {
            From = (1 - To.Z, From.Y, From.X),
            To = (1 - From.Z, To.Y, To.X)
        };
        return box;
    }

    public Box RotateNegativeY()
    {
        Box box = new()
        {
            From = (From.Z, From.Y, 1 - To.X),
            To = (To.Z, To.Y, 1 - From.X)
        };
        return box;
    }

    public Box RotatePositiveZ()
    {
        Box box = new()
        {
            From = (1 - To.Y, From.X, From.Z),
            To = (1 - From.Y, To.X, To.Z)
        };
        return box;
    }

    public Box RotateNegativeZ()
    {
        Box box = new()
        {
            From = (From.Y, 1 - To.X, To.Z),
            To = (To.Y, 1 - From.X, From.Z)
        };
        return box;
    }

    public Box Flip()
    {
        Box box = new()
        {
            From = (From.X, 1 - To.Y, From.Z),
            To = (To.X, 1 - From.Y, To.Z)
        };
        return box;
    }
}

public struct Faces(string[] faces)
{
    public string[] faces = faces;

    public bool Contains(string face) => faces.Contains(face);

    public Faces RotatePositiveX()
    {
        Faces rotatedFaces = new(faces);
        for (int i = 0; i < faces.Length; i++)
        {
            rotatedFaces.faces[i] = faces[i] switch
            {
                "front" => "bottom",
                "right" => "right",
                "top" => "front",
                "left" => "left",
                "bottom" => "back",
                "back" => "top",
                _ => throw new ArgumentException($"[Error] : {faces[i]} is not a valid face")
            };
        }
        return rotatedFaces;
    }

    public Faces RotateNegativeX()
    {
        Faces rotatedFaces = new(faces);
        for (int i = 0; i < faces.Length; i++)
        {
            rotatedFaces.faces[i] = faces[i] switch
            {
                "front" => "top",
                "right" => "right",
                "top" => "back",
                "left" => "left",
                "bottom" => "front",
                "back" => "bottom",
                _ => throw new ArgumentException($"[Error] : {faces[i]} is not a valid face")
            };
        }
        return rotatedFaces;
    }

    public Faces RotatePositiveY()
    {
        Faces rotatedFaces = new(faces);
        for (int i = 0; i < faces.Length; i++)
        {
            rotatedFaces.faces[i] = faces[i] switch
            {
                "front" => "right",
                "right" => "back",
                "top" => "top",
                "left" => "front",
                "bottom" => "bottom",
                "back" => "left",
                _ => throw new ArgumentException($"[Error] : {faces[i]} is not a valid face")
            };
        }
        return rotatedFaces;
    }

    public Faces RotateNegativeY()
    {
        Faces rotatedFaces = new(faces);
        for (int i = 0; i < faces.Length; i++)
        {
            rotatedFaces.faces[i] = faces[i] switch
            {
                "front" => "left",
                "right" => "front",
                "top" => "top",
                "left" => "back",
                "bottom" => "bottom",
                "back" => "right",
                _ => throw new ArgumentException($"[Error] : {faces[i]} is not a valid face")
            };
        }
        return rotatedFaces;
    }

    public Faces RotatePositiveZ()
    {
        Faces rotatedFaces = new(faces);
        for (int i = 0; i < faces.Length; i++)
        {
            rotatedFaces.faces[i] = faces[i] switch
            {
                "front" => "front",
                "right" => "top",
                "top" => "left",
                "left" => "bottom",
                "bottom" => "right",
                "back" => "back",
                _ => throw new ArgumentException($"[Error] : {faces[i]} is not a valid face")
            };
        }
        return rotatedFaces;
    }

    public Faces RotateNegativeZ()
    {
        Faces rotatedFaces = new(faces);
        for (int i = 0; i < faces.Length; i++)
        {
            rotatedFaces.faces[i] = faces[i] switch
            {
                "front" => "front",
                "right" => "bottom",
                "top" => "right",
                "left" => "top",
                "bottom" => "left",
                "back" => "back",
                _ => throw new ArgumentException($"[Error] : {faces[i]} is not a valid face")
            };
        }
        return rotatedFaces;
    }

    public Faces Flip()
    {
        Faces rotatedFaces = new(faces);
        for (int i = 0; i < faces.Length; i++)
        {
            rotatedFaces.faces[i] = faces[i] switch
            {
                "front" => "front",
                "right" => "right",
                "top" => "bottom",
                "left" => "left",
                "bottom" => "top",
                "back" => "back",
                _ => throw new ArgumentException($"[Error] : {faces[i]} is not a valid face")
            };
        }
        return rotatedFaces;
    }
}

public class FaceDefaultsJson
{
    public int Texture { get; set; } = 0;
    public string Uv { get; set; } = "base";
    public int UvRotation { get; set; } = 0;
}

public class FacesJson
{
    public FaceOverrideJson? Front { get; set; } = null;
    public FaceOverrideJson? Right { get; set; } = null;
    public FaceOverrideJson? Top { get; set; } = null;
    public FaceOverrideJson? Left { get; set; } = null;
    public FaceOverrideJson? Bottom { get; set; } = null;
    public FaceOverrideJson? Back { get; set; } = null;

    public FaceOverrideJson? GetFace(int side) => side switch
    {
        0     => Front,
        1     => Right,
        2       => Top,
        3      => Left,
        4    => Bottom,
        5      => Back,
        _           => Front,
    };
}

public class FaceOverrideJson
{
    public int Texture { get; set; } = 0;
    public int[] Uv { get; set; } = [];
    public int UvRotation { get; set; } = 0;
}