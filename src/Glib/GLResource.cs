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
    /// Called when Dispose is explicitly called.
    /// </summary>
    protected abstract void ExplicitDispose();

    /// <summary>
    /// The handle to the GL object.
    /// </summary>
    protected abstract uint Handle { get; }

    /// <summary>
    /// The GL function used to free the GL object.
    /// </summary>
    protected abstract Action<uint> FreeMethod { get; }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                ExplicitDispose();
                FreeMethod(Handle);
            }
            else
            {
                freeQueueMut.WaitOne();
                freeQueue.Enqueue(new QueueEntry()
                {
                    Method = FreeMethod,
                    Handle = Handle
                });
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