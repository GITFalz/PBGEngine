using System.Diagnostics;

namespace PBG.Diagnostic;

public class MsTimer : IDisposable
{
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private readonly Action<double>? _action = null;

    public MsTimer() {}
    public MsTimer(Action<double> action) { _action = action; }

    public void Dispose()
    {
        _action?.Invoke(_sw.Elapsed.TotalMilliseconds);
        _sw.Stop();
        GC.SuppressFinalize(this);
    }
}

public class SecondTimer : IDisposable
{
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private readonly Action<double>? _action = null;

    public SecondTimer() {}
    public SecondTimer(Action<double> action) { _action = action; }

    public void Dispose()
    {
        _action?.Invoke(_sw.Elapsed.TotalSeconds);
        _sw.Stop();
        GC.SuppressFinalize(this);
    }
}