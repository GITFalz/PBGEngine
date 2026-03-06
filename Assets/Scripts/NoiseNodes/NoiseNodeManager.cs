using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PBG.MathLibrary;
using PBG.MathLibrary;
using PBG.Threads;
using PBG.Voxel;

namespace PBG.Assets.Scripts.NoiseNodes
{
    public class NoiseNodeManager(int threadIndex)
    {
        private static Action<NoiseNodeManager, Vector2i> _basicAction = (_, _) => {};
        private static Action<NoiseNodeManager, VoxelChunk, Vector2i> _terrainAction = (_, _, _) => {};
        public static List<NoiseNodeManager> Managers = [];

        public int ThreadIndex { get; private set; } = threadIndex;

        public void Basic(int x, int y)
        {
            _basicAction(this, new Vector2i(x, y));
        }

        /// <summary>
        /// Runs on the main thread
        /// </summary>
        public static void RunMain(VoxelChunk chunk)
        {
            NoiseNodeManager manager = Managers[0];
            for (int x = 0; x < 32; x++) 
            { 
                for (int y = 0; y < 32; y++) 
                { 
                    _terrainAction(manager, chunk, new Vector2i(x, y) + chunk.WorldPosition.Xz);
                } 
            } 
        }

        public static bool Run(ThreadProcess process, VoxelChunk chunk) => RunBasic(process, chunk);
        public static bool RunBasic(ThreadProcess process, VoxelChunk chunk) 
        { 
            NoiseNodeManager manager = Managers[process.ThreadIndex];
            for (int x = 0; x < 32; x++) 
            { 
                for (int y = 0; y < 32; y++) 
                { 
                    if (process.Failed) 
                        return false; 
                        
                    _terrainAction(manager, chunk, new Vector2i(x, y) + chunk.WorldPosition.Xz);
                } 
            } 

            /*
            NoiseNodeManager manager = Managers[process.ThreadIndex]; 
            for (int x = 0; x < 32; x++) 
            { 
                for (int y = 0; y < 32; y++) 
                { 
                    if (process.Failed) 
                        return false; 
                        
                    for (int i = 0; i < manager.ActionMap.Count; i++) 
                    { 
                        var action = manager.ActionMap[i]; 
                        action.Run(manager, chunk, x + chunk.WorldPosition.X, y + chunk.WorldPosition.Z); 
                    } 
                } 
            } 
            */
            return true; 
        }
        
        public static bool RunProfiler(ThreadProcess process, VoxelChunk chunk)
        {
            /*
            if (process.ThreadIndex == 0)
                Console.WriteLine("===== START NODE PROFILING =====");

            NoiseNodeManager manager = Managers[process.ThreadIndex];

            var timings = new Dictionary<string, (double totalMs, int count)>();
            var totalStopwatch = Stopwatch.StartNew();

            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    if (process.Failed)
                        return false;

                    for (int i = 0; i < manager.ActionMap.Count; i++)
                    {
                        var action = manager.ActionMap[i];
                        string nodeName = action.GetType().Name;

                        var sw = Stopwatch.StartNew();
                        action.Run(manager, chunk, x + chunk.WorldPosition.X, y + chunk.WorldPosition.Z);
                        sw.Stop();

                        if (!timings.TryGetValue(nodeName, out var data))
                            data = (0.0, 0);

                        data.totalMs += sw.Elapsed.TotalMilliseconds;
                        data.count++;
                        timings[nodeName] = data;
                    }
                }
            }

            totalStopwatch.Stop();

            if (process.ThreadIndex == 0)
            {
                Console.WriteLine("---- NODE PROFILING RESULTS ----");
                foreach (var kvp in timings)
                {
                    string name = kvp.Key;
                    double total = kvp.Value.totalMs;
                    int count = kvp.Value.count;
                    double avg = count > 0 ? total / count : 0.0;
                    Console.WriteLine($"{name} | Total: {total:F4}ms | Count: {count} | Avg: {avg:F6}ms");
                }
                Console.WriteLine($"==== TOTAL CHUNK TIME: {totalStopwatch.Elapsed.TotalMilliseconds:F4}ms ====");
                Console.WriteLine("===== END NODE PROFILING =====");
            }
            */

            return true;
        }

