using System.Diagnostics;
using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.Voxel;

namespace PBG.Assets.Scripts.NoiseNodes.Nodes
{
    public class CacheNoiseNode(GetterValue[] getters, SetterValue[] setters, string type) : NoiseNode(getters, setters, type)
    {
        private SetterValue Output = setters[0];
        public int Id;

        public List<NoiseNode> ActionMap = new();
        public ColumnCache? columnCache = null;
        
        public override void Basic(int x, int y) => Function(x, y);
        public override void Run(NoiseNodeManager manager, VoxelChunk chunk, int x, int y) => Function(x, y);

        public void Function(int x, int y)
        {
            Vector3i relative = VoxelData.BlockToRelative((x, 0, y));
            Vector2i chunkPos = VoxelData.BlockToChunkRelative((x, 0, y)).Xz;
            Vector3i cacheID = (chunkPos.X, chunkPos.Y, Id);
            var cache = CacheManager.GetOrAdd(cacheID);
            //Output.SetValue(cache.Get(relative.X, relative.Z));
        }

        public override void LOD(NoiseNodeManager manager, LODChunk chunk, int level, int x, int y)
        {
            for (int i = 0; i < ActionMap.Count; i++)
            {
                var action = ActionMap[i];
                action.Basic(x, y);
            }

            //Output.SetValue(Input.V3());
        }

        public override NoiseNode Copy(NoiseValue[] variables)
        {
            Copy(variables, out var getters, out var setters);
            var cacheNode = new CacheNoiseNode(getters, setters, "");
            cacheNode.Id = Id;
            for (int i = 0; i < ActionMap.Count; i++)
            {
                var action = ActionMap[i];
                cacheNode.ActionMap.Add(action.Copy(variables));
            }
            return cacheNode;
        }

        protected override Expression BuildBasicExpression() => BuildExpression();
        protected override Expression BuildTerrainExpression() => BuildExpression();
        private Expression BuildExpression() => Assign(Output.GetVariable(), CallPosition(GetType(), "Function", [I], Expression.Constant(Id)));


        public static Vector3 Function(Vector2i position, int id)
        {
            Vector3i relative = VoxelData.BlockToRelative((position.X, 0, position.Y));
            Vector2i chunkPos = VoxelData.BlockToChunkRelative((position.X, 0, position.Y)).Xz;
            Vector3i cacheID = (chunkPos.X, chunkPos.Y, id);
            var cache = CacheManager.GetCacheBlocking(VoxelData.BlockToChunk((position.X, 0, position.Y)), cacheID);
            return cache.Get(relative.X, relative.Z);
        }
    }
}