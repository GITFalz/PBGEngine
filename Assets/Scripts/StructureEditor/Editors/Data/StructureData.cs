using System.Text;
using PBG.MathLibrary;
using PBG.Compiler.Lines;
using PBG.Hash;
using PBG.MathLibrary;
using PBG.UI;
using PBG.Voxel;

public struct BoundingBoxData
{
    public Vector3 Position;
    private readonly float p1;
    public Vector3 Size;
    private readonly float p2;
    public Vector4 Color;
}

public class StructureBoundingBox(Vector3i size, Vector3i position)
{
    public Vector3i Size = size;
    public Vector3i Position = position;
    public UIElementBase Element = null!;
}

public class StructureExtender(Vector3i size, Vector3i position)
{
    public Vector3i Size = (size.X, 0, size.Z);
    public Vector3i Position = position;
    public UIElementBase Element = null!;
}

public class StructureData
{
    public string Name = "Base";
    public bool Core = false;

    public PBG.UI.UIElementBase Element = null!;
    public Vector3i Size;
    public Vector3i SavePosition;
    public Vector3 Center => new Vector3(Size) * 0.5f;

    public List<StructureBoundingBox> BoundingBoxes = [];
    public List<StructureExtender> Extenders = [];
    public Dictionary<string, ConnectionPoint> ConnectionPoints = [];
    public Dictionary<string, RulesetPoint> RulesetPoints = [];
    public Block[] Blocks = [];
    public List<string> Lines = [];
    public Executor Executor = new();

    public void Run() => Executor.Run();

    public override string ToString()
    {
        string data = $"[BoundingBox] : SavePosition: {SavePosition}, Size: {Size}, Connections: {ConnectionPoints.Count}";
        foreach (var (_, connection) in ConnectionPoints)
        {
            data += "\n"+connection;
        }
        return data;
    }

    public StructureModule GetModule(StructureBoundingBoxPlacement placement)
    {
        StructureModule module = new(placement);
        foreach (var (_, connection) in ConnectionPoints)
        {
            module.Connections.Add(new(connection));
        }
        return module;
    }
}

public class ConnectionPoint(Vector3 connection, int yRotation, int side)
{
    public Vector3 Position = connection;
    public int Yrotation = yRotation;
    public int Side = side;
    public List<string> Categories = [];
    public List<string> Avoid = [];
    public HashSet<string> HashedCategories = []; // allows for faster lookup, computed when generating structure
    public HashSet<string> HashedAvoid = []; // allows for faster lookup, computed when generating structure
    public UIElementBase Element = null!;
    public bool Connected = false;

    public Value GetConnected() => new FunctionValue() { Action = () => new BoolValue(Connected) };

    public void Run(bool connected)
    {
        Connected = connected;
    }

    public bool HasAvoidCategory(ConnectionPoint point) => HashedCategories.Overlaps(point.HashedAvoid) || point.HashedCategories.Overlaps(HashedAvoid);

    public override string ToString()
    {
        return $"[Connection] : Position: {Position}, Rotation: {Yrotation}, Side: {Side}";
    }
}

public class RulesetPoint(Vector3 position)
{
    public Vector3 Position = position;
    public UIElementBase Element = null!;

    public int Height = 0;
    public int X = 0;
    public int Y = 0;
    public int Z = 0;

    public Value GetHeight() => new FunctionValue() { Action = () => new IntValue(Height) };
    public Value GetX() => new FunctionValue() { Action = () => new IntValue(X) };
    public Value GetY() => new FunctionValue() { Action = () => new IntValue(Y) };
    public Value GetZ() => new FunctionValue() { Action = () => new IntValue(Z) };

    public void Run(BaseStructureGenerator generator, int x, int y, int z)
    {
        var result = generator.GetHeight(x, z); 
        X = x; Y = y; Z = z;
        Height = result;
    }
}

public class StructureBoundingBoxPlacement
{
    public bool Occupied;
    public int Depth = 0;
    public StructureData Data = null!;
    public Vector3 Position;
    public Vector3i RotatedPosition;
    public Vector3i RotatedSize;
    public int LowestY = int.MaxValue;
    public int Yrotation; // in steps 0 is 0 degrees, 1 is 90 degrees, 2 is 180 degrees and 3 is 270 degrees
    public List<(Vector3i position, Vector3i size, Vector3 color)> boundingBoxes = [];
    public List<ExtenderData> ExtenderDatas = [];

    public Vector3 Center => Position + (new Vector3(Data.Size) * 0.5f);

