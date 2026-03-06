using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.Data;
using PBG.LoaderConfig;
using PBG.UI;

public class CustomNode : NodeBase
{
    /* Node info */
    public bool IsOutput;

    private List<NodeInputParam> _inputFields = [];
    private List<NodeOutputParam> _outputFields = [];

    /* Node logic */

    private NodeDefinition _nodeDefinition = null!;

    public string Type = "";
    

    public CustomNode(NodeCollection nodeCollection) : base(null, nodeCollection) {}
    public CustomNode(NodeCollection nodeCollection, CustomNodeConfig config) : this(config.GUID, nodeCollection, config.Name, (config.Position[0], config.Position[1]), config.Type)
    {
        OutputPriority = config.OutputPriority;
    }
    public CustomNode(NodeCollection nodeCollection, string name, Vector2 position, string type) : this(null, nodeCollection, name, position, type) { }
    public CustomNode(string? guid, NodeCollection nodeCollection, string name, Vector2 position, string type) : base(guid, nodeCollection)
    { 
        InitCore(name, position, type); 
    }

    public override string GetName() => $"{Type}Node " + ID;

    public CustomNode InitCore(string name, Vector2 position, string type)
    {
        if (string.IsNullOrEmpty(type))
            throw new ArgumentException("Type cannot be null or empty.", nameof(type));

        Type = type;
        Name = name;
        GridPosition = Mathf.Round(position / 10f) * 10f;
        Position = GridPosition;
        _position = position;
        if (!NodeDefinitionLoader.NodeDefinitions.TryGetValue(Name, out var nodeDefinition))
            throw new Exception($"Node definition '{Name}' not found. Make sure you have correctly filled in the JSON file.");
        else
            _nodeDefinition = nodeDefinition;

        Color = _nodeDefinition.GetColor();

        InitializeCore();

        return this;
    }

    public NoiseNode? GenerateNoiseNode(NoiseNodeManager manager, ref int index) => GenerateNoiseNode([], ref index);
    public NoiseNode? GenerateNoiseNode(NoiseValue[] variables, ref int index, int indexOffset = 0)
    {
        //Debug.Print($"[Node] : Generating noise node for node '{GetName()}'");
        NodeAction? action = _nodeDefinition.GetAction(Type) ?? throw new InvalidOperationException($"No action of type {Type} was found in the {Name} node");
        if (action.Category == "function")
        {
            List<OLD_GetterValue> getters = [];
            List<OLD_SetterValue> setters = [];

            foreach (var (name, input) in InputFields)
            {
                if (input.IsExternal)
                    continue;

                OLD_GetterValue getter;
                if (input.Input == null || !input.Input.IsConnected || input.Input.Output == null)
                {
                    getter = new OLD_GetterValue(variables, index + indexOffset);
                    //Debug.Print($"[Node] : Getter value is constant with index: {index} and value: {input.Value.GetNoiseValue()} " + name);
                    variables[index] = input.Value.GetNoiseValue();
                    index++;
                }
                else
                {
                    getter = new OLD_GetterValue(variables, input.Input.Output.Index + indexOffset);
                    //Debug.Print($"[Node] : Getter value is variable with index: {input.Input.Output.Index + indexOffset} " + name);
                }
                getters.Add(getter);
            }

            foreach (var (name, output) in OutputFields)
            {
                output.Output.Index = index;
                index++;

                OLD_SetterValue setter = new OLD_SetterValue(variables, output.Output.Index + indexOffset);
                //Debug.Print($"[Node] : Setter value is variable with index: {output.Output.Index + indexOffset} " + name);
                setters.Add(setter);
            }

            /*
            string className = $"PBG.Assets.Scripts.NoiseNodes.Nodes.{_nodeDefinition.Class}";
            Type? type = System.Type.GetType(className) ?? throw new InvalidOperationException("The class at " + className + " was not found");

            var instance = Activator.CreateInstance(type, getters.ToArray(), setters.ToArray(), Type);
            if (instance == null)
                throw new InvalidOperationException("Failed to create instance of type " + type.GetType().Name);
            if (instance is not NoiseNode)
                throw new InvalidOperationException("Noise node instance is not the correct type " + instance.GetType().Name);

            return (NoiseNode)instance;
            */
        }
        return null;
    }

