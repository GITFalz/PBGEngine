namespace PBG.Data;

public static class Debug
{
    public static bool Enabled = true;
 
    public static void Log(object message) => Print("INFO", message, ConsoleColor.Cyan);
    public static void Warn(object message) => Print("WARN", message, ConsoleColor.Yellow);
    public static void Error(object message) => Print("ERROR", message, ConsoleColor.Red);
    public static void Success(object message) => Print("OK", message, ConsoleColor.Green);
    public static void Trace(object message) => Print("TRACE", message, ConsoleColor.DarkGray);
 

    public static void Dump(object obj, string? label = null)
    {
        if (!Enabled) return;
        var tag = label ?? obj?.GetType().Name ?? "null";
        Print("DUMP", tag, ConsoleColor.Yellow);
        if (obj == null) { Console.WriteLine("  (null)"); return; }
        foreach (var prop in obj.GetType().GetProperties())
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write($"  {prop.Name}: ");
                Console.ResetColor();
                Console.WriteLine(prop.GetValue(obj) ?? "(null)");
            }
            catch { /* skip unreadable props */ }
        }
    }
 
    public static void Section(string title = "")
    {
        if (!Enabled) return;
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"\n{"─── " + title + " ",-20}{"─".PadRight(40, '─')}");
        Console.ResetColor();
    }
 
    public static void Time(string label, Action action)
    {
        if (!Enabled) { action(); return; }
        var sw = System.Diagnostics.Stopwatch.StartNew();
        action();
        sw.Stop();
        Print("TIME", $"{label} → {sw.ElapsedMilliseconds} ms", ConsoleColor.Blue);
    }
  
    private static void Print(string level, object message, ConsoleColor color)
    {
        if (!Enabled) return;
 
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"[{DateTime.Now:HH:mm:ss.fff}] ");
 
        Console.BackgroundColor = color;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.Write($" {level,-5} ");
        Console.ResetColor();
 
        Console.ForegroundColor = color;
        Console.WriteLine($"  {message}");
        Console.ResetColor();
    }
}