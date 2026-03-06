using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using PBG;
using PBG.Core;
using PBG.Data;
using PBG.LoaderConfig;
using PBG.UI;
using Silk.NET.Input;

public class NodeManager : ScriptingNode
{
    public static string CurrentPath => Path.Combine(Game.MainPath, "custom", "nodes", FileName + ".json");
    public static string FileName = "Base";
    public static string LoadedFileName = "";
    public static UIField FileNameInputField = null!;
    public static NodeEditorType NodeEditorType = NodeEditorType.Node;
    public static NodeCollection NodeCollection = null!;

    public static UIController NodeUIController { get; private set; } = null!;
    public static UIController GroupDisplayController { get; private set; } = null!;

    public static HashSet<NodeCollection> OverlayNodesRenderList = [];

    public static HashSet<NodeBase> SelectedNodes = [];
    public static IfElseNode? IfElseNode = null;

    private static Queue<Action> LateUpdateQueue = [];

    public NodeCopyData CopyData = new();

    void Start()
    {
        NodeUIController = Transform.ParentNode!.GetNode("Nodes").GetComponent<UIController>();
        GroupDisplayController = Transform.ParentNode.GetNode("Groups").GetComponent<UIController>();
        NodeCollection = new();
    }

    void Render()
    {
        if (StructureNodeManager.Instance.Parent.Editor == EditorType.Noise)
        {
            NodeCollection.ConnectionRenderer.RenderLines(NodeUIController);
        }
    }

    public static void SetName(string name)
    {
        FileName = name;
        FileNameInputField?.SetText(name).UpdateCharacters();
    }

    public static void SendToFront(NodeBase node)
    {
        if (!node.NodeCollection.Nodes.Remove(node))
            return;

        node.NodeCollection.Nodes.Insert(0, node);
    }

    public static void PrintLines()
    {
        //Console.WriteLine("----- Node lines -----");
        foreach (var node in NodeCollection.Nodes)
        {
            //Console.WriteLine(node.GetLine());
        }
        //Console.WriteLine("-----------------------");
    }

    public static void AddNode(NodeBase node)
    {
        if (node.NodeCollection.Nodes.Contains(node))
            return;

        node.NodeCollection.Nodes.Add(node);
        node.NodeCollection.Inputs.AddRange(node.GetInputs());
        node.NodeCollection.Outputs.AddRange(node.GetOutputs());
    }

    public static void RemoveNode(NodeBase node)
    {
        node.NodeCollection.Remove(node);
        SelectedNodes.Remove(node);
    }

    public static void Select(NodeBase node)
    {
        if (Input.IsKeyDown(Key.ShiftLeft))
        {
            if (SelectedNodes.Remove(node))
            {
                node.Deselect();
            }
            else
            {
                SelectedNodes.Add(node);
                node.Select();
            }
        }
        else
        {
            if (SelectedNodes.Contains(node))
                return;

            SelectedNodes.Add(node);
            node.Select();
        }
    }

    public static void UnselectAllNodes()
    {
        foreach (var selectedNode in SelectedNodes)
        {
            selectedNode.Deselect();
        }
        SelectedNodes = [];
    }

    public static void DeleteSelectedNode()
    {
        foreach (var selectedNode in NodeBase.SelectedNodes)
        {
            selectedNode.DeleteNode();
            RemoveNode(selectedNode);
        }
    }

    public static void Clear()
    {
        NodeCollection.Clear();
        GenerateLines();
    }

