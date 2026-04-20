using System.Diagnostics;
using PBG.MathLibrary;
using PBG.Data;
using PBG.UI;
using PBG.UI.Creator;
using static PBG.UI.Styles;


public class BaseNodeUI(
    NodeBase node,
    string name,
    Vector2i position,
    Vector3 color,
    bool canDelete,
    Func<UIElementBase[]>? content
) : UIScript
{
    public override UIElementBase Script() =>
    new UICol(blank_round, rgba_[0, 0, 0, 0], left_[position.X], top_[position.Y], border_[5, 5, 5, 5], grow_children)[
        new UICol(blank_round, rgb_v3_[color], border_[0, 30, 0, 0], grow_children)[
            new UIButton(w_full_minus_[25], h_[30], bottom_[30]).OnClick(Select).OnHold(MoveNode).OnHover(_ => { if (Input.IsKeyPressed(Key.K)) Console.WriteLine("Hovering " + node.GetName()); }),
            new UIText(name, mc_[name.Length], fs_[1], bottom_[20], left_[5]),
            If(canDelete, () => new UIText("X", top_right, mc_[1], fs_[1.2f], bottom_[20], right_[5]).OnClick(DeleteNode)),
            If(content != null, content!)
        ]
    ];

    public void Select(UIButton _) => NodeManager.Select(node);

    public void MoveNode(UIButton button)
    {
        node.MoveNode();
        NodeManager.UpdateLines();
    }

    public void DeleteNode(UIText _)
    {
        node.DeleteNode();
        NodeManager.GenerateLines();
    }
}