using PBG.Core;
using PBG.UI;
using static PBG.UI2.Styles;

namespace PBG.Editor;

public partial class EditorUI
{
    private UIVScroll _sceneHierarchy = null!;

    public UIElementBase LeftPanel =>
    new UIVCol().Class(w_[240], h_full_minus_[30], blank_full, rgba_v4_[Bg1], bottom_left, border_ui_[0, 0, 2, 0], border_color_[Border])[
        new UICol().Class(w_full_minus_[2], h_[30], blank_full, rgba_v4_[Bg2], border_ui_[0, 2, 0, 2], border_color_[Border])[
            new UIText("HIERARCHY").Class(middle_left, left_[10], fs_[1.2f], rgba_v4_[Text2]),
            new UIImg().Class(w_[20], h_[20], icon_[16], middle_right, right_[5], rgba_v4_[Text2])
        ],
        new UIVScroll().Class(w_full_minus_[2], h_full_minus_[30], mask_children).Ref(ref _sceneHierarchy)
    ];

    public void ReloadSceneHierarchy()
    { 
        _sceneHierarchy.DeleteChildren();

        var childCollection = new UIVCol().Class(w_full, h_[20], ignore_invisible);
        CreateNodeUI(1, childCollection, SceneBlueprint.BlueprintScene.RootNode);

        _sceneHierarchy.AddElement(childCollection);
        _sceneHierarchy.UIController?.AddElement(childCollection);
    }

    private void CreateNodeUI(int offset, UIVCol collection, RootNode node)
    {
        var childCollection = new UIVCol().Class(w_full, h_[20], not_toggle_old_invisible);
        var element = new UIVCol().Class(w_full, h_[20], left_[offset * 5], spacing_[10], ignore_invisible)[
            new UIText(node.Name).Class(mc_[20], middle_left, left_[5]),
            childCollection
        ];
        for (int i = 0; i < SceneBlueprint.ChildrenNodes.Count; i++)
        {
            var child = SceneBlueprint.ChildrenNodes[i];
            CreateNodeUI(offset + 1, childCollection, child);
        }
        collection.AddElement(element);
    }

    private void CreateNodeUI(int offset, UIVCol collection, SceneBlueprintNode blueprint)
    {
        var childCollection = new UIVCol().Class(w_full, h_[20], not_toggle_old_invisible);
        var element = new UIVCol().Class(w_full, h_[20], left_[offset * 5], spacing_[10], ignore_invisible)
        .OnClick(_ => ClickNode(blueprint))[
            new UIText(blueprint.Name).Class(mc_[20], middle_left, left_[5]),
            childCollection
        ];
        for (int i = 0; i < blueprint.ChildrenNodes.Count; i++)
        {
            var child = blueprint.ChildrenNodes[i];
            CreateNodeUI(offset + 1, childCollection, child);
        }
        collection.AddElement(element);
    }

    private void ClickNode(SceneBlueprintNode blueprint)
    {
        EditorManager.SelectedNode = blueprint;
        EditorWatcher.Clear();
        RefreshInspectorPanel(blueprint);
    }
}