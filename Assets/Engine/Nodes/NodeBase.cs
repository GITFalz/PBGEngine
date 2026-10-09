using PBG.Core;
using PBG.Data;
using PBG.MathLibrary;
using PBG.UI;

namespace PBG.Nodes;

[InternalSystemInit(InitPriority.Data)]
public class NodeBase
{
    private static uint _count = 0;

    private static bool _hoveringConnection = false;
    private static NodeBase? _hoverNode = null;
    private static int _hoverNodeIndex = -1;
    private static NodeConnectionType? _hoverConnectionType = null;

    public readonly NodeTemplate Template;

    public readonly uint ID;
    public readonly string Name;
    public readonly Vector4 Color;
    public readonly NodeDefinitionType DefinitionType;

    public int Priority { get; private set; }

    public UIElementBase Ui { get; private set; }
    private UIController _controller => Ui.UIController ?? throw new Exception("UI controller doesn't exist");
    private NodeModule _nodeModule = null!;

    //private static NodeRegistry _nodeRegistry = new();

    private const float FONT_SIZE = 1.2f;

    public readonly bool HasInputs;
    public readonly bool HasOutputs;
    public readonly bool HasBlocks;

    public NodeInput FlowInput { get; private set; }
    public NodeOutput FlowOutput { get; private set; }

    public readonly NodeInput[] Inputs;
    public readonly NodeOutput[] Outputs;
    public readonly NodeBlock[] Blocks;

    public List<int> InputDependencies = [];


    public NodeBase? ParentBlockNode = null;
    public int ParentBlockIndex = -1;

    public NodeBase(NodeTemplate template)
    {
        Template = template;

        ID = _count;
        Name = template.Name;
        Color = template.Color;
        DefinitionType = template.DefinitionType;

        _count++;

        HasInputs = template.Inputs.Count > 0;
        HasBlocks = template.Blocks.Count > 0;
        HasOutputs = template.Outputs.Count > 0 && !HasBlocks;

        Inputs = new NodeInput[template.Inputs.Count];
        Blocks = new NodeBlock[template.Blocks.Count];
        Outputs = new NodeOutput[HasOutputs ? template.Outputs.Count : 0];

        InputDependencies = [..template.InputDependencies];

        Ui = UI(ID + " " + Name, template);
    }

    public int HoveringBlockIndex = -1;
    public static NodeBase? HoveringBlockNode = null;


