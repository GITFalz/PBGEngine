using System;
using System.Collections;
using System.IO;
using System.Text;

public static class DebugDump
{
    public static void ToFile<T>(IEnumerable<T> data, string fileName = "debug_dump.txt")
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), fileName);
        var sb = new StringBuilder();

        int i = 0;
        foreach (var item in data)
            sb.AppendLine($"[{i++}] {Dump(item)}");

        File.WriteAllText(path, sb.ToString());
        Console.WriteLine($"[DebugDump] Wrote {i} entries to {path}");
    }

    private static string Dump(object obj)
    {
        if (obj == null) return "null";

        var type = obj.GetType();
        if (type.IsPrimitive || obj is string || type.IsEnum)
            return obj.ToString();

        var sb = new StringBuilder();
        sb.Append(type.Name).Append(" { ");
        foreach (var field in type.GetFields())
            sb.Append($"{field.Name}={field.GetValue(obj)} ");
        foreach (var prop in type.GetProperties())
        {
            if (prop.GetIndexParameters().Length > 0) continue; // skip indexers
            try { sb.Append($"{prop.Name}={prop.GetValue(obj)} "); }
            catch { /* skip props that throw */ }
        }
        sb.Append('}');
        return sb.ToString();
    }
}