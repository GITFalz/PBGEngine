using Newtonsoft.Json;
using PBG.LoaderConfig;
using PBG.UI;

public class NodeLoader(NodeLoaderSettings settings, NodeCollection nodes)
{
    public NodeLoaderSettings Settings = settings;
    public Dictionary<string, Dictionary<GroupNode, GroupData>> GroupNodes = [];
    public List<(NodeInput input, string outputName)> InputMap = [];
    public Dictionary<string, NodeOutput> OutputMap = [];
    public NodeCollection Nodes = nodes;

    public NodeLoader() : this(new(), new()) { }
    public NodeLoader(NodeLoaderSettings settings) : this(settings, new()) { }
    public NodeLoader(NodeCollection nodes) : this(new(), nodes) { }

    public bool Load(string path, out NodeCollection nodes)
    {
        nodes = Nodes;
        if (!File.Exists(path))
        {
            return false;
        }

        JsonSerializerSettings settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        string json = File.ReadAllText(path);
        json = json.Replace("PV.Load", "PBG.Load");
        json = json.Replace("Project-Voxel", "PBGEngine");
        NodesConfig? nodeConfig = JsonConvert.DeserializeObject<NodesConfig>(json, settings);
        if (nodeConfig == null)
        {
            return false;
        }

        for (int i = 0; i < nodeConfig.Nodes.Count; i++)
        {
            var config = nodeConfig.Nodes[i];
            config.Load(this);
        }

        foreach (var (input, outputName) in InputMap)
        {
            if (OutputMap.TryGetValue(outputName, out var output))
            {
                input.Connect(output);
                input.Node.InitNodeTypes();
            }
        }

        return true;
    }

    public void Save(string path) => Save(path, Nodes);
    public void Save(string path, NodeCollection collection) => Save(path, collection.Nodes);
    public void Save(string path, List<NodeBase> nodes)
    {
        JsonSerializerSettings settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        var config = NodeManager.GetSavedNodes(nodes);
        string json = JsonConvert.SerializeObject(config, Formatting.Indented, settings);
        File.WriteAllText(path, json);
    }

    public void HandleOutputs(NodeBase node, List<string> Outputs)
    {
        int j;
        for (j = 0; j < Outputs.Count; j++)
        {
            var configOutput = Outputs[j];
            var outputs = node.GetOutputs();
            if (j < outputs.Count)
            {
                var output = outputs[j];
                output.Name = configOutput;
                OutputMap[configOutput] = output;
            }
        }
    }

    public void HandleInputs(NodeBase node, Dictionary<string, string?> Inputs)
    {
        int j = 0;
        foreach (var (configInput, connect) in Inputs)
        {
            var inputs = node.GetInputs();
            if (j < inputs.Count && connect != null)
            {
                var input = inputs[j];
                input.Name = configInput;
                InputMap.Add((input, connect));
            }
            j++;
        }
    }

    public void HandleValues(NodeBase node, List<float[]> Values, List<string?> Blocks)
    {
        int blockCount = 0;
        for (int j = 0; j < Values.Count; j++)
        {
            var configValues = Values[j];
            var inputs = node.GetInputFields();
            if (j < inputs.Count)
            {
                var input = inputs[j];
                if (input.Value is NodeValue_Block blockInput && blockCount < Blocks.Count)
                {
                    blockInput.Name = Blocks[blockCount];
                    blockCount++;
                }
                else
                {
                    for (int k = 0; k < configValues.Length; k++)
                    {
                        input.Value.SetValue(configValues[k], k);
                    }
                }           
            }
        }
    }
}

public class NodeSaver
{
    public void Save(string path, NodeCollection collection) => Save(path, collection.Nodes);
    public void Save(string path, List<NodeBase> nodes)
    {
        JsonSerializerSettings settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        var config = NodeManager.GetSavedNodes(nodes);
        string json = JsonConvert.SerializeObject(config, Formatting.Indented, settings);
        File.WriteAllText(path, json);
    }
}

public struct NodeLoaderSettings()
{
    public NodeLoadingType LoadingType = NodeLoadingType.Full;
}

public enum NodeLoadingType
{
    Core,
    Full,
}