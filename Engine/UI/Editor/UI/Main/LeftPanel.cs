using PBG.Core;
using PBG.UI;
using static PBG.UI.Styles;

namespace PBG.Editor;

public partial class EditorUI
{
    private UIVScroll _sceneHierarchy = null!;
    private UIPanel? _elementPanel = null;

    public UIElementBase LeftPanel =>
    new UIVCol(w_[240], h_full_minus_[30], blank_full, rgba_v4_[Bg1], bottom_left, border_ui_[0, 0, 2, 0], border_color_[Border])[
        new UICol(w_full_minus_[2], h_[30], blank_full, rgba_v4_[Bg2], border_ui_[0, 2, 0, 2], border_color_[Border])[
            new UIText("HIERARCHY", middle_left, left_[10], fs_[1.2f], rgba_v4_[Text2]),
            new UIImg(w_[20], h_[20], icon_[16], middle_right, right_[5], rgba_v4_[Text2])
        ],
        new UIVScroll(w_full_minus_[2], h_full_minus_[30], mask_children).Ref(ref _sceneHierarchy)
    ];

    private void ToggleChildren(UIImg img, UIElementBase children)
    {
        if (children.Visible)
        {
            img.UpdateIconIndex(0);
            children.SetVisible(false);
        }
        else
        {
            img.UpdateIconIndex(1);
            children.SetVisible(true);
        }

        _sceneHierarchy.ApplyChanges(UIChange.Scale);
    }

    public void ReloadSceneHierarchy(SceneDefinition blueprint, Scene scene)
    { 
        _elementPanel = null;
        _sceneHierarchy.DeleteChildren();

        var childCollection = new UIVCol(w_full, h_[20], ignore_invisible);
        CreateNodeUI(blueprint, 1, childCollection, scene.RootNode);

        _sceneHierarchy.AddElement(childCollection);
        _sceneHierarchy.UIController?.AddElement(childCollection);
    }

    private void CreateNodeUI(SceneDefinition blueprint, int offset, UIVCol collection, RootNode node)
    {
        var childCollection = new UIVCol(w_full, h_[20], not_toggle_old_invisible, grow_children);
        var element = new UIVCol(w_full, h_[20], left_[offset * 5], ignore_invisible, grow_children)[
            new UICol(w_full, h_[20])[
                new UIImg(middle_left, icon_[1], w_[15], h_[15]).OnClick(i => ToggleChildren(i, childCollection)),
                new UIText(node.Name, mc_[20], middle_left, left_[25])
            ],
            childCollection
        ];
        for (int i = 0; i < blueprint.ChildrenNodes.Count; i++)
        {
            var child = blueprint.ChildrenNodes[i];
            CreateNodeUI(offset + 1, childCollection, child);
        }
        collection.AddElement(element);
    }

    private void CreateNodeUI(int offset, UIVCol collection, SceneDefinitionNode blueprint)
    {
        var childCollection = new UIVCol(w_full, h_[20], not_toggle_old_invisible, grow_children);
        var element = new UIVCol(w_full, h_[20], left_[offset * 5], ignore_invisible, grow_children)
        .OnClick(p => ClickNode(p, blueprint))[
            new UICol(w_full, h_[20], blank_sharp, EditorManager.SelectedNode == blueprint.Transform ? rgba_v4_[Bg4] : bg_transparent)[
                new UIImg(middle_left, icon_[1], w_[15], h_[15]).OnClick(i => ToggleChildren(i, childCollection)),
                new UIText(blueprint.Name, mc_[20], middle_left, left_[25])
            ].Out(out var panel),
            childCollection
        ];
        if (blueprint.Transform == EditorManager.SelectedNode)
            _elementPanel = panel;

        for (int i = 0; i < blueprint.ChildrenNodes.Count; i++)
        {
            var child = blueprint.ChildrenNodes[i];
            CreateNodeUI(offset + 1, childCollection, child);
        }
        collection.AddElement(element);
    }



    public void ReloadSceneHierarchy(Scene scene)
    { 
        _elementPanel = null;
        _sceneHierarchy.DeleteChildren();

        var childCollection = new UIVCol(w_full, h_[20], ignore_invisible);
        CreateNodeUI(1, childCollection, scene.RootNode);

        _sceneHierarchy.AddElement(childCollection);
        _sceneHierarchy.UIController?.AddElement(childCollection);
    }

    private void CreateNodeUI(int offset, UIVCol collection, RootNode node)
    {
        var childCollection = new UIVCol(w_full, h_[20], not_toggle_old_invisible, grow_children);
        var element = new UIVCol(w_full, h_[20], left_[offset * 5], ignore_invisible, grow_children)[
            new UICol(w_full, h_[20])[
                new UIImg(middle_left, icon_[1], w_[15], h_[15]).OnClick(i => ToggleChildren(i, childCollection)),
                new UIText(node.Name, mc_[20], middle_left, left_[25])
            ],
            childCollection
        ];
        for (int i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            CreateNodeUI(offset + 1, childCollection, child);
        }
        collection.AddElement(element);
    }

    private void CreateNodeUI(int offset, UIVCol collection, TransformNode transform)
    {
        var childCollection = new UIVCol(w_full, h_[20], not_toggle_old_invisible, grow_children);
        var element = new UIVCol(w_full, h_[20], left_[offset * 2], ignore_invisible, grow_children)
        .OnClick(p => ClickNode(p, transform))[
            new UICol(w_full, h_[20], blank_sharp, EditorManager.SelectedNode == transform ? rgba_v4_[Bg4] : bg_transparent)[
                new UIImg(middle_left, icon_[1], w_[15], h_[15]).OnClick(i => ToggleChildren(i, childCollection)),
                new UIText(transform.Name, mc_[20], middle_left, left_[25])
            ].Out(out var panel),
            childCollection
        ];
        if (transform == EditorManager.SelectedNode)
            _elementPanel = panel;

        for (int i = 0; i < transform.Children.Count; i++)
        {
            var child = transform.Children[i];
            CreateNodeUI(offset + 1, childCollection, child);
        }
        collection.AddElement(element);
    }

    private void ClickNode(UICol col, SceneDefinitionNode blueprint)
    {
        SceneBlueprint.SelectedNode = blueprint is not SceneBlueprintNode ? null : (SceneBlueprintNode?)blueprint;
        ClickNode(col, blueprint, blueprint.Transform);
    }
    private void ClickNode(UICol col, TransformNode transform) => ClickNode(col, null, transform);
    private void ClickNode(UICol col, SceneDefinitionNode? blueprint, TransformNode transform)
    {
        var panel = col.GetElement<UICol>();
        _elementPanel?.UpdateColor((0, 0, 0, 0));
        _elementPanel = panel;
        _elementPanel?.Class(rgba_v4_[Bg4]);

        EditorManager.SelectedNode = transform;
        RefreshInspectorPanel(blueprint, transform);
    }
}