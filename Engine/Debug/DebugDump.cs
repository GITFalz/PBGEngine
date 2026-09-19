using System;
using System.Collections;
using System.IO;
using System.Text;

public static class DebugDump
{
    public static void ToFile<T>(IEnumerable<T> data, string fileName = "debug_dump.txt")
    {
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            fileName);

        var sb = new StringBuilder();

        int i = 0;
        foreach (var item in data)
        {
            //sb.Append($"[{i++}] ");
            DumpRecursive(item, sb, 0);
        }

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


    private static void DumpRecursive(object? value, StringBuilder sb, int depth)
    {
        if (value is null)
        {
            sb.AppendLine("null");
            return;
        }

        // Don't recurse into strings
        if (value is string)
        {
            sb.AppendLine(value.ToString());
            return;
        }

        // Handle any enumerable (arrays, List<T>, etc.)
        if (value is System.Collections.IEnumerable enumerable)
        {
            sb.AppendLine("[");

            int i = 0;
            foreach (var item in enumerable)
            {
                sb.Append(new string(' ', (depth + 1) * 4));

                DumpRecursive(item, sb, depth + 1);
            }

            sb.Append(new string(' ', depth * 4));
            sb.AppendLine("]");
            return;
        }

        sb.AppendLine(Dump(value));
    }
}