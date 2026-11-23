using System.Reflection;
using PBG.Core;

namespace PBG.Editor;

public static class EditorHelper
{
    public static string GetSceneJson()
    {
        if (Scene.CurrentScene == null)
            return "";

        JsonHelper json = new();
        GetNodeJson(json, Scene.CurrentScene.RootNode);

        return json.ToString();
    }

    private static void GetNodeJson(JsonHelper json, Node node)
    {
        json.Serialize("name", node.Name);
        json.SerializeList(node.Children, "nodes", GetNodeJson);
    }

    public static string GetNodeComponentsJson(string path)
    {
        Console.WriteLine("path: " + path);
        if (!Scene.CurrentScene.GetNode(path, out var node))
            return "";

        JsonHelper json = new();
        json.SerializeList(node.Components, "components", (json, c) =>
        {
            var type = c.GetType();
            var fields = type.GetFields();

            json.Serialize("name", type.FullName);
            json.SerializeList(type.GetProperties(), "properties", (json, f) =>
            {
                json.Serialize("name", f.Name);
                json.Serialize("type", f.PropertyType.FullName);

                object? value = f.GetValue(c);

                if (JsonHelper.TrySerializeSimpleValue(json, value, f.PropertyType))
                {
                }
                else
                {
                    json.Serialize("expandable", true);

                    if (value is System.Collections.ICollection col)
                        json.Serialize("count", col.Count);
                }
            });
            json.SerializeList(fields, "fields", (json, f) =>
            {
                json.Serialize("name", f.Name);
                json.Serialize("type", f.FieldType.FullName);

                object? value = f.GetValue(c);

                if (JsonHelper.TrySerializeSimpleValue(json, value, f.FieldType))
                {
                }
                else
                {
                    json.Serialize("expandable", true);

                    if (value is System.Collections.ICollection col)
                        json.Serialize("count", col.Count);
                }
            });
        });

        return json.ToString();
    }

    public static string SearchClass(string search)
    {
        var types = SearchTypes(search);

        JsonHelper json = new();
        json.SerializeList(types, "Classes", (json, type) =>
        {
            json.Serialize("Class", type.FullName);
        });

        return json.ToString();
    }

    public static string ClassInfo(string name)
    {
        Type? type = GetGlobalType(name);
        if (type == null)
            return "";

        JsonHelper json = new();
        json.Serialize("name", type.FullName);
        json.SerializeList(type.GetProperties(), "properties", (json, f) =>
        {
            json.Serialize("name", f.Name);
            json.Serialize("type", f.PropertyType.FullName);

            bool isStatic = f.GetGetMethod()?.IsStatic == true;
            object? value = isStatic ? f.GetValue(null) : null;

            if (JsonHelper.TrySerializeSimpleValue(json, value, f.PropertyType))
            {
            }
            else
            {
                json.Serialize("expandable", true);

                if (value is System.Collections.ICollection col)
                    json.Serialize("count", col.Count);
            }
        });
        json.SerializeList(type.GetFields(), "fields", (json, f) =>
        {
            json.Serialize("name", f.Name);
            json.Serialize("type", f.FieldType.FullName);

            object? value = f.IsStatic ? f.GetValue(null) : null;

            if (f.IsStatic && JsonHelper.TrySerializeSimpleValue(json, value, f.FieldType))
            {
            }
            else
            {
                json.Serialize("expandable", true);

                if (value is System.Collections.ICollection col)
                    json.Serialize("count", col.Count);
            }
        });

        return json.ToString();
    }

