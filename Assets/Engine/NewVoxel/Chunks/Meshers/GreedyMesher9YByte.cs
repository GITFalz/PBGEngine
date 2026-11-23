using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;
using PBG.Core;
using PBG.MathLibrary;

namespace PBG.NewVoxel;

using VH = VoxelHelper;

[InternalSystemInit(InitPriority.Data)]
[InternalSystemCleanup]
public unsafe class GreedyMesher9YByte
{
    private static int THREAD_COUNT => VoxelRenderer.RenderingThreads;

    public static NewMeshData[] MeshDatas = [];

    public static void Init()
    {
        MeshDatas = new NewMeshData[THREAD_COUNT];

        for (int i = 0; i < THREAD_COUNT; i++)
        {
            MeshDatas[i] = new();
        }
    }

    public static void Cleanup()
    {
        for (int i = 0; i < THREAD_COUNT; i++)
        {
            MeshDatas[i].Dispose();
        }
    }

    private static void GenerateLeft(VoxelChunk chunk, ref MeshMapping mapping, uint row, int x, int z)
    {
        while (row != 0)
        {
            int trailingZeros = Bit.TrailingZeros(row);

            uint newRow = row >> trailingZeros;

            int h = Bit.TrailingZeros(~newRow);

            uint mask = (uint)((1UL << h) - 1) << trailingZeros;

            row &= ~mask;

            int w = 1;

            Block block = chunk.Get(trailingZeros + x * 32 + z * 1024);

            w--;
            h--;

            uint geometryIndex = (uint)block.GetSolidGeometryIndex(3);
            uint pos = (uint)(x | (trailingZeros << 5) | (z << 10));
            uint side = 3;
            
            uint size = (uint)(w | (h << 5));
            
            uint vx = geometryIndex | (pos << 14) | (side << 29);
            uint vy = size;

            mapping.AddFace(new(vx, vy));
        }
    }

    private static void GenerateRight(VoxelChunk chunk, ref MeshMapping mapping, uint row, int x, int z)
    {
        while (row != 0)
        {
            int trailingZeros = Bit.TrailingZeros(row);

            uint newRow = row >> trailingZeros;

            int h = Bit.TrailingZeros(~newRow);

            uint mask = (uint)((1UL << h) - 1) << trailingZeros;

            row &= ~mask;

            int w = 1;

            Block block = chunk.Get(trailingZeros + x * 32 + z * 1024);

            w--;
            h--;

            uint geometryIndex = (uint)block.GetSolidGeometryIndex(1);
            uint pos = (uint)(x | (trailingZeros << 5) | (z << 10));
            uint side = 1;
            
            uint size = (uint)(w | (h << 5));
            
            uint vx = geometryIndex | (pos << 14) | (side << 29);
            uint vy = size;

            mapping.AddFace(new(vx, vy));
        }
    }


    public static bool GenerateMesh(VoxelChunk chunk, ref MeshMapping meshMapping) => GenerateMesh(VoxelChunk.Empty, chunk, ref meshMapping);
    public static bool GenerateMesh(VoxelChunk empty, VoxelChunk chunk, ref MeshMapping meshMapping)
    {
        //Stopwatch stopwatch = Stopwatch.StartNew();

        VoxelChunk leftChunk = chunk.ChunkLookup[12]; //empty;
        VoxelChunk rightChunk = chunk.ChunkLookup[13]; //empty;
        
        for (int z = 0; z < 32; z++)
        {
            int zIndex = z * 1024;

            V256b blockRowA = V256B.Load(leftChunk.ByteBlocks + zIndex + 992);
            V256b blockRowB = V256B.Load(chunk.ByteBlocks + zIndex);

            V256b isAirA = blockRowA.CompareEqual(V256B.Zero);
            V256b isAirB = blockRowB.CompareEqual(V256B.Zero);

            uint rowAirA = (uint)Avx2.MoveMask(isAirA);
            uint rowAirB = (uint)Avx2.MoveMask(isAirB);

            uint rowSolidA = ~rowAirA;
            uint rowSolidB = ~rowAirB;

            uint rowLeft = rowSolidB & (~rowSolidA);
            uint rowRight = uint.MaxValue;

            if (rowLeft != 0)
                GenerateLeft(chunk, ref meshMapping, rowLeft, 0, z);

            rowSolidA = rowSolidB;

            byte* blocks = chunk.ByteBlocks + (zIndex + 32);
            
            for (int x = 1; x < 32; x++)
            {
                blockRowB = V256B.Load(blocks);

                isAirB = blockRowB.CompareEqual(V256B.Zero);

                rowAirB = (uint)Avx2.MoveMask(isAirB);

                rowSolidB = ~rowAirB;

                rowLeft = rowSolidB & (~rowSolidA);
                rowRight = rowSolidA & (~rowSolidB);

                if (rowLeft != 0)
                    GenerateLeft(chunk, ref meshMapping, rowLeft, x, z);

                if (rowRight != 0)
                    GenerateRight(chunk, ref meshMapping, rowRight, x - 1, z);

                rowSolidA = rowSolidB;

                blocks += 32;
            }

            blockRowB = V256B.Load(rightChunk.ByteBlocks + zIndex);

            isAirB = blockRowB.CompareEqual(V256B.Zero);

            rowAirB = (uint)Avx2.MoveMask(isAirB);

            rowSolidB = ~rowAirB;

            rowRight = rowSolidA & (~rowSolidB);

            if (rowRight != 0)
                GenerateRight(chunk, ref meshMapping, rowRight, 31, z);
        }

        //Console.WriteLine(stopwatch.Elapsed.TotalMilliseconds);

        return true;
    }

