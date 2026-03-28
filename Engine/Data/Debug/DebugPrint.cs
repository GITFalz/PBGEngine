namespace PBG.Data;

public static class Debug
{
    public static void Print(object? obj)
    {
        Console.WriteLine(ToString(obj));
    }

    private static string ToString(object? obj)
    {
        if (obj == null)
            return "null";

        if (obj is string s)
            return s;

        if (obj is System.Collections.IEnumerable enumerable && obj is not string)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("[");

            bool first = true;
            foreach (var item in enumerable)
            {
                if (!first)
                    sb.Append(", ");

                sb.Append(ToString(item));
                first = false;
            }

            sb.Append("]");
            return sb.ToString();
        }

        return obj.ToString() ?? "null";
    }
}