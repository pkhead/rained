using System.Collections.Generic;
using System.Threading;
namespace Glib;

public abstract class Resource : IDisposable
{
    private static List<Resource> _allResources = [];
    private static Queue<Resource> _disposedResources = [];
    private static Mutex _mutex = new();

    private bool _disposed = false;

    protected Resource()
    {
        _allResources.Add(this);
    }

    ~Resource() => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal static void Idle()
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
        Idle();

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