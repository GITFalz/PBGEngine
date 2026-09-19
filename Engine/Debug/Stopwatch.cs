using System;
using System.Collections.Generic;
using System.Diagnostics;

public class TimingStopwatch
{
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private readonly List<(string Label, double Ms)> _points = new();
    private int _index = 0;

    /// <summary>
    /// Records the current elapsed time (from construction).
    /// </summary>
    public void Record(string? label = null)
    {
        string name = label ?? $"#{_index}";
        _points.Add((name, _sw.Elapsed.TotalMicroseconds));
        _index++;
    }

    /// <summary>
    /// Prints every recorded point in milliseconds and the total time.
    /// </summary>
    public void End()
    {
        Console.WriteLine("── Timing ─────────────────────");
        double previous = 0;

        foreach (var (label, ms) in _points)
        {
            double delta = ms - previous;
            Console.WriteLine($"{label,-20} {ms,6} µs   (+{delta} µs)");
            previous = ms;
        }

        Console.WriteLine($"{"TOTAL",-20} {_sw.Elapsed.TotalMicroseconds,6} µs");
        Console.WriteLine("───────────────────────────────");
    }
}