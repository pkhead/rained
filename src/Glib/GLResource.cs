namespace Glib;

public abstract class GLResource : IDisposable
{
    struct QueueEntry
    {
        public Action<uint> Method;
        public uint Handle;
    }

    // TODO: make this a member of Window
    private readonly static Mutex freeQueueMut = new();
    private readonly static Queue<QueueEntry> freeQueue = new();

    private bool _disposed = false;

    ~GLResource() => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Called when the class instance is being disposed/garbage-collected.
    /// <param name="disposing">True if the Dispose function was manually called, false if the object is being finalized.</param>
    /// </summary>
    protected abstract void FreeResources(bool disposing);

    /// <summary>
    /// <p>Queue a handle to be freed.</p>
    /// 
    /// In FreeResources, this must be called instead of the method directly, because
    /// object finalization is done on a separate thread, conflicting with the fact that
    /// GL functions must be called on the main thread.
    /// 
    /// Following this, the given method will be queued to be called at some point
    /// in the main thread.
    /// </summary>
    /// <param name="method">The method that will be called to free the handle.</param>
    /// <param name="handle">The handle to free.</param>
    protected static void QueueFreeHandle(Action<uint> method, uint handle)
    {
        freeQueue.Enqueue(new QueueEntry()
        {
            Method = method,
            Handle = handle
        });
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                FreeResources(true);
            }
            else
            {
                freeQueueMut.WaitOne();
                FreeResources(false);
                freeQueueMut.ReleaseMutex();
            }

            _disposed = false;
        }
    }

    internal static void UnloadGCQueue()
    {
        freeQueueMut.WaitOne();
        foreach (var entry in freeQueue)
        {
            entry.Method(entry.Handle);
        }
        freeQueue.Clear();
        freeQueueMut.ReleaseMutex();
    }
}