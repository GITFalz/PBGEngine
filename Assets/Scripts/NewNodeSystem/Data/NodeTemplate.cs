using System.Diagnostics.CodeAnalysis;

public class NodeTemplat
{
    public string NodeType = "";
    public string ClassName = "";

    public bool IsOutput = false;

    public Dictionary<string, NodeInputFieldStruct> RegisteredInputFields = [];
    public Dictionary<string, NodeOutputFieldStruct> RegisteredOutputFields = [];
    public Dictionary<string, NodeActionStruct> RegisteredActions = [];
    public List<string> RegisteredSelectors = [];

    public bool IsInputTypeOverloadingEnabled = false;
    public bool IsOutputTypeOverloadingEnabled = false;

    public List<string> GLSLIncludes = [];

    public NodeTemplat(string nodeType, string className)
    {
        NodeType = nodeType;
        ClassName = className;
    }

    public void Clear()
    {
        RegisteredInputFields.Clear();
        RegisteredOutputFields.Clear();
        RegisteredActions.Clear();
    }

    public NodeOutputFieldStruct GetFirstOutput()
    {
        if (RegisteredOutputFields.Count == 0)
            throw new InvalidOperationException("No outputs registered for this node template.");

        return RegisteredOutputFields.First().Value;
    }


    public NodeActionStruct GetAction(bool hasType, string type)
    {
        if (RegisteredActions.Count == 0)
            throw new InvalidOperationException("No actions registered for this node template.");

        if (hasType)
        {
            if (RegisteredActions.TryGetValue(type, out var action))
                return action;
            throw new KeyNotFoundException($"Action with type '{type}' not found.");
        }

        return RegisteredActions.First().Value;
    }


    #region Registration helper functions
    private static void NameCheck(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            throw new ArgumentException("Identifier cannot be null or empty.");

        if (identifier.Contains(" "))
            throw new ArgumentException("Identifier cannot contain spaces.");
    }

    private static uint ParseFlags(string type, out uint flags)
    {
        flags = 0;
        foreach (var t in type.Split(';'))
        {
            if (_overloadFlags.TryGetValue(t, out var flag))
                flags |= flag;
        }
        return flags;
    }
    private static uint ParseFlags(string type) => ParseFlags(type, out _);

    private NodeInputFieldStruct GetInput(string indentifer, [NotNullWhen(true)] out NodeInputFieldStruct input)
    {
        NameCheck(indentifer);
        if (!RegisteredInputFields.TryGetValue(indentifer, out input))
            throw new KeyNotFoundException($"Input with identifier '{indentifer}' not found.");
        return input;
    }
    private NodeInputFieldStruct GetInput(string identifier) => GetInput(identifier, out _);

    private NodeOutputFieldStruct GetOutput(string identifier, [NotNullWhen(true)] out NodeOutputFieldStruct output)
    {
        NameCheck(identifier);
        if (!RegisteredOutputFields.TryGetValue(identifier, out output))
            throw new KeyNotFoundException($"Output with identifier '{identifier}' not found.");
        return output;
    }
    private NodeOutputFieldStruct GetOutput(string identifier) => GetOutput(identifier, out _);

    private uint GetType(string type)
    {
        if (!_overloadFlags.TryGetValue(type, out uint valueType))
            throw new ArgumentException($"Invalid type '{type}' for output.");

        return valueType;
    }
    #endregion
    public void SetNodeAsOutput() => IsOutput = true;

    public void RegisterInput(string identifier) => RegisterInput(identifier, NNS.FLOAT);
    public void RegisterInput(string identifier, string type) => RegisterInput(identifier, GetType(type));
    public void RegisterInput(string identifier, uint type)
    {
        NameCheck(identifier);
        if (RegisteredInputFields.ContainsKey(identifier))
            throw new InvalidOperationException($"Input with identifier '{identifier}' is already registered.");

        var input = new NodeInputFieldStruct
        {
            Identifier = identifier,
            Type = type,
            IsOverloadable = IsInputTypeOverloadingEnabled,
            OverloadFlags = 0
        };
        RegisteredInputFields.Add(identifier, input);
    }



    public void EnableInputTypeOverloading(bool enable)
    {
        IsInputTypeOverloadingEnabled = enable;
        foreach (var key in RegisteredInputFields.Keys.ToList())
        {
            var input = RegisteredInputFields[key];
            input.IsOverloadable = enable;
            RegisteredInputFields[key] = input;
        }
    }

