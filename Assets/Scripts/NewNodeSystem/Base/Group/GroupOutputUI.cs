using PBG.MathLibrary;
using PBG.UI;
using PBG.UI.Creator;
using static PBG.UI.Styles;

public class GroupOutputUI(
    Vector2i position,
    Vector3 color,
    GroupOutputNode node
) : UIScript
{
    public UIVCol _list = null!;

    public override UIElementBase Script() =>
    new UICol(blank_round, rgba_[0, 0, 0, 0], left_[position.X], top_[position.Y], border_[5, 5, 5, 5], grow_children)[
        new UICol(blank_round, rgb_v3_[color], border_[0, 30, 0, 0], grow_children)[
            new UIButton(w_full_minus_[25], h_[30], bottom_[30]).OnClick(Select).OnHold(MoveNode),
            new UIText("Outputs", mc_[7], fs_[1.5f], bottom_[20], left_[5]),
            new UIVCol(blank_sharp_g_[30], grow_children, w_[200]).Ref(ref _list),
            Run(RegenerateOutputs)
        ]
    ];

    public void RegenerateOutputs()
    {
        UIElementBase[] elements = new UIElementBase[node.InputFields.Count + 1];
        int i = 0;
        foreach (var (name, field) in node.InputFields)
        {
            if (field.Input == null) continue;
            var button = new UIButton(w_[15], h_[15], blank_sharp, rgb_v3_[node.Color], middle_left);
            var text = new UIText(name, mc_[18], fs_[1.5f], middle_right, text_align_right);
            field.SetButton(button);
            field.SetName(name);
            field.Input.Name = name;
            button.OnClick(_ => { NodeBase.Connect(field.Input); });
            elements[i] = new UICol(w_full_minus_[10], h_[30], left_[5]).OnClick(_ => NameClick(text, field))[
                text,
                button
            ];
            i++;
        }
        elements[i] = new UICol(w_full_minus_[10], h_[30], left_[5], top_[5])[
            new UICol(w_full, h_full_minus_[5], blank_sharp_g_[40]).OnClick(_ => node.AddValue("Result"))[
                new UIText("+", mc_[1], fs_[1.2f], middle_center)
            ]
        ];
        _list.DeleteChildren();
        _list.AddElements(elements);
        _list.UIController?.AddElements(elements);
        if (Created) Element.ApplyChanges(UIChange.Scale);
    }

    public void NameClick(UIText text, NodeInputField field)
    {
        StructureNodeUI._groupInputSettings.SetVisible(true);
        StructureNodeUI._groupInputSettings.QueryElement("values")?.SetVisible(false);
        StructureNodeUI._groupInputName.UpdateText(text.GetText());
        StructureNodeManager.GroupInputField = field;
        StructureNodeManager.GroupRemoveField = node.RemoveValue;
        StructureNodeManager.SetGroupFieldName = f =>
        {
            if (field.Input == null) return;
            var name = f.GetText();
            var oldName = text.GetText();
            if (node.InputFields.ContainsKey(name))
            {
                f.UpdateText(oldName);
                return;
            }
            text.UpdateText(name);
            node.InputFields.Remove(oldName);
            node.InputFields[name] = field;
            field.Input.Name = name;
        };
        StructureNodeManager.SetGroupFieldType = (_) => {};
    }
    
    public void Select(UIButton _) => NodeManager.Select(node);

    public void MoveNode(UIButton button)
    {
        node.MoveNode();
        NodeManager.UpdateLines();
    }
}