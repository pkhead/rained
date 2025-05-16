namespace Rained.Assets;
using System.Collections.Generic;
using Rained.EditorGui;
using Raylib_cs;

class GeometryIcons
{
    static bool _isLoaded = false;

    static RlManaged.Texture2D toolbarTexture = null!;
    static RlManaged.Texture2D renderTexture = null!;
    static string[] setList = null!;
    static string curSet = null!;

    public static string[] Sets => Sets;
    public static string CurrentSet
    {
        get => curSet;
        set
        {
            if (SelectSet(value))
                curSet = value;
        }
    }
    public static RlManaged.Texture2D ToolbarTexture => toolbarTexture;
    public static RlManaged.Texture2D RenderTexture => renderTexture;


    public static void Init()
    {
        if (_isLoaded) return;
        _isLoaded = true;

        setList = [.. ConfigDirectory.EnumerateFiles("geo-icons", "*.png").Select(x => Path.GetFileNameWithoutExtension(x))];
        Array.Sort(setList);

        curSet = "Rained";
        SelectSet(curSet);
    }

    static bool SelectSet(string name)
    {
        if (!setList.Contains(name))
        {
            Log.UserLogger.Error($"Unknown set {name}");
            EditorWindow.ShowNotification("Could not change icon set!");
            return false;
        }

        using var img = RlManaged.Image.Load(ConfigDirectory.GetFilePath(Path.Combine("geo-icons", name + ".png")));
        if (!Raylib.IsImageReady(img))
        {
            Log.UserLogger.Error($"Could not load geo-icons/{name}.png");
            EditorWindow.ShowNotification("Could not change icon set!");
            return false;
        }

        if (img.Width < 196 || img.Height < 192)
        {
            Log.UserLogger.Error($"Icon set {name} has invalid dimensions");
            EditorWindow.ShowNotification("Could not change icon set!");
            return false;
        }

        using var imgCopy = RlManaged.Image.Copy(img);

        // img becomes toolbar set
        Raylib.ImageCrop(ref img.Ref(), new Rectangle(0, 0, 96, 144));
        // imgCopy becomes render atlas
        Raylib.ImageCrop(ref imgCopy.Ref(), new Rectangle(96, 0, 100, 80));

        toolbarTexture?.Dispose();
        renderTexture?.Dispose();

        toolbarTexture = RlManaged.Texture2D.LoadFromImage(img);
        renderTexture = RlManaged.Texture2D.LoadFromImage(imgCopy);
        return true;
    }
}