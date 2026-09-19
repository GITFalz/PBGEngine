using System.Runtime.CompilerServices;
using PBG.MathLibrary;
using Silk.NET.OpenGL;

namespace PBG.NewVoxel
{
    public struct Block
    {
        public const uint ID_MASK = 0x00003FFF;         // binary: 0000 0000 0000 0000 0011 1111 1111 1111


        public const int VARIANT_SHIFT = 14;
        public const uint VARIANT_MASK = 0x001FC000;        // binary: 0000 0000 0001 1111 1100 0000 0000 0000
        public const uint INV_VARIANT_MASK = ~VARIANT_MASK; // binary: 1111 1111 1110 0000 0011 1111 1111 1111


        public const int  STATE_SHIFT = 28;
        public const uint STATE_MASK = 0xF0000000;      // binary: 1111 0000 0000 0000 0000 0000 0000 0000
        public const uint INV_STATE_MASK = ~STATE_MASK; // binary: 0000 1111 1111 1111 1111 1111 1111 1111

        public static Block Air = new Block(BlockState.Air, 0);

        public uint blockData = 0;
        public readonly uint ID => BlockId();

        public Block() { }
        public Block(uint blockData) : this(BlockState.Solid, blockData) { }
        public Block(BlockState blockState, uint blockData)
        {
            this.blockData = blockData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly uint BlockId() => blockData & ID_MASK; // 0b 0000 0000 0000 0000 1111 1111 1111 1111

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBlockId(ushort id) => blockData = (blockData & ~ID_MASK) | id; // 0b 0000 0000 0000 0000 1111 1111 1111 1111



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int GetVariant() => (int)(blockData & VARIANT_MASK) >> VARIANT_SHIFT;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVariant(int variant) => blockData = (blockData & INV_VARIANT_MASK) | (((uint)variant << VARIANT_SHIFT) & VARIANT_MASK);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int GetVariantIndex() => BlockData.VariantStartIndices[BlockId()] + GetVariant();


        public void ClearRotation() { } //blockData &= ~ROTATION_MASK;
        public void SetRotation(uint rotation)
        {
            /*
            rotation &= 0xFF;
            ClearRotation();
            blockData |= rotation << 16;
            */
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Rotation() => 0; //(blockData & ROTATION_MASK) >> 16;

        


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly uint State() => (blockData & STATE_MASK) >> STATE_SHIFT;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly uint GetState() => (blockData & STATE_MASK) >> STATE_SHIFT;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetState(uint state) => blockData = (blockData & INV_STATE_MASK) | ((state << STATE_SHIFT) & STATE_MASK);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsAir() => (blockData & ID_MASK) == 0; //State() == 0;




        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BlockDefinition Definition() => BlockData.BlockDefinitions[Math.Min(ID, BlockData.BLOCK_COUNT)];
        
        /// <summary>
        /// Only call when you are sure the block is solid
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int GetSolidGeometryIndex(int side)
        {
            /*
            ref var faceIndices = ref BlockData.VoxelFaceIndices[BlockId()];
            return BlockData.VoxelGeometryIndices[faceIndices.Start + side];
            */

            return BlockData.SolidVoxelGeometryIndices[Math.Min(ID, BlockData.BLOCK_COUNT)] + side * 4;
        }

        public static bool operator ==(Block a, Block b) => a.blockData == b.blockData;
        public static bool operator !=(Block a, Block b) => a.blockData != b.blockData;

        public override string ToString()
        {
            return $"Block: {BlockId()}, State: {State()}, Rotation: {Rotation()}";
        }
    }

    public enum BlockState
    {
        Air = 0,
        Solid = 1,
        Liquid = 2,
    }
}