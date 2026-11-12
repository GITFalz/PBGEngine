using System.Text;

namespace PBG.Editor;

public class JsonHelper
{
    public StringBuilder Text;

    private int _settingsCount = 0;

    public JsonHelper()
    {
        Text = new();
        Text.Append('{');
    }

    public void Serialize(string name, object? value)
    {
        if (_settingsCount > 0)
            Text.Append(',');

        Text.Append($"\"{name}\":\"{value}\"");

        _settingsCount++;
    }

    public void SerializeList<T>(T[] array, string name, Action<JsonHelper, T> action)
    {   
        if (_settingsCount > 0)
            Text.Append(',');

        Text.Append($"\"{name}\":[");

        for (int i = 0; i < array.Length; i++)
        {
            var oldCount = _settingsCount;
            _settingsCount = 0;

            if (i > 0)
                Text.Append(',');

            Text.Append('{');

            action.Invoke(this, array[i]);

            Text.Append('}');

            _settingsCount = oldCount;
        }

        Text.Append(']');

        _settingsCount++;
    }
    
    public void SerializeList<T>(List<T> list, string name, Action<JsonHelper, T> action)
    {   
        if (_settingsCount > 0)
            Text.Append(',');

        Text.Append($"\"{name}\":[");

        for (int i = 0; i < list.Count; i++)
        {
            var oldCount = _settingsCount;
            _settingsCount = 0;

            if (i > 0)
                Text.Append(',');

            Text.Append('{');

            action.Invoke(this, list[i]);

            Text.Append('}');

            _settingsCount = oldCount;
        }

        Text.Append(']');

        _settingsCount++;
    }

    public static bool TrySerializeSimpleValue(JsonHelper json, object? value, Type type)
    {
        if (value == null)
        {
            json.Serialize("value", null);
            return true;
        }

        // primitives + string
        if (IsPrimitiveOrString(type))
        {
            json.Serialize("value", value);
            return true;
        }

        // small arrays of primitives
        if (type.IsArray)
        {
            var elemType = type.GetElementType()!;
            if (IsPrimitiveOrString(elemType))
            {
                var arr = (Array)value;
                if (arr.Length <= 64)          // your threshold
                {
                    json.Serialize("value", arr);   // your JsonHelper needs to support this
                    json.Serialize("count", arr.Length);
                    return true;
                }
                else
                {
                    json.Serialize("count", arr.Length);
                    return false; // treat as expandable
                }
            }
        }

        // You can special-case Vector2, Vector3, Color, etc. here if you want them inlined

        return false; // everything else → expandable
    }

    private static bool IsPrimitiveOrString(Type type)
    {
        return type.IsPrimitive || type == typeof(string) || type.IsEnum;
    }
    

    public override string ToString() => Text.ToString() + '}';
}
