using ImGuiNET;
namespace RainEd;

static class RendererWindow
{
    public static bool IsWindowOpen = true;
    static Rendering.VoxelRenderer? renderer = null;

    public static void ShowWindow()
    {
        if (!IsWindowOpen) return;

        ImGuiExt.CenterNextWindow(ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Test Render", ref IsWindowOpen))
        {
            if (ImGui.Button("Render"))
            {
                renderer = new Rendering.VoxelRenderer(RainEd.Instance.Level);
                renderer.UpdateLevel();
            }

            if (renderer is not null)
            {
                var levelView = RainEd.Instance.LevelView;
                renderer.Render(levelView.ViewOffset.X, levelView.ViewOffset.Y, 1.0f / levelView.ViewZoom);
                ImGuiExt.ImageRenderTexture(renderer.Framebuffer);
            }
        } ImGui.End();
    }
}