    public override NoiseNode? BuildExpression(Dictionary<NodeBase, Dictionary<string, SetterValue>> connections, List<Expression> expressions)
    {
        if (connections.ContainsKey(this))
        {
            //Debug.Print($"[BuildExpression] Node '{GetName()}' already in connections, skipping");
            return null;
        }
        
        Dictionary<string, SetterValue> setterValues = [];
        //Debug.Print($"[BuildExpression] Starting expression generation for node '{GetName()}' (Type: {Type})");
        
        NodeAction? action = _nodeDefinition.GetAction(Type);
        if (action == null)
        {
            //Debug.Print($"[BuildExpression] ERROR: No action of type {Type} found in node '{Name}'");
            throw new InvalidOperationException($"No action of type {Type} was found in the {Name} node");
        }
        
        //Debug.Print($"[BuildExpression] Action category: {action.Category}");
        
        if (action.Category == "function")
        {
            List<GetterValue> getters = [];
            List<SetterValue> setters = [];
            
            //Debug.Print($"[BuildExpression] Processing {InputFields.Count} input fields");
            
            foreach (var (name, input) in InputFields)
            {
                if (input.IsExternal)
                {
                    //Debug.Print($"  [Input] '{name}' - SKIPPED (external)");
                    continue;
                }
                
                GetterValue getter;
                Expression defaultValue = input.Value.GetExpression();
                
                if (input.Input == null || !input.Input.IsConnected || input.Input.Output == null)
                {
                    //Debug.Print($"  [Input] '{input.Identifier}' - NOT CONNECTED, using default value " + input.Value);
                    getter = new(input.Value.GetValueType(), null, defaultValue);
                }
                else
                {
                    string sourceNode = input.Input.Output.Node.GetName();
                    string sourceOutput = input.Input.Output.OutputField.Identifier;
                    //Debug.Print($"  [Input] '{input.Identifier}' - Connected to '{sourceNode}.{sourceOutput}'");
                    
                    if (connections.TryGetValue(input.Input.Output.Node, out var sets) && sets.TryGetValue(sourceOutput, out var set))
                    {
                        //Debug.Print($"    [Setter] Found setter for '{sourceOutput}'");
                        
                        if (input.ConversionValue != null && set.Variable != null)
                        {
                            //Debug.Print($"    [Conversion] {input.ConversionValue.GetValueType()} -> {input.Value.GetValueType()}");
                            //Debug.Print($"    [Variable] Type: {set.Variable.Type}");
                            getter = new(input.Value.GetValueType(), ExpressionHelper.ConvertTo(input.ConversionValue.GetValueType(), input.Value.GetValueType(), set.Variable), defaultValue);
                        } 
                        else
                        {
                            string reason = input.ConversionValue == null ? "no conversion needed" : "setter variable is null";
                            //Debug.Print($"    [Direct] Using direct assignment ({reason})");
                            if (set.Variable != null)
                            {
                                //Debug.Print($"    [Variable] Type: {set.Variable.Type}, {input.Value}");
                            }
                            getter = new(set.Variable == null ? input.Value.GetValueType() : input.Input.Output.OutputField.Value.GetValueType(), set.Variable, defaultValue);
                        }
                    }
                    else
                    {
                        //Debug.Print($"    [Setter] NOT FOUND for '{sourceOutput}' in node '{sourceNode}'");
                        //Debug.Print($"    [Fallback] Using default value " + input.Value);
                        getter = new(input.Value.GetValueType(), null, defaultValue);
                    }
                }
                
                getters.Add(getter);
            }
            
            //Debug.Print($"[BuildExpression] Processing {OutputFields.Count} output fields");
            
            foreach (var (name, output) in OutputFields)
            {
                SetterValue setter = new SetterValue(output);
                //Debug.Print($"  [Output] Adding setter '{name}' (Type: {output.Value.GetValueType()})");
                setterValues.Add(name, setter);
                setters.Add(setter);
            }
            
            connections.Add(this, setterValues);
            //Debug.Print($"[BuildExpression] Added {setterValues.Count} setters to connections");
            
            string className = $"PBG.Assets.Scripts.NoiseNodes.Nodes.{_nodeDefinition.Class}";
            //Debug.Print($"[BuildExpression] Attempting to instantiate class: {className}");
            
            Type? type = System.Type.GetType(className);
            if (type == null)
            {
                //Debug.Print($"[BuildExpression] ERROR: Class not found at {className}");
                throw new InvalidOperationException("The class at " + className + " was not found");
            }
            
            //Debug.Print($"[BuildExpression] Creating instance with {getters.Count} getters and {setters.Count} setters");
            
            var instance = Activator.CreateInstance(type, getters.ToArray(), setters.ToArray(), Type);
            if (instance == null)
            {
                //Debug.Print($"[BuildExpression] ERROR: Failed to create instance of {type.Name}");
                throw new InvalidOperationException("Failed to create instance of type " + type.Name);
            }
            
            if (instance is not NoiseNode)
            {
                //Debug.Print($"[BuildExpression] ERROR: Instance is {instance.GetType().Name}, not NoiseNode");
                throw new InvalidOperationException("Noise node instance is not the correct type " + instance.GetType().Name);
            }
            
            //Debug.Print($"[BuildExpression] SUCCESS: NoiseNode created for '{GetName()}'");
            return (NoiseNode)instance;
        }
        
        //Debug.Print($"[BuildExpression] Category is not 'function', returning null");
        return null;
    }

