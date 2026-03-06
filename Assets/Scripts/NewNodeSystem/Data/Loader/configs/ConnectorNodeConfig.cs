using PBG.UI;

namespace PBG.LoaderConfig;

public class ConnectorNodeConfig : NodeConfig
{
    public float[] Position { get; set; } = [0, 0];
    public string GUID { get; set; } = "";
    public Dictionary<string, string?> Inputs { get; set; } = [];
    public List<string> Outputs { get; set; } = [];
    public List<float[]> Values { get; set; } = [];

    public override NodeBase? Load(NodeLoader loader)
    {
        ConnectorNode node;
        if (loader.Settings.LoadingType == NodeLoadingType.Core)
        {
            node = new ConnectorNode(GUID, loader.Nodes, (Position[0], Position[1])).InitBlankUI();
        }
        else
        {
            node = new ConnectorNode(GUID, loader.Nodes, (Position[0], Position[1])).InitUI();
        }

        loader.Nodes.Add(node);
        loader.HandleOutputs(node, Outputs);
        loader.HandleInputs(node, Inputs);
        loader.HandleValues(node, Values, []);
        return node;
    }
}
