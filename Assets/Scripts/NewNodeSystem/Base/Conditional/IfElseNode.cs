using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.Assets.Scripts.NoiseNodes.Nodes;
using PBG.Data;
using PBG.LoaderConfig;
using PBG.UI;
using PBG.UI.Creator;

using static PBG.UI.Styles;
using Silk.NET.Input;

public class IfElseNode : NodeBase
{
    public static List<IfElseNode> AllIfElseNodes = [];
    public static List<IfElseNode> HoveringIfElseNodes = [];

    private UIElementBase?[] _selection = [null, null, null, null];
    public string Type = "<";
    public UICol SubCollection = null!;

    public IfElseNode(NodeCollection nodeCollection) : this(null, nodeCollection, Vector2.Zero, "<") { }
    public IfElseNode(NodeCollection nodeCollection, Vector2 position) : this(null, nodeCollection, position, "<") { }
    public IfElseNode(NodeCollection nodeCollection, Vector2 position, string type) : this(null, nodeCollection, position, type) { }
    public IfElseNode(NodeCollection nodeCollection, IfElseNodeConfig config) : this(config.GUID, nodeCollection, (config.Position[0], config.Position[1]), config.Type) { }
    public IfElseNode(string? guid, NodeCollection nodeCollection, Vector2 pos, string type) : base(guid, nodeCollection)
    {
        Vector2 position = Mathf.Round(pos / 10f) * 10f;
        Position = position;
        GridPosition = position;
        _position = position;
        Color = (0.231f, 0.357f, 0.573f);
        Type = type;
    }

    public IfElseNode InitUI()
    {
        Collection = new IfElseUI(this, Mathf.FloorToInt(Position), Color);
        for (int i = 0; i < 4; i++)
        {
            _selection[i] = Collection.QueryElement($"Selection{i + 1}") ?? throw new Exception($"Selection{i + 1} in IfElse node not found at index");
        }
        AllIfElseNodes.Add(this); // Only add it when the UI is initialized
        return this;
    }

    public IfElseNode InitBlankUI()
    {
        InputFields.Add("Value", new(new(), this, new() { Name = "Value", Type = "float" }));
        InputFields.Add("Compare", new(new(), this, new() { Name = "Compare", Type = "float" }));
        return this;
    }

    public override string GetName() => "IfElseNode " + ID;

    public IfElseNoiseNode GenerateNoiseNode(NoiseNodeManager manager, ref int index) => GenerateNoiseNode([], ref index);
    public IfElseNoiseNode GenerateNoiseNode(NoiseValue[] variables, ref int index, int indexOffset = 0)
    {
        //Debug.Print($"[Node] : Generating noise node for node '{GetType().Name}'");
        List<GetterValue> getters = [];

        /*
        foreach (var (_, input) in InputFields)
        {
            if (input.IsExternal)
                continue;

            OLD_GetterValue getter;
            if (input.Input == null || !input.Input.IsConnected || input.Input.Output == null)
            {
                getter = new GetterValue(variables, index + indexOffset);
                Debug.Print($"[Node] : Getter value is constant with index: {index}");
                variables[index] = input.Value.GetNoiseValue();
                index++;
            }
            else
            {
                getter = new GetterValue(variables, input.Input.Output.Index + indexOffset);
                Debug.Print($"[Node] : Getter value is variable with index: {input.Input.Output.Index + indexOffset}");
            }
            getters.Add(getter);
        }
        */

        return new IfElseNoiseNode(getters[0], getters[0], [], Type);
    }

    public override NoiseNode? BuildExpression(Dictionary<NodeBase, Dictionary<string, SetterValue>> connections, List<Expression> expressions)
    {
        GetterValue value = null!;
        GetterValue compare = null!;

        foreach (var (name, input) in InputFields)
        {
            GetterValue getter;
            Expression defaultValue = input.Value.GetExpression();
            if (input.Input == null || !input.Input.IsConnected || input.Input.Output == null)
            {
                Console.WriteLine(input.Identifier + " not connected here 1");
                getter = new(input.Value.GetValueType(), null, defaultValue);
            }
            else
            {
                Console.WriteLine("Getting setter: " + input.Input.Output.OutputField.Identifier + " from node: " + input.Input.Output.Node.GetName());
                if (connections.TryGetValue(input.Input.Output.Node, out var sets) && sets.TryGetValue(input.Input.Output.OutputField.Identifier, out var set))
                {
                    Console.WriteLine("Setter found!!!");
                    if (input.ConversionValue != null && set.Variable != null)
                    {
                        Console.WriteLine("conversion");
                        Console.WriteLine(input.Value + " " + input.ConversionValue + " " + set.Variable.Type);
                        getter = new(input.Value.GetValueType(), ExpressionHelper.ConvertTo(input.ConversionValue.GetValueType(), input.Value.GetValueType(), set.Variable), defaultValue);
                    }
                    else
                    {
                        Console.WriteLine("normal because: " + (input.ConversionValue == null) + " or " + (set.Variable == null));
                        getter = new(input.Value.GetValueType(), set.Variable, defaultValue);
                    }
                }
                else
                {
                    Console.WriteLine(input.Identifier + " not connected here 2 because: ");

                    getter = new(input.Value.GetValueType(), null, defaultValue);
                }
            }
            
            if (name == "Value")
                value = getter;
            if (name == "Compare")
                compare = getter;
        }

        return new IfElseNoiseNode(value!, compare!, [], Type);
    }

