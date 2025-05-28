using ImGuiNET;
namespace Rained.EditorGui;

static class PaletteWindow
{
    static public bool IsWindowOpen = false;

    static public void ShowWindow()
    {
        if (!IsWindowOpen) return;

        var prefs = RainEd.Instance.Preferences;

        ImGuiExt.CenterNextWindow(ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Palettes", ref IsWindowOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.PushItemWidth(ImGui.GetTextLineHeight() * 8.0f);

            var usePalette = prefs.UsePalette;
            var renderer = RainEd.Instance.LevelView.Renderer;

            if (ImGui.Checkbox("Enabled", ref usePalette))
            {
                prefs.UsePalette = renderer.UsePalette = usePalette;
            }

            int paletteIndex = renderer.Palette.Index;
            if (ImGui.InputInt("Palette", ref paletteIndex))
            {
                prefs.PaletteIndex = renderer.Palette.Index = paletteIndex;
            }

            int fadePalette = renderer.Palette.FadeIndex;
            if (ImGui.InputInt("Fade Palette", ref fadePalette))
            {
                prefs.PaletteFadeIndex = renderer.Palette.FadeIndex = fadePalette;
            }

            float fadeAmt = renderer.Palette.Mix;
            if (ImGui.SliderFloat("Fade Amount", ref fadeAmt, 0f, 1f))
            {
                prefs.PaletteFade = fadeAmt;
                renderer.Palette.Mix = fadeAmt;
            }

            ImGui.TextDisabled("Note: These settings are not\nsaved in the project.");

            ImGui.PopItemWidth();
        }
        ImGui.End();
    }
}