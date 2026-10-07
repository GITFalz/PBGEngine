using PBG.Core;
using PBG.Data;
using PBG.MathLibrary;
using PBG.UI;

namespace PBG.Nodes;

[InternalSystemInit(InitPriority.ResourceManager)]
public class NodeSelection(NodeModule nodeModule) : ScriptingNode
{
    private static Dictionary<string, NodeSelectionData> _selections = [];

    private UIElementBase _ui = null!;
    private UIController _selectionController = null!;
    private NodeModule _nodeModule = nodeModule;

    private UIVScroll _leftScroll = null!;
    private UICol _rightCollection = null!;
    
    public static void Init()
    {
        void GetOrAdd(Dictionary<string, NodeSelectionData> list, NodeTemplate template, string[] sections, int index = 0)
        {
            if (index == sections.Length)
            {
                list[template.Name] = new(template);
                return;
            }

            var section = sections[index];
            if (!list.TryGetValue(section, out var data))
            {
                data = new NodeSelectionData();
                data.Selections ??= []; // otherwise it breaks for some reason
                list[section] = data;
            }
            
            GetOrAdd(data.Selections, template, sections, index + 1);
        }

        foreach (var (name, template) in NodeTemplate.NodeTemplates)
        {
            if (template.Selections.Count == 0)
                template.Selections.Add("Unsorted");

            foreach (var selection in template.Selections)
            {
                var sections = selection.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                GetOrAdd(_selections, template, sections);
            }
        }
    }

    static void PrintSelections(Dictionary<string, NodeSelectionData> selections, int indent = 0)
    {
        string prefix = new string(' ', indent * 2);

        foreach (var (name, data) in selections)
        {
            if (data.Template != null)
            {
                // Leaf node
                Console.WriteLine($"{prefix}• {name}  →  {data.Template.Name}");
            }
            else
            {
                // Section / category
                Console.WriteLine($"{prefix}[{name}]");
            }

            // Recurse into children
            if (data.Selections.Count > 0)
            {
                PrintSelections(data.Selections, indent + 1);
            }
        }
    }

    /*
    
    */

    void Start()
    {
        _leftScroll = new UIVScroll(w_full, h_full, mask_children, spacing_[5], border_[5, 5, 5, 5], border_ui_[0, 0, 2, 0], border_color_[NodeBase.Border]);
        _rightCollection = new UICol(w_[65f], h_full_minus_[2], mask_children, bottom_left);

        _ui = new UICol(w_[300], h_[300], middle_center, blank_round, rgba_v4_[NodeBase.NodeBody], border_ui_[2, 2, 2, 2], border_color_[NodeBase.Border])[
            new UICol(w_full, h_[30], rgba_v4_[NodeBase.NodeHeader], blank_round_2)[
                new UIText("Selection", left_[10], middle_left, fs_[1.2f]) 
            ],
            new UIHCol(w_full, h_full_minus_[30], bottom_left, border_ui_[0, 2, 0, 0], border_color_[NodeBase.Border])[
                new UICol(w_[35f], h_full_minus_[2], bottom_left)[
                    _leftScroll
                ],
                _rightCollection
            ]
        ];

        foreach (var (name, selection) in _selections)
        {
            var leftCol = new UICol(w_full_minus_[12], h_[25], blank_sharp, hover_color_[Vector4.Zero, NodeBase.Border], hover_color_easeinout, hover_color_duration_[0.1f])[
                new UIText(name, left_[10], middle_left, fs_[1.2f])
            ];

            leftCol.OnClick(_ => {
                _rightCollection.DeleteChildren();
                GenerateRightPanel(selection, _rightCollection);
            });

            _leftScroll.AddElement(leftCol);
        }

        void GenerateRightPanel(NodeSelectionData data, UICol collection)
        {
            var scroll = new UIVScroll(w_full, h_full, mask_children, spacing_[5], border_[5, 5, 5, 5]);

            foreach (var (name, selection) in data.Selections)
            {
                if (selection.Template != null)
                {
                    var template = new UICol(w_full_minus_[12], h_[25], blank_sharp, hover_color_[Vector4.Zero, NodeBase.Border], hover_color_easeinout, hover_color_duration_[0.1f])[
                        new UIText(selection.Template.Name, left_[10], middle_left, fs_[1.2f])
                    ];

                    template.OnClick(_ => AddTemplate(selection.Template));

                    scroll.AddElement(template);
                    continue;
                }

                var rightCol = new UICol(w_full_minus_[12], h_[25], blank_sharp, hover_color_[Vector4.Zero, NodeBase.Border], hover_color_easeinout, hover_color_duration_[0.1f])[
                    new UIText(name, left_[10], middle_left, fs_[1.2f])
                ];

                rightCol.OnClick(_ => {
                    _rightCollection.DeleteChildren();
                    GenerateRightPanel(selection, _rightCollection);
                });

                scroll.AddElement(rightCol);
            }

            collection.AddElement(scroll);
            _selectionController.AddElement(scroll);
        }


        _selectionController = Transform.GetComponent<UIController>();
        _selectionController.AddElement(_ui);
        _selectionController.Show = false;
    }

    private void AddTemplate(NodeTemplate template)
    {
        var node = new NodeBase(template);
        _nodeModule.AddNode(node);
        _nodeModule.MoveToFront(node);
        Close();
    }

    private void Close()
    {
        _selectionController.Show = false;
    }

    void Update()
    {
        if (Transform.Disabled)
            return;
            
        if (Input.IsMousePressed(MouseButton.Right))
        {
            if (_selectionController.Show)
            {
                _selectionController.Show = false;
            }
            else
            {
                _selectionController.Show = true;
                _leftScroll.ScrollToTop();            
            }
        }
    }


    private struct NodeSelectionData(NodeTemplate? template = null)
    {
        public NodeTemplate? Template = template;
        public Dictionary<string, NodeSelectionData> Selections = [];

        public UIElementBase? GenerateTemplateButton()
        {
            if (Template == null)
                return null;

            return new UICol(w_full_minus_[12], h_[25], blank_sharp, hover_color_[Vector4.Zero, NodeBase.Border], hover_color_easeinout, hover_color_duration_[0.1f])[
                new UIText(Template.Name, left_[10], middle_left, fs_[1.2f])
            ];
        }
    }
}