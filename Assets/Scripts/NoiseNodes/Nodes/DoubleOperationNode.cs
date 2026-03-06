using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.MathLibrary;
using PBG.Voxel;

namespace PBG.Assets.Scripts.NoiseNodes.Nodes
{
    public class DoubleOperationNode(GetterValue[] getters, SetterValue[] setters, string type) : NoiseNode(getters, setters, type)
    {
        public int Operation = type switch
        {
            "Add" => 0,
            "Subtract" => 1,
            "Multiply" => 2,
            "Divide" => 3,
            "Max" => 4,
            "Min" => 5,
            "Mod" => 6,
            "Power" => 7,
            _ => 0
        };
        public SetterValue Output = setters[0];
        public GetterValue A = getters[0];
        public GetterValue B = getters[1];
        
        public override void Basic(int x, int y) => Function();
        public override void Run(NoiseNodeManager manager, VoxelChunk chunk, int x, int y) => Function();
        public override void LOD(NoiseNodeManager manager, LODChunk chunk, int level, int x, int y) => Function();

        public void Function()
        {
            /*
            var a = A.V3();
            var b = B.V3();

            switch (Operation)
            {
                case 0: Output.SetValue(a + b); break;
                case 1: Output.SetValue(a - b); break;
                case 2: Output.SetValue(a * b); break;
                case 3: Output.SetValue(a / b); break;
                case 4: Output.SetValue(Mathf.Max(a, b)); break;
                case 5: Output.SetValue(Mathf.Min(a, b)); break;
                case 6: Output.SetValue(Mathf.Mod(a, b)); break;
                case 7: Output.SetValue(Mathf.Pow(a, b)); break;
            }
            */
        }

        protected override Expression BuildBasicExpression() => BuildExpression();
        protected override Expression BuildTerrainExpression() => BuildExpression();
        private Expression BuildExpression()
        {
            var aExpr = A.Vector3();
            var bExpr = B.Vector3();

            var resultVar = Operation switch
            {
                0 => Add(aExpr, bExpr),
                1 => Sub(aExpr, bExpr),
                2 => Mul(aExpr, bExpr),
                3 => Div(aExpr, bExpr),
                4 => Call(Math, "Max", [V3, V3], aExpr, bExpr),
                5 => Call(Math, "Min", [V3, V3], aExpr, bExpr),
                6 => Call(Math, "Mod", [V3, V3], aExpr, bExpr),
                7 => Call(Math, "Pow", [V3, V3], aExpr, bExpr),
                _ => throw new ArgumentOutOfRangeException(),
            };

            return Assign(Output.GetVariable(), resultVar);
        }
    }
}