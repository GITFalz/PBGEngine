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
    public static SharedAtomicFloat? EngineLoadingProgress;

    public override void OnKeyDown(IKeyboard keyboard, Silk.NET.Input.Key key, int scanCode)
    {
        UIController.InputField((Data.Key)key);

        if (!EditorMenu.Focused)
            return;

        TrySend(Mathf.IntsToString((int)CommandType.KeyDown, (int)key));
    }

    public override void OnKeyUp(IKeyboard keyboard, Silk.NET.Input.Key key, int scanCode)
    {
        if (!EditorMenu.Focused)
            return;

        TrySend(Mathf.IntsToString((int)CommandType.KeyUp, (int)key));
    }
    
    public override void OnMouseMove(IMouse mouse, Vector2 position)
    {
        if (!EditorMenu.Focused)
            return;

        TrySend(Mathf.IntsToString((int)CommandType.MouseMove, (int)position.X - EditorMenu.GameOffsetX, (int)position.Y - EditorMenu.GameOffsetY));
    }
    
    public override void OnMouseDown(IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        if (!EditorMenu.Focused)
            return;

        TrySend(Mathf.IntsToString((int)CommandType.MouseDown, (int)button));
    }
    
    public override void OnMouseUp(IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        if (!EditorMenu.Focused)
            return;

        TrySend(Mathf.IntsToString((int)CommandType.MouseUp, (int)button));
    }
    
    public override void OnScroll(IMouse mouse, ScrollWheel scroll)
    {
        if (!EditorMenu.Focused)
            return;

        TrySend(Mathf.IntsToString((int)CommandType.MouseWheel, (int)scroll.X, (int)scroll.Y));
    }
    

    public override void OnLoad()
    {
        FrameIndex = new SharedAtomicInt("engine_frame", true);
        EngineLoadingProgress = new SharedAtomicFloat("engine_loading", true);

        Scene.LoadScene("Editor");
        
        Engine.CommandHelper.Add(CommandType.SetCursor, msg =>
        {
            int key = msg.GetInt(0);
            CursorMode = (Data.CursorMode)key;
        });

        Engine.CommandHelper.Add(CommandType.Resize, msg =>
        {
            int width = msg.GetInt(0);
            int height = msg.GetInt(1);

            using var fdReceiver = new FdReceiver("/tmp/vk_share.sock");
            var fds = fdReceiver.Receive();
            int imageCount = GFX.MAX_FRAMES_IN_FLIGHT;
            if (fds.Count != imageCount && fds.Count != imageCount * 3)
                throw new InvalidDataException($"Expected {imageCount} image FDs and optionally {imageCount * 2} semaphore FDs, got {fds.Count}.");

            if (fds.Count == imageCount * 3)
                GFX.ImportSharedImageSemaphores(fds.Skip(imageCount).ToArray(), imageCount);

            Console.WriteLine("resizing: " + width + " " + height);

            Texture[] textures = new Texture[imageCount];

            for (int i = 0; i < imageCount; i++)
            {
                int fd = fds[i];

                //Console.WriteLine("fd: " + fd);

                Texture texture = new(new() { Width = width, Height = height, IsExternalImage = true, Fd = fd, Format = Silk.NET.Vulkan.Format.B8G8R8A8Srgb });

                textures[i] = texture;
            }

            EditorMenu.Instance.SetEditorTextures(width, height, textures);
        });

        Engine.CommandHelper.Add(CommandType.Loaded, msg =>
        {
            Send(CommandHelper.Create(CommandType.Resize, EditorMenu.GameWidth, EditorMenu.GameHeight));
        });

        Engine.CommandHelper.Add(CommandType.SceneHierarchy, msg =>
        {
            EditorMenu.Instance.GenerateHierarchy(msg.Content);
        });

        Engine.CommandHelper.Add(CommandType.NodeComponents, msg =>
        {
            EditorMenu.Instance.GenerateComponents(msg.Content);
        });

        Engine.CommandHelper.Add(CommandType.ClassInfo, msg =>
        {
            EditorMenu.Instance.GenerateClassInfo(msg.Content);
        });

        Engine.CommandHelper.Add(CommandType.SearchClass, msg =>
        {
            EditorMenu.Instance.GenerateSearch(msg.Content);
        });
    }

    public override void OnResize(int width, int height)
    {
        TrySend(Mathf.IntsToString((int)CommandType.Resize, EditorMenu.GameWidth, EditorMenu.GameHeight));
    }

    public override void OnUpdate(double delta)
    {
        
    }

    public override void OnUnload()
    {
        ProcessManager.Close();
    }
}