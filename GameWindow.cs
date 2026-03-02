using PBG.Graphics;
using PBG.MathLibrary;
using Silk.NET.Input;

public abstract class GameWindow
{
    private VulkanInstance instance;

    public IMouse Mouse;
    public IKeyboard Keyboard;
    public CursorMode CursorMode
    {
        get => Mouse.Cursor.CursorMode;
        set => Mouse.Cursor.CursorMode = value;
    }

    public GameWindow(int width, int height)
    {
        instance = new VulkanInstance(this, width, height);
    }

    public abstract void OnKeyDown(IKeyboard keyboard, Key key, int scanCode);
    public abstract void OnKeyUp(IKeyboard keyboard, Key key, int scanCode);
    public abstract void OnKeyChar(IKeyboard keyboard, char c);
    public abstract void OnMouseMove(IMouse mouse, Vector2 position);
    public abstract void OnMouseDown(IMouse mouse, MouseButton button);
    public abstract void OnMouseUp(IMouse mouse, MouseButton button);
    public abstract void OnScroll(IMouse mouse, ScrollWheel scroll);
    public abstract void OnLoad();
    public abstract void OnResize(int width, int height);
    public abstract void OnUpdate(double delta);
    public abstract void OnRender();
    public abstract void OnUnload();
    public void Run()
    {
        instance.Run();
    }
}