    public void SetRotation(BaseStructureGenerator generator, int rot)
    {
        Yrotation = rot;
        boundingBoxes = [];
        ExtenderDatas = [];
        LowestY = int.MaxValue;
        var degrees = rot * 90f;

        {
            var rotatedPosition = Mathf.RotatePoint(Position, Center, (0, 1, 0), rot * 90f);
            var rotatedSize = Mathf.RotatePoint(Data.Size, (0, 0, 0), (0, 1, 0), rot * 90f);

            if (rotatedSize.X < 0)
            {
                rotatedSize.X *= -1;
                rotatedPosition.X -= rotatedSize.X;
            }

            if (rotatedSize.Y < 0)
            {
                rotatedSize.Y *= -1;
                rotatedPosition.Y -= rotatedSize.Y;
            }

            if (rotatedSize.Z < 0)
            {
                rotatedSize.Z *= -1;
                rotatedPosition.Z -= rotatedSize.Z;
            }
            
            RotatedPosition = Mathf.RoundToInt(rotatedPosition);
            RotatedSize = Mathf.RoundToInt(rotatedSize);
        }

        for (int i = 0; i < Data.BoundingBoxes.Count; i++)
        {
            var boundingBox = Data.BoundingBoxes[i];

            var rotatedPosition = Mathf.RoundToInt(Mathf.RotatePoint(Position + boundingBox.Position - Data.SavePosition, Center, (0, 1, 0), degrees));
            var rotatedSize = Mathf.RoundToInt(Mathf.RotatePoint(boundingBox.Size, (0, 0, 0), (0, 1, 0), degrees));

            if (rotatedSize.X < 0)
            {
                rotatedSize.X *= -1;
                rotatedPosition.X -= rotatedSize.X;
            }

            if (rotatedSize.Y < 0)
            {
                rotatedSize.Y *= -1;
                rotatedPosition.Y -= rotatedSize.Y;
            }

            if (rotatedSize.Z < 0)
            {
                rotatedSize.Z *= -1;
                rotatedPosition.Z -= rotatedSize.Z;
            }

            boundingBoxes.Add((rotatedPosition, rotatedSize, (0, 1, 0)));
        }
        
        for (int i = 0; i < Data.Extenders.Count; i++)
        {
            var extender = Data.Extenders[i];

            var rotatedPosition = Mathf.RoundToInt(Mathf.RotatePoint(Position + extender.Position - Data.SavePosition, Center, (0, 1, 0), degrees));
            var rotatedSize = Mathf.RoundToInt(Mathf.RotatePoint(extender.Size, (0, 0, 0), (0, 1, 0), degrees));

            if (rotatedSize.X < 0)
            {
                rotatedSize.X *= -1;
                rotatedPosition.X -= rotatedSize.X;
            }

            if (rotatedSize.Y < 0)
            {
                rotatedSize.Y *= -1;
                rotatedPosition.Y -= rotatedSize.Y;
            }

            if (rotatedSize.Z < 0)
            {
                rotatedSize.Z *= -1;
                rotatedPosition.Z -= rotatedSize.Z;
            }

            var minHeight = rotatedPosition.Y.Max(0);

            int[] heights = new int[rotatedSize.X * rotatedSize.Z];

            for (int x = 0; x < rotatedSize.X; x++)
            {
                for (int z = 0; z < rotatedSize.Z; z++)
                {
                    var h = generator.GetHeight(x + rotatedPosition.X, z + rotatedPosition.Z);
                    minHeight = Mathf.Min(minHeight, h);
                    int index = z + x * rotatedSize.Z;
                    heights[index] = h;
                }
            }

            LowestY.MinSet(minHeight);
            var height = (rotatedPosition.Y - minHeight).Max(0);
            rotatedSize.Y = height;
            rotatedPosition.Y -= height;

            ExtenderData extenderData = new(rotatedPosition, rotatedSize, rotatedPosition.Y + rotatedSize.Y, heights);
            ExtenderDatas.Add(extenderData);

            boundingBoxes.Add((rotatedPosition, rotatedSize, (0, 0, 1)));
        }
    }

    public static bool operator &(StructureBoundingBoxPlacement a, StructureBoundingBoxPlacement b)
    {
        for (int i = 0; i < a.boundingBoxes.Count; i++)
        for (int j = 0; j < b.boundingBoxes.Count; j++)
        {
            if (Overlap(a.boundingBoxes[i], b.boundingBoxes[j]))
                return true;
        }
        return false; 
    }

