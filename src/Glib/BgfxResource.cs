namespace Glib;

public abstract class BgfxResource : IDisposable
{
    private bool _disposed = false;

    ~BgfxResource() => Dispose(false);

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

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            FreeResources(disposing);
            _disposed = false;
        }
    }
}