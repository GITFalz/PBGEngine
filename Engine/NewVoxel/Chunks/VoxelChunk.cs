using System.Diagnostics;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PBG.Core;
using PBG.MathLibrary;


namespace PBG.NewVoxel;

[InternalSystemInit(InitPriority.Data)]
[InternalSystemCleanup]
public unsafe class VoxelChunk : IDisposable
{
    public static Stopwatch sw = Stopwatch.StartNew();
    public static HashSet<VoxelChunk> AliveChunks = [];

    public static Block** EmptyMap { get; private set; }
    
    public const int BLOCK_COUNT = 32768;
    public const int SOLID_COUNT = 1024;
    public const int MAP_ELEMENTS_COUNT = 32 * 2 * 6; // 12x 32 uint arrays, 2 for every side, 1 for occlusion, 1 for ao

    public static VoxelChunk Empty = null!;
    public VoxelRenderer Renderer;

    public Allocation Allocation;
    public List<Allocation> Allocations = [];

    public Vector3i RelativePosition;
    public Vector3i WorldPosition;
    public Vector3i Center;
    public Matrix4 ModelMatrix; 

    public Block* Blocks { get; private set; } = null;
    public byte* ByteBlocks { get; private set; } = null;

    public Block** BlockMap { get; private set; }
    public byte* CountMap { get; private set; }

    public uint* SolidMap { get; private set; }

    private uint* _mapPtr;

    public uint* FrontOcclusion { get; private set; }
    public uint* RightOcclusion { get; private set; }
    public uint* TopOcclusion { get; private set; }
    public uint* LeftOcclusion { get; private set; }
    public uint* BottomOcclusion { get; private set; }
    public uint* BackOcclusion { get; private set; }

    public uint* FrontAoType { get; private set; }
    public uint* RightAoType { get; private set; }
    public uint* TopAoType { get; private set; }
    public uint* LeftAoType { get; private set; }
    public uint* BottomAoType { get; private set; }
    public uint* BackAoType { get; private set; }

    private int _status;

    /*
    public ChunkStatus Status
    {
        get => (ChunkStatus)Volatile.Read(ref _status);
        set => Volatile.Write(ref _status, (int)value);
    }
    */

    public ChunkStatus Status
    {
        get => (ChunkStatus)Volatile.Read(ref _status);
        set
        {
            var old = (ChunkStatus)_status;
            Volatile.Write(ref _status, (int)value);
            //AddStatusChange((sw.Elapsed.TotalMilliseconds, $"{old} - {value}"));
        }
    }


    private ulong _generation = 0;
    public ulong Gen => Volatile.Read(ref _generation);
    public ulong NewGen() => Interlocked.Increment(ref _generation);





    public List<(double, string)> StatusChanges = [];

    public void AddStatusChange((double, string) v)
    {
        //StatusChanges.Add(v);
    }

    public bool HasBlocks = false;

    private VoxelChunk()
    {
        AllocateMemory();
        ClearMemory();
    }

    public VoxelChunk(VoxelRenderer renderer, Vector3i relativePosition)
    {
        Allocation = new() { Chunk = this };

        Renderer = renderer;

        SetPosition(relativePosition);

        AllocateMemory();
        ClearCountMap();

        AliveChunks.Add(this);
    }


    public static void Init()
    {  
        Console.WriteLine("Start");
        EmptyMap = MemoryHelper.AllocPtr<Block>(1024);
        for (int i = 0; i < 1024; i++)
        {
            EmptyMap[i] = MemoryHelper.Alloc<Block>(32);
            MemoryHelper.Clear(EmptyMap[i], 32);
        }   
        Console.WriteLine("Middle");

        Empty = new();
        Console.WriteLine("End");
    }

    public void SetPosition(Vector3i relativePosition)
    {
        RelativePosition = relativePosition;
        WorldPosition = relativePosition * 32;
        Center = WorldPosition + (16, 16, 16);
        ModelMatrix = Matrix4.CreateTranslation(WorldPosition);
    }


