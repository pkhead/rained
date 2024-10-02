using System.Globalization;
using System.Numerics;
using ImGuiNET;
namespace RainEd;

static class DebugWindow
{
    public const string WindowName = "Rained Debug";
    public static bool IsWindowOpen = false;
    private static bool _showDemoWindow = false;

    public static void ShowWindow()
    {
        if (!IsWindowOpen) return;

        if (ImGui.Begin(WindowName, ref IsWindowOpen))
        {
            var io = ImGui.GetIO();

            ImGui.TextUnformatted(string.Format("Application average {0:F3} ms/frame ({1:F1} FPS)", 1000.0f / io.Framerate, io.Framerate));
            ImGui.TextUnformatted($"Total texture memory: {(float)Glib.RenderContext.Instance!.TotalTextureMemory / 1000000} mb");
            ImGui.Checkbox("Show demo window", ref _showDemoWindow);

            if (_showDemoWindow)
            {
                ImGui.ShowDemoWindow();
            }

            if (ImGui.Button("GC"))
            {
                GC.Collect();
            }

            if (ImGui.CollapsingHeader("Tile atlases"))
            {
                int index = 1;
                foreach (var tex in RainEd.Instance.AssetGraphics.TilePreviewAtlases)
                {
                    if (ImGui.BeginChild("tex" + index, new Vector2(tex.Width, tex.Height) / 2f, ImGuiChildFlags.Border))
                    {
                        ImGui.TextUnformatted(index.ToString(CultureInfo.InvariantCulture));
                        ImGuiExt.ImageSize(tex, tex.Width / 2f, tex.Height / 2f);
                    }
                    ImGui.EndChild();
                }
            }
        }
        ImGui.End();
    }
}