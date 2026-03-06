using PBG.UI;

namespace PBG.LoaderConfig;

public class IfElseNodeConfig : NodeConfig
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public float[] Position { get; set; } = [0, 0];
    public string GUID { get; set; } = "";
    public Dictionary<string, string?> Inputs { get; set; } = [];
    public List<float[]> Values { get; set; } = [];
    public List<NodeConfig> Nodes = [];

    public override NodeBase? Load(NodeLoader loader)
    {
        IfElseNode node;
        if (loader.Settings.LoadingType == NodeLoadingType.Core)
        {
            node = new IfElseNode(loader.Nodes, this).InitBlankUI();
        }
        else
        {
            node = new IfElseNode(loader.Nodes, this).InitUI();
        }

        loader.Nodes.Add(node);

        for (int i = 0; i < Nodes.Count; i++)
        {
            var childNodeConfig = Nodes[i];
            var childNode = childNodeConfig.Load(loader);
            if (childNode != null)
            {
                childNode.ParentIfElseNode = node;
                if (loader.Settings.LoadingType == NodeLoadingType.Full)
                {
                    childNode.Collection.UIController?.AbsoluteElements.Remove(childNode.Collection);
                    childNode.Collection.RemoveFromParent();
                    node.SubCollection.AddElement(childNode.Collection);
                }
            }
        }

        loader.HandleInputs(node, Inputs);
        loader.HandleValues(node, Values, []);
        return node;
    }
}
