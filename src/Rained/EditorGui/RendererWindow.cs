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
                renderer ??= new Rendering.VoxelRenderer();
                renderer.UpdateLevel(RainEd.Instance.Level);
            }

            if (renderer is not null)
            {
                ImGui.Checkbox("Wireframe", ref renderer.Wireframe);
                ImGui.SameLine();
                ImGui.SliderAngle("Light Angle", ref renderer.LightAngle);

                var levelView = RainEd.Instance.LevelView;
                renderer.Render(levelView.ViewOffset.X, levelView.ViewOffset.Y, 1.0f / levelView.ViewZoom);
                ImGuiExt.ImageRenderTexture(renderer.Framebuffer);
            }
        } ImGui.End();
    }
}