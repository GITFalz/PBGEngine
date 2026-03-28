using System.Diagnostics;
using PBG.Core;
using PBG.Data;
using PBG.Editor;
using PBG.Files;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.Threads;
using PBG.UI;
using Silk.NET.Input;
namespace PBG;

public class Game : GameWindow
{
    public static Game Instance { get; private set; } = null!;

    public static int Width;
    public static int Height;

    public static PString MainPath = FileManager.CreatePath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".projectVoxel");
    public static PString AssetsPath = FileManager.CreatePath(MainPath, "assets");
    public static PString ShaderPath = FileManager.CreatePath(AssetsPath, "shaders");
    public static PString TexturePath = FileManager.CreatePath(AssetsPath, "textures");

    public static PString DataPath = FileManager.CreatePath(MainPath, "data");
    public static PString ModelPath = FileManager.CreatePath(DataPath, "models");
    public static PString UndoModelPath = FileManager.CreatePath(ModelPath, "undo");
    public static PString EditorRegistryPath = FileManager.CreatePath(DataPath, "registry");
    public static PString EditorPalettePath = FileManager.CreatePath(DataPath, "palette");

    public static PString CustomPath = FileManager.CreatePath(MainPath, "custom");
    public static PString CustomTempPath = FileManager.CreatePath(CustomPath, "temp");

    public static PString ProjectsPath = FileManager.CreatePath(MainPath, "Projects");
    public static PString CurrentProjectPath = FileManager.CreatePath(ProjectsPath, "project_1");

    double accumulator = 0.0;
    double accumulator2 = 0.0;

    private static double MaxFPS = 99999.0;
    private readonly double TargetFrameTime = 1.0 / MaxFPS;
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();

    private static double MaxRenderingFPS = 99999.0;
    private readonly double TargetRenderingFrameTime = 1.0 / MaxRenderingFPS;
    private readonly Stopwatch frameTimer = Stopwatch.StartNew();
    private double _renderingDeltaTime = 0;
    private double lastUpdateTime = 0.0;
    private double lastAccumulator2Update = 0;

    private bool shouldRender = false;

    public static bool ForceSyncedRendering = true;

    public static int Counter = 0;

    

    public Game(int width, int height) : base(width, height)
    {
        Instance = this;
        Width = width;
        Height = height;
        //GraphicsContext.graphicsContext.window.FramesPerSecond = 20;
    }

    public override void OnKeyDown(Silk.NET.Input.IKeyboard keyboard, Silk.NET.Input.Key key, int scanCode)
    {
        Data.Input.OnKeyDown((Data.Key)key);
        UIController.InputField((Data.Key)key);
    }

    public override void OnKeyUp(Silk.NET.Input.IKeyboard keyboard, Silk.NET.Input.Key key, int scanCode)
    {
        Data.Input.OnKeyUp((Data.Key)key);
    }
    
    public override void OnKeyChar(Silk.NET.Input.IKeyboard keyboard, char c)
    {
        
    }
    
    public override void OnMouseMove(Silk.NET.Input.IMouse mouse, Vector2 position)
    {
        
    }
    
    public override void OnMouseDown(Silk.NET.Input.IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        Data.Input.OnMouseDown((Data.MouseButton)button);
    }
    
    public override void OnMouseUp(Silk.NET.Input.IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        Data.Input.OnMouseUp((Data.MouseButton)button);
    }
    
    public override void OnScroll(Silk.NET.Input.IMouse mouse, Silk.NET.Input.ScrollWheel scroll)
    {
        Data.Input.OnMouseWheel((scroll.X, scroll.Y));
    }
    

    public override void OnLoad()
    {
        
    }

    public override void OnRenderLoad()
    {

    }

    public override void OnResize(int width, int height)
    {
        Width = width;
        Height = height;

        Scene.CurrentScene?.Resize();
    }

    public override void OnUpdate(double delta)
    {
        // -- Rendering timer --
        shouldRender = false;
        double dt = frameTimer.Elapsed.TotalSeconds;
        if (dt >= TargetRenderingFrameTime)
        {
            shouldRender = !ForceSyncedRendering;
            _renderingDeltaTime = dt;
            frameTimer.Restart();
        }

        if (ExecutionMode == GameExecutionMode.Paused)
            return;

        // -- Fixed update --
        accumulator += delta;
        while (accumulator >= GameTime.FixedDeltaTime)
        {
            GameTime.FixedUpdate(GameTime.FixedDeltaTime);
            Scene.CurrentScene?.FixedUpdate();
            accumulator -= GameTime.FixedDeltaTime;
        }
        GameTime.PhysicsInterpolationT = accumulator / GameTime.FixedDeltaTime;

        // -- Update --
        Scene.CurrentScene?.Update();
        Scene.CurrentScene?.LateUpdate();
        if (ForceSyncedRendering)
            shouldRender = true;
    }

    public override void OnRender()
    {
        GameTime.Render((float)_renderingDeltaTime);

        UIController.CumulativeDepth = 0f;
        
        Scene.CurrentScene?.Render();

        UIController.GlobalRender();
        FBO.ResetAll();
    }

    public override void OnUnload()
    {
        
    }

    public static void SetCursorState(Data.CursorMode cursorMode)
    {
        Instance.CursorMode = cursorMode;
    }

    public static Data.CursorMode GetCursorState()
    {
        return Instance.CursorMode;
    }

    public static bool IsCursorState(Data.CursorMode cursorMode)
    {
        return Instance.CursorMode == cursorMode;
    }

    internal static void SetCursorState(object disabled)
    {
        throw new NotImplementedException();
    }
}