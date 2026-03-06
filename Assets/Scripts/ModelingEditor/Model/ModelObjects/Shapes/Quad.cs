public class Quad : Shape
{
    /*
    public string ID = "ID";

    public Vertex A;
    public Vertex B;
    public Vertex C;
    public Vertex D;

    public Vector2 UvA;
    public Vector2 UvB;
    public Vector2 UvC;
    public Vector2 UvD;

    public Edge AB;
    public Edge BC;
    public Edge CD;
    public Edge DA;

    public Vector3 NormalABC;
    public Vector3 NormalCDA;
    public Vector3 Center = Vector3.Zero;

    public bool Inverted = false;

    public Quad(Vertex a, Vertex b, Vertex c, Vector2 uvA, Vector2 uvB, Vector2 uvC, Edge ab, Edge bc, Edge ca)
    {
        A = a;
        B = b;
        C = c;

        UvA = uvA;
        UvB = uvB;
        UvC = uvC;

        AB = ab;
        BC = bc;
        CA = ca;

        A.AddParentTriangle(this);
        B.AddParentTriangle(this);
        C.AddParentTriangle(this);

        AB.AddParentTriangle(this);
        BC.AddParentTriangle(this);
        CA.AddParentTriangle(this);

        UpdateNormal();
    }

    public override void RefreshValues(List<Vector3> transformedVerts, List<Vector2> uvs, List<Vector2i> textureIndices, List<Vector3> normals)
    {
        transformedVerts.AddRange(GetVerticesPosition());
        uvs.AddRange(GetUvs());
        textureIndices.AddRange([(0, 1), (0, 1), (0, 1)]);
        normals.AddRange(Normal, Normal, Normal);
    }
    */
}