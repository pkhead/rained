using ImGuiNET;
namespace Rained;

/// <summary>
/// Provides methods for ImGui font handling
/// </summary>
static class Fonts
{
    private static string[] availableFontPaths = [];
    private static ImFontPtr[] loadedFonts = [];
    private static ImFontPtr[] loadedBigFonts = [];

    /// <summary>
    /// Embedded ProggyClean, 13px
    /// </summary>
    public static ImFontPtr DefaultFont;

    public static ReadOnlySpan<string> AvailableFonts => availableFontPaths;

    private static readonly string FontDirectory = Path.Combine(Boot.AppDataPath, "config","fonts");

    /// <summary>
    /// Set to true if the font should be reloaded before the start
    /// of the next frame.
    /// </summary>
    public static bool FontReloadQueued = false;

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

    public static void ReloadFonts(float fontSize)
    {
        var oldFont = GetCurrentFont();

        List<ImFontPtr> loadedFontList = [];
        List<ImFontPtr> loadedBigFontsList = [];

        var io = ImGui.GetIO();

        ImVector ranges;
        unsafe
        {
            var glyphRangeBuilder = ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder();
            var builderPtr = new ImFontGlyphRangesBuilderPtr(glyphRangeBuilder);

            builderPtr.AddRanges(io.Fonts.GetGlyphRangesCyrillic());
            builderPtr.AddRanges(io.Fonts.GetGlyphRangesChineseSimplifiedCommon());
            builderPtr.AddRanges(io.Fonts.GetGlyphRangesJapanese());

            builderPtr.BuildRanges(out ranges);
            builderPtr.Destroy();
        }

        io.Fonts.Clear();
        DefaultFont = io.Fonts.AddFontDefault();

        foreach (var file in availableFontPaths)
        {
            var fullFilePath = Path.Combine(FontDirectory, file + ".ttf");
            var font = io.Fonts.AddFontFromFileTTF(fullFilePath, fontSize * Boot.WindowScale, null, ranges.Data);
            loadedFontList.Add(font);

            var bigFont = io.Fonts.AddFontFromFileTTF(fullFilePath, 2f * fontSize * Boot.WindowScale, null, ranges.Data);
            loadedBigFontsList.Add(bigFont);
        }

        /*for (int i = 0; i < io.Fonts.ConfigData.Size; i++)
        {
            io.Fonts.ConfigData[i].OversampleH = 2;
            io.Fonts.ConfigData[i].OversampleV = 2;
            io.Fonts.ConfigData[i].PixelSnapH = true;
            io.Fonts.ConfigData[i].RasterizerDensity = Boot.WindowScale;
        }*/

        loadedFonts = [..loadedFontList];
        loadedBigFonts = [..loadedBigFontsList];

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

    public static ImFontPtr? GetCurrentBigFont()
    {
        var io = ImGui.GetIO();

        unsafe
        {
            for (int i = 0; i < loadedFonts.Length; i++)
            {
                if (loadedFonts[i].NativePtr == io.FontDefault.NativePtr)
                {
                    return loadedBigFonts[i];
                }
            }
        }

        return null;
    }

    public static ImFontPtr GetFontPtr(string name)
    {
        for (int i = 0; i < AvailableFonts.Length; i++)
        {
            if (AvailableFonts[i] == name) return loadedFonts[i];
        }

        return null;
    }
}