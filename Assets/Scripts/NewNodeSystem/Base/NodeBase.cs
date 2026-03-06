using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.Data;
using PBG.LoaderConfig;
using PBG.UI;
using Silk.NET.Input;

public abstract class NodeBase
{
    public Guid ID = Guid.NewGuid();

    public NodeCollection NodeCollection;

    public static Vector4 SELECTION_COLOR = new Vector4(0.95f, 0.95f, 0.35f, 1f); // yellow highlight (selection glow)
    public static HashSet<NodeBase> SelectedNodes = [];
    public static NodeConnector? SelectedConnection = null;
    
    public string Name = "Node";
    public Vector3 Color;
    public Vector2 Position = Vector2.Zero;
    public Vector2 GridPosition = Vector2.Zero;
    public Vector2 _position = Vector2.Zero;

    public int OutputPriority = 0;

    public UIElementBase Collection = null!;

    public IfElseNode? ParentIfElseNode = null;
    public CacheNode? ParentCacheNode  
    {
        get => ParentGroup != null ? ParentGroup.ParentCacheNode : _parentCacheNode;
        set
        {
            if (ParentGroup != null)
            {
                ParentGroup.ParentCacheNode = value;
                _parentCacheNode = value;
            }
            else
            {
                _parentCacheNode = value;
            }
        }
    }
    private CacheNode? _parentCacheNode = null;
   
    public GroupNode? ParentGroup = null;

    public List<NodeValue_Block> BlockIconCollections = [];


    public Dictionary<string, NodeInputField> InputFields = [];
    public Dictionary<string, NodeOutputField> OutputFields = [];

    public NodeBase(string? guid, NodeCollection nodeCollection)
    {
        NodeCollection = nodeCollection;
        if (guid == null || string.IsNullOrEmpty(guid))
            ID = Guid.NewGuid();
        else
            ID = new Guid(guid);
    }

    public void UpdateScalingAndParent()
    {
        Collection.ApplyChanges(UIChange.Scale);
        if (ParentIfElseNode != null)
        {
            StructureNodeManager.UpdateScalingAndParent(ParentIfElseNode);
        }
    }

    public bool HasParent(NodeBase node)
    {
        var parent = ParentIfElseNode;
        while (parent != null)
        {
            if (parent == node)
                return true;
            parent = parent.ParentIfElseNode;
        }
        return false;
    }

    public bool IsInSameIfElse(NodeBase node)
    {
        if (ParentIfElseNode == node.ParentIfElseNode)
            return true;

        var ifElse = node.ParentIfElseNode;
        while (ifElse != null)
        {
            if (ParentIfElseNode == ifElse.ParentIfElseNode)
                return true;
            ifElse = ifElse.ParentIfElseNode;
        }
        return ParentIfElseNode == ifElse;
    }

    public int GetInbeddingLevel()
    {
        int level = 0;
        var ifElse = ParentIfElseNode;
        while (ifElse != null)
        {
            level++;
            ifElse = ifElse.ParentIfElseNode;
        }
        return level;
    }

    public void ConnectToCache(CacheNode cacheNode)
    {
        ParentCacheNode = cacheNode;

        var inputs = GetInputNodes();
        foreach (var (_, node) in inputs)
        {
            node.ConnectToCache(cacheNode);
        }
    }

    public bool ConnectedToCache()
    {
        if (ParentCacheNode != null)
            return true;

        var inputs = GetInputNodes();
        foreach (var (_, node) in inputs)
        {
            if (node.ConnectedToCache())
                return true;
        }

        return false;
    }

    public void DisconnectFromCache()
    {
        ParentCacheNode = null;
        Console.WriteLine("Disconnecting " + GetName());
        
        var inputs = GetInputNodes();
        foreach (var (_, node) in inputs)
        {
            node.DisconnectFromCache();
        }
    }

    public virtual string GetName() => "";

    public virtual string GetLine(LineContext context) { return ""; }
    public abstract NoiseNode? BuildExpression(Dictionary<NodeBase, Dictionary<string, SetterValue>> connections, List<Expression> expressions);

