using PBG.MathLibrary;

namespace PBG.Nodes;

[InternalSystemInit(InitPriority.Data)]
public static class InternalNodeBuilder
{
    public static void Init()
    {
        NodeTemplate.New("FragColor", NodeDefinitionType.IsOutput | NodeDefinitionType.Priority)
        .SetColor(NodeTypeColors.Output).AddSelection("Output")
        .Input("Output", NodeDataType.Numeric)
        .SetExecute(context => NodeExpression.Assign(NodeExpression.Variable(typeof(Vector4), "FragColor"), context.GetInput(0)));

        NodeTemplate.New("SetBlock", NodeDefinitionType.IsOutput | NodeDefinitionType.Priority)
        .SetColor(NodeTypeColors.Output).AddSelection("Output")
        .Input("Position", NodeDataType.Vector3i)
        .Input("Block", NodeDataType.Int)
        .SetExecute(context => NodeExpression.Call("SetBlock", [typeof(Vector3i), typeof(int)], context.GetInput(0), context.GetInput(1)));

        NodeTemplate.New("Position", NodeDefinitionType.GPU)
        .SetColor(NodeTypeColors.Output).AddSelection("Variable")
        .Output("", NodeDataType.Vector3i)
        .SetExecute(context => context.SetOutput(0, NodeExpression.Variable(typeof(Vector3i), "iPosition")));

        NodeTemplate.New("Local", NodeDefinitionType.GPU)
        .SetColor(NodeTypeColors.Output).AddSelection("Variable")
        .Output("", NodeDataType.Vector3i)
        .SetExecute(context => context.SetOutput(0, NodeExpression.Variable(typeof(Vector3i), "iLocal")));

        NodeTemplate.New("X", NodeDefinitionType.GPU)
        .SetColor(NodeTypeColors.Output).AddSelection("Variable")
        .Output("", NodeDataType.Vector3i)
        .SetExecute(context => context.SetOutput(0, NodeExpression.Variable(typeof(Vector3i), "x")));

        NodeTemplate.New("Y", NodeDefinitionType.GPU)
        .SetColor(NodeTypeColors.Output).AddSelection("Variable")
        .Output("", NodeDataType.Vector3i)
        .SetExecute(context => context.SetOutput(0, NodeExpression.Variable(typeof(Vector3i), "y")));

        NodeTemplate.New("Z", NodeDefinitionType.GPU)
        .SetColor(NodeTypeColors.Output).AddSelection("Variable")
        .Output("", NodeDataType.Vector3i)
        .SetExecute(context => context.SetOutput(0, NodeExpression.Variable(typeof(Vector3i), "z")));
    }
}