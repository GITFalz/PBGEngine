using PBG.MathLibrary;
using PBG.UI;
using PBG.UI.Creator;
using static PBG.UI.Styles;

public class GradientNodeUI(
    CustomNode node,
    string name,
    Vector2i position,
    Vector3 color,
    List<NodeInputParam> inputFields,
    List<NodeOutputParam> outputFields
) : UIScript {
    public override UIElementBase Script() =>
    new BaseNodeUI(node, name, position, color, true, () => [
        new UIVCol(Class(blank_sharp_g_[30], grow_children), Sub(
            new UIVCol(Class(border_[5, 5, 5, 5], grow_children), Sub([
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
                ))
            ]))
        ))
    ]);
}