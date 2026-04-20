using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace PBG.Graphics;

[InternalSystemInit(InitPriority.Buffer)]
public unsafe abstract class BufferBase : IDisposable
{
    protected static ShaderCompiler _shaderCompiler = null!;
    protected static ShaderBuffer _shaderBuffer = null!;

    protected static readonly byte* _mainPtr = (byte*)"main".ToPtr();

    public static List<BufferBase> Buffers = [];
    public static bool BufferChanged { get; private set; } = false;

    private static bool _external = true;
    public static HashSet<BufferBase> ExternalBuffers = [];
    private static bool _externalBuffersChanged = false;

    private static HashSet<IResizeable> _highPriorityResizeList = [];
    private static HashSet<IResizeable> _lowPriorityResizeList = [];
    private static HashSet<BufferBase> _highPriorityDisposeBuffer = [];
    private static HashSet<BufferBase> _lowPriorityDisposeBuffer = [];

    private static HashSet<BufferBase> _toBeDisposed = [];

    public Action<BufferBase>? OnDispose = null;

    #if DEBUG
    private static Dictionary<ulong, (string TypeName, StackTrace Trace)> _handleTraces = [];
    #endif

    public BufferBase()
    {
        Buffers.Add(this);
        BufferChanged = true;

        if (_external)
        {
            ExternalBuffers.Add(this);
            _externalBuffersChanged = true;
        }

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

    public static bool IfExternalBuffersChanged()
    {
        if (!_externalBuffersChanged)
            return false;
        
        _externalBuffersChanged = false;
        return true;
    }

    internal static void SetNextAsInternal() => _external = false;
    internal static void SetNextAsExternal() => _external = true;
    
    #if DEBUG
    public static void SetDebug<T>(T[] t) where T : struct
    {
        for (int i = 0; i < t.Length; i++)
            SetDebug(t[i]);
    }

    public static void SetDebug<T>(T t) where T : struct
    {
        var field = typeof(T).GetField("Handle", BindingFlags.Public | BindingFlags.Instance);
        var prop  = typeof(T).GetProperty("Handle", BindingFlags.Public | BindingFlags.Instance);

        ulong handle;
        if (field != null)
            handle = Convert.ToUInt64(field.GetValue(t));
        else if (prop != null)
            handle = Convert.ToUInt64(prop.GetValue(t));
        else
            return; // no Handle, skip

        _handleTraces[handle] = (typeof(T).Name, new StackTrace(skipFrames: 1, fNeedFileInfo: true));
    }

    public static void RemoveDebug<T>(uint count, T* t) where T : unmanaged
    {
        for (int i = 0; i < count; i++)
            RemoveDebug(t[i]);
    }

    public static void RemoveDebug<T>(T[] t) where T : struct
    {
        for (int i = 0; i < t.Length; i++)
            RemoveDebug(t[i]);
    }

    public static void RemoveDebug<T>(T t) where T : struct
    {
        var field = typeof(T).GetField("Handle", BindingFlags.Public | BindingFlags.Instance);
        var prop  = typeof(T).GetProperty("Handle", BindingFlags.Public | BindingFlags.Instance);

        ulong handle;
        if (field != null)
            handle = Convert.ToUInt64(field.GetValue(t));
        else if (prop != null)
            handle = Convert.ToUInt64(prop.GetValue(t));
        else
            return;

        _handleTraces.Remove(handle);
    }

    public static bool TryGetTrace(ulong handle, [NotNullWhen(true)] out string? info)
    {
        if (_handleTraces.TryGetValue(handle, out var entry))
        {
            info = $"[{entry.TypeName}] handle=0x{handle:X}\n{entry.Trace}";
            return true;
        }
        info = null;
        return false;
    }
    #endif

    public static void Init()
    {
        _shaderCompiler = new();
        _shaderBuffer = new();
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
        RemoveResize();
        RemoveBuffer();
        RemoveExternal();
        return RemoveDispose();
    }

    protected void RemoveResize()
    {
        if (this is IResizeable resizeable)
        {
            if (this is Descriptor)
                _lowPriorityResizeList.Remove(resizeable);
            else
                _highPriorityResizeList.Remove(resizeable);
        }  
    }

    protected void RemoveBuffer()
    {
        Buffers.Remove(this);
        BufferChanged = true;
    }

    protected void RemoveExternal()
    {
        if (ExternalBuffers.Remove(this))
            _externalBuffersChanged = true;
    }

    protected bool RemoveDispose()
    {
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

    public static void DisposeExternal()
    {
        foreach (var buffer in ExternalBuffers)
        {
            buffer.RemoveResize();
            buffer.RemoveBuffer();
            if (buffer.RemoveDispose())
                _toBeDisposed.Add(buffer);   
        }

        ExternalBuffers = [];
        _externalBuffersChanged = true;
    }

    public static void DisposeAll()
    {
        ((nint)_mainPtr).Free();

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

        _shaderBuffer.Dispose();
        _shaderCompiler.Dispose();

        _highPriorityResizeList = [];
        _lowPriorityResizeList = [];
        _highPriorityDisposeBuffer = [];
        _lowPriorityDisposeBuffer = [];
    }
}