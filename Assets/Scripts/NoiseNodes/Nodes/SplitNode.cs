using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.Voxel;

namespace PBG.Assets.Scripts.NoiseNodes.Nodes
{
    public class SplitNode(GetterValue[] getters, SetterValue[] setters, string type) : NoiseNode(getters, setters, type)
    {
        public SetterValue X = setters[0];
        public SetterValue Y = setters[1];
        public SetterValue Z = setters[2];
        public GetterValue Vector = getters[0];
        
        public override void Basic(int x, int y) => Function();
        public override void Run(NoiseNodeManager manager, VoxelChunk chunk, int x, int y) => Function();
        public override void LOD(NoiseNodeManager manager, LODChunk chunk, int level, int x, int y) => Function();

        public void Function()
        {
            /*
            Vector3 value = Vector.V3();
            X.SetValue(value.X);
            Y.SetValue(value.Y);
            Z.SetValue(value.Z);
            */
        }

        protected override Expression BuildBasicExpression() => BuildExpression();
        protected override Expression BuildTerrainExpression() => BuildExpression();
        private Expression BuildExpression()
        {
            var inputExpr = Vector.GetExpression();

            var xVar = X.GetVariable();
            var yVar = Y.GetVariable();
            var zVar = Z.GetVariable();

            return Block(Assign(xVar, Field(inputExpr, "X")), Assign(yVar, Field(inputExpr, "Y")), Assign(zVar, Field(inputExpr, "Z")));
        }
    }
}