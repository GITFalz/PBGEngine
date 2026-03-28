using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using PBG.Editor;
using PBG.Graphics;
using PBG.Rendering;
using PBG.UI;

namespace PBG.Core
{
    public class Scene
    {
        public readonly Guid ID = new();
        public static Dictionary<string, Scene> Scenes = [];
        public static Scene? CurrentScene { get; private set; } = null;
        public static Scene? CurrentlyLoadingScene = null;
        private static Scene? _sceneToBeLoaded = null;

        private HashSet<TransformNode> _pendingHash = [];
        private List<TransformNode> _pendingList = [];
        private bool _updateStart = false;

        public string Name;

        public bool Started = false;
        public bool Restart = false;
        public bool ShouldResize = false;

        public RootNode RootNode;

        private Camera _gameCamera;
        public Camera Camera;

        private TransformNode _cameraNode;

        public Scene(string name)
        {
            CurrentScene = this;
            Name = name;
            RootNode = new RootNode(this);

            Scenes.Add(Name, this);
            
            _gameCamera = new Camera();

            _cameraNode = RootNode.AddNode("Camera");
            _cameraNode.AddComponent(_gameCamera);

            Camera = _gameCamera;

            CurrentScene = null;
        }

        internal Scene()
        {
            CurrentScene = this;

            Name = "";
            RootNode = new RootNode(this);
            _gameCamera = new Camera();

            _cameraNode = RootNode.AddNode("Camera");
            _cameraNode.AddComponent(_gameCamera);

            Camera = _gameCamera;

            CurrentScene = null!;
        }

        internal void SetGameCamera() => Camera = _gameCamera;
        internal void SetCamera(Camera camera) => Camera = camera;

        public static bool LoadScene(string name)
        {
            if (_sceneToBeLoaded == null && Scenes.TryGetValue(name, out Scene? scene) && CurrentScene != scene)
            {
                _sceneToBeLoaded = scene;
                return true;
            }
            return false;
        }

        public static void UnloadScene()
        {
            CurrentScene?.Exit();
            CurrentScene?.Dispose();
            CurrentScene = null;
        }

        public static void LoadSceneFinal()
        {
            if (_sceneToBeLoaded != null)
            {
                UIController.ClearFrameBuffer();
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
                _sceneToBeLoaded.Resize();

                _sceneToBeLoaded = null;
            }
        }

        public TransformNode[] NewNodes(params string[] names)
        {
            return RootNode.AddNode(names);
        }

        public TransformNode GetNode(string path) => RootNode.GetNode(path);
        public bool GetNode(string path, [NotNullWhen(true)] out TransformNode? node) => RootNode.GetNode(path, out node);
        
        public T QueryComponent<T>() where T : ScriptingNode => RootNode.QueryComponent<T>();
        public bool QueryComponent<T>([NotNullWhen(true)] out T? component) where T : ScriptingNode => RootNode.QueryComponent(out component);

        public TransformNode NewNode(string name)
        {
            return NewNodes(name)[0];
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
        public void FixedUpdate()
        {
            RootNode.FixedUpdate();
        }

        public void UpdatePending()
        {
            if (_pendingList.Count > 0)
            {
                for (int i = 0; i < _pendingList.Count; i++)
                {
                    var pending = _pendingList[i];
                    pending.InitPendingComponents();
                }

                _pendingList = [];
                _pendingHash = [];

                _updateStart = true;
            }
        }

        public void Update()
        {
            if (_updateStart)
            {
                Start();
                Awake();

                _updateStart = false;
            }
            
            RootNode.Update();
        }
        public void LateUpdate()
        {
            RootNode.LateUpdate();
        }
        public void Render()
        {
            RootNode.Render();
            GFX.Viewport();
        }

        public void SmallAwake()
        {
            _cameraNode.Awake();
        }

        public void SmallResize()
        {
            _cameraNode.Resize();
        }

        public void SmallUpdate()
        {
            _cameraNode.Update();
        }

        public void SetAsPending(TransformNode node)
        {
            if (_pendingHash.Add(node))
                _pendingList.Add(node);
        }

        public bool IsPending(TransformNode node) => _pendingHash.Contains(node);

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