namespace PBG;

public static class DebugLog
{
    public static void Write(string message)
    {
        Write("LOG", message, ConsoleColor.Gray);
    }

    public static void Info(string message)
    {
        Write("INFO", message, ConsoleColor.Cyan);
    }

    public static void Success(string message)
    {
        Write("OK", message, ConsoleColor.Green);
    }

    public static void Warning(string message)
    {
        Write("WARN", message, ConsoleColor.Yellow);
    }

    public static void Error(string message)
    {
        Write("ERROR", message, ConsoleColor.Red);
    }

    public static void Critical(string message)
    {
        Write("CRITICAL", message, ConsoleColor.DarkRed);
    }

    private static void Write(string type, string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write($"[{type}] ");
        Console.ResetColor();

        Console.WriteLine(message);
    }
}