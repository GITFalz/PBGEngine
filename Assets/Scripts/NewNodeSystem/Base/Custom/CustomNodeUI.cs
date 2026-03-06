using PBG.MathLibrary;
using PBG.UI;
using PBG.UI.Creator;
using static PBG.UI.Styles;

public class CustomNodeUI(
    CustomNode node,
    string name,
    Vector2i position,
    Vector3 color,
    List<NodeInputParam> inputFields,
    List<NodeOutputParam> outputFields
) : UIScript
{
    public override UIElementBase Script() =>
    new BaseNodeUI(node, name, position, color, true, () => [
        new UIVCol(Class(blank_sharp_g_[30], grow_children), Sub(
            new UIVCol(Class(border_[5, 5, 5, 5], grow_children, spacing_[5]), Sub([
                new UIHCol(Class(grow_children), Sub(
                    new UIVCol(Class(grow_children), Sub([
                        ..Foreach(inputFields, input => {
                            NodeInputField field;
                            if (input.CanConnect && !input.External)
                            {
                                var button = new UIButton(w_[15], h_[15], blank_sharp, rgb_v3_[color], middle_left);
                                field = new(button, node, input);
                                node.InputFields.Add(input.Name, field);
                                button.Dataset["field"] = field;
                                return new CustomNodeUIInput(input.Name, button, field);
                            }
                            field = new(null, node, input);
                            node.InputFields.Add(input.Name, field);
                            if (input.External)
                            {
                                return null;
                            }
                            return new CustomNodeUIInput(input.Name, null, field);
                        })
                    ])),
                    new UIVCol(Class(grow_children), Sub([
                        ..Foreach(outputFields, output => new CustomNodeUIOutput(node, output, output.Name))
                    ]))
                )),
                ..If(node.IsOutput, () => 
                new UICol(Class(w_full, h_[30]), Sub(
                    new UICol(Class(w_[50], h_full, blank_sharp_g_[10], middle_right), Sub(
                        new UIField(""+node.OutputPriority, Class(mc_[5], middle_right, right_[5]), OnTextChange(f => node.OutputPriority = f.GetInt()))
                    )),
                    new UIText("Priority", Class(middle_left))
                )))
            ]))
        ))
    ]);
}

public class CustomNodeUIInput(string name, UIButton? button, NodeInputField field) : UIScript
{
    public override UIElementBase Script() =>
    new UIVCol(Class(h_[30], grow_children), Sub([
        new UIHCol(Class(h_[30], spacing_[5], w_[130]), Sub([
            ..Run(() => button?.SetOnClick(_ => { if (field.Input != null) NodeBase.Connect(field.Input); })),
            new UIText(name.Length <= 10 ? name : name[..10], Class(mc_[Mathf.Max(name.Length, 10)], fs_[1], middle_left))
        ])),
        field.Value.GetInputFields()
    ]));
}

public class CustomNodeUIOutput(CustomNode node, NodeOutputParam output, string name) : UIScript
{
    public override UIElementBase Script() =>
    new UICol(Class(h_[30], spacing_[5], w_[130], top_left), Sub([
        new UIText(name.Length <= 10 ? name : name[..10], Class(mc_[Mathf.Min(name.Length, 10)], fs_[1], middle_left)),
        ..Run(() => {
            var button = new UIButton(w_[15], h_[15], blank_sharp, rgb_v3_[node.Color], middle_right);
            var field = new NodeOutputField(button, node, output);
            button.SetOnClick(_ => NodeBase.Connect(field.Output));
            node.OutputFields.Add(output.Name, field);
            return button;
        })
    ]));
}