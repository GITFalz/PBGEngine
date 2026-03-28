using PBG.UI;
using PBG.UI.Creator;
using static PBG.UI2.Styles;
using static PBG.Editor.EditorUI;
using PBG.Data;

using PBG.MathLibrary;
using System.Diagnostics;

namespace PBG.Editor;

public class FolderMenu : UIScript
{
    public static FolderMenu Instance = null!;
    public bool FolderHover = false;

    private MenuSection _menuSection = MenuSection.BaseOptions;

    private UIElementBase? _currentMenu = null;

    private UIVCol _baseOptions = null!;
    private UIVCol _folderOptions = null!;
    private UIVCol _fileOptions = null!;
    private UIVCol _scriptOptions = null!;

    private UIVCol _folderNameSection = null!;
    private UIField _folderNameField = null!;

    private UIVCol _fileNameSection = null!;
    private UIField _fileNameField = null!;

    public FolderMenu() { Instance = this; }

    public override UIElementBase Script() =>
    new UIVScroll().Class(w_[200], h_[200], depth_[20], blank_full, rgba_v4_[Bg3], hidden, ignore_invisible)
    .OnHoverEnter(_ => { FolderHover = true; })
    .OnHover(HoverMenu)
    .OnHoverExit(_ => { 
        FolderHover = false; 
        Close();
    })[
        new UIVCol().Class(w_full, grow_children, hidden, not_toggle_old_invisible)[
            new UICol().Class(w_full, h_[15]).OnClick(_ => AddFolderBtn())[
                new UIText("add folder").Class(middle_left, left_[5])
            ],
            new UICol().Class(w_full, h_[15]).OnClick(_ => AddFileBtn())[
                new UIText("add file").Class(middle_left, left_[5])
            ]
        ].Ref(ref _baseOptions),
        new UIVCol().Class(w_full, grow_children, hidden, not_toggle_old_invisible)[
            new UICol().Class(w_full, h_[15]).OnClick(_ => DeleteFolder(EditorUI.Instance.FolderIcon))[
                new UIText("delete folder").Class(middle_left, left_[5])
            ]
        ].Ref(ref _folderOptions),
        new UIVCol().Class(w_full, grow_children, hidden, not_toggle_old_invisible)[
            new UICol().Class(w_full, h_[15]).OnClick(_ => DeleteFile(EditorUI.Instance.FolderIcon))[
                new UIText("delete file").Class(middle_left, left_[5])
            ]
        ].Ref(ref _fileOptions),
        new UIVCol().Class(w_full, grow_children, hidden, not_toggle_old_invisible)[
            new UICol().Class(w_full, h_[15]).OnClick(_ => DeleteFile(EditorUI.Instance.FolderIcon))[
                new UIText("delete file").Class(middle_left, left_[5])
            ],
            new UICol().Class(w_full, h_[15]).OnClick(_ => DeleteFile(EditorUI.Instance.FolderIcon))[
                new UIText("hot reload").Class(middle_left, left_[5])
            ]
        ].Ref(ref _scriptOptions),
        new UIVCol().Class(w_full, grow_children, hidden, not_toggle_old_invisible)[
            new UICol().Class(w_full, h_[15])[
                new UIText("name").Class(middle_left, left_[5])
            ],
            new UICol().Class(w_full, h_[15])[
                new UIField("").Class(middle_left, left_[5], mc_[20]).Ref(ref _folderNameField)
            ]
        ].Ref(ref _folderNameSection),
        new UIVCol().Class(w_full, grow_children, hidden, not_toggle_old_invisible)[
            new UICol().Class(w_full, h_[15])[
                new UIText("name").Class(middle_left, left_[5])
            ],
            new UICol().Class(w_full, h_[15])[
                new UIField("").Class(middle_left, left_[5], mc_[20]).Ref(ref _fileNameField)
            ]
        ].Ref(ref _fileNameSection)
    ];

