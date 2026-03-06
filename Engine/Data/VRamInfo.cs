using System.Runtime.InteropServices;
using PBG.Graphics;
using Silk.NET.Vulkan;

public unsafe static class VRAMInfo
{
    private static bool _hasMemoryBudget;
    private static bool _started = false;
    private static PhysicalDevice _physicalDevice;

    public static void Initialize()
    {
        if (_started)
            return;

        _started = true;
        _physicalDevice = GraphicsContext.graphicsContext.physicalDevice;

        // check if extension is supported
        uint extCount;
        GFX.Vk.EnumerateDeviceExtensionProperties(_physicalDevice, (byte*)null, &extCount, null);
        var extensions = new ExtensionProperties[extCount];
        fixed (ExtensionProperties* pExt = extensions)
            GFX.Vk.EnumerateDeviceExtensionProperties(_physicalDevice, (byte*)null, &extCount, pExt);

        _hasMemoryBudget = extensions.Any(e => Marshal.PtrToStringAnsi((nint)e.ExtensionName) == "VK_EXT_memory_budget");
    }

    public static (long total, long used, long budget) GetVRAMInfo()
    {
        if (_hasMemoryBudget)
            return GetVRAMWithBudget();
        else
            return GetVRAMBasic();
    }

    private static unsafe (long total, long used, long budget) GetVRAMWithBudget()
    {
        var budgetProperties = new PhysicalDeviceMemoryBudgetPropertiesEXT
        {
            SType = StructureType.PhysicalDeviceMemoryBudgetPropertiesExt
        };

        var memProperties = new PhysicalDeviceMemoryProperties2
        {
            SType = StructureType.PhysicalDeviceMemoryProperties2,
            PNext = &budgetProperties
        };

        GFX.Vk.GetPhysicalDeviceMemoryProperties2(_physicalDevice, &memProperties);

        long totalBudget = 0;
        long totalUsage  = 0;
        long totalHeap   = 0;

        var props = memProperties.MemoryProperties;
        for (int i = 0; i < props.MemoryHeapCount; i++)
        {
            // only count device local heaps (actual VRAM)
            if (props.MemoryHeaps[i].Flags.HasFlag(MemoryHeapFlags.DeviceLocalBit))
            {
                totalHeap   += (long)props.MemoryHeaps[i].Size;
                totalBudget += (long)budgetProperties.HeapBudget[i];
                totalUsage  += (long)budgetProperties.HeapUsage[i];
            }
        }

        return (totalHeap, totalUsage, totalBudget);
    }

    private static unsafe (long total, long used, long budget) GetVRAMBasic()
    {
        // fallback without budget extension — only total is available
        PhysicalDeviceMemoryProperties memProperties;
        GFX.Vk.GetPhysicalDeviceMemoryProperties(_physicalDevice, &memProperties);

        long total = 0;
        for (int i = 0; i < memProperties.MemoryHeapCount; i++)
        {
            if (memProperties.MemoryHeaps[i].Flags.HasFlag(MemoryHeapFlags.DeviceLocalBit))
                total += (long)memProperties.MemoryHeaps[i].Size;
        }

        return (total, -1, -1); // usage not available without extension
    }

    public static long GetTotalVRAM()  => GetVRAMInfo().total;
    public static long GetUsedVRAM()   => GetVRAMInfo().used;
    public static long GetBudget()     => GetVRAMInfo().budget;
    public static long GetFreeVRAM()
    {
        var (_, used, budget) = GetVRAMInfo();
        return budget > 0 && used > 0 ? budget - used : -1;
    }

    public static float GetVRAMUsagePercentage()
    {
        var (total, used, _) = GetVRAMInfo();
        if (total > 0 && used > 0)
            return (float)used / total * 100f;
        return -1f;
    }

    public static bool IsVRAMCriticallyLow(long thresholdMB = 100)
    {
        long free = GetFreeVRAM();
        if (free > 0)
            return free / (1024 * 1024) < thresholdMB;
        return false;
    }

    public static void PrintVRAMInfo()
    {
        var (total, used, budget) = GetVRAMInfo();

        Console.WriteLine("=== VRAM Information ===");
        Console.WriteLine($"Memory budget extension: {(_hasMemoryBudget ? "YES" : "NO")}");

        if (total > 0)
            Console.WriteLine($"Total VRAM: {FormatBytes(total)}");
        if (used > 0)
            Console.WriteLine($"Used VRAM:  {FormatBytes(used)}");
        if (budget > 0)
            Console.WriteLine($"Budget:     {FormatBytes(budget)}");

        float pct = GetVRAMUsagePercentage();
        if (pct >= 0)
            Console.WriteLine($"Usage: {pct:F1}%");
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1) { number /= 1024; counter++; }
        return $"{number:n1} {suffixes[counter]}";
    }
}