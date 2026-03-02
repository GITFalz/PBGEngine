using PBG.Data;
using PBG.MathLibrary;
using PBG.UI.Creator;
using Silk.NET.Input;
using static PBG.UI.Styles;

namespace PBG.UI.FileManager;

public class FileManagerUI : UIScript
{
    public FileManager Manager;
    public string DefaultPath = Game.MainPath;
    public string CurrentPath
    {
        get => _currentPath;
        set
        {
            PathField.UpdateText(value);
            _currentPath = value;
        }
    }
    private string _currentPath;

    public string PathText => PathField.GetText();

    public List<string> CurrentPaths = [];

    private UIField PathField = null!;

    private UIVScroll _defaultPaths = null!;
    private UIVScroll _folders = null!;

    private UIText _elementCountText = null!;
    private UICol _fileNameCollection = null!;
    private UIField _fileNameField = null!;

    private UICol _gridButton = null!;
    private UICol _listButton = null!;

    private HashSet<UIElementBase> _selectedButtons = [];

    private float _time = 0;
    private string _clickedFolderPath = "";
    private bool _filesAsList = false;

    private bool _writeName = false;

    public FileManagerUI(FileManager manager) : base()
    {
        Manager = manager;
        _currentPath = DefaultPath;
        Run(GenerateFiles);
    }

