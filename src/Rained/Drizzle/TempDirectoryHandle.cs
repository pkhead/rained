namespace Rained.Drizzle;

class TempDirectoryHandle(string? prefix = null) : IDisposable
{
    public string Path => Info.FullName;
    public readonly DirectoryInfo Info = Directory.CreateTempSubdirectory(prefix);

    private bool _disposed = false;

    ~TempDirectoryHandle()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;

        Info.Delete(true);
    }
}