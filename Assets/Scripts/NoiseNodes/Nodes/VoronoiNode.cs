using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.Noise;
using PBG.Voxel;

namespace PBG.Assets.Scripts.NoiseNodes.Nodes
{
    public class VoronoiNode(GetterValue[] getters, SetterValue[] setters, string type) : NoiseNode(getters, setters, type)
    {
        public SetterValue Result = setters[0];
        public GetterValue Scale = getters[0];
        public GetterValue Offset = getters[1];
        public int Operation = type switch
        {
            "Basic" => 0,
            "Color" => 1,
            "Edge" => 2,
            "Distance" => 3,
            "CellPosition" => 4,
            _ => 0,
        };
        
        public override void Basic(int x, int y) => Function(x, y);
        public override void Run(NoiseNodeManager manager, VoxelChunk chunk, int x, int y) => Function(x, y);
        public override void LOD(NoiseNodeManager manager, LODChunk chunk, int level, int x, int y) => Function(x, y);

        public void Function(int x, int y)
        {
            /*
            var scale = Scale.V2();
            var offset = Offset.V2();

            switch (Operation)
            {
                case 0: Result.SetValue(Basic(scale, offset, (x, y))); break;
                case 1: Result.SetValue(Color(scale, offset, (x, y))); break;
                case 2: Result.SetValue(Edge(scale, offset, (x, y))); break;
                case 3: Result.SetValue(Distance(scale, offset, (x, y))); break;
                case 4: Result.SetValue(CellPosition(scale, offset, (x, y))); break;
            }
            */
        }

        public static Vector3 Basic(Vector2i samplePosition, Vector2 scale, Vector2 offset)
        {
            Vector2 position = (samplePosition + offset) * scale;
            float result = VoronoiLib.Voronoi(position, out _);
            return new Vector3(result);
        }

        public static Vector3 Color(Vector2i samplePosition, Vector2 scale, Vector2 offset)
        {
            Vector2 position = (samplePosition + offset) * scale;
            Vector3 result = VoronoiLib.Voronoi3(position, out _);
            return result;
        }

        public static Vector3 Edge(Vector2i samplePosition, Vector2 scale, Vector2 offset)
        {
            Vector2 position = (samplePosition + offset) * scale;
            float result = VoronoiLib.VoronoiF2(position, out _);
            return new Vector3(result);
        }

        public static Vector3 Distance(Vector2i samplePosition, Vector2 scale, Vector2 offset)
        {
            Vector2 position = (samplePosition + offset) * scale;
            float result = VoronoiLib.VoronoiDistance(position, out _);
            return new Vector3(result);
        }

        public static Vector3 CellPosition(Vector2i samplePosition, Vector2 scale, Vector2 offset)
        {
            Vector2 position = (samplePosition + offset) * scale;
            Vector2 result = VoronoiLib.VoronoiOrigin(position);
            return new Vector3(result);
        }

        protected override Expression BuildBasicExpression() => BuildExpression();
        protected override Expression BuildTerrainExpression() => BuildExpression();
        private Expression BuildExpression()
        {
            var scale = Scale.Vector2();
            var offset = Offset.Vector2();

            var resultVar = Operation switch
            {
                0 => CallPosition(GetType(), "Basic", [V2, V2], scale, offset),
                1 => CallPosition(GetType(), "Color", [V2, V2], scale, offset),
                2 => CallPosition(GetType(), "Edge", [V2, V2], scale, offset),
                3 => CallPosition(GetType(), "Distance", [V2, V2], scale, offset),
                4 => CallPosition(GetType(), "CellPosition", [V2, V2], scale, offset),
                _ => CallPosition(GetType(), "Basic", [V2, V2], scale, offset),
            };

            return Assign(Result.GetVariable(), resultVar);
        }
    }  
}