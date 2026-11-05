using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using PBG.Compilation;
using PBG.Core;
using PBG.Editor;
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

    public static PipeServer server;
    public static SharedAtomicInt FrameIndex;


    public override void OnKeyDown(IKeyboard keyboard, Silk.NET.Input.Key key, int scanCode)
    {
        UIController.InputField((Data.Key)key);
        server.Send(Mathf.IntsToString((int)CommandType.KeyDown, (int)key));
    }

    public override void OnKeyUp(IKeyboard keyboard, Silk.NET.Input.Key key, int scanCode)
    {
        server.Send(Mathf.IntsToString((int)CommandType.KeyUp, (int)key));
    }
    
    public override void OnKeyChar(IKeyboard keyboard, char c)
    {
        
    }
    
    public override void OnMouseMove(IMouse mouse, Vector2 position)
    {
        server.Send(Mathf.IntsToString((int)CommandType.MouseMove, (int)position.X - 240, (int)position.Y - 50));
    }
    
    public override void OnMouseDown(IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        server.Send(Mathf.IntsToString((int)CommandType.MouseDown, (int)button));
    }
    
    public override void OnMouseUp(IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        server.Send(Mathf.IntsToString((int)CommandType.MouseUp, (int)button));
    }
    
    public override void OnScroll(IMouse mouse, ScrollWheel scroll)
    {
        server.Send(Mathf.IntsToString((int)CommandType.MouseWheel, (int)scroll.X, (int)scroll.Y));
    }
    

    public override void OnLoad()
    {
        FrameIndex = new SharedAtomicInt("engine_frame", true);

        var initAttributes = AttributeManager.GetOrderedAttribute<InternalSystemInitAttribute, int>(a => (int)a.Attribute.Priority);
        AttributeManager.InvokeAttributeMethod(initAttributes, "Init", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    
        Type[] subClasses;

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
        

        Scene.LoadScene("Editor");

        ProcessManager.Run();

        CreateServer();
    }

    private async void SendCommand(string command)
    {
        if (server.IsConnected)
            await server.SendAsync(command);
    }

    private async void CreateServer()    {
        server = new("pbg_commands");
    }

    public override void OnRenderLoad()
    {
        UIData.Init();
    }

    public override void OnResize(int width, int height)
    {
        Width = width;
        Height = height;

        Scene.CurrentScene?.Resize();

        if (server != null && server.IsConnected)
            server.Send(Mathf.IntsToString((int)CommandType.Resize, EditorMenu.GameWidth, EditorMenu.GameHeight));
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

        HandleMessages();
    }

    public void HandleMessages()
    {
        var messages = server.Read();
        for (int i = 0; i < messages.Count; i++)
        {
            HandleMessage(messages[i]);
        }
    }

    public void HandleMessage(string message)
    {
        CommandType type = (CommandType)message.GetInt(0);

        Console.WriteLine("HANDLING MESSAGE OF TYPE: " + type);

        if (type == CommandType.SetCursor)
        {
            int key = message.GetInt(1);
            CursorMode = (Data.CursorMode)key;
        }
        else if (type == CommandType.Resize)
        {
            int width = message.GetInt(1);
            int height = message.GetInt(2);

            using var fdReceiver = new FdReceiver("/tmp/vk_share.sock");
            var fds = fdReceiver.Receive();

            Console.WriteLine("resizing: " + width + " " + height);

            Texture[] textures = new Texture[GFX.MAX_FRAMES_IN_FLIGHT];

            for (int i = 0; i < GFX.MAX_FRAMES_IN_FLIGHT; i++)
            {
                int fd = fds[i];

                Console.WriteLine("fd: " + fd);

                Texture texture = new(new() { Width = width, Height = height, IsExternalImage = true, Fd = fd, Format = Silk.NET.Vulkan.Format.B8G8R8A8Srgb });

                textures[i] = texture;
            }

            EditorMenu.Instance.SetEditorTextures(width, height, textures);
        }
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
        Scene.DisposeAll();

        var cleanupAttributes = AttributeManager.GetAttribute<InternalSystemCleanupAttribute>();
        AttributeManager.InvokeAttributeMethod(cleanupAttributes, "Cleanup", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    
        ProcessManager.Close();
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