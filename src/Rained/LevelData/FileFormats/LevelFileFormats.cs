using Rained.EditorGui;

namespace Rained.LevelData.FileFormats;

enum LevelFileFormat
{
    Vanilla,
    RWLZ
}

static class LevelFileFormats
{
    static readonly VanillaFileFormat _vanilla = new();
    static readonly RWLZFileFormat _rwlz = new();

    public static ILevelFileFormat AutoDetect(string path)
    {
        var ext = Path.GetExtension(path);

        if (ext.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            return _vanilla;

        if (ext.Equals(".rwlz", StringComparison.OrdinalIgnoreCase))
            return _rwlz;

        return _vanilla;
    }

    public static void SetUpFileBrowser(FileBrowser fileBrowser, FileBrowser.OpenMode openMode)
    {
        static bool levelCheck(string path, bool isRw)
            => isRw;

        static bool vanillaLevelCheck(string path, bool isRw)
            => isRw && Path.GetExtension(path).Equals(".txt", StringComparison.OrdinalIgnoreCase);

        switch (openMode)
        {
            case FileBrowser.OpenMode.Write:
                fileBrowser.AddFilterWithCallback("Plain level file", vanillaLevelCheck, ".txt");
                fileBrowser.AddFilterWithCallback("Level ZIP file", null, ".rwlz");

                switch (RainEd.Instance.Preferences.PreferredFileFormat)
                {
                    case LevelFileFormat.Vanilla:
                        fileBrowser.SetDefaultFilter("Plain level file");
                        break;

                    case LevelFileFormat.RWLZ:
                        fileBrowser.SetDefaultFilter("Level ZIP file");
                        break;
                }
                
                break;

            case FileBrowser.OpenMode.Read:
            case FileBrowser.OpenMode.MultiRead:
            default:
                fileBrowser.AddFilterWithCallback("Level file", levelCheck, ".txt", ".rwlz");
                fileBrowser.AddFilterWithCallback("Plain level file", vanillaLevelCheck, ".txt");
                fileBrowser.AddFilterWithCallback("Level ZIP file", null, ".rwlz");
                break;
        }

        fileBrowser.PreviewCallback = (string path, bool isRw) =>
        {
            if (isRw) return new BrowserLevelPreview(path);
            return null;
        };
    }
}