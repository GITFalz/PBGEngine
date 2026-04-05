using PBG.Rendering;
using PBG.UI;
using PBG.Voxel;

namespace PBG.Core;

public class Scene
{
    public Camera Camera = new();

    public List<UIController> ActiveUIControllers = [];
    public VoxelRenderer VoxelRenderer;

    public Scene()
    {
        VoxelRenderer = new(this);
    }

    public void Start()
    {
        VoxelRenderer.Test();
    }

    public void Render()
    {
        VoxelRenderer.Render();
    }

    public void Dispose()
    {
        VoxelRenderer.Dispose();
    }
}