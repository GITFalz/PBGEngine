using System.Linq.Expressions;
using System.Text.Json;
using PBG.MathLibrary;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.Data;
using PBG.UI;
using PBG.UI.Creator;
using PBG.Voxel;
using static PBG.UI.Styles;
using Silk.NET.Input;

public abstract class NodeValue
{
    public NodeBase Node;

    public NodeValue(NodeBase node)
    {
        Node = node;
    }
    
    public abstract void ResetValueReferences();
    public abstract void SetValueReferences(List<float> values, ref int index);
    public abstract string GetVariable(bool getCurrentValue);
    public abstract NoiseValue GetNoiseValue();
    public abstract ValueType GetValueType();
    public abstract UIElementBase GetInputFields();
    public abstract string GetGLSLType();
    public abstract void SetValue(float value, int index);
    public abstract Expression GetExpression();

    public void SetSlideValue<TSelf>(UIElement<TSelf> input, int index) where TSelf : UIElement<TSelf>
    {
        if (input is UIField field) SetSlideValue(field, index);
    }
    public abstract void SetSlideValue(UIField input, int index);
    public abstract float[] GetValues();
    public abstract Type GetRealType();
    public abstract void UpdateValue(int index, float value);

    public static uint GetDefaultType(string type)
    {
        return type switch
        {
            "float" => NNS.FLOAT,
            "int" => NNS.INT,
            "vec2" => NNS.VECTOR2,
            "ivec2" => NNS.VECTOR2I,
            "vec3" => NNS.VECTOR3,
            "ivec3" => NNS.VECTOR3I,
            "block" => NNS.BLOCK,
            _ => throw new ArgumentException($"Unsupported value type: {type}")
        };
    }

    public static NodeValue GetValueFromType(NodeBase node, uint type)
    {
        return type switch
        {
            NNS.FLOAT => new NodeValue_Float(node, 0f),
            NNS.INT => new NodeValue_Int(node, 0),
            NNS.VECTOR2 => new NodeValue_Vector2(node, 0f, 0f),
            NNS.VECTOR2I => new NodeValue_Vector2Int(node, 0, 0),
            NNS.VECTOR3 => new NodeValue_Vector3(node, 0f, 0f, 0f),
            NNS.VECTOR3I => new NodeValue_Vector3Int(node, 0, 0, 0),
            NNS.BLOCK => new NodeValue_Block(node, null),
            _ => throw new ArgumentException($"Unsupported type: {type}")
        };
    }

    public static UIElementBase DefaultParent(params UIElementBase[] SubElements) => new UIVCol(new Class(h_[30], spacing_[5], top_left, grow_children), SubElements);
    public static UIElementBase DefaultInput(UIField inputfield)
    {
        return new UICol(new Class(h_[30], w_[100], blank_sharp_g_[10]), [inputfield]).SetOnClick(c => UIController.SetInputfield(inputfield));
    }
}

public class NodeValue_Float(NodeBase node, float value) : NodeValue(node)
{
    public float Value = value;
    private float _defaultValue = value; // Store the default value for reset functionality
    public int Index = -1;
    public UIField? input1 = null;

    public override void ResetValueReferences()
    {
        Index = -1;
    }
    public override void SetValueReferences(List<float> values, ref int index)
    {
        Index = index;
        values.Add(Value);
        index++;
    }

    public override string GetVariable(bool getCurrentValue) => getCurrentValue ? $"{Value}" : $"values[{Index}]";
    public override NoiseValue GetNoiseValue() => NoiseValue.Float(Value);
    public override ValueType GetValueType() => ValueType.Float;
    public override string GetGLSLType() => "float";
    public override UIElementBase GetInputFields()
    {
        input1 = new UIField(Value.ToString(), mc_[8], middle_left, left_[5], fs_[1]);
        input1.SetOnTextChange(i => NodeHelper.SetValue(ref Value, i, _defaultValue, Index));
        input1.SetOnHold(i => NodeHelper.SetSlideValue(ref Value, i, NodeHelper.SlideSpeed, Index));
        return NodeValue.DefaultParent(NodeValue.DefaultInput(input1));
    }
    public override string ToString() => $"Float: {Value}";
    public override void SetValue(float value, int index)
    {
        Value = value;
        input1?.UpdateText(Value.ToString());
    }
    public override void SetSlideValue(UIField input, int index)
    {
        NodeHelper.SetSlideValue(ref Value, input, NodeHelper.SlideSpeed, Index);
    }
    public override float[] GetValues() => [Value];
    public override Type GetRealType() => typeof(float);
    public override void UpdateValue(int index, float value)
    {
        if (index == 0)
        {
            Value = value;
            GLSLManager.UpdateValue(Index, value);
        }
    }
    public override Expression GetExpression() => Expression.Constant(Value);
}

