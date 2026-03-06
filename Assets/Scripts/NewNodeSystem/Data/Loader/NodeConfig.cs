using System.Text;
using PBG.MathLibrary;
using PBG.UI;

namespace PBG.LoaderConfig;

public class NodesConfig
{
    public List<NodeConfig> Nodes { get; set; } = [];
}

public class GroupsConfig
{
    public List<int> InputPosition { get; set; } = [];
    public List<int> OutputPosition { get; set; } = [];
    public string GUID { get; set; } = "";
    public Dictionary<string, string> Inputs { get; set; } = [];
    public Dictionary<string, string> Outputs { get; set; } = [];
    public List<float[]> Values { get; set; } = [];
    public List<NodeConfig> Nodes { get; set; } = [];

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine("GroupsConfig {");

        // InputPosition
        sb.Append("  InputPosition: [");
        sb.Append(string.Join(", ", InputPosition));
        sb.AppendLine("]");

        // OutputPosition
        sb.Append("  OutputPosition: [");
        sb.Append(string.Join(", ", OutputPosition));
        sb.AppendLine("]");

        // Inputs dictionary
        sb.AppendLine("  Inputs:");
        if (Inputs.Count == 0)
            sb.AppendLine("    (none)");
        else
            foreach (var kv in Inputs)
                sb.AppendLine($"    {kv.Key}: {kv.Value}");

        // Outputs dictionary
        sb.AppendLine("  Outputs:");
        if (Outputs.Count == 0)
            sb.AppendLine("    (none)");
        else
            foreach (var kv in Outputs)
                sb.AppendLine($"    {kv.Key}: {kv.Value}");

        // Values: List<float[]>
        sb.AppendLine("  Values:");
        if (Values.Count == 0)
        {
            sb.AppendLine("    (none)");
        }
        else
        {
            for (int i = 0; i < Values.Count; i++)
            {
                var arr = Values[i];
                string arrStr = arr != null ? string.Join(", ", arr) : "null";
                sb.AppendLine($"    [{i}]: [{arrStr}]");
            }
        }

        // Nodes: List<NodeConfig>
        sb.AppendLine("  Nodes:");
        if (Nodes.Count == 0)
        {
            sb.AppendLine("    (none)");
        }
        else
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                var node = Nodes[i];
                sb.AppendLine($"    Node {i}: {node?.ToString() ?? "null"}");
            }
        }

        sb.Append("}");

        return sb.ToString();
    }
}

public abstract class NodeConfig
{
    public abstract NodeBase? Load(NodeLoader loader);
}

public class EmptyNodeConfig : NodeConfig
{
    public override NodeBase? Load(NodeLoader loader) => null;
}