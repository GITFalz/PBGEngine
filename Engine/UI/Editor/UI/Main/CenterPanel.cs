using PBG.Core;
using PBG.Data;
using PBG.Files;
using PBG.MathLibrary;
using PBG.Modeling;
using PBG.Rendering;
using PBG.UI;

using static PBG.Editor.FolderMenu;
using static PBG.UI.Styles;

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

    public UIText MsText = null!;
    public UIGraph MsGraph = null!;

    public UIText DrawCallText = null!;
    public UIGraph DrawCallGraph = null!;

    public UIElementBase CenterPanel =>
    new UICol().Class(w_full_minus_[480], h_full_minus_[30], top_left, left_[240], top_[30])[
        new UIHCol().Class(w_full, h_[30], blank_full, rgba_v4_[Bg2], border_[10, 0, 10, 0], border_ui_[0, 2, 0, 2], border_color_[Border], spacing_[10])[
            new UIImg().Ref(ref _playIcon).Class(middle_left, icon_[0], w_[20], h_[20], rgba_v4_[Text1]).OnClick(Play),
            new UIImg().Class(middle_left, icon_[9], w_[20], h_[20], rgba_v4_[Text1]).OnClick(_ => Stop())
        ],
        new UICol().Class(w_full, h_full_minus_[180], top_[30]).OnHoverEnter(_ => HoveringCenter = true).OnHoverExit(_ => HoveringCenter = false)[
            new UIVCol().Class(top_right, w_[300], grow_children, border_[5, 5, 5, 5], top_[10], right_[10], rgba_[Bg1.X, Bg1.Y, Bg1.Z, 0.5f], blank_sharp, spacing_[10])[
                new UICol().Class(w_full, h_[10])[
                    new UIText("Fps").Class(fs_[1.2f]),
                    new UIText("0").Ref(ref FPSText).Class(top_right, mc_[10], text_align_right)
                ],
                new UICol().Class(w_full, h_[10])[
                    new UIText("Ms").Class(fs_[1.2f]),
                    new UIText("0").Ref(ref MsText).Class(top_right, mc_[5], text_align_right)
                ],
                new UIGraph().Ref(ref MsGraph).Class(w_full, h_[30], rgba_[1, 0, 0, 1], graph_points_[50]),
                new UICol().Class(w_full, h_[10])[
                    new UIText("Draw Calls").Class(fs_[1.2f]),
                    new UIText("0").Ref(ref DrawCallText).Class(top_right, mc_[5], text_align_right)
                ],
                new UIGraph().Ref(ref DrawCallGraph).Class(w_full, h_[30], rgba_[0, 1, 0, 1], graph_points_[50])          
            ]
            
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
            SceneDefinition.ActiveScene?.SetCamera(EditorManager.Instance.Camera);
        }
        else
        {
            EditorManager.SetEditorState(EditorState.Playing);
            _playIcon.UpdateIconIndex(8);
            _playing = true;
            SceneDefinition.ActiveScene?.SetGameCamera();
        }

        if (wasStoppped)
        {
            GameTime.Reset();   
            var scene = SceneDefinition.CreateScene();
            Scene.LoadScene(scene.Name);
            scene.SetGameCamera();
            if (_selectedNode != null)
                RefreshInspectorPanel(_selectedNode, _selectedNode.Transform);

            if (SceneDefinition.Active != null && SceneDefinition.ActiveScene != null)
                ReloadSceneHierarchy(SceneDefinition.Active, SceneDefinition.ActiveScene);
        }
    }

    public void Stop()
    {
        if (EditorManager.Stopped)
            return;

        _playing = false;
        _started = false;
        _playIcon.UpdateIconIndex(0);
        EditorManager.SetEditorState(EditorState.Stopped);
        SceneDefinition.ActiveScene?.SetCamera(EditorManager.Instance.Camera);
        if (_selectedNode != null)
            RefreshInspectorPanel(_selectedNode, _selectedNode.Transform);

        ReloadSceneHierarchy(SceneDefinition.Blueprint, SceneDefinition.BlueprintScene);
    }

    public void Reset()
    {
        FolderHover = false;
        FolderIconHover = false;
        FolderIcon = null;
    }
}

