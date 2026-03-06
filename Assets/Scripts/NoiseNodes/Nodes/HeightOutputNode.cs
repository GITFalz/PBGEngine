using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Voxel;

namespace PBG.Assets.Scripts.NoiseNodes.Nodes
{
    public class HeightOutputNode(GetterValue[] getters, SetterValue[] setters, string type) : NoiseNode(getters, setters, type)
    {
        public GetterValue Start = getters[0];
        public GetterValue Height = getters[1];
        public GetterValue TextureIndex = getters[2];
        
        public override void Basic(int x, int y) { }
        public override void Run(NoiseNodeManager manager, VoxelChunk chunk, int x, int y)
        {
            /*
            float start = Start.F();
            float height = Height.F();
            int textureIndex = TextureIndex.I();
            Vector2 localPosition = new Vector2(x, y) - chunk.WorldPosition.Xz;

            int s = (int)start;
            int e = s + (int)height;

            int blockStart = Math.Min(s, e);
            int blockEnd = Math.Max(s, e);

            // Clamp to this chunks vertical span
            int chunkMin = chunk.WorldPosition.Y;
            int chunkMax = chunk.WorldPosition.Y + 32;

            int fillMin = Math.Max(blockStart, chunkMin);
            int fillMax = Math.Min(blockEnd, chunkMax);

            if (fillMin >= fillMax)
            {
                return;
            }  

            for (int a = fillMin; a < fillMax; a++)
            {
                int i = a - chunk.WorldPosition.Y;
                int index = (int)localPosition.X + (int)localPosition.Y * 32 + i * 1024;
                chunk.Blocks.SetBlockFast(index, new Block(BlockState.Solid, (uint)textureIndex));
            }
            */
        }

        public override void LOD(NoiseNodeManager manager, LODChunk chunk, int level, int x, int y)
        {
            /*
            int blockSize = 1 << level;

            float start = Start.F();
            float height = Height.F();
            int textureIndex = TextureIndex.I();
            if (textureIndex == 7 || textureIndex == 1)
            {
                start = Mathf.CeilToInt(start / blockSize) * blockSize;
            }
            else
            {
                start = Mathf.FloorToInt(start / blockSize) * blockSize;
            }
            Vector2 localPosition = (new Vector2(x, y) - chunk.WorldPosition.Xz) / blockSize;

            int s = (int)start;
            int e = s + (int)height;

            int blockStart = Math.Min(s, e);
            int blockEnd = Math.Max(s, e);

            int chunkMin = chunk.WorldPosition.Y;
            int chunkMax = chunk.WorldPosition.Y + 32 * blockSize;

            int fillMin = Math.Max(blockStart, chunkMin);
            int fillMax = Math.Min(blockEnd, chunkMax);

            if (fillMin >= fillMax)
                return;

            for (int a = fillMin; a < fillMax; a += blockSize)
            {
                int i = (a - chunk.WorldPosition.Y) / blockSize;
                int index = (int)localPosition.X + (int)localPosition.Y * 32 + i * 1024;
                chunk.Blocks[index] = new Block(BlockState.Solid, (uint)textureIndex);
            }
            */
        }

        protected override Expression BuildBasicExpression() { return Const(0); }
        protected override Expression BuildTerrainExpression()
        {
            var start = Start.Float();
            var height = Height.Float();
            var texture = TextureIndex.Int();

            return CallChunk(GetType(), "SetHeight", [F, F, I], start, height, texture);
        }

        public static void SetHeight(VoxelChunk chunk, Vector2i position, float start, float height, int texture)
        {
            Vector2 localPosition = position - chunk.WorldPosition.Xz;

            int s = (int)start;
            int e = s + (int)height;

            int blockStart = System.Math.Min(s, e);
            int blockEnd = System.Math.Max(s, e);

            // Clamp to this chunks vertical span
            int chunkMin = chunk.WorldPosition.Y;
            int chunkMax = chunk.WorldPosition.Y + 32;

            int fillMin = System.Math.Max(blockStart, chunkMin);
            int fillMax = System.Math.Min(blockEnd, chunkMax);

            if (fillMin >= fillMax)
            {
                return;
            }  

            for (int a = fillMin; a < fillMax; a++)
            {
                int i = a - chunk.WorldPosition.Y;
                int index = (int)localPosition.X + (int)localPosition.Y * 32 + i * 1024;
                chunk.Set(index, new Block(BlockState.Solid, (uint)texture));
            }
        }
    }  
}