    public void EnableInputTypeOverloading(string identifier, bool enabled)
    {
        GetInput(identifier, out var input);
        input.IsOverloadable = enabled;
        RegisteredInputFields[identifier] = input;
    }



    public void RegisterOutput(string identifier) => RegisterOutput(identifier, NNS.FLOAT);
    public void RegisterOutput(string identifier, string type) => RegisterOutput(identifier, GetType(type));
    public void RegisterOutput(string identifier, uint type)
    {
        NameCheck(identifier);
        if (RegisteredOutputFields.ContainsKey(identifier))
            throw new InvalidOperationException($"Output with identifier '{identifier}' is already registered.");

        var output = new NodeOutputFieldStruct
        {
            Identifier = identifier,
            Type = type,
            IsOverloadable = false,
            OverloadFlags = 0
        };
        RegisteredOutputFields.Add(identifier, output);
    }



    public void RegisterAction(string name, string type, string action)
    {
        NameCheck(name);
        if (RegisteredActions.ContainsKey(name))
            throw new InvalidOperationException($"Action with name '{name}' is already registered.");

        var actionStruct = new NodeActionStruct
        {
            Name = name,
            Type = type,
            Action = action,
            OverloadFlags = 0
        };
        RegisteredActions.Add(name, actionStruct);
    }

    public void RegisterGlobalActionFunction(string name, Func<string> function)
    {
        NameCheck(name);
        if (RegisteredActions.TryGetValue(name, out var action))
        {
            action.NodeFunction = function;
            action.ComputeFunction = function;
            RegisteredActions[name] = action;
        }
        else
        {
            throw new KeyNotFoundException($"Action with name '{name}' not found.");
        }
    }

    public void RegisterNodeActionFunction(string name, Func<string> function)
    {
        NameCheck(name);
        if (RegisteredActions.TryGetValue(name, out var action))
        {
            action.NodeFunction = function;
            RegisteredActions[name] = action;
        }
        else
        {
            throw new KeyNotFoundException($"Action with name '{name}' not found.");
        }
    }

    public void RegisterComputeActionFunction(string name, Func<string> function)
    {
        NameCheck(name);
        if (RegisteredActions.TryGetValue(name, out var action))
        {
            action.ComputeFunction = function;
            RegisteredActions[name] = action;
        }
        else
        {
            throw new KeyNotFoundException($"Action with name '{name}' not found.");
        }
    }

    public void RegisterSelector(string name)
    {
        RegisteredSelectors.Add(name);
    }
        

    public void IncludeGLSLFile(string fileName)
    {
        GLSLIncludes.Add(fileName);
    }

    public void DisableInputConnectionPoint(string identifier)
    {
        NameCheck(identifier);
        if (RegisteredInputFields.TryGetValue(identifier, out var input))
        {
            input.HasConnectionPoint = false;
            RegisteredInputFields[identifier] = input;
        }
        else
        {
            throw new KeyNotFoundException($"Input with identifier '{identifier}' not found.");
        }
    }


    public void SetOutputAsOutParameter(string identifier)
    {
        NameCheck(identifier);
        if (RegisteredOutputFields.TryGetValue(identifier, out var output))
        {
            output.IsOutParameter = true;
            RegisteredOutputFields[identifier] = output;
        }
        else
        {
            throw new KeyNotFoundException($"Output with identifier '{identifier}' not found.");
        }
    }

    public void SetInputAsExternal(string identifier)
    {
        NameCheck(identifier);
        if (RegisteredInputFields.TryGetValue(identifier, out var input))
        {
            input.IsExternal = true;
            RegisteredInputFields[identifier] = input;
        }
        else
        {
            throw new KeyNotFoundException($"Input with identifier '{identifier}' not found.");
        }
    }

    /*
        Flagging does not work here, this is solely for a single type
        If multiple types are found, it will take the smallest as default
        If none have been found, it will default to float
    */
    public void SetDefaultValueType(string identifier, string type) => SetDefaultValueType(identifier, GetType(type));
    public void SetDefaultValueType(string identifier, uint type)
    {
        NameCheck(identifier);
        if (RegisteredInputFields.TryGetValue(identifier, out var input))
        {
            input.Type = type;
            RegisteredInputFields[identifier] = input;
        }
        else if (RegisteredOutputFields.TryGetValue(identifier, out var output))
        {
            output.Type = type;
            RegisteredOutputFields[identifier] = output;
        }
        else
        {
            throw new KeyNotFoundException($"Input or output with identifier '{identifier}' not found.");
        }
    }

