
using PBG.MathLibrary;

namespace PBG.Modeling;

public struct PBG_Triangle
{
    public int VA;
    public int VB;
    public int VC;

    public int EAB;
    public int EBC;
    public int ECA;

    public Vector2 UvA;
    public Vector2 UvB;
    public Vector2 UvC;

    public Vector3 NA;
    public Vector3 NB;
    public Vector3 NC;

    public int Index;
    
    public int MIA;
    public int MIB;
    public int MIC;

    public readonly bool HasVertex(PBG_Vertex vertex) => VA == vertex.Index || VB == vertex.Index || VC == vertex.Index;

    public void UpdateNormal(PBG_Model model)
    {
        var a = model.VertexList[VA].Position;
        var b = model.VertexList[VB].Position;
        var c = model.VertexList[VC].Position;
    }

    public static Vector3 CalculateNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 edge1 = b - a;
        Vector3 edge2 = c - a;
        return Vector3.Cross(edge1, edge2).Normalized();
    }
}