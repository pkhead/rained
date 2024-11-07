using ImGuiNET;
namespace Rained.EditorGui;

/// <summary>
/// Class to handle the ImGui drawing of the Rained logo, used ini
/// both the about window and the home screen.
/// </summary>
static class RainedLogo
{
    private static RlManaged.Texture2D rainedLogo0 =
        RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath,"assets","rained-logo-colorless.png"));
    private static RlManaged.Texture2D rainedLogo1 =
        RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath,"assets","rained-logo-color.png"));
    
    public static int Width => rainedLogo0.Width;
    public static int Height => rainedLogo0.Height;

    public static void Draw()
    {
        // draw rained logo, with the outline colored according to the theme
        var initCursor = ImGui.GetCursorPos();
        var themeColor = ImGui.GetStyle().Colors[(int) ImGuiCol.Button];
        ImGuiExt.Image(rainedLogo0);
        ImGui.SetCursorPos(initCursor);
        ImGuiExt.Image(rainedLogo1.GlibTexture!, new Glib.Color(themeColor.X, themeColor.Y, themeColor.Z, themeColor.W));
    }
}