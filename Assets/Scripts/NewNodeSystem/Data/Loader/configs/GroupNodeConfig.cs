using PBG;

namespace PBG.LoaderConfig;

public class GroupNodeConfig : NodeConfig
{
    public string Name { get; set; } = "";
    public string File { get; set; } = "";
    public float[] Position { get; set; } = [0, 0];
    public string GUID { get; set; } = "";
    public Dictionary<string, string?> Inputs { get; set; } = [];
    public List<string> Outputs { get; set; } = [];
    public List<float[]> Values { get; set; } = [];
    
    public override NodeBase? Load(NodeLoader loader)
    {
        var file = Path.Combine(Game.MainPath, "custom", "groups", File + ".json");
        var groupSettings = new GroupLoaderSettings() { LoadingType = GroupLoadingType.Node };
        if (!GroupLoader.Load(file, groupSettings, out var groupData))
            return null;

        var data = groupData.Value;
        var node = new GroupNode(GUID, loader.Nodes, data, (Position[0], Position[1]), File);
    
        int i;
        for (i = 0; i < data.Nodes.Nodes.Count; i++)
        {
            data.Nodes.Nodes[i].ParentGroup = node;
        }

        loader.GroupNodes.TryAdd(node.GroupName, []);
        node.SetInputSaveNames(loader.GroupNodes[node.GroupName].Count);

        i = 0;
        foreach (var (key, _) in data.Inputs)
        {
            if (node.InputFields.TryGetValue(key, out var gInput))
            {
                var name = $"{GUID}_{key}";
                if (Inputs.TryGetValue(name, out var outputName) && outputName != null)
                {
                    if (gInput.Input != null)
                    {
                        loader.InputMap.Add((gInput.Input, outputName));
                    }
                }
                if (Values.Count > i)
                {
                    for (int k = 0; k < Values[i].Length; k++)
                    {
                        var value = Values[i][k];
                        gInput.Value.SetValue(value, k);
                    }
                }
                i++;
            } 
        }

        foreach (var (key, _) in data.Outputs)
        {
            if (node.OutputFields.TryGetValue(key, out var output))
            {
                var name = $"{GUID}_{key}";
                loader.OutputMap.TryAdd(name, output.Output);
            }
        }

        loader.GroupNodes[node.GroupName].Add(node, data);
        loader.Nodes.Add(node);
        return node;
    }
}