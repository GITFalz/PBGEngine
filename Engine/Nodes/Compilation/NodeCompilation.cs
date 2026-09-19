using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using PBG.Core;
using PBG.MathLibrary;

namespace PBG.Nodes;

public class NodeCompilation
{
    private NodeModule _nodeModule;
    private NodeExpressionCompileContext _nodeCompileContext = new();
    private NodeExpressionBlock _blockExpression = NodeExpression.Block();

    public NodeCompilation(NodeModule module)
    {
        _nodeModule = module;
    }

    public void Sort()
    {
        #region ORGANIZING
        List<NodeBase> outputNodes = [];
        List<NodeBase> dependencyNodes = [];

        foreach(var node in _nodeModule.Nodes)
        {
            if (node.DefinitionType.HasFlag(NodeDefinitionType.IsOutput))
                outputNodes.Add(node);
        }

        foreach(var node in _nodeModule.Nodes)
        {
            if (node.DefinitionType.HasFlag(NodeDefinitionType.Dependency))
                dependencyNodes.Add(node);
        }

        // Create a connection map for both input => output
        Dictionary<NodePointer<TNodeInput>, NodePointer<TNodeOutput>> inputToOutputConnections = [];
        HashSet<NodePointer<TNodeOutput>> connectedOutputs = [];

        foreach (var connection in _nodeModule.Connections)
        {
            var inputPointer = TNodeInput.New(connection.InputNode, connection.Input.Index);
            var outputPointer = TNodeOutput.New(connection.OutputNode, connection.Output.Index);

            inputToOutputConnections.Add(inputPointer, outputPointer);
            connectedOutputs.Add(outputPointer);
        }
        #endregion

        #region SORTING
        Dictionary<NodeBase, int> queueCountMap = [];
        Queue<NodeBase> waiting = [];

        LinkedList<NodeBase> linkedSorted = [];
        Dictionary<NodeBase, LinkedListNode<NodeBase>> sortedMap = [];
        Dictionary<NodeBase, int> sortedIndices = [];

        Console.WriteLine("---- SORTING NODES ----");

        // enqueue output node before any other so they are sorted first
        foreach (var node in outputNodes) { Enqueue(node); }

        foreach (var node in dependencyNodes) { Enqueue(node); }

        void Enqueue(NodeBase node)
        {
            queueCountMap.TryAdd(node, 0);
            queueCountMap[node]++;
            waiting.Enqueue(node);
        }

        bool TryDequeue([NotNullWhen(true)] out NodeBase? node)
        {
            node = null;
            var n = waiting.Dequeue();
            queueCountMap[n]--;
            if (queueCountMap[n] > 0)
                return false;

            node = n;
            return true;
        }

        // Allows for fast queue like sorting
        void MoveSorted(NodeBase node)
        {
            if (sortedMap.TryGetValue(node, out var existing))
            {
                linkedSorted.Remove(existing);
                linkedSorted.AddLast(existing);
            }
            else
            {
                var newNode = linkedSorted.AddLast(node);
                sortedMap[node] = newNode;
            }
        }

        // if a node input is connected to an output, just send it back to the waiting queue
        void CheckInputPointerConnection(NodePointer<TNodeInput> inputPointer)
        {
            if (inputToOutputConnections.TryGetValue(inputPointer, out var outputPointer))
            {
                Enqueue(outputPointer.Node); Console.WriteLine("found connection: " + outputPointer);
            }
        }

        // if a node input is connected to an output, check if the order is correct
        void CheckInputPointerOrder(NodePointer<TNodeInput> inputPointer, NodeBase parent, int depth)
        {
            if (inputToOutputConnections.TryGetValue(inputPointer, out var outputPointer))
            {
                // if the input is connected check if the output is outside the scope of the parent
                if (outputPointer.Node.GetBlockDepth() >= depth)
                    return;

                var parentIndex = sortedIndices[parent];
                var outputIndex = sortedIndices[outputPointer.Node]; Console.WriteLine(outputPointer + " " + parentIndex + " " + outputIndex);
                
                if (outputIndex == -1 || parentIndex == -1)
                    return;

                // if the connected output node is executed after the parent scope, it needs to be reevaluated so it executes before
                if (outputIndex < parentIndex)
                {
                    Enqueue(outputPointer.Node); Console.WriteLine("happens too early");
                }

                
            }
        }

        void SortNodes()
        {
            while (waiting.Count > 0)
            {
                if (!TryDequeue(out var current))
                    continue;

                MoveSorted(current); Console.WriteLine(current.ID + " " + current.Name);

                if (current.ParentBlockNode != null)
                {
                    Enqueue(current.ParentBlockNode); Console.WriteLine("found parent: " + current.ParentBlockNode.ID + " " + current.ParentBlockNode.Name);
                }

                var inputPointer = TNodeInput.New(current, -1); // index is not used to index array, -1 represent the flow connection point
                CheckInputPointerConnection(inputPointer);

                foreach (var input in current.Inputs)
                {
                    inputPointer = TNodeInput.New(current, input.Index);
                    CheckInputPointerConnection(inputPointer);    
                }
            }     
        }

        SortNodes(); Console.WriteLine("---- SEPARATION ----");

        int sortedIndex = 0;
        foreach (var node in linkedSorted)
        {
            sortedIndices[node] = sortedIndex;
            sortedIndex++;
        }
        
        // sometimes nodes outside of an if else but connected to a node inside is able to be called after the if which shouldn't be possible, 
        foreach (var node in linkedSorted)
        {
            // the problems only accurs with parent nodes so if the node has none it can be ignored
            if (node.ParentBlockNode == null)
                continue;

            int depth = node.GetBlockDepth();

            foreach (var input in node.Inputs)
            {
                var inputPointer = TNodeInput.New(node, input.Index); Console.WriteLine(inputPointer);
                CheckInputPointerOrder(inputPointer, node.ParentBlockNode, depth);
            }
        } Console.WriteLine("---- SEPARATION ----");

        // rerun the algorithm if the scope ordering found a node that happens before a parent scope
        SortNodes(); Console.WriteLine("---- END ----"); Console.WriteLine("---- SORTED NODES ----");

        List<NodeBase> sorted = [.. linkedSorted.Reverse()]; // reverse the so the node at index 0 is the first to execute
        #endregion

        #region EXECUTION BUILDER
        Dictionary<NodePointer<TNodeOutput>, int> expressions = [];

        // lists to keep track
        List<NodeExpressionVariable> variables = [];
        List<NodeExpression> actions = [];

        // list of outside data
        List<NodeExpressionVariable> baseVariables = [];
        List<NodeExpression> baseActions = [];

        Dictionary<NodePointer<TNodeBlock>, (List<NodeExpressionVariable> variables, List<NodeExpression> actions)> blockData = [];

        Dictionary<NodeBase, int> blockIndices = [];

        NodeExpression defaultExpression = NodeExpression.Constant(0);

        // Some utility functions
        void PopulateInputArray(NodeBase node, NodeExpression[] inputExpressions)
        {
            for (int j = 0; j < node.Inputs.Length; j++)
            {
                inputExpressions[j] = defaultExpression;

                var input = node.Inputs[j];
                var inputPointer = TNodeInput.New(node, input.Index);

                // for every input, get the connected output variable
                if (inputToOutputConnections.TryGetValue(inputPointer, out var outputPointer) && expressions.TryGetValue(outputPointer, out var expressionIndex))
                {
                    inputExpressions[j] = variables[expressionIndex];
                }
                else if (input.Value != null)
                {
                    inputExpressions[j] = NodeExpression.Constant(input.Value);
                }
            }
        }
        
        // This code goes through every node in order to get it's epxressions
        foreach (var node in sorted)
        {
            if (node.DefinitionType.HasFlag(NodeDefinitionType.Block))
            {
                // Block nodes (if, for, while, etc.) contain nested scopes.
                // Unlike sequential nodes, a block cannot be fully compiled until
                // every expression inside it has already been compiled.
                //
                // Therefore we insert a placeholder Block expression now and
                // remember its index. Later, once the inner nodes are processed,
                // we replace the placeholder with the finished block.
                if (node.ParentBlockNode != null)
                {
                    var blockBlockPointer = TNodeBlock.New(node.ParentBlockNode, node.ParentBlockIndex);
                    blockData.TryAdd(blockBlockPointer, ([], []));
                    blockIndices.TryAdd(node, blockData[blockBlockPointer].actions.Count);
                    blockData[blockBlockPointer].actions.Add(NodeExpression.Block()); // placeholder
                }
                else
                {
                    blockIndices.TryAdd(node, baseActions.Count);
                    baseActions.Add(NodeExpression.Block()); // placeholder
                }

                continue;
            } Console.WriteLine("node: " + node.ID + " " + node.Name);

            NodeExpression[] inputExpressions = new NodeExpression[node.Inputs.Length];
            NodeExpression[] outputExpressions = new NodeExpression[node.Outputs.Length];

            PopulateInputArray(node, inputExpressions);
            
            for (int i = 0; i < node.Outputs.Length; i++)
                outputExpressions[i] = defaultExpression;

            NodeExecutionContext context = new(defaultExpression, inputExpressions, outputExpressions, []);
            var action = node.Template.RunGPUExecute(context);

            // Create a pointer to the parent scope
            NodePointer<TNodeBlock>? blockPointer = null;
            if (node.ParentBlockNode != null)
            {
                blockPointer = new NodePointer<TNodeBlock>(node.ParentBlockNode, node.ParentBlockIndex);
            }

            // A node’s execution is split into two stages:
            //
            // 1. Call stage - side-effectful work that does not produce a value
            //    (e.g. a void function call, nested statements, or any code that
            //    simply needs to run).
            //
            // 2. Assignment stage - the value that flows out of the node’s output
            //    connection (e.g. the result of an Add node, a function return value,
            //    etc.).

            // Call stage
            if (action != null)
            {
                actions.Add(action);

                if (blockPointer == null)
                {
                    baseActions.Add(action);
                }
                else
                {
                    blockData.TryAdd(blockPointer.Value, ([], []));
                    blockData[blockPointer.Value].actions.Add(action);
                }
            }

            // Assignment stage
            for (int i = 0; i < node.Outputs.Length; i++)
            {
                var output = node.Outputs[i];
                var outputExpression = outputExpressions[i]; Console.WriteLine($"Output expression is: " + outputExpression.GetType().Name);

                var outputPointer = TNodeOutput.New(node, output.Index);

                if (!connectedOutputs.Contains(outputPointer))
                    continue;

                // Add the variable for later nodes
                if (expressions.TryAdd(outputPointer, variables.Count))
                {
                    var variable = NodeExpression.Variable(outputExpression.ValueType);
                    var assign = NodeExpression.Assign(variable, outputExpression);

                    variables.Add(variable);
                    actions.Add(assign);

                    if (blockPointer == null)
                    {
                        baseVariables.Add(variable);
                        baseActions.Add(assign);
                    }
                    else
                    {
                        blockData.TryAdd(blockPointer.Value, ([], []));
                        blockData[blockPointer.Value].variables.Add(variable);
                        blockData[blockPointer.Value].actions.Add(assign);
                    }
                }
                else
                {
                    Console.WriteLine($"[Warning] : Output '{outputPointer}' already exists in variable pointer dictionnary");
                }
            }
        }

        Console.WriteLine("---- BLOCK HANDLING ----");

        // When the default nodes are done compiling, we need to group them by their parent scopes
        for (int i = sorted.Count - 1; i >= 0; i--)
        {
            var node = sorted[i];
            if (!node.DefinitionType.HasFlag(NodeDefinitionType.Block))
                continue;

            Console.WriteLine(node.ID + " " + node.Name);
            if (!blockIndices.TryGetValue(node, out var index))
            {
                Console.WriteLine("get index failed for block nodes");
                continue;
            }

            NodeExpression[] inputExpressions = new NodeExpression[node.Inputs.Length];
            NodeExpressionBlock[] blockExpressions = new NodeExpressionBlock[node.Blocks.Length];

            PopulateInputArray(node, inputExpressions);

            // Contrary to default nodes who can have both a call stage or a assignment stage, scopes only have a call stage
            for (int j = 0; j < node.Blocks.Length; j++)
            {
                var blockPointer = TNodeBlock.New(node, j); Console.WriteLine(blockPointer);
                if (blockData.TryGetValue(blockPointer, out var data))
                {
                    Console.WriteLine(data.variables.Count + " " + data.actions.Count);
                    var block = NodeExpression.Block([.. data.variables], [.. data.actions]);
                    blockExpressions[j] = block;
                }    
                else
                {
                    blockExpressions[j] = NodeExpression.Block();
                    Console.WriteLine($"[Warning] : Could not find pointer '{blockPointer}' in block data dictionnary for node '{node.ID}'");
                }
            }

            NodeExecutionContext context = new(defaultExpression, inputExpressions, [], blockExpressions);

            var action = node.Template.RunGPUExecute(context);

            if (node.ParentBlockNode != null && blockData.TryGetValue(TNodeBlock.New(node.ParentBlockNode, node.ParentBlockIndex), out var data2))
            {
                if (action != null)
                    data2.actions[index] = action;
            }
            else
            {
                if (action != null)
                    baseActions[index] = action;
            }
        }

        _blockExpression = NodeExpression.Block([..baseVariables], [..baseActions]);
        #endregion

        
    }

