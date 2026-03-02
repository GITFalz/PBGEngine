using System.Runtime.InteropServices;
using PBG.Core;
using PBG.Data;
using Silk.NET.Input;
namespace PBG.UI.FileManager;

public class FileManager : ScriptingNode
{
    public string DefaultPath;

    private List<string> _pathHistory = [];
    private int _currentHistoryIndex = 0;

    public HashSet<string> SelectedFiles = [];

    public bool IsVisible { get => _visible; }
    private bool _visible = false;

    public bool IsHovering = false;

    public FileManagerType HandleType = FileManagerType.Import;

    public Action<string> SaveFile = (path) => { };

    public string FileType = "";

    public FileManagerUI UI;

    public FileManager()
    {
        DefaultPath = Game.MainPath;
        UI = new(this);
    }

    void Start()
    {
        var controller = Transform.GetComponent<UIController>(); 
        controller.AddElement(UI);
        _pathHistory.Add(DefaultPath);
        Transform.Disabled = true;
    }

    public string[] GetSelectedFiles() => [.. SelectedFiles];

    public void SetAction(FileManagerType type)
    {
        HandleType = type;
    }

    public void ToggleOn()
    {
        if (_visible)
            return;

        _visible = true;
        Transform.Disabled = false;
    }

    public void ToggleOff()
    {
        if (!_visible)
            return;

        _visible = false;
        Transform.Disabled = true;
        IsHovering = false;
    }

    public void Toggle()
    {
        if (_visible)
            ToggleOff();
        else
            ToggleOn();
    }

    public string? GetSaveFilePath() => UI.GetSaveFilePath();

    public string GetPath(UIField field)
    {
        string[] paths = field.GetText().Split(['\'', '/']);
        if (paths.Length == 0)
            return DefaultPath;


        string path = paths[0];
        for (int i = 1; i < paths.Length; i++)
        {
            string p = Path.Combine(path, paths[i]);
            if (i == paths.Length - 1 && File.Exists(p)) // return only the folder path
                break;

            path = p;
        }

        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? '/' + path : path;
    }

    void Update()
    {
        if (Input.IsKeyDown(Key.ControlLeft) && Input.IsKeyPressed(Key.W) && Undo(out var path) && path != UI.CurrentPath)
        {
            UI.CurrentPath = path;
            UI.GenerateFiles();
        }
        
        if (Input.IsKeyDown(Key.ControlLeft) && Input.IsKeyPressed(Key.Y) && Redo(out path) && path != UI.CurrentPath)
        {
            UI.CurrentPath = path;
            UI.GenerateFiles();
        }
    }

    public void AddToHistory(string path)
    {
        if (!Directory.Exists(path))
            return;

        for (int i = 0; i < _currentHistoryIndex; i++)
        {
            _pathHistory.RemoveAt(0);
        }

        _pathHistory.Insert(0, path);
        _currentHistoryIndex = 0;

        if (_pathHistory.Count > 100)
        {
            _pathHistory.RemoveAt(_pathHistory.Count - 1);
        }
    }

    public bool Undo(out string path)
    {
        path = "";
        if (_pathHistory.Count == 0)
            return false;

        int last = _pathHistory.Count - 1;
        if (_currentHistoryIndex >= last)
        {
            _currentHistoryIndex = last;
            path = _pathHistory[last];
            return true;
        }

        _currentHistoryIndex++;
        path = _pathHistory[_currentHistoryIndex];
        return true;
    }

    public bool Redo(out string path)
    {   
        path = "";
        if (_pathHistory.Count == 0)
            return false;

        if (_currentHistoryIndex <= 0)
        {
            _currentHistoryIndex = 0;
            path = _pathHistory[0];
            return true;
        }

        _currentHistoryIndex--;
        path = _pathHistory[_currentHistoryIndex];
        return true;
    }

    void Exit()
    {
        SelectedFiles = [];
    }
}

public enum FileManagerType
{
    Import,
    Export,
}