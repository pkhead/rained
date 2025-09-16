using Rained.EditorGui;

namespace Rained.LevelData.FileFormats;

static class LevelFileFormats
{
    public static ILevelFileFormat AutoDetect(string path)
    {
        var ext = Path.GetExtension(path);

        if (ext.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            return new VanillaFileFormat();

        if (ext.Equals(".rwlz", StringComparison.OrdinalIgnoreCase))
            return new RWLZFileFormat();

        return new VanillaFileFormat();
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