    public void Compile()
    {
        #region COMPILING
        _nodeCompileContext.Clear();
        _blockExpression.Compile(_nodeCompileContext);
        #endregion
    }

    public void Print()
    {
        Console.WriteLine(_nodeCompileContext);
    }

    public string GetCode()
    {
        return _nodeCompileContext.ToString();
    }
}

public struct NodeExecutionContext(NodeExpression defaultExpression, NodeExpression[] inputExpressions, NodeExpression[] outputExpressions, NodeExpressionBlock[] blockExpressions)
{
    private NodeExpression[] _inputExpressions = inputExpressions;
    private NodeExpression[] _outputExpressions = outputExpressions;
    private NodeExpressionBlock[] _blockExpressions = blockExpressions;
    
    public readonly NodeExpression GetInput(int index)
    {
        if (index < 0 || index >= _inputExpressions.Length)
            return defaultExpression;
        return _inputExpressions[index];
    }

    public readonly NodeExpressionBlock GetBlock(int index)
    {
        if (index < 0 || index >= _blockExpressions.Length)
            return NodeExpression.Block();
        return _blockExpressions[index];
    }
    
    public readonly void SetOutput(int index, NodeExpression expression)
    {
        if (index >= 0 && index < _outputExpressions.Length)
            _outputExpressions[index] = expression;
    }
}

public readonly struct NodeExecutionOrder(NodeBase node, int execution)
{
    public readonly NodeBase Node = node;
    public readonly int Execution = execution;
}

public readonly struct NodePointer<T>(NodeBase node, int index)
{
    public readonly NodeBase Node = node;
    public readonly int Index = index;

    public static bool operator ==(NodePointer<T> a, NodePointer<T> b) => a.Node.ID == b.Node.ID && a.Index == b.Index;
    public static bool operator !=(NodePointer<T> a, NodePointer<T> b) => a.Node.ID != b.Node.ID || a.Index != b.Index;
    
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is NodePointer<T> v)
            return this == v;
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Node.ID, Index);
    }

    public override string ToString()
    {
        return $"[{typeof(T).Name}] : Node {Node.ID} - Index {Index}";
    }
}

public readonly struct TNodeInput { public static NodePointer<TNodeInput> New(NodeBase node, int index) => new(node, index); }
public readonly struct TNodeOutput { public static NodePointer<TNodeOutput> New(NodeBase node, int index) => new(node, index); }
public readonly struct TNodeBlock { public static NodePointer<TNodeBlock> New(NodeBase node, int index) => new(node, index); }