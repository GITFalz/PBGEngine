using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using PBG.Rendering;

namespace PBG.Core;

public class ScriptingNode
{
    public Scene Scene = Scene.CurrentScene;
    public Camera Camera => Scene.Camera;

    public string Name = "ScriptingNode";
    public TransformNode Transform = null!;

    public bool GetMethod(string methodName, [NotNullWhen(true)] out MethodInfo? methodInfo)
    {
        methodInfo = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return methodInfo != null;
    }

    public MemberInfo[] GetMembers()
    {
        List<MemberInfo> infos = [];
        var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var field in fields)
        {
            if (Attribute.IsDefined(field, typeof(FieldAttribute)))
                infos.Add(field);
        }
        var properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var property in properties)
        {
            if (property.CanRead && property.CanWrite && Attribute.IsDefined(property, typeof(FieldAttribute)))
                infos.Add(property);
        }
        return [..infos.OrderBy(f => f.MetadataToken)];
    }

    public ScriptingNode? Copy()
    {
        var instance = (ScriptingNode?)Activator.CreateInstance(GetType());
        if (instance == null)
            return null;

        var fields = GetType().GetFields();
        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            if (!Attribute.IsDefined(field, typeof(FieldAttribute)))
                continue;

            var value = field.GetValue(this);
            field.SetValue(instance, value);
        }

        var properties = GetType().GetProperties();
        for (int i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            if (!Attribute.IsDefined(property, typeof(FieldAttribute)))
                continue;

            var value = property.CanRead ? property.GetValue(this) : null;
            if (property.CanWrite) property.SetValue(instance, value);
        }

        return instance;
    }
}