    /*
        This is where overloading is handled
        The type is a string, which can be "all", "float", "int", "vector2", "vector2i", "vector3", "vector3i"
        And if you want to specify flags you can write them like this: "float;int;vector2;vector2i;vector3;vector3i"
    */
    public void EnableValueTypeOverloading(string action, string type) => EnableValueTypeOverloading(action, ParseFlags(type));
    public void EnableValueTypeOverloading(string action, uint flags)
    {
        NameCheck(action);
        if (RegisteredInputFields.TryGetValue(action, out var input))
        {
            input.IsOverloadable = true;
            input.OverloadFlags = flags;
            RegisteredInputFields[action] = input;
        }
        else if (RegisteredOutputFields.TryGetValue(action, out var output))
        {
            output.IsOverloadable = true;
            output.OverloadFlags = flags;
            RegisteredOutputFields[action] = output;
        }
        else if (RegisteredActions.TryGetValue(action, out var actionStruct))
        {
            actionStruct.IsOverloadable = true;
            actionStruct.OverloadFlags = flags;
            RegisteredActions[action] = actionStruct;
        }
        else
        {
            throw new KeyNotFoundException($"Identifier '{action}' not found.");
        }
    }

    

    public void EnableOutputTypeOverloading(bool enable)
    {
        IsOutputTypeOverloadingEnabled = enable;
        foreach (var key in RegisteredOutputFields.Keys.ToList())
        {
            var output = RegisteredOutputFields[key];
            output.IsOverloadable = enable;
            RegisteredOutputFields[key] = output;
        }
    }

    public void EnableOutputTypeOverloading(string identifier, bool enable)
    {
        GetOutput(identifier, out var output);
        output.IsOverloadable = enable;
        RegisteredOutputFields[identifier] = output;
    }

    public void SetValueOverloadingAsInput(string identifier, string overloadAsInput)
    {
        NameCheck(identifier);
        if (RegisteredInputFields.TryGetValue(identifier, out var input))
        {
            input.HasInput = true;
            input.OverloadAsInput = overloadAsInput;
            RegisteredInputFields[identifier] = input;
        }
        else if (RegisteredOutputFields.TryGetValue(identifier, out var output))
        {
            output.HasInput = true;
            output.OverloadAsInput = overloadAsInput;
            RegisteredOutputFields[identifier] = output;
        }
        else
        {
            throw new KeyNotFoundException($"Input or output with identifier '{identifier}' not found.");
        }
    }

    private static readonly Dictionary<string, uint> _overloadFlags = new Dictionary<string, uint>()
    {
        { "float", NNS.FLOAT },
        { "int", NNS.INT },
        { "vector2", NNS.VECTOR2 },
        { "vector2i", NNS.VECTOR2I },
        { "vector3", NNS.VECTOR3 },
        { "vector3i", NNS.VECTOR3I }
    };
}

public struct NodeInputFieldStruct
{
    public string Identifier;
    public uint Type;
    public bool IsOverloadable;
    public uint OverloadFlags;
    public bool HasInput;
    public string OverloadAsInput;
    public bool HasConnectionPoint;
    public bool IsExternal;

    public NodeInputFieldStruct()
    {
        Identifier = "";
        Type = NNS.FLOAT;
        IsOverloadable = false;
        OverloadFlags = 0;
        HasInput = false;
        OverloadAsInput = "";
        HasConnectionPoint = true;
        IsExternal = false;
    }
}

public struct NodeOutputFieldStruct
{
    public string Identifier;
    public uint Type;
    public bool IsOverloadable;
    public uint OverloadFlags;
    public bool HasInput;
    public string OverloadAsInput;
    public bool IsOutParameter;

    public NodeOutputFieldStruct()
    {
        Identifier = "";
        Type = NNS.FLOAT;
        IsOverloadable = false;
        OverloadFlags = 0;
        HasInput = false;
        OverloadAsInput = "";
        IsOutParameter = false;
    }
}

public struct NodeActionStruct
{
    public string Name;
    public string Type;
    public string Action;
    public bool IsOverloadable;
    public uint OverloadFlags;
    public Func<string>? NodeFunction;
    public Func<string>? ComputeFunction;

    public NodeActionStruct()
    {
        Name = "";
        Type = "function";
        Action = "";
        IsOverloadable = false;
        OverloadFlags = 0;
        NodeFunction = null;
        ComputeFunction = null;
    }
}