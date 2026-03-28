using System.Diagnostics;
using PBG.Core;
using PBG.Data;
using PBG.Graphics;
using PBG.Modeling;
using PBG.Rendering;

using Silk.NET.Vulkan;

namespace PBG.Editor;

public class EditorManager : ScriptingNode
{
    private static EditorState _editorState = EditorState.Stopped;
    public static SceneBlueprintNode? SelectedNode = null;
    public static EditorManager Instance = null!;
    public static bool WorldSpace = true;

    private static bool _fileChange = false;
    private static bool _fileChangeCache = false;
    private static float _fileChangeAt = 0;
    private static bool _updateFileManager = false;
    private static object _lock = new();
    
    public static bool Playing => _editorState == EditorState.Playing;
    public static bool Paused => _editorState == EditorState.Paused;
    public static bool Stopped => _editorState == EditorState.Stopped;

    public TransformGizmo transformGizmo = null!;
    public RotationGizmo rotationGizmo = null!;

    public EditorUI UI;

    public FileSystemWatcher FileSystemWatcher = new();
    public HashSet<string> FileChanges = [];
    
    public EditorManager(EditorUI ui)
    {
        Instance = this;
        UI = ui;

        FileSystemWatcher.Path = Game.CurrentProjectPath;
        FileSystemWatcher.EnableRaisingEvents = true;
        FileSystemWatcher.Filter = "*.cs";
        FileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
        FileSystemWatcher.IncludeSubdirectories = true;

        FileSystemWatcher.Changed += OnChanged;
        FileSystemWatcher.Created += OnCreated;
        FileSystemWatcher.Renamed += OnRenamed;
        FileSystemWatcher.Deleted += OnDeleted;
    }

    void Start()
    {
        transformGizmo = new(Camera);
        rotationGizmo = new(Camera);

        Camera.SetCameraMode(CameraMode.Free);
        SceneBlueprint.BlueprintScene.SetCamera(Camera);

        GameWindow.ExecutionMode = GameExecutionMode.Paused;

        EditorLoader.Load(Game.CurrentProjectPath / "assets.json");
        EditorUI.Instance.CurrentPath = EditorLoader.Files;
        EditorUI.Instance.GenerateCurrentFolderFiles();
    }

    public void LoadSuccess(MeshRenderer mesh)
    {
        Console.WriteLine("Loaded mesh successfully " + (SceneBlueprint.BlueprintScene == null));
        if (SceneBlueprint.BlueprintScene != null)
        {
            SceneBlueprintNode node = SceneBlueprint.AddNode("Model");
            node.AddScript(mesh);
            node.OrderScripts();

            SceneBlueprint.BlueprintScene.UpdatePending();
            EditorUI.Instance.ReloadSceneHierarchy();
        } 
    }

    public void LoadFail()
    {
        Console.WriteLine("Loaded mesh unsuccessfully");
    }
    
    void Resize()
    {
        if (Stopped)
        {
            SceneBlueprint.BlueprintScene?.SmallResize();
        }
    }

    void Update()
    { 
        if (Stopped)
        {
            SceneBlueprint.BlueprintScene.SmallUpdate();

            lock (_lock)
            {
                if (GraphicsContext.IsFocused)
                {   
                    HandleFileChange();
                    HandleFileManagerUpdate();
                }
            }

            if (Camera.GetCameraMode() == CameraMode.Fixed) 
                GizmoUpdate();

            if (Input.IsKeyDown(Key.ControlLeft))
            {
                if (Input.IsKeyPressed(Key.S))
                {
                    SceneSerializer.Serialize(SceneBlueprint.CurrentPath, SceneBlueprint.GetJson());
                }
            }
        }

        if (Stopped || Paused)
        {
            if (Input.IsKeyPressed(Key.Escape))
            {
                if (Camera.GetCameraMode() == CameraMode.Free)
                {
                    Camera.SetCameraMode(CameraMode.Fixed);
                    Game.SetCursorState(CursorMode.Normal);

                    FixedCameraStart();
                }
                else
                {
                    Camera.SetCameraMode(CameraMode.Free);
                    Game.SetCursorState(CursorMode.Disabled);
                }
            }
        }

        if (Playing)
        {
            if (Input.IsKeyDown(Key.ControlLeft) && Input.IsKeyPressed(Key.Escape))
            {
                Game.SetCursorState(CursorMode.Normal);
            }
        }
        
        if (GameTime.FpsUpdated)
        {
            EditorUI.Instance.FPSText.UpdateText($"FPS: {GameTime.Fps}");
        }    
    }