        public static bool RunLOD(ThreadProcess process, Vector2i position, LODChunk chunk, int level) 
        { 
            /*
            NoiseNodeManager manager = Managers[process.ThreadIndex]; 
            int blockSize = 1 << level;
            for (int x = 0; x < 32 * blockSize; x++) 
            { 
                for (int y = 0; y < 32 * blockSize; y++) 
                { 
                    if (process.Failed) 
                        return false; 
                    
                    for (int i = 0; i < manager.ActionMap.Count; i++) 
                    { 
                        var action = manager.ActionMap[i]; 
                        action.LOD(manager, chunk, level, x + position.X, y + position.Y); 
                    } 
                } 
            } 
            */

            return true; 
        }

        public void Clear()
        {

        }

        public static void ClearAllManagers()
        {
            for (int i = 0; i < Managers.Count; i++)
            {
                Managers[i].Clear();
            }
        }

        public static void Load(string path)
        {
            var settings = new NodeLoaderSettings()
            {
                LoadingType = NodeLoadingType.Core
            };

            CacheNode.IDCounter = 0; 

            var loader = new NodeLoader(settings);
            if (!loader.Load(path, out var collection))
            {
                Console.WriteLine("[Error] : Noise file not found at path: " + path);
                return;
            }

            var connectedNodes = collection.GetConnectedNodeList(out var outputCount);
            NodeManager.RemoveUselessNodes(connectedNodes);
            var nodeTree = NodeManager.GetNodeTree(connectedNodes);

            NoiseNode.SetBasic();

            NoiseNode.NodeManager = Expression.Parameter(typeof(NoiseNodeManager), "nodeManager");
            NoiseNode.Position = Expression.Parameter(typeof(Vector2i), "iPosition");

            var basicBlock = GetNodesExpression(nodeTree);
            var basicLambda = Expression.Lambda<Action<NoiseNodeManager, Vector2i>>(basicBlock, NoiseNode.NodeManager, NoiseNode.Position);
            
            NoiseNode.SetTerrain();

            NoiseNode.NodeManager = Expression.Parameter(typeof(NoiseNodeManager), "nodeManager");
            NoiseNode.Chunk = Expression.Parameter(typeof(VoxelChunk), "chunk");
            NoiseNode.Position = Expression.Parameter(typeof(Vector2i), "iPosition");

            var terrainBlock = GetNodesExpression(nodeTree);
            var terrainLambda = Expression.Lambda<Action<NoiseNodeManager, VoxelChunk, Vector2i>>(terrainBlock, NoiseNode.NodeManager, NoiseNode.Chunk, NoiseNode.Position);

            var blockExpr = (BlockExpression)terrainLambda.Body;

            /*
            string ExpressionToCode(Expression expr, int indentLevel = 0)
            {
                string indent = new string(' ', indentLevel * 4);
                
                if (expr is BlockExpression block)
                {
                    var sb = new System.Text.StringBuilder();
                    
                    // Variables
                    foreach (var v in block.Variables)
                    {
                        sb.AppendLine($"{indent}{v.Type.Name} {v.Name};");
                    }
                    
                    // Expressions
                    foreach (var e in block.Expressions)
                    {
                        sb.AppendLine(ExpressionToCode(e, indentLevel + 1));
                    }
                    
                    return sb.ToString();
                }
                else if (expr is BinaryExpression bin && bin.NodeType == ExpressionType.Assign)
                {
                    return $"{indent}{ExpressionToCode(bin.Left)} = {ExpressionToCode(bin.Right)};";
                }
                else if (expr is BinaryExpression binary)
                {
                    return $"{indent}({ExpressionToCode(binary.Left)} {GetOperator(binary.NodeType)} {ExpressionToCode(binary.Right)});";
                }
                else if (expr is ParameterExpression param)
                {
                    return param.Name;
                }
                else if (expr is ConstantExpression c)
                {
                    if (c.Value == null)
                    {
                        return "null";
                    }
                    
                    if (c.Type == typeof(string))
                    {
                        return $"\"{c.Value}\"";
                    }
                    
                    if (c.Type == typeof(Vector2))
                    {
                        var v2 = (Vector2)c.Value;
                        return $"new Vector2({v2.X}, {v2.Y})";
                    }
                    
                    if (c.Type == typeof(Vector3))
                    {
                        var v3 = (Vector3)c.Value;
                        return $"new Vector3({v3.X}, {v3.Y}, {v3.Z})";
                    }
                    
                    return c.Value.ToString();
                }
                else if (expr is MethodCallExpression call)
                {
                    var args = string.Join(", ", call.Arguments.Select(a => ExpressionToCode(a)));
                    return $"{indent}{call.Method.Name}({args});";
                }
                else if (expr is ConditionalExpression cond)
                {
                    return $"{indent}({ExpressionToCode(cond.Test)} ? {ExpressionToCode(cond.IfTrue)} : {ExpressionToCode(cond.IfFalse)});";
                }
                else if (expr is UnaryExpression unary)
                {
                    return $"{unary.NodeType} {ExpressionToCode(unary.Operand)}";
                }
                else
                {
                    return $"{indent}// {expr.NodeType} : {expr.Type.Name}";
                }
            }

            string GetOperator(ExpressionType type)
            {
                if (type == ExpressionType.Add)
                    return "+";
                else if (type == ExpressionType.Subtract)
                    return "-";
                else if (type == ExpressionType.Multiply)
                    return "*";
                else if (type == ExpressionType.Divide)
                    return "/";
                else if (type == ExpressionType.Modulo)
                    return "%";
                else if (type == ExpressionType.And)
                    return "&";
                else if (type == ExpressionType.Or)
                    return "|";
                else if (type == ExpressionType.ExclusiveOr)
                    return "^";
                else if (type == ExpressionType.LeftShift)
                    return "<<";
                else if (type == ExpressionType.RightShift)
                    return ">=";
                else if (type == ExpressionType.Equal)
                    return "==";
                else if (type == ExpressionType.NotEqual)
                    return "!=";
                else if (type == ExpressionType.GreaterThan)
                    return ">";
                else if (type == ExpressionType.GreaterThanOrEqual)
                    return ">=";
                else if (type == ExpressionType.LessThan)
                    return "<";
                else if (type == ExpressionType.LessThanOrEqual)
                    return "<=";
                else
                    return type.ToString();
            }


            // Print declared variables
            Console.WriteLine("Variables:");
            foreach (var v in blockExpr.Variables)
                Console.WriteLine("  " + v.Name + " : " + v.Type);

            // Print expressions (assignments and final result)
            Console.WriteLine("Expressions:");
            foreach (var e in blockExpr.Expressions)
                Console.WriteLine(ExpressionToCode(e));

            Console.WriteLine(terrainBlock.ToString());
            Console.WriteLine("-----------------------------------");
            */

            _basicAction = basicLambda.Compile();
            _terrainAction = terrainLambda.Compile();

            NoiseValue[] variables = [.. Enumerable.Repeat(NoiseValue.Default, outputCount)];
            Console.WriteLine("There are " + outputCount + " variables");

            Console.WriteLine("Node tree has " + nodeTree.Count + " nodes");

            /*
            int index = 0;
            for (int i = 0; i < nodeTree.Count; i++)
            {
                var node = nodeTree[i];
                var noiseNode = node.GenerateNoiseNode(baseManager, ref index);
                if (noiseNode != null)
                    baseManager.ActionMap.Add(noiseNode);
            }
            */

#if MYDEBUG
            Console.WriteLine($"[Loading] : Base manager has {baseManager.ActionMap.Count} actions");
#endif
            PopulateManagers(TaskPool.ThreadCount);
        }

