using System.Diagnostics;

namespace PBG.Core
{
    public class RootNode : Node
    {
        public RootNode(Scene scene)
        {
            Scene = scene;
        }

        public TransformNode AddNode(string name)
        {
            name = GetUniqueName(name);
            var node = new TransformNode(name, Scene);
            Children.Add(node);
            Scene.AddedScripts = true;
            return node;
        }

        public TransformNode[] AddNode(params string[] names)
        {
            TransformNode[] nodes = new TransformNode[names.Length];
            for (int i = 0; i < names.Length; i++)
                nodes[i] = AddNode(names[i]);
            return nodes;
        }

        internal void InitAwake(ScriptInfo info)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].InitAwake(info);
            }
        }

        internal void InitPendingComponents(ScriptInfo info)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].InitPendingComponents(info);
            }
        }
    }
}