    public static bool Overlap((Vector3i position, Vector3i size, Vector3 _) a, (Vector3i position, Vector3i size, Vector3 _) b)
    {
        var aMin = a.position;
        var aMax = a.position + a.size;

        var bMin = b.position;
        var bMax = b.position + b.size;

        bool xOverlap = aMin.X < bMax.X && aMax.X > bMin.X;
        bool yOverlap = aMin.Y < bMax.Y && aMax.Y > bMin.Y;
        bool zOverlap = aMin.Z < bMax.Z && aMax.Z > bMin.Z;

        return xOverlap && yOverlap && zOverlap;
    }

    public void InitConnectionPoints(StructureModule module)
    {
        foreach (var connection in module.Connections)
        {
            connection.Point.Run(connection.Module != null);
        }
    }

    public void InitRulesets(BaseStructureGenerator generator)
    {
        foreach (var (_, rule) in Data.RulesetPoints)
        {
            var rotated = Mathf.RotatePoint(rule.Position + new Vector3(0.5f), Data.Center, (0, 1, 0), Yrotation * 90f);
            Vector3i worldPos = Mathf.FloorToInt(Position + rotated);
            rule.Run(generator, worldPos.X, worldPos.Y, worldPos.Z);
        }
    }

    public void RunCode(BaseStructureGenerator generator, StructureModule module)
    {
        generator.ResetScore();
        InitConnectionPoints(module);
        InitRulesets(generator);
        Data.Run();
    }

    public override string ToString()
    {
        return $"SBBP {{ "
            + $"Pos={Position} | "
            + $"RotPos={RotatedPosition} | "
            + $"Size={RotatedSize} | "
            + $"Rot={Yrotation}×90° | "
            + $"Occupied={Occupied} | "
            + $"Depth={Depth} | "
            + (Data != null ? $"Data={Data.Name}" : "Data=null") 
            + " }}";
    }
}

public class ExtenderData(Vector3i position, Vector3i size, int top, int[] heights)
{
    public Vector3i Position = position;
    public Vector3i Size = size;
    public int Top = top;
    public int[] Heights = heights;
}


public class StructureModule
{
    public StructureBoundingBoxPlacement Placement;
    public List<ConnectionModule> Connections = [];

    public StructureModule(StructureBoundingBoxPlacement placement)
    {
        Placement = placement;
    }

    bool occupied(List<StructureBoundingBoxPlacement> placements, StructureBoundingBoxPlacement newPlacement)
    {
        for (int i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (placement == newPlacement)
                continue;

            if (placement & newPlacement)
                return true;
        }
        return false;
    }
    