    // Bytes
    public Block this[int x, int y, int z]
    {
        get => Get(x, y, z);
        set => Set(x, y, z, value);
    }
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(Vector3i position, Block block) => Set(position.X, position.Y, position.Z, block);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int x, int y, int z, Block block) => Set(GetIndex(x, y, z), block);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Block Get(Vector3i position) => Get(position.X, position.Y, position.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Block Get(int x, int y, int z) => Get(GetIndex(x, y, z));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetIndex(Vector3i position) => GetIndex(position.X, position.Y, position.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetIndex(int x, int y, int z) => (y & 31) + (x & 31) * 32 + (z & 31) * 1024;



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, Block block)
    {
        ByteBlocks[index] = (byte)(block.blockData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Block Get(int index)
    {
        return new(ByteBlocks[index]);
    }

    /*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, Block block) => Blocks[index] = block;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Block Get(int index) => Blocks[index];
    */

    /*  X aligned
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, Block block) => Blocks[index] = block;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Block Get(int index) => Blocks[index];
    */


    /*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, Block block)
    {
        int x = index & 31;
        int zy = index >> 5;

        var row = BlockMap[zy];
        var count = CountMap[zy];
        var old = row[x];

        if (block.blockData == old.blockData)
            return; // no-op, nothing changed

        if (block.blockData == 0)
        {
            // old was non-air, new is air -> removing a block
            row[x] = block;
            count--;
            CountMap[zy] = count;

            if (count == 0)
            {
                MemoryHelper.Free(row);
                BlockMap[zy] = EmptyMap[zy];
            }
        }
        else if (old.blockData == 0)
        {
            // old was air, new is non-air -> adding a block
            if (count == 0)
            {
                row = MemoryHelper.Alloc<Block>(32);
                BlockMap[zy] = row;
            }

            row[x] = block;
            count++;
            CountMap[zy] = count;
        }
        else
        {
            // overwriting one non-air block with another
            row[x] = block;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Block Get(int index) => BlockMap[index >> 5][index & 31];
    */


    public bool InBounds(Vector3i pos) => InBounds(pos.X, pos.Y, pos.Z);
    public bool InBounds(int x, int y, int z)
    {
        int lx = x - WorldPosition.X;
        int ly = y - WorldPosition.Y;
        int lz = z - WorldPosition.Z;

        return (uint)lx < 32u && (uint)ly < 32u && (uint)lz < 32u;
    }

    public bool HasAllNeighbours()
    {
        AddStatusChange((sw.Elapsed.TotalMilliseconds, $"check all neighbours"));
        int yStart = RelativePosition.Y <= 0 ? 0 : -1;
        int yEnd = RelativePosition.Y >= Renderer.WorldHeight - 1 ? 0 : 1;

        int i = 0;
        
        for (int x = -1; x <= 1; x++)
        for (int y = yStart; y <= yEnd; y++)
        for (int z = -1; z <= 1; z++)
        {
            if (x == 0 && y == 0 && z == 0)
                continue;

            if (Renderer.GetChunk(RelativePosition + (x, y, z), out var neighborChunk))
            {
                if (neighborChunk.Status < ChunkStatus.Generated)
                {
                    AddStatusChange((sw.Elapsed.TotalMilliseconds, $"neighbour {i} not generated: {neighborChunk.RelativePosition}"));
                    return false;
                }
            }
            else
            {
                AddStatusChange((sw.Elapsed.TotalMilliseconds, $"neighbour {i} not exist: {RelativePosition + (x, y, z)}"));
                return false;
            }
            i++;
        }

        AddStatusChange((sw.Elapsed.TotalMilliseconds, $"success"));
        return true;
    }


    public bool HasAllAndRenderNeighbours()
    {
        bool hasAllNeighbours = true;

        int yStart = RelativePosition.Y <= 0 ? 0 : -1;
        int yEnd = RelativePosition.Y >= Renderer.WorldHeight - 1 ? 0 : 1;

        int i = 0;
        
        for (int x = -1; x <= 1; x++)
        for (int y = yStart; y <= yEnd; y++)
        for (int z = -1; z <= 1; z++)
        {
            if (x == 0 && y == 0 && z == 0)
                continue;

            if (Renderer.GetChunk(RelativePosition + (x, y, z), out var neighborChunk))
            {
                if (neighborChunk.Status < ChunkStatus.Generated)
                {
                    AddStatusChange((sw.Elapsed.TotalMilliseconds, $"{neighborChunk.RelativePosition} not generated"));
                    Status = ChunkStatus.FailedToMesh;
                    hasAllNeighbours = false;
                }
                else if (neighborChunk.Status < ChunkStatus.QueuedToMesh)
                {
                    if (neighborChunk.HasAllNeighbours())
                        neighborChunk.EnqueueRendering();
                }
            }
            else
            {
                AddStatusChange((sw.Elapsed.TotalMilliseconds, $"{RelativePosition + (x, y, z)} doesn't exist"));
                Status = ChunkStatus.FailedToMesh;
                hasAllNeighbours = false;
            }
            i++;
        }

        return hasAllNeighbours;
    }

    public List<RenderingInfo> RenderingHistory = [];

    private object _chunkLock = new();
    private uint _generatedFlag = 0;

    public uint SetChunkGeneratedFlag(int index)
    {
        lock (_chunkLock)
        {
            _generatedFlag |= 1u << index;
            return _generatedFlag;
        }
    }

    public uint RemoveChunkGeneratedFlag(int index)
    {
        lock (_chunkLock)
        {
            _generatedFlag &= ~(1u << index);
            return _generatedFlag;
        }
    }

    public uint GetChunkGeneratedFlag(int index)
    {
        lock (_chunkLock)
        {
            return (_generatedFlag >> index) & 1u;
        }
    }

    public void TryEnqueueRendering()
    {
        bool hasAllNeighbours = true;

        int yStart = RelativePosition.Y <= 0 ? 0 : -1;
        int yEnd = RelativePosition.Y >= Renderer.WorldHeight - 1 ? 0 : 1;

        for (int x = -1; x <= 1; x++)
        for (int y = yStart; y <= yEnd; y++)
        for (int z = -1; z <= 1; z++)
        {
            if (x == 0 && y == 0 && z == 0)
                continue;

            if (Renderer.GetChunk(RelativePosition + (x, y, z), out var neighborChunk))
            {
                if (neighborChunk.Status == ChunkStatus.FailedToMesh)
                {   
                    neighborChunk.Status = ChunkStatus.QueuedToMesh;
                    neighborChunk.EnqueueRendering();
                }
            }
            else
            {
                AddStatusChange((sw.Elapsed.TotalMilliseconds, $"{RelativePosition + (x, y, z)} doesn't exist try enqueue"));
                Status = ChunkStatus.FailedToMesh;
                hasAllNeighbours = false;
            }
        }

        if (HasBlocks && hasAllNeighbours)
        {
            EnqueueRendering();
        }

    }

    public struct RenderingInfo
    {
        public string Text = "";
        public double Time = sw.Elapsed.TotalMicroseconds;
        public Vector3i WorldPosition;
        public ChunkStatus Status;
        public RenderingInfo() {}
    }


    private void AllocateMemory()
    {
        if (Blocks == null && false)
            Blocks = MemoryHelper.Alloc<Block>(BLOCK_COUNT);

        if (ByteBlocks == null)
            ByteBlocks = MemoryHelper.AllocClear<byte>(BLOCK_COUNT);

        if (BlockMap == null)
        {
            BlockMap = MemoryHelper.AllocPtr<Block>(1024);

            for (int i = 0; i < 1024; i++) 
            {
                BlockMap[i] = EmptyMap[i];
            }
        }

        if (CountMap == null)
            CountMap = MemoryHelper.Alloc<byte>(1024);

        if (SolidMap == null)
            SolidMap = MemoryHelper.Alloc<uint>(SOLID_COUNT);

        if (_mapPtr == null)
            _mapPtr = MemoryHelper.Alloc<uint>(MAP_ELEMENTS_COUNT); 

        FrontOcclusion =    _mapPtr + 32 * 0;
        RightOcclusion =    _mapPtr + 32 * 1;
        TopOcclusion =      _mapPtr + 32 * 2;
        LeftOcclusion =     _mapPtr + 32 * 3;
        BottomOcclusion =   _mapPtr + 32 * 4;
        BackOcclusion =     _mapPtr + 32 * 5;

        FrontAoType =       _mapPtr + 32 * 6;
        RightAoType =       _mapPtr + 32 * 7;
        TopAoType =         _mapPtr + 32 * 8;
        LeftAoType =        _mapPtr + 32 * 9;
        BottomAoType =      _mapPtr + 32 * 10;
        BackAoType =        _mapPtr + 32 * 11;
    }

    public void ClearMemory()
    {
        ClearBlocks();
        ClearByteBlocks();
        ClearBlockMap();
        ClearCountMap();
        ClearSolidMap();
        ClearMaps();
    }

    public void ClearBlocks()
    {
        if (Blocks != null)
            MemoryHelper.Clear(Blocks, BLOCK_COUNT);
    }

    public void ClearByteBlocks()
    {
        if (ByteBlocks != null)
            MemoryHelper.Clear(ByteBlocks, BLOCK_COUNT);
    }

    public void ClearBlockMap()
    {
        
    }    
    public void ClearCountMap() => MemoryHelper.Clear(CountMap, 1024);
    public void ClearSolidMap() => MemoryHelper.Clear(SolidMap, SOLID_COUNT);
    public void ClearMaps() => MemoryHelper.Clear(_mapPtr, MAP_ELEMENTS_COUNT);

    public void EnqueueRendering()
    {
        Renderer.EnqueueRendering(this);
    }


    public float DistanceSquaredTo(Vector3 position) => Vector3.DistanceSquared(WorldPosition, position);

    public void FreeAllocation()
    {
        Allocation.Free();
        for (int i = 0; i < Allocations.Count; i++)
        {
            Allocations[i].Free();
        }
        Allocations = [];
    }

    public void Dispose()
    {
        FreeAllocation();

        AliveChunks.Remove(this);

        if (Blocks != null)
        {
            MemoryHelper.Free(Blocks);
            Blocks = null;
        }

        if (ByteBlocks != null)
        {
            MemoryHelper.Free(ByteBlocks);
            ByteBlocks = null;
        }

        if (BlockMap != null)
        {
            
        }

        if (SolidMap != null)
        {
            MemoryHelper.Free(SolidMap);
            SolidMap = null;
        }

        if (_mapPtr != null)
        {
            MemoryHelper.Free(_mapPtr);
            _mapPtr = null;

            FrontOcclusion = null;
            RightOcclusion = null;
            TopOcclusion = null;
            LeftOcclusion = null;
            BottomOcclusion = null;
            BackOcclusion = null;

            FrontAoType = null;
            RightAoType = null;
            TopAoType = null;
            LeftAoType = null;
            BottomAoType = null;
            BackAoType = null;
        }

        GC.SuppressFinalize(this);
    }


    public static void Cleanup()
    {
        Empty.Dispose();

        for (int i = 0; i < 1024; i++)
        {
            MemoryHelper.Free(EmptyMap[i]);
        }
        MemoryHelper.Free(EmptyMap);
    }
}

public enum ChunkStatus
{
    Canceled = -2,
    Deleted = -1,
    Empty = 0,
    QueuedToGenerate = 1,
    Generating = 2,
    Generated = 3,
    FailedToMesh = 4,
    QueuedToMesh = 5,
    Meshing = 6,
    QueuedToUpload = 7,
    Rendered = 8,
}