    public override string GetLine(LineContext context)
    {
        NodeAction? action = _nodeDefinition.GetAction(Type) ?? throw new InvalidOperationException($"No action of type {Type} was found in the {Name} node");
        var line = action.Category switch
        {
            "output" => GetOutputLine(context, action, context.GetCurrentValue),
            "function" => GetFunctionLine(context, action, context.GetCurrentValue),
            _ => throw new InvalidOperationException($"Unknown action type: {action.Category}"),
        };
        return line;
    }

    public void GetCompiledCode(ParameterExpression[] variables)
    {

    }

    private string GetOutputLine(LineContext context, NodeAction action, bool getCurrentValue)
    {
        var input = InputFields.First().Value;
        string line = $"display += {input.GetVariable(getCurrentValue)};";
        return line;
    }

    private string GetFunctionLine(LineContext context, NodeAction action, bool getCurrentValue)
    {
        string line = "";
        // Testing if there is a default return value
        foreach (var (_, output) in OutputFields)
        {
            if (output.IsOutParameter)
                line += $"{output.Value.GetGLSLType()} {output.GetVariable(false)};";
        }
        foreach (var (_, output) in OutputFields)
        {
            if (!output.IsOutParameter)
            {
                line += $"{output.GetVariable(false)} = ";
                break; // Only the first output is used as the function return value
            }
        }
        line += (context.Queryable && action.Queryable  ? "Query_" : "") + $"{action.Function}(";
        // Processing inputs
        int i = 0;
        foreach (var input in InputFields.Values)
        {
            line += (i == 0 ? "" : $", ") + (input.IsExternal ? input.Identifier : input.GetVariable(getCurrentValue)); i++;
        }
        // Tesing if there are out parameters
        i = 0;
        foreach (var (_, output) in OutputFields)
        {
            if (output.IsOutParameter)
                line += ((InputFields.Count == 0 && i == 0) ? "" : $", ") + output.GetVariable(false);
            i++;
        }
        line += ");";
        return line;
    }



