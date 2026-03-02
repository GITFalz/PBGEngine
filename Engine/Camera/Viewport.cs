using PBG.MathLibrary;

namespace PBG.Rendering;

public class Viewport//
{
    int _left = 0;
    int _right = 0;
    int _bottom = 0;
    int _top = 0;

    int _width = Game.Width;
    int _height = Game.Height;

    public Matrix4 ProjectionMatrix;

    public Viewport() {}
    public Viewport(int left, int right, int bottom, int top) => SetViewport(left, right, bottom, top);

    public void SetViewport(int left, int right, int bottom, int top)
    {
        _left = left; _right = right; _bottom = bottom; _top = top;
        _width = Game.Width - (_left + _right);
        _height = Game.Height - (_bottom + _top);
        ProjectionMatrix = Matrix4.CreatePerspective(
            Mathf.DegToRad(70), //Camera.FOV),
            (float)_width / (float)_height,
            0.1f,
            10000f
        );
    }

    public void ApplyViewport()
    {
        //Camera.Viewport(_left, _right, _bottom, _top);
        //GL.Viewport(_left, _bottom, _width, _height);
    }

    void Resize()
    {
        _width = Game.Width - (_left + _right);
        _height = Game.Height - (_bottom + _top);
        ProjectionMatrix = Matrix4.CreatePerspective(
            Mathf.DegToRad(70), //Camera.FOV),
            (float)_width / (float)_height,
            0.1f,
            10000f
        );
    }

    void Render() => ApplyViewport();
}