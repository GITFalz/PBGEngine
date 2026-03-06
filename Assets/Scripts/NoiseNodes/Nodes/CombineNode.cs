using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.Voxel;

namespace PBG.Assets.Scripts.NoiseNodes.Nodes
{
    public class CombineNode(GetterValue[] getters, SetterValue[] setters, string type) : NoiseNode(getters, setters, type)
    {
        private SetterValue Output = setters[0];
        private GetterValue A = getters[0];
        private GetterValue B = getters[1];
        private GetterValue C = getters[2];
        
        public override void Basic(int x, int y) => Function();
        public override void Run(NoiseNodeManager manager, VoxelChunk chunk, int x, int y) => Function();
        public override void LOD(NoiseNodeManager manager, LODChunk chunk, int level, int x, int y) => Function();

        public void Function()
        {
            // Avoid repeated array lookups
            //Output.SetValue(new Vector3(A.F(), B.F(), C.F()));
        }

        protected override Expression BuildBasicExpression() => BuildExpression();
        protected override Expression BuildTerrainExpression() => BuildExpression();
        private Expression BuildExpression()
        {
            var aExpr = A.Float();
            var bExpr = B.Float();
            var cExpr = C.Float();
            return Assign(Output.GetVariable(), Vec3(aExpr, bExpr, cExpr));
        }
    }
}