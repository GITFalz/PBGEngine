using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.Data;
using PBG.LoaderConfig;
using PBG.MathLibrary;
using PBG.UI;
using PBG.UI.Creator;
using static PBG.UI.Styles;
using GroupNoise = PBG.Assets.Scripts.NoiseNodes.Nodes.GroupNode;
using Silk.NET.Input;

public class GroupNode : NodeBase
{
    public string GroupName;
    public GroupData Data;

    public static Action? DeselectGroup = null;

    

    public GroupNode(NodeCollection nodeCollection, GroupData data, Vector2 pos, string groupName) : this(null, nodeCollection, data, pos, groupName) {}
    public GroupNode(string? guid, NodeCollection nodeCollection, GroupData data, Vector2 pos, string groupName) : base(guid, nodeCollection)
    {
        Vector2 position = Mathf.Round(pos / 10f) * 10f;
        Data = data;
        GroupName = groupName;
        Position = position;
        GridPosition = position;
        _position = position;
        Color = (0.6f, 0.5f, 0.8f);
        Collection = new GroupNodeUI(Mathf.FloorToInt(Position), Color, this, data);
    }

    public override string GetName() => "GroupNode " + ID;

    public string GetFunctionName() => GroupName.Trim().Replace(' ', '_').Replace('-', '_');

    public override string GetLine(LineContext context)
    {
        var line = "";
        foreach (var (name, output) in OutputFields)
        {
            line += $"{output.GetVariable(false)}; ";
        }
        line += $"{GetFunctionName()}(iPosition";

        foreach (var (name, input) in InputFields)
        {
            line += $", {input.GetVariable(true)}";
        }

        foreach (var (name, output) in OutputFields)
        {
            line += $", {output.Output.VariableName}";
        }

        line += ");";
        return line;
    }

    public void GetFunction(List<string> lines)
    {
        string functionLine = $"void {GetFunctionName()}(vec2 iPosition";

        foreach (var (name, input) in InputFields)
        {
            functionLine += $", {input.Value.GetGLSLType()} {name}";
        }

        foreach (var (name, output) in OutputFields)
        {
            functionLine += $", out {output.Value.GetGLSLType()} {name}";
        }

        functionLine += ")";
 
        lines.Add(functionLine);
        lines.Add("{");

        var nodes = Data.Nodes.GetConnectedNodeList();
        for (int i = 0; i < nodes.Count; i++)
        {
            NodeBase node = nodes[i];
            lines.Add("    " + node.GetLine(new() { GetCurrentValue = true }));
        }

        lines.Add("}");
    }

    public GroupNoise GenerateNoiseNode(NoiseNodeManager manager, ref int extIndex) => GenerateNoiseNode([], ref extIndex);
    public GroupNoise GenerateNoiseNode(NoiseValue[] variables, ref int extIndex, int indexOffset = 0)
    {
        //Debug.Print($"[Node] : Generating noise node for GROUP node '{GetName()}'");
        var nodeTree = NodeManager.GetNodeTree(Data.Nodes.GetConnectedNodeList(out var outputCount));

        List<GetterValue> getters = [];
        List<GetterValue> internalGetters = [];

        /*
        List<SetterValue> setters = [];
        List<SetterValue> internalSetters = [];

        NoiseValue[] internalVariables = [.. Enumerable.Repeat(NoiseValue.Default, outputCount)];
        List<NoiseNode> actionMap = [];

        int intIndex = 0;

        int index = 0;
        foreach (var (name, input) in InputFields)
        {
            OLD_GetterValue getter;
            if (input.Input == null || !input.Input.IsConnected || input.Input.Output == null)
            {
                getter = new GetterValue(variables, extIndex);
                Debug.Print($"[Node] : Getter value is constant with external index: {extIndex}");
                variables[extIndex] = input.Value.GetNoiseValue();
                extIndex++;
            }
            else
            {
                getter = new GetterValue(variables, input.Input.Output.Index);
                Debug.Print($"[Node] : Getter value is variable with external index: {input.Input.Output.Index}");
            }
            getters.Add(getter);

            if (Data.GroupInputNode != null && Data.GroupInputNode.OutputFields.TryGetValue(name, out var gInput))
            {
                gInput.Output.Index = intIndex;
                intIndex++;

                SetterValue setter = new(internalVariables, gInput.Output.Index);
                Debug.Print($"[Node] : Setter value is variable with internal index: {gInput.Output.Index}");
                internalSetters.Add(setter);
                index++;
            }
        }

        for (int i = 0; i < nodeTree.Count; i++)
        {
            var nodeData = nodeTree[i];
            var noiseNode = nodeData.GenerateInternalNoiseNode(internalVariables, ref intIndex, 0);
            if (noiseNode != null)
                actionMap.Add(noiseNode);
        }

        foreach (var (name, output) in OutputFields)
        {
            if (Data.GroupOutputNode != null && Data.GroupOutputNode.InputFields.TryGetValue(name, out var gInput))
            {
                SetterValue setter = new SetterValue(variables, output.Output.Index);
                Debug.Print($"[Node] : Setter value is variable with external index: {output.Output.Index} " + name);
                setters.Add(setter);

                OLD_GetterValue getter;
                if (gInput.Input == null || !gInput.Input.IsConnected || gInput.Input.Output == null)
                {
                    getter = new GetterValue(internalVariables, intIndex);
                    Debug.Print($"[Node] : Getter value is constant with internal index: {intIndex} " + name);
                    internalVariables[intIndex] = gInput.Value.GetNoiseValue();
                    intIndex++;
                }
                else
                {
                    getter = new GetterValue(internalVariables, gInput.Input.Output.Index);
                    Debug.Print($"[Node] : Getter value is variable with internal index: {gInput.Input.Output.Index} " + name);
                }
                internalGetters.Add(getter);
            }
        }
        */

        var node = new GroupNoise([], [], [], [], [], []); //var node = new GroupNoise(internalVariables, actionMap, getters.ToArray(), internalGetters.ToArray(), setters.ToArray(), internalSetters.ToArray());
        return node;
    }


