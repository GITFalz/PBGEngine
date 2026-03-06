using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using PBG.LoaderConfig;
using PBG.UI;
using static PBG.UI.Styles;

public static class GroupLoader
{
    public static bool Load(string path, [NotNullWhen(true)] out GroupData? data) => Load(path, new(), out data);
    public static bool Load(string path, GroupLoaderSettings settings, [NotNullWhen(true)] out GroupData? data)
    {
        data = null;
        if (!File.Exists(path))
        {
            return false;
        }

        JsonSerializerSettings jsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        string json = File.ReadAllText(path);
        json = json.Replace("PV.Load", "PBG.Load");
        json = json.Replace("Project-Voxel", "PBGEngine");
        GroupsConfig? nodeConfig = JsonConvert.DeserializeObject<GroupsConfig>(json, jsonSettings);
        if (nodeConfig == null)
        {
            Console.WriteLine("Something went wrong when loading the nodes");
            return false;
        }

        NodeLoader loader = new();

        var groupData = new GroupData();
        groupData.Nodes = loader.Nodes;

        var inputNode = new GroupInputNode(loader.Nodes, nodeConfig);
        groupData.Add(inputNode);
        foreach (var (name, output) in inputNode.OutputFields)
        {
            loader.OutputMap.Add(output.Output.OutputField.GetUniqueName(), output.Output);
        }

        var outputNode = new GroupOutputNode(loader.Nodes, nodeConfig);
        groupData.Add(outputNode);
        int j = 0;
        var inputs = outputNode.GetInputs();
        foreach (var (configInput, connect) in nodeConfig.Outputs)
        {
            var input = inputs[j];
            input.Name = configInput;
            loader.InputMap.Add((input, connect));
            j++;
        }

        if (settings.LoadingType == GroupLoadingType.Edit)
        {
            NodeManager.NodeUIController.AddElement(inputNode.Collection);
            NodeManager.NodeUIController.AddElement(outputNode.Collection);
        }

        if (settings.LoadingType == GroupLoadingType.Node)
        {
            int i = 0;
            foreach (var (name, type) in nodeConfig.Inputs)
            {
                var size = type switch
                {
                    "float" => 1,
                    "int" => 1,
                    "vec2" => 2,
                    "ivec2" => 2,
                    "vec3" => 3,
                    "ivec3" => 3,
                    _ => 0
                };
                groupData.Inputs.Add(name, type);
                i++;
            }
        }

        for (int i = 0; i < nodeConfig.Nodes.Count; i++)
        {
            var config = nodeConfig.Nodes[i];
            var node = config.Load(loader);
            if (node != null)
            {
                groupData.Add(node);
            }
        }

        if (settings.LoadingType == GroupLoadingType.Node)
        {
            NodeManager.GroupDisplayController.AddElement(groupData.GroupNodeDisplayCollection);

            for (int i = 0; i < groupData.Nodes.Nodes.Count; i++)
            {
                var node = groupData.Nodes.Nodes[i];
                groupData.GroupNodeDisplayCollection.AddElement(node.Collection);
            }

            NodeManager.GroupDisplayController.Transform.Disabled = false;
        }
        else
        {
            for (int i = 0; i < groupData.Nodes.Nodes.Count; i++)
            {
                var node = groupData.Nodes.Nodes[i];
                NodeManager.NodeUIController.AddElement(node.Collection);
            }
        }

        for (int i = 0; i < loader.InputMap.Count; i++)
        {
            var (input, outputName) = loader.InputMap[i];
            if (loader.OutputMap.TryGetValue(outputName, out var output))
            {
                input.Connect(output);
                input.Node.InitNodeTypes();
            }
        }

        foreach (var (name, connect) in nodeConfig.Outputs)
        {
            if (loader.OutputMap.TryGetValue(connect, out var output))
            {
                groupData.Outputs[name] = output;
            }
        }

        data = groupData;
        return true;
    }
}

public struct GroupData
{
    public NodeCollection Nodes = new();
    public Dictionary<string, string> Inputs = [];
    public Dictionary<string, NodeOutput> Outputs = [];
    public GroupInputNode? GroupInputNode;
    public GroupOutputNode? GroupOutputNode;
    public UICol GroupNodeDisplayCollection = new(blank_round_g_[15], border_ui_[10, 10, 10, 10], border_color_g_[25], hidden);

    public GroupData() { }

    public void Add(NodeBase node)
    {
        if (node is GroupInputNode gin)
            GroupInputNode = gin;
        if (node is GroupOutputNode goon)
            GroupOutputNode = goon;
        Nodes.Add(node);
    }
}

public struct GroupLoaderSettings
{
    public GroupLoadingType LoadingType = GroupLoadingType.Edit;
    public GroupLoaderSettings() { }
}

public enum GroupLoadingType
{
    Node,
    Edit
}