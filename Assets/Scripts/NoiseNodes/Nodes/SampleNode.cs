using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.Noise;
using PBG.Voxel;

namespace PBG.Assets.Scripts.NoiseNodes.Nodes
{
    public class SampleNode(GetterValue[] getters, SetterValue[] setters, string type) : NoiseNode(getters, setters, type)
    {
        public SetterValue Result = setters[0];
        public GetterValue Scale = getters[0];
        public GetterValue Offset = getters[1];

        public override void Basic(int x, int y) => Function(x, y);
        public override void Run(NoiseNodeManager manager, VoxelChunk chunk, int x, int y) => Function(x, y);
        public override void LOD(NoiseNodeManager manager, LODChunk chunk, int level, int x, int y) => Function(x, y);

        public void Function(int x, int y)
        {
            /*
            Vector2 scale = Scale.V2();
            Vector2 offset = Offset.V2();
            Vector2 samplePosition = (x + 0.001f, y + 0.001f);

            Vector2 position = (samplePosition + offset) * scale;
            float result = (NoiseLib.Noise(position) + 1) * 0.5f;

            Result.SetValue(result);

            //Console.WriteLine(Scale + " " + Offset + " " + result);
            */
        }

        protected override Expression BuildBasicExpression() => BuildExpression();
        protected override Expression BuildTerrainExpression() => BuildExpression();
        private Expression BuildExpression()
        {
            var scale = Scale.Vector2();
            var offset = Offset.Vector2();

            return Assign(Result.GetVariable(), CallPosition(GetType(), "Sample", [V2, V2], scale, offset));
        }

        public static float Sample(Vector2i samplePosition, Vector2 scale, Vector2 offset)
        {
            Vector2 position = (samplePosition + new Vector2(0.001f, 0.001f) + offset) * scale;
            return (NoiseLib.Noise(position) + 1) * 0.5f;
        }
    }
}