namespace PBG.Nodes;

public class NodeRegistry
{
    public Dictionary<string, NodeTemplate> Nodes = [];

    public void Register(NodeTemplate template)
    {
        Nodes.Add(template.Name, template);
    }
}