using System.Reflection;
using PBG.MathLibrary;
using Silk.NET.Input;

namespace PBG.Graphics;

public abstract class GameWindow
{
    public static GameWindow Instance { get; private set; } = null!;

    public static int Width = 0;
    public static int Height = 0;

    private VulkanInstance VulkanInstance;
    public static PBG.Data.CursorMode CursorMode
    {
        get => Instance.VulkanInstance.CursorMode;
        set => Instance.VulkanInstance.CursorMode = value;
    }

    public GameWindow(int width, int height)
    {
        VulkanInstance = new VulkanInstance(this, width, height);
        Instance = this;
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
    public abstract void OnCompute();
    public abstract void OnRender();
    public abstract void OnUnload();
    public void Run()
    {
        VulkanInstance.Run();
    }

    public static GameWindow New(int width, int height)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets.dll");
        var assembly = Assembly.LoadFrom(path);
        var gameWindowType = assembly.GetTypes().FirstOrDefault(t => t.IsSubclassOf(typeof(GameWindow)) && !t.IsAbstract);
        if (gameWindowType != null)
        {
            var constructor = gameWindowType.GetConstructor(new Type[] { typeof(int), typeof(int) });
            if (constructor != null)
            {
                var gameWindow = (GameWindow)constructor.Invoke(new object[] { width, height });
                return gameWindow;
            }
        }
        throw new Exception("No GameWindow subclass found in Assets.dll");
    }
}