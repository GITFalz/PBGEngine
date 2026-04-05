using PBG.Core;
using PBG.Graphics;
using PBG.Graphics.Vulkan;
using PBG.Mathematics;

namespace PBG.Rendering;

public class Viewport
{
    int _left = 0;
    int _right = 0;
    int _bottom = 0;
    int _top = 0;

    int _width = VoxelEngine.Width;
    int _height = VoxelEngine.Height;

    public Matrix4 ProjectionMatrix;
    public Camera Camera;

    public Viewport(Camera camera) { Camera = camera; }
    public Viewport(Camera camera, int left, int right, int bottom, int top) { Camera = camera; SetViewport(left, right, bottom, top); }

    public void SetViewport(int left, int right, int bottom, int top)
    {
        _left = left; _right = right; _bottom = bottom; _top = top;
        _width = VoxelEngine.Width - (_left + _right);
        _height = VoxelEngine.Height - (_bottom + _top);
        ProjectionMatrix = Matrix4.CreatePerspective(
            Mathf.DegToRad(70), //Camera.FOV),
            (float)_width / (float)_height,
            0.1f,
            10000f
        );
    }

    public void ApplyViewport()
    {
        Camera.Viewport(_left, _right, _bottom, _top);
        GFX.Viewport(_left, _top, _width, _height);
    }

    void Resize()
    {
        _width = VoxelEngine.Width - (_left + _right);
        _height = VoxelEngine.Height - (_bottom + _top);
        ProjectionMatrix = Matrix4.CreatePerspective(
            Mathf.DegToRad(70), //Camera.FOV),
            (float)_width / (float)_height,
            0.1f,
            10000f
        );
    }

    void Render() => ApplyViewport();
}