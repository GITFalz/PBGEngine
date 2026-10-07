using System.Diagnostics;
using PBG.Core;
using PBG.Files;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Threads;
using PBG.UI;
using Silk.NET.Input;

namespace PBG;

public class Game : GameWindow
{
    public static Game Instance { get; private set; } = null!;

    public static PString MainPath => FileManager.MainPath;
    public static PString AssetsPath => FileManager.AssetsPath;
    public static PString ShaderPath => FileManager.ShaderPath;
    public static PString FixedShaderPath => FileManager.FixedShaderPath;
    public static PString TexturePath => FileManager.TexturePath;
    public static PString SettingsPath => FileManager.SettingsPath;

    public static PString DataPath => FileManager.DataPath;
    public static PString ModelPath => FileManager.ModelPath;
    public static PString UndoModelPath => FileManager.UndoModelPath;
    public static PString EditorRegistryPath => FileManager.EditorRegistryPath;
    public static PString EditorPalettePath => FileManager.EditorPalettePath;

    public static PString CustomPath => FileManager.CustomPath;
    public static PString CustomTempPath => FileManager.CustomTempPath;


    double accumulator = 0.0;

    private static double MaxFPS = 99999.0;
    private readonly double TargetFrameTime = 1.0 / MaxFPS;
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();

    private static double MaxRenderingFPS = 99999.0;
    private readonly double TargetRenderingFrameTime = 1.0 / MaxRenderingFPS;
    private readonly Stopwatch frameTimer = Stopwatch.StartNew();
    private double _renderingDeltaTime = 0;
    private double lastUpdateTime = 0.0;
    private double lastAccumulator2Update = 0;


    public static int Counter = 0;

    

    public Game(int width, int height) : base(width, height)
    {
        Instance = this;
        Width = width;
        Height = height;
        //GraphicsContext.graphicsContext.window.FramesPerSecond = 20;
    }

    public override void OnKeyDown(IKeyboard keyboard, Silk.NET.Input.Key key, int scanCode)
    {
        UIController.InputField((Data.Key)key);
    }

    public override void OnKeyUp(IKeyboard keyboard, Silk.NET.Input.Key key, int scanCode)
    {
        
    }
    
    public override void OnKeyChar(IKeyboard keyboard, char c)
    {
        
    }
    
    public override void OnMouseMove(IMouse mouse, Vector2 position)
    {
        
    }
    
    public override void OnMouseDown(IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        
    }
    
    public override void OnMouseUp(IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        
    }
    
    public override void OnScroll(IMouse mouse, ScrollWheel scroll)
    {
        
    }
    

    public override void OnLoad()
    {
        ItemDataManager.Init();

        Voxel.VoxelChunkGenerator.InitCache();

        Type[] subClasses;
        /*
        var subClasses = ASettings.GetSubclasses();
        Console.WriteLine("There are " + subClasses.Length + " settings");
        for (int i = 0; i < subClasses.Length; i++)
        {
            var subClass = subClasses[i];
            Console.WriteLine("Instanced " + subClass.GetType().Name + " settings");
            var instance = Activator.CreateInstance(subClass);
            if (instance != null)
                SettingsManager.Register((ASettings)instance);
        }
        SettingsManager.InitializeUI();
        */

        subClasses = Scene.GetSubclasses();
                    
        Console.WriteLine("There are " + subClasses.Length + " scenes");
        for (int i = 0; i < subClasses.Length; i++)
        {
            var subClass = subClasses[i];
            Console.WriteLine("Instanced " + subClass.GetType().Name + " scene");
            Activator.CreateInstance(subClass);
        }

        foreach (var (name, scene) in Scene.Scenes)
        {
            Scene.CurrentlyLoadingScene = scene;
            scene.Preload();
        }

        foreach (var (name, scene) in Scene.Scenes)
        {
            Scene.CurrentlyLoadingScene = scene;
            scene.Load();
            scene.InitScripts();
        }
        Scene.CurrentlyLoadingScene = null;
        // Load mods
        

        Scene.LoadScene("NodeTest");
    }

    public override void OnRenderLoad()
    {
        ItemDataManager.GenerateIcons();
        UIData.Init();
    }

    public override void OnResize(int width, int height)
    {
        Width = width;
        Height = height;

        Scene.CurrentScene?.Resize();
    }

    private static readonly System.Diagnostics.Stopwatch debugTimer = new();
    private static long frameStartTicks; // marks the beginning of OnUpdate for this frame

    public override void OnUpdate(double delta)
    {
        Scene.LoadSceneFinal();

        // -- Physics update --
        accumulator += delta;
        while (accumulator >= Data.GameTime.FixedDeltaTime)
        {
            Data.GameTime.FixedUpdate(Data.GameTime.FixedDeltaTime);
            Scene.CurrentScene?.FixedUpdate();
            accumulator -= Data.GameTime.FixedDeltaTime;
        }
        Data.GameTime.PhysicsInterpolationT = accumulator / Data.GameTime.FixedDeltaTime;
        Data.GameTime.Update((float)delta);

        Scene.CurrentScene?.Update();
        Scene.CurrentScene?.LateUpdate();

        TaskPool.Update();

        //if (GameTime.FpsUpdated)
            //Console.WriteLine(GameTime.Fps);
    }

    public override void OnCompute()
    {
        Scene.CurrentScene?.Compute();
    }

    public override void OnRender()
    {
        Data.GameTime.Render();

        UIController.CumulativeDepth = 0f;
        
        Scene.CurrentScene?.Render();

        UIController.GlobalRender();
    }

    public override void OnUnload()
    {
        Scene.CurrentScene?.Exit();

        PBGConsole.Save();
    }

    public static void SetCursorState(PBG.Data.CursorMode cursorMode)
    {
        CursorMode = cursorMode;
    }

    public static PBG.Data.CursorMode GetCursorState()
    {
        return CursorMode;
    }

    public static bool IsCursorState(PBG.Data.CursorMode cursorMode)
    {
        return CursorMode == cursorMode;
    }

    internal static void SetCursorState(object disabled)
    {
        throw new NotImplementedException();
    }
}