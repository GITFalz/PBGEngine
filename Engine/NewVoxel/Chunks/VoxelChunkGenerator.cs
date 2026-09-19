using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using PBG.Core;
using PBG.MathLibrary;
using PBG.Threads;


using BitOperations = System.Numerics.BitOperations;

namespace PBG.NewVoxel;

using VH = VoxelHelper;

[InternalSystemInit(InitPriority.Data)]
[InternalSystemCleanup]
public unsafe static partial class VoxelChunkGenerator
{
    private static int THREAD_COUNT => VoxelRenderer.RenderingThreads;

    const int MAX_X = 31;
    const int MAX_Z = 992;
    const int MAX_Y = 31744;



    public static ulong[][] BitMaps = [];
    public static ulong[][] SolidMaps = [];
    public static MeshSlices[] Slices = [];

    public static void Init()
    {
        BitMaps = new ulong[THREAD_COUNT][];
        SolidMaps = new ulong[THREAD_COUNT][];

        Slices = new MeshSlices[THREAD_COUNT];

        for (int i = 0; i < THREAD_COUNT; i++)
        {
            BitMaps[i] = new ulong[34 * 34];     
            SolidMaps[i] = new ulong[34 * 34];
            Slices[i] = new();
        }
    }

    public static void Cleanup()
    {
        for (int i = 0; i < THREAD_COUNT; i++)
        {
            Slices[i].Dispose();
        }
    }

    
    public static bool GenerateIndirectMesh1(VoxelChunk chunk, List<Vector4i> vertexData, Vector3i worldPosition, VoxelChunk blocks, out int vertexCount)
    {
        return IndirectMesher1.GenerateMesh(chunk, vertexData, worldPosition, blocks, out vertexCount);
    }

    public static bool GenerateIndirectMesh2(VoxelChunk chunk, List<Vector4i> vertexData, Vector3i worldPosition, int workerId, out int vertexCount)
    {
        return IndirectMesher2.GenerateMesh(chunk, vertexData, worldPosition, workerId, out vertexCount);
    }

    public static bool GenerateIndirectMesh3(VoxelChunk chunk, List<Vector4i> vertexData, Vector3i worldPosition, int workerId, out int vertexCount)
    {
        return IndirectMesher3.GenerateMesh(chunk, vertexData, worldPosition, workerId, out vertexCount);
    }

    public static bool GenerateGreedyMesh1(VoxelChunk chunk, List<Vector4i> vertexData, Vector3i worldPosition, int workerId, out int vertexCount)
    {
        return GreedyMesher1.GenerateMesh(chunk, vertexData, worldPosition, workerId, out vertexCount);
    }

    public static bool GenerateGreedyMesh4(VoxelChunk chunk, List<Vector4i> vertexData, Vector3i worldPosition, int workerId, out int vertexCount)
    {
        return GreedyMesher4.GenerateMesh(chunk, vertexData, worldPosition, workerId, out vertexCount);
    }

    public static bool GenerateGreedyMesh4Solid(VoxelChunk chunk, List<Vector4i> vertexData, Vector3i worldPosition, int workerId, out int vertexCount)
    {
        return GreedyMesher4Solid.GenerateMesh(chunk, vertexData, worldPosition, workerId, out vertexCount);
    }

    public static bool GenerateGreedyMesh5(VoxelChunk chunk, List<Vector4i> vertexData, Vector3i worldPosition, int workerId, out int vertexCount)
    {
        return GreedyMesher5.GenerateMesh(chunk, vertexData, worldPosition, workerId, out vertexCount);
    }

    public static bool GenerateGreedyMesh6(VoxelChunk chunk, ref MeshMapping mapping, int workerId)
    {
        return GreedyMesher6.GenerateMesh(chunk, ref mapping, workerId);
    }

    public static bool GenerateGreedyMesh7(VoxelChunk chunk, ref MeshMapping mapping, int workerId)
    {
        return GreedyMesher7.GenerateMesh(chunk, ref mapping, workerId);
    }

    public static bool GenerateGreedyMesh7Y(VoxelChunk chunk, ref MeshMapping mapping, int workerId)
    {
        return GreedyMesher7Y.GenerateMesh(chunk, ref mapping, workerId);
    }

    public static bool GenerateGreedyMesh7YByte(VoxelChunk chunk, ref MeshMapping mapping, int workerId)
    {
        return GreedyMesher7YByte.GenerateMesh(chunk, ref mapping, workerId);
    }
}

public unsafe struct NeighbourChunks
{
    public Block* Blocks0;  // (-1, -1, -1)
    public Block* Blocks1;  // ( 0, -1, -1) 
    public Block* Blocks2;  // ( 1, -1, -1)

    public Block* Blocks3;  // (-1, -1,  0)
    public Block* Blocks4;  // ( 0, -1,  0)
    public Block* Blocks5;  // ( 1, -1,  0)

    public Block* Blocks6;  // (-1, -1,  1)
    public Block* Blocks7;  // ( 0, -1,  1)
    public Block* Blocks8;  // ( 1, -1,  1)

    public Block* Blocks9;  // (-1,  0, -1)
    public Block* Blocks10; // ( 0,  0, -1)
    public Block* Blocks11; // ( 1,  0, -1)

