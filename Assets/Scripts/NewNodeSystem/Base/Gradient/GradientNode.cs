using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.Data;
using PBG.LoaderConfig;
using PBG.UI;
using PBG.UI.Creator;
using static PBG.UI.Styles;

public class GradientNode : NodeBase
{
    private static readonly string _groupsPath = Path.Combine(Game.MainPath, "custom", "groups");

    public Dictionary<string, NodeInputField> InputFields = [];

    public GradientNode(NodeCollection nodeCollection) : base(null, nodeCollection)
    {

    }

    public override string GetName() => "GradientNode";

    public override NodeConfig Save()
    {
        /*
        var config = new OldNodeConfig()
        {
            Name = "Gradient",
            File = "",
            Position = [Position.X, Position.Y]
        };
        foreach (var input in InputFields.Values)
        {
            if (input.Input != null)
                config.Inputs.Add(input.Input.Name, input.Input.Output?.Name);
        }
        foreach (var input in InputFields.Values)
        {
            if (input.Value is NodeValue_Block block)
            {
                config.Block = block.Name;
            }
            else
            {
                var values = input.Value.GetValues();
                if (values.Length == 0)
                    continue;
                config.Values.Add(values);
            }
        }
        return config;
        */
        return new EmptyNodeConfig();
    }

    public override NoiseNode? BuildExpression(Dictionary<NodeBase, Dictionary<string, SetterValue>> connections, List<Expression> expressions)
    {
        return null;
    }
    
    public override void DeleteCore()
    {
        foreach (var input in GetInputs())
        {
            input.Disconnect();
        }
        InputFields = [];
        BlockIconCollections = [];
    }

    public override void DeleteNode()
    {
        base.DeleteNode();
        DeleteCore();
        Collection?.Delete();
        NodeManager.RemoveNode(this);
    }
}