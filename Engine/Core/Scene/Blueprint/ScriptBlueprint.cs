namespace PBG.Core;

public abstract class ScriptBlueprint(SceneDefinitionNode parent, string name) : ScriptDefinition(parent, name)
{
    public abstract ScriptingNode? CreateInstance();
    public abstract void Refresh();
    public ScriptingNode? Copy()
    {
        var instance = CreateInstance();
        if (instance == null || ScriptingNode == null)
            return null;

        var fields = ScriptingNode.GetType().GetFields();
        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            if (!Attribute.IsDefined(field, typeof(FieldAttribute)))
                continue;
            
            var value = field.GetValue(ScriptingNode);
            field.SetValue(instance, value);
        }

        var properties = ScriptingNode.GetType().GetProperties();
        for (int i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            if (!Attribute.IsDefined(property, typeof(FieldAttribute)))
                continue;
            
            var value = property.CanRead ? property.GetValue(ScriptingNode) : null;
            if (property.CanWrite) property.SetValue(instance, value);
        }

        return instance;
    }
}
