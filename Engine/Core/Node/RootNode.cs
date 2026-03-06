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
            if (!Scene.PendingList.Contains(node))
                Scene.PendingList.Add(node);
            return node;
        }

        public TransformNode[] AddNode(params string[] names)
        {
            TransformNode[] nodes = new TransformNode[names.Length];
            for (int i = 0; i < names.Length; i++)
                nodes[i] = AddNode(names[i]);
            return nodes;
        }

        public void Resize()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                child.Resize();
            }
        }

        public void Start()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                child.Start();
            }
        }

        public void Awake()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                child.Awake();
            }
        }

        public void Update()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                child.Update();
            }
        }

        public void FixedUpdate()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                child.FixedUpdate();
                
            }
        }

        public void Render()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                child.Render();
            }
        }

        public void LateUpdate()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                child.LateUpdate();
            }
        }

        public void Exit()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                child.Exit();
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                child.Dispose();
            }
        }
    }
}