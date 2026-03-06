using PBG.MathLibrary;
using PBG.UI;
using PBG.UI.Creator;
using static PBG.UI.Styles;

public class CacheNodeUI(
    CacheNode node,
    string name,
    Vector2i position,
    Vector3 color
) : UIScript
{
    public override UIElementBase Script() =>
    new BaseNodeUI(node, name, position, color, true, () => [
        new UIVCol(Class(blank_sharp_g_[30], grow_children), Sub(
            new UIVCol(Class(border_[5, 5, 5, 5], grow_children), Sub([
                new UIHCol(Class(grow_children, spacing_[20]), [
                    ..Run(() => {
                        var button = new UIButton(w_[15], h_[15], blank_sharp, rgb_v3_[color], middle_left);
                        var field = new NodeInputField(button, node, new() { Name = "Input", Type = "vec3" });
                        button.SetOnClick(_ => { if (field.Input != null) NodeBase.Connect(field.Input); });
                        node.InputFields.Add("Input", field);
                        button.Dataset["field"] = field;
                        return button;
                    }),
                    ..Run(() => {
                        var button = new UIButton(w_[15], h_[15], blank_sharp, rgb_v3_[color], middle_left);
                        var field = new NodeOutputField(button, node, new() { Name = "Output", Type = "vec3" });
                        button.SetOnClick(_ => NodeBase.Connect(field.Output));
                        node.OutputFields.Add("Output", field);
                        button.Dataset["field"] = field;
                        return button;
                    })
                ])
            ]))
        ))
    ]);
}