using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using PBG.MathLibrary;

namespace PBG.Core
{
    public unsafe class TransformNode : Node
    {
        public string Name;
        public Vector3 Position = Vector3.Zero;
        public Vector3 Scale = Vector3.One;
        public Quaternion Rotation = Quaternion.Identity;

        private List<ScriptingNode> _pendingComponents = [];
        public List<ScriptingNode> Components = new();

        public bool Disabled = false;

        public TransformNode ParentNode = null!;

        private unsafe struct ScriptCall(ScriptingNode node)
        {
            public delegate*<ScriptingNode, void> Ptr;
            public ScriptingNode Instance = node;
            public void Invoke() => Ptr(Instance);
        }

        private ScriptCall[] OnStart = [];
        private ScriptCall[] OnAwake = [];
        private ScriptCall[] OnResize = [];
        private ScriptCall[] OnFixedUpdate = [];
        private ScriptCall[] OnUpdate = [];
        private ScriptCall[] OnLateUpdate = [];
        private ScriptCall[] OnRender = [];
        private ScriptCall[] OnExit = [];
        private ScriptCall[] OnDispose = [];

        internal TransformNode(string name, Scene scene)
        {
            Name = name;
            Scene = scene;
        }

        public void AddComponent(ScriptingNode component)
        {
            component.Transform = this;
            component.Scene = Scene;

            if (!_pendingComponents.Contains(component) && !Components.Contains(component))
            {
                // Add the component to the main list.
                // Note: Its logic/code shouldn't execute immediately. This ensures that
                // the node tree updates instantly, while scripts with dependencies on other
                // components are added together to maintain synchronization.
                Components.Add(component); 

                _pendingComponents.Add(component);
            }
        }

        public override void InitAwake()
        {
            int awakeCount = 0;

            for (int i = 0; i < Components.Count; i++)
            {
                var component = Components[i];
                if (!_pendingComponents.Contains(component) && component.GetMethod("Awake", out _)) awakeCount++;
            }

            OnAwake = new ScriptCall[awakeCount];
            int a = 0;

            for (int i = 0; i < Components.Count; i++)
            {
                var component = Components[i];
                if (!_pendingComponents.Contains(component) && component.GetMethod("Awake", out MethodInfo? mi)) OnAwake[a++] = new(component) { Ptr = (delegate*<ScriptingNode, void>)mi.MethodHandle.GetFunctionPointer() };
            }

            base.InitAwake();
        }

        public void InitPendingComponents()
        {
            int startCount = 0, awakeCount = 0, resizeCount = 0, fixedUpdateCount = 0;
            int updateCount = 0, lateUpdateCount = 0, renderCount = 0, exitCount = 0, disposeCount = 0;

            for (int i = 0; i < _pendingComponents.Count; i++)
            {
                var component = _pendingComponents[i];
                if (component.GetMethod("Start", out _)) startCount++;
                if (component.GetMethod("Awake", out _)) awakeCount++;
                if (component.GetMethod("Resize", out _)) resizeCount++;
                if (component.GetMethod("FixedUpdate", out _)) fixedUpdateCount++;
                if (component.GetMethod("Update", out _)) updateCount++;
                if (component.GetMethod("LateUpdate", out _)) lateUpdateCount++;
                if (component.GetMethod("Render", out _)) renderCount++;
                if (component.GetMethod("Exit", out _)) exitCount++;
                if (component.GetMethod("Dispose", out _)) disposeCount++;
            }

            OnStart      = ResizeAndCopy(OnStart, startCount);
            OnAwake      = ResizeAndCopy(OnAwake, awakeCount);
            OnResize     = ResizeAndCopy(OnResize, resizeCount);
            OnFixedUpdate= ResizeAndCopy(OnFixedUpdate, fixedUpdateCount);
            OnUpdate     = ResizeAndCopy(OnUpdate, updateCount);
            OnLateUpdate = ResizeAndCopy(OnLateUpdate, lateUpdateCount);
            OnRender     = ResizeAndCopy(OnRender, renderCount);
            OnExit       = ResizeAndCopy(OnExit, exitCount);
            OnDispose    = ResizeAndCopy(OnDispose, disposeCount);

            int s = OnStart.Length - startCount;
            int a = OnAwake.Length - awakeCount;
            int r = OnResize.Length - resizeCount;
            int f = OnFixedUpdate.Length - fixedUpdateCount;
            int u = OnUpdate.Length - updateCount;
            int l = OnLateUpdate.Length - lateUpdateCount;
            int re= OnRender.Length - renderCount;
            int e = OnExit.Length - exitCount;
            int d = OnDispose.Length - disposeCount;

            for (int i = 0; i < _pendingComponents.Count; i++)
            {
                var component = _pendingComponents[i];
                if (component.GetMethod("Start", out var mi))   OnStart[s++]       = new(component) { Ptr = (delegate*<ScriptingNode, void>)mi.MethodHandle.GetFunctionPointer()};
                if (component.GetMethod("Awake", out mi))       OnAwake[a++]       = new(component) { Ptr = (delegate*<ScriptingNode, void>)mi.MethodHandle.GetFunctionPointer()};
                if (component.GetMethod("Resize", out mi))      OnResize[r++]      = new(component) { Ptr = (delegate*<ScriptingNode, void>)mi.MethodHandle.GetFunctionPointer()};
                if (component.GetMethod("FixedUpdate", out mi)) OnFixedUpdate[f++] = new(component) { Ptr = (delegate*<ScriptingNode, void>)mi.MethodHandle.GetFunctionPointer()};
                if (component.GetMethod("Update", out mi))      OnUpdate[u++]      = new(component) { Ptr = (delegate*<ScriptingNode, void>)mi.MethodHandle.GetFunctionPointer()};
                if (component.GetMethod("LateUpdate", out mi))  OnLateUpdate[l++]  = new(component) { Ptr = (delegate*<ScriptingNode, void>)mi.MethodHandle.GetFunctionPointer()};
                if (component.GetMethod("Render", out mi))      OnRender[re++]     = new(component) { Ptr = (delegate*<ScriptingNode, void>)mi.MethodHandle.GetFunctionPointer()};
                if (component.GetMethod("Exit", out mi))        OnExit[e++]        = new(component) { Ptr = (delegate*<ScriptingNode, void>)mi.MethodHandle.GetFunctionPointer()};
                if (component.GetMethod("Dispose", out mi))     OnDispose[d++]     = new(component) { Ptr = (delegate*<ScriptingNode, void>)mi.MethodHandle.GetFunctionPointer()};
            }

            _pendingComponents.Clear();
        }