public class NodeValue_Int(NodeBase node, int value) : NodeValue(node)
{
    public int Value = value;
    private int _defaultValue = value; // Store the default value for reset functionality
    public int Index = -1;
    public UIField? input1 = null;

    public override void ResetValueReferences()
    {
        Index = -1;
    }

    public override void SetValueReferences(List<float> values, ref int index)
    {
        Index = index;
        values.Add(Value);
        index++;
    }

    public override string GetVariable(bool getCurrentValue) => getCurrentValue ? $"{Value}" : $"int(values[{Index}])";
    public override NoiseValue GetNoiseValue() => NoiseValue.Int(Value);
    public override ValueType GetValueType() => ValueType.Int;
    public override string GetGLSLType() => "int";

    public override UIElementBase GetInputFields()
    {
        input1 = new UIField(Value.ToString(), mc_[8], middle_left, left_[5], fs_[1]);
        input1.SetOnTextChange(i => NodeHelper.SetValue(ref Value, i, _defaultValue, Index));
        input1.SetOnHold(i => NodeHelper.SetSlideValue(ref Value, i, NodeHelper.SlideSpeed, Index));
        return NodeValue.DefaultParent(NodeValue.DefaultInput(input1));
    }
    public override string ToString() => $"Int: {Value}";
    public override void SetValue(float value, int index)
    {
        Value = (int)value;
        input1?.UpdateText(Value.ToString());
    }
    public override void SetSlideValue(UIField input, int index)
    {
        NodeHelper.SetSlideValue(ref Value, input, NodeHelper.SlideSpeed, Index);
    }
    public override float[] GetValues() => [Value];
    public override Type GetRealType() => typeof(int);
    public override void UpdateValue(int index, float value)
    {
        if (index == 0)
        {
            Value = (int)value;
            GLSLManager.UpdateValue(Index, value);
        }
    }
    public override Expression GetExpression() => Expression.Constant(Value);
}

public class NodeValue_Vector2(NodeBase node, float x, float y) : NodeValue(node)
{
    public float X = x;
    public float Y = y;
    private float _defaultX = x; // Store the default value for reset functionality
    private float _defaultY = y; // Store the default value for reset functionality
    public int Index1 = -1;
    public int Index2 = -1;
    public UIField? input1 = null;
    public UIField? input2 = null;

    public override void ResetValueReferences()
    {
        Index1 = -1;
        Index2 = -1;
    }

    public override void SetValueReferences(List<float> values, ref int index)
    {
        Index1 = index;
        values.Add(X);
        index++;
        Index2 = index;
        values.Add(Y);
        index++;
    }


    public override string GetVariable(bool getCurrentValue) => getCurrentValue ? $"vec2({X}, {Y})" : $"vec2(values[{Index1}], values[{Index2}])";
    public override NoiseValue GetNoiseValue() => NoiseValue.Vec2((X, Y));
    public override ValueType GetValueType() => ValueType.Vector2;
    public override string GetGLSLType() => "vec2";

    public override UIElementBase GetInputFields()
    {
        input1 = new UIField(X.ToString(), mc_[8], middle_left, left_[5], fs_[1]);
        input1.SetOnTextChange(i => NodeHelper.SetValue(ref X, i, _defaultX, Index1));
        input1.SetOnHold(i => NodeHelper.SetSlideValue(ref X, i, NodeHelper.SlideSpeed, Index1));
        input2 = new UIField(Y.ToString(), mc_[8], middle_left, left_[5], fs_[1]);
        input2.SetOnTextChange(i => NodeHelper.SetValue(ref Y, i, _defaultY, Index2));
        input2.SetOnHold(i => NodeHelper.SetSlideValue(ref Y, i, NodeHelper.SlideSpeed, Index2));
        return NodeValue.DefaultParent(NodeValue.DefaultInput(input1), NodeValue.DefaultInput(input2));
    }

