namespace Glib;

internal class ResourceList
{
    private readonly List<WeakReference<Resource>> _allResources = [];
    private readonly Queue<Resource> _disposeQueue = [];
    private readonly Mutex _mutex = new();

    public void RegisterResource(Resource res)
    {
        _allResources.Add(new WeakReference<Resource>(res));
    }

    public void Idle()
    {
        _mutex.WaitOne();
        foreach (var resource in _disposeQueue)
        {
            resource.Dispose();
        }
        _disposeQueue.Clear();
        _mutex.ReleaseMutex();
    }

    public void DisposeRemaining()
    {
        Idle();

        foreach (var resourceRef in _allResources)
            if (resourceRef.TryGetTarget(out var resource))
                resource.Dispose();
    }

    public void QueueDispose(Resource resource)
    {
        _mutex.WaitOne();
        _disposeQueue.Enqueue(resource);
        _mutex.ReleaseMutex();
    }
}

public abstract class Resource : IDisposable
{
    private bool _disposed = false;
    private RenderContext _rctx;

    protected Resource()
    {
        _rctx = RenderContext.Instance!;
        _rctx.resourceList.RegisterResource(this);
    }

    ~Resource() => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Called when the class instance is being disposed/garbage-collected.<br />
    /// Will always either run on the thread that called Dispose(), or the thread<br />
    /// that called RenderContext.End()
    /// </summary>
    /// <param name="rctx">The render context the resource was created with.</param>
    protected abstract void FreeResources(RenderContext rctx);

    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
            FreeResources(_rctx);
        else
            _rctx.resourceList.QueueDispose(this);
        
        _disposed = false;
    }
}