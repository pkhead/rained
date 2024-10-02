namespace RainEd.EditorGui;

abstract class FileBrowserPreview : IDisposable
{
    protected string path;
    public string Path => path;
    public abstract bool IsReady { get; }
    
    public FileBrowserPreview(string path)
    {
        this.path = path;
    }

    /// <summary>
    /// Render the preview.
    /// </summary>
    public abstract void Render();
    public abstract void Dispose();
}