using System.Diagnostics;
using System.Reflection;
using PBG.MathLibrary;
using Silk.NET.Input;

namespace PBG.Graphics;

public abstract class GameWindow
{
    private static ScriptLoader _scriptLoader = new ScriptLoader();

    public static GameWindow Instance { get; private set; } = null!;

    public static int Width = 0;
    public static int Height = 0;

    private static VulkanInstance VulkanInstance;
    public static PBG.Data.CursorMode CursorMode
    {
        get => VulkanInstance.CursorMode;
        set => VulkanInstance.CursorMode = value;
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

    public static void New(int width, int height)
    {
        var gameWindow = GetNewWindow(width, height);

        VulkanInstance = new VulkanInstance(gameWindow, width, height);
        Instance = gameWindow;

        gameWindow.Run();
    }

    public static GameWindow GetNewWindow(int width, int height)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets.dll");
        return GetNewWindow(width, height, path);
    }

    private static GameWindow GetNewWindow(int width, int height, string path)
    {
        Width = width;
        Height = height;

        var assembly = _scriptLoader.Load(path);
        var gameWindowType = assembly.GetTypes().FirstOrDefault(t => t.IsSubclassOf(typeof(GameWindow)) && !t.IsAbstract);
        if (gameWindowType != null)
        {
            var constructor = gameWindowType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (constructor != null)
            {
                var gameWindow = (GameWindow)constructor.Invoke(null);
                return gameWindow;
            }
        }

        throw new Exception("No GameWindow subclass found in Assets.dll");
    }

    public static void HotReload()
    {
        var builtAssemblyPath = BuildAssets();
        if (builtAssemblyPath == null)
            return;

        Instance.OnUnload();

        BufferBase.DisposeAllBuffers();

        MemoryHelper.ReportLeaks();

        _scriptLoader.Unload();

        var gameWindow = GetNewWindow(Width, Height, builtAssemblyPath);

        VulkanInstance.SetGameWindow(gameWindow);
        //Instance = gameWindow;

        //Instance.OnLoad();
    }

    private static string? BuildAssets()
    {
        var projectRoot = FindProjectRoot();
        var projectPath = Path.Combine(projectRoot, "Assets", "Assets.csproj");

#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = projectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add("build");
        process.StartInfo.ArgumentList.Add(projectPath);
        process.StartInfo.ArgumentList.Add("--configuration");
        process.StartInfo.ArgumentList.Add(configuration);

        if (!process.Start())
            throw new InvalidOperationException("Failed to start dotnet to build the Assets project.");

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        Task.WaitAll(standardOutput, standardError);

        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine($"Assets hot reload build failed (exit code {process.ExitCode}).");
            Console.Error.WriteLine(standardOutput.Result);
            Console.Error.WriteLine(standardError.Result);
            return null;
        }

        Console.WriteLine(standardOutput.Result);
        Console.Error.WriteLine(standardError.Result);

        var builtAssemblyPath = Path.Combine(projectRoot, "Assets", "bin", configuration, "net9.0", "Assets.dll");
        if (!File.Exists(builtAssemblyPath))
            throw new FileNotFoundException("The Assets build succeeded but did not produce Assets.dll.", builtAssemblyPath);

        return builtAssemblyPath;
    }

    private static string FindProjectRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Assets", "Assets.csproj")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException(
            $"Could not find Assets/Assets.csproj above the application directory '{AppContext.BaseDirectory}'.");
    }
}