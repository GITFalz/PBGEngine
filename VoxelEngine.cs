using PBG.Files;
using PBG.Graphics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace PBG;

public class VoxelEngine : IDisposable
{
    public static VoxelEngine Instance { get; private set; } = null!;

    public static PString MainPath = FileManager.CreatePath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".pbgAssets");

    // Assets
    public static PString AssetsPath = FileManager.CreatePath(MainPath, "assets");
    public static PString TexturePath = FileManager.CreatePath(AssetsPath, "textures");
    public static PString ShaderPath = FileManager.CreatePath(AssetsPath, "shaders");

    // Data
    public static PString DataPath = FileManager.CreatePath(MainPath, "data");
    public static PString CoreDataPath = FileManager.CreatePath(DataPath, "core");

    public static int Width;
    public static int Height;
    
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
    
    private IWindow _window;

    public Renderer Renderer;
    
    public VoxelEngine(int width, int height)
    {
        Instance = this;
        
        Width = width;
        Height = height;

        var options = WindowOptions.DefaultVulkan;

        options.Size   = new Vector2D<int>(width, height);
        options.Title  = "My First Silk.NET Window";
        options.VSync  = false;

        _window = Window.Create(options);

        #if DEBUG
        bool enableValidation = true;
        #else
        bool enableValidation = false;
        #endif

        Renderer = new Renderer(_window, enableValidation);
    }

    public void SetMouse(IMouse mouse)
    {
        Mouse = mouse;
        _cursorMode = (PBG.Data.CursorMode)mouse.Cursor.CursorMode;
    }

    public void Run()
    {
        Renderer.Run();
    }

    public void Dispose()
    {
        Renderer.Dispose();
        _window.Dispose();
    }
}