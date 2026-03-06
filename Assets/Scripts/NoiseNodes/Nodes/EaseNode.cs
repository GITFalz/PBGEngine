using System.Linq.Expressions;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.MathLibrary;
using PBG.Voxel;

namespace PBG.Assets.Scripts.NoiseNodes.Nodes
{
    public class EaseNode(GetterValue[] getters, SetterValue[] setters, string type) : NoiseNode(getters, setters, type)
    {
        private SetterValue Output = setters[0];
        private GetterValue Factor = getters[0];
        private GetterValue Start = getters[1];
        private GetterValue End = getters[2];
        private GetterValue T = getters[3];
        private int OperationType = type switch
        {
            "Linear" => 0,
            "EaseIn" => 1,
            "EaseOut" => 2,
            "EaseInOut" => 3,
            _ => 0
        }; // 0 = Linear, 1 = EaseIn, 2 = EaseOut, 3 = EaseInOut
        
        public override void Basic(int x, int y) => Function();
        public override void Run(NoiseNodeManager manager, VoxelChunk chunk, int x, int y) => Function();
        public override void LOD(NoiseNodeManager manager, LODChunk chunk, int level, int x, int y) => Function();

        public void Function()
        {
            /*
            float factor = Factor.F();
            float start = Start.F();
            float end = End.F();
            float t = T.F();
            float result;

            switch (OperationType)
            {
                case 0: // Linear
                    result = start + (end - start) * t;
                    break;
                case 1: // EaseIn
                    result = start + (end - start) * (float)Math.Pow(t, factor);
                    break;
                case 2: // EaseOut
                    result = start + (end - start) * (1f - (float)Math.Pow(1f - t, factor));
                    break;
                case 3: // EaseInOut
                    if (t <= 0f) result = start;
                    else if (t >= 1f) result = end;
                    else if (t < 0.5f)
                    {
                        float tt = t * 2f;
                        result = start + (end - start) * 0.5f * (float)Math.Pow(tt, factor);
                    }
                    else
                    {
                        float tt = (t - 0.5f) * 2f;
                        result = start + (end - start) * (0.5f + 0.5f * (1f - (float)Math.Pow(1f - tt, factor)));
                    }
                    break;
                default:
                    result = start + (end - start) * t;
                    break;
            }

            Output.SetValue(result);
            */
        }

        protected override Expression BuildBasicExpression() => BuildExpression();
        protected override Expression BuildTerrainExpression() => BuildExpression();
        private Expression BuildExpression()
        {
            var factor = Factor.Float(); 
            var start = Start.Float();
            var end = End.Float();
            var t = T.Float();

            var resultVar = OperationType switch
            {
                0 => Add(start, Mul(Sub(end, start), t)),
                1 => Add(start, Mul(Sub(end, start), Cast(Call(Math, "Pow", [F, F], t, factor), F))),
                2 => Add(start, Mul(Sub(end, start), Sub(Const(1f), Cast(Call(Math, "Pow", [F, F], Sub(Const(1f), t), factor), F)))),
                3 => 
                IfElse(LessOrEqual(t, Const(0f)), 
                    start, 
                    IfElse(GreaterOrEqual(t, Const(1f)), 
                        end, 
                        IfElse(Less(t, Const(0.5f)), 
                            Add(start, Mul(Mul(Sub(end, start), Const(0.5f)), Cast(Call(Math, "Pow", [F, F], Mul(t, Const(2f)), factor), F))),
                            Add(start, Mul(Sub(end, start), Add(Const(0.5f), Mul(Const(0.5f), Cast(Call(Math, "Pow", [F, F], 
                                Sub(Const(1f), Mul(Sub(t, Const(0.5f)), Const(2f))), factor), F)))))
                        )
                    )
                ),
                _ => Add(start, Mul(Sub(end, start), t)),
            };

            return Assign(Output.GetVariable(), resultVar);
        }
    }
}