        public static Expression GetNodesExpression(List<NodeData> nodeTree)
        {
            List<NoiseNode> noiseNodes = [];

            List<ParameterExpression> vars = [];
            List<Expression> expressions = [];

            Dictionary<NodeBase, Dictionary<string, SetterValue>> connections = [];

            foreach (var node in nodeTree)
            { 
                Console.WriteLine("Connected to cache? " + node.Node.ParentCacheNode != null);
                var n = node.BuildExpression(connections, expressions);
                if (n != null)
                {
                    noiseNodes.Add(n);
                    expressions.Add(NoiseNode.BuildExpression(n));
                }
            }

            List<SetterValue> setters = [];

            foreach (var node in noiseNodes)
            {
                setters.AddRange(node.Setters);
            }

            foreach (var setter in setters)
            {
                if (setter.Variable != null)
                {
                    vars.Add(setter.Variable);
                }
            }

            return Expression.Block(
                vars,
                expressions
            );
        }

        public static void PopulateManagers(int count)
        {
#if MYDEBUG
            Console.WriteLine($"[Populating] : Manager to be copied has {manager.ActionMap.Count} actions");
#endif
            ClearAllManagers();
            Managers.Clear();
            for (int i = 0; i < count; i++)
            {
                Managers.Add(new(i));
            }
        }
    }

