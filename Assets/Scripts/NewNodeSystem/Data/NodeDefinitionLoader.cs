using System.Text.Json;
using System.Text.RegularExpressions;
using PBG.MathLibrary;
using PBG;

public class NodeDefinitionLoader
{
    public static Dictionary<string, NodeDefinition> NodeDefinitions = [];

    public static string NodeDefinitionPath = Path.Combine(Game.MainPath, "data", "nodeDefinitions");
    public static string NodePath = Path.Combine(NodeDefinitionPath, "nodes");

    private static bool _loaded = false;

    public NodeDefinitionLoader()
    {
        if (_loaded)
            return;
        
        _loaded = true;
        if (!Directory.Exists(NodeDefinitionPath))
            Directory.CreateDirectory(NodeDefinitionPath);

        if (!Directory.Exists(NodePath))
            Directory.CreateDirectory(NodePath);

        LoadNodes();
    }

    public static void LoadNodes()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var nodes = Directory.GetDirectories(NodePath);
        for (int i = 0; i < nodes.Length; i++)
        {
            string nodeFolder = nodes[i];
            if (nodeFolder.EndsWith("base"))
                continue;

            string configFilePath = Path.Combine(nodeFolder, "config.json");
            if (!File.Exists(configFilePath))
            {
                Console.WriteLine("No config file was found for the node at: " + nodeFolder);
                continue;
            }

            string json = File.ReadAllText(configFilePath);
            var definition = JsonSerializer.Deserialize<NodeDefinition>(json, options);
            if (definition != null)
            {
                definition.Folder = nodeFolder;
                NodeDefinitions.Add(definition.Name, definition);
            }
        }
    }

    public static NodeDefinition? GetNodeDefinition(string name)
    {
        if (NodeDefinitions.TryGetValue(name, out var definition))
            return definition;
        return null;
    }

    public static string[] GetInclude(NodeDefinition node, string file)
    {
        var path = Path.Combine(node.Folder, "includes", file);
        if (!File.Exists(path))
            return[];
            
        var lines = File.ReadAllLines(path);
        return lines;
    }

    public static string[] GetFunction(NodeDefinition node, string file)
    {
        var path = Path.Combine(node.Folder, "functions", file);
        if (!File.Exists(path))
            return[];

        var lines = File.ReadAllLines(path);
        return lines;
    }
}

public class NodeInputParam
{
    public string Name { get; set; } = "";
    public List<string> Sub { get; set; } = [];
    public string Type { get; set; } = "";
    public bool External { get; set; } = false;
    public bool CanConnect { get; set; } = true;
    public bool CanOverload { get; set; } = false;

    public override string ToString()
    {
        var sub = Sub != null && Sub.Count > 0 ? $"[{string.Join(", ", Sub)}]" : "[]";
        return $"Input: {Name} ({Type}) - Sub: {sub}, External: {External}";
    }
}

public class NodeOutputParam
{
    public string Name { get; set; } = "";
    public List<string> Sub { get; set; } = [];
    public string Type { get; set; } = "";
    public bool IsOutParameter { get; set; } = false;
    public NodeOutputParam() { }
    public NodeOutputParam(string name, string type) { Name = name; Type = type; }

    public override string ToString()
    {
        var sub = Sub != null && Sub.Count > 0 ? $"[{string.Join(", ", Sub)}]" : "[]";
        return $"Output: {Name} ({Type}) - Sub: {sub}";
    }
}

public class NodeAction
{
    public string Type { get; set; } = "";
    public List<string> Sub { get; set; } = [];
    public string Category { get; set; } = "function";
    public string Function { get; set; } = "";
    public bool Queryable { get; set; } = false;
    public bool Custom { get; set; } = true;

    public override string ToString()
    {
        return $"Action: {Type} - Category: {Category}, Function: {Function}";
    }
}

public class NodeDefinition 
{
    public string Name { get; set; } = "";
    public string Folder { get; set; } = "";
    public string Class { get; set; } = "";
    public string Color { get; set; } = "";
    public bool IsOutput { get; set; } = false;
    public bool Precompile { get; set; } = true;
    public List<NodeInputParam> Inputs { get; set; } = [];
    public List<NodeOutputParam> Outputs { get; set; } = [];
    public List<NodeAction> Actions { get; set; } = [];
    public Dictionary<string, string> GlobalFunctions { get; set; } = [];
    public Dictionary<string, string> NodeFunctions { get; set; } = [];
    public Dictionary<string, string> ComputeFunctions { get; set; } = [];
    public List<string> Includes { get; set; } = [];
    public List<string> Selectors { get; set; } = [];

    public NodeAction? GetAction(string type)
    {
        if (Actions.Count == 0)
            return null;

        for (int i = 0; i < Actions.Count; i++)
        {
            var action = Actions[i];
            if (action.Type == type)
                return action;
                
            for (int j = 0; j < action.Sub.Count; j++)
            {
                if (action.Sub[i] == type)
                    return action;
            }
        }

        return Actions[0];
    }

    public Vector3 GetColor()
    {
        if (Color.StartsWith("#")) Color = Color.Substring(1);
        if (Color.Length != 6)
            throw new ArgumentException("Hex must be 6 characters (RRGGBB)");

        byte r = Convert.ToByte(Color.Substring(0, 2), 16);
        byte g = Convert.ToByte(Color.Substring(2, 2), 16);
        byte b = Convert.ToByte(Color.Substring(4, 2), 16);

        return new Vector3(r / 255f, g / 255f, b / 255f);
    }

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Node: {Name} ({Color})");

        // Inputs
        if (Inputs != null && Inputs.Count > 0)
        {
            sb.AppendLine("  Inputs:");
            foreach (var input in Inputs)
            {
                sb.AppendLine($"    {input}");
            }
        }

        // Outputs
        if (Outputs != null && Outputs.Count > 0)
        {
            sb.AppendLine("  Outputs:");
            foreach (var output in Outputs)
            {
                sb.AppendLine($"    {output}");
            }
        }

        // Actions
        if (Actions != null && Actions.Count > 0)
        {
            sb.AppendLine("  Actions:");
            foreach (var action in Actions)
            {
                sb.AppendLine($"    {action}");
            }
        }

        // GlobalFunctions
        if (GlobalFunctions != null && GlobalFunctions.Count > 0)
        {
            sb.AppendLine("  GlobalFunctions:");
            foreach (var kvp in GlobalFunctions)
            {
                sb.AppendLine($"    {kvp.Key}: {kvp.Value}");
            }
        }

        // Includes
        if (Includes != null && Includes.Count > 0)
        {
            sb.AppendLine($"  Includes: [{string.Join(", ", Includes)}]");
        }

        // Selectors
        if (Selectors != null && Selectors.Count > 0)
        {
            sb.AppendLine($"  Selectors: [{string.Join(", ", Selectors)}]");
        }

        return sb.ToString().TrimEnd();
    }
}