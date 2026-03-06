namespace PBG.Graphics;

public abstract class BufferBase : IDisposable
{
    private static HashSet<BufferBase> _highPriorityResizeList = [];
    private static HashSet<BufferBase> _lowPriorityResizeList = [];
    private static HashSet<BufferBase> _highPriorityDisposeBuffer = [];
    private static HashSet<BufferBase> _lowPriorityDisposeBuffer = [];

    public BufferBase()
    {
        if (this is Shader || this is ComputeShader)
            _highPriorityDisposeBuffer.Add(this);
        else
            _lowPriorityDisposeBuffer.Add(this);

        var method = GetType().GetMethod(nameof(Resize));
        if(method?.DeclaringType != typeof(BufferBase))
        {
            if (this is Descriptor)
                _lowPriorityResizeList.Add(this);
            else
                _highPriorityResizeList.Add(this);
        }
    }

    public virtual void Resize(uint width, uint height) {}
    protected abstract void Destroy();

    public void Dispose()
    {
        if (RemoveFromList())
            Destroy();
    }

    protected bool RemoveFromList()
    {
        if (this is Descriptor)
        {
            _lowPriorityResizeList.Remove(this);
        }
        else
        {
            _highPriorityResizeList.Remove(this);
        }      

        if (this is Shader || this is ComputeShader)
        {
            return _highPriorityDisposeBuffer.Remove(this);
        }
        else
        {
            return _lowPriorityDisposeBuffer.Remove(this);
        }   
    }

    public static void ResizeAll(uint width, uint height)
    {
        foreach (var buffer in _highPriorityResizeList)
            buffer.Resize(width, height);

        foreach (var buffer in _lowPriorityResizeList)
            buffer.Resize(width, height);
    }

    public static void DisposeAll()
    {
        foreach (var buffer in _highPriorityDisposeBuffer)
            buffer.Dispose();

        foreach (var buffer in _lowPriorityDisposeBuffer)
            buffer.Dispose();

        _highPriorityResizeList = [];
        _lowPriorityResizeList = [];
        _highPriorityDisposeBuffer = [];
        _lowPriorityDisposeBuffer = [];
    }
}