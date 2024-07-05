using ImGuiNET;
namespace RainEd;

/// <summary>
/// Provides methods for ImGui font handling
/// </summary>
static class Fonts
{
    private static string[] availableFontPaths = [];
    private static ImFontPtr[] loadedFonts = [];

    public static ReadOnlySpan<string> AvailableFonts => availableFontPaths;

    private static readonly string FontDirectory = Path.Combine(Boot.AppDataPath, "config","fonts");

    public static void UpdateAvailableFonts()
    {
        List<string> fontFiles = [];
        
        foreach (var file in Directory.EnumerateFiles(FontDirectory))
        {
            var ext = Path.GetExtension(file);
            if (ext != ".ttf") continue;
            fontFiles.Add(Path.GetFileNameWithoutExtension(file));
        }
        
        availableFontPaths = [..fontFiles];
    }

    public static void ReloadFonts()
    {
        var oldFont = GetCurrentFont();

        List<ImFontPtr> loadedFontList = [];

        var io = ImGui.GetIO();

        io.Fonts.Clear();

        foreach (var file in availableFontPaths)
        {
            var font = io.Fonts.AddFontFromFileTTF(Path.Combine(FontDirectory, file + ".ttf"), 13f * Boot.WindowScale);
            loadedFontList.Add(font);
        }

        for (int i = 0; i < io.Fonts.ConfigData.Size; i++)
        {
            io.Fonts.ConfigData[i].OversampleH = 2;
            io.Fonts.ConfigData[i].OversampleV = 2;
            io.Fonts.ConfigData[i].PixelSnapH = true;
            io.Fonts.ConfigData[i].RasterizerDensity = Boot.WindowScale;
        }

        loadedFonts = [..loadedFontList];

        if (!string.IsNullOrEmpty(oldFont))
        {
            SetFont(oldFont);
        }
    }

    public static bool SetFont(string fontName)
    {
        for (int i = 0; i < availableFontPaths.Length; i++)
        {
            if (availableFontPaths[i] == fontName)
            {
                unsafe
                {
                    var io = ImGui.GetIO();
                    io.NativePtr->FontDefault = loadedFonts[i];
                }
                
                return true;
            }
        }

        return false;
    }

    public static string? GetCurrentFont()
    {
        var io = ImGui.GetIO();

        unsafe
        {
            for (int i = 0; i < loadedFonts.Length; i++)
            {
                if (loadedFonts[i].NativePtr == io.FontDefault.NativePtr)
                {
                    return availableFontPaths[i];
                }
            }
        }

        return string.Empty;
    }
}