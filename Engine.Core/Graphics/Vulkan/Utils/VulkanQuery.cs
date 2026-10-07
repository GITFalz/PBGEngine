using Silk.NET.Vulkan;

namespace PBG.Graphics.Vulkan;

public unsafe class VulkanQuery : IDisposable
{
    private VulkanDevice _vulkanDevice;

    private QueryPool TimestampPool;

    public VulkanQuery(VulkanDevice vulkanDevice)
    {
        _vulkanDevice = vulkanDevice;

        CreateQueryPool();
    }

    private void CreateQueryPool()
    {
        QueryPoolCreateInfo queryPoolInfo = new()
        {
            SType = StructureType.QueryPoolCreateInfo,
            QueryType = QueryType.Timestamp,
            QueryCount = 2
        };

        var result = _vulkanDevice.Vk.CreateQueryPool(_vulkanDevice.Device, &queryPoolInfo, null, out TimestampPool);
        if (result != Result.Success)
            throw new Exception("Failed to create query pool with result: " + result);
    }

    public void ResetQueryPool(CommandBuffer cmd)
    {
        _vulkanDevice.Vk.CmdResetQueryPool(cmd, TimestampPool, 0, 2);
    }

    public void WriteTimestamp(CommandBuffer cmd, PipelineStageFlags stageFlags, uint query)
    {
        _vulkanDevice.Vk.CmdWriteTimestamp(cmd, stageFlags, TimestampPool, query);
    }

    public void ReadTimestamp(out ulong start, out ulong end)
    {
        ulong[] timestamps = new ulong[2];
        _vulkanDevice.Vk.GetQueryPoolResults(_vulkanDevice.Device, TimestampPool, 0, 2, (nuint)(timestamps.Length * sizeof(ulong)), ref timestamps[0], sizeof(ulong), QueryResultFlags.Result64Bit);
        start = timestamps[0];
        end = timestamps[1];
    }

    public double GetTimestampMs()
    {
        PhysicalDeviceProperties properties;
        _vulkanDevice.Vk.GetPhysicalDeviceProperties(_vulkanDevice.PhysicalDevice, &properties);

        ReadTimestamp(out var start, out var end);

        double nanoseconds = (end - start) * properties.Limits.TimestampPeriod;
        double milliseconds = nanoseconds / 1_000_000.0;

        return milliseconds;
    }

    public void Dispose()
    {
        _vulkanDevice.Vk.DestroyQueryPool(_vulkanDevice.Device, TimestampPool, null);

        GC.SuppressFinalize(this);
    }
}