    public override void Select()
    {
        _position = Collection.Origin;
        for (int i = 0; i < 4; i++)
        {
            var selection = _selection[i];
            if (selection == null) return;
            selection.Color = SELECTION_COLOR;
            selection.UpdateColor();
        }
    }

    public override void Deselect()
    {
        for (int i = 0; i < 4; i++)
        {
            var selection = _selection[i];
            if (selection == null) return;
            selection.Color = Vector4.Zero;
            selection.UpdateColor();
        }
    }

    public override IfElseNodeConfig Save()
    {
        var config = new IfElseNodeConfig()
        {
            Name = "IfElse",
            Type = Type,
            Position = [Position.X, Position.Y],
            GUID = ID.ToString()
        };
        foreach (var input in InputFields.Values)
        {
            if (input.Input != null)
            {
                config.Inputs.Add(input.GetUniqueName(), (input.Input.IsConnected && input.Input.Output != null) ? input.Input.Output.OutputField.GetUniqueName() : null);
            }
        }
        foreach (var input in InputFields.Values)
        {
            var values = input.Value.GetValues();
            if (values.Length == 0)
                continue;
            config.Values.Add(values);
        }
        return config;
    }
    
    public override void DeleteCore()
    {
        foreach (var input in GetInputs())
        {
            input.Disconnect();
        }
        InputFields = [];
        BlockIconCollections = [];
        AllIfElseNodes.Remove(this);
        HoveringIfElseNodes.Remove(this);
    }

    public override void DeleteNode()
    {
        base.DeleteNode();
        DeleteCore();
        Collection?.Delete();
        NodeManager.RemoveNode(this);
    }
}

