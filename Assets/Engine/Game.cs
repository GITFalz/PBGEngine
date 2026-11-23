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

    public override void OnKeyDown(IKeyboard keyboard, Silk.NET.Input.Key key, int scanCode)
    {
        UIController.InputField((Data.Key)key);
    }

    public override void OnLoad()
    {
        Scene.OnLoaded += scene =>
        {
            var json = Editor.EditorHelper.GetSceneJson();
            Send(CommandHelper.Create(CommandType.SceneHierarchy, json));
        };
        
        Scene.LoadScene("NodeTest");

        Engine.CommandHelper.Add(CommandType.KeyDown, msg => 
        {
            int key = msg.GetInt(0);
            Input.OnKeyDown((Data.Key)key);
        });

        Engine.CommandHelper.Add(CommandType.KeyUp, msg => 
        {
            int key = msg.GetInt(0);
            Input.OnKeyUp((Data.Key)key);
        });

        Engine.CommandHelper.Add(CommandType.MouseDown, msg => 
        {
            int key = msg.GetInt(0);
            Input.OnMouseDown((Data.MouseButton)key);
        });

        Engine.CommandHelper.Add(CommandType.MouseUp, msg => 
        {
            int key = msg.GetInt(0);
            Input.OnMouseUp((Data.MouseButton)key);
        });

        Engine.CommandHelper.Add(CommandType.MouseMove, msg => 
        {
            int x = msg.GetInt(0);
            int y = msg.GetInt(1);
            Input.OnMouseMove((x, y));
        });

        Engine.CommandHelper.Add(CommandType.MouseWheel, msg => 
        {
            int x = msg.GetInt(0);
            int y = msg.GetInt(1);
            Input.OnMouseWheel((x, y));
        });

        Engine.CommandHelper.Add(CommandType.Resize, msg => 
        {
            int width = msg.GetInt(0);
            int height = msg.GetInt(1);

            Engine.OnResize(new(width, height));
        });

        Engine.CommandHelper.Add(CommandType.NodeComponents, msg => 
        {
            var json = Editor.EditorHelper.GetNodeComponentsJson(msg.Content);
            Send(CommandHelper.Create(CommandType.NodeComponents, json));
        });

        Engine.CommandHelper.Add(CommandType.SearchClass, msg => 
        {
            var json = Editor.EditorHelper.SearchClass(msg.Content);
            Send(CommandHelper.Create(CommandType.SearchClass, json));
        });

        Engine.CommandHelper.Add(CommandType.ClassInfo, msg => 
        {
            var json = Editor.EditorHelper.ClassInfo(msg.Content);
            Send(CommandHelper.Create(CommandType.ClassInfo, json));
        });

        Engine.CommandHelper.Add(CommandType.SetField, msg => 
        {
            Editor.EditorHelper.SetValue(msg.Content);
        });

    }

    public override void OnRenderLoad()
    {
        ItemDataManager.GenerateIcons();
    }

    public override void OnUpdate(double delta)
    {

    }

    public override void OnRender()
    {
        FrameIndex?.Increment();
    }

    public override void OnUnload()
    {
        Engine.pipeEndpoint?.Dispose();
        FrameIndex?.Dispose();

        PBGConsole.Save();
    }
}