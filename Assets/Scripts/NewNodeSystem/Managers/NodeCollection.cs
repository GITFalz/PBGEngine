using PBG.Core;
using PBG.MathLibrary;
using PBG.UI;

public class NodeCollection : ScriptingNode
{
    public ConnectionRenderer ConnectionRenderer = new();

    public List<NodeBase> Nodes = [];
    public List<NodeInput> Inputs = [];
    public List<NodeOutput> Outputs = [];

    public void Add(NodeBase node)
    {
        if (Nodes.Contains(node))
            return;

        Nodes.Add(node);
        Inputs.AddRange(node.GetInputs());
        Outputs.AddRange(node.GetOutputs());
    }

    public void Remove(NodeBase node)
    {
        Nodes.Remove(node);
        Inputs.RemoveAll(input => input.Node == node);
        Outputs.RemoveAll(output => output.Node == node);
    }

    public List<NodeBase> GetConnectedNodeList() => GetConnectedNodeList(out _);
    public List<NodeBase> GetConnectedNodeList(out int variableCount)
    {
        List<NodeBase> ConnectedNodeList = [];
        HashSet<NodeBase> visited = [];

        List<NodeBase> outputNodes = [];
        List<NodeBase> IfElseNodes = [];

        for (int i = 0; i < Nodes.Count; i++)
        {
            NodeBase node = Nodes[i];
            Console.WriteLine("Node: " + node);
            if (node is CustomNode customNode && customNode.IsOutput)
                outputNodes.Add(customNode);
            if (node is StructureNode)
                outputNodes.Add(node);
            if (node is IfElseNode)
                IfElseNodes.Add(node);
            if (node is GroupOutputNode)
                outputNodes.Add(node);
        }

        // sort outputNodes by OutputPriority, higher = first in the list
        outputNodes.Sort((a, b) => a.OutputPriority.CompareTo(b.OutputPriority));

        for (int i = 0; i < IfElseNodes.Count; i++)
        {
            GetConnectedNodes(IfElseNodes[i], ConnectedNodeList, visited);
        }

        for (int i = 0; i < outputNodes.Count; i++)
        {
            GetConnectedNodes(outputNodes[i], ConnectedNodeList, visited);
        }
        
        variableCount = InitOutputs(ConnectedNodeList);
        return ConnectedNodeList;
    }

    public static int ID = 0;

    public static int InitOutputs(List<NodeBase> nodes)
    {
        int outputCount = 0;
        for (int i = 0; i < nodes.Count; i++)
        {
            NodeBase node = nodes[i];
            outputCount += node.GetNonConnectedInputs().Count;
            var outputs = node.GetOutputs();
            if (outputs.Count == 0)
                continue;

            for (int j = 0; j < outputs.Count; j++)
            {
                NodeOutput output = outputs[j];
                output.VariableName = $"variable{i}_{j}_{ID}";
                output.Index = outputCount;
                outputCount++;
            }
        }
        ID++;
        return outputCount;
    }

    public static void GetConnectedNodes(NodeBase node, List<NodeBase> nodes, HashSet<NodeBase> visited)
    {
        if (visited.Contains(node))
            return;
            
        visited.Add(node);
        var inputs = node.GetInputNodes();
        if (inputs.Count == 0)
        {
            nodes.Insert(0, node);
            return;
        }
        foreach (var (input, cNode) in inputs)
        {
            if (!input.IsConnected || nodes.Contains(cNode))
                continue;

            GetConnectedNodes(cNode, nodes, visited);
        }
        int maxIndex = -1;
        foreach (var (input, cNode) in inputs)
        {
            if (!input.IsConnected)
                continue;
            maxIndex = Mathf.Max(maxIndex, nodes.IndexOf(cNode));
        }
        nodes.Insert(maxIndex + 1, node);
    }

    public List<string> GetLines(List<string> lines, LineContext context) => GetLines(lines, [], context);
    public List<string> GetLines(List<string> lines, List<float> values, LineContext context)
    {
        var nodes = GetConnectedNodeList();
        GetLines(nodes, lines, values, context);
        return lines;
    }

    public static void GetLines(List<NodeBase> nodes, List<string> lines, LineContext context) => GetLines(nodes, lines, [], context);
    public static void GetLines(List<NodeBase> nodes, List<string> lines, List<float> values, LineContext context)
    {
        int index = 0;
        for (int i = 0; i < nodes.Count; i++)
        {
            NodeBase node = nodes[i];

            if (!context.GetCurrentValue)
            {
                node.ResetValueReferences();
                node.SetValueReferences(values, ref index);
            }
            lines.Add("    " + node.GetLine(context));
        }
    }

    public void Clear()
    {
        List<NodeBase> copy = [.. Nodes];
        foreach (var node in copy)
        {
            node.DeleteNode();
        }
        Nodes = [];
        Inputs = [];
        Outputs = [];
    }
}