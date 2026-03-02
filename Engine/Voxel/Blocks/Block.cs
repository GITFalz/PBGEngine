namespace PBG.Voxel
{
    public struct Block
    {
        // Structure of block data: |0000|   00   00|00   00|0000  |0000|0000|0000|0000|
        //                          ^-----^ ^--^ ^-----^ ^-------^ ^-------------------^
        // 5 bits, unused
        // 2 bit, state (0 = air, 1 = solid, 2 = liquid) (to be expanded)
        // 3 bits, rotation
        // 6 bits, occlusion
        // 16 bits, block id       

        public const uint ID_MASK = 0x0000FFFF;         // binary: 0000 0000 0000 0000 1111 1111 1111 1111
        public const uint ROTATION_MASK = 0x00FF0000;   // binary: 0000 0000 1111 1111 0000 0000 0000 0000
        public const uint STATE_MASK = 0x0C000000;      // binary: 0000 1100 0000 0000 0000 0000 0000 0000

        public static Block Air = new Block(BlockState.Air, 0);

        public uint blockData = 0;
        public uint ID => BlockId();

        public Block() { }
        public Block(uint blockData) : this(BlockState.Air, blockData) { }
        public Block(BlockState blockState, uint blockData)
        {
            this.blockData = blockData;
            switch (blockState)
            {
                case BlockState.Air:
                    SetAir();
                    break;
                case BlockState.Solid:
                    SetSolid();
                    break;
                case BlockState.Liquid:
                    SetLiquid();
                    break;
            }
        }

        public uint BlockId()
        {
            return blockData & ID_MASK; // 0b 0000 0000 0000 0000 1111 1111 1111 1111
        }

        public void SetBlockId(ushort id)
        {
            blockData = (blockData & ~ID_MASK) | id; // 0b 0000 0000 0000 0000 1111 1111 1111 1111
        }

        public bool Equal(Block block)
        {
            return BlockId() == block.BlockId();
        }

        public bool Equal(short blockId)
        {
            return BlockId() == blockId;
        }

        public void ClearRotation()
        {
            blockData &= ~ROTATION_MASK;
        }

        public void SetRotation(uint rotation)
        {
            rotation &= 0xFF;
            ClearRotation();
            blockData |= rotation << 16;
        }

        public uint Rotation()
        {
            return (blockData & ROTATION_MASK) >> 16;
        }

        public uint State()
        {
            return (blockData & STATE_MASK) >> 26;
        }

        public bool IsAir()
        {
            return State() == 0;
        }

        public bool IsSolid()
        {
            return State() == 1;
        }

        public bool IsLiquid()
        {
            return State() == 2;
        }

        public void SetAir()
        {
            blockData = blockData & ~STATE_MASK; // 0b 0000 0000 0000 0000 0000 0000 0000 0000
        }

        public void SetSolid()
        {
            blockData = (blockData & ~STATE_MASK) | 0x04000000; // 0b 0000 0100 0000 0000 0000 0000 0000 0000
        }

        public void SetLiquid()
        {
            blockData = (blockData & ~STATE_MASK) | 0x08000000; // 0b 0000 1000 0000 0000 0000 0000 0000 0000
        }

        public BlockDefinition Definition()
        {
            return BlockData.BlockDefinitions[ID];
        }

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