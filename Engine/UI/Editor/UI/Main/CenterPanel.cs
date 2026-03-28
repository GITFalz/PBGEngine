using PBG.Core;
using PBG.Data;
using PBG.Files;
using PBG.MathLibrary;
using PBG.Modeling;
using PBG.Rendering;
using PBG.UI;

using static PBG.Editor.FolderMenu;
using static PBG.UI2.Styles;

namespace PBG.Editor;

public partial class EditorUI
{
    public EditorFile CurrentPath = new();


    public UIHScroll FolderPanel = null!;
    public bool FolderHover = false;
    public bool FolderIconHover = false;
    public bool IsFolder = false;
    public UICol? FolderIcon = null;
    public UICol? ClickedIcon = null;
    public float ClickedTimer = 0;

    private UIImg _playIcon = null!;
    private bool _started = false;
    private bool _playing = false;

    private bool HoveringCenter = false;

    public UIElementBase CenterPanel =>
    new UICol().Class(w_full_minus_[480], h_full_minus_[30], top_left, left_[240], top_[30])[
        new UIHCol().Class(w_full, h_[30], blank_full, rgba_v4_[Bg2], border_[10, 0, 10, 0], border_ui_[0, 2, 0, 2], border_color_[Border], spacing_[10])[
            new UIImg().Ref(ref _playIcon).Class(middle_left, icon_[0], w_[20], h_[20], rgba_v4_[Text1]).OnClick(Play),
            new UIImg().Class(middle_left, icon_[9], w_[20], h_[20], rgba_v4_[Text1]).OnClick(_ => Stop())
        ],
        new UICol().Class(w_full, h_full_minus_[180], top_[30]).OnHoverEnter(_ => HoveringCenter = true).OnHoverExit(_ => HoveringCenter = false),
        new UICol().Class(w_full, h_[150], bottom_center, blank_full, rgba_v4_[Bg1])[
            new UICol().Class(w_full, h_[30], blank_full, rgba_v4_[Bg2], border_ui_[0, 2, 0, 2], border_color_[Border])[
                new UIImg().Class(icon_[6], middle_left, left_[10], w_[20], h_[20], rgba_v4_[Text1]).OnClick(_ => PreviousFolder())
            ],
            new UIHScroll().Ref(ref FolderPanel).Class(w_full, h_[120], bottom_center, mask_children, border_[15, 15, 15, 0], spacing_[15], scroll_speed_[20])
            .OnHover(_ => FolderPanelHover())
            .OnHoverExit(_ => FolderPanelHoverExit()),
            new Run(GenerateCurrentFolderFiles)
        ]
    ];

    public void Play(UIImg img)
    {
        bool wasStoppped = EditorManager.Stopped;
        if (_playing)
        {
            EditorManager.SetEditorState(EditorState.Paused);
            _playIcon.UpdateIconIndex(0);
            _playing = false;
            SceneBlueprint.ActiveScene?.SetCamera(EditorManager.Instance.Camera);
        }
        else
        {
            EditorManager.SetEditorState(EditorState.Playing);
            _playIcon.UpdateIconIndex(8);
            _playing = true;
            SceneBlueprint.ActiveScene?.SetGameCamera();
        }

        if (wasStoppped)
        {
            GameTime.Reset();   
            var scene = SceneBlueprint.CreateScene();
            Scene.LoadScene(scene.Name);
            scene.SetGameCamera();
            if (_selectedNode != null)
                RefreshInspectorPanel(_selectedNode);
        }
    }

    public void Stop()
    {
        _playing = false;
        _started = false;
        _playIcon.UpdateIconIndex(0);
        EditorManager.SetEditorState(EditorState.Stopped);
        SceneBlueprint.ActiveScene?.SetCamera(EditorManager.Instance.Camera);
        if (_selectedNode != null)
            RefreshInspectorPanel(_selectedNode);
    }

    public void Reset()
    {
        FolderHover = false;
        FolderIconHover = false;
        FolderIcon = null;
    }

    public void FolderPanelHover()
    {
        FolderHover = true;
        if (Input.IsMousePressed(MouseButton.Right) && !FolderMenu.Instance.FolderHover)
        {
            if (FolderIconHover)
            {
                FolderMenu.Instance.Open(IsFolder ? MenuSection.FolderOptions : MenuSection.FileOptions);
            }
            else
            {
                FolderMenu.Instance.Open(MenuSection.BaseOptions);
            }
        }
    }

    public void FolderPanelHoverExit()
    {
        FolderHover = false;
        if (!FolderMenu.Instance.FolderHover)
        {
            FolderMenu.Instance.Close();
        }
    }

    public void PreviousFolder()
    {
        if (CurrentPath.Parent != null)
        {
            CurrentPath = CurrentPath.Parent;
            GenerateCurrentFolderFiles();
        }
    }