    public override NoiseNode? BuildExpression(Dictionary<NodeBase, Dictionary<string, SetterValue>> connections, List<Expression> expressions)
    {
        if (connections.ContainsKey(this))
            return null;
            
        Dictionary<string, SetterValue> setterValues = [];
        Dictionary<string, SetterValue> internalSetterValues = [];

        List<GetterValue> getters = [];
        List<SetterValue> setters = [];

        List<GetterValue> internalGetters = [];
        List<SetterValue> internalSetters = [];

        var nodeTree = NodeManager.GetNodeTree(Data.Nodes.GetConnectedNodeList());
        
        foreach (var (name, input) in InputFields)
        {
            GetterValue getter;
            Expression defaultValue = input.Value.GetExpression();
            if (input.Input == null || !input.Input.IsConnected || input.Input.Output == null)
            {
                getter = new(input.Value.GetValueType(), null, defaultValue);
            }
            else
            {
                if (connections.TryGetValue(input.Input.Output.Node, out var sets) && sets.TryGetValue(input.Input.Output.OutputField.Identifier, out var set))
                {
                    if (input.ConversionValue != null && set.Variable != null)
                    {
                        getter = new(input.Value.GetValueType(), ExpressionHelper.ConvertTo(input.ConversionValue.GetValueType(), input.Value.GetValueType(), set.Variable), defaultValue);
                    }
                    else
                    {
                        getter = new(set.Variable == null ? input.Value.GetValueType() : input.Input.Output.OutputField.Value.GetValueType(), set.Variable, defaultValue);
                    }
                }
                else
                {
                    getter = new(input.Value.GetValueType(), null, defaultValue);
                }
            }
            getters.Add(getter);

            if (Data.GroupInputNode != null && Data.GroupInputNode.OutputFields.TryGetValue(name, out var gInput))
            {
                SetterValue setter = new(gInput);
                internalSetters.Add(setter);
                internalSetterValues.Add(name, setter);
            }
        }

        if (Data.GroupInputNode != null)
            connections.Add(Data.GroupInputNode, internalSetterValues);

        List<NoiseNode> noiseNodes = [];

        for (int i = 0; i < internalSetters.Count; i++)
        {
            var expr = getters[i].GetExpression();
            var internalSetter = internalSetters[i];
            expressions.Add(Expression.Assign(internalSetter.GetVariable(), expr));
        }

        foreach (var ns in nodeTree)
        {
            var n = ns.BuildExpression(connections, expressions);
            if (n != null)
            {
                noiseNodes.Add(n);
                expressions.Add(NoiseNode.BuildExpression(n));
            }
        }

        foreach (var (name, output) in OutputFields)
        {
            if (Data.GroupOutputNode != null && Data.GroupOutputNode.InputFields.TryGetValue(name, out var gInput))
            {
                SetterValue setter = new SetterValue(output.Output.OutputField);
                setters.Add(setter);
                setterValues.Add(name, setter);

                GetterValue getter;
                Expression defaultValue = gInput.Value.GetExpression();
                if (gInput.Input == null || !gInput.Input.IsConnected || gInput.Input.Output == null)
                {
                    getter = new(gInput.Value.GetValueType(), null, defaultValue);
                }
                else
                {
                    if (connections.TryGetValue(gInput.Input.Output.Node, out var sets) && sets.TryGetValue(gInput.Input.Output.OutputField.Identifier, out var set))
                    {
                        if (gInput.ConversionValue != null && set.Variable != null)
                        {
                            getter = new(gInput.Value.GetValueType(), ExpressionHelper.ConvertTo(gInput.ConversionValue.GetValueType(), gInput.Value.GetValueType(), set.Variable), defaultValue);
                        }
                        else
                        {
                            getter = new(gInput.Value.GetValueType(), set.Variable, defaultValue);
                        }
                    }
                    else
                    {
                        getter = new(gInput.Value.GetValueType(), null, defaultValue);
                    }
                }
                internalGetters.Add(getter);
            }
        }

        for (int i = 0; i < setters.Count; i++)
        {
            var expr = internalGetters[i].GetExpression();
            var setter = setters[i];
            expressions.Add(Expression.Assign(setter.GetVariable(), expr));
        }

        setters.AddRange(internalSetters);
        foreach (var n in noiseNodes)
        {
            setters.AddRange(n.Setters);
        }

        connections.Add(this, setterValues);

        var node = new GroupNoise([], [], [.. getters], [..internalGetters], [.. setters], [..internalSetters]);
        return node;
    }