    public class GetterValue(ValueType valueType, Expression? setterValue, Expression defaultValue)
    {
        public ValueType ValueType = valueType;

        public Expression GetExpression()
        {
            return setterValue ?? defaultValue;
        }

        public Expression Float() => ExpressionHelper.ConvertTo(valueType, ValueType.Float, GetExpression());
        public Expression Int() => ExpressionHelper.ConvertTo(valueType, ValueType.Int, GetExpression());
        public Expression Vector2() => ExpressionHelper.ConvertTo(valueType, ValueType.Vector2, GetExpression());
        public Expression Vector2i() => ExpressionHelper.ConvertTo(valueType, ValueType.Vector2i, GetExpression());
        public Expression Vector3() => ExpressionHelper.ConvertTo(valueType, ValueType.Vector3, GetExpression());
        public Expression Vector3i() => ExpressionHelper.ConvertTo(valueType, ValueType.Vector3i, GetExpression());
    }

    public class SetterValue(NodeOutputField outputField)
    {
        public ParameterExpression? Variable = null;

        public Expression GetVariable()
        {
            Variable = Expression.Variable(outputField.Value.GetRealType(), outputField.Output.VariableName);
            return Variable;
        }
    }

    public struct OLD_GetterValue(NoiseValue[] variables, int index)
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NoiseValue GetValue() => variables[index];

        public OLD_GetterValue Copy(NoiseValue[] newVariables)
        {
            newVariables[index] = variables[index].Copy();
            return new OLD_GetterValue(newVariables, index);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float F() => GetValue().Float();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int I() => GetValue().Int();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 V2() => GetValue().Vector2();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2i V2i() => GetValue().Vector2i();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 V3() => GetValue().Vector3();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 V3i() => GetValue().Vector3i();

        public override string ToString() => "Variable getter at index " + index;
    }