    private void InitializeCore()
    {
        NodeAction? action = _nodeDefinition.GetAction(Type) ?? throw new InvalidOperationException($"No action of type {Type} was found in the {Name} node");
        IsOutput = _nodeDefinition.IsOutput;
        string actionType = action.Category;
        if (actionType == "output")
        {
            if (_nodeDefinition.Inputs.Count == 0)
                throw new InvalidOperationException("No input fields registered for this node.");

            _inputFields.Add(_nodeDefinition.Inputs[0]);
        }
        else if (actionType == "operation")
        {
            if (_nodeDefinition.Inputs.Count == 0)
                throw new InvalidOperationException("No input fields registered for this node.");

            NodeOutputParam output = _nodeDefinition.Outputs[0]; // Take the first just in case
            foreach (var outField in _nodeDefinition.Outputs)
            {
                if (!outField.IsOutParameter) // If a correct output field is found, you can just use that
                {
                    output = outField;
                    break;
                }
            }
            _outputFields.Add(output);

            foreach (var inputField in _nodeDefinition.Inputs)
            {
                _inputFields.Add(inputField);
            }
        }
        else if (actionType == "function")
        {
            if (_nodeDefinition.Inputs.Count == 0)
                throw new InvalidOperationException("No input fields registered for this node.");

            foreach (var outField in _nodeDefinition.Outputs)
            {
                if (outField.IsOutParameter)
                    continue;

                _outputFields.Add(outField);
                break;
            }

            foreach (var outField in _nodeDefinition.Outputs)
            {
                if (outField.IsOutParameter)
                    _outputFields.Add(outField);
            }

            foreach (var inputField in _nodeDefinition.Inputs)
            {
                _inputFields.Add(inputField);
            }
        }
        else if (actionType == "curve")
        {
            // No curve yet
        }
        else
        {
            throw new InvalidOperationException($"Unknown action type: {actionType}");
        }
    }

    /* Initialization */
    public CustomNode InitBlankUI()
    {
        foreach (var input in _inputFields)
        {
            InputFields.Add(input.Name, new((input.CanConnect && !input.External) ? new() : null, this, input));
        }
        foreach (var output in _outputFields)
        {
            OutputFields.Add(output.Name, new(new(), this, output));
        }
        return this;
    }

    public CustomNode InitUI()
    {
        string nodeName = _nodeDefinition.Actions.Count > 1 ? Type : Name;
        Collection = new CustomNodeUI(this, nodeName, Mathf.FloorToInt(Position), Color, _nodeDefinition.Inputs, _nodeDefinition.Outputs);
        return this;
    }

    public override bool WritesLines() => true;

    public override void ResetValueReferences()
    {
        foreach (var field in InputFields.Values)
        {
            field.Value.ResetValueReferences();
        }
    }

    public override void SetValueReferences(List<float> values, ref int index)
    {
        foreach (var field in InputFields.Values)
        {
            if (field.IsConnected() || field.IsExternal)
                continue;

            field.Value.SetValueReferences(values, ref index);
        }
    }

    public override void DeleteCore()
    {
        foreach (var input in GetInputs())
        {
            input.Disconnect();
        }
        InputFields = [];
        foreach (var output in GetOutputs())
        {
            output.Disconnect();
        }
        OutputFields = [];
        BlockIconCollections = [];
    }

    public override void DeleteNode()
    {
        base.DeleteNode();
        DeleteCore();
        Collection?.Delete();
        NodeManager.RemoveNode(this);
    }

    public override NodeConfig Save()
    {
        var config = new CustomNodeConfig()
        {
            Name = Name,
            Type = Type,
            OutputPriority = OutputPriority,
            Position = [Position.X, Position.Y],
            GUID = ID.ToString()
        };
        foreach (var (_, output) in OutputFields)
        {
            config.Outputs.Add(output.GetUniqueName());
        }
        foreach (var input in InputFields.Values)
        {
            if (input.Input != null)
            {
                config.Inputs.Add(input.GetUniqueName(), (input.Input.IsConnected && input.Input.Output != null) ? input.Input.Output.OutputField.GetUniqueName() : null);
            }
            
            
        }
        foreach (var input in InputFields.Values)
        {
            if (input.Value is NodeValue_Block block)
            {
                config.Blocks.Add(block.Name);
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
    }
}