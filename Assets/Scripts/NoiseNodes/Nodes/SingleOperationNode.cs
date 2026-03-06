using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.MathLibrary;
using PBG.Voxel;

namespace PBG.Assets.Scripts.NoiseNodes.Nodes
{
    public class SingleOperationNode : NoiseNode
    {
        private GetterValue Input;
        private SetterValue Output;
        private int OperationType; // 0 = Invert, 1 = Absolute, etc.

        public SingleOperationNode(GetterValue[] getters, SetterValue[] setters, string type) : base(getters, setters, type)
        {
            Input = getters[0];
            Output = setters[0];

            OperationType = type switch
            {
                "Invert" => 0,
                "Absolute" => 1,
                "Squared" => 2,
                "SquareRoot" => 3,
                "Sign" => 4,
                "Sin" => 5,
                "Cos" => 6,
                "Tan" => 7,
                "Floor" => 8,
                "Ceil" => 9,
                "Round" => 10,
                "Fraction" => 11,
                _ => 0
            };
        }
        
        public override void Basic(int x, int y) => Function();
        public override void Run(NoiseNodeManager manager, VoxelChunk chunk, int x, int y) => Function();
        public override void LOD(NoiseNodeManager manager, LODChunk chunk, int level, int x, int y) => Function();

        public void Function()
        {
            /*
            Vector3 value = Input.V3();
            Vector3 result = OperationType switch
            {
                0 => new Vector3(1, 1, 1) - value,                 // Invert
                1 => Mathf.Abs(value),                              // Absolute
                2 => new Vector3(value.X * value.X, value.Y * value.Y, value.Z * value.Z), // Squared
                3 => Mathf.Sqrt(value),                             // SquareRoot
                4 => Mathf.Sign(value),                             // Sign
                5 => Mathf.Sin(value),                              // Sin
                6 => Mathf.Cos(value),                              // Cos
                7 => Mathf.Tan(value),                              // Tan
                8 => Mathf.Floor(value),                            // Floor
                9 => Mathf.Ceil(value),                             // Ceil
                10 => Mathf.Round(value),                           // Round
                11 => Mathf.Fraction(value),                        // Fraction
                _ => new Vector3(1, 1, 1) - value
            };

            Output.SetValue(result);
            */
        }

        protected override Expression BuildBasicExpression() => BuildExpression();
        protected override Expression BuildTerrainExpression() => BuildExpression();
        private Expression BuildExpression()
        {
            var inputExpr = Input.Vector3();

            var x = Field(inputExpr, "X");
            var y = Field(inputExpr, "Y");
            var z = Field(inputExpr, "Z");

            var resultExpr = OperationType switch
            {
                0 => Sub(Vec3(1f, 1f, 1f), inputExpr),
                1 => Call(Math, "Abs", [V3], inputExpr),
                2 => Vec3(Mul(x, x), Mul(y, y), Mul(z, z)),
                3 => Call(Math, "Sqrt", [V3], inputExpr),
                4 => Call(Math, "Sign", [V3], inputExpr),
                5 => Call(Math, "Sin", [V3], inputExpr),
                6 => Call(Math, "Cos", [V3], inputExpr),
                7 => Call(Math, "Tan", [V3], inputExpr),
                8 => Call(Math, "Floor", [V3], inputExpr),
                9 => Call(Math, "Ceil", [V3], inputExpr),
                10 => Call(Math, "Round", [V3], inputExpr),
                11 => Call(Math, "Fraction", [V3], inputExpr),
                _ => Sub(Vec3(1f, 1f, 1f), inputExpr),
            };

            return Assign(Output.GetVariable(), resultExpr);
        }
    }
}