    public struct OLD_SetterValue(NoiseValue[] variables, int index)
    {
        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(NoiseValue value) => variables[index] = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(float value)
        {
            ref var v = ref variables[index];
            v.Type = NoiseValueType.Float;
            v.FloatValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(int value)
        {
            ref var v = ref variables[index];
            v.Type = NoiseValueType.Int;
            v.IntValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(Vector2 value)
        {
            ref var v = ref variables[index];
            v.Type = NoiseValueType.Vector2;
            v.Vector2Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(Vector2i value)
        {
            ref var v = ref variables[index];
            v.Type = NoiseValueType.Vector2i;
            v.Vector2iValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(Vector3 value)
        {
            ref var v = ref variables[index];
            v.Type = NoiseValueType.Vector3;
            v.Vector3Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(Vector3i value)
        {
            ref var v = ref variables[index];
            v.Type = NoiseValueType.Vector3i;
            v.Vector3iValue = value;
        }

        public OLD_SetterValue Copy(NoiseValue[] newVariables) => new OLD_SetterValue(newVariables, index);
        public override string ToString() => "Setter at index " + index;
    }

    public abstract class NoiseNode
    {
        public GetterValue[] Getters;
        public List<SetterValue> Setters;
        protected string Type;

        public NoiseNode(GetterValue[] getters, SetterValue[] setters, string type)
        {
            Getters = getters;
            Setters = [..setters];
            Type = type;
        }

        public abstract void Basic(int x, int y);
        public abstract void Run(NoiseNodeManager manager, VoxelChunk chunk, int x, int y);
        public abstract void LOD(NoiseNodeManager manager, LODChunk chunk, int level, int x, int y);

        /// <summary>
        /// Builds an expression tree that represents the node in PURE MATH mode.
        /// This version must have no side effects and must not interact with chunks,
        /// blocks, or world data. It is used for previewing, heightmap evaluation,
        /// parameter queries, and any situation where the node graph should behave
        /// as a pure functional computation.
        /// 
        /// The resulting expression should only compute and return values.
        /// </summary>
        protected abstract Expression BuildBasicExpression();

        /// <summary>
        /// Builds an expression tree that represents the node in TERRAIN GENERATION mode.
        /// This version is allowed to modify chunks, place blocks, and perform world
        /// mutation. It is used during actual world generation when the graph should
        /// produce concrete voxel data instead of just numeric results.
        /// 
        /// Expressions built here may contain side effects such as writing to chunk data.
        /// </summary>
        protected abstract Expression BuildTerrainExpression();

        private static Func<NoiseNode, Expression> _buildExpression = (node) => node.BuildBasicExpression();
        public static Expression BuildExpression(NoiseNode node) => _buildExpression(node);

        public static void SetBasic() => _buildExpression = (node) => node.BuildBasicExpression();
        public static void SetTerrain() => _buildExpression = (node) => node.BuildTerrainExpression();

        public void Copy(NoiseValue[] variables, out GetterValue[] getters, out SetterValue[] setters)
        {
            getters = new GetterValue[Getters.Length];
            setters = new SetterValue[Setters.Count];

            /*
            for (int i = 0; i < Getters.Length; i++)
            {
                var getter = Getters[i];
                getters[i] = getter.Copy(variables);
            }

            for (int i = 0; i < Setters.Length; i++)
            {
                var setter = Setters[i];
                setters[i] = setter.Copy(variables);
            }
            */
        }

        public virtual NoiseNode Copy(NoiseValue[] variables)
        {
            Copy(variables, out var getters, out var setters);
            var instance = Activator.CreateInstance(GetType(), getters, setters, Type) ?? throw new Exception("It was not possible to create a copy of node " + GetType().Name);
            return (NoiseNode)instance;
        }
        
        public override string ToString()
        {
            var gettersStr = string.Join(", ", Getters.Select(g => g.ToString()));
            var settersStr = string.Join(", ", Setters.Select(s => s.ToString()));
            
            return $"{GetType().Name} [Type: {Type}] - Getters: [{gettersStr}] - Setters: [{settersStr}]";
        }

        public static Type F => typeof(float);
        public static Type I => typeof(int);
        public static Type V2 => typeof(Vector2);
        public static Type V2i => typeof(Vector2i);
        public static Type V3 => typeof(Vector3);
        public static Type V3i => typeof(Vector3i);
        public static Type Math => typeof(Mathf);

        public static Expression Add(Expression a, Expression b) => Expression.Add(a, b);
        public static Expression Sub(Expression a, Expression b) => Expression.Subtract(a, b);
        public static Expression Mul(Expression a, Expression b) => Expression.Multiply(a, b);
        public static Expression Div(Expression a, Expression b) => Expression.Divide(a, b);
        public static Expression Mod(Expression a, Expression b) => Expression.Modulo(a, b);

        public static Expression Neg(Expression a) => Expression.Negate(a);
        public static Expression Not(Expression a) => Expression.Not(a);

        public static Expression And(Expression a, Expression b) => Expression.AndAlso(a, b);
        public static Expression Or(Expression a, Expression b) => Expression.OrElse(a, b);
        public static Expression Equal(Expression a, Expression b) => Expression.Equal(a, b);
        public static Expression NotEqual(Expression a, Expression b) => Expression.NotEqual(a, b);
        public static Expression Less(Expression a, Expression b) => Expression.LessThan(a, b);
        public static Expression LessOrEqual(Expression a, Expression b) => Expression.LessThanOrEqual(a, b);
        public static Expression Greater(Expression a, Expression b) => Expression.GreaterThan(a, b);
        public static Expression GreaterOrEqual(Expression a, Expression b) => Expression.GreaterThanOrEqual(a, b);

        public static Expression OrElse(Expression a, Expression b) => Expression.OrElse(a, b);
        public static Expression AndAlso(Expression a, Expression b) => Expression.AndAlso(a, b);

        public static Expression IfElse(Expression condition, Expression ifTrue, Expression ifFalse)
        {
            try
            {
                // If both are void, use IfThenElse
                if (ifTrue.Type == typeof(void) && ifFalse.Type == typeof(void))
                    return Expression.IfThenElse(condition, ifTrue, ifFalse);

                return Expression.Condition(condition, ifTrue, ifFalse);
            }
            catch (Exception ex)
            {
                Console.WriteLine("---- Expression Debug ----");
                Console.WriteLine($"Condition Type: {condition.Type}");
                Console.WriteLine($"IfTrue Type:    {ifTrue.Type}");
                Console.WriteLine($"IfFalse Type:   {ifFalse.Type}");
                Console.WriteLine("------------------------------------");

                throw;
            }
        }
        public static Expression If(Expression condition, Expression ifTrue)
        {
            return Expression.Condition(condition, ifTrue, ifTrue.Type == typeof(void) ? Expression.Empty() : Expression.Constant(GetDefaultValue(ifTrue.Type)));
        }

        private static object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type)!;
            return null!;
        }

        public static Expression Assign(Expression a, Expression b) => Expression.Assign(a, b);

        public static Expression Vec2(Expression x, Expression y) => Expression.New(V2.GetConstructor([F, F])!, x, y);
        public static Expression Vec2i(Expression x, Expression y) => Expression.New(V2i.GetConstructor([I, I])!, x, y);
        public static Expression Vec3(Expression x, Expression y, Expression z) => Expression.New(V3.GetConstructor([F, F, F])!, x, y, z);
        public static Expression Vec3i(Expression x, Expression y, Expression z) => Expression.New(V3i.GetConstructor([I, I, I])!, x, y, z);

        public static Expression Vec2(float x, float y) => Expression.New(V2.GetConstructor([F, F])!, Const(x), Const(y));
        public static Expression Vec2i(float x, float y) => Expression.New(V2i.GetConstructor([I, I])!, Const(x), Const(y));
        public static Expression Vec3(float x, float y, float z) => Expression.New(V3.GetConstructor([F, F, F])!, Const(x), Const(y), Const(z));
        public static Expression Vec3i(float x, float y, float z) => Expression.New(V3i.GetConstructor([I, I, I])!, Const(x), Const(y), Const(z));

        public static Expression Field(Expression expression, string name) => Expression.Field(expression, name);

        public static Expression Block(params Expression[] expressions) => Expression.Block(expressions);
        public static Expression Block(List<Expression> expressions) => Expression.Block(expressions);

        public static ParameterExpression NodeManager = Expression.Parameter(typeof(NoiseNodeManager), "nodeManager");
        public static ParameterExpression Chunk = Expression.Parameter(typeof(VoxelChunk), "chunk");
        public static ParameterExpression Position = Expression.Parameter(typeof(Vector2i), "iPosition");

        public static Expression Call(Type type, string name) => Call(type, name, [], []);
        public static Expression Call(Type type, string name, Type[] parameterTypes, params Expression[] expressions)
        {
            var method = type.GetMethod(name, parameterTypes) ?? throw new MissingMethodException(type.FullName, name);
            return Expression.Call(method, expressions);
        }

        public static Expression CallPosition(Type type, string name) => CallPosition(type, name, [], []);
        public static Expression CallPosition(Type type, string name, Type[] parameterTypes, params Expression[] expressions)
        {
            var method = type.GetMethod(name, [typeof(Vector2i), ..parameterTypes]) ?? throw new MissingMethodException(type.FullName, name);
            return Expression.Call(method, [Position, ..expressions]);
        }

        public static Expression CallChunk(Type type, string name) => CallChunk(type, name, [], []);
        public static Expression CallChunk(Type type, string name, Type[] parameterTypes, params Expression[] expressions)
        {
            var method = type.GetMethod(name, [typeof(VoxelChunk), typeof(Vector2i), ..parameterTypes]) ?? throw new MissingMethodException(type.FullName, name);
            return Expression.Call(method, [Chunk, Position, ..expressions]);
        }   

        public static Expression CallManager(Type type, string name) => CallManager(type, name, [], []);
        public static Expression CallManager(Type type, string name, Type[] parameterTypes, params Expression[] expressions)
        {
            var method = type.GetMethod(name, [typeof(NoiseNodeManager), ..parameterTypes]) ?? throw new MissingMethodException(type.FullName, name);
            return Expression.Call(method, [NodeManager, ..expressions]);
        }

        public static Expression CallMain(Type type, string name) => CallMain(type, name, [], []);
        public static Expression CallMain(Type type, string name, Type[] parameterTypes, params Expression[] expressions)
        {
            var method = type.GetMethod(name, [typeof(NoiseNodeManager), typeof(VoxelChunk), typeof(Vector2i), ..parameterTypes]) ?? throw new MissingMethodException(type.FullName, name);
            return Expression.Call(method, [NodeManager, Chunk, Position, ..expressions]);
        }

        public static Expression Cast(Expression a, Type targetType) => Expression.Convert(a, targetType);

        public static Expression Const(float f) => Expression.Constant(f);
        public static Expression Const(int i) => Expression.Constant(i);
    }

    public enum NoiseValueType : byte
    {
        Float,
        Int,
        Vector2,
        Vector2i,
        Vector3,
        Vector3i
    }

    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    public struct NoiseValue
    {
        [FieldOffset(0)] public NoiseValueType Type;

        [FieldOffset(4)] public float FloatValue;
        [FieldOffset(4)] public int   IntValue;
        [FieldOffset(4)] public Vector2   Vector2Value;
        [FieldOffset(4)] public Vector2i  Vector2iValue;
        [FieldOffset(4)] public Vector3   Vector3Value;
        [FieldOffset(4)] public Vector3i  Vector3iValue;        
        
        public static NoiseValue Float(float v)   => new() { Type = NoiseValueType.Float,   FloatValue = v };
        public static NoiseValue Int(int v)       => new() { Type = NoiseValueType.Int,     IntValue = v };
        public static NoiseValue Vec2(Vector2 v)  => new() { Type = NoiseValueType.Vector2, Vector2Value = v };
        public static NoiseValue Vec2i(Vector2i v)=> new() { Type = NoiseValueType.Vector2i,Vector2iValue = v };
        public static NoiseValue Vec3(Vector3 v)  => new() { Type = NoiseValueType.Vector3, Vector3Value = v };
        public static NoiseValue Vec3i(Vector3i v)=> new() { Type = NoiseValueType.Vector3i,Vector3iValue = v };

        public static NoiseValue Default => Vec3(PBG.MathLibrary.Vector3.Zero);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Float()
        {
            return Type switch
            {
                NoiseValueType.Float    => FloatValue,
                NoiseValueType.Int      => IntValue,
                NoiseValueType.Vector2  => Vector2Value.X,
                NoiseValueType.Vector2i => Vector2iValue.X,
                NoiseValueType.Vector3  => Vector3Value.X,
                NoiseValueType.Vector3i => Vector3iValue.X,
                _ => throw new InvalidOperationException($"Invalid NoiseValue type {Type}")
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Int()
        {
            return Type switch
            {
                NoiseValueType.Float    => (int)FloatValue,
                NoiseValueType.Int      => IntValue,
                NoiseValueType.Vector2  => (int)Vector2Value.X,
                NoiseValueType.Vector2i => Vector2iValue.X,
                NoiseValueType.Vector3  => (int)Vector3Value.X,
                NoiseValueType.Vector3i => Vector3iValue.X,
                _ => throw new InvalidOperationException($"Invalid NoiseValue type {Type}")
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 Vector2()
        {
            return Type switch
            {
                NoiseValueType.Float    => new Vector2(FloatValue),
                NoiseValueType.Int      => new Vector2(IntValue),
                NoiseValueType.Vector2  => Vector2Value,
                NoiseValueType.Vector2i => Vector2iValue,           // implicit conversion exists in Godot
                NoiseValueType.Vector3  => Vector3Value.Xy,
                NoiseValueType.Vector3i => Vector3iValue.Xy,
                _ => throw new InvalidOperationException($"Invalid NoiseValue type {Type}")
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2i Vector2i()
        {
            return Type switch
            {
                NoiseValueType.Float    => new Vector2i((int)FloatValue),
                NoiseValueType.Int      => new Vector2i(IntValue),
                NoiseValueType.Vector2  => new Vector2i((int)Vector2Value.X, (int)Vector2Value.Y),
                NoiseValueType.Vector2i => Vector2iValue,
                NoiseValueType.Vector3  => new Vector2i((int)Vector3Value.X, (int)Vector3Value.Y),
                NoiseValueType.Vector3i => new Vector2i(Vector3iValue.X, Vector3iValue.Y),
                _ => throw new InvalidOperationException($"Invalid NoiseValue type {Type}")
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Vector3()
        {
            return Type switch
            {
                NoiseValueType.Float    => new Vector3(FloatValue, FloatValue, FloatValue),
                NoiseValueType.Int      => new Vector3(IntValue, IntValue, IntValue),
                NoiseValueType.Vector2  => new Vector3(Vector2Value.X, Vector2Value.Y, 0f),
                NoiseValueType.Vector2i => new Vector3(Vector2iValue.X, Vector2iValue.Y, 0f),
                NoiseValueType.Vector3  => Vector3Value,
                NoiseValueType.Vector3i => new Vector3(Vector3iValue.X, Vector3iValue.Y, Vector3iValue.Z),
                _ => throw new InvalidOperationException($"Invalid NoiseValue type {Type}")
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3i Vector3i()
        {
            return Type switch
            {
                NoiseValueType.Float    => new Vector3i((int)FloatValue, (int)FloatValue, (int)FloatValue),
                NoiseValueType.Int      => new Vector3i(IntValue, IntValue, IntValue),
                NoiseValueType.Vector2  => new Vector3i((int)Vector2Value.X, (int)Vector2Value.Y, 0),
                NoiseValueType.Vector2i => new Vector3i(Vector2iValue.X, Vector2iValue.Y, 0),
                NoiseValueType.Vector3  => new Vector3i((int)Vector3Value.X, (int)Vector3Value.Y, (int)Vector3Value.Z),
                NoiseValueType.Vector3i => Vector3iValue,
                _ => throw new InvalidOperationException($"Invalid NoiseValue type {Type}")
            };
        }

        public NoiseValue Copy() => this; // value-type copy is free

        public override string ToString()
        {
            return Type switch
            {
                NoiseValueType.Float    => $"FloatValue: {FloatValue}",
                NoiseValueType.Int      => $"IntValue: {IntValue}",
                NoiseValueType.Vector2  => $"Vector2Value: {Vector2Value}",
                NoiseValueType.Vector2i => $"Vector2iValue: {Vector2iValue}",
                NoiseValueType.Vector3  => $"Vector3Value: {Vector3Value}",
                NoiseValueType.Vector3i => $"Vector3iValue: {Vector3iValue}",
                _ => $"Unknown({Type})"
            };
        }
    }
}