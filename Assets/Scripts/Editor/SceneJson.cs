using PBG.Editor;
using PBG.Graphics;
using PBG.Pipes;
using PBG.UI;

public class SceneGraph
{
    public SceneNode[] Nodes { get; set; } = [];

    public void GenerateNodes(UIVScroll scroll)
    {
        foreach (var node in Nodes)
        {
            var sub = node.GenerateNodes(scroll, 0, scroll);
            scroll.UIController += sub;
        }
    }
}

public class SceneNode
{
    public string Name { get; set; } = "N/A";
    public SceneNode[] Nodes { get; set; } = [];

    public UIElementBase GenerateNodes(UIVScroll scroll, int offset, UICol collection, string path = "")
    {
        path += Name;
        var subCol = new UIVCol(grow_children, w_full, spacing_[2])[
            new UICol(
                data_["path", path],
                border_[offset * 5, 0, 0, 0], 
                h_[20], w_full_minus_[4], 
                blank_full, bg_transparent, 
                hover_color_[(0.3f, 0.3f, 0.3f, 0f), (0.5f, 0.5f, 0.5f, 0.5f)], hover_color_ignore_when_selected, 
                hover_color_duration_[0.1f], hover_color_easeinout,
                border_ui_[2, 0, 0, 0]
            ).OnClick(col => OnClick(scroll, col))
            [
                new UIText(Name, middle_left, left_[5])
            ]
        ];

        foreach (var node in Nodes)
        {
            node.GenerateNodes(scroll, offset + 1, subCol, path + '/');
        }

        collection.AddElement(subCol);

        return subCol;
    }

    private static void OnClick(UIVScroll scroll, UICol col)
    {
        string path = col.Dataset.String("path");
        var oldCol = scroll.Dataset.Get<UICol>("selected");
        if (oldCol != null)
        {   
            oldCol.IsSelected = false;
            oldCol.UpdateColor((0, 0, 0, 0));
            oldCol.UpdateBorderColor((0, 0, 0, 0));
        }

        col.IsSelected = true;
        col.UpdateColor("#2d5a3d");
        col.UpdateBorderColor("#3d7a4d");
        scroll.Dataset["selected"] = col;

        EditorMenu.CurrentNodePath = path;

        GameWindow.Send(CommandType.NodeComponents, path);
    }
}