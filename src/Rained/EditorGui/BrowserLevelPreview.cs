namespace RainEd;

class BrowserLevelPreview : FileBrowserPreview
{
    public override bool IsReady => false;

    public BrowserLevelPreview(string path) : base(path)
    {}

    public override void Render()
    {}

    public override void Dispose()
    {}
}