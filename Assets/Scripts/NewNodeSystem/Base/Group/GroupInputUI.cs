using PBG.MathLibrary;
using PBG.UI;
using PBG.UI.Creator;
using static PBG.UI.Styles;

public class GroupInputUI(
    Vector2i position,
    Vector3 color,
    GroupInputNode node
) : UIScript
{
    private UIVCol _list = null!;

    public override UIElementBase Script() =>
    new UICol(Class(blank_round, rgba_[0, 0, 0, 0], left_[position.X], top_[position.Y], border_[5, 5, 5, 5], grow_children), Sub(
        new UICol(Class(blank_round, rgb_v3_[color], border_[0, 30, 0, 0], grow_children), Sub([
            new UIButton(Class(w_full_minus_[25], h_[30], bottom_[30]), OnClick<UIButton>(Select), OnHold<UIButton>(MoveNode)),
            new UIText("Inputs", Class(mc_[6], fs_[1], bottom_[20], left_[5])),
            newVCol(Class(blank_sharp_g_[30], grow_children, w_[200]), Sub(), ref _list),
            ..Run(RegenerateInputs)
        ]))
    ));

    public void RegenerateInputs()
    {
        UIElementBase[] elements = new UIElementBase[node.OutputFields.Count + 1];
        int i = 0;
        foreach (var (name, field) in node.OutputFields)
        {
            var button = new UIButton(w_[15], h_[15], blank_sharp, rgb_v3_[node.Color], middle_right);
            var text = new UIText(name, Class(mc_[18], fs_[1f], middle_left));
            field.SetButton(button);
            field.SetName(name);
            field.Output.Name = name;
            button.SetOnClick(_ => { NodeBase.Connect(field.Output); });
            elements[i] = new UICol(Class(w_full_minus_[10], h_[30], left_[5]), OnClickCol(_ => NameClick(text, field)), Sub([
                text,
                button
            ]));
            i++;
        }
        elements[i] = new UICol(Class(w_full_minus_[10], h_[30], left_[5], top_[5]), Sub(
            new UICol(Class(w_full, h_full_minus_[5], blank_sharp_g_[40]), OnClickCol(_ => node.AddValue("Value")), Sub(
                new UIText("+", Class(mc_[1], fs_[1.2f], middle_center))
            ))
        ));
        _list.DeleteChildren();
        _list.AddElements(elements);
        _list.UIController?.AddElements(elements);
        if (Created) Element.ApplyChanges(UIChange.Scale);
    }

    public void NameClick(UIText text, NodeOutputField field)
    {
        StructureNodeUI.GroupInputSettings.SetVisible(true);
        StructureNodeUI.GroupInputName.UpdateText(text.GetText());
        StructureNodeManager.GroupInputField = field;
        StructureNodeManager.GroupRemoveField = node.RemoveValue;
        StructureNodeManager.SetGroupFieldName = f =>
        {
            var name = f.GetText();
            var oldName = text.GetText();
            if (node.OutputFields.ContainsKey(name))
            {
                f.UpdateText(oldName);
                return;
            }
            text.UpdateText(name);
            node.OutputFields.Remove(oldName);
            node.OutputFields[name] = field;
            field.Output.Name = name;
        };
        StructureNodeManager.SetGroupFieldType = type =>
        {
            field.Value = type;
            for (int j = 0; j < field.Output.Inputs.Count; j++)
            {
                var inputNode = field.Output.Inputs[j];
                inputNode.Node.InitNodeTypes();
            }
            NodeManager.GenerateLines();
        };
        StructureNodeUI.ResetGroupInputValues(field.Value.GetGLSLType(), field.Value.GetValues().Length, field.Value.GetValues());
    }

    public void Select(UIButton _) => NodeManager.Select(node);

    public void MoveNode(UIButton button)
    {
        node.MoveNode();
        NodeManager.UpdateLines();
    }
}
