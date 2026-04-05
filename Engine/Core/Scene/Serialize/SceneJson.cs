using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using PBG.Data;
using PBG.MathLibrary;

namespace PBG.Core;

public class SceneJsonParser
{
    public static Vector3 GetVector3(List<float> values) => GetVector3(values, Vector3.Zero);
    public static Vector3 GetVector3(List<float> values, Vector3 @default)
    {
        for (int i = 0; i < 3.Min(values.Count); i++)
            @default[i] = values[i];
        return @default;
    }

    public static Vector4 GetVector4(List<float> values)
    {
        Vector4 v = Vector4.Zero;
        for (int i = 0; i < 4.Min(values.Count); i++)
            v[i] = values[i];
        return v;
    }

    public static Quaternion GetQuaternion(List<float> values)
    {
        Quaternion v = Quaternion.Identity;
        for (int i = 0; i < 4.Min(values.Count); i++)
            v[i] = values[i];
        return v;
    }
}

public class SceneBaseJson : SceneJsonParser
{
    public string Name { get; set; } = "";
    public List<SceneNodeJson> Nodes { get; set; } = [];

    public void Load()
    {
        if (Name == "" || Scene.Scenes.ContainsKey(Name))
        {
            string sceneName = "Scene";
            int z = 1;
            while (Scene.Scenes.ContainsKey(sceneName))
            {
                sceneName = "Scene_"+z;
                z++;
            }
            Name = sceneName;
        }

        SceneBlueprint.Blueprint.Clear();
        SceneBlueprint.Name = Name;
        for (int i = 0; i < Nodes.Count; i++)
            Nodes[i].Load();
    }
}

public class SceneNodeJson : SceneJsonParser
{
    public string Name { get; set; } = "";
    public List<float>? Position { get; set; } = null;
    public List<float>? Rotation { get; set; } = null;
    public List<float>? Scale { get; set; } = null;
    public List<SceneScriptJson>? Scripts { get; set; } = null;
    public List<SceneNodeJson>? Nodes { get; set; } = null;

    public void Load()
    {
        SceneBlueprintNode node = (SceneBlueprintNode)SceneDefinition.Blueprint.AddNode(Name);
        LoadGlobal(node);
    }

    public void Load(SceneBlueprintNode parent)
    {
        SceneBlueprintNode node = parent.AddNode(Name);
        LoadGlobal(node);
    }

    public void LoadGlobal(SceneBlueprintNode node)
    {
        if (Position != null) node.Transform.Position = GetVector3(Position);
        if (Rotation != null) node.Transform.Rotation = GetQuaternion(Rotation);
        if (Scale != null) node.Transform.Scale    = GetVector3(Scale, Vector3.One);

        if (Scripts != null)
        for (int i = 0; i < Scripts.Count; i++)
            Scripts[i].Load(node);

        if (Nodes != null) 
        for (int i = 0; i < Nodes.Count; i++)
            Nodes[i].Load(node);
    }
}

public class SceneScriptJson : SceneJsonParser
{
    public string Name { get; set; } = "";
    public List<SceneFieldJson>? Fields { get; set; } = null;

    public void Load(SceneBlueprintNode parent)
    {
        parent.AddScript(this);
    }

    public void ParseFields(ScriptBlueprint script)
    {
        var members = script.GetMembers();
        Dictionary<string, MemberInfo> memberMap = [];
        for (int i = 0; i < members.Length; i++)
        {
            var member = members[i];
            memberMap.Add(member.Name, member);
        }

        if (Fields != null)
        for (int i = 0; i < Fields.Count; i++)
        {
            var field = Fields[i];
            field.ParseField(memberMap, script);
        }
    }
}

public class SceneFieldJson : SceneJsonParser
{
    public string? Name { get; set; } = null;
    public string? Type { get; set; } = null;
    public string? Value { get; set; } = null;
    public List<SceneFieldJson>? Values { get; set; } = null;

    public SceneFieldJson() {}
    public SceneFieldJson(object value)
    {
        Type = value.GetType().FullName;
        Value = value.ToString();
    }

    public void ParseField(Dictionary<string, MemberInfo> memberMap, ScriptDefinition script)
    {
        if (Name == null || !memberMap.TryGetValue(Name, out var member))
            return;

        var value = GetValue();
        if (member is FieldInfo fieldInfo)
        {
            Type memberType = fieldInfo.FieldType;
            if (CheckValue(memberType, value))
                fieldInfo.SetValue(script.ScriptingNode, value);
        }
        else if (member is PropertyInfo propertyInfo && propertyInfo.CanRead && propertyInfo.CanWrite)
        {
            Type memberType = propertyInfo.PropertyType;
            if (CheckValue(memberType, value))
                propertyInfo.SetValue(script.ScriptingNode, value);
        }
        Console.WriteLine("=== Parsing Field ===");
        Debug.Print(value);
    }

    private bool CheckValue(Type type, object? value)
    {
        if (value == null)
        {
            if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
            {
                return false; 
            }
        }
        else if (!type.IsAssignableFrom(value.GetType()))
        {
            return false;
        }

        return true;
    }

    public bool TryParse<T>([NotNullWhen(true)] out T? value)
    {
        value = default;
        if (Type == null)
            return false;

        Type? type = System.Type.GetType(Type);
        if (type == null)
            return false;
            
        value = (T?)Convert.ChangeType(Value, type);
        return value != null;
    }

    public object? GetValue(Type? type = null)
    {
        if (type == null && Type != null)
            type = System.Type.GetType(Type);
    
        if (type == null)
            return null;

        if (type.IsArray)
        {
            Type? elementType = type.GetElementType();
            if (elementType == null || Values == null)
                return null;

            Array array = Array.CreateInstance(elementType, Values.Count);
            for (int i = 0; i < Values.Count; i++)
            {
                var value = Values[i].GetValue(elementType);
                array.SetValue(value, i);
            }

            return array;
        }


        if (Value != null)
        {
            if (type.IsEnum)
                return Enum.Parse(type, Value);

            if (type.IsPrimitive || type == typeof(decimal))
                return Convert.ChangeType(Value, type);
        }
        
        if (Values != null)
        {
            object? obj = Activator.CreateInstance(type);
            if (obj is ISceneSerializable sceneSerializable && Values != null)
            {
                sceneSerializable.Deserialize(Values);
                return sceneSerializable;
            }    
        }

        return null;
    }

    private T ParseVector<T>(List<SceneFieldJson> values, int count) where T : new()
    {
        T v = new();
        if (values == null) return v;

        for (int i = 0; i < Math.Min(count, values.Count); i++)
            if (values[i].TryParse<float>(out var value))
                ((dynamic)v)[i] = value;

        return v;
    }
}