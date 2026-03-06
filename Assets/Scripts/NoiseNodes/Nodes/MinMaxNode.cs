using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.Voxel;

namespace PBG.Assets.Scripts.NoiseNodes.Nodes
{
    public class MinMaxNode(GetterValue[] getters, SetterValue[] setters, string type) : NoiseNode(getters, setters, type)
    {
        private GetterValue Min = getters[0];
        private GetterValue Max = getters[1];
        private GetterValue Value = getters[2];
        private SetterValue Output = setters[0];

        private int OperationType = type switch
        {
            "Clamp" => 0,
            "Ignore" => 1,
            "Lerp" => 2,
            "Slide" => 3,
            "Smooth" => 4,
            _ => 0
        }; // 0 = Clamp, 1 = Ignore, 2 = Lerp, 3 = Slide, 4 = Smooth
        
        public override void Basic(int x, int y) => Function();
        public override void Run(NoiseNodeManager manager, VoxelChunk chunk, int x, int y) => Function();
        public override void LOD(NoiseNodeManager manager, LODChunk chunk, int level, int x, int y) => Function();

        public void Function()
        {
            /*
            float result = OperationType switch
            {
                0 => Math.Clamp(Value.F(), Min.F(), Max.F()),                          // Clamp
                1 => (Value.F() < Min.F() || Value.F() > Max.F()) ? 0f : Value.F(),       // Ignore
                2 => Mathf.Lerp(Min.F(), Max.F(), Value.F()),                          // Lerp
                3 => Value.F() <= Min.F() ? 0f : (Value.F() >= Max.F() ? 1f : (Value.F() - Min.F()) / (Max.F() - Min.F())), // Slide
                4 => (Value.F() < Min.F() || Value.F() > Max.F()) ? 0f : (1f - Math.Abs(Value.F() - ((Min.F() + Max.F()) / 2f)) / ((Max.F() - Min.F()) / 2f)), // Smooth
                _ => Math.Clamp(Value.F(), Min.F(), Max.F())
            };

            Output.SetValue(result);
            */
        }

        protected override Expression BuildBasicExpression() => BuildExpression();
        protected override Expression BuildTerrainExpression() => BuildExpression();
        private Expression BuildExpression()
        {
            var valueExpr = Value.Float();
            var minExpr = Min.Float();
            var maxExpr = Max.Float();

            var resultVar = OperationType switch
            {
                0 => Call(Math, "Clampy", [F, F, F], valueExpr, minExpr, maxExpr),
                1 => IfElse(OrElse(Less(valueExpr, minExpr), Greater(valueExpr, maxExpr)), Const(0f), valueExpr),
                2 => Call(Math, "Lerp", [F, F, F], minExpr, maxExpr, valueExpr),
                3 => 
                IfElse(LessOrEqual(valueExpr, minExpr), 
                    Const(0f), 
                    IfElse(GreaterOrEqual(valueExpr, maxExpr), 
                        Const(1f), 
                        Div(Sub(valueExpr, minExpr), Sub(maxExpr, minExpr)))),
                4 => 
                IfElse(OrElse(Less(valueExpr, minExpr), Greater(valueExpr, maxExpr)), 
                    Const(0f), 
                    Sub(Const(1f), Div(Call(Math, "Abs", [F], Sub(valueExpr, Div(Add(minExpr, maxExpr), Const(2f)))), Div(Sub(maxExpr, minExpr), Const(2f))))
                ),
                _ => Call(Math, "Clampy", [F, F, F], valueExpr, minExpr, maxExpr)
            };

            return Assign(Output.GetVariable(), resultVar);
        }
    }
}