    public static List<NodeData> GetNodeTree(List<NodeBase> nodes)
    {
        List<NodeData> nodeTree = [];
        Dictionary<IfElseNode, NodeData> ifElseNodeDatas = [];
        Dictionary<CacheNode, NodeData> cacheNodeDatas = [];

        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node is IfElseNode ifElseNode)
            {
                ifElseNodeDatas.TryAdd(ifElseNode, new(ifElseNode));
            }
            if (node is CacheNode cacheNode)
            {
                cacheNodeDatas.TryAdd(cacheNode, new(cacheNode));
            }
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            Console.WriteLine("Making node tree with " + node.GetName());
            Console.WriteLine("node has group parent? " + (node.ParentGroup != null) + " has cache node? " + (node.ParentCacheNode != null));
            if (node is IfElseNode ifElseNode)
            {
                if (node.ParentIfElseNode != null)
                {
                    ifElseNodeDatas[node.ParentIfElseNode].Children.Add(ifElseNodeDatas[ifElseNode]);   
                }
                else
                {
                    nodeTree.Add(ifElseNodeDatas[ifElseNode]);
                }
            }
            else if (node is CacheNode cacheNode)
            {
                if (node.ParentCacheNode == null)
                {
                    nodeTree.Add(cacheNodeDatas[cacheNode]);
                }
                else
                {
                    throw new Exception("[Error] : Cache node cannot have a parent cache node");
                }
            }
            else
            {
                var nodeData = new NodeData(node);
                if (node.ParentIfElseNode != null)
                {
                    ifElseNodeDatas[node.ParentIfElseNode].Children.Add(nodeData);   
                }
                else if (node.ParentCacheNode != null && cacheNodeDatas.TryGetValue(node.ParentCacheNode, out var data))
                {
                    data.Children.Add(nodeData);
                }
                else
                {
                    nodeTree.Add(nodeData);
                }
            }
        }

        return nodeTree;
    }

    public static void GenerateLines()
    {
        NodeCollection.ConnectionRenderer.GenerateLines(NodeCollection);

        Compile();
    }

    public static void UpdateLines()
    {
        if (NodeCollection.Outputs.Count == 0 || NodeCollection.Inputs.Count == 0)
            return;

        NodeCollection.ConnectionRenderer.UpdateLines(NodeCollection);
    }

    public static void RemoveUselessNodes(List<NodeBase> nodes)
    {
        List<NodeBase> copy = [.. nodes];
        for (int i = 0; i < copy.Count; i++)
        {
            var node = copy[i];
            if (node is ConnectorNode connectorNode)
            {
                if (connectorNode.InputField.Input != null && connectorNode.InputField.Input.Output != null && connectorNode.OutputField.Output.Inputs.Count > 0)
                {
                    var output = connectorNode.InputField.Input.Output;
                    List<NodeInput> inputs = connectorNode.OutputField.Output.Inputs;

                    connectorNode.InputField.Disconnect();
                    connectorNode.OutputField.Disconnect();

                    for (int j = 0; j < inputs.Count; j++)
                    {
                        var input = inputs[j];
                        output.Connect(input);
                    }
                }
                else if (!connectorNode.InputField.IsConnected() && connectorNode.OutputField.Output.Inputs.Count > 0)
                {
                    List<NodeInput> inputs = connectorNode.OutputField.Output.Inputs;

                    connectorNode.OutputField.Disconnect();
     
                    var values = connectorNode.InputField.Value.GetValues();

                    for (int j = 0; j < inputs.Count; j++)
                    {
                        var input = inputs[j];
                        for (int k = 0; k < values.Length; k++)
                        {
                            input.InputField.Value.SetValue(values[k], k);
                        }
                    }
                }

                nodes.Remove(connectorNode);
            }
        }
    }

    public static void LateUpdate()
    {
        while (LateUpdateQueue.Count > 0)
        {
            LateUpdateQueue.Dequeue()?.Invoke();
        }
    }

    public static void Compile()
    {
        GLSLManager.Compile();
    }

    public static int GetCurrentNodeCount()
    {
        var files = Directory.GetFiles(Path.Combine(Game.MainPath, "custom", "nodes"), "*.json");
        return files.Length;
    }

    public static int GetCurrentGroupCount()
    {
        var files = Directory.GetFiles(Path.Combine(Game.MainPath, "custom", "groups"), "*.json");
        return files.Length;
    }

    public static string[] GetNodeFiles() => Directory.GetFiles(Path.Combine(Game.MainPath, "custom", "nodes"), "*.json");

    public static void Save()
    {
#if MYDEBUG
        Console.WriteLine("Saving nodes: " + FileName);
#endif
        string filePath = Path.Combine(Game.MainPath, "custom", "nodes", FileName + ".json");
        if (File.Exists(filePath))
        {
            // handle potential overwrite confirmation
        }

        var loader = new NodeLoader(NodeCollection);
        loader.Save(filePath);
    }

    public static void SaveCopyNodes()
    {
        string filePath = Path.Combine(Game.CustomTempPath, "copy.json");
        SaveSelectedNodes(filePath);
    }
    
    public static void SaveSelectedNodes(string path)
    {
#if MYDEBUG
        Console.WriteLine("Saving nodes: " + FileName);
#endif

        var saver = new NodeSaver();
        saver.Save(path, [.. SelectedNodes]);
    }

    public static void SaveGroup()
    {
#if MYDEBUG
        Console.WriteLine("Saving nodes: " + FileName);
#endif
        string filePath = Path.Combine(Game.MainPath, "custom", "groups", FileName + ".json");
        if (File.Exists(filePath))
        {
            // handle potential overwrite confirmation
        }

        GroupsConfig groupConfig = new();

        var config = GetSavedNodes();
        groupConfig.Nodes = config.Nodes;

        for (int i = 0; i < NodeCollection.Nodes.Count; i++)
        {
            var node = NodeCollection.Nodes[i];
            if (node is GroupInputNode groupInput)
            {
                groupConfig.InputPosition = [(int)groupInput.Position.X, (int)groupInput.Position.Y];
                groupConfig.GUID = groupInput.ID.ToString();
                foreach (var (name, input) in groupInput.InputFields)
                {
                    groupConfig.Inputs.Add(name, input.Value.GetGLSLType());
                    groupConfig.Values.Add(input.Value.GetValues());

                }
            }
            if (node is GroupOutputNode groupOutput)
            {
                groupConfig.OutputPosition = [(int)groupOutput.Position.X, (int)groupOutput.Position.Y];
                foreach (var (name, output) in groupOutput.InputFields)
                {
                    groupConfig.Outputs.Add(name, output.Input?.Output?.OutputField.GetUniqueName() ?? "null");
                }
            }
        }

        JsonSerializerSettings settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        string json = JsonConvert.SerializeObject(groupConfig, Formatting.Indented, settings);
        File.WriteAllText(filePath, json);
    }

    public static NodesConfig GetSavedNodes() => GetSavedNodes(NodeCollection);
    public static NodesConfig GetSavedNodes(NodeCollection collection) => GetSavedNodes(collection.Nodes);
    public static NodesConfig GetSavedNodes(List<NodeBase> nodes)
    {
        NodesConfig config = new();
        Dictionary<string, int> groupCounts = [];

        // Setting the name of every node
        for (int i = 0; i < NodeCollection.Nodes.Count; i++)
        {
            var node = NodeCollection.Nodes[i];
            int index = i;
            if (node is GroupNode groupNode)
            {
                groupCounts.TryAdd(groupNode.GroupName, 0);
                index = groupCounts[groupNode.GroupName];
                groupCounts[groupNode.GroupName] = index + 1;
            }

            node.SetInputSaveNames(index);
            node.SetOutputSaveNames(index);
        }

        // Only getting the tree of the selected nodes
        var nodesData = GetNodeTree(nodes);
        for (int i = 0; i < nodesData.Count; i++)
        {
            var save = nodesData[i].Save();
            if (save != null)
                config.Nodes.Add(save);
        }
        return config;
    }

    public static bool Load() => Load(Path.Combine(Game.MainPath, "custom", "nodes", FileName + ".json"));
    public static bool Load(string filePath)
    {
        if (!Load(filePath, out var collection))
            return false;

        Clear();
        LoadedFileName = FileName;
        NodeCollection = collection;

        LateUpdateQueue.Enqueue(GenerateLines);

        return true;
    }

    public static bool Load(string filePath, [NotNullWhen(true)] out NodeCollection? collection)
    {
        CacheNode.IDCounter = 0;
        
        collection = null;
        var loader = new NodeLoader();
        if (!File.Exists(filePath) || !loader.Load(filePath, out collection))
            return false;
        
        for (int i = 0; i < collection.Nodes.Count; i++)
        {
            NodeUIController.AddElement(collection.Nodes[i].Collection);
        }
        return true;
    }

    public static bool LoadGroup() => LoadGroup(Path.Combine(Game.MainPath, "custom", "groups", FileName + ".json"));
    public static bool LoadGroup(string filePath)
    {
        if (!File.Exists(filePath) || !GroupLoader.Load(filePath, out var data))
            return false;

        Clear();
        LoadedFileName = FileName;
        NodeCollection = data.Value.Nodes;

        LateUpdateQueue.Enqueue(GenerateLines);

        return true;
    }

    public static bool LoadCopyNodes()
    {
        string filePath = Path.Combine(Game.CustomTempPath, "copy.json");
        if (!Load(filePath, out var collection))
            return false;

        UnselectAllNodes();
        for (int i = 0; i < collection.Nodes.Count; i++)
        {
            var node = collection.Nodes[i];
            node.Collection.Origin = node.Position; // Setting the position manually, because to have a correct origin it needs to go through the Update loop before calling select
            AddNode(node);
            SelectedNodes.Add(node);
            node.Select();
        }

        LateUpdateQueue.Enqueue(GenerateLines);
        return true;
    }


    public static bool DeleteFile(string name)
    {
        if (File.Exists(name))
        {
            File.Delete(name);
            //Console.WriteLine($"Node file deleted: {name}");
            if (LoadedFileName == Path.GetFileNameWithoutExtension(name))
            {
                SetName("Base");
            }
            return true;
        }
        else
        {
            //Console.WriteLine($"Node file not found: {name}");
            return false;
        }
    }
}

public enum NodeEditorType
{
    Node,
    Group
}