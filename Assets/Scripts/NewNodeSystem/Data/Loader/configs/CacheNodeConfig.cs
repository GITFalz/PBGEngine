using PBG.UI;

namespace PBG.LoaderConfig;

public class CacheNodeConfig : NodeConfig
{
    public float[] Position { get; set; } = [0, 0];
    public string GUID { get; set; } = "";
    public int CacheID { get; set; } = 0;
    public Dictionary<string, string?> Inputs { get; set; } = [];
    public List<string> Outputs { get; set; } = [];
    public List<float[]> Values { get; set; } = [];
    public List<NodeConfig> Nodes = [];

    public override NodeBase? Load(NodeLoader loader)
    {
        CacheNode node;
        if (loader.Settings.LoadingType == NodeLoadingType.Core)
        {
            node = new CacheNode(GUID, loader.Nodes, (Position[0], Position[1])).InitBlankUI();
        }
        else
        {
            node = new CacheNode(GUID, loader.Nodes, (Position[0], Position[1])).InitUI();
        }

        if (CacheID != CacheNode.IDCounter)
            node.CacheID = CacheNode.IDCounter;
        else
            node.CacheID = CacheID;
            
        CacheNode.IDCounter++;
        

        loader.Nodes.Add(node);

        for (int i = 0; i < Nodes.Count; i++)
        {
            var childNodeConfig = Nodes[i];
            var childNode = childNodeConfig.Load(loader);
            if (childNode != null)
            {
                childNode.ParentCacheNode = node;
            }
        }

        loader.HandleOutputs(node, Outputs);
        loader.HandleInputs(node, Inputs);
        loader.HandleValues(node, Values, []);
        return node;
    }
}