    public override string ToString() => $"Vector2: ({X}, {Y})";
    public override void SetValue(float value, int index)
    {
        if (index == 0)
        {
            X = value;
            input1?.UpdateText(X.ToString());
        }
        else if (index == 1)
        {
            Y = value;
            input2?.UpdateText(Y.ToString());
        }
    }
    public override void SetSlideValue(UIField input, int index)
    {
        if (index == 0) NodeHelper.SetSlideValue(ref X, input, NodeHelper.SlideSpeed, Index1);
        else if (index == 1) NodeHelper.SetSlideValue(ref Y, input, NodeHelper.SlideSpeed, Index2);
    }
    public override float[] GetValues() => [X, Y];
    public override Type GetRealType() => typeof(Vector2);
    public override void UpdateValue(int index, float value)
    {
        switch (index)
        {
            case 0: X = value; GLSLManager.UpdateValue(Index1, value); break;
            case 1: Y = value; GLSLManager.UpdateValue(Index2, value); break;
        }  
    }
    public override Expression GetExpression()
    {
        return Expression.New(
            typeof(Vector2).GetConstructor(new[] { typeof(float), typeof(float) }),
            Expression.Constant(X),
            Expression.Constant(Y)
        );
    }
}

public class NodeValue_Vector2Int(NodeBase node, int x, int y) : NodeValue(node)
{
    public int X = x;
    public int Y = y;
    private int _defaultX = x; // Store the default value for reset functionality
    private int _defaultY = y; // Store the default value for reset functionality
    public int Index1 = -1;
    public int Index2 = -1;
    public UIField? input1 = null;
    public UIField? input2 = null;

    public override void ResetValueReferences()
    {
        Index1 = -1;
        Index2 = -1;
    }

    public override void SetValueReferences(List<float> values, ref int index)
    {
        Index1 = index;
        values.Add(X);
        index++;
        Index2 = index;
        values.Add(Y);
        index++;
    }

    public override string GetVariable(bool getCurrentValue) => getCurrentValue ? $"ivec2({X}, {Y})" : $"ivec2(int(values[{Index1}]), int(values[{Index2}]))";
    public override NoiseValue GetNoiseValue() => NoiseValue.Vec2i((X, Y));
    public override ValueType GetValueType() => ValueType.Vector2i;
    public override string GetGLSLType() => "ivec2";

    public override UIElementBase GetInputFields()
    {
        input1 = new UIField(X.ToString(), mc_[8], middle_left, left_[5], fs_[1]);
        input1.SetOnTextChange(i => NodeHelper.SetValue(ref X, i, _defaultX, Index1));
        input1.SetOnHold(i => NodeHelper.SetSlideValue(ref X, i, NodeHelper.SlideSpeed, Index1));
        input2 = new UIField(Y.ToString(), mc_[8], middle_left, left_[5], fs_[1]);
        input2.SetOnTextChange(i => NodeHelper.SetValue(ref Y, i, _defaultY, Index2));
        input2.SetOnHold(i => NodeHelper.SetSlideValue(ref Y, i, NodeHelper.SlideSpeed, Index2));
        return NodeValue.DefaultParent(NodeValue.DefaultInput(input1), NodeValue.DefaultInput(input2));
    }
    public override string ToString() => $"Vector2i: ({X}, {Y})";
    public override void SetValue(float value, int index)
    {
        if (index == 0)
        {
            X = (int)value;
            input1?.UpdateText(X.ToString());
        }
        else if (index == 1)
        {
            Y = (int)value;
            input2?.UpdateText(Y.ToString());
        }
    }
    public override void SetSlideValue(UIField input, int index)
    {
        if (index == 0) NodeHelper.SetSlideValue(ref X, input, NodeHelper.SlideSpeed, Index1);
        else if (index == 1) NodeHelper.SetSlideValue(ref Y, input, NodeHelper.SlideSpeed, Index2);
    }
    public override float[] GetValues() => [X, Y];
    public override Type GetRealType() => typeof(Vector2i);
    public override void UpdateValue(int index, float value)
    {
        switch (index)
        {
            case 0: X = (int)value; GLSLManager.UpdateValue(Index1, value); break;
            case 1: Y = (int)value; GLSLManager.UpdateValue(Index2, value); break;
        }  
    }
    public override Expression GetExpression()
    {
        return Expression.New(
            typeof(Vector2i).GetConstructor(new[] { typeof(int), typeof(int) }),
            Expression.Constant(X),
            Expression.Constant(Y)
        );
    }
}