    public Block* Blocks12; // (-1,  0,  0)
    // 13 skipped — (0, 0, 0) is the chunk being rendered
    public Block* Blocks14; // ( 1,  0,  0)

    public Block* Blocks15; // (-1,  0,  1)
    public Block* Blocks16; // ( 0,  0,  1)
    public Block* Blocks17; // ( 1,  0,  1)

    public Block* Blocks18; // (-1,  1, -1)
    public Block* Blocks19; // ( 0,  1, -1)
    public Block* Blocks20; // ( 1,  1, -1)

    public Block* Blocks21; // (-1,  1,  0)
    public Block* Blocks22; // ( 0,  1,  0)
    public Block* Blocks23; // ( 1,  1,  0)

    public Block* Blocks24; // (-1,  1,  1)
    public Block* Blocks25; // ( 0,  1,  1)
    public Block* Blocks26; // ( 1,  1,  1)

    public NeighbourChunks(VoxelRenderer renderer, Vector3i relativePosition)
    {
        Blocks0  = GetBlocks(renderer, relativePosition, -1, -1, -1);
        Blocks1  = GetBlocks(renderer, relativePosition,  0, -1, -1);
        Blocks2  = GetBlocks(renderer, relativePosition,  1, -1, -1);

        Blocks3  = GetBlocks(renderer, relativePosition, -1, -1,  0);
        Blocks4  = GetBlocks(renderer, relativePosition,  0, -1,  0);
        Blocks5  = GetBlocks(renderer, relativePosition,  1, -1,  0);

        Blocks6  = GetBlocks(renderer, relativePosition, -1, -1,  1);
        Blocks7  = GetBlocks(renderer, relativePosition,  0, -1,  1);
        Blocks8  = GetBlocks(renderer, relativePosition,  1, -1,  1);

        Blocks9  = GetBlocks(renderer, relativePosition, -1,  0, -1);
        Blocks10 = GetBlocks(renderer, relativePosition,  0,  0, -1);
        Blocks11 = GetBlocks(renderer, relativePosition,  1,  0, -1);

        Blocks12 = GetBlocks(renderer, relativePosition, -1,  0,  0);
        Blocks14 = GetBlocks(renderer, relativePosition,  1,  0,  0);

        Blocks15 = GetBlocks(renderer, relativePosition, -1,  0,  1);
        Blocks16 = GetBlocks(renderer, relativePosition,  0,  0,  1);
        Blocks17 = GetBlocks(renderer, relativePosition,  1,  0,  1);

        Blocks18 = GetBlocks(renderer, relativePosition, -1,  1, -1);
        Blocks19 = GetBlocks(renderer, relativePosition,  0,  1, -1);
        Blocks20 = GetBlocks(renderer, relativePosition,  1,  1, -1);

        Blocks21 = GetBlocks(renderer, relativePosition, -1,  1,  0);
        Blocks22 = GetBlocks(renderer, relativePosition,  0,  1,  0);
        Blocks23 = GetBlocks(renderer, relativePosition,  1,  1,  0);

        Blocks24 = GetBlocks(renderer, relativePosition, -1,  1,  1);
        Blocks25 = GetBlocks(renderer, relativePosition,  0,  1,  1);
        Blocks26 = GetBlocks(renderer, relativePosition,  1,  1,  1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Block* GetBlocks(VoxelRenderer renderer, Vector3i relativePosition, int ox, int oy, int oz)
    {
        relativePosition.X += ox;
        relativePosition.Y += oy;
        relativePosition.Z += oz;
        return renderer.GetChunk(relativePosition, out var chunk) ? chunk.Blocks : VoxelChunk.Empty.Blocks;
    }
}

public unsafe struct MeshSlices
{
    public uint* FrontSlice;
    public uint* BackSlice;

    public uint* RightSlice;
    public uint* LeftSlice;

    public uint* TopSlice;
    public uint* BottomSlice;

    public uint* RightMask;
    public uint* XSlice;
    public uint* LeftMask;

    public MeshSlices()
    {
        FrontSlice = MemoryHelper.Alloc<uint>(32);
        BackSlice = MemoryHelper.Alloc<uint>(32);

        RightSlice = MemoryHelper.Alloc<uint>(32);
        LeftSlice = MemoryHelper.Alloc<uint>(32);

        TopSlice = MemoryHelper.Alloc<uint>(32);
        BottomSlice = MemoryHelper.Alloc<uint>(32);

        RightMask = MemoryHelper.Alloc<uint>(32);
        XSlice = MemoryHelper.Alloc<uint>(32);
        LeftMask = MemoryHelper.Alloc<uint>(32);
    }

    public void Dispose()
    {
        MemoryHelper.Free(FrontSlice);
        MemoryHelper.Free(BackSlice);

        MemoryHelper.Free(RightSlice);
        MemoryHelper.Free(LeftSlice);

        MemoryHelper.Free(TopSlice);
        MemoryHelper.Free(BottomSlice);

        MemoryHelper.Free(RightMask);
        MemoryHelper.Free(XSlice);
        MemoryHelper.Free(LeftMask);
    }
}