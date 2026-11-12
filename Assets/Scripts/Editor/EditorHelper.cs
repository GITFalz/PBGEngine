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

            json.Serialize("name", type.Name);
            json.SerializeList(fields, "fields", (json, f) =>
            {
                json.Serialize("name", f.Name);
                json.Serialize("type", f.FieldType.Name);

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
}