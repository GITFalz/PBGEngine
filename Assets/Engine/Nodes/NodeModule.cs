using PBG.Core;
using PBG.MathLibrary;
using PBG.UI;

namespace PBG.Nodes;

public class NodeModule : ScriptingNode
{
    private const int NODE_HEIGHT = 8;
    private UIController _ui = null!;
    
    public float Scale => _ui.Scale;
    public Vector3 Position => _ui.Position;
    public UIAlignment Alignment => _ui.Alignment;

    private HashSet<NodeBase> _nodeSet = [];
    public List<NodeBase> Nodes = [];
    public Dictionary<uint, NodeBase> NodeLookup = [];

    public List<NodeConnection> Connections = [];
    public DragConnection? DragConnection = null;

    private bool _started = false;
    public bool _modifiedConnections = false;
    public bool _updatedConnections = false;
    

    void Start()
    {
        _ui = Transform.GetComponent<UIController>();
        _started = true;

        for (int i = 0; i < Nodes.Count; i++)
        {
            var ui = Nodes[i].GetUI();
            ui.Depth = i * NODE_HEIGHT;
            _ui.AddElement(ui);
        }
    }   

    public void AddNode(NodeBase node)
    {
        if (!_nodeSet.Add(node))
            return;

        Nodes.Add(node);
        NodeLookup.Add(node.ID, node);
        node.SetParentModule(this);

        if (_started)
        {
            var ui = node.GetUI();
            ui.Depth = Nodes.Count * NODE_HEIGHT;
            _ui.AddElement(ui);
        }
    }

    public void AddNode(NodeBase node, Vector2 position)
    {
        if (!_nodeSet.Add(node))
            return;

        Nodes.Add(node);
        NodeLookup.Add(node.ID, node);
        node.SetParentModule(this);

        if (_started)
        {
            var ui = node.GetUI();
            ui.Depth = Nodes.Count * NODE_HEIGHT;
            _ui.AddElement(ui);
        }
    }

    public void RemoveNode(NodeBase node)
    {
        _nodeSet.Remove(node);
        Nodes.Remove(node);
        NodeLookup.Remove(node.ID);
        RemoveConnection(node);
    }

    public void MoveToFront(NodeBase node)
    {
        if (!_nodeSet.Contains(node))
            return;

        int index = Nodes.IndexOf(node);
        if (index < 0 || index == Nodes.Count - 1)
            return;

        Nodes.RemoveAt(index);
        Nodes.Add(node);

        for (int i = index; i < Nodes.Count - 1; i++)
            Nodes[i].SetDepth(i * NODE_HEIGHT);

        node.SetDepth((Nodes.Count - 1) * NODE_HEIGHT);
    }

    private bool IsConnectionValid(NodeConnection a, NodeConnection b)
    {
        return a.HasInput(b) || a.Equals(b);
    }

    public bool HasCircularConnection(NodeBase outputNode, NodeBase inputNode)
    {
        Dictionary<NodeBase, List<NodeBase>> connectionLookup = [];

        for (int i = 0; i < Connections.Count; i++)
        {
            var connection = Connections[i];
            connectionLookup.TryAdd(connection.InputNode, []);
            connectionLookup[connection.InputNode].Add(connection.OutputNode);
        }

        // A circular connection can only exist if the output node already has
        // a path through existing connections.
        if (!connectionLookup.ContainsKey(outputNode))
            return false;

        bool HasConnectedNode(NodeBase current)
        {
            // We found the node we are trying to connect to,
            // meaning adding this connection would create a loop.
            if (current == inputNode)
                return true;

            if (connectionLookup.TryGetValue(current, out var connectedNodes))
            {
                for (int i = 0; i < connectedNodes.Count; i++)
                {
                    if (HasConnectedNode(connectedNodes[i]))
                        return true;
                }
            }

            return false;
        }

        return HasConnectedNode(outputNode);
    }

    public bool IsConnected(NodeBase inputNode, NodeInput input)
    {
        return Connections.Any(c => c.HasInput(inputNode, input));
    }

    public bool IsConnected(NodeBase outputNode, NodeOutput output)
    {
        return Connections.Any(c => c.HasOutput(outputNode, output));
    }

    public bool TryAddConnection(NodeBase outputNode, NodeOutput output, NodeBase inputNode, NodeInput input)
    {
        if (HasCircularConnection(outputNode, inputNode))
            return false;

        if (outputNode.ParentBlockNode != null && (outputNode.GetBlockDepth() > inputNode.GetBlockDepth() || !inputNode.HasParentBlock(outputNode.ParentBlockNode, outputNode.ParentBlockIndex)))
            return false;

        var connection = new NodeConnection()
        {
            OutputNode = outputNode,
            Output = output,
            InputNode = inputNode,
            Input = input
        };

        // can't connect a node to itself
        if (outputNode.ID == inputNode.ID)
        {
            return false;
        }

        //Console.WriteLine("trying connection");

        // if input is already connection or any duplicates exist
        if (Connections.Any(c => IsConnectionValid(c, connection)))
        {
            //Console.WriteLine("connection failed");
            return false;
        }

        //Console.WriteLine("connection succeeded");
        Connections.Add(connection);
        _modifiedConnections = true;
        return true;
    }

    public void RemoveConnection(NodeBase node)
    {
        int count = Connections.RemoveAll(c => c.HasNode(node));
        if (count > 0)
            _modifiedConnections = true;
    }

    public void RemoveConnection(NodeBase outputNode, NodeOutput output)
    {
        int count = Connections.RemoveAll(c => c.HasOutput(outputNode, output));
        if (count > 0)
            _modifiedConnections = true;
    }

    public void RemoveConnection(NodeBase inputNode, NodeInput input)
    {
        int count = Connections.RemoveAll(c => c.HasInput(inputNode, input));
        if (count > 0)
            _modifiedConnections = true;
    }

    public void ModifyConnections()
    {
        _modifiedConnections = true;
    }

    public void MoveConnections()
    {
        _updatedConnections = true;
    }

    public bool DoRemakeConnections()
    {
        bool state = _modifiedConnections;
        _modifiedConnections = false;
        return state;
    }

    public bool DoUpdateConnections()
    {
        bool state = _updatedConnections;
        _updatedConnections = false;
        return state;
    }
}