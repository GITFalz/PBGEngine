namespace PBG.ThreadSync;

public sealed class ThreadLock
{
    private readonly object _lock = new();

    public IDisposable Lock()
    {
        Monitor.Enter(_lock);
        return new Scope(_lock);
    }

    private sealed class Scope(object lockObject) : IDisposable
    {
        public void Dispose()
        {
            Monitor.Exit(lockObject);
        }
    }
}