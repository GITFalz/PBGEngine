using PBG.Graphics;

namespace PBG.PBGConsole;

/*
public static class PBGConsole
{
    private static List<string> _actionNames = [];
    public static Dictionary<string, PBGConsoleModule> Commands = new Dictionary<string, PBGConsoleModule>
    {
        { "reload", new PBGConsoleCommandList(new Dictionary<string, PBGConsoleModule>
            {
                { "shader",  new PBGConsoleAction(HandleShaderReload) }
            })
        }
    };


    public static PBGConsoleCommandResult HandleCommand(string line)
    {
        var parameters = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return HandleCommand([.. parameters]);
    }

    private static PBGConsoleCommandResult HandleCommand(List<string> parameters)
    {
        if (parameters.Count == 0)
            return PBGConsoleCommandResult.Empty;

        var first = parameters[0];
        if (!Commands.TryGetValue(first, out var module))
            return PBGConsoleCommandResult.CommandNotFound;

        parameters.RemoveAt(0);
        return module.HandleCommand(parameters);
    }

    private static void HandleShaderReload(string line)
    {
        var parameters = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parameters.Length == 0)
            throw new Exception("No shader name provided");

        if (parameters.Length > 1)
            throw new Exception("To many parameters, only a shader name needed");

        if (Shader.Shaders.TryGetValue(parameters[0], out var shader))
        {
            GFX.DeviceWaitIdle();
            shader.RenewDescriptors();
        }
        else
        {
            throw new Exception($"No shader found called '{parameters[0]}'");
        }
    }
}

/*
public abstract class PBGConsoleModule
{
    public PBGConsoleCommandResult HandleCommand(List<string> parameters)
    {
        var first = parameters[0];
        if (!Commands.TryGetValue(first, out var module))
            return PBGConsoleCommandResult.CommandNotFound;
        
        return module.HandleCommand(line);
    }
}

public class PBGConsoleCommandList(Dictionary<string, PBGConsoleModule> commands) : PBGConsoleModule
{
    public Dictionary<string, PBGConsoleModule> Commands = commands;

    public PBGConsoleCommandResult HandleCommand(List<string> parameters)
    {
        var first = parameters[0];
        if (!Commands.TryGetValue(first, out var module))
            return PBGConsoleCommandResult.CommandNotFound;
        
        return module.HandleCommand(line);
    }
}

public class PBGConsoleAction(Action<string> action) : PBGConsoleModule
{
    public Action<string> Action = action;
}

public enum PBGConsoleCommandResult
{
    Success,
    Empty,
    CommandNotFound
}
*/