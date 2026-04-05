using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using PBG.MathLibrary;


namespace PBG.Core
{
    public unsafe class TransformNode : Node
    {
        public Vector3 Position = Vector3.Zero;
        public Vector3 Scale = Vector3.One;

        public Quaternion Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                _eulerRotation = Mathf.RadiansToDegrees(_rotation.ToEuler());
            }
        }
        private Quaternion _rotation = Quaternion.Identity;

        public Vector3 EulerRotation
        {
            get => _eulerRotation;
            set
            {
                _eulerRotation = value;
                _rotation = Quaternion.FromEuler(Mathf.DegreesToRadians(_eulerRotation));
            }
        }
        private Vector3 _eulerRotation = Vector3.Zero;

        private HashSet<ScriptingNode> _scriptHash = [];
        public List<ScriptingNode> Components = new();
        private List<ScriptingNode> _addedNodes = [];

        public bool Disabled = false;

        private unsafe struct ScriptCall(ScriptingNode node, MethodInfo mi)
        {
            public delegate*<ScriptingNode, void> Ptr = (delegate*<ScriptingNode, void>)mi.MethodHandle.GetFunctionPointer();
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

        internal TransformNode()
        {
            Name = "";
        }

        public TransformNode Copy()
        {
            var node = new TransformNode(Name, Scene)
            {
                Position = Position,
                Scale = Scale,
                Rotation = Rotation
            };

            for (int i = 0; i < Components.Count; i++)
            {
                var script = Components[i];
                var instance = script.Copy();
                if (instance == null)
                    continue;

                node.AddComponent(instance);
            }

            for (int i = 0; i < Children.Count; i++)
            {
                var childNode = Children[i];
                var copy = childNode.Copy();
                AddNode(copy);
            }

            return node;
        }

        public void AddComponent(ScriptingNode component)
        {
            component.Transform = this;
            component.Scene = Scene;

            if (_scriptHash.Add(component))
            {
                Components.Add(component); 
                _addedNodes.Add(component);
                Scene.SetAsPending(this);
            }
        }

        public void RemoveScript(ScriptingNode script)
        {
            if (_scriptHash.Remove(script))
            {
                Components.Remove(script); 
                if (script.GetMethod("Exit", out var mi)) new ScriptCall(script, mi).Invoke();
                if (script.GetMethod("Dispose", out mi)) new ScriptCall(script, mi).Invoke();
                Scene.SetAsPending(this);
            }
        }

        public Matrix4 GetModelMatrix() => Matrix4.CreateTranslation(Position) * Matrix4.CreateFromQuaternion(Rotation) * Matrix4.CreateScale(Scale);

        // Awake is called when a script is loaded, Start is only called once when the script is created
        public override void InitAwake()
        {
            if (Scene.IsPending(this))
                return;

            List<ScriptCall> onAwake = [];

            for (int i = 0; i < Components.Count; i++)
            {
                var component = Components[i];
                if (component.GetMethod("Awake", out var mi)) onAwake.Add(new(component, mi));
            }
            
            OnAwake = [..onAwake];

            base.InitAwake();
        }

        internal void InitPendingComponents()
        {
            List<ScriptCall> onStart = [];
            List<ScriptCall> onAwake = [];
            List<ScriptCall> onResize = [];
            List<ScriptCall> onFixedUpdate = [];
            List<ScriptCall> onUpdate = [];
            List<ScriptCall> onLateUpdate = [];
            List<ScriptCall> onRender = [];
            List<ScriptCall> onExit = [];
            List<ScriptCall> onDispose = [];

            for (int i = 0; i < Components.Count; i++)
            {
                var component = Components[i];
                if (component.GetMethod("Resize", out var mi))  onResize.Add(new(component, mi));
                if (component.GetMethod("FixedUpdate", out mi)) onFixedUpdate.Add(new(component, mi));
                if (component.GetMethod("Update", out mi))      onUpdate.Add(new(component, mi));
                if (component.GetMethod("LateUpdate", out mi))  onLateUpdate.Add(new(component, mi));
                if (component.GetMethod("Render", out mi))      onRender.Add(new(component, mi));
                if (component.GetMethod("Exit", out mi))        onExit.Add(new(component, mi));
                if (component.GetMethod("Dispose", out mi))     onDispose.Add(new(component, mi));
            }

            // only newly added components can have Start and Awake called
            for (int i = 0; i < _addedNodes.Count; i++)
            {
                var component = _addedNodes[i];
                if (component.GetMethod("Start", out var mi))   onStart.Add(new(component, mi));
                if (component.GetMethod("Awake", out mi))       onAwake.Add(new(component, mi));
            }
            
            OnStart =       [..onStart];
            OnAwake =       [..onAwake];
            OnResize =      [..onResize];
            OnFixedUpdate = [..onFixedUpdate];
            OnUpdate =      [..onUpdate];
            OnLateUpdate =  [..onLateUpdate];
            OnRender =      [..onRender];
            OnExit =        [..onExit];
            OnDispose =     [..onDispose];

            _addedNodes = [];
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