    public void Open(MenuSection menuSection)
    {
        if (menuSection == MenuSection.FolderOptions)
        {
            _currentMenu = _folderOptions;
        }
        else if (menuSection == MenuSection.FileOptions)
        {
            _currentMenu = _fileOptions;
        }
        else
        {
            _currentMenu = _baseOptions;
        }

        Element.SetVisible(true);
        _currentMenu.SetVisible(true);
        Element.BaseOffset = Mathf.Clampy(Input.MousePosition, (0, 0), (Game.Width - Element.Size.X, Game.Height - Element.Size.Y));
        Element.ApplyChanges(UIChange.Transform);
    }

    public void Close()
    {
        _currentMenu?.SetVisible(false);
        _currentMenu = null;
        Element.SetVisible(false);
        _menuSection = MenuSection.BaseOptions;
        FolderHover = false;
    }

    private void HoverMenu(UIVScroll col)
    {
        if (Input.IsKeyPressed(Key.Enter))
        {
            if (_menuSection == MenuSection.AddFolder)
            {
                var folderName = _folderNameField.GetTrimmedText();
                if (folderName.Length == 0)
                {
                    Close();
                    return;
                }

                EditorUI.Instance.CurrentPath.CreateDirectory(folderName);
                EditorUI.Instance.GenerateCurrentFolderFiles();
                Close();
                return;
            }
            else if (_menuSection == MenuSection.AddFile)
            {
                var fileName = _fileNameField.GetTrimmedText();
                if (fileName.Length == 0)
                {
                    Close();
                    return;
                }

                EditorUI.Instance.CurrentPath.CreateFile(fileName);
                EditorUI.Instance.GenerateCurrentFolderFiles();
                Close();
                return;
            }
        }
    }

    private void AddFolderBtn()
    {
        _menuSection = MenuSection.AddFolder;
        _baseOptions.SetVisible(false);
        _folderNameSection.SetVisible(true);
        _folderNameField.UpdateText("");
        _currentMenu = _folderNameSection;
    }

    private void AddFileBtn()
    {
        _menuSection = MenuSection.AddFile;
        _baseOptions.SetVisible(false);
        _fileNameSection.SetVisible(true);
        _fileNameField.UpdateText("");
        _currentMenu = _fileNameSection;
    }

    private void DeleteFolder(UICol? col)
    {
        if (col == null)
        {
            Console.WriteLine("No collection provided to delete the folder");
            Close();
            return;
        }

        var path = col.Dataset.String("path");
        if (path == null)
        {
            Console.WriteLine("No path provided to delete the folder");
            Close();
            return;
        }

        if (Directory.Exists(path))
        {
            Directory.Delete(path);
            EditorUI.Instance.GenerateCurrentFolderFiles();
        }

        Close();
    }

    private void DeleteFile(UICol? col)
    {
        if (col == null)
        {
            Console.WriteLine("No collection provided to delete the file");
            Close();
            return;
        }

        var path = col.Dataset.String("path");
        if (path == null)
        {
            Console.WriteLine("No path provided to delete the file");
            Close();
            return;
        }

        if (File.Exists(path))
        {
            EditorUI.Instance.CurrentPath.DeleteFile(Path.GetFileNameWithoutExtension(path));
            EditorUI.Instance.GenerateCurrentFolderFiles();
        }

        Close();
    }

    public void OpenInVSCode(string filePath, int line = 0, int column = 0)
    {
        try
        {
            string args = line > 0
                ? $"--goto \"{filePath}:{line}:{column}\""
                : $"\"{filePath}\"";

            Process.Start(new ProcessStartInfo
            {
                FileName        = "code",
                Arguments       = args,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not open VSCode: {ex.Message}");
        }
    }

    public void ReloadFile(UICol? col)
    {
        if (col == null)
        {
            Console.WriteLine("No collection provided to reload the file");
            Close();
            return;
        }

        var path = col.Dataset.String("path");
        if (path == null)
        {
            Console.WriteLine("No path provided to reload the file");
            Close();
            return;
        }

        if (!File.Exists(path))
        {
            Console.WriteLine("file doesn't exist");
            Close();
            return;
        }

        if (Path.GetExtension(path) == ".cs")
        {
            
        }

        Close();
    }

    public enum MenuSection
    {
        BaseOptions,
        FileOptions,
        FolderOptions,
        AddFolder,
        AddFile
    }
}