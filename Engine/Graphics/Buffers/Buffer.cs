namespace PBG.Graphics;

public abstract class BufferBase : IDisposable
{
    private static HashSet<IResizeable> _highPriorityResizeList = [];
    private static HashSet<IResizeable> _lowPriorityResizeList = [];
    private static HashSet<BufferBase> _highPriorityDisposeBuffer = [];
    private static HashSet<BufferBase> _lowPriorityDisposeBuffer = [];

    private static HashSet<BufferBase> _toBeDisposed = [];

    public Action<BufferBase>? OnDispose = null;

    public BufferBase()
    {
        if (this is Shader || this is ComputeShader)
            _highPriorityDisposeBuffer.Add(this);
        else
            _lowPriorityDisposeBuffer.Add(this);

        if (this is IResizeable resizeable)
        {
            if (this is Descriptor)
                _lowPriorityResizeList.Add(resizeable);
            else
                _highPriorityResizeList.Add(resizeable);
        }
    }

    protected abstract void Destroy();

    public void Dispose()
    {
        if (RemoveFromList())
            _toBeDisposed.Add(this);
    }

    public static void DisposeCached()
    {
        foreach (var buffer in _toBeDisposed)
        {
            buffer.OnDispose?.Invoke(buffer);
            buffer.OnDispose = null;
            buffer.Destroy();
        }
        _toBeDisposed = [];
    }

    protected bool RemoveFromList()
    {
        if (this is IResizeable resizeable)
        {
            if (this is Descriptor)
                _lowPriorityResizeList.Remove(resizeable);
            else
                _highPriorityResizeList.Remove(resizeable);
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
        {
            buffer.OnDispose?.Invoke(buffer);
            buffer.OnDispose = null;
            buffer.Destroy();
        }

        foreach (var buffer in _lowPriorityDisposeBuffer)
        {
            buffer.OnDispose?.Invoke(buffer);
            buffer.OnDispose = null;
            buffer.Destroy();
        }

        _highPriorityResizeList = [];
        _lowPriorityResizeList = [];
        _highPriorityDisposeBuffer = [];
        _lowPriorityDisposeBuffer = [];
    }
}