public class NodeValue_Vector3(NodeBase node, float x, float y, float z) : NodeValue(node)
{
    public float X = x;
    public float Y = y;
    public float Z = z;
    private float _defaultX = x; // Store the default value for reset functionality
    private float _defaultY = y; // Store the default value for reset functionality
    private float _defaultZ = z; // Store the default value for reset functionality
    public int Index1 = -1;
    public int Index2 = -1;
    public int Index3 = -1;
    public UIField? input1 = null;
    public UIField? input2 = null;
    public UIField? input3 = null;

    public override void ResetValueReferences()
    {
        Index1 = -1;
        Index2 = -1;
        Index3 = -1;
    }

    public override void SetValueReferences(List<float> values, ref int index)
    {
        Index1 = index;
        values.Add(X);
        index++;
        Index2 = index;
        values.Add(Y);
        index++;
        Index3 = index;
        values.Add(Z);
        index++;
    }

    public override string GetVariable(bool getCurrentValue) => getCurrentValue ? $"vec3({X}, {Y}, {Z})" : $"vec3(values[{Index1}], values[{Index2}], values[{Index3}])";
    public override NoiseValue GetNoiseValue() => NoiseValue.Vec3((X, Y, Z));
    public override ValueType GetValueType() => ValueType.Vector3;
    public override string GetGLSLType() => "vec3";

    public override UIElementBase GetInputFields()
    {
        input1 = new UIField(X.ToString(), mc_[8], middle_left, left_[5], fs_[1]);
        input1.SetOnTextChange(i => NodeHelper.SetValue(ref X, i, _defaultX, Index1));
        input1.SetOnHold(i => NodeHelper.SetSlideValue(ref X, i, NodeHelper.SlideSpeed, Index1));
        input2 = new UIField(Y.ToString(), mc_[8], middle_left, left_[5], fs_[1]);
        input2.SetOnTextChange(i => NodeHelper.SetValue(ref Y, i, _defaultY, Index2));
        input2.SetOnHold(i => NodeHelper.SetSlideValue(ref Y, i, NodeHelper.SlideSpeed, Index2));
        input3 = new UIField(Z.ToString(), mc_[8], middle_left, left_[5], fs_[1]);
        input3.SetOnTextChange(i => NodeHelper.SetValue(ref Z, i, _defaultZ, Index3));
        input3.SetOnHold(i => NodeHelper.SetSlideValue(ref Z, i, NodeHelper.SlideSpeed, Index3));
        return NodeValue.DefaultParent(NodeValue.DefaultInput(input1), NodeValue.DefaultInput(input2), NodeValue.DefaultInput(input3));
    }
    public override string ToString() => $"Vector3: ({X}, {Y}, {Z})";
    public override void SetValue(float value, int index)
    {
        if (index == 0)
        {
            X = value;
            input1?.UpdateText(X.ToString());
        }
        else if (index == 1)
        {
            Y = value;
            input2?.UpdateText(Y.ToString());
        }
        else if (index == 2)
        {
            Z = value;
            input3?.UpdateText(Z.ToString());
        }
    }
    public override void SetSlideValue(UIField input, int index)
    {
        if (index == 0) NodeHelper.SetSlideValue(ref X, input, NodeHelper.SlideSpeed, Index1);
        else if (index == 1) NodeHelper.SetSlideValue(ref Y, input, NodeHelper.SlideSpeed, Index2);
        else if (index == 2) NodeHelper.SetSlideValue(ref Z, input, NodeHelper.SlideSpeed, Index3);
    }
    public override float[] GetValues() => [X, Y, Z];
    public override Type GetRealType() => typeof(Vector3);
    public override void UpdateValue(int index, float value)
    {
        switch (index)
        {
            case 0: X = value; GLSLManager.UpdateValue(Index1, value); break;
            case 1: Y = value; GLSLManager.UpdateValue(Index2, value); break;
            case 2: Z = value; GLSLManager.UpdateValue(Index3, value); break;
        }  
    }

