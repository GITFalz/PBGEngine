using PBG.MathLibrary;
using PBG;
using PBG.Core;
using PBG.Data;
using PBG.UI;
using PBG.UI.Creator;
using static PBG.UI.Styles;

public class NodeSelector : ScriptingNode
{
    public UIController Controller = null!;
    public UIElementBase MainCollection = null!;
    public UIElementBase? SideCollection = null!;

    public Vector2 Position = Vector2.Zero;

    private Dictionary<string, SelectorCollection> _collections = [];

    private Action _regenerateGroupList = () => { };

    void Start()
    {
        Controller = Transform.GetComponent<UIController>();

        foreach (var (name, definition) in NodeDefinitionLoader.NodeDefinitions)
        {
            foreach (var selector in definition.Selectors)
            {
                var collections = selector.Split('/').Select(s => s.Trim()).ToList();
                CollectionCreation(collections, definition);
            }
        }

        MainCollection = new SelectorUI(this, _collections);
        var side = MainCollection.GetElement<UICol>() ?? throw new ArgumentNullException("Side collection in the node selector was not found");
        SideCollection = side;
        Controller.AddElement(MainCollection);
    }

    public void UpdatePosition()
    {
        Position = Input.GetMousePosition();
        MainCollection.SetVisible(!MainCollection.Visible);
        if (MainCollection.Visible)
        {
            SideCollection?.SetVisible(false);
            Controller.SetPosition(new Vector3(Position.X, Position.Y, 0f));
        }
    }

    private void CollectionCreation(List<string> collections, NodeDefinition definition)
    {
        if (collections.Count == 0)
            return;
        string currentName = collections[0];
        if (!_collections.TryGetValue(currentName, out SelectorCollection? collection))
        {
            collection = new SelectorCollection
            {
                Name = currentName,
                SubCollections = new Dictionary<string, SelectorCollection>(),
                IsNodeCreationModule = false,
                HasType = false,
                Type = ""
            };
            _collections[currentName] = collection;
        }

        collections.RemoveAt(0);
        CreateCollection(collection, collections, definition);
    }

    private void CreateCollection(SelectorCollection collection, List<string> collections, NodeDefinition definition)
    {
        if (collections.Count == 0)
        {
            foreach (var action in definition.Actions)
            {
                try
                {
                    collection.SubCollections.Add(action.Type, new SelectorCollection
                    {
                        Name = definition.Name,
                        Type = action.Type,
                        IsNodeCreationModule = true,
                        HasType = true,
                    });
                }
                catch (Exception ex)
                {
                    throw new Exception($"Action type {action.Type} was already added", ex);
                }
            }
            return;
        }

        string currentName = collections[0];
        if (!collection.SubCollections.TryGetValue(currentName, out SelectorCollection? subCollection))
        {
            subCollection = new SelectorCollection
            {
                Name = currentName,
                SubCollections = [],
                IsNodeCreationModule = false,
                HasType = false,
                Type = ""
            };
            collection.SubCollections[currentName] = subCollection;
        }

        collections.RemoveAt(0);
        CreateCollection(subCollection, collections, definition);
    }

    public void RegenerateGroupList() => _regenerateGroupList();

    private class SelectorCollection
    {
        public string Name;
        public Dictionary<string, SelectorCollection> SubCollections = [];
        public bool IsNodeCreationModule;
        public bool HasType;
        public string Type = "";
    }