    public static List<Type> SearchTypes(string searchTerm, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<Type>();

        searchTerm = searchTerm.Trim();
        string lowerSearch = searchTerm.ToLowerInvariant();

        // Get all types from the current assembly (you can expand this later)
        var allTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Concat(typeof(Scene).Assembly.GetTypes())
            .Distinct()
            .Where(t => t.IsClass || t.IsValueType || t.IsInterface);

        var results = allTypes
            .Select(t => new
            {
                Type = t,
                FullName = t.FullName ?? t.Name,
                Score = GetMatchScore(t, lowerSearch)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.FullName.Length) // prefer shorter names when score is equal
            .Take(maxResults)
            .Select(x => x.Type)
            .ToList();

        return results;
    }

    private static int GetMatchScore(Type type, string lowerSearch)
    {
        string fullName = (type.FullName ?? type.Name).ToLowerInvariant();
        string name = type.Name.ToLowerInvariant();

        // Exact full name match
        if (fullName == lowerSearch)
            return 100;

        // Exact class name match
        if (name == lowerSearch)
            return 90;

        // Starts with the search term (e.g. "New.Voxel")
        if (fullName.StartsWith(lowerSearch))
            return 80;

        // Contains the full search term
        if (fullName.Contains(lowerSearch))
            return 60;

        // Class name contains the search term
        if (name.Contains(lowerSearch))
            return 40;

        // Partial match on namespace + name pieces
        var parts = lowerSearch.Split(new[] { '.', '+' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.All(p => fullName.Contains(p)))
            return 30;

        return 0;
    }

    public static string GetClass(string path)
    {
        Console.WriteLine("path: " + path);
        if (!Scene.CurrentScene.GetNode(path, out var node))
            return "";

        JsonHelper json = new();
        json.SerializeList(node.Components, "components", (json, c) =>
        {
            var type = c.GetType();

            json.Serialize("name", type.FullName);
            json.SerializeList(type.GetProperties(), "properties", (json, f) =>
            {
                json.Serialize("name", f.Name);
                json.Serialize("type", f.PropertyType.FullName);

                object? value = f.GetValue(c);

                if (JsonHelper.TrySerializeSimpleValue(json, value, f.PropertyType))
                {
                }
                else
                {
                    json.Serialize("expandable", true);

                    if (value is System.Collections.ICollection col)
                        json.Serialize("count", col.Count);
                }
            });
            json.SerializeList(type.GetFields(), "fields", (json, f) =>
            {
                json.Serialize("name", f.Name);
                json.Serialize("type", f.FieldType.FullName);

                object? value = f.GetValue(c);

                if (JsonHelper.TrySerializeSimpleValue(json, value, f.FieldType))
                {
                }
                else
                {
                    json.Serialize("expandable", true);

                    if (value is System.Collections.ICollection col)
                        json.Serialize("count", col.Count);
                }
            });
        });

        return json.ToString();
    }


    public static void SetValue(string json)
    {
        FieldJson? field = JsonHelper.Parse<FieldJson>(json);
        if (field == null)
        {
            Console.WriteLine("[Warning] : Failed to parse field json");
            return;
        }

        if (field.Path == null)
        {
            SetClassField(field);
        }
        else
        {
            SetComponentField(field);
        }
    }

    public static Type? GetGlobalAssembly(string name)
    {
        return typeof(Scene).Assembly.GetType(name, throwOnError: false)
            ?? AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(name, throwOnError: false))
                .FirstOrDefault(foundType => foundType != null);
    }

    public static Type? GetGlobalType(string name)
    {
        return Type.GetType(name) ?? typeof(Scene).Assembly.GetType(name, throwOnError: false)
            ?? AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(name, throwOnError: false))
                .FirstOrDefault(foundType => foundType != null);
    }

    private static void SetClassField(FieldJson field)
    {
        if (field.Class == null || field.Name == null || field.Type == null || field.Value == null)
        {
            Console.WriteLine("[Warning] : Field does not have the required fields");
            return;
        }

        Type? type = GetGlobalType(field.Class);
        if (type == null)
        {
            Console.WriteLine("[Warning] : No type found " + field.Class);
            return;
        }

        var fieldInfo = type.GetField(field.Name);
        var propertyInfo = type.GetProperty(field.Name);
        if (fieldInfo == null && propertyInfo == null)
        {       
            Console.WriteLine("[Warning] : No field or property " + field.Name + " found in component " + field.Class);
            return;
        }

        var value = JsonHelper.Deserialize(field.Value, field.Type);
        try
        {
            fieldInfo?.SetValue(type, value);
            if (propertyInfo != null && propertyInfo.CanWrite)
                propertyInfo.SetValue(type, value);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] : Unable to set field because of reason: " + ex);
            return;
        }
    }

    private static void SetComponentField(FieldJson field)
    {
        if (field.Path == null || field.Class == null || field.Name == null || field.Type == null || field.Value == null)
        {
            Console.WriteLine("[Warning] : Field does not have the required fields");
            return;
        }

        if (!Scene.CurrentScene.RootNode.GetNode(field.Path, out var node))
        {
            Console.WriteLine("[Warning] : No nodes found at path " + field.Path);
            return;
        }

        Type? type = GetGlobalType(field.Class);
        if (type == null)
        {
            Console.WriteLine("[Warning] : No type found " + field.Class);
            return;
        }

        if (!node.GetComponent(type, out var component))
        {
            Console.WriteLine("[Warning] : No component found of type " + field.Class + " in node at " + field.Path);
            return;
        }

        var fieldInfo = type.GetField(field.Name);
        var propertyInfo = type.GetProperty(field.Name);
        if (fieldInfo == null && propertyInfo == null)
        {       
            Console.WriteLine("[Warning] : No field or property " + field.Name + " found in component " + field.Class);
            return;
        }

        var value = JsonHelper.Deserialize(field.Value, field.Type);
        try
        {
            fieldInfo?.SetValue(component, value);
            if (propertyInfo != null && propertyInfo.CanWrite)
                propertyInfo.SetValue(component, value);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] : Unable to set field because of reason: " + ex);
            return;
        }
    }

    private class FieldJson
    {
        public string? Path { get; set; }
        public string? Class { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Value { get; set; }
    }
}