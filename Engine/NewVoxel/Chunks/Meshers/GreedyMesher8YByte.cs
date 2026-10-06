using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using PBG.Core;
using PBG.MathLibrary;

namespace PBG.NewVoxel;

using VH = VoxelHelper;

[InternalSystemInit(InitPriority.Data)]
[InternalSystemCleanup]
public unsafe static class GreedyMesher8YByte
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


    public static void GetBitMaps(VoxelChunk chunk, ref NeighbourChunks neighbours, int workerId)
    {
        var meshData =  MeshDatas[workerId];

        ulong* bitMap = meshData.BitMap;
        
        uint* aoTypeMap = meshData.AOTypeMap;
        uint* yaoTypeMap = meshData.YAOTypeMap;
        uint* yBitMap = meshData.YBitMap;

        uint* typeMap = meshData.TypeMap;
        uint* zTypeMap = meshData.ZTypeMap;
        uint* nonSolidMap = meshData.NonSolidMap;

        ulong* topBits = meshData.TopBits;
        ulong* bottomBits = meshData.BottomBits;


        uint* topMask = meshData.RightMask;
        uint* bottomMask = meshData.LeftMask;

        var yrows = meshData.YRows;
        var ztypes = meshData.ZTypes;

        //uint* blockPtr = (uint*)chunk.Blocks;

        int maxYlayerIndex = 31744;

        V256u nzRow1 = V256U.Zero;
        V256u nzRow2 = V256U.Zero;
        V256u nzRow3 = V256U.Zero;
        V256u nzRow4 = V256U.Zero;

        V256u pzRow1 = V256U.Zero;
        V256u pzRow2 = V256U.Zero;
        V256u pzRow3 = V256U.Zero;
        V256u pzRow4 = V256U.Zero;

        for (int z = 0; z < 32; z++)
        {
            int blockIndex = z * 1024;
            int mapIndex = (z + 1) * 34 + 1;

            /*
            V256u mem1 = V256U.Zero;
            V256u mem2 = V256U.Zero;
            V256u mem3 = V256U.Zero;
            V256u mem4 = V256U.Zero;
            */
            V256b mem = V256B.Zero;


            yrows[0] = V256B.Zero;
            yrows[1] = V256B.Zero;
            yrows[2] = V256B.Zero;
            yrows[3] = V256B.Zero;

            ztypes[0] = V256B.Zero;
            ztypes[1] = V256B.Zero;
            ztypes[2] = V256B.Zero;
            ztypes[3] = V256B.Zero;


            uint rowBottom = 0;
            uint rowTop = 0;

            uint* typePtr     = typeMap + z * 32;
            ulong* bitPtr     = bitMap + mapIndex;
            uint* aoPtr       = aoTypeMap + mapIndex;

            //uint* blockPtrLocal = blockPtr + z * 1024;

            for (int x = 0; x < 32; x++)
            {
                int shiftIndex = x >> 3;
                byte shift = (byte)(x & 7);

                V256b blockRow = V256B.Load(chunk.ByteBlocks + x * 32 + z * 1024);

                var isAir = blockRow.CompareEqual(V256B.Zero);
                var isSolid = isAir ^ V256B.MaxValue;

                ulong row = VH.GetRowSolidBitsV256X64(isSolid);
                uint bitTop    = neighbours.Blocks22[blockIndex] != 0 ?     1U : 0;
                uint bitBottom = neighbours.Blocks4[blockIndex + 31] != 0 ? 1U : 0;

                row <<= 1;
                row |= (ulong)bitTop << 33;
                row |= (ulong)bitBottom;
                
                *bitPtr = row;
                *aoPtr = (uint)(VH.GetAOTypeMap(row) >> 1);

                V256b rowOffset = V256B.Load(chunk.ByteBlocks + x * 32 + z * 1024 - 1);
                V256b offsetEqual = blockRow.CompareEqual(rowOffset);

                *typePtr = (~VH.GetRowSolidBitsV256X64(offsetEqual)) & 0xFFFFFFFEu;

                V256b ybit = isSolid & V256B.One;

                yrows[shiftIndex] |= ybit.ShiftOneLeft(shift);


                rowTop    |= bitTop    << x;
                rowBottom |= bitBottom << x;


                // handle the type checking for the y axis
                if (x > 0)
                {   
                    // check if block are unequal
                    V256b zequal = blockRow.CompareEqual(mem);

                    // invert the equality
                    V256b znot = zequal ^ V256B.MaxValue;

                    // make it so the values are either 0 or 1
                    V256b zbit = znot & V256B.One;

                    //var (zbit1, zbit2, zbit3, zbit4) = VH.WidenToV256u(zbit);

                    // shift the bits to their place in the map
                    /*
                    V256u zshift1 = zbit1 << (byte)x;
                    V256u zshift2 = zbit2 << (byte)x;
                    V256u zshift3 = zbit3 << (byte)x;
                    V256u zshift4 = zbit4 << (byte)x;

                    zTypeRow1 |= zshift1;
                    zTypeRow2 |= zshift2;
                    zTypeRow3 |= zshift3;
                    zTypeRow4 |= zshift4;
                    */

                    ztypes[shiftIndex] |= zbit.ShiftOneLeft(shift);
                }

                mem = blockRow;

                mapIndex++;
                blockIndex += 32;

                bitPtr++;
                aoPtr++;
                typePtr++;  
            }

            int yIndex = z * 1024;
            int zIndexY = z * 32;

            int rowZindex = (z + 1) * 34;

            
            V256u zTypeRow1 = V256U.Zero;
            V256u zTypeRow2 = V256U.Zero;
            V256u zTypeRow3 = V256U.Zero;
            V256u zTypeRow4 = V256U.Zero;

            var (zTypes11, zTypes12, zTypes13, zTypes14) = VH.WidenToV256u(ztypes[0]);
            var (zTypes21, zTypes22, zTypes23, zTypes24) = VH.WidenToV256u(ztypes[1]);
            var (zTypes31, zTypes32, zTypes33, zTypes34) = VH.WidenToV256u(ztypes[2]);
            var (zTypes41, zTypes42, zTypes43, zTypes44) = VH.WidenToV256u(ztypes[3]);

            zTypeRow1 = zTypes11 | (zTypes21 << 8) | (zTypes31 << 16) | (zTypes41 << 24);
            zTypeRow2 = zTypes12 | (zTypes22 << 8) | (zTypes32 << 16) | (zTypes42 << 24);
            zTypeRow3 = zTypes13 | (zTypes23 << 8) | (zTypes33 << 16) | (zTypes43 << 24);
            zTypeRow4 = zTypes14 | (zTypes24 << 8) | (zTypes34 << 16) | (zTypes44 << 24);

            Avx.Store(zTypeMap + zIndexY,      zTypeRow1);
            Avx.Store(zTypeMap + zIndexY + 8,  zTypeRow2);
            Avx.Store(zTypeMap + zIndexY + 16, zTypeRow3);
            Avx.Store(zTypeMap + zIndexY + 24, zTypeRow4);




            yBitMap[z + 1123]  = 0;//rowTop;
            yBitMap[z + 1]     = 0;//rowBottom;

            yBitMap[rowZindex] = rowBottom;//uint.MaxValue;
            yBitMap[rowZindex + 33] = rowTop;//uint.MaxValue;

            yaoTypeMap[z + 1123]  = 0;//VH.GetAOTypeMap(rowTop);
            yaoTypeMap[z + 1]     = 0;//VH.GetAOTypeMap(rowBottom);

            yaoTypeMap[rowZindex] = (uint)VH.GetAOTypeMap(rowBottom);//uint.MaxValue;
            yaoTypeMap[rowZindex + 33] = (uint)VH.GetAOTypeMap(rowTop);//uint.MaxValue;


            V256u yRow1 = V256U.Zero;
            V256u yRow2 = V256U.Zero;
            V256u yRow3 = V256U.Zero;
            V256u yRow4 = V256U.Zero;

            var (yRows11, yRows12, yRows13, yRows14) = VH.WidenToV256u(yrows[0]);
            var (yRows21, yRows22, yRows23, yRows24) = VH.WidenToV256u(yrows[1]);
            var (yRows31, yRows32, yRows33, yRows34) = VH.WidenToV256u(yrows[2]);
            var (yRows41, yRows42, yRows43, yRows44) = VH.WidenToV256u(yrows[3]);

            yRow1 = yRows11 | (yRows21 << 8) | (yRows31 << 16) | (yRows41 << 24);
            yRow2 = yRows12 | (yRows22 << 8) | (yRows32 << 16) | (yRows42 << 24);
            yRow3 = yRows13 | (yRows23 << 8) | (yRows33 << 16) | (yRows43 << 24);
            yRow4 = yRows14 | (yRows24 << 8) | (yRows34 << 16) | (yRows44 << 24);

            Avx.Store(yBitMap + rowZindex + 1, yRow1);
            Avx.Store(yBitMap + rowZindex + 9, yRow2);
            Avx.Store(yBitMap + rowZindex + 17, yRow3);
            Avx.Store(yBitMap + rowZindex + 25, yRow4);

            Avx.Store(yaoTypeMap + rowZindex + 1, VH.GetAOTypeMap(yRow1));
            Avx.Store(yaoTypeMap + rowZindex + 9, VH.GetAOTypeMap(yRow2));
            Avx.Store(yaoTypeMap + rowZindex + 17, VH.GetAOTypeMap(yRow3));
            Avx.Store(yaoTypeMap + rowZindex + 25, VH.GetAOTypeMap(yRow4));
            


            int indexNx = 992 + yIndex;
            int indexPx = yIndex;

            int indexNz = zIndexY + maxYlayerIndex;
            int indexPz = zIndexY;

            byte* ptr12 = neighbours.Blocks12 + indexNx;
            byte* ptr14 = neighbours.Blocks14 + indexPx;
            byte* ptr10 = neighbours.Blocks10 + indexNz;
            byte* ptr16 = neighbours.Blocks16 + indexPz;

            // rows in the direction of y, on the plane P (+) / N (-), on the axis x / z

            // handle chunk that is in -x at index 12 and i need the last block
            ulong rowNx = GetSolidRow(ptr12);

            // handle chunk that is in +x at index 14 and i need the first block
            ulong rowPx = GetSolidRow(ptr14);

            // handle chunk that is in -z at index 10 and i need the last block
            ulong rowNz = GetSolidRow(ptr10, z, ref nzRow1, ref nzRow2, ref nzRow3, ref nzRow4);

            // handle chunk that is in +z at index 16 and i need the first block
            ulong rowPz = GetSolidRow(ptr16, z, ref pzRow1, ref pzRow2, ref pzRow3, ref pzRow4);

            bitMap[rowZindex] = rowNx;
            bitMap[33 + rowZindex] = rowPx;

            bitMap[z + 1] = rowNz;
            bitMap[1123 + z] = rowPz;


            aoTypeMap[rowZindex] = (uint)(VH.GetAOTypeMap(rowNx) >> 1);
            aoTypeMap[33 + rowZindex] = (uint)(VH.GetAOTypeMap(rowPx) >> 1);

            aoTypeMap[z + 1] = (uint)(VH.GetAOTypeMap(rowNz) >> 1);
            aoTypeMap[1123 + z] = (uint)(VH.GetAOTypeMap(rowPz) >> 1);
        }

        // store the shifted rows for the z axis
        Avx.Store(yBitMap + 1, nzRow1);
        Avx.Store(yBitMap + 9, nzRow2);
        Avx.Store(yBitMap + 17, nzRow3);
        Avx.Store(yBitMap + 25, nzRow4);

        Avx.Store(yBitMap + 1123, pzRow1);
        Avx.Store(yBitMap + 1131, pzRow2);
        Avx.Store(yBitMap + 1139, pzRow3);
        Avx.Store(yBitMap + 1147, pzRow4);

        // store the ao types
        Avx.Store(yaoTypeMap + 1, VH.GetAOTypeMap(nzRow1));
        Avx.Store(yaoTypeMap + 9, VH.GetAOTypeMap(nzRow2));
        Avx.Store(yaoTypeMap + 17, VH.GetAOTypeMap(nzRow3));
        Avx.Store(yaoTypeMap + 25, VH.GetAOTypeMap(nzRow4));

        Avx.Store(yaoTypeMap + 1123, VH.GetAOTypeMap(pzRow1));
        Avx.Store(yaoTypeMap + 1131, VH.GetAOTypeMap(pzRow2));
        Avx.Store(yaoTypeMap + 1139, VH.GetAOTypeMap(pzRow3));
        Avx.Store(yaoTypeMap + 1147, VH.GetAOTypeMap(pzRow4));
    }

    

    public static void GetBitRowX0(
        VoxelChunk chunk, ref NeighbourChunks neighbours, 
        ref int blockIndex, int x, int z, 
        ref ulong* bitPtr, ref uint* aoPtr, ref uint* typePtr, 
        ref V256b yrow, ref V256b ztype, ref V256b mem,
        ref uint rowTop, ref uint rowBottom
    ){
        byte shift = (byte)(x & 7);

        V256b blockRow = V256B.Load(chunk.ByteBlocks + blockIndex);

        var isAir = blockRow.CompareEqual(V256B.Zero);
        var isSolid = isAir ^ V256B.MaxValue;

        ulong row = VH.GetRowSolidBitsV256X64(isSolid);
        uint bitTop    = neighbours.Blocks22[blockIndex] != 0 ?     1U : 0;
        uint bitBottom = neighbours.Blocks4[blockIndex + 31] != 0 ? 1U : 0;

        row <<= 1;
        row |= (ulong)bitTop << 33;
        row |= (ulong)bitBottom;
        
        *bitPtr = row;
        *aoPtr = (uint)(VH.GetAOTypeMap(row) >> 1);

        // type
        V256b rowOffset = V256B.Load(chunk.ByteBlocks + blockIndex - 1);
        V256b offsetEqual = blockRow.CompareEqual(rowOffset);

        *typePtr = (~VH.GetRowSolidBitsV256X64(offsetEqual)) & 0xFFFFFFFEu;

        // transposed
        V256b ybit = isSolid & V256B.One;

        yrow |= ybit.ShiftOneLeft(shift);

        rowTop    |= bitTop    << x;
        rowBottom |= bitBottom << x;

        mem = blockRow;

        blockIndex += 32;

        bitPtr++;
        aoPtr++;
        typePtr++;  
    }

    public static void GetBitRowX1(
        VoxelChunk chunk, ref NeighbourChunks neighbours, 
        ref int blockIndex, int x, int z, 
        ref ulong* bitPtr, ref uint* aoPtr, ref uint* typePtr, 
        ref V256b yrow, ref V256b ztype, ref V256b mem,
        ref uint rowTop, ref uint rowBottom
    ){
        byte shift = (byte)(x & 7);

        V256b blockRow = V256B.Load(chunk.ByteBlocks + blockIndex);

        var isAir = blockRow.CompareEqual(V256B.Zero);
        var isSolid = isAir ^ V256B.MaxValue;

        ulong row = VH.GetRowSolidBitsV256X64(isSolid);

        uint bitTop    = neighbours.Blocks22[blockIndex] != 0 ?     1U : 0;
        uint bitBottom = neighbours.Blocks4[blockIndex + 31] != 0 ? 1U : 0;

        row <<= 1;
        row |= (ulong)bitTop << 33;
        row |= (ulong)bitBottom;
        
        *bitPtr = row;
        *aoPtr = (uint)(VH.GetAOTypeMap(row) >> 1);

        // type
        V256b rowOffset = V256B.Load(chunk.ByteBlocks + blockIndex - 1);
        V256b offsetEqual = blockRow.CompareEqual(rowOffset);

        *typePtr = (~VH.GetRowSolidBitsV256X64(offsetEqual)) & 0xFFFFFFFEu;

        // transposed
        V256b ybit = isSolid & V256B.One;

        yrow |= ybit.ShiftOneLeft(shift);


        rowTop    |= bitTop    << x;
        rowBottom |= bitBottom << x;


        // handle the type checking for the y axis
        if (x > 0)
        {   
            // 1. check if block are unequal
            // 2. invert the equality
            // 3. make it so the values are either 0 or 1
            V256b zequal = blockRow.CompareEqual(mem);
            V256b znot = zequal ^ V256B.MaxValue;
            V256b zbit = znot & V256B.One;

            ztype |= zbit.ShiftOneLeft(shift);
        }

        mem = blockRow;

        blockIndex += 32;

        bitPtr++;
        aoPtr++;
        typePtr++;  
    }

    public static void GetBitMaps2(VoxelChunk chunk, ref NeighbourChunks neighbours, int workerId)
    {
        var meshData =  MeshDatas[workerId];

        ulong* bitMap = meshData.BitMap;
        
        uint* aoTypeMap = meshData.AOTypeMap;
        uint* yaoTypeMap = meshData.YAOTypeMap;
        uint* yBitMap = meshData.YBitMap;

        uint* typeMap = meshData.TypeMap;
        uint* zTypeMap = meshData.ZTypeMap;
        uint* nonSolidMap = meshData.NonSolidMap;

        ulong* topBits = meshData.TopBits;
        ulong* bottomBits = meshData.BottomBits;


        uint* topMask = meshData.RightMask;
        uint* bottomMask = meshData.LeftMask;

        //uint* blockPtr = (uint*)chunk.Blocks;

        int maxYlayerIndex = 31744;

        V256u nzRow1 = V256U.Zero;
        V256u nzRow2 = V256U.Zero;
        V256u nzRow3 = V256U.Zero;
        V256u nzRow4 = V256U.Zero;

        V256u pzRow1 = V256U.Zero;
        V256u pzRow2 = V256U.Zero;
        V256u pzRow3 = V256U.Zero;
        V256u pzRow4 = V256U.Zero;

        for (int z = 0; z < 32; z++)
        {
            int blockIndex = z * 1024;
            int mapIndex = (z + 1) * 34 + 1;

            /*
            V256u mem1 = V256U.Zero;
            V256u mem2 = V256U.Zero;
            V256u mem3 = V256U.Zero;
            V256u mem4 = V256U.Zero;
            */
            V256b mem = V256B.Zero;


            V256b yrow1 = V256B.Zero;
            V256b yrow2 = V256B.Zero;
            V256b yrow3 = V256B.Zero;
            V256b yrow4 = V256B.Zero;

            V256b ztype1 = V256B.Zero;
            V256b ztype2 = V256B.Zero;
            V256b ztype3 = V256B.Zero;
            V256b ztype4 = V256B.Zero;


            uint rowBottom = 0;
            uint rowTop = 0;

            uint* typePtr     = typeMap;
            ulong* bitPtr     = bitMap + mapIndex;
            uint* aoPtr       = aoTypeMap + mapIndex;

            //uint* blockPtrLocal = blockPtr + z * 1024;

            for (int x = 0; x < 8; x++)
            {
                int shiftIndex = x >> 3;
                byte shift = (byte)(x & 7);

                V256b blockRow = V256B.Load(chunk.ByteBlocks + x * 32 + z * 1024);

                var isAir = blockRow.CompareEqual(V256B.Zero);
                var isSolid = isAir ^ V256B.MaxValue;

                ulong row = VH.GetRowSolidBitsV256X64(isSolid);
                uint bitTop    = neighbours.Blocks22[blockIndex] != 0 ?     1U : 0;
                uint bitBottom = neighbours.Blocks4[blockIndex + 31] != 0 ? 1U : 0;

                row <<= 1;
                row |= (ulong)bitTop << 33;
                row |= (ulong)bitBottom;
                
                *bitPtr = row;
                *aoPtr = (uint)(VH.GetAOTypeMap(row) >> 1);

                // type
                V256b rowOffset = V256B.Load(chunk.ByteBlocks + x * 32 + z * 1024 - 1);
                V256b offsetEqual = blockRow.CompareEqual(rowOffset);

                *typePtr = (~VH.GetRowSolidBitsV256X64(offsetEqual)) & 0xFFFFFFFEu;

                // transposed
                V256b ybit = isSolid & V256B.One;

                yrow1 |= ybit.ShiftOneLeft(shift);


                rowTop    |= bitTop    << x;
                rowBottom |= bitBottom << x;


                // handle the type checking for the y axis
                if (x > 0)
                {   
                    // 1. check if block are unequal
                    // 2. invert the equality
                    // 3. make it so the values are either 0 or 1
                    V256b zequal = blockRow.CompareEqual(mem);
                    V256b znot = zequal ^ V256B.MaxValue;
                    V256b zbit = znot & V256B.One;

                    ztype1 |= zbit.ShiftOneLeft(shift);
                }

                mem = blockRow;

                mapIndex++;
                blockIndex += 32;

                bitPtr++;
                aoPtr++;
                typePtr++;  
            }

            for (int x = 8; x < 16; x++)
            {
                int shiftIndex = x >> 3;
                byte shift = (byte)(x & 7);

                V256b blockRow = V256B.Load(chunk.ByteBlocks + x * 32 + z * 1024);

                var isAir = blockRow.CompareEqual(V256B.Zero);
                var isSolid = isAir ^ V256B.MaxValue;

                ulong row = VH.GetRowSolidBitsV256X64(isSolid);
                uint bitTop    = neighbours.Blocks22[blockIndex] != 0 ?     1U : 0;
                uint bitBottom = neighbours.Blocks4[blockIndex + 31] != 0 ? 1U : 0;

                row <<= 1;
                row |= (ulong)bitTop << 33;
                row |= (ulong)bitBottom;
                
                *bitPtr = row;
                *aoPtr = (uint)(VH.GetAOTypeMap(row) >> 1);

                V256b rowOffset = V256B.Load(chunk.ByteBlocks + x * 32 + z * 1024 - 1);
                V256b offsetEqual = blockRow.CompareEqual(rowOffset);

                *typePtr = (~VH.GetRowSolidBitsV256X64(offsetEqual)) & 0xFFFFFFFEu;

                V256b ybit = isSolid & V256B.One;

                //var (ybit1, ybit2, ybit3, ybit4) = VH.WidenToV256u(ybit);

                // shift the bits to their place in the map
                /*
                V256u yshift1 = ybit1 << (byte)x;
                V256u yshift2 = ybit2 << (byte)x;
                V256u yshift3 = ybit3 << (byte)x;
                V256u yshift4 = ybit4 << (byte)x;

                yRow1 |= yshift1;
                yRow2 |= yshift2;
                yRow3 |= yshift3;
                yRow4 |= yshift4;
                */

                yrow2 |= ybit.ShiftOneLeft(shift);


                rowTop    |= bitTop    << x;
                rowBottom |= bitBottom << x;


                // handle the type checking for the y axis
                if (x > 0)
                {   
                    // 1. check if block are unequal
                    // 2. invert the equality
                    // 3. make it so the values are either 0 or 1
                    V256b zequal = blockRow.CompareEqual(mem);
                    V256b znot = zequal ^ V256B.MaxValue;
                    V256b zbit = znot & V256B.One;

                    ztype1 |= zbit.ShiftOneLeft(shift);
                }

                mem = blockRow;

                mapIndex++;
                blockIndex += 32;

                bitPtr++;
                aoPtr++;
                typePtr++;  
            }

            for (int x = 16; x < 24; x++)
            {
                int shiftIndex = x >> 3;
                byte shift = (byte)(x & 7);

                V256b blockRow = V256B.Load(chunk.ByteBlocks + x * 32 + z * 1024);

                var isAir = blockRow.CompareEqual(V256B.Zero);
                var isSolid = isAir ^ V256B.MaxValue;

                ulong row = VH.GetRowSolidBitsV256X64(isSolid);
                uint bitTop    = neighbours.Blocks22[blockIndex] != 0 ?     1U : 0;
                uint bitBottom = neighbours.Blocks4[blockIndex + 31] != 0 ? 1U : 0;

                row <<= 1;
                row |= (ulong)bitTop << 33;
                row |= (ulong)bitBottom;
                
                *bitPtr = row;
                *aoPtr = (uint)(VH.GetAOTypeMap(row) >> 1);

                V256b rowOffset = V256B.Load(chunk.ByteBlocks + x * 32 + z * 1024 - 1);
                V256b offsetEqual = blockRow.CompareEqual(rowOffset);

                *typePtr = (~VH.GetRowSolidBitsV256X64(offsetEqual)) & 0xFFFFFFFEu;

                V256b ybit = isSolid & V256B.One;

                //var (ybit1, ybit2, ybit3, ybit4) = VH.WidenToV256u(ybit);

                // shift the bits to their place in the map
                /*
                V256u yshift1 = ybit1 << (byte)x;
                V256u yshift2 = ybit2 << (byte)x;
                V256u yshift3 = ybit3 << (byte)x;
                V256u yshift4 = ybit4 << (byte)x;

                yRow1 |= yshift1;
                yRow2 |= yshift2;
                yRow3 |= yshift3;
                yRow4 |= yshift4;
                */

                yrow3 |= ybit.ShiftOneLeft(shift);


                rowTop    |= bitTop    << x;
                rowBottom |= bitBottom << x;


                // handle the type checking for the y axis
                if (x > 0)
                {   
                    // check if block are unequal
                    V256b zequal = blockRow.CompareEqual(mem);

                    // invert the equality
                    V256b znot = zequal ^ V256B.MaxValue;

                    // make it so the values are either 0 or 1
                    V256b zbit = znot & V256B.One;

                    //var (zbit1, zbit2, zbit3, zbit4) = VH.WidenToV256u(zbit);

                    // shift the bits to their place in the map
                    /*
                    V256u zshift1 = zbit1 << (byte)x;
                    V256u zshift2 = zbit2 << (byte)x;
                    V256u zshift3 = zbit3 << (byte)x;
                    V256u zshift4 = zbit4 << (byte)x;

                    zTypeRow1 |= zshift1;
                    zTypeRow2 |= zshift2;
                    zTypeRow3 |= zshift3;
                    zTypeRow4 |= zshift4;
                    */

                    ztype3 |= zbit.ShiftOneLeft(shift);
                }

                mem = blockRow;

                mapIndex++;
                blockIndex += 32;

                bitPtr++;
                aoPtr++;
                typePtr++;  
            }

            for (int x = 24; x < 32; x++)
            {
                int shiftIndex = x >> 3;
                byte shift = (byte)(x & 7);

                V256b blockRow = V256B.Load(chunk.ByteBlocks + x * 32 + z * 1024);

                var isAir = blockRow.CompareEqual(V256B.Zero);
                var isSolid = isAir ^ V256B.MaxValue;

                ulong row = VH.GetRowSolidBitsV256X64(isSolid);
                uint bitTop    = neighbours.Blocks22[blockIndex] != 0 ?     1U : 0;
                uint bitBottom = neighbours.Blocks4[blockIndex + 31] != 0 ? 1U : 0;

                row <<= 1;
                row |= (ulong)bitTop << 33;
                row |= (ulong)bitBottom;
                
                *bitPtr = row;
                *aoPtr = (uint)(VH.GetAOTypeMap(row) >> 1);

                V256b rowOffset = V256B.Load(chunk.ByteBlocks + x * 32 + z * 1024 - 1);
                V256b offsetEqual = blockRow.CompareEqual(rowOffset);

                *typePtr = (~VH.GetRowSolidBitsV256X64(offsetEqual)) & 0xFFFFFFFEu;

                V256b ybit = isSolid & V256B.One;

                //var (ybit1, ybit2, ybit3, ybit4) = VH.WidenToV256u(ybit);

                // shift the bits to their place in the map
                /*
                V256u yshift1 = ybit1 << (byte)x;
                V256u yshift2 = ybit2 << (byte)x;
                V256u yshift3 = ybit3 << (byte)x;
                V256u yshift4 = ybit4 << (byte)x;

                yRow1 |= yshift1;
                yRow2 |= yshift2;
                yRow3 |= yshift3;
                yRow4 |= yshift4;
                */

                yrow4 |= ybit.ShiftOneLeft(shift);


                rowTop    |= bitTop    << x;
                rowBottom |= bitBottom << x;


                // handle the type checking for the y axis
                if (x > 0)
                {   
                    // check if block are unequal
                    V256b zequal = blockRow.CompareEqual(mem);

                    // invert the equality
                    V256b znot = zequal ^ V256B.MaxValue;

                    // make it so the values are either 0 or 1
                    V256b zbit = znot & V256B.One;

                    //var (zbit1, zbit2, zbit3, zbit4) = VH.WidenToV256u(zbit);

                    // shift the bits to their place in the map
                    /*
                    V256u zshift1 = zbit1 << (byte)x;
                    V256u zshift2 = zbit2 << (byte)x;
                    V256u zshift3 = zbit3 << (byte)x;
                    V256u zshift4 = zbit4 << (byte)x;

                    zTypeRow1 |= zshift1;
                    zTypeRow2 |= zshift2;
                    zTypeRow3 |= zshift3;
                    zTypeRow4 |= zshift4;
                    */

                    ztype4 |= zbit.ShiftOneLeft(shift);
                }

                mem = blockRow;

                mapIndex++;
                blockIndex += 32;

                bitPtr++;
                aoPtr++;
                typePtr++;  
            }


            int yIndex = z * 1024;
            int zIndexY = z * 32;

            int rowZindex = (z + 1) * 34;

            
            V256u zTypeRow1 = V256U.Zero;
            V256u zTypeRow2 = V256U.Zero;
            V256u zTypeRow3 = V256U.Zero;
            V256u zTypeRow4 = V256U.Zero;

            var (zTypes11, zTypes12, zTypes13, zTypes14) = VH.WidenToV256u(ztype1);
            var (zTypes21, zTypes22, zTypes23, zTypes24) = VH.WidenToV256u(ztype2);
            var (zTypes31, zTypes32, zTypes33, zTypes34) = VH.WidenToV256u(ztype3);
            var (zTypes41, zTypes42, zTypes43, zTypes44) = VH.WidenToV256u(ztype4);

            zTypeRow1 = zTypes11 | (zTypes21 << 8) | (zTypes31 << 16) | (zTypes41 << 24);
            zTypeRow2 = zTypes12 | (zTypes22 << 8) | (zTypes32 << 16) | (zTypes42 << 24);
            zTypeRow3 = zTypes13 | (zTypes23 << 8) | (zTypes33 << 16) | (zTypes43 << 24);
            zTypeRow4 = zTypes14 | (zTypes24 << 8) | (zTypes34 << 16) | (zTypes44 << 24);

            Avx.Store(zTypeMap + zIndexY,      zTypeRow1);
            Avx.Store(zTypeMap + zIndexY + 8,  zTypeRow2);
            Avx.Store(zTypeMap + zIndexY + 16, zTypeRow3);
            Avx.Store(zTypeMap + zIndexY + 24, zTypeRow4);




            yBitMap[z + 1123]  = 0;//rowTop;
            yBitMap[z + 1]     = 0;//rowBottom;

            yBitMap[rowZindex] = rowBottom;//uint.MaxValue;
            yBitMap[rowZindex + 33] = rowTop;//uint.MaxValue;

            yaoTypeMap[z + 1123]  = 0;//VH.GetAOTypeMap(rowTop);
            yaoTypeMap[z + 1]     = 0;//VH.GetAOTypeMap(rowBottom);

            yaoTypeMap[rowZindex] = (uint)VH.GetAOTypeMap(rowBottom);//uint.MaxValue;
            yaoTypeMap[rowZindex + 33] = (uint)VH.GetAOTypeMap(rowTop);//uint.MaxValue;


            V256u yRow1 = V256U.Zero;
            V256u yRow2 = V256U.Zero;
            V256u yRow3 = V256U.Zero;
            V256u yRow4 = V256U.Zero;

            var (yRows11, yRows12, yRows13, yRows14) = VH.WidenToV256u(yrow1);
            var (yRows21, yRows22, yRows23, yRows24) = VH.WidenToV256u(yrow2);
            var (yRows31, yRows32, yRows33, yRows34) = VH.WidenToV256u(yrow3);
            var (yRows41, yRows42, yRows43, yRows44) = VH.WidenToV256u(yrow4);

            yRow1 = yRows11 | (yRows21 << 8) | (yRows31 << 16) | (yRows41 << 24);
            yRow2 = yRows12 | (yRows22 << 8) | (yRows32 << 16) | (yRows42 << 24);
            yRow3 = yRows13 | (yRows23 << 8) | (yRows33 << 16) | (yRows43 << 24);
            yRow4 = yRows14 | (yRows24 << 8) | (yRows34 << 16) | (yRows44 << 24);

            Avx.Store(yBitMap + rowZindex + 1, yRow1);
            Avx.Store(yBitMap + rowZindex + 9, yRow2);
            Avx.Store(yBitMap + rowZindex + 17, yRow3);
            Avx.Store(yBitMap + rowZindex + 25, yRow4);

            Avx.Store(yaoTypeMap + rowZindex + 1, VH.GetAOTypeMap(yRow1));
            Avx.Store(yaoTypeMap + rowZindex + 9, VH.GetAOTypeMap(yRow2));
            Avx.Store(yaoTypeMap + rowZindex + 17, VH.GetAOTypeMap(yRow3));
            Avx.Store(yaoTypeMap + rowZindex + 25, VH.GetAOTypeMap(yRow4));
            


            int indexNx = 992 + yIndex;
            int indexPx = yIndex;

            int indexNz = zIndexY + maxYlayerIndex;
            int indexPz = zIndexY;

            byte* ptr12 = neighbours.Blocks12 + indexNx;
            byte* ptr14 = neighbours.Blocks14 + indexPx;
            byte* ptr10 = neighbours.Blocks10 + indexNz;
            byte* ptr16 = neighbours.Blocks16 + indexPz;

            // rows in the direction of y, on the plane P (+) / N (-), on the axis x / z

            // handle chunk that is in -x at index 12 and i need the last block
            ulong rowNx = GetSolidRow(ptr12);

            // handle chunk that is in +x at index 14 and i need the first block
            ulong rowPx = GetSolidRow(ptr14);

            // handle chunk that is in -z at index 10 and i need the last block
            ulong rowNz = GetSolidRow(ptr10, z, ref nzRow1, ref nzRow2, ref nzRow3, ref nzRow4);

            // handle chunk that is in +z at index 16 and i need the first block
            ulong rowPz = GetSolidRow(ptr16, z, ref pzRow1, ref pzRow2, ref pzRow3, ref pzRow4);

            bitMap[rowZindex] = rowNx;
            bitMap[33 + rowZindex] = rowPx;

            bitMap[z + 1] = rowNz;
            bitMap[1123 + z] = rowPz;


            aoTypeMap[rowZindex] = (uint)(VH.GetAOTypeMap(rowNx) >> 1);
            aoTypeMap[33 + rowZindex] = (uint)(VH.GetAOTypeMap(rowPx) >> 1);

            aoTypeMap[z + 1] = (uint)(VH.GetAOTypeMap(rowNz) >> 1);
            aoTypeMap[1123 + z] = (uint)(VH.GetAOTypeMap(rowPz) >> 1);
        }

        // store the shifted rows for the z axis
        Avx.Store(yBitMap + 1, nzRow1);
        Avx.Store(yBitMap + 9, nzRow2);
        Avx.Store(yBitMap + 17, nzRow3);
        Avx.Store(yBitMap + 25, nzRow4);

        Avx.Store(yBitMap + 1123, pzRow1);
        Avx.Store(yBitMap + 1131, pzRow2);
        Avx.Store(yBitMap + 1139, pzRow3);
        Avx.Store(yBitMap + 1147, pzRow4);

        // store the ao types
        Avx.Store(yaoTypeMap + 1, VH.GetAOTypeMap(nzRow1));
        Avx.Store(yaoTypeMap + 9, VH.GetAOTypeMap(nzRow2));
        Avx.Store(yaoTypeMap + 17, VH.GetAOTypeMap(nzRow3));
        Avx.Store(yaoTypeMap + 25, VH.GetAOTypeMap(nzRow4));

        Avx.Store(yaoTypeMap + 1123, VH.GetAOTypeMap(pzRow1));
        Avx.Store(yaoTypeMap + 1131, VH.GetAOTypeMap(pzRow2));
        Avx.Store(yaoTypeMap + 1139, VH.GetAOTypeMap(pzRow3));
        Avx.Store(yaoTypeMap + 1147, VH.GetAOTypeMap(pzRow4));
    }


    public static void GetBitMaps3(VoxelChunk chunk, ref NeighbourChunks neighbours, int workerId)
    {
        var meshData =  MeshDatas[workerId];

        ulong* bitMap = meshData.BitMap;
        
        uint* aoTypeMap = meshData.AOTypeMap;
        uint* yaoTypeMap = meshData.YAOTypeMap;
        uint* yBitMap = meshData.YBitMap;

        uint* typeMap = meshData.TypeMap;
        uint* zTypeMap = meshData.ZTypeMap;
        uint* nonSolidMap = meshData.NonSolidMap;

        ulong* topBits = meshData.TopBits;
        ulong* bottomBits = meshData.BottomBits;


        uint* topMask = meshData.RightMask;
        uint* bottomMask = meshData.LeftMask;

        //uint* blockPtr = (uint*)chunk.Blocks;

        int maxYlayerIndex = 31744;

        var nzRows = stackalloc V256b[4];
        var pzRows = stackalloc V256b[4];

        for (int z = 0; z < 32; z++)
        {
            int shiftIndex = z >> 3;
            byte shift = (byte)(z & 7);

            int blockIndex = z * 1024;
            int mapIndex = (z + 1) * 34 + 1;

            /*
            V256u mem1 = V256U.Zero;
            V256u mem2 = V256U.Zero;
            V256u mem3 = V256U.Zero;
            V256u mem4 = V256U.Zero;
            */
            V256b mem = V256B.Zero;


            V256b yrow1 = V256B.Zero;
            V256b yrow2 = V256B.Zero;
            V256b yrow3 = V256B.Zero;
            V256b yrow4 = V256B.Zero;

            V256b ztype1 = V256B.Zero;
            V256b ztype2 = V256B.Zero;
            V256b ztype3 = V256B.Zero;
            V256b ztype4 = V256B.Zero;


            uint rowBottom = 0;
            uint rowTop = 0;

            uint* typePtr     = typeMap;
            ulong* bitPtr     = bitMap + mapIndex;
            uint* aoPtr       = aoTypeMap + mapIndex;

            GetBitRowX0(chunk, ref neighbours, ref blockIndex, 0, z, ref bitPtr, ref aoPtr, ref typePtr, ref yrow1, ref ztype1, ref mem, ref rowTop, ref rowBottom);
            
            for (int x = 1; x < 8; x++)
                GetBitRowX1(chunk, ref neighbours, ref blockIndex, x, z, ref bitPtr, ref aoPtr, ref typePtr, ref yrow1, ref ztype1, ref mem, ref rowTop, ref rowBottom);
            
            for (int x = 8; x < 16; x++)
                GetBitRowX1(chunk, ref neighbours, ref blockIndex, x, z, ref bitPtr, ref aoPtr, ref typePtr, ref yrow2, ref ztype2, ref mem, ref rowTop, ref rowBottom);

            for (int x = 16; x < 24; x++)
                GetBitRowX1(chunk, ref neighbours, ref blockIndex, x, z, ref bitPtr, ref aoPtr, ref typePtr, ref yrow3, ref ztype3, ref mem, ref rowTop, ref rowBottom);

            for (int x = 24; x < 32; x++)
                GetBitRowX1(chunk, ref neighbours, ref blockIndex, x, z, ref bitPtr, ref aoPtr, ref typePtr, ref yrow4, ref ztype4, ref mem, ref rowTop, ref rowBottom);

            int yIndex = z * 1024;
            int zIndexY = z * 32;

            int rowZindex = (z + 1) * 34;

            
            var (zTypeRow1, zTypeRow2, zTypeRow3, zTypeRow4) = VH.InterleaveToV256u(ztype1, ztype2, ztype3, ztype4);

            Avx.Store(zTypeMap + zIndexY,      zTypeRow1);
            Avx.Store(zTypeMap + zIndexY + 8,  zTypeRow2);
            Avx.Store(zTypeMap + zIndexY + 16, zTypeRow3);
            Avx.Store(zTypeMap + zIndexY + 24, zTypeRow4);




            yBitMap[z + 1123]  = 0;//rowTop;
            yBitMap[z + 1]     = 0;//rowBottom;

            yBitMap[rowZindex] = rowBottom;//uint.MaxValue;
            yBitMap[rowZindex + 33] = rowTop;//uint.MaxValue;

            yaoTypeMap[z + 1123]  = 0;//VH.GetAOTypeMap(rowTop);
            yaoTypeMap[z + 1]     = 0;//VH.GetAOTypeMap(rowBottom);

            yaoTypeMap[rowZindex] = (uint)VH.GetAOTypeMap(rowBottom);//uint.MaxValue;
            yaoTypeMap[rowZindex + 33] = (uint)VH.GetAOTypeMap(rowTop);//uint.MaxValue;


            var (yRow1, yRow2, yRow3, yRow4) = VH.InterleaveToV256u(yrow1, yrow2, yrow3, yrow4);

            Avx.Store(yBitMap + rowZindex + 1, yRow1);
            Avx.Store(yBitMap + rowZindex + 9, yRow2);
            Avx.Store(yBitMap + rowZindex + 17, yRow3);
            Avx.Store(yBitMap + rowZindex + 25, yRow4);

            Avx.Store(yaoTypeMap + rowZindex + 1, VH.GetAOTypeMap(yRow1));
            Avx.Store(yaoTypeMap + rowZindex + 9, VH.GetAOTypeMap(yRow2));
            Avx.Store(yaoTypeMap + rowZindex + 17, VH.GetAOTypeMap(yRow3));
            Avx.Store(yaoTypeMap + rowZindex + 25, VH.GetAOTypeMap(yRow4));
            


            int indexNx = 992 + yIndex;
            int indexPx = yIndex;

            int indexNz = zIndexY + maxYlayerIndex;
            int indexPz = zIndexY;

            byte* ptr12 = neighbours.Blocks12 + indexNx;
            byte* ptr14 = neighbours.Blocks14 + indexPx;
            byte* ptr10 = neighbours.Blocks10 + indexNz;
            byte* ptr16 = neighbours.Blocks16 + indexPz;

            // rows in the direction of y, on the plane P (+) / N (-), on the axis x / z

            // handle chunk that is in -x at index 12 and i need the last block
            ulong rowNx = GetSolidRow(ptr12);

            // handle chunk that is in +x at index 14 and i need the first block
            ulong rowPx = GetSolidRow(ptr14);

            // handle chunk that is in -z at index 10 and i need the last block
            ulong rowNz = GetSolidRow(ptr10, z, shift, ref nzRows[shiftIndex]);

            // handle chunk that is in +z at index 16 and i need the first block
            ulong rowPz = GetSolidRow(ptr16, z, shift, ref pzRows[shiftIndex]);

            bitMap[rowZindex] = rowNx;
            bitMap[33 + rowZindex] = rowPx;

            bitMap[z + 1] = rowNz;
            bitMap[1123 + z] = rowPz;


            aoTypeMap[rowZindex] = (uint)(VH.GetAOTypeMap(rowNx) >> 1);
            aoTypeMap[33 + rowZindex] = (uint)(VH.GetAOTypeMap(rowPx) >> 1);

            aoTypeMap[z + 1] = (uint)(VH.GetAOTypeMap(rowNz) >> 1);
            aoTypeMap[1123 + z] = (uint)(VH.GetAOTypeMap(rowPz) >> 1);
        }

        var (nzRow1, nzRow2, nzRow3, nzRow4) = VH.InterleaveToV256u(nzRows[0], nzRows[1], nzRows[2], nzRows[3]);
        var (pzRow1, pzRow2, pzRow3, pzRow4) = VH.InterleaveToV256u(pzRows[0], pzRows[1], pzRows[2], pzRows[3]);

        // store the shifted rows for the z axis
        Avx.Store(yBitMap + 1, nzRow1);
        Avx.Store(yBitMap + 9, nzRow2);
        Avx.Store(yBitMap + 17, nzRow3);
        Avx.Store(yBitMap + 25, nzRow4);

        Avx.Store(yBitMap + 1123, pzRow1);
        Avx.Store(yBitMap + 1131, pzRow2);
        Avx.Store(yBitMap + 1139, pzRow3);
        Avx.Store(yBitMap + 1147, pzRow4);

        // store the ao types
        Avx.Store(yaoTypeMap + 1, VH.GetAOTypeMap(nzRow1));
        Avx.Store(yaoTypeMap + 9, VH.GetAOTypeMap(nzRow2));
        Avx.Store(yaoTypeMap + 17, VH.GetAOTypeMap(nzRow3));
        Avx.Store(yaoTypeMap + 25, VH.GetAOTypeMap(nzRow4));

        Avx.Store(yaoTypeMap + 1123, VH.GetAOTypeMap(pzRow1));
        Avx.Store(yaoTypeMap + 1131, VH.GetAOTypeMap(pzRow2));
        Avx.Store(yaoTypeMap + 1139, VH.GetAOTypeMap(pzRow3));
        Avx.Store(yaoTypeMap + 1147, VH.GetAOTypeMap(pzRow4));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetSolidRow(byte* ptr)
    {
        V256b blockRow = V256B.Load(ptr);

        var isAir = blockRow.CompareEqual(V256B.Zero);
        var isSolid = isAir ^ V256B.MaxValue;

        return (ulong)VH.GetRowSolidBitsV256X64(isSolid) << 1;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetSolidRow(byte* ptr, int z, byte shift, ref V256b sRow)
    {
        V256b blockRow = V256B.Load(ptr);

        var isAir = blockRow.CompareEqual(V256B.Zero);
        var isSolid = isAir ^ V256B.MaxValue;

        V256b ybit = isSolid & V256B.One;

        sRow |= ybit.ShiftOneLeft(shift);

        return (ulong)VH.GetRowSolidBitsV256X64(isSolid) << 1;
    }

    private static ulong GetSolidRow(byte* ptr, int z, ref V256u sRow1, ref V256u sRow2, ref V256u sRow3, ref V256u sRow4)
    {
        V256b blockRow = V256B.Load(ptr);

        var (row1, row2, row3, row4) = VH.WidenToV256u(blockRow);

       /*
        var state1 = row1 & StateMaskV256;
        var state2 = row2 & StateMaskV256;
        var state3 = row3 & StateMaskV256;
        var state4 = row4 & StateMaskV256;

        var isSolid1 = state1.CompareEqual(SolidMaskV256);
        var isSolid2 = state2.CompareEqual(SolidMaskV256);
        var isSolid3 = state3.CompareEqual(SolidMaskV256);
        var isSolid4 = state4.CompareEqual(SolidMaskV256);
        */

        var isAir1 = row1.CompareEqual(V256U.Zero);
        var isAir2 = row2.CompareEqual(V256U.Zero);
        var isAir3 = row3.CompareEqual(V256U.Zero);
        var isAir4 = row4.CompareEqual(V256U.Zero);

        var isSolid1 = isAir1 ^ V256U.MaxValue;
        var isSolid2 = isAir2 ^ V256U.MaxValue;
        var isSolid3 = isAir3 ^ V256U.MaxValue;
        var isSolid4 = isAir4 ^ V256U.MaxValue;

        // row shift
        V256u ybit1 = isSolid1 & V256U.One;
        V256u ybit2 = isSolid2 & V256U.One;
        V256u ybit3 = isSolid3 & V256U.One;
        V256u ybit4 = isSolid4 & V256U.One;

        // shift the bits to their place in the map
        V256u yshift1 = ybit1 << (byte)z;
        V256u yshift2 = ybit2 << (byte)z;
        V256u yshift3 = ybit3 << (byte)z;
        V256u yshift4 = ybit4 << (byte)z;

        sRow1 |= yshift1;
        sRow2 |= yshift2;
        sRow3 |= yshift3;
        sRow4 |= yshift4;

        return (ulong)VH.GetRowSolidBitsV256X64(isSolid1, isSolid2, isSolid3, isSolid4) << 1;
    }

    private static void BuildMesh(VoxelChunk chunk, ref MeshMapping mapping, int workerId)
    {
        var meshData = MeshDatas[workerId];
        
        ulong* bitMap = meshData.BitMap;
        
        uint* aoTypeMap = meshData.AOTypeMap;
        uint* yaoTypeMap = meshData.YAOTypeMap;
        uint* yBitMap = meshData.YBitMap;

        uint* typeMap = meshData.TypeMap;
        uint* zTypeMap = meshData.ZTypeMap;
        uint* nonSolidMap = meshData.NonSolidMap;

        ulong* topBits = meshData.TopBits;
        ulong* bottomBits = meshData.BottomBits;
        

        uint* frontSlice = meshData.FrontSlice;
        uint* backSlice = meshData.BackSlice;

        uint* topSlice = meshData.TopSlice;
        uint* bottomSlice = meshData.BottomSlice;

        uint* rightSlice = meshData.RightSlice;
        uint* leftSlice = meshData.LeftSlice;

        for (int i = 0; i < 32; i++)
        {  
            int iy = (i + 1) * 32;
            int iz = (i + 1) * 34;

            for (int s = 0; s < 32; s++)
            {
                int sz = (s + 1) * 34;
                int si = i + 1 + sz;

                frontSlice[s]   = (uint)(bitMap[iz + s + 1] >> 1);
                backSlice[s]    = (uint)(bitMap[iz + s + 1] >> 1);

                rightSlice[s]   = (uint)(bitMap[si] >> 1);
                leftSlice[s]    = (uint)(bitMap[si] >> 1);

                topSlice[s]     = yBitMap[si];
                bottomSlice[s]  = yBitMap[si];
            }

            HandleGreedyFrontAndBack(chunk, ref mapping, bitMap, aoTypeMap, typeMap, frontSlice, backSlice, i);
            HandleGreedyRightAndLeft(chunk, ref mapping, bitMap, aoTypeMap, typeMap, rightSlice, leftSlice, i);
            HandleGreedyTopAndBottom(chunk, ref mapping, yBitMap, yaoTypeMap, zTypeMap, topSlice, bottomSlice, i);
        }
    }

    
    public static bool GenerateMesh(VoxelChunk chunk, ref MeshMapping mapping, int workerId)
    {
        NeighbourChunks neighbours = new(chunk, chunk.RelativePosition);
        GetBitMaps(chunk, ref neighbours, workerId);
        BuildMesh(chunk, ref mapping, workerId);   
        return true;
    }

    public static void HandleGreedyFrontAndBack(VoxelChunk chunk, ref MeshMapping mapping, ulong* bitMap, uint* aoTypeMap, uint* typeMap, uint* frontData, uint* backData, int z)
    {
        int frontZindex = z * 34;
        int backZindex = (z + 2) * 34;

        ulong* frontBitMapPtr = bitMap + frontZindex;
        uint *frontAoTypeMapPtr = aoTypeMap + frontZindex;
        uint *frontTypeMapPtr = typeMap + z * 32;

        uint frontAoType1 = frontAoTypeMapPtr[0];
        uint frontAoType2 = frontAoTypeMapPtr[1];

        ulong frontFront1 = frontBitMapPtr[0];
        ulong frontFront2 = frontBitMapPtr[1];

        ulong* backBitMapPtr = bitMap + backZindex;
        uint* backAoTypeMapPtr = aoTypeMap + backZindex;
        uint* backTypeMapPtr = typeMap + z * 32;

        uint backAoType1 = backAoTypeMapPtr[0];
        uint backAoType2 = backAoTypeMapPtr[1];

        ulong backFront1 = backBitMapPtr[0];
        ulong backFront2 = backBitMapPtr[1];

        byte* blockPtr = chunk.ByteBlocks + z * 1024;

        for (int i = 0; i < 32; i++)
        {
            // Front
            {
                uint aoType3 = frontAoTypeMapPtr[i + 2];
                ulong front3 = frontBitMapPtr[i + 2];

                uint type = frontTypeMapPtr[i] | frontAoType1 | frontAoType2 | aoType3;
                uint row = frontData[i] & (uint)((~frontFront2) >> 1);

                while (row != 0)
                {
                    int trailingZeros = Bit.TrailingZeros(row);

                    uint newRow = row >> trailingZeros;
                    uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                    int h = Bit.TrailingZeros((~newRow) | newType);

                    uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                    row &= ~mask;
                    type &= ~mask;

                    int w = 1;

                    byte block = blockPtr[trailingZeros + i * 32];
                                    
                    int ao = VH.GetAO(frontFront1, frontFront2, front3, trailingZeros);

                    uint sAoType1 = frontAoType2;
                    uint sAoType2 = aoType3;

                    ulong sFront1 = frontFront2;
                    ulong sFront2 = front3;

                    while (w < 32 - i)
                    {
                        int iw = i + w; // base iw is i + 1

                        uint sAoType3 = frontAoTypeMapPtr[iw + 2];
                        ulong sFront3 = frontBitMapPtr[iw + 2];

                        uint sType = frontTypeMapPtr[iw] | sAoType1 | sAoType2 | sAoType3;
                        uint sRow = frontData[iw] & (uint)((~sFront2) >> 1);

                        sRow &= ~(sType & Bit.Invert(trailingZeros));

                        int sAo = VH.GetAO(sFront1, sFront2, sFront3, trailingZeros);
                        byte sBlock = blockPtr[trailingZeros + iw * 32];

                        if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                            break;

                        frontData[iw] &= ~mask;
                        w++;

                        sAoType1 = sAoType2;
                        sAoType2 = sAoType3;

                        sFront1 = sFront2;
                        sFront2 = sFront3;
                    }

                    w--;
                    h--;

                    uint geometryIndex = (uint)Block.GetSolidGeometryIndex(block, 0);
                    uint pos = (uint)(i | (trailingZeros << 5) | (z << 10));
                    uint side = 0;
                    
                    uint size = (uint)(w | (h << 5));
                    uint packedAo = (uint)VH.GetPackedAO2(ao);
                    
                    uint vx = geometryIndex | (pos << 14) | (side << 29);
                    uint vy = size | (packedAo << 10);

                    mapping.AddFace(new(vx, vy));
                }

                frontAoType1 = frontAoType2;
                frontAoType2 = aoType3;

                frontFront1 = frontFront2;
                frontFront2 = front3;
            }

            // Back
            {
                uint aoType3 = backAoTypeMapPtr[i + 2];
                ulong front3 = backBitMapPtr[i + 2];

                uint type = backTypeMapPtr[i] | backAoType1 | backAoType2 | aoType3;
                uint row = backData[i] & (uint)((~backFront2) >> 1);

                while (row != 0)
                {
                    int trailingZeros = Bit.TrailingZeros(row);

                    uint newRow = row >> trailingZeros;
                    uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                    int h = Bit.TrailingZeros((~newRow) | newType);

                    uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                    row &= ~mask;
                    type &= ~mask;

                    int w = 1;

                    byte block = blockPtr[trailingZeros + i * 32];
                                    
                    int ao = VH.GetAO(front3, backFront2, backFront1, trailingZeros);

                    uint sAoType1 = backAoType2;
                    uint sAoType2 = aoType3;

                    ulong sFront1 = backFront2;
                    ulong sFront2 = front3;

                    while (w < 32 - i)
                    {
                        int iw = i + w;

                        uint sAoType3 = backAoTypeMapPtr[iw + 2];
                        ulong sFront3 = backBitMapPtr[iw + 2];

                        uint sType = backTypeMapPtr[iw] | sAoType1 | sAoType2 | sAoType3;
                        uint sRow = backData[iw] & (uint)((~sFront2) >> 1);

                        sRow &= ~(sType & Bit.Invert(trailingZeros));

                        int sAo = VH.GetAO(sFront3, sFront2, sFront1, trailingZeros);
                        byte sBlock = blockPtr[trailingZeros + iw * 32];

                        if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                            break;

                        backData[iw] &= ~mask;
                        w++;

                        sAoType1 = sAoType2;
                        sAoType2 = sAoType3;

                        sFront1 = sFront2;
                        sFront2 = sFront3;
                    }

                    w--;
                    h--;

                    uint geometryIndex = (uint)Block.GetSolidGeometryIndex(block, 5);
                    uint pos = (uint)(i | (trailingZeros << 5) | (z << 10));
                    uint side = 5;
                    
                    uint size = (uint)(w | (h << 5));
                    uint packedAo = (uint)VH.GetPackedAO2(ao);
                    
                    uint vx = geometryIndex | (pos << 14) | (side << 29);
                    uint vy = size | (packedAo << 10);

                    mapping.AddFace(new(vx, vy));
                }

                backAoType1 = backAoType2;
                backAoType2 = aoType3;

                backFront1 = backFront2;
                backFront2 = front3;
            }
        }
    }


    public static void HandleGreedyRightAndLeft(VoxelChunk chunk, ref MeshMapping mapping, ulong* bitMap, uint* aoTypeMap, uint* typeMap, uint* rightData, uint* leftData, int x)
    {
        uint rightAoType1 = aoTypeMap[x + 2];
        uint rightAoType2 = aoTypeMap[x + 2 + 34];

        ulong rightFront1 = bitMap[x + 2];
        ulong rightFront2 = bitMap[x + 2 + 34];

        uint leftAoType1 = aoTypeMap[x];
        uint leftAoType2 = aoTypeMap[x + 34];

        ulong leftFront1 = bitMap[x];
        ulong leftFront2 = bitMap[x + 34];

        for (int i = 0; i < 32; i++)
        {
            // Right
            {
                uint aoType3 = aoTypeMap[x + 2 + (i + 2) * 34];
                ulong front3 = bitMap[x + 2 + (i + 2) * 34];

                uint type = typeMap[x + i * 32] | rightAoType1 | rightAoType2 | aoType3;
                uint row = leftData[i] & (uint)((~rightFront2) >> 1);

                while (row != 0)
                {
                    int trailingZeros = Bit.TrailingZeros(row);

                    uint newRow = row >> trailingZeros;
                    uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                    int h = Bit.TrailingZeros((~newRow) | newType);

                    uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                    row &= ~mask;
                    type &= ~mask;

                    int w = 1;

                    Block block = chunk.Get(trailingZeros + x * 32 + i * 1024);

                    int ao = VH.GetAO(rightFront1, rightFront2, front3 ,trailingZeros);

                    uint sAoType1 = rightAoType2;
                    uint sAoType2 = aoType3;

                    ulong sFront1 = rightFront2;
                    ulong sFront2 = front3;
                    
                    while (w < 32 - i)
                    {
                        int iw = i + w;

                        uint sAoType3 = aoTypeMap[x + 2 + (i + 2 + w) * 34];
                        ulong sFront3 = bitMap[x + 2 + (i + 2 + w) * 34];

                        uint sType = typeMap[x + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                        uint sRow = leftData[iw] & (uint)((~sFront2) >> 1);

                        sRow &= ~(sType & Bit.Invert(trailingZeros));

                        int sAo = VH.GetAO(sFront1, sFront2, sFront3, trailingZeros);
                        Block sBlock = chunk.Get(trailingZeros + x * 32 + iw * 1024);

                        if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                            break;

                        leftData[iw] &= ~mask;
                        w++;

                        sAoType1 = sAoType2;
                        sAoType2 = sAoType3;

                        sFront1 = sFront2;
                        sFront2 = sFront3;
                    }

                    w--;
                    h--;

                    uint geometryIndex = (uint)block.GetSolidGeometryIndex(1);
                    uint pos = (uint)(x | (trailingZeros << 5) | (i << 10));
                    uint side = 1;
                    
                    uint size = (uint)(w | (h << 5));
                    uint packedAo = (uint)VH.GetPackedAO2(ao);
                    
                    uint vx = geometryIndex | (pos << 14) | (side << 29);
                    uint vy = size | (packedAo << 10);

                    mapping.AddFace(new(vx, vy));
                }

                rightAoType1 = rightAoType2;
                rightAoType2 = aoType3;

                rightFront1 = rightFront2;
                rightFront2 = front3;
            }

            // Left
            {
                uint aoType3 = (uint)aoTypeMap[x + (i + 2) * 34];
                ulong front3 = bitMap[x + (i + 2) * 34];

                uint type = typeMap[x + i * 32] | leftAoType1 | leftAoType2 | aoType3;
                uint row = rightData[i] & (uint)((~leftFront2) >> 1);

                while (row != 0)
                {
                    int trailingZeros = Bit.TrailingZeros(row);

                    uint newRow = row >> trailingZeros;
                    uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                    int h = Bit.TrailingZeros((~newRow) | newType);

                    uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                    row &= ~mask;
                    type &= ~mask;

                    int w = 1;

                    Block block = chunk.Get(trailingZeros + x * 32 + i * 1024);

                    int ao = VH.GetAO(front3, leftFront2, leftFront1, trailingZeros);

                    uint sAoType1 = leftAoType2;
                    uint sAoType2 = aoType3;

                    ulong sFront1 = leftFront2;
                    ulong sFront2 = front3;

                    while (w < 32 - i)
                    {
                        int iw = i + w;

                        uint sAoType3 = (uint)aoTypeMap[x + (i + 2 + w) * 34];
                        ulong sFront3 = bitMap[x + (i + 2 + w) * 34];

                        uint sType = typeMap[x + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                        uint sRow = rightData[iw] & (uint)((~sFront2) >> 1);

                        sRow &= ~(sType & Bit.Invert(trailingZeros));

                        int sAo = VH.GetAO(sFront3, sFront2, sFront1, trailingZeros);
                        Block sBlock = chunk.Get(trailingZeros + x * 32 + iw * 1024);

                        if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                            break;

                        rightData[iw] &= ~mask;
                        w++;

                        sAoType1 = sAoType2;
                        sAoType2 = sAoType3;

                        sFront1 = sFront2;
                        sFront2 = sFront3;
                    }

                    w--;
                    h--;

                    uint geometryIndex = (uint)block.GetSolidGeometryIndex(3);
                    uint pos = (uint)(x | (trailingZeros << 5) | (i << 10));
                    uint side = 3;
                    
                    uint size = (uint)(w | (h << 5));
                    uint packedAo = (uint)VH.GetPackedAO2(ao);
                    
                    uint vx = geometryIndex | (pos << 14) | (side << 29);
                    uint vy = size | (packedAo << 10);

                    mapping.AddFace(new(vx, vy));
                }

                leftAoType1 = leftAoType2;
                leftAoType2 = aoType3;

                leftFront1 = leftFront2;
                leftFront2 = front3;
            }
        }
    }



    public static void HandleGreedyTopAndBottom(VoxelChunk chunk, ref MeshMapping mapping, uint* bitMap, uint* aoTypeMap, uint* typeMap, uint* topData, uint* bottomData, int y)
    {
        uint topAoType1 = aoTypeMap[y + 2];
        uint topAoType2 = aoTypeMap[y + 2 + 34];

        ulong topFront1 = (ulong)bitMap[y + 2] << 1;
        ulong topFront2 = (ulong)bitMap[y + 2 + 34] << 1;

        uint bottomAoType1 = aoTypeMap[y];
        uint bottomAoType2 = aoTypeMap[y + 34];

        ulong bottomFront1 = (ulong)bitMap[y] << 1;
        ulong bottomFront2 = (ulong)bitMap[y + 34] << 1;

        for (int i = 0; i < 32; i++)
        {
            // Top
            {
                uint aoType3 = aoTypeMap[y + 2 + (i + 2) * 34];
                ulong front3 = (ulong)bitMap[y + 2 + (i + 2) * 34] << 1;

                uint type = typeMap[y + i * 32] | topAoType1 | topAoType2 | aoType3;
                uint row = topData[i] & (uint)((~topFront2) >> 1);

                while (row != 0)
                {
                    int trailingZeros = Bit.TrailingZeros(row);

                    uint newRow = row >> trailingZeros;
                    uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                    int h = Bit.TrailingZeros((~newRow) | newType);

                    uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                    row &= ~mask;
                    type &= ~mask;

                    int w = 1;

                    Block block = chunk.Get(y + trailingZeros * 32 + i * 1024);

                    int ao = VH.GetAO(topFront1, topFront2, front3, trailingZeros);

                    uint sAoType1 = topAoType2;
                    uint sAoType2 = aoType3;

                    ulong sFront1 = topFront2;
                    ulong sFront2 = front3;

                    while (w < 32 - i)
                    {
                        int iw = i + w;

                        uint sAoType3 = aoTypeMap[y + 2 + (iw + 2) * 34];
                        ulong sFront3 = (ulong)bitMap[y + 2 + (iw + 2) * 34] << 1;

                        uint sType = typeMap[y + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                        uint sRow = topData[iw] & (uint)((~sFront2) >> 1);

                        sRow &= ~(sType & Bit.Invert(trailingZeros));

                        int sAo = VH.GetAO(sFront1, sFront2, sFront3, trailingZeros);
                        Block sBlock = chunk.Get(y + trailingZeros * 32 + iw * 1024);

                        if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                            break;

                        topData[iw] &= ~mask;
                        w++;

                        sAoType1 = sAoType2;
                        sAoType2 = sAoType3;

                        sFront1 = sFront2;
                        sFront2 = sFront3;
                    }

                    w--;
                    h--;

                    uint geometryIndex = (uint)block.GetSolidGeometryIndex(2);
                    uint pos = (uint)(trailingZeros | (y << 5) | (i << 10));
                    uint side = 2;
                    
                    uint size = (uint)(w | (h << 5));
                    uint packedAo = (uint)VH.GetPackedAO2(ao);
                    
                    uint vx = geometryIndex | (pos << 14) | (side << 29);
                    uint vy = size | (packedAo << 10);

                    mapping.AddFace(new(vx, vy));
                }

                topAoType1 = topAoType2;
                topAoType2 = aoType3;

                topFront1 = topFront2;
                topFront2 = front3;
            }

            // Bottom
            {
                uint aoType3 = aoTypeMap[y + (i + 2) * 34];
                ulong front3 = (ulong)bitMap[y + (i + 2) * 34] << 1;
                
                uint type = typeMap[y + i * 32] | bottomAoType1 | bottomAoType2 | aoType3;
                uint row = bottomData[i] & (uint)((~bottomFront2) >> 1);

                while (row != 0)
                {
                    int trailingZeros = Bit.TrailingZeros(row);

                    uint newRow = row >> trailingZeros;
                    uint newType = (type >> trailingZeros) & Bit.INVERTED_ONE_MASK;

                    int h = Bit.TrailingZeros((~newRow) | newType);

                    uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                    row &= ~mask;
                    type &= ~mask;

                    int w = 1;

                    Block block = chunk.Get(y + trailingZeros * 32 + i * 1024);

                    int ao = VH.GetFlippedAO(bottomFront1, bottomFront2, front3, trailingZeros);

                    uint sAoType1 = bottomAoType2;
                    uint sAoType2 = aoType3;

                    ulong sFront1 = bottomFront2;
                    ulong sFront2 = front3;
                    
                    while (w < 32 - i)
                    {
                        int iw = i + w;

                        uint sAoType3 = aoTypeMap[y + (iw + 2) * 34];
                        ulong sFront3 = (ulong)bitMap[y + (iw + 2) * 34] << 1;

                        uint sType = typeMap[y + iw * 32] | sAoType1 | sAoType2 | sAoType3;
                        uint sRow = bottomData[iw] & (uint)((~sFront2) >> 1);

                        sRow &= ~(sType & Bit.Invert(trailingZeros));

                        int sAo = VH.GetFlippedAO(sFront1, sFront2, sFront3, trailingZeros);
                        Block sBlock = chunk.Get(y + trailingZeros * 32 + iw * 1024);

                        if ((sRow & mask) != mask || ao != sAo || block != sBlock)
                            break;

                        bottomData[iw] &= ~mask;
                        w++;

                        sAoType1 = sAoType2;
                        sAoType2 = sAoType3;

                        sFront1 = sFront2;
                        sFront2 = sFront3;
                    }

                    w--;
                    h--;

                    uint geometryIndex = (uint)block.GetSolidGeometryIndex(4);
                    uint pos = (uint)(trailingZeros | (y << 5) | (i << 10));
                    uint side = 4;
                    
                    uint size = (uint)(w | (h << 5));
                    uint packedAo = (uint)VH.GetPackedAO2(ao);
                    
                    uint vx = geometryIndex | (pos << 14) | (side << 29);
                    uint vy = size | (packedAo << 10);

                    mapping.AddFace(new(vx, vy));
                }

                bottomAoType1 = bottomAoType2;
                bottomAoType2 = aoType3;

                bottomFront1 = bottomFront2;
                bottomFront2 = front3;
            }
        }
    }



    private static ulong ExtractBitMap(ulong* ptr, byte shift) =>
        (ulong)VH.ExtractBitsX64(ptr + 0,  shift)       | 
        (ulong)VH.ExtractBitsX64(ptr + 8,  shift) << 8  | 
        (ulong)VH.ExtractBitsX64(ptr + 16, shift) << 16 | 
        (ulong)VH.ExtractBitsX64(ptr + 24, shift) << 24 |
        (ulong)VH.ExtractBits(   ptr + 32, shift) << 32;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Copy32(uint* src, uint* dst) => Buffer.MemoryCopy(src, dst, 128, 128);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Copy(uint* src, uint* dst, ulong srcByteSize, ulong dstByteSize) => Buffer.MemoryCopy(src, dst, dstByteSize, srcByteSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Copy(ulong* src, ulong* dst, ulong srcByteSize, ulong dstByteSize) => Buffer.MemoryCopy(src, dst, dstByteSize, srcByteSize);


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
            Blocks0  = GetBlocks(chunk, relativePosition, -1, -1, -1);
            Blocks1  = GetBlocks(chunk, relativePosition,  0, -1, -1);
            Blocks2  = GetBlocks(chunk, relativePosition,  1, -1, -1);

            Blocks3  = GetBlocks(chunk, relativePosition, -1, -1,  0);
            Blocks4  = GetBlocks(chunk, relativePosition,  0, -1,  0);
            Blocks5  = GetBlocks(chunk, relativePosition,  1, -1,  0);

            Blocks6  = GetBlocks(chunk, relativePosition, -1, -1,  1);
            Blocks7  = GetBlocks(chunk, relativePosition,  0, -1,  1);
            Blocks8  = GetBlocks(chunk, relativePosition,  1, -1,  1);

            Blocks9  = GetBlocks(chunk, relativePosition, -1,  0, -1);
            Blocks10 = GetBlocks(chunk, relativePosition,  0,  0, -1);
            Blocks11 = GetBlocks(chunk, relativePosition,  1,  0, -1);

            Blocks12 = GetBlocks(chunk, relativePosition, -1,  0,  0);
            Blocks14 = GetBlocks(chunk, relativePosition,  1,  0,  0);

            Blocks15 = GetBlocks(chunk, relativePosition, -1,  0,  1);
            Blocks16 = GetBlocks(chunk, relativePosition,  0,  0,  1);
            Blocks17 = GetBlocks(chunk, relativePosition,  1,  0,  1);

            Blocks18 = GetBlocks(chunk, relativePosition, -1,  1, -1);
            Blocks19 = GetBlocks(chunk, relativePosition,  0,  1, -1);
            Blocks20 = GetBlocks(chunk, relativePosition,  1,  1, -1);

            Blocks21 = GetBlocks(chunk, relativePosition, -1,  1,  0);
            Blocks22 = GetBlocks(chunk, relativePosition,  0,  1,  0);
            Blocks23 = GetBlocks(chunk, relativePosition,  1,  1,  0);

            Blocks24 = GetBlocks(chunk, relativePosition, -1,  1,  1);
            Blocks25 = GetBlocks(chunk, relativePosition,  0,  1,  1);
            Blocks26 = GetBlocks(chunk, relativePosition,  1,  1,  1);
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