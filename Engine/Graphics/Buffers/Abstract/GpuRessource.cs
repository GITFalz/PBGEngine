using System.Collections.Concurrent;

namespace PBG.Graphics;

[InternalSystemInit(InitPriority.Texture)]
public abstract class GpuRessource : BufferBase
{
    private static bool _queue = true;
    protected static Queue<GpuRessource> _creationQueue = [];

    public GpuRessource()
    {
        if (_queue) 
            _creationQueue.Enqueue(this);
        else
            Create();
    }

    public abstract void Create();

    private static void Init()
    {
        _queue = false;
        while (_creationQueue.Count > 0)
        {
            var ressource = _creationQueue.Dequeue();
            ressource.Create();
        }
    }
}