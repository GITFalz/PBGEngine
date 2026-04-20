using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using PBG.Data;
using PBG.Graphics;
using PBG.Rendering;
using PBG.UI;

namespace PBG.Core
{
    public abstract class Scene
    {
        public static Dictionary<string, Scene> Scenes = [];
        public static Scene CurrentScene { get; private set; } = null!;
        public static Scene? CurrentlyLoadingScene = null;
        private static Scene? _sceneToBeLoaded = null;

        public bool AddedScripts = false;

        public string Name;

        public bool Started = false;
        public bool Restart = false;
        public bool ShouldResize = false;

        public RootNode RootNode;

        public Camera DefaultCamera { get; private set; }
        public Camera ActiveCamera { get; private set; }


        private ScriptCall[] OnStart = [];
        private ScriptCall[] OnAwake = [];
        private ScriptCall[] OnResize = [];
        private ScriptCall[] OnFixedUpdate = [];
        private ScriptCall[] OnUpdate = [];
        private ScriptCall[] OnLateUpdate = [];
        private ScriptCall[] OnRender = [];
        private ScriptCall[] OnExit = [];
        private ScriptCall[] OnDispose = [];

        public Scene(string name)
        {
            CurrentScene = this;
            Name = name;
            RootNode = new RootNode(this);

            Scenes.Add(Name, this);
            
            DefaultCamera = new Camera();

            var cameraNode = RootNode.AddNode("Camera");
            cameraNode.AddComponent(DefaultCamera);

            CurrentScene = null!;
        }

        public static void LoadScene(string name)
        {
            if (_sceneToBeLoaded == null && Scenes.TryGetValue(name, out Scene? scene) && CurrentScene != scene)
                _sceneToBeLoaded = scene;
        }

        public static void LoadSceneFinal()
        {
            if (_sceneToBeLoaded != null)
            {
                UIController.ClearFrameBuffer();
                Console.WriteLine("Loading scene");

                CurrentScene?.Exit();
                CurrentScene = _sceneToBeLoaded;
                _sceneToBeLoaded.InitComponents();

                ScriptInfo info = new();
                _sceneToBeLoaded.RootNode.InitAwake(info);
                _sceneToBeLoaded.OnAwake = [..info.OnAwake];
                info.Clear();

                if (!_sceneToBeLoaded.Started)
                {
                    _sceneToBeLoaded.Start();
                    _sceneToBeLoaded.Started = true;
                }
                _sceneToBeLoaded.Awake();
                _sceneToBeLoaded.Resize();

                _sceneToBeLoaded = null;
            }
        }

        public virtual void Preload() {}
        public abstract void Load();

        public void SetCameraAsActive(Camera camera) => ActiveCamera = camera;

        public TransformNode[] AddNode(params string[] names)
        {
            return RootNode.AddNode(names);
        }

        public TransformNode GetNode(string path) => RootNode.GetNode(path);
        public bool GetNode(string path, [NotNullWhen(true)] out TransformNode? node) => RootNode.GetNode(path, out node);
        
        public T QueryComponent<T>() where T : ScriptingNode => RootNode.QueryComponent<T>();
        public bool QueryComponent<T>([NotNullWhen(true)] out T? component) where T : ScriptingNode => RootNode.QueryComponent(out component);

        public TransformNode NewInternalNode(string name)
        {
            return AddNode(name)[0];
        }

        public void InitComponents()
        {
            UIController.InitControllers(this);
        }

        public void Calls(ScriptCall[] calls)
        {
            for (int i = 0; i < calls.Length; i++)
                calls[i].Invoke();
        }

        public void Start()
        {
            InitComponents();
            Calls(OnStart);
            OnStart = [];
        }

        public void Awake()
        {
            Calls(OnAwake);
            OnAwake = [];
        }

        public void Resize()
        {
            Calls(OnResize);
            ShouldResize = false;
        }
        public void FixedUpdate() => Calls(OnFixedUpdate);
        public void Update()
        {
            if (AddedScripts)
            {
                InitScripts();

                Start();
                Awake();

                AddedScripts = false;
            }

            UIController.HandleInputs(this);
            
            Calls(OnUpdate);
        }
        public void LateUpdate()
        {
            Calls(OnLateUpdate);
        }
        public void Render()
        {
            Calls(OnRender);
            GFX.Viewport();
        }

        public void Exit() => Calls(OnExit);
        public void Dispose() => Calls(OnDispose);

        public void InitScripts()
        {
            ScriptInfo info = new();
            RootNode.InitPendingComponents(info);

            OnStart       = [..info.OnStart];
            OnAwake       = [..info.OnAwake];
            OnResize      = [..info.OnResize];
            OnFixedUpdate = [..info.OnFixedUpdate];
            OnUpdate      = [..info.OnUpdate];
            OnLateUpdate  = [..info.OnLateUpdate];
            OnRender      = [..info.OnRender];
            OnExit        = [..info.OnExit];
            OnDispose     = [..info.OnDispose];

            info.Clear();
        }

        public static void ResizeAll()
        {
            foreach (var (_, scene) in Scenes)
            {
                scene.ShouldResize = true;
            }
        }

        public static void DisposeAll()
        {
            foreach (var (_, scene) in Scenes)
            {
                scene.Dispose();
            }
        }
        
        public static Type[] GetSubclasses()
        {
            var baseType = typeof(Scene);
            var subclasses = Assembly.GetAssembly(baseType) ?? throw new Exception("Scene does not have subclasses for some reason");
            return [.. subclasses.GetTypes().Where(t => t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t))]; ;
        }
    }
}