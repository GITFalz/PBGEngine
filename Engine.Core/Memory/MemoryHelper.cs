using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

public unsafe static class MemoryHelper
{
#if DEBUG
    private sealed record AllocInfo(long Id, string Type, long Bytes, StackTrace Trace);

    private static readonly ConcurrentDictionary<nint, AllocInfo> _allocs = new();
    private static long _nextId;

    private static void Track(void* ptr, string type, long bytes)
    {
        if (ptr == null) return;
        _allocs[(nint)ptr] = new AllocInfo(
            Interlocked.Increment(ref _nextId),
            type,
            bytes,
            new StackTrace(skipFrames: 2, fNeedFileInfo: true)); // skip Track + the Alloc method
    }

    private static void Untrack(void* ptr)
    {
        if (ptr == null) return; // freeing null is legal
        if (!_allocs.TryRemove((nint)ptr, out _))
        {
            Console.WriteLine($"[MEM] Free of untracked pointer 0x{(nint)ptr:X} (double free, or not allocated by MemoryHelper)\n{new StackTrace(2, true)}");
        }
    }
#endif

    /// <summary>Returns a marker you can pass to ReportLeaks to only look at allocations made after this point.</summary>
    public static long Mark()
    {
#if DEBUG
        return Interlocked.Read(ref _nextId);
#else
        return 0;
#endif
    }

    /// <summary>Prints every allocation that was never freed (optionally only those made after 'sinceMark'). Returns the leaked count.</summary>
    public static int ReportLeaks(long sinceMark = 0)
    {
#if DEBUG
        var leaks = _allocs.Values.Where(a => a.Id > sinceMark).ToList();

        if (leaks.Count == 0)
        {
            Console.WriteLine("[MEM] No leaked native allocations.");
            return 0;
        }

        Console.WriteLine($"[MEM] {leaks.Count} leaked allocation(s), {leaks.Sum(l => l.Bytes)} bytes total:");

        // Group by creation site so a leaking loop prints once with a count
        var groups = leaks.GroupBy(l => l.Type + "\n" + l.Trace);
        foreach (var g in groups.OrderByDescending(g => g.Sum(x => x.Bytes)))
        {
            var first = g.First();
            var sb = new StringBuilder();
            sb.AppendLine($"[MEM] {g.Count()}x {first.Type}, {g.Sum(x => x.Bytes)} bytes, allocated at:");
            sb.Append(first.Trace);
            Console.WriteLine(sb);
        }

        return leaks.Count;
#else
        return 0;
#endif
    }

    public static T* Alloc<T>(int count) where T : unmanaged
    {
        var ptr = (T*)NativeMemory.Alloc((nuint)(sizeof(T) * count));
#if DEBUG
        Track(ptr, typeof(T).Name, (long)sizeof(T) * count);
#endif
        return ptr;
    }

    public static T* AllocClear<T>(int count) where T : unmanaged
    {
        var map = (T*)NativeMemory.Alloc((nuint)(sizeof(T) * count));
        Clear(map, count);
#if DEBUG
        Track(map, typeof(T).Name, (long)sizeof(T) * count);
#endif
        return map;
    }

    public static T** AllocPtr<T>(int count) where T : unmanaged
    {
        var ptr = (T**)NativeMemory.Alloc((nuint)(sizeof(T*) * count));
#if DEBUG
        Track(ptr, typeof(T).Name + "*", (long)sizeof(T*) * count);
#endif
        return ptr;
    }

    public static void Clear<T>(T* ptr, int elementCount) where T : unmanaged
    {
        NativeMemory.Clear(ptr, (nuint)(sizeof(T) * elementCount));
    }

    public static void Clear<T>(T** ptr, int elementCount) where T : unmanaged
    {
        NativeMemory.Clear(ptr, (nuint)(sizeof(T*) * elementCount));
    }

    public static void Free<T>(T* ptr) where T : unmanaged
    {
#if DEBUG
        Untrack(ptr);
#endif
        NativeMemory.Free(ptr);
    }

    public static void Free<T>(T** ptr) where T : unmanaged
    {
#if DEBUG
        Untrack(ptr);
#endif
        NativeMemory.Free(ptr);
    }

    public static void Free<T>(ref T* ptr) where T : unmanaged
    {
#if DEBUG
        Untrack(ptr);
#endif
        NativeMemory.Free(ptr);
        ptr = null;
    }
}