    private UIElementBase UI(string name, NodeTemplate template) =>
    new UICol(grow_children, blank_round, rgba_v4_[NodeBody], border_ui_[2, 2, 2, 2], border_[2, 2, 2, 2], border_color_[Border], min_w_[50 + (int)(name.Length * (7 * FONT_SIZE))])[
        new UICol(grow_children, rgba_v4_[NodeHeader], blank_round_2, w_full, border_ui_[0, 0, 0, 2], border_color_[Border])[
            new UIButton(h_full, w_full_minus_[30])
                .OnClick(Click)
                .OnHold(Move)
                .OnRelease(OnReleaseNode),
            new UICol(h_[32], w_[7], mask_children)[
                new UIImg(h_[26], w_[20], top_[2], left_[2], blank_round_1, rot_270, rgba_v4_[Color])
            ],
            new UIText(name, fs_[1.2f], middle_left, left_[17], bottom_[1]),
            new UICol(w_[20], h_[20], blank_sharp, rgba_v4_[PBG.MathLibrary.Color.Invisible], middle_right, right_[5], bottom_[1])[
                new UIImg(w_[20], h_[20], icon_[15], rgba_v4_[NodeDelete], middle_center)
                    .OnClick(_ => Delete())
                    .OnHover(delete => { delete.ParentElement?.UpdateColor(NodeDeleteDark); })
                    .OnHoverExit(delete => { delete.ParentElement?.UpdateColor(PBG.MathLibrary.Color.Invisible); })
            ]
        ],
        new UIVCol(grow_children, top_[32], min_w_full)[
            new UIHCol(grow_children, min_w_full, spacing_[15], border_[0, 0, 0, 0])[
                new UIVCol(grow_children, border_[0, 10, 0, 10], spacing_[10])[
                    new Run(() =>
                    {
                        var icon = new UIImg(w_[10], h_[10], blank_sharp, middle_left, right_[5], rgba_v4_[PortHover]);
                        FlowInput = new NodeInput("Flow", -1, icon, NodeDataType.Execute, NodeConnectionType.FlowInput);
                        return new UICol(grow_children)[
                            new UICol(w_[10], h_[10], right_[2], mask_children)
                                .OnClick(_ => OnClickConnection(NodeConnectionType.FlowInput, 0))
                                .OnHold(_ => OnHoldConnection(NodeConnectionType.FlowInput, 0))
                                .OnRelease(_ => OnReleaseConnection())
                                .OnHoverEnter(_ => OnHoverEnterConnection(NodeConnectionType.FlowInput, 0))
                                .OnHover(_ => _hoveringConnection = true)
                                .OnHoverExit(_ => _hoveringConnection = false)
                            [
                                icon
                            ],
                            new UIText("Flow", left_[17], fs_[1.2f])
                        ];
                    }),
                    new If(HasInputs, () =>  
                        new Foreach<NodePinTemplate>(template.Inputs, (index, input) =>
                        {
                            UIElementBase element;
                            NodeInput nodeInput;
                            if (input.ValueType == null)
                            {
                                var icon = new UIImg(w_[10], h_[10], blank_sharp, middle_left, right_[5], rgba_v4_[InputPort]);
                                nodeInput = new NodeInput(input.Name, index, icon, input.Type, NodeConnectionType.Input);
                                element = new UICol(grow_children)[
                                    new UICol(w_[10], h_[10], right_[2], mask_children)
                                        .OnClick(_ => OnClickConnection(NodeConnectionType.Input, index))
                                        .OnHold(_ => OnHoldConnection(NodeConnectionType.Input, index))
                                        .OnRelease(_ => OnReleaseConnection())
                                        .OnHoverEnter(_ => OnHoverEnterConnection(NodeConnectionType.Input, index))
                                        .OnHover(_ => _hoveringConnection = true)
                                        .OnHoverExit(_ => _hoveringConnection = false)
                                    [
                                        icon
                                    ],
                                    new UIText(input.Name, left_[17], fs_[1.2f])
                                ];
                            }
                            else
                            {
                                Type type = input.ValueType;
                                UIElementBase? icon = null;
                                UICol? iconButton = null;
                                if (input.HasInput)
                                {
                                    icon = new UIImg(w_[10], h_[10], blank_sharp, middle_left, right_[5], rgba_v4_[InputPort]);
                                    iconButton = new UICol(w_[10], h_[10], right_[2], mask_children)
                                        .OnClick(_ => OnClickConnection(NodeConnectionType.Input, index))
                                        .OnHold(_ => OnHoldConnection(NodeConnectionType.Input, index))
                                        .OnRelease(_ => OnReleaseConnection())
                                        .OnHoverEnter(_ => OnHoverEnterConnection(NodeConnectionType.Input, index))
                                        .OnHover(_ => _hoveringConnection = true)
                                        .OnHoverExit(_ => _hoveringConnection = false)
                                    [
                                        icon
                                    ];
                                }

                                nodeInput = new NodeInput(input.Name, index, icon, input.Type, NodeConnectionType.Input)
                                {
                                    Value = input.DefaultValue
                                };

                                element = new UICol(grow_children)[
                                    iconButton,
                                    new UIText(input.Name, left_[17], fs_[1.2f]),
                                    NodePinTemplate.GenerateTypeFields(this, index, input)
                                ];
                            }

                            Inputs[index] = nodeInput;
                            return element;
                        })
                    )
                ],
                new If(HasBlocks, () => 
                    new UIVCol(grow_children, border_[0, 10, 0, 10], spacing_[10])[
                        new Foreach<string>(template.Blocks, (index, block) =>
                        {
                            var col = new UICol(grow_children, min_h_[50], min_w_[50], blank_round, rgba_v4_[Background], border_ui_[2, 2, 2, 2], border_[10, 10, 10, 10], border_color_[Border]);
                            var nodeBlock = new NodeBlock(block, index, col);
                            Blocks[index] = nodeBlock;
                            return new UIVCol(grow_children, spacing_[10])[
                                new UIText(block, fs_[1.2f]),
                                    col.OnHover(e => OnHoverBlock(e, index)).OnHoverExit(e => OnHoverExitBlock(e, index))
                            ];
                        })
                    ]
                ),
                new UIVCol(grow_children, border_[0, 10, 0, 10], spacing_[10], top_right)[
                    new Run(() =>
                    {
                        var icon = new UIImg(w_[10], h_[10], blank_sharp, middle_right, left_[5], rgba_v4_[PortHover]);
                        FlowOutput = new NodeOutput("Flow", -1, icon, NodeDataType.Execute, NodeConnectionType.FlowOutput);
                        return new UICol(grow_children, min_w_full)[
                            new UICol(top_right, w_[10], h_[10], left_[2], mask_children)
                                .OnClick(_ => OnClickConnection(NodeConnectionType.FlowOutput, 0))
                                .OnHold(_ => OnHoldConnection(NodeConnectionType.FlowOutput, 0))
                                .OnRelease(_ => OnReleaseConnection())
                                .OnHoverEnter(_ => OnHoverEnterConnection(NodeConnectionType.FlowOutput, 0))
                                .OnHover(_ => _hoveringConnection = true)
                                .OnHoverExit(_ => _hoveringConnection = false)
                            [
                                icon
                            ]
                        ];
                    }),
                    new If(HasOutputs, () => 
                        new Foreach<NodePinTemplate>(template.Outputs, (index, output) =>
                        {
                            var icon = new UIImg(w_[10], h_[10], blank_sharp, middle_right, left_[5], rgba_v4_[OutputPort]);
                            var nodeOutput = new NodeOutput(output.Name, index, icon, output.Type, NodeConnectionType.Output);
                            Outputs[index] = nodeOutput;
                            return new UICol(grow_children)[
                                new UICol(top_right, w_[10], h_[10], left_[2], mask_children)
                                    .OnClick(_ => OnClickConnection(NodeConnectionType.Output, index))
                                    .OnHold(_ => OnHoldConnection(NodeConnectionType.Output, index))
                                    .OnRelease(_ => OnReleaseConnection())
                                    .OnHoverEnter(_ => OnHoverEnterConnection(NodeConnectionType.Output, index))
                                    .OnHover(_ => _hoveringConnection = true)
                                    .OnHoverExit(_ => _hoveringConnection = false)
                                [
                                    icon
                                ],
                                new UIText(output.Name, fs_[1.2f], middle_right, right_[17])
                            ];
                        })  
                    )
                ]
            ],
            new If(DefinitionType.HasFlag(NodeDefinitionType.Priority), () => 
                new UICol(w_full, h_[32], border_[0, 2, 0, 0], border_ui_[0, 2, 0, 0], border_color_[Border])[
                    new UIText("Prio", fs_[1f], middle_left, left_[10], rgba_v4_[TextSecondary]),
                    new UICol(h_[20], middle_right, right_[5], w_[30], blank_sharp, rgba_v4_[Background])[
                        new UIField("0", fs_[1f], mc_[3], middle_center, text_type_numeric)
                    ]
                ]
            )
        ]
    ];