    [MemoryDiagnoser]
    public class MeshBench
    {
        VoxelChunk empty;
        VoxelChunk chunk; 
        MeshMapping mapping;
        

        [GlobalSetup] public void Setup()
        {
            empty = new();
            chunk = new();

            for (int z = 0; z < 32; z++)
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                byte block = 0;

                // Main terrain
                int height = 8
                    + (int)(Math.Sin(x * 0.25) * 3)
                    + (int)(Math.Cos(z * 0.2) * 3);

                if (y <= height)
                    block = y == height ? (byte)2 : (byte)1;

                // Underground caves
                if (y < height - 2)
                {
                    double cave =
                        Math.Sin(x * 0.4) *
                        Math.Cos(y * 0.35) *
                        Math.Sin(z * 0.4);

                    if (cave > 0.45)
                        block = 0;
                }

                // Floating platforms
                if (y >= 20 && y <= 23 &&
                    x >= 5 && x <= 12 &&
                    z >= 5 && z <= 12)
                {
                    block = y == 23 ? (byte)2 : (byte)1;
                }

                // Random block placements
                uint hash = (uint)(x * 73856093 ^ y * 19349663 ^ z * 83492791);

                if (hash % 100 < 8)
                    block = 3;

                chunk.ByteBlocks[y + x * 32 + z * 1024] = block;
            }
        }

        [Benchmark] public bool Mesh() => GenerateMesh(empty, chunk, ref mapping);

