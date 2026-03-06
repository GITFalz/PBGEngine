using PBG.Core;
using static PBG.UI.UIHelper;

public class StructureEngineManager : ScriptingNode
{
    public static StructureEngineManager Instance = null!;

    public StructureMeshRenderer MeshRenderer = null!;

    public Skybox Skybox = null!;

    public EditorType Editor = EditorType.Tree;

    public static bool CanRotate = false;

    public StructureEngineManager()
    {
        Instance = this; 
    }

    void Start()
    {
        MeshRenderer = new StructureMeshRenderer(this);

        Scene.DefaultCamera.SetCameraSpeed(10f);

        Skybox = Scene.GetNode("Root/World").GetComponent<Skybox>();
        Skybox.Color = GRAY_010.Xyz;
        MeshRenderer.Start();
    }

    void Awake()
    {
        MeshRenderer.Awake();
        Scene.DefaultCamera.SetCameraSpeed(50);
    }

    void Resize()
    {
        MeshRenderer.Resize();
    }

    void Update()
    {
        MeshRenderer.Update();
    } 

    void LateUpdate()
    {
        NodeManager.LateUpdate();
    }

    void Render()
    {
        if (Editor == EditorType.Tree)
        {
            MeshRenderer.Render();
        }
    }

    void Exit()
    {
        //NodeUI.Exit();
        MeshRenderer.Exit();
    }
}

public enum EditorType
{
    Tree,
    Structure,
    Noise
}