    public override Expression GetExpression()
    {
        return Expression.New(
            typeof(Vector3).GetConstructor(new[] { typeof(float), typeof(float), typeof(float) }),
            Expression.Constant(X),
            Expression.Constant(Y),
            Expression.Constant(Z)
        );
    }
}

public class NodeValue_Vector3Int(NodeBase node, int x, int y, int z) : NodeValue(node)
{
    public int X = x;
    public int Y = y;
    public int Z = z;
    private int _defaultX = x; // Store the default value for reset functionality
    private int _defaultY = y; // Store the default value for reset functionality
    private int _defaultZ = z; // Store the default value for reset functionality
    public int Index1 = -1;
    public int Index2 = -1;
    public int Index3 = -1;
    public UIField? input1 = null;
    public UIField? input2 = null;
    public UIField? input3 = null;

    public override void ResetValueReferences()
    {
        Index1 = -1;
        Index2 = -1;
        Index3 = -1;
    }

    public override void SetValueReferences(List<float> values, ref int index)
    {
        Index1 = index;
        values.Add(X);
        index++;
        Index2 = index;
        values.Add(Y);
        index++;
        Index3 = index;
        values.Add(Z);
        index++;
    }

    public override string GetVariable(bool getCurrentValue) => getCurrentValue ? $"ivec3({X}, {Y}, {Z})" : $"ivec3(int(values[{Index1}]), int(values[{Index2}]), int(values[{Index3}]))";
    public override NoiseValue GetNoiseValue() => NoiseValue.Vec3i((X, Y, Z));
    public override ValueType GetValueType() => ValueType.Vector3i;
    public override string GetGLSLType() => "ivec3";

    public override UIElementBase GetInputFields()
    {
        input1 = new UIField(X.ToString(), mc_[8], middle_left, left_[5], fs_[1]);
        input1.SetOnTextChange(i => NodeHelper.SetValue(ref X, i, _defaultX, Index1));
        input1.SetOnHold(i => NodeHelper.SetSlideValue(ref X, i, NodeHelper.SlideSpeed, Index1));
        input2 = new UIField(Y.ToString(), mc_[8], middle_left, left_[5], fs_[1]);
        input2.SetOnTextChange(i => NodeHelper.SetValue(ref Y, i, _defaultY, Index2));
        input2.SetOnHold(i => NodeHelper.SetSlideValue(ref Y, i, NodeHelper.SlideSpeed, Index2));
        input3 = new UIField(Z.ToString(), mc_[8], middle_left, left_[5], fs_[1]);
        input3.SetOnTextChange(i => NodeHelper.SetValue(ref Z, i, _defaultZ, Index3));
        input3.SetOnHold(i => NodeHelper.SetSlideValue(ref Z, i, NodeHelper.SlideSpeed, Index3));
        return NodeValue.DefaultParent(NodeValue.DefaultInput(input1), NodeValue.DefaultInput(input2), NodeValue.DefaultInput(input3));
    }
    public override string ToString() => $"Vector3i: ({X}, {Y}, {Z})";
    public override void SetValue(float value, int index)
    {
        if (index == 0)
        {
            X = (int)value;
            input1?.UpdateText(X.ToString());
        }
        else if (index == 1)
        {
            Y = (int)value;
            input2?.UpdateText(Y.ToString());
        }
        else if (index == 2)
        {
            Z = (int)value;
            input3?.UpdateText(Z.ToString());
        }
    }
    public override void SetSlideValue(UIField input, int index)
    {
        if (index == 0) NodeHelper.SetSlideValue(ref X, input, NodeHelper.SlideSpeed, Index1);
        else if (index == 1) NodeHelper.SetSlideValue(ref Y, input, NodeHelper.SlideSpeed, Index2);
        else if (index == 2) NodeHelper.SetSlideValue(ref Z, input, NodeHelper.SlideSpeed, Index3);
    }
    public override float[] GetValues() => [X, Y, Z];
    public override Type GetRealType() => typeof(Vector3i);
    public override void UpdateValue(int index, float value)
    {
        switch (index)
        {
            case 0: X = (int)value; GLSLManager.UpdateValue(Index1, value); break;
            case 1: Y = (int)value; GLSLManager.UpdateValue(Index2, value); break;
            case 2: Z = (int)value; GLSLManager.UpdateValue(Index3, value); break;
        }  
    }
    public override Expression GetExpression()
    {
        return Expression.New(
            typeof(Vector3i).GetConstructor(new[] { typeof(int), typeof(int), typeof(int) }),
            Expression.Constant(X),
            Expression.Constant(Y),
            Expression.Constant(Z)
        );
    }
}

