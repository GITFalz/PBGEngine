using System.Diagnostics.CodeAnalysis;
using PBG.UI;

public abstract class NodeAbstractField
{
    public NodeBase Node;
    public string Identifier;
    public uint DefaultType = 0;
    public uint CurrentType = 0;
    public uint OverloadFlags = 0;
    public string? OverloadAsInput = null;
    public NodeValue Value = null!;

    public string GetUniqueName() => Node.ID + "_" + Identifier;

    public NodeAbstractField(NodeBase node, string identifier, string type)
    {
        Node = node;
        Identifier = identifier;
        SetType(type);
    }

    public abstract string GetVariable(bool getCurrentValue);
    public bool IsOverloadable() => OverloadFlags != 0;
    public bool IsOverloadable(uint flags) => (OverloadFlags & flags) != 0;
    public void SetValueReferences(List<float> values, ref int index) => Value.SetValueReferences(values, ref index);
    public abstract void SetButton(UIButton button);
    public void SetName(string name) => Identifier = name;
    public void SetType(string type)
    {
        DefaultType = NodeValue.GetDefaultType(type);
        SetDefaultType();
    }

    public void SetValueType(string type)
    {
        uint typeId = NodeValue.GetDefaultType(type);
        CurrentType = typeId;
        Value = NodeValue.GetValueFromType(Node, typeId);
    }

    public void SetValueType(uint type)
    {
        CurrentType = type;
        Value = NodeValue.GetValueFromType(Node, type);
    }
    public void SetDefaultType()
    {
        CurrentType = DefaultType;
        Value = NodeValue.GetValueFromType(Node, DefaultType);
    }
}

public class NodeInputField : NodeAbstractField
{
    public NodeInput? Input = null;
    public bool HasConnectionPoint = false;
    public bool IsExternal = false;
    public bool CanOverload = false;
    public NodeValue? ConversionValue = null; // Is the "from" value for conversion, e.g. from vector3 to float; we can assume it isn't null if NeedsConversion is true

    public NodeInputField(UIButton? button, NodeBase nodeBase, NodeInputParam input) : base(nodeBase, input.Name, input.Type)
    {
        HasConnectionPoint = input.CanConnect;
        IsExternal = input.External;
        CanOverload = input.CanOverload;
        if (!input.CanConnect || button == null)
            return;

        Input = new NodeInput(button, nodeBase, this);
    }

    public void Disconnect() => Input?.Disconnect();
    public void Connect()
    {
        if (Input != null)
            NodeBase.Connect(Input);
    }

    public override void SetButton(UIButton button)
    {   
        if (Input != null) Input.Button = button;
    }

    public bool IsConnected() => Input != null && Input.IsConnected;
    public bool IsConnected([NotNullWhen(true)] out NodeOutput? output)
    {
        if (Input == null || !Input.IsConnected || Input.Output == null)
        {
            output = null;
            return false;
        }

        output = Input.Output;
        return true;
    }
    public override string GetVariable(bool getCurrentValue)
    {
        if (Input == null || !Input.IsConnected || Input.Output == null)
        {
            return Value.GetVariable(getCurrentValue);
        }
        if (ConversionValue != null)
        {
            return GLSLHelper.GLSLConvertTo(ConversionValue.GetValueType(), Value.GetValueType(), Input.Output.VariableName);
        }
        return Input.Output.VariableName;
    }
}

public class NodeOutputField : NodeAbstractField
{
    public NodeOutput Output;
    public bool IsOutParameter;

    public NodeOutputField(UIButton button, NodeBase nodeBase, NodeOutputParam output) : base(nodeBase, output.Name, output.Type)
    {
        Output = new NodeOutput(button, nodeBase, this);
        IsOutParameter = output.IsOutParameter;
    }

    public void Disconnect() => Output.Disconnect();
    public void Connect() => NodeBase.Connect(Output);


    public override void SetButton(UIButton button) => Output.Button = button;

    public bool IsConnected() => Output != null && Output.IsConnected;
    public override string GetVariable(bool getCurrentValue) => IsOutParameter ? Output.VariableName : $"{Value.GetGLSLType()} {Output.VariableName}";
}