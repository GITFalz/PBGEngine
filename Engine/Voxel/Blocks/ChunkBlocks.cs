using System.Runtime.CompilerServices;
using PBG.MathLibrary;

namespace PBG.Voxel
{
    public class ChunkBlocks
    {
        public const int BLOCK_COUNT = 32768;
        public VoxelChunk Chunk;
        private Block[] _blocks = new Block[BLOCK_COUNT];
        public ushort[] BlockTypesCount { get; private set; } = new ushort[BlockData.BLOCK_COUNT];
        public int UniqueBlockCount { get; private set; } = 0;
        public List<uint> UniqueBlockTypes { get; private set; } = [];
        public int NonAirBlocks = 0;
        public bool HasBlocks => UniqueBlockTypes.Count > 0;

        public ChunkBlocks(VoxelChunk chunk)
        {
            Chunk = chunk;
        }

        public void Set(Vector3i position, Block block) => Set(GetIndex(position), block);
        public void Set(int index, Block block)
        {
            var oldBlock = _blocks[index];
            _blocks[index] = block;

            if (!oldBlock.IsAir() && --BlockTypesCount[oldBlock.ID] == 0)
            {
                UniqueBlockCount--;
                UniqueBlockTypes.Remove(oldBlock.ID);
            }

            if (!block.IsAir() && BlockTypesCount[block.ID]++ == 0)
            {
                UniqueBlockCount++;
                UniqueBlockTypes.Add(block.ID);
            }
        }

        public Block Get(Vector3i position) => Get(GetIndex(position));
        public Block GetInner(Vector3i position) => Get(GetIndexInner(position.X, position.Y, position.Z));
        public Block Get(int index)
        {
            return _blocks[index];
        }
        
        public void Clear()
        {
            for (int i = 0; i < _blocks.Length; i++)
                _blocks[i] = Block.Air;

            BlockTypesCount = new ushort[BlockData.BLOCK_COUNT];
            UniqueBlockCount = 0;
            UniqueBlockTypes = [];
        }
        

        public Block[] GetBlocks() => _blocks;

        /// <summary>
        /// Used when you are certain the x, y and z values are all between 0 and 31
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndexInner(int x, int y, int z) => x + z * 32 + y * 1024;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndex(int x, int y, int z) => (x & 31) + (z & 31) * 32 + (y & 31) * 1024;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndex(Vector3i position) => (position.X & 31) + (position.Z & 31) * 32 + (position.Y & 31) * 1024;
    }
}