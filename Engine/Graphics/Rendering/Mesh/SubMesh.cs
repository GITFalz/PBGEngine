using System.Runtime.InteropServices;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;

namespace PBG.Rendering;

public unsafe class SubMesh(Mesh Mesh, SubMeshInfo info) : IDisposable
{
    public Material Material = Material.DefaultMaterial;
    
    private IBO _indexBuffer = new([]);
    private uint _indexCount = 0;
    
    public void Generate()
    {
        uint[] indices = new uint[info.Count];

        uint start = info.Start.Min(((uint)Mesh.Indices.Length - 1).Max(0));
        uint count = info.Count.Min(((uint)(Mesh.Indices.Length - start)).Max(0));

        int j = 0;
        for (uint i = start; i < start + count; i++)
            indices[j++] = Mesh.Indices[i];

        _indexBuffer.Renew(indices);
        _indexCount = (uint)indices.Length;
    }

    public void Render(MeshRenderer meshRenderer)
    {
        Material.Bind();
        meshRenderer.Descriptor.Bind();

        meshRenderer.Descriptor.Uniform(Material.ModelLocation, meshRenderer.Transform.GetModelMatrix());
        meshRenderer.Descriptor.Uniform(Material.ViewLocation, meshRenderer.Camera.ViewMatrix);
        meshRenderer.Descriptor.Uniform(Material.ProjectionLocation, meshRenderer.Camera.ProjectionMatrix);

        _indexBuffer.Bind();

        GFX.DrawIndexed(_indexCount, 1, 0, 0, 0);
    }

    public void Dispose()
    {
        _indexBuffer.Dispose();
    }
}

public struct SubMeshInfo
{
    public uint Start;
    public uint Count;
}