    public int GetBlockDepth(int depth = 0) => ParentBlockNode == null ? depth : ParentBlockNode.GetBlockDepth(depth + 1);
    public bool HasParentBlock(NodeBase node, int index)
    {
        if (ParentBlockNode == null)
            return false;

        return (ParentBlockNode == node && ParentBlockIndex == index) || ParentBlockNode.HasParentBlock(node, index);
    }
    private UICol GetHoverBlockCollection() => Blocks[HoveringBlockIndex].Collection;

    private void Click(UIElementBase col)
    {
        _nodeModule.MoveToFront(this);
        col.SetAsHighestPriority();
        col.AbortInteractions();
        _clickOffset = Input.MousePosition * (1f / _controller.Scale) - Ui.Origin;
    }

    private Vector2 _clickOffset = Vector2.Zero;
    private void Move(UIElementBase col)
    {
        var delta = Input.MouseDelta;
        if (delta == Vector2.Zero)
            return;

        CheckAddToBlock();
        CheckRemoveFromBlock();

        _nodeModule?.MoveConnections();

        var offset = Ui.ParentElement?.Origin + (10, 10) ?? Vector2.Zero;

        Ui.BaseOffset = Input.MousePosition * (1f / _controller.Scale) - offset - _clickOffset;
        Ui.GetBaseParent().ApplyChanges(UIChange.Scale);

        col.AbortInteractions();
    }

