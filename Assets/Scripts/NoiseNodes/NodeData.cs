using System.Linq.Expressions;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.Assets.Scripts.NoiseNodes.Nodes;
using PBG.LoaderConfig;

public class NodeData(NodeBase node)
{
    public NodeBase Node = node;
    public List<NodeData> Children = [];

    public void Print(int o = 0)
    {
        Console.WriteLine(PBG.String.Repeat(" ", o) + "" + Node.GetName() + " has parent cache? " + (Node.ParentCacheNode != null));

        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            child.Print(o + 4);
        }
    }

    public NoiseNode? GenerateNoiseNode(NoiseNodeManager manager, ref int index)
    {
        if (Node is IfElseNode ifElseNode)
        {
            var ifElseNoiseNode = ifElseNode.GenerateNoiseNode(manager, ref index);
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                var childNode = child.GenerateNoiseNode(manager, ref index);
                //if (childNode != null)
                    //ifElseNoiseNode.ActionMap.Add(childNode);
            }
            return ifElseNoiseNode;
        }
        else if (Node is CacheNode cacheNode)
        {
            Console.WriteLine("Is cache node in node data and has " + Children.Count + " children");
            var cacheNoiseNode = cacheNode.GenerateNoiseNode(manager, ref index);
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                var childNode = child.GenerateNoiseNode(manager, ref index);
                if (childNode != null)
                    cacheNoiseNode.ActionMap.Add(childNode);
            }
            return cacheNoiseNode;
        }
        else if (Node is StructureNode structureNode)
        {
            return structureNode.GenerateNoiseNode(manager, ref index);
        }
        else if (Node is CustomNode customNode)
        {
            return customNode.GenerateNoiseNode(manager, ref index);
        }
        else if (Node is GroupNode groupNode)
        {
            return groupNode.GenerateNoiseNode(manager, ref index);
        }
        return null;
    }


    public NoiseNode? BuildExpression(Dictionary<NodeBase, Dictionary<string, SetterValue>> connections, List<Expression> expressions)
    {
        if (Node is IfElseNode ifElseNode)
        {
            var ifElseNoiseNode = ifElseNode.BuildExpression(connections, expressions);
            if (ifElseNoiseNode == null || ifElseNoiseNode is not IfElseNoiseNode ifElseNoiseNode1)
                return null;

            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                var childNode = child.BuildExpression(connections, expressions);
                if (childNode != null)
                {
                    var expr = NoiseNode.BuildExpression(childNode);
                    ifElseNoiseNode1.Expressions.Add(expr);
                    ifElseNoiseNode1.Setters.AddRange(childNode.Setters);
                }
            }
            return ifElseNoiseNode;
        }
        else if (Node is CacheNode cacheNode)
        {
            var cacheNoiseNode = cacheNode.BuildExpression(connections, expressions);
            return cacheNoiseNode;
        }
        else if (Node is StructureNode structureNode)
        {
            return structureNode.BuildExpression(connections, expressions);
        }
        else if (Node is CustomNode customNode)
        {
            return customNode.BuildExpression(connections, expressions);
        }
        else if (Node is GroupNode groupNode)
        {
            return groupNode.BuildExpression(connections, expressions);
        }
        return null;
    }


    public NodeConfig? Save()
    {
        NodeConfig config = Node.Save();
        Console.WriteLine("Saving " + Node.GetName() + " " + config.GetType().Name);
        if (config is EmptyNodeConfig)
            return null;

        if (Node is IfElseNode && config is IfElseNodeConfig ifElseNodeConfig)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var childConfig = Children[i].Save();
                if (childConfig != null)
                    ifElseNodeConfig.Nodes.Add(childConfig);
            }
        }
        else if (Node is CacheNode && config is CacheNodeConfig cacheNodeConfig)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var childConfig = Children[i].Save();
                if (childConfig != null)
                    cacheNodeConfig.Nodes.Add(childConfig);
            }
        }
        return config;
    }
}