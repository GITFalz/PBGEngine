using System.Diagnostics;
using PBG.Data;
using PBG.Graphics;
using Silk.NET.Vulkan;

namespace PBG.NewVoxel;

public class AsyncVoxelGenerationBatch : IDisposable
{
    public ComputeShader WorldShader;
    public int ChunkPositionLocation;
    public ChunkSlot[] slots = [];

    private ulong _currentTimelineValue = 0;
    private Queue<ChunkJob> _pendingReadbacks = [];

    public AsyncVoxelGenerationBatch(int slotCount, ComputeShader computeShader)
    {
        WorldShader = computeShader;
        ChunkPositionLocation = computeShader.GetLocation("ubo.uChunkWorldPosition");

        slots = new ChunkSlot[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            ChunkSlot slot = new(computeShader);
            slots[i] = slot;
        }
    }
    
    public void Update()
    {
        _currentTimelineValue = GFX.GetTimelineValue();
    }

    public ChunkSlot? TryGetAvailableSlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot.LastSubmittedValue <= _currentTimelineValue && slot.ReadBack)
            {
                slot.ReadBack = false;
                return slot;
            }
        }
        return null;
    }

    public void SubmitBatch(ChunkJob job)
    {
        var signalValue = GFX.AllocateTimelineValue();
        var cmd = GFX.BeginSingleTimeComputeCommands();

        WorldShader.Bind(cmd);
        job.Slot.Descriptor.Bind(cmd, PipelineBindPoint.Compute);
        job.Slot.Descriptor.Uniform(ChunkPositionLocation, job.Chunk.WorldPosition);

        GFX.ResetQueryPool(cmd);
        GFX.WriteTimestamp(cmd, PipelineStageFlags.ComputeShaderBit, 0);
        
        WorldShader.DispatchBarrier(cmd, job.Slot.Descriptor, 8, 8, 8);

        GFX.WriteTimestamp(cmd, PipelineStageFlags.ComputeShaderBit, 1);

        GFX.EndCommandBuffer(cmd);
        GFX.SubmitToComputeQueue(cmd, signalValue);
        
        job.Slot.LastSubmittedValue = signalValue; 

        _pendingReadbacks.Enqueue(job);
    }

    public unsafe bool TryReadSlot(ChunkJob job)
    {
        Stopwatch sw = Stopwatch.StartNew();
        var slot = job.Slot;
        if (_currentTimelineValue >= slot.LastSubmittedValue)
        {   
            uint count = slot.Count.ReadBack(1)[0];
            slot.Count.Update([0]);

            if (count > 0)
            {
                slot.Blocks.Map(job.Chunk.Blocks, VoxelChunk.BLOCK_COUNT, (int)count);
            }

            //ChunkGenTimer.AddSample(GFX.GetTimestampMs());
            //ChunkReadTimer.AddSample(sw.Elapsed.TotalMilliseconds);

            //Console.WriteLine("chunk gen ms: " + GFX.GetTimestampMs() + " read back took " + sw.Elapsed.TotalMilliseconds);
            
            slot.ReadBack = true;
            return true;
        }
        return false;
    }

    public void HandleCompletions()
    {
        while (_pendingReadbacks.TryPeek(out var pendingJob))
        {
            if (!TryReadSlot(pendingJob))
                break;

            _pendingReadbacks.Dequeue();

            if (pendingJob.Chunk.Status != ChunkStatus.Canceled)
            {
                pendingJob.Chunk.Status = ChunkStatus.Generated;
                pendingJob.Chunk.TryEnqueueRendering();
            }
        }
    }

    public void Dispose()
    {
        GFX.ComputeQueueWaitIdle();

        foreach (var slot in slots)
            slot.Dispose();

        GC.SuppressFinalize(this);
    }
}

public class ChunkSlot : IDisposable
{
    public SSBO<Block> Blocks;
    public SSBO<uint> Count;
    public Descriptor Descriptor;
    public ulong LastSubmittedValue;
    public bool ReadBack = true;

    public ChunkSlot(ComputeShader computeShader)
    {
        Blocks = new(VoxelChunk.BLOCK_COUNT, true);
        Count = new(1, true);

        Descriptor = computeShader.GetDescriptorSet();
        Descriptor.BindSSBO(Blocks, 0);
        Descriptor.BindSSBO(Count, 1);
    }

    public void Dispose()
    {
        Blocks.Dispose();
        Count.Dispose();
        Descriptor.Dispose();

        GC.SuppressFinalize(this);
    }
}

public struct ChunkJob
{
    public VoxelChunk Chunk;
    public ChunkSlot Slot;
}