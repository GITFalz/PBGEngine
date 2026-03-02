using System.Diagnostics;

namespace PBG.Data;

public class RollingAverageTimer
{
    private const int SampleCount = 100;
    private readonly long[] _samples = new long[SampleCount];
    private long _sum;
    private int _index;
    private int _count;
    private Stopwatch _stopwatch;

    private object _lock = new();

    public void Start()
    {
        _stopwatch = Stopwatch.StartNew();
    }

    public void End()
    {
        if (_stopwatch == null)
            throw new InvalidOperationException("Timer was not started");

        _stopwatch.Stop();
        AddSample(_stopwatch.Elapsed.Milliseconds);
    }

    private void AddSample(long ms)
    {
        lock(_lock)
        {
            int i = Interlocked.Increment(ref _index) - 1;
            int slot = i % SampleCount;
            long old = Interlocked.Exchange(ref _samples[slot], ms);
            Interlocked.Add(ref _sum, ms - old);
            
            if (_count < SampleCount)
                Interlocked.Increment(ref _count);
        }
        
    }

    public float GetAverageMs()
    {
        lock(_lock)
        {
            int count = Volatile.Read(ref _count);
            if (count == 0) return 0f;
            
            long sum = Volatile.Read(ref _sum);
            return (float)sum / count;
        }
    }

    public long GetLastMs()
    {
        return _stopwatch?.ElapsedMilliseconds ?? 0;
    }
}