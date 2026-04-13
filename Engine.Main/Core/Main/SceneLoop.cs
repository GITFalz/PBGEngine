using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using PBG.Files;

namespace PBG.Core;

[SystemInit(InitPriority.Global)]
public class SceneLoop
{
    public static SceneLoop Scene = new();
    private static Dictionary<string, SceneLoop> _sceneLoops = [];

    private HashSet<ScriptingNode> _scriptHash = [];
    public List<ScriptingNode> Components = [];
    private List<ScriptingNode> _addedNodes = [];

    private unsafe struct ScriptCall(ScriptingNode node, MethodInfo mi)
    {
        public delegate*<ScriptingNode, void> Ptr = (delegate*<ScriptingNode, void>)mi.MethodHandle.GetFunctionPointer();
        public ScriptingNode Instance = node;
        public void Invoke() => Ptr(Instance);
    }

    private ScriptCall[] OnAwake = [];
    private ScriptCall[] OnResize = [];
    private ScriptCall[] OnFixedUpdate = [];
    private ScriptCall[] OnUpdate = [];
    private ScriptCall[] OnLateUpdate = [];
    private ScriptCall[] OnRender = [];
    private ScriptCall[] OnExit = [];
    private ScriptCall[] OnDispose = [];

    private static IEnumerable<T> CreateAllInstances<T>(Assembly? assembly = null) where T : class
    {
        assembly ??= Assembly.GetExecutingAssembly();
        return assembly.GetTypes().Where(t => typeof(T).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass).Select(t => (T)Activator.CreateInstance(t)!);
    }

    public static void Init()
    {
        var baseType = typeof(Game);
        var scriptTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(asm => asm.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t))
            .ToList();

        Game game = new DefaultScene();

        foreach (var c in scriptTypes)
        {
            if (c != typeof(DefaultScene))
            {
                game = (Game)Activator.CreateInstance(c)!;
                
                var scene = new SceneLoop();
                var scripts = game.Initialize();
                foreach (var script in scripts)
                {
                    Scene.AddComponent(script);
                    Scene.InitPendingComponents();
                } 
            }
        }
        
        Scene.Awake();
    }

    public void AddComponent(ScriptingNode component)
    {
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

    public void InitAwake()
    {
        List<ScriptCall> onAwake = [];

        for (int i = 0; i < Components.Count; i++)
        {
            var component = Components[i];
            if (component.GetMethod("Awake", out var mi)) onAwake.Add(new(component, mi));
        }
        
        OnAwake = [..onAwake];
    }

    internal void InitPendingComponents()
    {
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

        for (int i = 0; i < _addedNodes.Count; i++)
        {
            var component = _addedNodes[i];
            if (component.GetMethod("Awake", out var mi))       onAwake.Add(new(component, mi));
        }
        
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

    internal static void AwakeInternal() => Scene.Awake();
    public void Awake()
    {
        for (int i = 0; i < OnAwake.Length; i++) 
            OnAwake[i].Invoke();
        OnAwake = [];
    }

    internal static void ResizeInternal() => Scene.Resize();
    public void Resize()
    {
        for (int i = 0; i < OnResize.Length; i++) 
            OnResize[i].Invoke();
    }

    internal static void FixedUpdateInternal() => Scene.FixedUpdate();
    public void FixedUpdate()
    {
        for (int i = 0; i < OnFixedUpdate.Length; i++) 
            OnFixedUpdate[i].Invoke(); 
    }

    internal static void UpdateInternal() => Scene.Update();
    public void Update()
    {
        for (int i = 0; i < OnUpdate.Length; i++) 
            OnUpdate[i].Invoke(); 
    }

    internal static void LateUpdateInternal() => Scene.LateUpdate();
    public void LateUpdate()
    {
        for (int i = 0; i < OnLateUpdate.Length; i++) 
            OnLateUpdate[i].Invoke(); 
    }

    internal static void RenderInternal() => Scene.Render();
    public void Render()
    {
        for (int i = 0; i < OnRender.Length; i++) 
            OnRender[i].Invoke(); 
    }

    internal static void ExitInternal() => Scene.Exit();
    public void Exit()
    {
        for (int i = 0; i < OnExit.Length; i++) 
            OnExit[i].Invoke(); 
    }

    internal static void DisposeInternal() => Scene.Dispose();
    public void Dispose()
    {
        for (int i = 0; i < OnDispose.Length; i++) 
            OnDispose[i].Invoke(); 
        Components = [];
    }
}