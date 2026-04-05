using System.Reflection;
using PBG.Rendering;

namespace PBG.Core;

public abstract class ScriptDefinition(SceneDefinitionNode parent, string name)
{
    public readonly SceneDefinitionNode Parent = parent;
    public string Name = name;
    public ScriptingNode? ScriptingNode = null; 
    public ScriptStatus Status { get; protected set; } = ScriptStatus.None;
    public abstract bool IsScriptValid { get; }

    public FieldInfo[] GetFields()
    {
        if (ScriptingNode == null) return [];
        List<FieldInfo> infos = [];
        var fields = ScriptingNode.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).OrderBy(f => f.MetadataToken);
        foreach (var field in fields)
        {
            if (Attribute.IsDefined(field, typeof(FieldAttribute)))
                infos.Add(field);
        }
        return [..infos];
    }

    public PropertyInfo[] GetProperties()
    {
        if (ScriptingNode == null) return [];
        List<PropertyInfo> infos = [];
        var fields = ScriptingNode.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).OrderBy(f => f.MetadataToken);
        foreach (var field in fields)
        {
            if (Attribute.IsDefined(field, typeof(FieldAttribute)))
                infos.Add(field);
        }
        return [..infos];
    }

    public MemberInfo[] GetMembers()
    {
        if (ScriptingNode == null) return [];
        return ScriptingNode.GetMembers();
    }

    public SceneScriptJson GetJson()
    {
        var json = new SceneScriptJson
        {
            Name = Name
        };

        if (ScriptingNode == null)
            return json;

        var fields = GetMembers();
        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            object? value;
            if (field is FieldInfo fieldInfo)
                value = fieldInfo.GetValue(ScriptingNode);
            else if (field is PropertyInfo propertyInfo)
                value = propertyInfo.GetValue(ScriptingNode);
            else
                continue;

            json.Fields ??= [];
            json.Fields.Add(GetField(value, field.Name));
        }

        return json;
    }

    private SceneFieldJson GetField(object? value, bool setType) => GetField(value, null, setType);
    private SceneFieldJson GetField(object? value, string? name = null, bool setType = true)
    {
        SceneFieldJson json = new()
        {
            Name = name
        };
        
        if (value is Mesh mesh && mesh.IsCached)
        {
            json.Type = "Mesh";
            json.Value = mesh.LocalPath;
        }
        else if (value is Array array)
        {
            json.Type = value.GetType().FullName;
            json.Values ??= [];

            foreach (object element in array)
                json.Values.Add(GetField(element, false));
        }
        else if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            json.Type = value.GetType().FullName;
            json.Values ??= [];
            
            foreach (object element in enumerable)
                json.Values.Add(GetField(element, false));
        }
        else if (value is ISceneSerializable sceneSerializable)
        {
            json.Type = sceneSerializable.GetType().FullName;
            json.Values = sceneSerializable.Serialize();
        }
        else if (value != null)
        {
            if (setType)
                json.Type = value.GetType().FullName;
            json.Value = value.ToString();
        }
        return json;
    } 
    
    private abstract class FieldSerializer
    {
        
    } 
    private class FieldSerializer<T>(Func<T, string> serialize, Func<string, T> deserialize) : FieldSerializer
    {
        private Func<T, string> _serialize = serialize;
        private Func<string, T> _deserialize = deserialize;
    }
}