    public override NodeConfig Save()
    {
        var config = new GroupNodeConfig()
        {
            Name = "Group",
            File = GroupName,
            Position = [Position.X, Position.Y],
            GUID = ID.ToString()
        };
        foreach (var (_, output) in OutputFields)
        {
            config.Outputs.Add(output.GetUniqueName());
        }
        foreach (var input in InputFields.Values)
        {
            if (input.Input != null)
                config.Inputs.Add(input.GetUniqueName(), input.Input.Output?.OutputField.GetUniqueName() ?? null);
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
        Data.GroupNodeDisplayCollection.Delete();

        foreach (var node in Data.Nodes.Nodes)
        {
            node.DeleteCore();
        }

        Data.Inputs.Clear();    
        Data.Nodes.Nodes.Clear();
        Data.Nodes.Inputs.Clear();
        Data.Nodes.Outputs.Clear();

        foreach (var output in GetOutputs())
        {
            output.Disconnect();
        }

        InputFields = [];
        OutputFields = [];
        BlockIconCollections = [];
    }

    public override void DeleteNode()
    {
        base.DeleteNode();
        DeleteCore();
        Collection?.Delete();
        NodeManager.RemoveNode(this);
    }
}

public class GroupNodeUI(
    Vector2i position,
    Vector3 color,
    GroupNode node,
    GroupData data
) : UIScript
{
    public override UIElementBase Script() =>
    new UICol(Class(blank_round, rgba_[0, 0, 0, 0], left_[position.X], top_[position.Y], border_[5, 5, 5, 5], grow_children), Sub(
        new UICol(Class(blank_round, rgb_v3_[color], border_[0, 30, 0, 0], grow_children), Sub([
            new UIButton(Class(w_full_minus_[25], h_[30], bottom_[30]), OnClick<UIButton>(Select), OnHold<UIButton>(MoveNode), 
            OnHoverEnterButton(_ => 
            {
                if (GroupDisplay.connectionRenderer != null)
                    return;

                GroupDisplay.connectionRenderer = data.Nodes.ConnectionRenderer;
                data.GroupNodeDisplayCollection.UIController!.Transform.Disabled = false;
                data.GroupNodeDisplayCollection.SetVisible(true);

                if (data.Nodes.Nodes.Count > 0)
                {
                    Vector2 min = (float.MaxValue, float.MaxValue);
                    Vector2 max = (float.MinValue, float.MinValue);
                    int count = 0;
                    for (int i = 0; i < data.Nodes.Nodes.Count; i++)
                    {
                        var n = data.Nodes.Nodes[i];
                        if (n.Collection != null)
                        {
                            min.MinSet(min, n.Collection.TopLeft);
                            max.MaxSet(max, n.Collection.BottomRight);
                            count++;
                        }
                    }
                    Vector2 size = max - min;

                    int left = (int)min.X - 25;
                    int top = (int)min.Y - 25;

                    data.GroupNodeDisplayCollection.BaseOffset = (left, top);
                    data.GroupNodeDisplayCollection.Width = (int)size.X + 50;
                    data.GroupNodeDisplayCollection.Height = (int)size.Y + 50;
                    data.GroupNodeDisplayCollection.Border = (-left, -top, 0, 0);
                    data.GroupNodeDisplayCollection.ApplyChanges(UIChange.Scale);

                    if (count > 0)
                    {
                        Vector2 pos = (-min - size * 0.5f) * 0.4f + new Vector2(StructureNodeManager.NodePanelWidth, StructureNodeManager.NodePanelHeight) * 0.5f;
                        NodeManager.GroupDisplayController.SetScale(0.4f, (0, 0, 0));
                        NodeManager.GroupDisplayController.SetPosition((pos.X, pos.Y, 0));
                    }

                    GroupDisplay.UpdateDisplay(data.Nodes);
                }
            }),
            OnHoverExitButton(_ =>  { GroupDisplay.connectionRenderer = null; data.GroupNodeDisplayCollection.SetVisible(false); } ),
            OnHoverButton(_ => 
            { 
                var delta = Input.ScrollDelta.Y;
                if (delta != 0)
                {
                    if (Input.IsKeyDown(Key.ShiftLeft))
                    {
                        NodeManager.GroupDisplayController.SetPosition(NodeManager.GroupDisplayController.Position + (delta * 20, 0, 0));
                    }
                    else
                    {
                        NodeManager.GroupDisplayController.SetPosition(NodeManager.GroupDisplayController.Position + (0, delta * 20, 0));
                    }
                }

                if (Input.IsKeyPressed(Key.K))
                {
                    Console.WriteLine("Hovering Group node " + node.ID); 
                    Console.WriteLine("---- Nodes ----");
                    for (int i = 0; i < node.Data.Nodes.Nodes.Count; i++)
                    {
                        Console.WriteLine(node.Data.Nodes.Nodes[i].GetName());
                    }
                    Console.WriteLine("---- End ----");
                }
            })),
            new UIText($"Group {node.GroupName}", Class(mc_[node.GroupName.Length + 6], fs_[1], bottom_[20], left_[5])),
            new UIText("X", Class(top_right, mc_[1], fs_[1.2f], bottom_[20], right_[5]), OnClick<UIText>(DeleteNode)),
            new UIVCol(Class(blank_sharp_g_[30], grow_children), Sub(
                new UIVCol(Class(border_[5, 5, 5, 5], grow_children), Sub([
                    new UIHCol(Class(grow_children), Sub(
                        new UIVCol(Class(grow_children), Sub([
                            ..Foreach(data.Inputs, (name, type) => {
                                var button = new UIButton(w_[15], h_[15], blank_sharp, rgb_v3_[color], middle_left);
                                var field = new NodeInputField(button, node, new() { Name = name, Type = type });
                                node.InputFields.Add(name, field);
                                button.SetOnClick(_ => { if (field.Input != null) NodeBase.Connect(field.Input);});
                                return new UIVCol(Class(h_[30], grow_children), Sub([
                                    new UIHCol(Class(h_[30], spacing_[5], w_[130]), Sub([
                                        button,
                                        new UIText(name.Length <= 10 ? name : name[..10], Class(mc_[Mathf.Max(name.Length, 10)], fs_[1], middle_left))
                                    ])),
                                    field.Value.GetInputFields()
                                ]));
                            })
                        ])),
                        new UIVCol(Class(grow_children), Sub([
                            ..Foreach(data.Outputs, (name, output) => {
                                var button = new UIButton(w_[15], h_[15], blank_sharp, rgb_v3_[node.Color], middle_right);
                                var field = new NodeOutputField(button, node, new() { Name = name, Type = output.OutputField.Value.GetGLSLType() });
                                button.SetOnClick(_ => NodeBase.Connect(field.Output));
                                node.OutputFields.Add(name, field);
                                return new UICol(Class(h_[30], spacing_[5], w_[130], top_left), Sub([
                                    new UIText(name.Length <= 10 ? name : name[..10], Class(mc_[Mathf.Min(name.Length, 10)], fs_[1], middle_left)),
                                    button
                                ]));
                            })
                        ]))
                    ))
                ]))
            ))
        ]))
    ));

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