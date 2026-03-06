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
    new UICol(Class(blank_round, rgba_[0, 0, 0, 0], left_[position.X], top_[position.Y], border_[5, 5, 5, 5], grow_children), Sub(
        new UICol(Class(blank_round, rgb_v3_[color], border_[0, 30, 0, 0], grow_children), Sub([
            new UIButton(Class(w_full_minus_[25], h_[30], bottom_[30]), OnClick<UIButton>(Select), OnHold<UIButton>(MoveNode)),
            new UIText("Outputs", Class(mc_[7], fs_[1], bottom_[20], left_[5])),
            newVCol(Class(blank_sharp_g_[30], grow_children, w_[200]), Sub(), ref _list),
            ..Run(RegenerateOutputs)
        ]))
    ));

    public void RegenerateOutputs()
    {
        UIElementBase[] elements = new UIElementBase[node.InputFields.Count + 1];
        int i = 0;
        foreach (var (name, field) in node.InputFields)
        {
            if (field.Input == null) continue;
            var button = new UIButton(w_[15], h_[15], blank_sharp, rgb_v3_[node.Color], middle_left);
            var text = new UIText(name, Class(mc_[18], fs_[1f], middle_right, text_align_right));
            field.SetButton(button);
            field.SetName(name);
            field.Input.Name = name;
            button.SetOnClick(_ => { NodeBase.Connect(field.Input); });
            elements[i] = new UICol(Class(w_full_minus_[10], h_[30], left_[5]), OnClickCol(_ => NameClick(text, field)), Sub([
                text,
                button
            ]));
            i++;
        }
        elements[i] = new UICol(Class(w_full_minus_[10], h_[30], left_[5], top_[5]), Sub(
            new UICol(Class(w_full, h_full_minus_[5], blank_sharp_g_[40]), OnClickCol(_ => node.AddValue("Result")), Sub(
                new UIText("+", Class(mc_[1], fs_[1.2f], middle_center))
            ))
        ));
        _list.DeleteChildren();
        _list.AddElements(elements);
        _list.UIController?.AddElements(elements);
        if (Created) Element.ApplyChanges(UIChange.Scale);
    }

    public void NameClick(UIText text, NodeInputField field)
    {
        StructureNodeUI.GroupInputSettings.SetVisible(true);
        StructureNodeUI.GroupInputSettings.QueryElement("values")?.SetVisible(false);
        StructureNodeUI.GroupInputName.UpdateText(text.GetText());
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