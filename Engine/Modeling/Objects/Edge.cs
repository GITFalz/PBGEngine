using PBG.MathLibrary;

namespace PBG.Modeling;

public struct PBG_Edge
{
    public int VA;
    public int VB;
    public int Index;
    
    public PBG_Edge() {}
    public PBG_Edge(Vector2i v) 
    { 
        VA = v.X; 
        VB = v.Y; 
    }
    public PBG_Edge(int va, int vb) 
    { 
        VA = va; 
        VB = vb; 
    }
}