public class IfElseUI(
    IfElseNode node,
    Vector2i position,
    Vector3 color
) : UIScript
{
    private UICol? _type = null;

    public override UIElementBase Script() =>
    new UICol(Class(left_[position.X], top_[position.Y], grow_children), Sub(
        new UIImg("Selection1", Class(blank_round, rgba_[0, 0, 0, 0], w_[25], h_full, middle_left)),
        new UIImg("Selection2", Class(blank_round, rgba_[0, 0, 0, 0], w_[25], h_full, middle_right)),
        new UIImg("Selection3", Class(blank_round, rgba_[0, 0, 0, 0], h_[25], w_full, top_center)),
        new UIImg("Selection4", Class(blank_round, rgba_[0, 0, 0, 0], h_[25], w_full, bottom_center)),
        new UICol(Class(border_[5, 5, 5, 5], grow_children), Sub([
            new UIImg(Class(h_[40], w_full, blank_round, rgb_v3_[color])),
            new UICol(Class(border_[0, 30, 0, 0], grow_children), Sub([ 
                new UIButton(Class(w_full_minus_[25], h_[30], bottom_[30]), OnClick(Select), OnHold(MoveNode), OnHoverButton(_ => { if (Input.IsKeyPressed(Key.K)) Console.WriteLine("Hovering " + node.GetName()); })),
                new UIText($"IfElse", Class(mc_[6], fs_[1], bottom_[20], left_[5])),
                new UIText("X", Class(top_right, mc_[1], fs_[1.2f], bottom_[20], right_[5]), OnClick(DeleteNode)),
                new UICol(Class(grow_children), Sub(
                    new UIImg(Class(w_full, h_[20], blank_sharp_g_[30])),
                    new UIImg(Class(w_full, h_[20], blank_sharp_g_[30], bottom_left)),
                    new UIImg(Class(w_[20], h_full, blank_sharp_g_[30], top_right)),
                    new UIImg(Class(blank_sharp_g_[30], h_full, w_[110])),
                    new UIVCol(Class(border_[5, 5, 5, 5], grow_children), Sub([
                        new UIHCol(Class(grow_children), Sub(
                            new UIVCol(Class(grow_children), Sub([
                                ..Run(() => {
                                    var UIButton = new UIButton(w_[15], h_[15], blank_sharp, rgb_v3_[color], middle_left);
                                    var field = new NodeInputField(UIButton, node, new() { Name = "Value", Type = "float" });
                                    UIButton.SetOnClick(_ => { if (field.Input != null) NodeBase.Connect(field.Input); });
                                    node.InputFields.Add("Value", field);
                                    var input = new UIField("0", mc_[8], middle_left, left_[5], fs_[1], data_["value", 0f]);
                                    if ( field.Value is NodeValue_Float floatValue)
                                    {
                                        floatValue.input1 = input;
                                    }
                                    return new UIVCol(Class(h_[30], grow_children), Sub([
                                        new UIHCol(Class(h_[30], spacing_[5], w_[130]), Sub([
                                            UIButton,
                                            new UIText("Value", Class(mc_[5], fs_[1], middle_left))
                                        ])),
                                        NodeValue.DefaultParent(NodeValue.DefaultInput(
                                            (UIField)input.
                                            SetOnTextChange(f => {
                                                float value = f.GetFloat();
                                                f.Dataset["value"] = value;
                                                field.Value.UpdateValue(0, value);
                                            }).SetOnHold(f => {
                                                float value = f.Dataset.Float("value");
                                                float delta = Input.GetMouseDelta().X * NodeHelper.SlideSpeed;
                                                if (delta == 0f) return;
                                                value += delta;
                                                f.Dataset["value"] = value;
                                                f.SetText(value.ToString()).UpdateCharacters();
                                                field.Value.UpdateValue(0, value);
                                            })
                                        )),
                                    ]));
                                }),
                                ..Run(() => {
                                    var UIButton = new UIButton(w_[15], h_[15], blank_sharp, rgb_v3_[color], middle_left);
                                    var field = new NodeInputField(UIButton, node, new() { Name = "Compare", Type = "float" });
                                    UIButton.SetOnClick(_ => { if (field.Input != null) NodeBase.Connect(field.Input); });
                                    node.InputFields.Add("Compare", field);
                                    var input = new UIField("0", mc_[8], middle_left, left_[5], fs_[1], data_["value", 0f]);
                                    if (field.Value is NodeValue_Float floatValue)
                                    {
                                        floatValue.input1 = input;
                                    }
                                    return new UIVCol(Class(h_[30], grow_children), Sub([
                                        new UIHCol(Class(h_[30], spacing_[5], w_[130]), Sub([
                                            UIButton,
                                            new UIText("Compare", Class(mc_[7], fs_[1], middle_left))
                                        ])),
                                        NodeValue.DefaultParent(NodeValue.DefaultInput(
                                            (UIField)input.
                                            SetOnTextChange(f => {
                                                float value = f.GetFloat();
                                                f.Dataset["value"] = value;
                                                field.Value.UpdateValue(0, value);
                                            }).SetOnHold(f => {
                                                float value = f.Dataset.Float("value");
                                                float delta = Input.GetMouseDelta().X * NodeHelper.SlideSpeed;
                                                if (delta == 0f) return;
                                                value += delta;
                                                f.Dataset["value"] = value;
                                                f.SetText(value.ToString()).UpdateCharacters();
                                                field.Value.UpdateValue(0, value);
                                            })
                                        )),
                                    ]));
                                }),
                                ..Foreach([new List<string>() { "==", "!=", "<" }, ["<=", ">", ">="]], types => 
                                    new UICol(Class(w_full_minus_[30], h_[30], top_[5]), Sub([
                                        ..Foreach(types, (i, type) => {
                                            var align = new List<UIStyleData>(){ top_left, top_center, top_right };
                                            var col = new UICol(w_[30], h_[30], blank_sharp_g_[node.Type == type ? 50 : 40], data_["type", type], align[i]);
                                            if (node.Type == type) _type = col;
                                            col.SetOnClick(SetType);
                                            col.AddElement(new UIText(type, Class(middle_center, fs_[1], mc_[type.Length])));
                                            return col;
                                        })
                                    ]))
                                ),
                            ])),
                            newCol(Class(min_w_[200], min_h_[200], grow_children, border_[0, 0, 20, 20]),
                            OnHoverEnter<UICol>(_ => IfElseNode.HoveringIfElseNodes.Add(node)),
                            OnHoverExit<UICol>(_ => IfElseNode.HoveringIfElseNodes.Remove(node)), 
                            Sub(
                                new UIButton(Class(w_[30], h_[30], blank_sharp, rgb_v3_[color], top_left, left_[-20], top_[20]),
                                OnClick<UIButton>(UIButton => _oldScaleUIButtonOffset = UIButton.BaseOffset),
                                OnHold<UIButton>(UIButton => {
                                    if (Input.IsKeyDown(Key.ControlLeft)) return;
                                    Vector2 mouseDelta = Input.GetMouseDelta();
                                    if (mouseDelta == Vector2.Zero) return;
                                    _oldScaleUIButtonOffset += mouseDelta * (1 / UIButton.UIController?.Scale ?? 1f);
                                    UIButton.BaseOffset = Mathf.Max(_oldScaleUIButtonOffset, (-20, 20));
                                    Element.ApplyChanges(UIChange.Scale);
                                }))
                            ), ref node.SubCollection)
                        ))
                    ]))
                ))
            ]))
        ]))
    ));

    public void SetType(UICol collection)
    {
        if (_type != null)
        {
            _type.Color = (0.4f, 0.4f, 0.4f, 1f);
            _type.UpdateColor();
        }

        node.Type = collection.Dataset.String("type");
        collection.Color = (0.5f, 0.5f, 0.5f, 1f);
        collection.UpdateColor();
        _type = collection;
    }

    private Vector2 _oldScaleUIButtonOffset = Vector2.Zero;
    
    public void Select(UIButton _) => NodeManager.Select(node);

    public void MoveNode(UIButton UIButton)
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