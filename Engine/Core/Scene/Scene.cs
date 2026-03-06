using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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

        public List<TransformNode> PendingList = [];

        public string Name;

        public bool Started = false;
        public bool Restart = false;
        public bool ShouldResize = false;

        public RootNode RootNode;

        public Camera DefaultCamera { get; private set; }
        public Camera ActiveCamera { get; private set; }

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
                Console.WriteLine("Loading scene");
                CurrentScene?.Exit();
                CurrentScene = _sceneToBeLoaded;
                _sceneToBeLoaded.InitComponents();
                _sceneToBeLoaded.RootNode.InitAwake();
                if (!_sceneToBeLoaded.Started)
                {
                    _sceneToBeLoaded.Start();
                    _sceneToBeLoaded.Started = true;
                }
                _sceneToBeLoaded.Awake();
                if (_sceneToBeLoaded.ShouldResize)
                {
                    _sceneToBeLoaded.Resize();
                    _sceneToBeLoaded.ShouldResize = false;
                }

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

        public void Start()
        {
            InitComponents();
            RootNode.Start();
        }

        public void Awake()
        {
            RootNode.Awake();
        }

        public void Resize()
        {
            RootNode.Resize();
            ShouldResize = false;
        }
        public void FixedUpdate() => RootNode.FixedUpdate();
        public void Update()
        {
            if (PendingList.Count > 0)
            {
                for (int i = 0; i < PendingList.Count; i++)
                {
                    var pending = PendingList[i];
                    pending.InitPendingComponents();
                }

                Start();
                Awake();

                PendingList = [];
            }

            UIController.HandleInputs(this);
            
            RootNode.Update();
        }
        public void LateUpdate()
        {
            RootNode.LateUpdate();
        }
        public void Render()
        {
            RootNode.Render();
        }

        public void Exit() => RootNode.Exit();
        public void Dispose() => RootNode.Dispose();

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