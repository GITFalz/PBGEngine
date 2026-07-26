public class ScriptInfo
{    public List<ScriptCall> OnStart = [];
    public List<ScriptCall> OnAwake = [];
    public List<ScriptCall> OnResize = [];
    public List<ScriptCall> OnFixedUpdate = [];
    public List<ScriptCall> OnUpdate = [];
    public List<ScriptCall> OnLateUpdate = [];
    public List<ScriptCall> OnCompute = [];
    public List<ScriptCall> OnRender = [];
    public List<ScriptCall> OnExit = [];
    public List<ScriptCall> OnDispose = [];

    public void Clear()
    {
        OnStart.Clear();
        OnAwake.Clear();
        OnResize.Clear();
        OnFixedUpdate.Clear();
        OnUpdate.Clear();
        OnLateUpdate.Clear();
        OnCompute.Clear();
        OnRender.Clear();
        OnExit.Clear();
        OnDispose.Clear();
    }
}