    string Repeat(string s, int n)
    {
        if (n <= 0) return "";
        if (n == 1) return s;
        
        var sb = new StringBuilder(s.Length * n);
        for (int i = 0; i < n; i++)
            sb.Append(s);
        return sb.ToString();
    }
    public bool GenerateRandom(BaseStructureGenerator generator, List<StructureData> boundingBoxes, List<StructureBoundingBoxPlacement> placements, int maxChain, int seed)
    {
        if (generator.Debug)
            Console.WriteLine(Repeat("--", Placement.Depth) + "Generating: " + Placement + "\n");

        List<ConnectionModule> connections1_copy = [.. Connections];
        StructureEditor.Shuffle(connections1_copy, seed + Mathf.FloorToInt(111 * Hash.HashVector3(Placement.Position)));
        for (int i = 0; i < connections1_copy.Count; i++)
        {
            var c1_module = connections1_copy[i];
            if (c1_module.Module != null)
            {
                continue;
            }

            int y1 = (c1_module.Yrotation + Placement.Yrotation) % 4; 
            var center1 = Placement.Data.Center;
            if (Placement.Depth + 1 < maxChain)
            {
                if (generator.Debug)
                    Console.WriteLine(Repeat("_____", Placement.Depth) + $"-Testing for connection {i+1}");

                StructureBoundingBoxPlacement? bestPlacement = null;
                StructureModule bestModule = null!;
                ConnectionModule bestConnection = null!;
                float bestScore = 0;

                bool found = false;
                List<StructureData> boundingBoxes_copy = [.. boundingBoxes];
                StructureEditor.Shuffle(boundingBoxes_copy, seed + Mathf.FloorToInt((i + 1) * 222 * Hash.HashVector3(Placement.Position)));
                for (int j = 0; j < boundingBoxes_copy.Count; j++)
                {
                    var boundingBox = boundingBoxes_copy[j];
                    if (generator.Debug)
                        Console.WriteLine(Repeat("_____", Placement.Depth) + "--Trying bounding box " + boundingBox.Name);

                    StructureModule module = boundingBox.GetModule(new()
                    {
                        Data = boundingBox,
                        Depth = Placement.Depth + 1
                    });

                    List<ConnectionModule> connections2_copy = [.. module.Connections];
                    StructureEditor.Shuffle(connections2_copy, seed + Mathf.FloorToInt((j + 1) * 333 * Hash.HashVector3(Placement.Position)));

                    for (int k = 0; k < connections2_copy.Count; k++)
                    {
                        var c2_module = connections2_copy[k];
                        if (c2_module.Module != null)
                        {
                            continue;
                        }
                        if (generator.Debug)
                            Console.WriteLine(Repeat("_____", Placement.Depth) + "---Trying connection " + k+1);

                        if (c1_module.Point.HasAvoidCategory(c2_module.Point))
                            continue;

                        var newPlacement = new StructureBoundingBoxPlacement()
                        {
                            Data = boundingBox,
                            Depth = Placement.Depth + 1
                        };

                        int y2 = c2_module.Yrotation;
                        int minY = Mathf.Min(y1, y2);
                        int maxY = Mathf.Max(y1, y2);
                        int rot;
                        if (y1 == y2)
                            rot = 2;
                        else if ((minY == 0 && maxY == 2) || (minY == 1 && maxY == 3))
                            rot = 0;
                        else if (y1 + 1 == y2 || (y1 == 3 && y2 == 0))
                            rot = 1;
                        else
                            rot = 3;

                        Vector3 connection1 = Mathf.RotatePoint(c1_module.Position, center1, (0, 1, 0), Placement.Yrotation * 90f);
                        Vector3 connection2 = Mathf.RotatePoint(c2_module.Position, boundingBox.Center, (0, 1, 0), rot * 90f);
                        Vector3 direction = connection1 - connection2;
                        
                        newPlacement.Position = Placement.Position + direction;
                        newPlacement.SetRotation(generator, rot);

                        if (!occupied(placements, newPlacement))
                        {      
                            c1_module.Module = module;
                            c2_module.Module = this; // Set the connection before running the code to be able to test the connections

                            newPlacement.RunCode(generator, module);

                            c1_module.Module = null;
                            c2_module.Module = null;

                            var score = generator.GetScore();
                            if (score >= 1)
                            {
                                if (generator.Debug)
                                    Console.WriteLine(Repeat("_____", Placement.Depth) + "----[Sucess] : score is max, moving to next");
                                placements.Add(newPlacement);
                                c1_module.Module = module;
                                c2_module.Module = this;
                                module.Placement = newPlacement;

                                if (!module.GenerateRandom(generator, boundingBoxes_copy, placements, maxChain, seed))
                                    return false;

                                found = true;
                                break;
                            }
                            else if (score > 0 && score > bestScore)
                            {
                                if (generator.Debug)
                                    Console.WriteLine(Repeat("_____", Placement.Depth) + "----[Success] : score is new best: " + score);
                                bestPlacement = newPlacement;
                                bestModule = module;
                                bestConnection = c2_module;
                                bestScore = score;
                            }
                            else
                            {
                                if (generator.Debug)
                                    Console.WriteLine(Repeat("_____", Placement.Depth) + "----[Meh] : score is lacking: " + score);
                            }
                        }  
                        else
                        {
                            if (generator.Debug)
                                Console.WriteLine(Repeat("_____", Placement.Depth) + "----[Error] : is occupied");
                        }
                    }

                    if (found)
                    {
                        break;
                    }
                }
                
                if (!found && bestPlacement != null)
                {
                    placements.Add(bestPlacement);
                    c1_module.Module = bestModule;
                    bestConnection.Module = this;
                    bestModule.Placement = bestPlacement;

                    if (generator.Debug)
                        Console.WriteLine(Repeat("_____", Placement.Depth) + "----[Success] : best bounding box is " + bestPlacement.Data.Name + " with a score of " + bestScore + ", moving on to next");

                    bestModule.GenerateRandom(generator, boundingBoxes_copy, placements, maxChain, seed);
                }
            }
        }
        return true;
    } 
}

public class ConnectionModule(ConnectionPoint point)
{
    public ConnectionPoint Point = point;
    public Vector3 Position = point.Position;
    public int Yrotation = point.Yrotation;
    public int Side = point.Side;
    public StructureModule? Module = null;
}