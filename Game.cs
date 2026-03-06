using System.Diagnostics;
using System.Runtime.InteropServices;
using PBG.Core;
using PBG.Data;
using PBG.Files;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.Threads;
using PBG.UI;
using PBG.Voxel;
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

    public override void OnKeyDown(IKeyboard keyboard, Key key, int scanCode)
    {
        Input.OnKeyDown(key);
    }

    public override void OnKeyUp(IKeyboard keyboard, Key key, int scanCode)
    {
        Input.OnKeyUp(key);
    }
    
    public override void OnKeyChar(IKeyboard keyboard, char c)
    {
        
    }
    
    public override void OnMouseMove(IMouse mouse, Vector2 position)
    {
        
    }
    
    public override void OnMouseDown(IMouse mouse, MouseButton button)
    {
        Input.OnMouseDown(button);
    }
    
    public override void OnMouseUp(IMouse mouse, MouseButton button)
    {
        Input.OnMouseUp(button);
    }
    
    public override void OnScroll(IMouse mouse, ScrollWheel scroll)
    {
        Input.OnMouseWheel((scroll.X, scroll.Y));
    }
    

    public override void OnLoad()
    {
        Input.Start(Mouse);

        BlockData.Init();
        //WeaponData.Init();
        ItemDataManager.GenerateIcons();

        VoxelChunkGenerator.InitCache();

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

            for (int i = 0; i < scene.PendingList.Count; i++)
            {
                var pending = scene.PendingList[i];
                pending.InitPendingComponents();
            }
            scene.PendingList = [];
        }
        Scene.CurrentlyLoadingScene = null;
        // Load mods
        

        Scene.LoadScene("MainMenu");
    }

    public override void OnResize(int width, int height)
    {
        Width = width;
        Height = height;

        Scene.CurrentScene?.Resize();
    }

    public override void OnUpdate(double delta)
    {
        Scene.LoadSceneFinal();

        shouldRender = false;
        double dt = frameTimer.Elapsed.TotalSeconds;
        if (dt >= TargetRenderingFrameTime)
        {
            shouldRender = !ForceSyncedRendering;
            _renderingDeltaTime = dt;
            frameTimer.Restart();
        }

        // -- Physics update --
        accumulator += delta;
        while (accumulator >= GameTime.FixedDeltaTime)
        {
            GameTime.FixedUpdate(GameTime.FixedDeltaTime);
            Scene.CurrentScene?.FixedUpdate();
            accumulator -= GameTime.FixedDeltaTime;
        }
        GameTime.PhysicsInterpolationT = accumulator / GameTime.FixedDeltaTime;

        double now = stopwatch.Elapsed.TotalSeconds;
        accumulator2 += now - lastAccumulator2Update;
        lastAccumulator2Update = now;
        
        if (accumulator2 >= TargetFrameTime)
        {
            accumulator2 -= TargetFrameTime;

            float deltaTime = (float)(now - lastUpdateTime);
            lastUpdateTime = now;

            Input.Update(Mouse);
            GameTime.Update(deltaTime);

            Scene.CurrentScene?.Update();
            Scene.CurrentScene?.LateUpdate();
            
            if (ForceSyncedRendering)
                shouldRender = true;
        }

        TaskPool.Update();

        if (GameTime.FpsUpdated)
            Console.WriteLine(GameTime.Fps);
    }

    public override void OnRender()
    {
        GameTime.Render((float)_renderingDeltaTime);

        UIController.ClearFrameBuffer();
        
        Scene.CurrentScene?.Render();

        UIController.GlobalRender();
    }

    public override void OnUnload()
    {
        
    }

    public static void SetCursorState(CursorMode cursorMode)
    {
        Instance.CursorMode = cursorMode;
    }

    public static CursorMode GetCursorState()
    {
        return Instance.CursorMode;
    }

    public static bool IsCursorState(CursorMode cursorMode)
    {
        return Instance.CursorMode == cursorMode;
    }

    internal static void SetCursorState(object disabled)
    {
        throw new NotImplementedException();
    }
}