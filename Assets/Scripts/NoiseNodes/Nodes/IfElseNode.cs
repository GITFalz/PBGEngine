using System.Linq.Expressions;
using PBG.Voxel;

namespace PBG.Assets.Scripts.NoiseNodes.Nodes
{
    public class IfElseNoiseNode(GetterValue value, GetterValue compare, SetterValue[] setters, string type) : NoiseNode([value, compare], setters, type)
    {
        public List<Expression> Expressions = [];
    
        // Cached references
        private GetterValue A = value;
        private GetterValue B = compare;
        private int TestType = type switch
        {
            "==" => 0,
            "!=" => 1,
            "<" => 2,
            "<=" => 3,
            ">" => 4,
            ">=" => 5,
            _ => 0
        }; // 0 = ==, 1 = !=, 2 = <, 3 = <=, 4 = >, 5 = >=

        public override void Basic(int x, int y)
        {
            /*
            if (!Test(A.F(), B.F())) return;

            for (int i = 0; i < ActionMap.Count; i++)
            {
                ActionMap[i].Basic(x, y);
            }
            */
        }

        public override void Run(NoiseNodeManager manager, VoxelChunk chunk, int x, int y)
        {
            /*
            if (!Test(A.F(), B.F())) return;

            for (int i = 0; i < ActionMap.Count; i++)
            {
                ActionMap[i].Run(manager, chunk, x, y);
            }
            */
        }

        public override void LOD(NoiseNodeManager manager, LODChunk chunk, int level, int x, int y)
        {
            /*
            if (!Test(A.F(), B.F())) return;

            for (int i = 0; i < ActionMap.Count; i++)
            {
                ActionMap[i].LOD(manager, chunk, level, x, y);
            }
            */
        }

        private bool Test(float a, float b)
        {
            return TestType switch
            {
                0 => a == b,
                1 => a != b,
                2 => a < b,
                3 => a <= b,
                4 => a > b,
                5 => a >= b,
                _ => a == b
            };
        }

        public override NoiseNode Copy(NoiseValue[] variables)
        {
            return new IfElseNoiseNode(value, compare, [], Type);
            /*
            Copy(variables, out var getters, out var setters);
            var instance = Activator.CreateInstance(GetType(), getters, setters, Type) ?? throw new Exception("It was not possible to create a copy of node " + GetType().Name);
            if (instance is IfElseNoiseNode ifElseNode) 
            for (int i = 0; i < ActionMap.Count; i++)
            {
                var action = ActionMap[i];
                ifElseNode.ActionMap.Add(action.Copy(variables));
            }
            return (NoiseNode)instance;
            */
        }

        protected override Expression BuildBasicExpression() => BuildExpression();
        protected override Expression BuildTerrainExpression() => BuildExpression();
        private Expression BuildExpression()
        {
            var a = A.GetExpression();
            var b = B.GetExpression();

            var conditionVar = TestType switch
            {
                0 => Equal(a, b),
                1 => NotEqual(a, b),
                2 => Less(a, b),
                3 => LessOrEqual(a, b),
                4 => Greater(a, b),
                5 => GreaterOrEqual(a, b),
                _ => Equal(a, b)
            };

            Console.WriteLine("There are " + Expressions.Count + " expressions");
            foreach (var expr in Expressions)
            {
                Console.WriteLine("expression: " + expr.Type);
            }

            return If(conditionVar, Block(Expressions));
        }

        private static object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type)!;
            return null!;
        }
    }
}