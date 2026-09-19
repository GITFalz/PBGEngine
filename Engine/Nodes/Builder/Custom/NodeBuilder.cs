using PBG.MathLibrary;

namespace PBG.Nodes;

[InternalSystemInit(InitPriority.Data)]
public static class CustomNodeBuilder
{
    public static void Init()
    {
        NodeTemplate.New("Add")
        .SetColor(NodeTypeColors.Math).AddSelection("Math")
        .Input("A", NodeDataType.Numeric).Input("B", NodeDataType.Numeric).Output("Result", NodeDataType.Numeric)
        .SetExecute(context => context.SetOutput(0, NodeExpression.Add(context.GetInput(0), context.GetInput(1))));


        NodeTemplate.New("Subtract")
        .SetColor(NodeTypeColors.Math).AddSelection("Math")
        .Input("A", NodeDataType.Numeric).Input("B", NodeDataType.Numeric).Output("Result", NodeDataType.Numeric)
        .SetExecute(context => context.SetOutput(0, NodeExpression.Subtract(context.GetInput(0), context.GetInput(1))));
        

        NodeTemplate.New("Multiply")
        .SetColor(NodeTypeColors.Math).AddSelection("Math")
        .Input("A", NodeDataType.Numeric).Input("B", NodeDataType.Numeric).Output("Result", NodeDataType.Numeric)
        .SetExecute(context => context.SetOutput(0, NodeExpression.Multiply(context.GetInput(0), context.GetInput(1))));


        NodeTemplate.New("Divide")
        .SetColor(NodeTypeColors.Math).AddSelection("Math")
        .Input("A", NodeDataType.Numeric).Input("B", NodeDataType.Numeric).Output("Result", NodeDataType.Numeric)
        .SetExecute(context => context.SetOutput(0, NodeExpression.Divide(context.GetInput(0), context.GetInput(1))));


        NodeTemplate.New("Mod")
        .SetColor(NodeTypeColors.Math).AddSelection("Math")
        .Input("A", NodeDataType.Numeric).Input("B", NodeDataType.Numeric).Output("Result", NodeDataType.Numeric)
        .SetExecute(context => context.SetOutput(0, NodeExpression.Modulo(context.GetInput(0), context.GetInput(1))));


        NodeTemplate.New("Max")
        .SetColor(NodeTypeColors.Utility).AddSelection("Utility")
        .Input("A", NodeDataType.Numeric).Input("B", NodeDataType.Numeric).Output("Result", NodeDataType.Numeric)
        .SetExecute(context => context.SetOutput(0, NodeExpression.Max(context.GetInput(0), context.GetInput(1))));


        NodeTemplate.New("Min")
        .SetColor(NodeTypeColors.Utility).AddSelection("Utility")
        .Input("A", NodeDataType.Numeric).Input("B", NodeDataType.Numeric).Output("Result", NodeDataType.Numeric)
        .SetExecute(context => context.SetOutput(0, NodeExpression.Min(context.GetInput(0), context.GetInput(1))));


        NodeTemplate.New("Power")
        .SetColor(NodeTypeColors.Math).AddSelection("Math")
        .Input("A", NodeDataType.Numeric).Input("B", NodeDataType.Numeric).Output("Result", NodeDataType.Numeric)
        .SetExecute(context => context.SetOutput(0, NodeExpression.Power(context.GetInput(0), context.GetInput(1))));


        NodeTemplate.New("Atan2")
        .SetColor(NodeTypeColors.Math).AddSelection("Math")
        .Input("A", NodeDataType.Numeric).Input("B", NodeDataType.Numeric).Output("Result", NodeDataType.Numeric)
        .SetExecute(context => context.SetOutput(0, NodeExpression.Atan2(context.GetInput(0), context.GetInput(1))));



        NodeTemplate.New("If", NodeDefinitionType.Block | NodeDefinitionType.Priority)
        .SetColor(NodeTypeColors.Logic).AddSelection("Logic")
        .Input("Condition", NodeDataType.Bool).Block("True")
        .SetExecute(context => NodeExpression.If(context.GetInput(0), context.GetBlock(0)));


        NodeTemplate.New("IfElse", NodeDefinitionType.Block | NodeDefinitionType.Priority)
        .SetColor(NodeTypeColors.Logic).AddSelection("Logic")
        .Input("Condition", NodeDataType.Bool).Block("True").Block("False")
        .SetExecute(context => NodeExpression.IfElse(context.GetInput(0), context.GetBlock(0), context.GetBlock(1)));


        NodeTemplate.New("Assign", NodeDefinitionType.Dependency | NodeDefinitionType.Priority)
        .SetColor(NodeTypeColors.Logic).AddSelection("Logic")
        .Input("Value", NodeDataType.Any).Input("Variable", NodeDataType.Any)
        .SetExecute(context => NodeExpression.Assign(context.GetInput(1), context.GetInput(0)));



        NodeTemplate.New("Equal")
        .SetColor(NodeTypeColors.Math).AddSelection("Logic")
        .Input("A", NodeDataType.Numeric).Input("B", NodeDataType.Numeric).Output("Result", NodeDataType.Bool)
        .SetExecute(context => context.SetOutput(0, NodeExpression.Equal(context.GetInput(0), context.GetInput(1))));

        NodeTemplate.New("Not Equal")
        .SetColor(NodeTypeColors.Math).AddSelection("Logic")
        .Input("A", NodeDataType.Numeric).Input("B", NodeDataType.Numeric).Output("Result", NodeDataType.Bool)
        .SetExecute(context => context.SetOutput(0, NodeExpression.NotEqual(context.GetInput(0), context.GetInput(1))));

        NodeTemplate.New("Greater")
        .SetColor(NodeTypeColors.Math).AddSelection("Logic")
        .Input("A", NodeDataType.Numeric).Input("B", NodeDataType.Numeric).Output("Result", NodeDataType.Bool)
        .SetExecute(context => context.SetOutput(0, NodeExpression.GreaterThan(context.GetInput(0), context.GetInput(1))));

        NodeTemplate.New("Greater Or Equal")
        .SetColor(NodeTypeColors.Math).AddSelection("Logic")
        .Input("A", NodeDataType.Numeric).Input("B", NodeDataType.Numeric).Output("Result", NodeDataType.Bool)
        .SetExecute(context => context.SetOutput(0, NodeExpression.GreaterThanOrEqual(context.GetInput(0), context.GetInput(1))));

        NodeTemplate.New("Less")
        .SetColor(NodeTypeColors.Math).AddSelection("Logic")
        .Input("A", NodeDataType.Numeric).Input("B", NodeDataType.Numeric).Output("Result", NodeDataType.Bool)
        .SetExecute(context => context.SetOutput(0, NodeExpression.LessThan(context.GetInput(0), context.GetInput(1))));

        NodeTemplate.New("Less Or Equal")
        .SetColor(NodeTypeColors.Math).AddSelection("Logic")
        .Input("A", NodeDataType.Numeric).Input("B", NodeDataType.Numeric).Output("Result", NodeDataType.Bool)
        .SetExecute(context => context.SetOutput(0, NodeExpression.LessThanOrEqual(context.GetInput(0), context.GetInput(1))));


        NodeTemplate.New("Float")
        .SetColor(NodeTypeColors.Input).AddSelection("Value")
        .Input("", NodeDataType.Float | NodeDataType.FieldOnly, 0f)
        .Output("", NodeDataType.Float)
        .SetExecute(context => context.SetOutput(0, context.GetInput(0)));

        NodeTemplate.New("Int")
        .SetColor(NodeTypeColors.Input).AddSelection("Value")
        .Input("", NodeDataType.Int | NodeDataType.FieldOnly, 0)
        .Output("", NodeDataType.Int)
        .SetExecute(context => context.SetOutput(0, context.GetInput(0)));


        NodeTemplate.New("Vector2")
        .SetColor(NodeTypeColors.Input).AddSelection("Value")
        .Input("", NodeDataType.Vector2 | NodeDataType.FieldOnly, Vector2.Zero)
        .Output("", NodeDataType.Vector2)
        .SetExecute(context => context.SetOutput(0, context.GetInput(0)));

        NodeTemplate.New("Vector2i")
        .SetColor(NodeTypeColors.Input).AddSelection("Value")
        .Input("", NodeDataType.Vector2i | NodeDataType.FieldOnly, Vector2i.Zero)
        .Output("", NodeDataType.Vector2i)
        .SetExecute(context => context.SetOutput(0, context.GetInput(0)));


        NodeTemplate.New("Vector3")
        .SetColor(NodeTypeColors.Input).AddSelection("Value")
        .Input("", NodeDataType.Vector3 | NodeDataType.FieldOnly, Vector3.Zero)
        .Output("", NodeDataType.Vector3)
        .SetExecute(context => context.SetOutput(0, context.GetInput(0)));
        
        NodeTemplate.New("Vector3i")
        .SetColor(NodeTypeColors.Input).AddSelection("Value")
        .Input("", NodeDataType.Vector3i | NodeDataType.FieldOnly, Vector3i.Zero)
        .Output("", NodeDataType.Vector3i)
        .SetExecute(context => context.SetOutput(0, context.GetInput(0)));


        NodeTemplate.New("Vector4")
        .SetColor(NodeTypeColors.Input).AddSelection("Value")
        .Input("", NodeDataType.Vector4 | NodeDataType.FieldOnly, Vector4.Zero)
        .Output("", NodeDataType.Vector4)
        .SetExecute(context => context.SetOutput(0, context.GetInput(0)));
        
        NodeTemplate.New("Vector4i")
        .SetColor(NodeTypeColors.Input).AddSelection("Value")
        .Input("", NodeDataType.Vector4 | NodeDataType.FieldOnly, Vector4i.Zero)
        .Output("", NodeDataType.Vector4i)
        .SetExecute(context => context.SetOutput(0, context.GetInput(0)));


        
        NodeTemplate.New("Split")
        .SetColor(NodeTypeColors.Vector)
        .Input("Value", NodeDataType.Numeric)
        .Output("X", NodeDataType.Float | NodeDataType.Int)
        .Output("Y", NodeDataType.Float | NodeDataType.Int)
        .Output("Z", NodeDataType.Float | NodeDataType.Int)
        .Output("W", NodeDataType.Float | NodeDataType.Int)
        .SetGPUExecute(context => {
            var input = context.GetInput(0);
            var subType = input.GetSubType();
            var dataSize = input.DataSize();
            if (dataSize == 1)
            {
                context.SetOutput(0, input);
            }
            else
            {
                context.SetOutput(0, NodeExpression.Field(input, subType, "x"));
                context.SetOutput(1, NodeExpression.Field(input, subType, "y"));
                if (dataSize > 2) context.SetOutput(2, NodeExpression.Field(input, subType, "z"));
                if (dataSize > 3) context.SetOutput(3, NodeExpression.Field(input, subType, "w"));
            }
        })
        .SetCPUExecute(context => {
            var input = context.GetInput(0);
            var subType = input.GetSubType();
            var dataSize = input.DataSize();
            if (dataSize == 1)
            {
                context.SetOutput(0, input);
            }
            else
            {
                context.SetOutput(0, NodeExpression.Field(input, subType, "X"));
                context.SetOutput(1, NodeExpression.Field(input, subType, "Y"));
                if (dataSize > 2) context.SetOutput(2, NodeExpression.Field(input, subType, "Z"));
                if (dataSize > 3) context.SetOutput(3, NodeExpression.Field(input, subType, "W"));
            }
        });
    }
}