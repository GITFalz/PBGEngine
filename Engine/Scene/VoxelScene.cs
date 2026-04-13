using PBG.Data;
using PBG.Rendering;
using PBG.UI;
using PBG.Voxel;

namespace PBG.Core;

public class Scene : Game
{
    public Camera Camera = new();

    public VoxelRenderer VoxelRenderer;

    public Scene() : base("Main")
    {
        VoxelRenderer = new(this);
        Camera.SetCameraMode(CameraMode.Free);
    }

    public override List<ScriptingNode> Initialize()
    {
        return [
            Camera,
            VoxelRenderer
        ];
    }
}