    public void GenerateCurrentFolderFiles()
    {
        FolderPanel.DeleteChildren();
        
        var directories = CurrentPath.GetDirectories();
        var files = CurrentPath.GetFiles();

        var elements = new UICol[directories.Length + files.Length];

        void HoverFile(UICol col, bool isFolder)
        {
            IsFolder = isFolder;
            if (!FolderMenu.Instance.FolderHover) 
            { 
                FolderIconHover = true; 
                FolderIcon = col; 
            }
        }

        void HoverExitFile(UICol col)
        {
            if (!FolderMenu.Instance.FolderHover) 
            { 
                FolderIconHover = false; FolderIcon = null; 
            }
        }

        void OnClickFile(UICol col)
        {
            FileMover.Instance.Show();

            if (ClickedTimer < GameTime.TotalTime - 0.3f)
            {
                ClickedIcon = null;
            }
            
            if (ClickedIcon == null)
            {
                ClickedTimer = GameTime.TotalTime;
                ClickedIcon = col;
            }
            else if (ClickedIcon == col && ClickedTimer >= GameTime.TotalTime - 0.3f)
            {
                var path = col.Dataset.String("path");
                if (path != null && Path.GetExtension(path).Equals(".cs", StringComparison.CurrentCultureIgnoreCase))
                    FolderMenu.Instance.OpenInVSCode(path);

                if (path != null && Path.GetExtension(path).Equals(".json", StringComparison.CurrentCultureIgnoreCase))
                {
                    try
                    {
                        if (SceneSerializer.Deserialize(path))
                        {
                            SceneBlueprint.BlueprintScene?.UpdatePending();
                            EditorManager.ResetScene();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load scene, serializer threw '{ex.Message}'");
                        throw;
                    }
                }

                ClickedIcon = null;
            }
            else
            {
                ClickedIcon = null;
            }
        }

        int i = 0;
        foreach (var directory in directories)
        {
            string fileName = Path.GetFileName(directory);
            elements[i] = new UICol().Class(data_["path", directory], data_["name", fileName], w_[90], h_[90], top_left, blank_sharp, rgba_v4_[Bg2], hover_color_[Bg2, Bg4], hover_color_easeout, hover_color_duration_[0.5f])
            .OnClick(col =>
            {
                var name = col.Dataset.String("name");
                var file = CurrentPath.GetFile(name);
                if (file != null)
                {
                    CurrentPath = file;
                    GenerateCurrentFolderFiles();
                }
                else
                {
                    Console.WriteLine($"[Warning] : Directory '{name}' not found");
                }
            })
            .OnHover(c => HoverFile(c, true))
            .OnHoverExit(HoverExitFile)
            [
                new UIImg().Class(w_[50], h_[50], top_center, top_[10], icon_[19], rgba_v4_[Text1]),
                new UIText(fileName).Class(bottom_center, bottom_[fileName.Length > 12 ? 15 : 5], mc_[12.Min(fileName.Length)], rgba_v4_[Text1])
            ];
            if (fileName.Length > 12)
            {
                string trimmed = fileName[12..];
                elements[i].AddElement(new UIText(trimmed).Class(bottom_center, bottom_[5], mc_[12.Min(trimmed.Length)], rgba_v4_[Text1]));
            }
            i++;
        }

        foreach (var file in files)
        {
            string fileName = Path.GetFileName(file);
            elements[i] = new UICol().Class(data_["path", file], data_["name", Path.GetFileNameWithoutExtension(file)], w_[90], h_[90], top_left, blank_sharp, rgba_v4_[Bg2], hover_color_[Bg2, Bg4], hover_color_easeout, hover_color_duration_[0.5f]) 
            .OnClick(col => { OnClickFile(col); FileMover.Instance.SetText(fileName); })
            .OnHover(c => HoverFile(c, false))
            .OnHold(_ => FileMover.Instance.SetPosition(Input.MousePosition - (45, 45)))
            .OnRelease(OnFileReleased)
            .OnHoverExit(HoverExitFile)
            [
                new UIImg().Class(w_[50], h_[50], top_center, top_[10], icon_[20], rgba_v4_[Text1]),
                new UIText(fileName).Class(bottom_center, bottom_[fileName.Length > 12 ? 15 : 5], mc_[12.Min(fileName.Length)], rgba_v4_[Text1])
            ];
            if (fileName.Length > 12)
            {
                string trimmed = fileName[12..];
                elements[i].AddElement(new UIText(trimmed).Class(bottom_center, bottom_[5], mc_[12.Min(trimmed.Length)], rgba_v4_[Text1]));
            }
            i++;
        }

        FolderPanel.AddElements(elements);
        UIController?.AddElements(elements);

        Reset();
    }

    private void OnFileReleased(UICol c)
    {
        FileMover.Instance.Hide();
        string path = c.Dataset.String("path");
        string ext = Path.GetExtension(path);
        if (HoveringOverInspector && EditorManager.SelectedNode != null && ext == ".cs")
        {
            EditorManager.SelectedNode.AddScript(c.Dataset.String("name"));
            RefreshInspectorPanel(EditorManager.SelectedNode);
        }

        if (HoveringCenter && ext == ".obj")
        {
            Console.WriteLine($"Loading model at path: '{path}'");

            var mesh = new MeshRenderer();
            var modelNode = SceneBlueprint.AddOrGetNode("Models");

            var name = c.Dataset.String("name");
            var node = modelNode.AddNode(name);

            node.AddScript(mesh);
            node.OrderScripts();

            SceneBlueprint.BlueprintScene.UpdatePending();
            ObjLoader.LoadMesh(path, mesh);

            ReloadSceneHierarchy();
        }
    }
}

