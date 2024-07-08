using ImGuiNET;
namespace RainEd;

static class PaletteWindow
{
    static public bool IsWindowOpen = false;

    static public void ShowWindow()
    {
        if (!IsWindowOpen) return;

        var prefs = RainEd.Instance.Preferences;

        ImGuiExt.CenterNextWindow(ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Palette Preview##Palette", ref IsWindowOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.PushItemWidth(ImGui.GetTextLineHeight() * 8.0f);

            var usePalette = prefs.UsePalette;
            var renderer = RainEd.Instance.LevelView.Renderer;

            if (ImGui.Checkbox("Enabled", ref usePalette))
            {
                prefs.UsePalette = usePalette;

                if (usePalette)
                    renderer.Palette = prefs.PaletteIndex;
                else
                    renderer.Palette = -1;
            }

            int paletteIndex = prefs.PaletteIndex;
            if (ImGui.InputInt("Palette", ref paletteIndex))
            {
                paletteIndex = Math.Clamp(paletteIndex, 0, renderer.Palettes.Length - 1);
                prefs.PaletteIndex = paletteIndex;

                if (prefs.UsePalette)
                    renderer.Palette = paletteIndex;
                else
                    renderer.Palette = -1;
            }

            int fadePalette = prefs.PaletteFadeIndex;
            if (ImGui.InputInt("Fade Palette", ref fadePalette))
            {
                fadePalette = Math.Clamp(fadePalette, 0, renderer.Palettes.Length - 1);
                prefs.PaletteFadeIndex = fadePalette;
                renderer.FadePalette = fadePalette;
            }

            float fadeAmt = prefs.PaletteFade;
            if (ImGui.SliderFloat("Fade Amount", ref fadeAmt, 0f, 1f))
            {
                prefs.PaletteFade = fadeAmt;
                renderer.PaletteMix = fadeAmt;
            }

            ImGui.TextDisabled("Note: These settings are not\nsaved in the project.");

            ImGui.PopItemWidth();
        }
        ImGui.End();
    }
}