    class SelectorUI(NodeSelector selector, Dictionary<string, SelectorCollection> Collections) : UIScript
    {
        public override UIElementBase Script() =>
        new UICol(Class(grow_children, blank_sharp_g_[20], border_[5, 5, 5, 5], invisible), Sub([
            ..Run(() =>
            {
                var mainScroll = new UIVScroll(w_[200], h_[400], blank_sharp_g_[10], border_[5, 5, 5, 5], spacing_[5], mask_children);
                var bg = new UIImg(w_[200], h_[400], left_[205], blank_sharp_g_[10]);
                var sideScroll = new UICol(w_[200], h_[400], left_[205], spacing_[5], mask_children);
                foreach (var (name, collection) in Collections)
                {
                    var col = new UICol(w_[190], h_[30], blank_sharp_g_[20]);
                    var text = new UIText(name, middle_center, mc_[name.Length], fs_[1]);
                    col.AddElement(text);
                    mainScroll.AddElement(col);

                    var subButtonCollection = new UIVScroll(spacing_[5], w_[190], h_[30], border_[5, 5, 5, 5], grow_children);
                    CreateUIElements(collection, subButtonCollection, sideScroll);
                    sideScroll.AddElement(subButtonCollection);

                    col.SetOnClick(c =>
                    {
                        if (subButtonCollection.Visible)
                            return;
                        sideScroll.SetVisible(false);
                        subButtonCollection.SetVisible(true);
                    });
                }

                // Connectors
                GenerateCustomSection("Connectors", () => [CreateConnectorNodeButton()], mainScroll, sideScroll);

                // Preformance
                GenerateCustomSection("Performance", () => [CreatePerformanceNodeButton()], mainScroll, sideScroll);

                // Groups
                selector._regenerateGroupList = GenerateCustomSection("Groups", GenerateGroupButtons, mainScroll, sideScroll);

                // Conditional
                GenerateCustomSection("Conditional", () => [CreateIfElseNodeButton()], mainScroll, sideScroll);

                // Structures
                GenerateCustomSection("Structures", () => CreateStructureNodeButtons(), mainScroll, sideScroll);

                return [mainScroll, bg, sideScroll];
            })
        ]));

        private Action GenerateCustomSection(string name, Func<UIElementBase[]> createElements, UIVScroll mainScroll, UICol sideScroll)
        {
            var side = new UIVScroll(spacing_[5], w_[190], h_[30], border_[5, 5, 5, 5], grow_children) { Name = name };
            var col = new UICol(Class(w_[190], h_[30], blank_sharp_g_[20]),
            OnClick<UICol>(_ =>
            {
                if (side.Visible)
                    return;
                sideScroll.SetVisible(false);
                side.SetVisible(true);
            }),
            Sub(
                new UIText(name, Class(middle_center, mc_[name.Length], fs_[1]))
            ));
            var elements = createElements();
            side.AddElements(elements);

            mainScroll.AddElement(col);
            sideScroll.AddElement(side);

            return () =>
            {
                side.DeleteChildren();
                var elements = createElements();
                side.AddElements(elements);
                selector.Controller.AddElements(elements);
            };
        }

        private UIElementBase CreateConnectorNodeButton() =>
        new UICol(Class(w_[190], h_[30], blank_sharp_g_[20]),
        OnClick<UICol>(_ =>
        {
            Vector3 position = new Vector3(selector.Position.X, selector.Position.Y, 0);
            position = Vector3.TransformPosition(position, NodeManager.NodeUIController.ModelMatrix.Inverted());
            var node = new ConnectorNode(NodeManager.NodeCollection, position.Xy).InitUI();
            NodeManager.NodeUIController.AddElement(node.Collection);
            NodeManager.AddNode(node);
            Element.SetVisible(false);
        }),
        Sub(
            new UIText("Basic", Class(middle_center, mc_[6], fs_[1]))
        ));

        private UIElementBase CreatePerformanceNodeButton() =>
        new UICol(Class(w_[190], h_[30], blank_sharp_g_[20]),
        OnClick<UICol>(_ =>
        {
            Vector3 position = new Vector3(selector.Position.X, selector.Position.Y, 0);
            position = Vector3.TransformPosition(position, NodeManager.NodeUIController.ModelMatrix.Inverted());
            var node = new CacheNode(NodeManager.NodeCollection, position.Xy).InitUI();
            NodeManager.NodeUIController.AddElement(node.Collection);
            NodeManager.AddNode(node);
            Element.SetVisible(false);
        }),
        Sub(
            new UIText("Cache", Class(middle_center, mc_[6], fs_[1]))
        ));
        
        private UIElementBase CreateIfElseNodeButton() =>
        new UICol(Class(w_[190], h_[30], blank_sharp_g_[20]),
        OnClick<UICol>(_ =>
        {
            Vector3 position = new Vector3(selector.Position.X, selector.Position.Y, 0);
            position = Vector3.TransformPosition(position, NodeManager.NodeUIController.ModelMatrix.Inverted());
            var node = new IfElseNode(NodeManager.NodeCollection, position.Xy, "<").InitUI();
            NodeManager.NodeUIController.AddElement(node.Collection);
            NodeManager.AddNode(node);
            Element.SetVisible(false);
        }),
        Sub(
            new UIText("IfElse", Class(middle_center, mc_[6], fs_[1]))
        ));

