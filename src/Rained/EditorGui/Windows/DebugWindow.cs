using System.Globalization;
using System.Numerics;
using Hexa.NET.ImGui;
using Rained.LuaScripting;
namespace Rained.EditorGui;

static class DebugWindow
{
    public const string WindowName = "Rained Debug";
    public static bool IsWindowOpen = false;
    private static bool _showDemoWindow = false;

    public static void ShowWindow()
    {
        if (_showDemoWindow)
        {
            ImGui.ShowDemoWindow();
        }
        
        if (!IsWindowOpen) return;

        if (ImGui.Begin(WindowName, ref IsWindowOpen))
        {
            var io = ImGui.GetIO();

            var luaMemKiB = LuaInterface.LuaState.GarbageCollector(KeraLua.LuaGC.Count, 0);
            var csMemMiB = (double)GC.GetTotalMemory(false) / (1024 * 1024);

            ImGui.TextUnformatted(string.Format("Application average {0:F3} ms/frame ({1:F1} FPS)", 1000.0f / io.Framerate, io.Framerate));
            ImGui.TextUnformatted("Loaded tile graphics: " + RainEd.Instance.AssetGraphics.TileTextureCount);
            ImGui.TextUnformatted("Loaded prop graphics: " + RainEd.Instance.AssetGraphics.PropTextureCount);
            ImGui.TextUnformatted($"Total texture memory: {(float)Glib.RenderContext.Instance!.TotalTextureMemory / 1000000} mb");
            ImGui.TextUnformatted($"Lua memory: {luaMemKiB} KiB");
            ImGui.TextUnformatted(string.Format("C# memory: {0:F2} MiB", csMemMiB));

            if (RainEd.Instance.CurrentTab?.Level is not null)
            {
                ImGui.TextUnformatted("Undo stack cnt: " + RainEd.Instance.ChangeHistory.UndoStackCount);
                ImGui.TextUnformatted("Redo stack cnt: " + RainEd.Instance.ChangeHistory.RedoStackCount);
            }
            
            ImGui.Checkbox("Show demo window", ref _showDemoWindow);

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