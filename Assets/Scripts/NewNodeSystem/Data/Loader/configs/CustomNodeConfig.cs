using PBG.UI;

namespace PBG.LoaderConfig;

public class CustomNodeConfig : NodeConfig
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string GUID { get; set; } = "";
    public int OutputPriority { get; set; } = 0;
    public float[] Position { get; set; } = [0, 0];
    public Dictionary<string, string?> Inputs { get; set; } = [];
    public List<string> Outputs { get; set; } = [];
    public List<float[]> Values { get; set; } = [];
    public List<string?> Blocks { get; set; } = [];

    public override NodeBase? Load(NodeLoader loader)
    {
        CustomNode node;
        if (loader.Settings.LoadingType == NodeLoadingType.Core)
        {
            try
            {
                node = new CustomNode(loader.Nodes, this).InitBlankUI();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] : blank node {Name} was not found");
                Console.WriteLine($"[Error] : it threw error '{ex.Message}'");
                return null;
            }
        }
        else
        {
            try
            {
                node = new CustomNode(loader.Nodes, this).InitUI();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] : node {Name} was not found");
                Console.WriteLine($"[Error] : it threw error '{ex.Message}'");
                throw;
            }
        }

        loader.Nodes.Add(node);
        loader.HandleOutputs(node, Outputs);
        loader.HandleInputs(node, Inputs);
        loader.HandleValues(node, Values, Blocks);
        return node;
    }
}
