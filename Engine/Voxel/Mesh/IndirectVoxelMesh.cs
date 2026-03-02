using PBG.Core;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;

namespace PBG.Voxel;

public class IndirectVoxelMesh
{
    //private static VAO _vao = new();
    private SSBO<Vector4i>? sSBO = null;
    public List<Vector4i> VertexData = [];
    private int _vertexCount = 0;

    public IndirectVoxelMesh()
    {

    }

    public void GenerateMesh()
    {
        /*
        if (sSBO == null)
            sSBO = new(VertexData);
        else
            sSBO.Renew(VertexData);

        _vertexCount = VertexData.Count * 6;
        VertexData = [];
        */
    }

    public bool HasVertices() => _vertexCount > 0;

    public void Render()
    {
        /*
        if (sSBO == null) return;

        BlockData.FaceGeometrySSBO.Bind(0);
        sSBO.Bind(1);
        _vao.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);

        _vao.Unbind();
        sSBO.Unbind();
        BlockData.FaceGeometrySSBO.Unbind();
        */
    }

    public void Dispose()
    {
        //sSBO?.DeleteBuffer();
    }
}