    public void InitNodeTypes() => InitNodeTypes([]);
    public void InitNodeTypes(HashSet<NodeBase> nodes)
    {
        if (!nodes.Add(this))
            return;

        var inputFields = InputFields.Values.ToList();
        while (inputFields.Count > 0)
        {
            var field = inputFields[0];

            if (field.HasConnectionPoint)
            {
                if (field.Input != null && field.Input.IsConnected && !field.CanOverload && field.Input.Output != null)
                {
                    field.ConversionValue = field.Input.Output.OutputField.Value;
                }
                else
                {
                    field.ConversionValue = null;
                }
                inputFields.RemoveAt(0);
                continue;
            }

            inputFields.RemoveAt(0);
        }

        var outputFields = OutputFields.Values.ToList();
        while (outputFields.Count > 0)
        {
            var field = outputFields[0];
            if (field.CurrentType != field.DefaultType)
            {
                foreach (var input in field.Output.Inputs)
                {
                    input.Node.InitNodeTypes(nodes);
                }
            }

            outputFields.RemoveAt(0);
        }
    }

    public virtual void Select()
    {
        _position = Collection.Origin;
        Collection.Color = SELECTION_COLOR;
        Collection.ApplyChanges(UIChange.Color);
    }

    public virtual void Deselect()
    {
        Collection.Color = Vector4.Zero;
        Collection.ApplyChanges(UIChange.Color);
    }

    public virtual void DeleteCore() { }
    public virtual void DeleteNode()
    {
        if (ParentIfElseNode != null)
        {
            Collection.RemoveFromParent();
            ParentIfElseNode.Collection.ApplyChanges(UIChange.Scale);
        }
    }

    public List<NodeInputField> GetNonConnectedInputs()
    {
        List<NodeInputField> inputNodes = [];
        foreach (var input in InputFields.Values)
        {
            if (input.Input == null || (input.Input != null && !input.Input.IsConnected))
                inputNodes.Add(input);
        }
        return inputNodes;
    }

    public List<NodeInput> GetInputs()
    {
        List<NodeInput> inputs = [];
        foreach (var input in InputFields.Values)
        {
            if (input.Input == null)
                continue;
            inputs.Add(input.Input);
        }
        return inputs;
    }

    public List<NodeInputField> GetInputFields()
    {
        List<NodeInputField> inputs = [];
        foreach (var input in InputFields.Values)
        {
            inputs.Add(input);
        }
        return inputs;
    }

    public List<NodeOutput> GetOutputs()
    {
        List<NodeOutput> outputs = [];
        foreach (var (_, output) in OutputFields)
        {
            outputs.Add(output.Output);
        }
        return outputs;
    }

    public List<(NodeInput, NodeBase)> GetInputNodes()
    {
        List<(NodeInput, NodeBase)> inputNodes = [];
        foreach (var input in InputFields.Values)
        {
            if (input.Input?.Output == null)
                continue;

            inputNodes.Add((input.Input, input.Input.Output.Node));
        }
        return inputNodes;
    }

    public void SetOutputSaveNames(int index)
    { 
        foreach (var (key, output) in OutputFields)
        {
            output.Output.Name = $"output_{key}_{index}";
        }
    }

    public void SetInputSaveNames(int index)
    { 
        foreach (var (key, input) in InputFields)
        {
            if (input.Input == null)
                continue;

            input.Input.Name = $"input_{key}_{index}";
        }
    }

    public virtual bool WritesLines() => false;
    public virtual void ResetValueReferences() {}
    public virtual void SetValueReferences(List<float> values, ref int index) {}

    public virtual NodeConfig Save() => new EmptyNodeConfig();

    public void MoveNode()
    {
        if (Input.IsKeyDown(Key.ControlLeft)) return;
        Vector2 mouseDelta = Input.GetMouseDelta();
        if (mouseDelta == Vector2.Zero) return;
        _position += mouseDelta * (1 / Collection.UIController?.Scale ?? 1f);
        GridPosition = Mathf.Round(_position / 10f) * 10f;
        Collection.BaseOffset = GridPosition - (Collection.ParentElement?.Origin ?? Vector2.Zero);
        Position = Collection.BaseOffset;

        var center = (Collection.Point1 + Collection.Point2) * 0.5f;
        
        // Check if trying to put nodes inside of a parent node
        var highestIfElseNode = StructureNodeManager.GetHighestParentIfElseNode(IfElseNode.AllIfElseNodes, center, this);        
        if (highestIfElseNode != null)
        {
            if (this != highestIfElseNode && ParentIfElseNode != highestIfElseNode && !highestIfElseNode.SubCollection.Has(Collection) && !highestIfElseNode.HasParent(this))
            {
                if (!Collection.RemoveFromParent())
                    NodeManager.NodeUIController.AbsoluteElements.Remove(Collection);

                highestIfElseNode.SubCollection.AddElement(Collection);
                ParentIfElseNode = highestIfElseNode;
            }
        }
        else if (ParentIfElseNode != null)
        {
            Collection.RemoveFromParent();
            NodeManager.NodeUIController.AbsoluteElements.Add(Collection);
            ParentIfElseNode.UpdateScalingAndParent();
            ParentIfElseNode = null;
        }

        Collection.ApplyChanges(UIChange.Transform);
        UpdateScalingAndParent();
    }