        private ScriptCall[] ResizeAndCopy(ScriptCall[] existing, int additional)
        {
            var newArr = new ScriptCall[existing.Length + additional];
            Array.Copy(existing, newArr, existing.Length);
            return newArr;
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
            
            if (!Scene.PendingList.Contains(node))
                Scene.PendingList.Add(node);

            return node;
        }

        public TransformNode[] AddChild(params string[] children)
        {
            TransformNode[] nodes = new TransformNode[children.Length];
            for (int i = 0; i < children.Length; i++)
                nodes[i] = AddChild(children[i]);
            return nodes;
        }

        public void Start()
        {
            for (int i = 0; i < OnStart.Length; i++) OnStart[i].Invoke();
            OnStart = [];
            for (int i = 0; i < Children.Count; i++)
                Children[i].Start();
        }
        
        public void Awake()
        {
            for (int i = 0; i < OnAwake.Length; i++) OnAwake[i].Invoke();
            OnAwake = [];
            for (int i = 0; i < Children.Count; i++)
                Children[i].Awake();
        }

        public void Resize()
        {
            for (int i = 0; i < OnResize.Length; i++) OnResize[i].Invoke();
            for (int i = 0; i < Children.Count; i++)
                Children[i].Resize();
        }

        public void FixedUpdate()
        {
            if (Disabled)
                return;

            for (int i = 0; i < OnFixedUpdate.Length; i++) OnFixedUpdate[i].Invoke(); 
            for (int i = 0; i < Children.Count; i++)
                Children[i].FixedUpdate();
        }

        public void Update()
        {
            if (Disabled)
                return;

            for (int i = 0; i < OnUpdate.Length; i++) OnUpdate[i].Invoke(); 
            for (int i = 0; i < Children.Count; i++)
                Children[i].Update();
        }

        public void LateUpdate()
        {
            if (Disabled)
                return;

            for (int i = 0; i < OnLateUpdate.Length; i++) OnLateUpdate[i].Invoke(); 
            for (int i = 0; i < Children.Count; i++)
                Children[i].LateUpdate();
        }

        public void Render()
        {
            if (Disabled)
                return;

            for (int i = 0; i < OnRender.Length; i++) OnRender[i].Invoke(); 
            for (int i = 0; i < Children.Count; i++)
                Children[i].Render();
        }

        public void Exit()
        {
            for (int i = 0; i < OnExit.Length; i++) OnExit[i].Invoke(); 
            for (int i = 0; i < Children.Count; i++)
                Children[i].Exit();
        }

        public void Dispose()
        {
            for (int i = 0; i < OnDispose.Length; i++) OnDispose[i].Invoke(); 
            for (int i = 0; i < Children.Count; i++)
                Children[i].Dispose();

            Components = [];
            Children = [];
        }

        public override void Delete()
        {
            ParentNode?.RemoveChild(this);
            Dispose();
        }
    }
}