        private UIElementBase[] CreateStructureNodeButtons()
        {
            var col = new UICol(Class(w_[190], h_[30], blank_sharp_g_[20]),
            OnClick<UICol>(_ =>
            {
                Vector3 position = new Vector3(selector.Position.X, selector.Position.Y, 0);
                position = Vector3.TransformPosition(position, NodeManager.NodeUIController.ModelMatrix.Inverted());
                var node = new StructureNode(NodeManager.NodeCollection, position.Xy, "Base").InitUI();
                NodeManager.NodeUIController.AddElement(node.Collection);
                NodeManager.AddNode(node);
                Element.SetVisible(false);
            }),
            Sub(
                new UIText("Base", Class(middle_center, mc_[6], fs_[1]))
            ));
            return [col];
        }
        
        private UIElementBase[] GenerateGroupButtons()
        {
            string groupsFolder = Path.Combine(Game.MainPath, "custom", "groups");
            var files = Directory.GetFiles(groupsFolder, "*.json");
            UIElementBase[] buttons = new UIElementBase[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var name = Path.GetFileNameWithoutExtension(file);
                var button = new UICol(Class(w_[190], h_[30], blank_sharp_g_[20]),
                OnClick<UICol>(_ => {
                    if (name == NodeManager.LoadedFileName && NodeManager.NodeEditorType == NodeEditorType.Group)
                        return;

                    var settings = new GroupLoaderSettings()
                    {
                        LoadingType = GroupLoadingType.Node,
                    };

                    if (!GroupLoader.Load(file, settings, out var data))
                    {
                        Console.WriteLine($"[Warning] : Failed to load group at {file}");
                        return;
                    }

                    Vector3 position = new Vector3(selector.Position.X, selector.Position.Y, 0);
                    position = Vector3.TransformPosition(position, NodeManager.NodeUIController.ModelMatrix.Inverted());
                    var node = new GroupNode(NodeManager.NodeCollection, data.Value, position.Xy, name);
                    NodeManager.NodeUIController.AddElement(node.Collection);
                    NodeManager.AddNode(node);
                    Element.SetVisible(false);
                }),
                Sub(
                    new UIText(name, Class(middle_center, mc_[name.Length], fs_[1]))
                ));
                buttons[i] = button;
            }
            return buttons;
        }

        
        private void CreateUIElements(SelectorCollection collection, UIVScroll buttonCollection, UICol sideCollection)
        {
            foreach (var col in collection.SubCollections)
            {
                string name = col.Key;
                SelectorCollection subCollection = col.Value;
                UICol subUICol = new UICol(w_[190], h_[30], blank_sharp_g_[20]);
                UIText text = new UIText(name, middle_center, mc_[name.Length], fs_[1]);
                subUICol.AddElement(text);
                buttonCollection.AddElement(subUICol);
                if (subCollection.IsNodeCreationModule)
                {
                    subUICol.SetOnClick(collection =>
                    {
                        if (NodeManager.NodeEditorType == NodeEditorType.Group && (subCollection.Name == "Height" || subCollection.Name == "Structure"))
                            return;

                        Vector3 position = new Vector3(selector.Position.X, selector.Position.Y, 0);
                        position = Vector3.TransformPosition(position, NodeManager.NodeUIController.ModelMatrix.Inverted());
                        var node = new CustomNode(NodeManager.NodeCollection, subCollection.Name, position.Xy, subCollection.Type).InitUI();
                        NodeManager.NodeUIController.AddElement(node.Collection);
                        NodeManager.AddNode(node);
                        Element.SetVisible(false);
                    });
                }
                else
                {
                    var subButtonCollection = new UIVScroll(spacing_[5], w_[190], h_[30], border_[5, 5, 5, 5], grow_children);
                    CreateUIElements(subCollection, subButtonCollection, sideCollection);
                    sideCollection.AddElement(subButtonCollection);

                    subUICol.SetOnClick(collection =>
                    {
                        if (subButtonCollection.Visible)
                            return;

                        sideCollection.SetVisible(false);
                        buttonCollection.SetVisible(false);
                        subButtonCollection.SetVisible(true);
                    });
                }
            }
        }
    }
}