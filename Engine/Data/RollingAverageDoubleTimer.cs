using System.Diagnostics;

namespace PBG.Data;

public class RollingAverageDoubleTimer
{
    private readonly int SampleCount = 100;

    private readonly double[] _samples;
    private double _sum;
    private int _index;
    private int _count;
    private Stopwatch _stopwatch = Stopwatch.StartNew();

    private readonly object _lock = new();

    public RollingAverageDoubleTimer() : this(100) {}
    public RollingAverageDoubleTimer(int sampleCount)
    {
        SampleCount = sampleCount;
        _samples = new double[SampleCount];
    }

    public void Start()
    {
        _stopwatch = Stopwatch.StartNew();
    }

    public void End()
    {
        if (_stopwatch == null)
            throw new InvalidOperationException("Timer was not started");

        _stopwatch.Stop();
        AddSample(_stopwatch.Elapsed.TotalMilliseconds);
    }

    public void AddSample(double value)
    {
        lock (_lock)
        {
            int slot = _index % SampleCount;
            double old = _samples[slot];
            _samples[slot] = value;
            _sum += value - old;

            _index++;
            if (_count < SampleCount)
                _count++;
        }
    }

    public double GetAverage()
    {
        lock (_lock)
        {
            if (_count == 0) return 0.0;
            return _sum / _count;
        }
    }

    public double GetLast()
    {
        return _stopwatch?.Elapsed.TotalMilliseconds ?? 0.0;
    }
}