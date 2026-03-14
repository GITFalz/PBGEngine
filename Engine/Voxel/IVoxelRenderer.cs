using PBG.Graphics;
using PBG.Rendering;

namespace PBG.Voxel;

public interface IVoxelRenderer
{
    public void UpdateUniforms(Descriptor descriptor);
    public Camera GetCamera();
}