    private void OnReleaseNode(UIElementBase col)
    {
        if (ParentBlockNode != null)
        {
            var block = ParentBlockNode.Blocks[ParentBlockIndex];
            Ui.BaseOffset = Mathf.Max(Ui.Origin - block.Collection.Origin, Vector2.Zero);
            Ui.GetBaseParent().ApplyChanges(UIChange.Scale);
        }

        _nodeModule?.MoveConnections();
    }

    private void CheckAddToBlock()
    {
        if (HoveringBlockNode != null)
        {
            if (ParentBlockNode == null)
            {
                if (HoveringBlockNode == this)
                    return;

                var block = HoveringBlockNode.Blocks[HoveringBlockNode.HoveringBlockIndex];

                Ui.RemoveFromParent();
                block.Collection.AddElement(Ui);

                ParentBlockNode = HoveringBlockNode;
                ParentBlockIndex = HoveringBlockNode.HoveringBlockIndex;
            }
        }
    }

    private void CheckRemoveFromBlock()
    {
        if (HoveringBlockNode == null)
        {
            if (ParentBlockNode != null)
            {
                Ui.RemoveFromParent();

                ParentBlockNode.Ui.GetBaseParent().ApplyChanges(UIChange.Scale);
                ParentBlockNode = null;      
            }
        }
    }

    private void OnClickConnection(NodeConnectionType connectionType, int index)
    {
        if (connectionType == NodeConnectionType.Input)
        {
            _nodeModule.RemoveConnection(this, Inputs[index]);
        }
        else if (connectionType == NodeConnectionType.FlowOutput)
        {
            _nodeModule.RemoveConnection(this, FlowOutput);
        }
        else if (connectionType == NodeConnectionType.FlowInput)
        {
            _nodeModule.RemoveConnection(this, FlowInput);
        }
    }

    private void OnHoldConnection(NodeConnectionType connectionType, int index)
    {
        var delta = Input.MouseDelta;
        if (delta == Vector2.Zero)
            return;

        if (_nodeModule.DragConnection == null)
        {
            _nodeModule.DragConnection = new()
            {
                Node = this,
                Index = index,
                Type = connectionType
            };
            _nodeModule.ModifyConnections();
        }
        else
        {
            _nodeModule.MoveConnections();
        }
    }

    private void OnReleaseConnection()
    {
        if (_hoveringConnection && _nodeModule.DragConnection != null && _hoverNode != null)
        {
            var drag = _nodeModule.DragConnection.Value;
            if (drag.Type == NodeConnectionType.Output && _hoverConnectionType == NodeConnectionType.Input)
            {
                var outputNode = drag.Node;
                var inputNode = _hoverNode;

                var output = outputNode.Outputs[drag.Index];
                var input = inputNode.Inputs[_hoverNodeIndex];

                if (!_nodeModule.IsConnected(inputNode, input))
                    _nodeModule.TryAddConnection(outputNode, output, inputNode, input);
            }
            else if (drag.Type == NodeConnectionType.Input && _hoverConnectionType == NodeConnectionType.Output)
            {
                var outputNode = _hoverNode;
                var inputNode = drag.Node;

                var output = outputNode.Outputs[_hoverNodeIndex];
                var input = inputNode.Inputs[drag.Index];

                _nodeModule.TryAddConnection(outputNode, output, inputNode, input);
            } 
            else if (drag.Type == NodeConnectionType.FlowOutput && _hoverConnectionType == NodeConnectionType.FlowInput)
            {
                var outputNode = drag.Node;
                var inputNode = _hoverNode;

                var output = outputNode.FlowOutput;
                var input = inputNode.FlowInput;

                if (!_nodeModule.IsConnected(inputNode, input))
                    _nodeModule.TryAddConnection(outputNode, output, inputNode, input);
            }
            else if (drag.Type == NodeConnectionType.FlowInput && _hoverConnectionType == NodeConnectionType.FlowOutput)
            {
                var outputNode = _hoverNode;
                var inputNode = drag.Node;

                var output = outputNode.FlowOutput;
                var input = inputNode.FlowInput;

                if (!_nodeModule.IsConnected(outputNode, output))
                    _nodeModule.TryAddConnection(outputNode, output, inputNode, input);
            }
        }

        if (_nodeModule.DragConnection != null)
        {
            _nodeModule.DragConnection = null;
            _nodeModule.ModifyConnections();
        }
    }