public class NodeValue_Block : NodeValue
{
    public string? Name
    {
        get => _name;
        set
        {
            _name = value;
            _deleteButton.SetVisible(_name != null);
            var img = Collection.GetElement<UIImg>();
            if (img != null)
            {
                if (Name != null)
                {
                    img.SetVisible(true);
                    img.UpdateItem(Name);
                }
                else
                {
                    img.SetVisible(false);
                }
            }
            
        }
    }
    private string? _name = "";
    public UIElementBase Collection;
    private UIElementBase _deleteButton = null!;
    private bool click = false;

    public NodeValue_Block(NodeBase node, string? name) : base(node)
    {
        _deleteButton = new UICol(new Class(w_[20], h_[20], top_right, bottom_[5], left_[5], rgb_[0.9f, 0.2f, 0.1f], blank_sharp),
        new OnClickEvent<UICol>(col =>
        {
            if (Name == null)
                return;

            Name = null;
            NodeManager.Compile();
        }),
        [
            new UIText("X", new Class(mc_[1], fs_[0.9f], middle_center))
        ]);


        var blockIcon = new UIImg(w_[50], h_[50], middle_left, left_[5], name != null ? item_[name] : hidden, bg_white);

        Collection = new UICol(new Class(h_[60], w_[60], blank_sharp_g_[60]),
        new OnClickEvent<UICol>(col => click = true),
        new OnHoverEvent<UICol>(_ =>
        {
            if (Input.IsMouseReleased(MouseButton.Left))
            {
                if (!click)
                {
                    var dragBlock = StructureNodeManager.Instance.dragBlockUI.DragBlockCollection;
                    Name = dragBlock.Dataset.String("block");
                    Console.WriteLine(Name);
                    NodeManager.Compile();
                }
                click = false;
            }
        }),
        [
            blockIcon,
            _deleteButton
        ]);
        Node.BlockIconCollections.Add(this);
        StructureNodeManager.UpdateIconsAfterUpdate = true;

        Name = name;
    }

    public int GetID() => (Name != null && BlockData.BlockNames.TryGetValue(Name, out var id)) ? (int)id : -1;

    public override NoiseValue GetNoiseValue() => Name != null && BlockData.BlockNames.TryGetValue(Name, out var id) ? NoiseValue.Int((int)id) : NoiseValue.Int(-1);
    public override string GetGLSLType() => "";
    public override UIElementBase GetInputFields() => Collection;
    public override float[] GetValues() => [];
    public override ValueType GetValueType() => ValueType.Block;
    public override string GetVariable(bool getCurrentValue) => Name?.ToUpper() ?? "0";
    public override void ResetValueReferences() { }
    public override void SetValue(float value, int index) { }
    public override void SetSlideValue(UIField input, int index) {}
    public override void SetValueReferences(List<float> values, ref int index) { }
    public override Type GetRealType() => typeof(int);
    public override void UpdateValue(int index, float value)
    {

    }
    public override string ToString() => "Block: " + GetID();
    public override Expression GetExpression() => Expression.Constant(GetID());
}