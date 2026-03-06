using PBG.UI;

namespace PBG.LoaderConfig;

public class StructureNodeConfig : NodeConfig
{
    public string Name { get; set; } = "";
    public string StructureName { get; set; } = "";
    public float[] Position { get; set; } = [0, 0];
    public string GUID { get; set; } = "";
    public int OutputPriority { get; set; } = 0;
    public Dictionary<string, string?> Inputs { get; set; } = [];
    public List<float[]> Values { get; set; } = [];
    public List<NodeConfig> Nodes = [];

    public override NodeBase? Load(NodeLoader loader)
    {
        StructureNode node;
        if (loader.Settings.LoadingType == NodeLoadingType.Core)
        {
            node = new StructureNode(loader.Nodes, this).InitBlankUI();
        }
        else
        {
            node = new StructureNode(loader.Nodes, this).InitUI();
        }

        loader.Nodes.Add(node);
        loader.HandleInputs(node, Inputs);
        loader.HandleValues(node, Values, []);
        return node;
    }
}
