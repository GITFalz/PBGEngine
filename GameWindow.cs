using PBG.Graphics;
using PBG.MathLibrary;
using Silk.NET.Input;


public abstract class GameWindow
{
    private VulkanInstance instance;

    public IMouse Mouse;
    public IKeyboard Keyboard;
    private PBG.Data.CursorMode _cursorMode;
    public PBG.Data.CursorMode CursorMode
    {
        get => _cursorMode;
        set
        {
            if (_cursorMode == value)
                return;

            _cursorMode = value;
            Mouse.Cursor.CursorMode = (CursorMode)value;
        }
    }
    
    public static GameExecutionMode ExecutionMode = GameExecutionMode.Running;

    public GameWindow(int width, int height)
    {
        instance = new VulkanInstance(this, width, height);
    }

    public void SetMouse(IMouse mouse)
    {
        Mouse = mouse;
        _cursorMode = (PBG.Data.CursorMode)mouse.Cursor.CursorMode;
    }

    public abstract void OnKeyDown(IKeyboard keyboard, Key key, int scanCode);
    public abstract void OnKeyUp(IKeyboard keyboard, Key key, int scanCode);
    public abstract void OnKeyChar(IKeyboard keyboard, char c);
    public abstract void OnMouseMove(IMouse mouse, Vector2 position);
    public abstract void OnMouseDown(IMouse mouse, MouseButton button);
    public abstract void OnMouseUp(IMouse mouse, MouseButton button);
    public abstract void OnScroll(IMouse mouse, ScrollWheel scroll);
    public abstract void OnLoad();
    public virtual void OnRenderLoad() {}
    public abstract void OnResize(int width, int height);
    public abstract void OnUpdate(double delta);
    public abstract void OnRender();
    public abstract void OnUnload();
    public void Run()
    {
        instance.Run();
    }
}

public enum GameExecutionMode
{
    Running,
    Paused
}