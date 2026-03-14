using PBG.Core;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;

namespace PBG.Voxel;

public class IndirectVoxelMesh
{
    private Descriptor _descriptor;
    private SSBO<Vector4i>? sSBO = null;
    public Vector4i[] VertexData = [];
    private int _vertexCount = 0;
    private VoxelChunk _chunk;

    public IndirectVoxelMesh(VoxelChunk chunk)
    {
        _chunk = chunk;
    }

    public void GenerateMesh()
    {
        
        
        _vertexCount = VertexData.Length * 6;
        VertexData = [];
    }

    public void Render()
    {
        if (sSBO == null) 
            return;

    }

    public void Dispose()
    {
        _descriptor?.Dispose();
        sSBO?.Dispose();
    }
}