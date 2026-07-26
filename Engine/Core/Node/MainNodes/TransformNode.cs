using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using PBG.Core;
using PBG.MathLibrary;

namespace PBG.Core
{
    public unsafe class TransformNode : Node
    {
        public string Name;
        public Vector3 Position = Vector3.Zero;
        public Vector3 Scale = Vector3.One;
        public Quaternion Rotation = Quaternion.Identity;

        private HashSet<ScriptingNode> _scriptHash = [];
        private List<ScriptingNode> _addedNodes = [];
        public List<ScriptingNode> Components = new();

        public bool Disabled = false;

        public TransformNode ParentNode = null!;

        internal TransformNode(string name, Scene scene)
        {
            Name = name;
            Scene = scene;
        }

        public void AddComponent(ScriptingNode component)
        {
            component.Transform = this;
            component.Scene = Scene;

            if (_scriptHash.Add(component))
            {
                Components.Add(component); 
                _addedNodes.Add(component);
            }
        }

        public void RemoveScript(ScriptingNode script)
        {
            if (_scriptHash.Remove(script))
            {
                Components.Remove(script); 
                if (script.GetMethod("Exit", out var mi)) new ScriptCall(script, mi).Invoke();
                if (script.GetMethod("Dispose", out mi)) new ScriptCall(script, mi).Invoke();
            }
        }

        public void InitAwake(ScriptInfo info)
        {
            for (int i = 0; i < Components.Count; i++)
            {
                var component = Components[i];
                if (component.GetMethod("Awake", out var mi)) info.OnAwake.Add(new(component, mi));
            }

            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].InitAwake(info);
            }
        }

        internal void InitPendingComponents(ScriptInfo info)
        {
            for (int i = 0; i < Components.Count; i++)
            {
                var component = Components[i];
                if (component.GetMethod("Resize", out var mi))  info.OnResize.Add(new(component, mi));
                if (component.GetMethod("FixedUpdate", out mi)) info.OnFixedUpdate.Add(new(component, mi));
                if (component.GetMethod("Update", out mi))      info.OnUpdate.Add(new(component, mi));
                if (component.GetMethod("LateUpdate", out mi))  info.OnLateUpdate.Add(new(component, mi));
                if (component.GetMethod("Compute", out mi))     info.OnCompute.Add(new(component, mi));
                if (component.GetMethod("Render", out mi))      info.OnRender.Add(new(component, mi));
                if (component.GetMethod("Exit", out mi))        info.OnExit.Add(new(component, mi));
                if (component.GetMethod("Dispose", out mi))     info.OnDispose.Add(new(component, mi));
            }

            for (int i = 0; i < _addedNodes.Count; i++)
            {
                var component = _addedNodes[i];
                if (component.GetMethod("Start", out var mi))   info.OnStart.Add(new(component, mi));
                if (component.GetMethod("Awake", out mi))       info.OnAwake.Add(new(component, mi));
            }

            _addedNodes = [];

            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].InitPendingComponents(info);
            }
        }

        private ScriptCall[] ResizeAndCopy(ScriptCall[] existing, int additional)
        {
            var newArr = new ScriptCall[existing.Length + additional];
            Array.Copy(existing, newArr, existing.Length);
            return newArr;
        }

        public T? TryGetComponent<T>() where T : ScriptingNode
        {
            for (int i = 0; i < Components.Count; i++)
            {
                if (Components[i] is T t)
                    return t;
            }
            return null;
        }

        public T GetComponent<T>() where T : ScriptingNode
        {
            for (int i = 0; i < Components.Count; i++)
            {
                if (Components[i] is T t)
                    return t;
            }
            throw new Exception("Component not found");
        }

        public bool GetComponent<T>([NotNullWhen(true)] out T? component) where T : ScriptingNode
        {
            component = null;
            for (int i = 0; i < Components.Count; i++)
            {
                if (Components[i] is T t)
                {
                    component = t;
                    return true;
                }
            }
            return false;
        }

        public void GetComponents<T>(List<T> components) where T : ScriptingNode
        {
            for (int i = 0; i < Components.Count; i++)
            {
                if (Components[i] is T t)
                {
                    components.Add(t);
                }
            }
        }

        public void AddComponent(params ScriptingNode[] components)
        {
            for (int i = 0; i < components.Length; i++)
                AddComponent(components[i]);
        }


        public TransformNode AddChild(string name)
        {
            name = GetUniqueName(name);
            TransformNode node = new(name, Scene);
            Children.Add(node);
            node.ParentNode = this;
            Scene.AddedScripts = true;
            return node;
        }

        public TransformNode[] AddChild(params string[] children)
        {
            TransformNode[] nodes = new TransformNode[children.Length];
            for (int i = 0; i < children.Length; i++)
                nodes[i] = AddChild(children[i]);
            return nodes;
        }

        public override void Delete()
        {
            ParentNode?.RemoveChild(this);
        }
    }
}

public unsafe struct ScriptCall(ScriptingNode node, MethodInfo mi)
{
    public delegate*<ScriptingNode, void> Ptr = (delegate*<ScriptingNode, void>)mi.MethodHandle.GetFunctionPointer();
    public ScriptingNode Instance = node;
    public void Invoke() => Ptr(Instance);
}