    public static void Connect(NodeInput input)
    {
        if (input.IsConnected)
        {
            input.Disconnect();
            input.Deselect();
            input.Node.InitNodeTypes();
            NodeManager.GenerateLines();
            return;
        }

        if (SelectedConnection == null)
        {
            SelectedConnection = input;
            input.Select();
            return;
        }

        if (SelectedConnection is NodeInput)
        {
            SelectedConnection = null;
            input.Deselect();
            return;
        }

        if (SelectedConnection is NodeOutput output)
        {
            if (output.Node.ParentCacheNode != null || output.Node == input.Node || output.IsConnectedTo(input) || !output.Node.IsInSameIfElse(input.Node))
            {
                output.Deselect();
                input.Deselect();
                SelectedConnection = null;
                return;
            }

            if (input.Node is CacheNode cacheNode)
            {
                // if the output is connected to another node it can't be connected to a cache
                // if the output node or its children are connected to another cache i can't connect to another
                if (output.IsConnected || output.Node.ConnectedToCache())
                {
                    SelectedConnection.Deselect();
                    output.Deselect();
                    SelectedConnection = null;
                    return;
                }

                output.Node.ConnectToCache(cacheNode);
            }
            else if (input.Node.ParentCacheNode != null)
            {
                // if the output is connected to another node it can't be connected to a cache
                // if the output node or its children are connected to another cache i can't connect to another
                if (output.IsConnected || output.Node.ConnectedToCache())
                {
                    SelectedConnection.Deselect();
                    output.Deselect();
                    SelectedConnection = null;
                    return;
                }

                output.Node.ConnectToCache(input.Node.ParentCacheNode);
            }

            output.Connect(input);
            output.Deselect();
            input.Deselect();
            SelectedConnection = null;
            input.Node.InitNodeTypes();
            NodeManager.GenerateLines();
        }
        else
        {
            throw new InvalidOperationException("Selected connection is not a valid output.");
        }
    }

    public static void Connect(NodeOutput output)
    {
        if (SelectedConnection == null)
        {
            SelectedConnection = output;
            output.Select();
            return;
        }

        if (SelectedConnection is NodeOutput)
        {
            SelectedConnection = null;
            output.Deselect();
            return;
        }

        if (SelectedConnection is NodeInput input)
        {
            if (output.Node.ParentCacheNode != null || input.Node == output.Node || output.IsConnectedTo(input) || !output.Node.IsInSameIfElse(input.Node))
            {
                SelectedConnection.Deselect();
                output.Deselect();
                SelectedConnection = null;
                return;
            }

            if (input.Node is CacheNode cacheNode)
            {
                // if the output is connected to another node it can't be connected to a cache
                // if the output node or its children are connected to another cache i can't connect to another
                if (output.IsConnected || output.Node.ConnectedToCache())
                {
                    SelectedConnection.Deselect();
                    output.Deselect();
                    SelectedConnection = null;
                    return;
                }

                output.Node.ConnectToCache(cacheNode);
            }
            else if (input.Node.ParentCacheNode != null)
            {
                // if the output is connected to another node it can't be connected to a cache
                // if the output node or its children are connected to another cache i can't connect to another
                if (output.IsConnected || output.Node.ConnectedToCache())
                {
                    SelectedConnection.Deselect();
                    output.Deselect();
                    SelectedConnection = null;
                    return;
                }

                output.Node.ConnectToCache(input.Node.ParentCacheNode);
            }

            output.Connect(input);
            output.Deselect();
            input.Deselect();
            SelectedConnection = null;
            input.Node.InitNodeTypes();
            NodeManager.GenerateLines();
        }
        else
        {
            throw new InvalidOperationException("Selected connection is not a valid input.");
        }
    }

    public override string ToString() => GetName();
}

public enum ValueType
{
    Float,
    Int,
    Vector2,
    Vector2i,
    Vector3,
    Vector3i,
    Block
}