        public static void Profile(int iterations)
        {
            if (iterations <= 0)
                throw new ArgumentOutOfRangeException(nameof(iterations));

            var benchmark = new MeshBench();
            benchmark.Setup();

            for (int i = 0; i < 100; i++)
                benchmark.Mesh();

            for (int i = 0; i < iterations; i++)
                benchmark.Mesh();

            Console.WriteLine($"Completed {iterations} measured mesh iterations.");
        }
    }


    public unsafe struct NeighbourChunks
    {
        public byte* Blocks0;  // (-1, -1, -1)
        public byte* Blocks1;  // ( 0, -1, -1)
        public byte* Blocks2;  // ( 1, -1, -1)

        public byte* Blocks3;  // (-1, -1,  0)
        public byte* Blocks4;  // ( 0, -1,  0)
        public byte* Blocks5;  // ( 1, -1,  0)

        public byte* Blocks6;  // (-1, -1,  1)
        public byte* Blocks7;  // ( 0, -1,  1)
        public byte* Blocks8;  // ( 1, -1,  1)

        public byte* Blocks9;  // (-1,  0, -1)
        public byte* Blocks10; // ( 0,  0, -1)
        public byte* Blocks11; // ( 1,  0, -1)

        public byte* Blocks12; // (-1,  0,  0)
        // 13 skipped — (0, 0, 0) is the chunk being rendered
        public byte* Blocks14; // ( 1,  0,  0)

        public byte* Blocks15; // (-1,  0,  1)
        public byte* Blocks16; // ( 0,  0,  1)
        public byte* Blocks17; // ( 1,  0,  1)

        public byte* Blocks18; // (-1,  1, -1)
        public byte* Blocks19; // ( 0,  1, -1)
        public byte* Blocks20; // ( 1,  1, -1)

        public byte* Blocks21; // (-1,  1,  0)
        public byte* Blocks22; // ( 0,  1,  0)
        public byte* Blocks23; // ( 1,  1,  0)

        public byte* Blocks24; // (-1,  1,  1)
        public byte* Blocks25; // ( 0,  1,  1)
        public byte* Blocks26; // ( 1,  1,  1)

        public NeighbourChunks(VoxelChunk chunk, Vector3i relativePosition)
        {
            Blocks0  = chunk.ChunkLookup[ 0].ByteBlocks;
            Blocks1  = chunk.ChunkLookup[ 1].ByteBlocks;
            Blocks2  = chunk.ChunkLookup[ 2].ByteBlocks;

            Blocks3  = chunk.ChunkLookup[ 3].ByteBlocks;
            Blocks4  = chunk.ChunkLookup[ 4].ByteBlocks;
            Blocks5  = chunk.ChunkLookup[ 5].ByteBlocks;

            Blocks6  = chunk.ChunkLookup[ 6].ByteBlocks;
            Blocks7  = chunk.ChunkLookup[ 7].ByteBlocks;
            Blocks8  = chunk.ChunkLookup[ 8].ByteBlocks;

            Blocks9  = chunk.ChunkLookup[ 9].ByteBlocks;
            Blocks10 = chunk.ChunkLookup[10].ByteBlocks;
            Blocks11 = chunk.ChunkLookup[11].ByteBlocks;

            Blocks12 = chunk.ChunkLookup[12].ByteBlocks;
            Blocks14 = chunk.ChunkLookup[13].ByteBlocks;

            Blocks15 = chunk.ChunkLookup[14].ByteBlocks;
            Blocks16 = chunk.ChunkLookup[15].ByteBlocks;
            Blocks17 = chunk.ChunkLookup[16].ByteBlocks;

            Blocks18 = chunk.ChunkLookup[17].ByteBlocks;
            Blocks19 = chunk.ChunkLookup[18].ByteBlocks;
            Blocks20 = chunk.ChunkLookup[19].ByteBlocks;

            Blocks21 = chunk.ChunkLookup[20].ByteBlocks;
            Blocks22 = chunk.ChunkLookup[21].ByteBlocks;
            Blocks23 = chunk.ChunkLookup[22].ByteBlocks;

            Blocks24 = chunk.ChunkLookup[23].ByteBlocks;
            Blocks25 = chunk.ChunkLookup[24].ByteBlocks;
            Blocks26 = chunk.ChunkLookup[25].ByteBlocks;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte* GetBlocks(VoxelChunk chunk, Vector3i relativePosition, int ox, int oy, int oz)
        {
            relativePosition.X += ox * chunk.LodMult;
            relativePosition.Y += oy * chunk.LodMult;
            relativePosition.Z += oz * chunk.LodMult;
            return chunk.Renderer.GetChunk(relativePosition, out var sideChunk) ? sideChunk.ByteBlocks : VoxelChunk.Empty.ByteBlocks;
        }
    }

    public sealed class NewMeshData
    {
        const int MAP_34_SIZE = 34 * 34;
        const int MAP_32_SIZE = 32 * 32;
        
        // ulong maps 34 x 34
        public ulong* BitMap;
        
        // uint maps 34 x 34
        public uint* YBitMap;
        public uint* AOTypeMap;
        public uint* YAOTypeMap;

        // uint maps 32 x 32
        public uint* TypeMap;
        public uint* ZTypeMap;
        public uint* NonSolidMap;

        // ulong 32
        public ulong* TopBits;
        public ulong* BottomBits;
        
        // uints 32
        public uint* FrontSlice;
        public uint* BackSlice;

        public uint* TopSlice;
        public uint* BottomSlice;

        public uint* RightSlice;
        public uint* MiddleSlice;
        public uint* LeftSlice;

        public uint* RightMask;
        public uint* XSlice;
        public uint* LeftMask;

        public V256b* YRows;
        public V256b* ZTypes;


        // base pointers
        private Allocator<ulong> _ulongMaps34_34;
        private Allocator<uint> _uintMaps34_34;
        private Allocator<uint> _uintMaps32_32;
        private Allocator<ulong> _ulong32;
        private Allocator<uint> _uints32;
        private Allocator<V256b> _v256b4;

        public NewMeshData()
        {
            // maps 34
            _ulongMaps34_34 = new(MAP_34_SIZE, 1);

            BitMap          = _ulongMaps34_34.Next();

            // uint maps 34
            _uintMaps34_34  = new(MAP_34_SIZE, 3);

            YBitMap         = _uintMaps34_34.Next();
            AOTypeMap       = _uintMaps34_34.Next();
            YAOTypeMap      = _uintMaps34_34.Next();

            // maps 32
            _uintMaps32_32  = new(MAP_32_SIZE, 3);
            
            TypeMap         = _uintMaps32_32.Next();
            ZTypeMap        = _uintMaps32_32.Next();
            NonSolidMap     = _uintMaps32_32.Next();

            // ulong 32
            _ulong32        = new(32, 2);
            
            TopBits         = _ulong32.Next();
            BottomBits      = _ulong32.Next();

            // uints 32
            _uints32        = new(32, 10); 

            FrontSlice      = _uints32.Next();
            BackSlice       = _uints32.Next();

            TopSlice        = _uints32.Next();
            BottomSlice     = _uints32.Next();

            RightSlice      = _uints32.Next();
            MiddleSlice     = _uints32.Next();
            LeftSlice       = _uints32.Next();

            RightMask       = _uints32.Next();
            XSlice          = _uints32.Next();
            LeftMask        = _uints32.Next();

            
            _v256b4         = new(4, 2); 

            YRows           = _v256b4.Next();
            ZTypes          = _v256b4.Next();
        }

        public void Dispose()
        {
            _ulongMaps34_34.Free();
            _uintMaps34_34.Free();
            _uintMaps32_32.Free();
            _ulong32.Free();
            _uints32.Free();    
            _v256b4.Free();
        }

        private struct Allocator<T>(int size, int count) where T : unmanaged
        {
            private readonly T* _ptr = MemoryHelper.AllocClear<T>(size * count);
            private int i = 0;
            public T* Next() => _ptr + size * i++;
            public readonly void Free() => MemoryHelper.Free(_ptr);
        }
    }
}