using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PBG.Compilation;

public static class ProcessManager
{
    public static Process? _gameProcess;
    public static bool PrintEngineConsole = true;

    private static readonly object _processLock = new();

    private static int _operationInProgress;

    private static PString _projectFolder = "/home".P() / "bjorn" / "Documents" / "Projects";
    private static PString _projectPath = _projectFolder / "PBGEngine" / "PBGEngine.csproj";
    private static PString _projectDir = _projectFolder / "PBGEngine";
    private static PString _projectExePath = _projectFolder / "PBGEngine" / "bin" / "Debug" / "net9.0" / "PBGEngine";

    public static void Build()
    {
        RunInBackground("Build", () => BuildProject(_projectPath, _projectDir));
    }

    public static void Run()
    {
        RunInBackground("Run", RunProjectIfNeeded);
    }

    public static void Reload()
    {
        RunInBackground("Reload", () =>
        {
            Close();
            RunProjectIfNeeded();
        });
    }

    public static void Stop()
    {
        RunInBackground("Close", () =>
        {
            Close();
        });
    }

    public static void Close()
    {
        Process? process;
        lock (_processLock)
            process = _gameProcess;

        if (process != null && !process.HasExited)
        {
            process.CloseMainWindow();

            if (!process.WaitForExit(1500))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(1000);
            }
        }

        lock (_processLock)
        {
            if (ReferenceEquals(_gameProcess, process) && process?.HasExited == true)
            {
                process.Dispose();
                _gameProcess = null;
            }
        }
    }

    private static void RunProjectIfNeeded()
    {
        lock (_processLock)
        {
            if (_gameProcess is { HasExited: false })
            {
                Console.WriteLine("The game is already running.");
                return;
            }
        }

        if (!BuildProject(_projectPath, _projectDir))
            return;

        RunProject(_projectPath, _projectDir, _projectExePath);
    }

    private static void RunInBackground(string operation, Action action)
    {
        if (Interlocked.CompareExchange(ref _operationInProgress, 1, 0) != 0)
        {
            Console.WriteLine($"{operation} ignored because another process operation is in progress.");
            return;
        }

        _ = Task.Run(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"{operation} failed: {exception}");
            }
            finally
            {
                Volatile.Write(ref _operationInProgress, 0);
            }
        });
    }

    public static bool BuildProject(string projectPath, string projectDir)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.ArgumentList.Add("build");
        psi.ArgumentList.Add(projectPath);
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("Debug");
        psi.ArgumentList.Add("-p:ExtraConstants=EXTERNAL%3BPIPES");
        psi.ArgumentList.Add("--nologo");

        using var build = Process.Start(psi);
        if (build == null)
            return false;

        var outputTask = build.StandardOutput.ReadToEndAsync();
        var errorTask = build.StandardError.ReadToEndAsync();

        build.WaitForExit();

        string output = outputTask.Result;
        string error = errorTask.Result;

        if (build.ExitCode != 0)
        {
            Console.WriteLine("=== BUILD FAILED ===");
            Console.WriteLine(error);
            Console.WriteLine(output);
            return false;
        }

        return true;
    }

    public static void RunProject(string projectPath, string projectDir, string playerExePath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = playerExePath,
            Arguments = $"\"{projectPath}\"",
            WorkingDirectory = projectDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        lock (_processLock)
        {
            if (_gameProcess is { HasExited: false })
            {
                Console.WriteLine("The game is already running.");
                return;
            }

            if (_gameProcess != null)
            {
                _gameProcess.WaitForExit();
                _gameProcess.Dispose();
                _gameProcess = null;
            }

            var process = Process.Start(startInfo);
            if (process == null)
                return;

            process.OutputDataReceived += (s, e) => { if (e.Data != null && PrintEngineConsole) Console.WriteLine("[ENGINE] : " + e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null && PrintEngineConsole) Console.WriteLine("[ENGINE] : " + e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _gameProcess = process;
        }
    }
}