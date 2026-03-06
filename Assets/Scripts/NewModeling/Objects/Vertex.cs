
using PBG.MathLibrary;

namespace PBG.Modeling;

public struct PBG_Vertex
{
    public Vector3 Position;
    public int Index;

    public PBG_Vertex() {}
    public PBG_Vertex(Vector3 position) { Position = position; }

    public static bool operator ==(PBG_Vertex a, PBG_Vertex b) => a.Position == b.Position;
    public static bool operator !=(PBG_Vertex a, PBG_Vertex b) => a.Position != b.Position;

    public override bool Equals(object? obj) => obj is PBG_Vertex v && v == this;
    public override int GetHashCode() => Position.GetHashCode();
}