    private void OnHoverEnterConnection(NodeConnectionType connectionType, int index)
    {
        _hoverNode = this;
        _hoverNodeIndex = index;
        _hoverConnectionType = connectionType;
    }

    private void OnHoverBlock(UICol e, int index)
    { 
        if (HoveringBlockNode == null || HoveringBlockNode.GetBlockDepth() < GetBlockDepth())
        {
            HoveringBlockNode?.GetHoverBlockCollection().UpdateColor(Background);

            e.UpdateColor(Border);

            HoveringBlockIndex = index;
            HoveringBlockNode = this;

            _nodeModule.MoveToFront(this);

            e.SetAsHighestPriority();
            e.AbortInteractions();
        }
    }

    private void OnHoverExitBlock(UICol e, int index)
    {
        e.UpdateColor(Background);

        if (HoveringBlockIndex == index)
        {
            HoveringBlockIndex = -1;
            HoveringBlockNode = null;
        }
    }

    public void SetDepth(int depth)
    {
        Ui.Depth = depth;
        Ui.ApplyChanges(UIChange.Transform);
    }
    
    public void SetParentModule(NodeModule module)
    {
        _nodeModule = module;
    }

    public UIElementBase GetUI() => Ui;



    public void Delete()
    {
        if (HasBlocks)
        {
            if (ParentBlockNode != null)
            {
                var subCol = ParentBlockNode.Blocks[ParentBlockIndex].Collection;
                foreach (var node in _nodeModule.Nodes)
                {
                    if (node.ParentBlockNode == this)
                    {
                        node.Ui.RemoveFromParent();
                        subCol.AddElement(node.Ui);
                        node.Ui.BaseOffset = node.Ui.Origin - subCol.Origin;
                        node.Ui.GetBaseParent().ApplyChanges(UIChange.Scale);
                        node.ParentBlockNode = null;
                    }
                }
            }
            else
            {
                foreach (var node in _nodeModule.Nodes)
                {
                    if (node.ParentBlockNode == this)
                    {
                        node.Ui.RemoveFromParent();
                        node.Ui.BaseOffset = node.Ui.Origin;
                        node.Ui.ApplyChanges(UIChange.Scale);
                        node.ParentBlockNode = null;
                    }
                }
            }
        }

        _nodeModule?.RemoveNode(this);
        Ui.Delete();
    }

    public static void Init()
    {
        /*
        _nodeRegistry.Register(
            new NodeTemplate("Add")
                .Input("A", NodeDataType.)
        );
        */
    }

    

    // --- existing, unchanged ---
    public static readonly Color Background     = new Color("#0f1115");
    public static readonly Color NodeBody       = new Color("#1a1d24");
    public static readonly Color NodeHeader     = new Color("#252a33");
    public static readonly Color Border         = new Color("#2e3440");
    public static readonly Color TextPrimary    = new Color("#e6eaf0");
    public static readonly Color TextSecondary  = new Color("#8b93a7");
    public static readonly Color Accent         = new Color("#3b82f6");
    public static readonly Color InputPort      = new Color("#22c55e");
    public static readonly Color OutputPort     = new Color("#f59e0b");
    public static readonly Color Mouse          = new Color("#38bdf8");
    public static readonly Color NodeDelete     = new Color("#ef4444");
    public static readonly Color NodeDeleteDark = new Color("#9128284f");
    public static readonly Color PortHover      = new Color("#ffffff");

    // --- additions ---
    public static readonly Color TextMuted        = new Color("#565d6d"); // coords, timestamps, disabled labels — a step below TextSecondary
    public static readonly Color BackgroundGrid   = new Color("#181b22"); // canvas dot-grid, barely lifted off Background so it doesn't compete with wires
    public static readonly Color NodeBodySelected = new Color("#20242e"); // selected node fill, paired with a Border set to Accent
    public static readonly Color BorderSelected   = new Color("#5b9bfb"); // brighter than Accent — selection outline needs to read at 1px
    public static readonly Color ConnectionIdle   = new Color("#3e4759"); // resting wire color for untyped/exec links, before Input/Output tinting takes over
    public static readonly Color CommentBox       = new Color("#211f2c"); // soft violet, for comment/group frames — distinct from NodeBody so groups read as background, not nodes
    public static readonly Color CommentBorder    = new Color("#3a3650");
    public static readonly Color Warning          = new Color("#eab308"); // reserved for validation/warning states — kept distinct from OutputPort's amber
}