    public override UIElementBase Script() =>
    new UICol(Class(w_[800], h_[640], top_left),
    OnHoverEnterCol(_ => Manager.IsHovering = true),
    OnHoverExitCol(_ => Manager.IsHovering = false),
    Sub(
        new UICol(Class(w_full, h_[40], blank_full_g_[25], border_ui_[2, 2, 2, 0], border_color_g_[30]), OnHold(Move), Sub(
            new UIImg(Class(w_[30], h_[30], middle_left, left_[10], icon_[14], gray_[50], top_[1])),
            new UIText("File Manager", Class(mc_[12], fs_[1.3f], middle_left, left_[45], top_[1])),
            new UICol(Class(w_[25], h_[25], border_ui_[2, 2, 2, 2], border_color_g_[30], blank_full, middle_right, right_[12], top_[1], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
            OnClickCol(_ => Manager.ToggleOff()),
            OnHoverEnter(HoverEnterIconButton),
            OnHoverExit(HoverExitIconButton),
            Sub(
                new UIImg(Class(icon_[15], w_[20], h_[20], gray_[50], middle_center))
            ))
        )),
        new UIVCol(Class(w_full, h_full_minus_[40], bottom_center, blank_full_g_[20], border_ui_[2, 0, 2, 2], border_color_g_[30]), Sub(
            new UICol(Class(w_full, h_[40], border_ui_[0, 2, 0, 2], border_color_g_[30]), Sub(
                new UIText("PATH:", Class(mc_[5], fs_[1f], middle_left, left_[12], gray_[50])),
                new UIHScroll(Class(w_full_minus_[110], middle_right, right_[42], h_[25], border_ui_[2, 2, 2, 2], border_color_g_[30], blank_full_g_[10], mask_children), Sub(
                    newField(DefaultPath, Class(mc_[1000], fs_[1f], middle_left, left_[5]),
                    OnTextEnter(f =>
                    {
                        CurrentPath = Manager.GetPath(f);
                        if (Directory.Exists(CurrentPath))
                            GenerateFiles();
                    }),
                    ref PathField)
                )),
                new UICol(Class(w_[25], h_[25], border_ui_[2, 2, 2, 2], border_color_g_[30], rgba_[0.3f, 0.3f, 0.3f, 0f], blank_full, middle_right, right_[12], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                OnClickCol(_ =>
                {
                    if (Directory.Exists(CurrentPath)) GenerateFiles();
                }),
                OnHoverEnter(HoverEnterIconButton),
                OnHoverExit(c => { if (_filesAsList) HoverExitIconButton(c); }),
                Sub(
                    new UIImg(Class(icon_[39], w_[20], h_[20], gray_[50], middle_center))
                ))
            )),
            new UIHCol(Class(w_full_minus_[4], top_center, h_full_minus_[80]), Sub(
                newVScroll(Class(w_[150], h_full, border_ui_[0, 0, 2, 0], border_color_g_[30], mask_children, border_[5, 5, 5, 5]), Sub([
                    ..Foreach(_knownDirectories, section =>
                        new UIVCol(Class(w_full, grow_children, ignore_invisible), Sub([
                            new UICol(Class(w_full, h_[20]), Sub(
                                new UIText(section.Name, Class(mc_[section.Name.Length], fs_[1f], gray_[50], left_[5], middle_left))
                            )),
                            new UIVCol(Class(w_full, h_[section.Paths.Length * 26], spacing_[1]), Sub([
                                ..Foreach(section.Paths, data =>
                                    new UICol(Class(w_full, h_[25], blank_full, hover_translation_[(-10, 0)], hover_translation_duration_[0.2f], hover_translation_easeout),
                                    OnHoverEnterCol(c => c.UpdateColor((0.3f, 0.3f, 0.3f, 1f))),
                                    OnHoverExitCol(c => c.UpdateColor((0f, 0f, 0f, 0f))),
                                    OnClickCol(_ => GenerateFiles(data.Path)),
                                    Sub(Run(() => {
                                        string? fileName = Path.GetDirectoryName(data.Path);
                                        if (fileName == null)
                                            return [];
                                            
                                        try 
                                        {
                                            fileName = char.ToUpper(fileName[0]) + fileName[1..];
                                        }
                                        catch (IndexOutOfRangeException ex)
                                        {
                                            Console.WriteLine($"[Error] : couldn't get filename: {fileName} from path: '{data.Path}'");
                                            throw;
                                        }
                                        
                                        return [
                                            new UICol(Class(w_full, h_full, hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout), [
                                                new UIImg(Class(h_[22], w_[22], left_[7], middle_left, icon_[data.Icon], slice_null, rgb_[0.45f, 0.60f, 0.75f]))
                                            ]),
                                            new UIText(fileName, Class(mc_[fileName.Length], fs_[1.2f], middle_left, left_[40]))
                                        ];
                                    })))
                                )
                            ]))
                        ]))
                    )
                ]), ref _defaultPaths),
                new UIVCol(Class(w_full_minus_[150], h_full), Sub(
                    new UICol(Class(w_full, h_[40], border_ui_[0, 0, 0, 2], border_color_g_[30]), Sub(
                        newCol(Class(w_[25], h_[25], border_ui_[2, 2, 2, 2], bottom_[1], border_color_g_[!_filesAsList ? 40 : 30], rgba_[0.3f, 0.3f, 0.3f, !_filesAsList ? 1f : 0f], blank_full, middle_right, right_[40], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                        OnClickCol(_ =>
                        {
                            _filesAsList = false;
                            HoverExitIconButton(_listButton);
                            GenerateFiles();
                        }),
                        OnHoverEnter(HoverEnterIconButton),
                        OnHoverExit(c => { if (_filesAsList) HoverExitIconButton(c); }),
                        Sub(
                            new UIImg(Class(icon_[38], w_[20], h_[20], gray_[!_filesAsList ? 70 : 50], middle_center))
                        ), ref _gridButton),
                        newCol(Class(w_[25], h_[25], border_ui_[2, 2, 2, 2], bottom_[1], border_color_g_[_filesAsList ? 40 : 30], rgba_[0.3f, 0.3f, 0.3f, _filesAsList ? 1f : 0f], blank_full, middle_right, right_[10], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                        OnClickCol(_ =>
                        {
                            _filesAsList = true;
                            HoverExitIconButton(_gridButton);
                            GenerateFiles();
                        }),
                        OnHoverEnter(HoverEnterIconButton),
                        OnHoverExit(c => { if (!_filesAsList) HoverExitIconButton(c); }),
                        Sub(
                            new UIImg(Class(icon_[14], w_[20], h_[20], gray_[!_filesAsList ? 70 : 50], middle_center))
                        ), ref _listButton)
                    )),
                    newVScroll(Class(w_full, h_full_minus_[40], mask_children, border_[0, 5, 0, 5], spacing_[5]), Sub(), ref _folders)
                ))
            )),
            new UICol(Class(w_full_minus_[4], top_center, h_[40], border_ui_[0, 2, 0, 0], border_color_g_[30]), Sub(
                newText($"{CurrentPaths.Count} ITEMS", Class(mc_[15], fs_[1f], middle_left, left_[12]), ref _elementCountText),
                new UIText($"FILE NAME", Class(mc_[9], fs_[1f], middle_right, right_[340])),
                newCol(Class(w_full, h_full), [
                    new UIHScroll(Class(w_[300], middle_right, right_[35], h_[25], border_ui_[2, 2, 2, 2], border_color_g_[30], blank_full_g_[10], mask_children), Sub(
                        newField("name", Class(mc_[100], fs_[1f], middle_left, left_[5]), ref _fileNameField)
                    )),
                    new UICol(Class(w_[25], h_[25], border_ui_[2, 2, 2, 2], border_color_g_[30], blank_full, middle_right, right_[5], hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout),
                    OnClickCol(_ => {
                        var path = GetSaveFilePath();
                        if (path != null)
                            Manager.SaveFile(path);
                    }),
                    OnHoverEnter(HoverEnterIconButton),
                    OnHoverExit(HoverExitIconButton),
                    Sub(
                        new UIImg(Class(icon_[42], w_[20], h_[20], gray_[50], middle_center))
                    ))
                ], ref _fileNameCollection)
            ))
        ))
    ));
    
    public string? GetSaveFilePath()
    {
        var fileName = _fileNameField.GetTrimmedText() + Manager.FileType;
        return string.IsNullOrEmpty(fileName) ? null : Path.Combine(CurrentPath, fileName);
    }

    public void Move(UICol _)
    {
        Manager.IsHovering = true;
        Vector2 mouseDelta = Input.GetMouseDelta();
        if (mouseDelta == Vector2.Zero || Element.UIController == null)
            return;

        Element.UIController.SetPosition(Element.UIController.Position.Xy + mouseDelta);
    }

    public void GenerateFiles(string path)
    {
        if (Directory.Exists(path))
        {
            CurrentPath = path;
            GenerateFiles();
        }
    }

    public void SetFieldAsWriteName()
    {
        _writeName = true;
        PathField.UpdateText("");
        UIController.SetInputfield(PathField);
    }

    public void SetFieldAsPath()
    {
        _writeName = false;
        PathField.UpdateText(CurrentPath);
        UIController.RemoveInputfield();
    }

    public void GenerateFiles()
    {
        _folders.ScrollPosition = 0;
        if (Created)
            _folders.DeleteChildren();

        CurrentPaths = [];
        Manager.SelectedFiles = [];
        _selectedButtons = [];

        string[] directories;
        string[] files;

        try
        {
            directories = Directory.GetDirectories(CurrentPath);
            files = Directory.GetFiles(CurrentPath);
        }
        catch (System.Exception)
        {
            return;
        }

        Array.Sort(directories);
        Array.Sort(files);

        List<string> allFiles = [];

        for (int i = 0; i < directories.Length; i++)
        {
            string? directoryName = Path.GetFileName(directories[i]);
            if (directoryName != null)
            {
                CurrentPaths.Add(directories[i]);
                allFiles.Add(directoryName);
            }
        }

        int directoryCount = allFiles.Count;

        for (int i = 0; i < files.Length; i++)
        {
            string? fileName = Path.GetFileName(files[i]);
            if (fileName != null)
            {
                CurrentPaths.Add(files[i]);
                allFiles.Add(fileName);
            }
        }

        int elementCount = allFiles.Count;
        
        UIElementBase[] collections;

        if (!_filesAsList)
        {
            int hCount = 5;
            int height = Mathf.CeilToInt((float)elementCount / (float)hCount);
            int current = 0;
            collections = new UIElementBase[height];

            for (int y = 0; y < height; y++)
            {
                UIHCol collection = new(Class(w_full_minus_[5], top_center, h_[70], border_[2.5f, 0, 0, 0], spacing_[5]));

                for (int x = 0; x < hCount; x++)
                {
                    if (current == elementCount)
                        break;

                    string file = allFiles[current];
                    int icon = 19;
                    if (current >= directoryCount)
                    {
                        if (!_iconMap.TryGetValue(Path.GetExtension(file), out icon))
                            icon = 20;
                    }

                    var element = new UICol(Class(w_minus_[100f / (float)hCount, 5], h_[70], data_["path", CurrentPaths[current]], blank_sharp),
                    OnClick(current < directoryCount ? ViewFolder : ClickFile),
                    OnHoverEnterCol(c => c.UpdateColor((0.3f, 0.3f, 0.3f, 1f))),
                    OnHoverExitCol(c => { if (!_selectedButtons.Contains(c)) c.UpdateColor((0f, 0f, 0f, 0f)); }),
                    Sub([
                        new UICol(Class(w_full, h_full, hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout), Sub(
                            new UIImg(Class(w_[40], h_[40], top_center, top_[5], icon_[icon], bg_white))
                        )),
                        new UIText(file, Class(mc_[Mathf.Min(17, file.Length)], fs_[1f], bottom_center, bottom_[15])),
                        ..If(file.Length > 17, () => {
                            string next = file.Substring(17, Mathf.Min(17, file.Length - 17));
                            return new UIText(next, Class(mc_[next.Length], fs_[1f], bottom_center, bottom_[5]));
                        })
                    ]));
                    collection.AddElement(element);

                    current++;
                }

                collections[y] = collection;

                if (current == elementCount)
                    break;
            }
        }
        else
        {
            collections = new UIElementBase[elementCount];

            for (int i = 0; i < elementCount; i++)
            {
                string file = allFiles[i];
                int icon = 19;
                if (i >= directoryCount)
                {
                    if (!_iconMap.TryGetValue(Path.GetExtension(file), out icon))
                        icon = 20;
                }

                collections[i] = new UICol(Class(w_full_minus_[10], top_center, h_[30], blank_sharp),
                OnClick(i < directoryCount ? ViewFolder : ClickFile),
                OnHoverEnterCol(c => c.UpdateColor((0.3f, 0.3f, 0.3f, 1f))),
                OnHoverExitCol(c => { if (!_selectedButtons.Contains(c)) c.UpdateColor((0f, 0f, 0f, 0f)); }),
                Sub(
                    new UICol(Class(w_full, h_full, hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout), Sub(
                        new UIImg(Class(middle_left, left_[5], icon_[icon], h_[25], w_[25], bg_white))
                    )),
                    new UIText(file, Class(mc_[Mathf.Min(83, file.Length)], fs_[1f], middle_left, left_[40]))
                ));
            }
        }

        _folders.AddElements(collections);
        if (Created)
        {
            _elementCountText.UpdateText($"{elementCount} ITEMS");
            _folders.UIController?.AddElements(collections);
        }
    }

    private void HoverEnterIconButton(UICol c)
    {
        c.UpdateColor((0.3f, 0.3f, 0.3f, 1f));
        c.UpdateBorderColor((0.4f, 0.4f, 0.4f, 1f));
        c.GetElement<UIImg>()?.UpdateColor((0.7f, 0.7f, 0.7f, 1f));
    }

    private void HoverExitIconButton(UICol c)
    {
        c.UpdateColor((0f, 0f, 0f, 0f));
        c.UpdateBorderColor((0.3f, 0.3f, 0.3f, 1f));
        c.GetElement<UIImg>()?.UpdateColor((0.5f, 0.5f, 0.5f, 1f));
    }

    private void ViewFolder(UICol collection)
    {
        string path = collection.Dataset.String("path");
        if (_time + 0.2f > GameTime.TotalTime && _clickedFolderPath == path)
        {
            collection.UpdateColor((0f, 0f, 0f, 0f));
            CurrentPath = path;
            Manager.AddToHistory(path);
            GenerateFiles();
            _time = 0;
        }
        else
        {
            foreach (var button in _selectedButtons)
            {
                button.UpdateColor((0f, 0f, 0f, 0f));
            }
            _selectedButtons = [];
            
            if (!_selectedButtons.Remove(collection))
            {
                _selectedButtons.Add(collection);
                collection.UpdateColor((0.3f, 0.3f, 0.3f, 1f));
            }
            else
            {
                collection.UpdateColor((0f, 0f, 0f, 0f));
            }

            _time = GameTime.TotalTime;
            _clickedFolderPath = path;
        }
    }
    
    private void ClickFile(UICol collection)
    {
        string path = collection.Dataset.String("path");
        _fileNameField.UpdateText(Path.GetFileNameWithoutExtension(path));

        if (!Input.IsKeyDown(Key.ShiftLeft))
        {
            Manager.SelectedFiles = [];
            foreach (var button in _selectedButtons)
            {
                button.UpdateColor((0f, 0f, 0f, 0f));
            }
            _selectedButtons = [];
        }
        
        if (!_selectedButtons.Remove(collection))
        {
            Manager.SelectedFiles.Add(path);
            _selectedButtons.Add(collection);
            collection.UpdateColor((0.3f, 0.3f, 0.3f, 1f));
        }
        else
        {
            Manager.SelectedFiles.Remove(path);
            collection.UpdateColor((0f, 0f, 0f, 0f));
        }
    }


    private struct Section(string name, SectionPath[] paths)
    {
        public string Name = name;
        public SectionPath[] Paths = paths;
        public static implicit operator Section((string name, SectionPath[] paths) data) => new Section(data.name, data.paths);
    }
    private struct SectionPath(string path, int icon)
    {
        public string Path = path;
        public int Icon = icon;
        public static implicit operator SectionPath((string path, int icon) data) => new SectionPath(data.path, data.icon);
    }
    private static readonly Section[] _knownDirectories =
    {
        (
            "PROJECT FOLDERS",
            [
                (Path.Combine(Game.MainPath, "custom", "models"), 31),
                (Path.Combine(Game.MainPath, "custom", "textures"), 21),
                (Path.Combine(Game.MainPath, "custom", "animations"), 32),
                (Path.Combine(Game.MainPath, "custom", "nodes"), 34),
                (Path.Combine(Game.MainPath, "custom", "groups"), 35),
                (Path.Combine(Game.MainPath, "custom", "audio"), 33)
            ]
        ),
        (
            "SYSTEM",
            [
                (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 36),
                (Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 37),
                (Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 19),
                (Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), 21),
                (Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), 32),
                (Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), 33),
            ]
        ),
        (
            "APPDATA",
            [
                (Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 19),
                (Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 19),
                (Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 19),
                (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Cache"), 19),
                (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Logs"), 20),
            ]
        ),
    };

    private Dictionary<string, int> _iconMap = new()
    {
        { ".png", 21 },
        { ".jpg", 21 },
        { ".jpeg", 21 },
        { ".bmp", 21 },
        { ".tga", 21 },
        { ".gif", 21 },
        { ".tiff", 21 },
        { ".webp", 21 },
        { ".dds", 21 },
        { ".hdr", 21 },

        { ".mp4", 32 },
        { ".avi", 32 },
        { ".mov", 32 },
        { ".mkv", 32 },
        { ".wmv", 32 },
        { ".flv", 32 },
        { ".webm", 32 },
        { ".mpeg", 32 },
        { ".mpg", 32 },

        { ".mp3", 33 },
        { ".wav", 33 },
        { ".ogg", 33 },
        { ".flac", 33 },
        { ".aac", 33 },
        { ".m4a", 33 },
        { ".wma", 33 },
        { ".aiff", 33 },
        { ".mid", 33 },

        { ".obj", 31 },
        { ".fbx", 31 },
        { ".glb", 31 },
        { ".gltf", 31 },
        { ".blend", 31 },
        { ".stl", 31 },
        { ".ply", 31 },
        { ".3ds", 31 },
        { ".dae", 31 },
        { ".x", 31 },

        { ".exe", 30 },
        { ".bat", 30 },
        { ".sh", 30 },
        { ".cmd", 30 },
        { ".dll", 30 },
        { ".so", 30 },
        { ".ini", 30 },
        { ".cfg", 30 },
        { ".config", 30 },
        { ".sys", 30 },
        { ".dat", 30 },
        { ".json", 30 },
        { ".yaml", 30 },
        { ".yml", 30 },
        { ".toml", 30 },
        { ".manifest", 30 },
        { ".prefs", 30 },
        { ".settings", 30 },
        { ".properties", 30 },
        { ".env", 30 },
        { ".reg", 30 },
        { ".service", 30 },
    };
}