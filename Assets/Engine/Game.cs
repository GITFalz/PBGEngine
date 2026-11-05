using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using PBG.Core;
using PBG.Data;
using PBG.Files;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Pipes;
using PBG.Threads;  
using PBG.UI;
using Silk.NET.Input;

namespace PBG;

public class Game : GameWindow
{
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

    public static SharedAtomicInt FrameIndex;

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
        FrameIndex = new SharedAtomicInt("engine_frame");

        var initAttributes = AttributeManager.GetOrderedAttribute<InternalSystemInitAttribute, int>(a => (int)a.Attribute.Priority);
        AttributeManager.InvokeAttributeMethod(initAttributes, "Init", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        
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
        HandleMessages();

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
    }

    public void HandleMessages()
    {
        if (Engine.pipeEndpoint == null)
            return;

        var messages = Engine.pipeEndpoint.Read();
        for (int i = 0; i < messages.Count; i++)
        {
            HandleMessage(messages[i]);
        }
    }

    public void HandleMessage(string message)
    {
        CommandType type = (CommandType)message.GetInt(0);

        //Console.WriteLine("HANDLING MESSAGE OF TYPE: " + type);

        // key input
        if (type == CommandType.KeyDown)
        {
            int key = message.GetInt(1);
            Input.OnKeyDown((Data.Key)key);
        }
        else if (type == CommandType.KeyUp)
        {
            int key = message.GetInt(1);
            Input.OnKeyUp((Data.Key)key);
        }
        else if (type == CommandType.MouseDown)
        {
            int key = message.GetInt(1);
            Input.OnMouseDown((Data.MouseButton)key);
        }
        else if (type == CommandType.MouseUp)
        {
            int key = message.GetInt(1);
            Input.OnMouseUp((Data.MouseButton)key);
        }
        else if (type == CommandType.MouseMove)
        {
            int x = message.GetInt(1);
            int y = message.GetInt(2);
            Input.OnMouseMove((x, y));
        }
        else if (type == CommandType.MouseWheel)
        {
            int x = message.GetInt(1);
            int y = message.GetInt(2);
            Input.OnMouseWheel((x, y));
        }
        else if (type == CommandType.Resize)
        {
            int width = message.GetInt(1);
            int height = message.GetInt(2);

            Engine.OnResize(new(width, height));
        }
    }

    public override void OnCompute()
    {
        Scene.CurrentScene?.Compute();
    }

    public override void OnRender()
    {
        FrameIndex.Value = (int)GFX.CurrentFrame;

        Data.GameTime.Render();

        UIController.CumulativeDepth = 0f;
        
        Scene.CurrentScene?.Render();

        UIController.GlobalRender();
    }

    public override void OnUnload()
    {
        Engine.pipeEndpoint.Dispose();
        
        Scene.CurrentScene?.Exit();
        Scene.DisposeAll();

        PBGConsole.Save();

        var cleanupAttributes = AttributeManager.GetAttribute<InternalSystemCleanupAttribute>();
        AttributeManager.InvokeAttributeMethod(cleanupAttributes, "Cleanup", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
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