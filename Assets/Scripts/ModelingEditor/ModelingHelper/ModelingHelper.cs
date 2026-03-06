using PBG.MathLibrary;

public static class ModelingHelper
{
    public static bool Generate_2_Selected(List<Vertex> SelectedVertices)
    {
        Vertex A = SelectedVertices[0];
        Vertex B = SelectedVertices[1];

        Edge? sharedEdge = A.GetEdgeWith(B);
        if (sharedEdge == null)
            return false;
        
        /*
        Vertex? singleVertexA = null;
        Vertex? singleVertexB = null;

        foreach (var edge in A.ParentEdges)
        {
            if (edge == sharedEdge)
                continue;

            if (edge.Not(A).ParentTriangles.Count == 0)
            {
                singleVertexA = edge.Not(A);
                break;
            }
        }

        if (singleVertexA == null)
            return false;

        foreach (var edge in B.ParentEdges)
        {
            if (edge == sharedEdge)
                continue;

            if (edge.Not(B).ParentTriangles.Count == 0)
            {
                singleVertexB = edge.Not(B);
                break;
            }
        }

        if (singleVertexB == null)
            return false;
        */

        List<Edge> edgesA = [.. A.ParentEdges];
        List<Edge> edgesB = [.. B.ParentEdges];

        edgesA.Remove(sharedEdge);
        edgesB.Remove(sharedEdge);

        var pair = FindMostParallelPair(edgesA, edgesB, A, B);
        if (pair == null)
            return false;

        SelectedVertices.AddRange(pair.Value.Item1.Not(A), pair.Value.Item2.Not(B));
        return true;
    }

    public static (Edge, Edge)? FindMostParallelPair(List<Edge> edges1, List<Edge> edges2, Vertex A, Vertex B)
    {
        float bestParallelism = -1f; 
        (Edge, Edge)? bestPair = null;

        for (int i = 0; i < edges1.Count; i++)
        {
            for (int j = 0; j < edges2.Count; j++)
            {
                Edge e1 = edges1[i];
                Edge e2 = edges2[j];

                if (e1.Not(A).ShareEdgeWith(B) || e2.Not(B).ShareEdgeWith(A) || e1.Not(A) == e2.Not(B) || e1.Not(A).ShareEdgeWith(e2.Not(B)))
                    continue;

                Vector3 edge1 = e1.GetDirectionFrom(A).Normalized();
                Vector3 edge2 = e2.GetDirectionFrom(B).Normalized();

                float dot = Vector3.Dot(edge1, edge2);

                if (dot > bestParallelism)
                {
                    bestParallelism = dot;
                    bestPair = (e1, e2);
                }
            }
        }

        return bestPair;
    }

    public static void Generate_3_Selected(List<Vertex> SelectedVertices, ModelMesh Mesh)
    {
        Vertex A = SelectedVertices[0];
        Vertex B = SelectedVertices[1];
        Vertex C = SelectedVertices[2];

        if (A.ShareTriangle(B, C))
            return;

        Console.WriteLine("Generating new face");
        
        Edge AB;
        Edge BC;
        Edge CA;

        Edge? b = A.GetEdgeWith(B);
        if (b == null)
        {
            AB = new Edge(A, B);
            Mesh.EdgeList.Add(AB);
        }
        else
        {
            AB = b;
        }

        Edge? c = B.GetEdgeWith(C);
        if (c == null)
        {
            BC = new Edge(B, C);
            Mesh.EdgeList.Add(BC);
        }
        else
        {
            BC = c;
        }

        Edge? a = C.GetEdgeWith(A);
        if (a == null)
        {
            CA = new Edge(C, A);
            Mesh.EdgeList.Add(CA);
        }
        else
        {
            CA = a;
        }

        Triangle newTriangle = new Triangle(A, B, C, AB, BC, CA);
        newTriangle.InvertIfInverted();

        Mesh.AddTriangle(newTriangle);
    }

    public static void Generate_4_Selected(Model model, List<Vertex> Vs, ModelMesh Mesh)
    {
        Vertex A = Vs[0];
        Vertex B = Vs[1];
        Vertex C = Vs[2];
        Vertex D = Vs[3];
        
        float bD = Vector3.Distance(A, B);
        float cD = Vector3.Distance(A, C);
        float dD = Vector3.Distance(A, D);
        
        if (bD > cD && bD > dD) // if B is furthest from A
        {
            (B, D) = (D, B); // B is pushed last
        }
        else if (cD > bD && cD > dD) // if C is furthest from A
        {
            (C, D) = (D, C); // C is pushed last
        }
        // else D is already the furthest from A

        if (Vector3.Dot(A - B, A - D) >= 0) // A is on the other side of the edge BC, then ABC and ACD would overlap
        {
            (A, B) = (B, A);
        }

        Edge? AB = A.GetEdgeWith(B);
        Edge? BC = B.GetEdgeWith(C);
        Edge? CD = C.GetEdgeWith(D);
        Edge? DA = D.GetEdgeWith(A);
        Edge? AC = A.GetEdgeWith(C);

        if (AB == null)
        {
            AB = new Edge(A, B);
            Mesh.EdgeList.Add(AB);
        }
        if (BC == null)
        {
            BC = new Edge(B, C);
            Mesh.EdgeList.Add(BC);
        }
        if (CD == null)
        {
            CD = new Edge(C, D);
            Mesh.EdgeList.Add(CD);
        }
        if (DA == null)
        {
            DA = new Edge(D, A);
            Mesh.EdgeList.Add(DA);
        }
        if (AC == null)
        {
            AC = new Edge(A, C);
            Mesh.EdgeList.Add(AC);
        }

        Triangle triangle1 = new Triangle(A, B, C, AB, BC, AC);
        Triangle triangle2 = new Triangle(A, C, D, AC, CD, DA);

        Mesh.AddTriangle(triangle1);
        Mesh.AddTriangle(triangle2);

        triangle1.InvertIfInverted();
        triangle2.InvertIfInverted();

        triangle1.UpdateNormal();
        triangle2.UpdateNormal();
    }
}