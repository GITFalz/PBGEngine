public class ObjectEditor : BaseEditor
{
    public ObjectEditor(GeneralModelingEditor editor) : base(editor)
    {
        // Constructor logic here
    }
    
    public override void Awake()
    {
        throw new NotImplementedException();
    }

    public override void Exit()
    {
        throw new NotImplementedException();
    }

    public override void Render()
    {
        throw new NotImplementedException();
    }

    public override void EndRender()
    {
        throw new NotImplementedException();
    }

    public override void Resize()
    {
        throw new NotImplementedException();
    }

    public override void Start()
    {
        Started = true;
        /*
        Console.WriteLine("Start Modeling Editor");

        if (_started)
        {
            return;
        }

        Editor = editor;
        Mesh = editor.model.modelMesh;

        _started = true;
        */
    }

    public override void Update()
    {
        throw new NotImplementedException();
    }
}