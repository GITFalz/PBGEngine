using System.Linq.Expressions;
using PBG.Voxel;

namespace PBG.Assets.Scripts.NoiseNodes.Nodes
{
    public class Vector3iNode(GetterValue[] getters, SetterValue[] setters, string type) : NoiseNode(getters, setters, type)
    {
        public SetterValue Output = setters[0];
        public GetterValue Input = getters[0];
        
        public override void Basic(int x, int y) => Function();
        public override void Run(NoiseNodeManager manager, VoxelChunk chunk, int x, int y) => Function();
        public override void LOD(NoiseNodeManager manager, LODChunk chunk, int level, int x, int y) => Function();

        public void Function() {}// => Output.SetValue(Input.V3i());
        protected override Expression BuildBasicExpression() => BuildExpression();
        protected override Expression BuildTerrainExpression() => BuildExpression();
        private Expression BuildExpression()
        {
            var inputExpr = Input.Vector3i();
            var outputVar = Output.GetVariable();
            return Assign(outputVar, inputExpr);
        }
    }  
}