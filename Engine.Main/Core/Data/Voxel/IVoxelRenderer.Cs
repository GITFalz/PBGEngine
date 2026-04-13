using System.Diagnostics.CodeAnalysis;
using PBG.Graphics;
using PBG.Mathematics;
using PBG.Rendering;

namespace PBG.Voxel;

public interface IVoxelRenderer
{
    public void UpdateUniforms(Descriptor descriptor);
    public Camera GetCamera();
    public Block GetBlock(Vector3i position);
    public bool GetBlock(Vector3i position, [NotNullWhen(true)] out Block? block);
}