using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.Assets.Scripts.NoiseNodes.Nodes;
using PBG.Data;
using PBG.LoaderConfig;
using PBG.UI;
using PBG.UI.Creator;

using static PBG.UI.Styles;

public class StructureNode : NodeBase
{
    public string Name = "<";
    public UICol SubCollection = null!;
    
    public StructureNode(NodeCollection nodeCollection, StructureNodeConfig config) : this(config.GUID, nodeCollection, (config.Position[0], config.Position[1]), config.StructureName)
    {
        OutputPriority = config.OutputPriority;
    }

    public StructureNode(string guid, NodeCollection nodeCollection, Vector2 pos, string name) : base(guid, nodeCollection)
    {
        Vector2 position = Mathf.Round(pos / 10f) * 10f;
        Position = position;
        GridPosition = position;
        _position = position;
        Color = (0.231f, 0.357f, 0.573f);
        Name = name;
    }

    public StructureNode(NodeCollection nodeCollection, Vector2 pos, string name) : base(null, nodeCollection)
    {
        Vector2 position = Mathf.Round(pos / 10f) * 10f;
        Position = position;
        GridPosition = position;
        _position = position;
        Color = (0.231f, 0.357f, 0.573f);
        Name = name;
    }

    public StructureNode InitUI()
    {
        Collection = new StructureUI(this, Mathf.FloorToInt(Position), Color);
        return this;
    }

    public StructureNode InitBlankUI()
    {
        InputFields.Add("Height", new(new(), this, new() { Name = "Height", Type = "float" }));
        return this;
    }

    public override string GetName() => "StructureNode " + ID;

    public StructurePlacementNode GenerateNoiseNode(NoiseNodeManager manager, ref int index) => GenerateNoiseNode([], ref index);
    public StructurePlacementNode GenerateNoiseNode(NoiseValue[] variables, ref int index, int indexOffset = 0)
    {
#if MYDEBUG
        Console.WriteLine($"[Node] : Generating noise node for node '{GetType().Name}'");
#endif
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
                variables[index] = input.Value.GetNoiseValue();
#if MYDEBUG
                Debug.Print($"[Node] : Getter value is constant with index: {index}");
#endif
                index++;
            }
            else
            {
                getter = new GetterValue(variables, input.Input.Output.Index + indexOffset);
#if MYDEBUG
                Console.WriteLine($"[Node] : Getter value is variable with index: {input.Input.Output.Index + indexOffset}");
#endif
            }
            getters.Add(getter);
        }
        */

        return new StructurePlacementNode([.. getters], [], Name);
    }

    public override NoiseNode? BuildExpression(Dictionary<NodeBase, Dictionary<string, SetterValue>> connections, List<Expression> expressions)
    {
        List<GetterValue> getters = [];

        foreach (var (name, input) in InputFields)
        {
            if (input.IsExternal)
                continue;
            
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
        }

        return new StructurePlacementNode([.. getters], [], Name);
    }

    public override StructureNodeConfig Save()
    {
        var config = new StructureNodeConfig()
        {
            Name = "Structure",
            StructureName = Name,
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
    }

    public override void DeleteNode()
    {
        base.DeleteNode();
        DeleteCore();
        Collection?.Delete();
        NodeManager.RemoveNode(this);
    }
}

public class StructureUI(
    StructureNode node,
    Vector2i position,
    Vector3 color
) : UIScript
{
    private UICol? _type = null;

    public override UIElementBase Script() =>
    new UICol(Class(blank_round, rgba_[0, 0, 0, 0], left_[position.X], top_[position.Y], border_[5, 5, 5, 5], grow_children), Sub(
        new UICol(Class(blank_round, rgb_v3_[color], border_[0, 30, 0, 0], grow_children), Sub([
            new UIButton(Class(w_full_minus_[25], h_[30], bottom_[30]), OnClick(Select), OnHold(MoveNode)),
            new UIText($"Structure", Class(mc_[6], fs_[1], bottom_[20], left_[5])),
            new UIText("X", Class(top_right, mc_[1], fs_[1.2f], bottom_[20], right_[5]), OnClick(DeleteNode)),
            new UICol(Class(grow_children), Sub(
                new UIVCol(Class(blank_sharp_g_[30], grow_children), Sub(
                    new UIVCol(Class(border_[5, 5, 5, 5], grow_children, spacing_[5]), Sub([
                        new UIHCol(Class(grow_children), Sub(
                            new UIVCol(Class(grow_children), Sub([
                                ..Run(() => {
                                    var button = new UIButton(w_[15], h_[15], blank_sharp, rgb_v3_[color], middle_left);
                                    var field = new NodeInputField(button, node, new() { Name = "Height", Type = "float" });
                                    node.InputFields.Add("Height", field);
                                    button.Dataset["field"] = field;
                                    return new CustomNodeUIInput("Height", button, field);
                                })
                            ]))
                        )),
                        new UICol(Class(w_full, h_[30]), Sub(
                            new UICol(Class(w_[50], h_full, blank_sharp_g_[10], middle_right), Sub(
                                new UIField(""+node.OutputPriority, Class(mc_[5], middle_right, right_[5]), OnTextChange(f => node.OutputPriority = f.GetInt()))
                            )),
                            new UIText("Priority", Class(middle_left))
                        ))
                    ]))
                ))
            ))
        ]))
    ));

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