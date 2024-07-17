namespace Glib;

public abstract class BgfxResource : IDisposable
{
    private static List<BgfxResource> _allResources = [];
    private static Queue<BgfxResource> _disposedResources = [];
    private static Mutex _mutex = new();

    private bool _disposed = false;

    protected BgfxResource()
    {
        _allResources.Add(this);
    }

    ~BgfxResource() => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal static void Housekeeping()
    {
        _mutex.WaitOne();
        foreach (var resource in _disposedResources)
        {
            _allResources.Remove(resource);
        }
        _mutex.ReleaseMutex();
    }

    internal static void DisposeRemaining()
    {
        Housekeeping();

        foreach (var resource in _allResources)
            resource.Dispose();
    }

    /// <summary>
    /// Called when the class instance is being disposed/garbage-collected.
    /// <param name="disposing">True if the Dispose function was manually called, false if the object is being finalized.</param>
    /// </summary>
    protected abstract void FreeResources(bool disposing);

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        FreeResources(disposing);

        _mutex.WaitOne();
        _disposedResources.Enqueue(this);
        _mutex.ReleaseMutex();

        _disposed = false;
    }
}