    private void HandleFileChange()
    {
        if (_fileChangeCache || !_fileChange)
            return;

        if (_editorState == EditorState.Stopped)
        {
            string outputPath = Game.CurrentProjectPath / (Path.GetFileName(Game.CurrentProjectPath) + ".dll");

            FileSystemWatcher.EnableRaisingEvents = false;
            if (HotReloadManager.Compile(Game.CurrentProjectPath, outputPath))
            {
                Console.WriteLine("Compiled dll");
                if (!HotReloadManager.Load(outputPath))
                {
                    Console.WriteLine("[Warning] : Failed to load project .dll");
                }
                else
                {
                    Console.WriteLine("Loaded dll");
                }
            }
            else
            {
                Console.WriteLine("[Warning] : Failed to compile scripts into .dll");
            }

            FileSystemWatcher.EnableRaisingEvents = true;
            
            ResetScene();
            HotReloadManager.CleanUp();

            _fileChange = false;
        }
        else
        {
            Console.WriteLine("[Warning] : Cannot recompile files when the game is running, please stop the game");
            _fileChangeCache = true;
        }
    }

    private void HandleFileManagerUpdate()
    {
        if (!_updateFileManager)
            return;

        EditorUI.Instance.GenerateCurrentFolderFiles();
        _updateFileManager = false;
    }

    private void FixedCameraStart()
    {
        transformGizmo.GenerateWorldSpacePoints();
        rotationGizmo.GenerateWorldSpacePoints();
    }

    private void GizmoUpdate()
    {
        if (SelectedNode == null)
            return;

        transformGizmo.Position = SelectedNode.Transform.Position;
        if (!WorldSpace)
        {
            rotationGizmo.Rotation = SelectedNode.Transform.Rotation;
        }

        transformGizmo.Update();
        transformGizmo.Rotation = rotationGizmo.Rotation;
        rotationGizmo.Position = transformGizmo.Position;

        SelectedNode.Transform.Position = transformGizmo.Position;

        if (rotationGizmo.Update())
        {
            if (WorldSpace)
            {
                SelectedNode.Transform.Rotation = rotationGizmo.ChangedRotation * SelectedNode.Transform.Rotation;
            }
            else
            {
                SelectedNode.Transform.Rotation = rotationGizmo.Rotation;
            }
        }

        if (transformGizmo.UpdateScreenSpacePositions || rotationGizmo.UpdateScreenSpacePositions)
        {
            transformGizmo.GenerateWorldSpacePoints();
            rotationGizmo.GenerateWorldSpacePoints();
        }
    }

    void Render()
    {
        if (Stopped)
        {
            SceneBlueprint.BlueprintScene.Render();

            if (SelectedNode == null)
                return;

            transformGizmo.Render();
            rotationGizmo.Render();   
        }  
    }

    void Dispose()
    {
        SceneBlueprint.Clear();
        FileSystemWatcher.Dispose();
    }


    public static void ResetScene()
    {
        SceneBlueprint.RefreshScripts();
        Instance.UI.ReloadSceneHierarchy();
        Scene.UnloadScene();
    }

    public static void SetEditorState(EditorState editorState)
    {
        lock (_lock)
        {
            if (editorState == EditorState.Playing)
            {
                GameWindow.ExecutionMode = GameExecutionMode.Running;
            }
            else
            {
                GameWindow.ExecutionMode = GameExecutionMode.Paused;
            }

            _editorState = editorState;
            if (editorState == EditorState.Stopped)
            {
                if (_fileChangeCache)
                {
                    _fileChangeCache = false;
                    _fileChange = true;
                }

                if (!_fileChange)
                {
                    ResetScene(); // if no file has changed, this is just an ordinary reset
                }
            }
        }
    }

    public void OnChanged(object s, FileSystemEventArgs e)
    {
        Console.WriteLine($"File changed at path '{e.FullPath}'");
        lock (_lock)
        {
            _fileChange = true;
            _fileChangeCache = false;
            _fileChangeAt = GameTime.TotalTime;
        }
    }

    public void OnCreated(object s, FileSystemEventArgs e)
    {
        lock (_lock)
        {
            var directory = Path.GetDirectoryName(e.FullPath);
            if (directory == EditorUI.Instance.CurrentPath.GlobalPath)
                _updateFileManager = true;
        }
    }

    public void OnRenamed(object s, FileSystemEventArgs e)
    {
        lock (_lock)
        {
            var directory = Path.GetDirectoryName(e.FullPath);
            if (directory == EditorUI.Instance.CurrentPath.GlobalPath)
                _updateFileManager = true;
        }
    }

    public void OnDeleted(object s, FileSystemEventArgs e)
    {
        lock (_lock)
        {
            var directory = Path.GetDirectoryName(e.FullPath);
            if (directory == EditorUI.Instance.CurrentPath.GlobalPath)
                _updateFileManager = true;
        }
    }
}

public enum EditorState
{
    Stopped,
    Playing,
    Paused
}