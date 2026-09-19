using System.Diagnostics.CodeAnalysis;

namespace PBG.Nodes;

public struct NodeConnection : IEquatable<NodeConnection>
{
    public NodeBase OutputNode;
    public NodeOutput Output;
    public NodeBase InputNode;
    public NodeInput Input;

    public readonly bool HasNode(NodeBase node)
    {
        return InputNode.ID == node.ID || OutputNode.ID == node.ID;
    }

    public readonly bool HasInput(NodeConnection connection)
    {
        return InputNode.ID == connection.InputNode.ID && Input.Index == connection.Input.Index;
    }

    public readonly bool HasInput(NodeBase inputNode, NodeInput input)
    {
        return InputNode.ID == inputNode.ID && Input.Index == input.Index;
    }

    public readonly bool HasOutput(NodeConnection connection)
    {
        return OutputNode.ID == connection.OutputNode.ID && Output.Index == connection.Output.Index;
    }

    public readonly bool HasOutput(NodeBase outputNode, NodeOutput output)
    {
        return OutputNode.ID == outputNode.ID && Output.Index == output.Index;
    }

    public readonly bool Equals(NodeConnection other)
    {
        return OutputNode == other.OutputNode &&
        Output.Index == other.Output.Index &&
        InputNode == other.InputNode &&
        Input.Index == other.Input.Index;
    }
}

public struct DragConnection
{
    public NodeBase Node;
    public int Index;
    public NodeConnectionType Type;
}