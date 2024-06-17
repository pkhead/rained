using ImGuiNET;
using Raylib_cs;
using System.Numerics;
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

                ImGui.PushItemWidth(ImGui.GetTextLineHeight() * 10f);
                ImGui.SliderFloat("FOV", ref renderer.FieldOfView, 10f, 180f);
                ImGui.PopItemWidth();

                var levelView = RainEd.Instance.LevelView;
                renderer.Render(levelView.ViewOffset.X, levelView.ViewOffset.Y, 1.0f / levelView.ViewZoom);

                var cursor = ImGui.GetCursorPos();
                ImGuiExt.ImageRenderTexture(renderer.Framebuffer);
                ImGui.SetCursorPos(cursor);
                ImGui.InvisibleButton("interactive_area", new Vector2(renderer.Framebuffer.Width, renderer.Framebuffer.Height));

                if (ImGui.IsItemActive())
                {
                    renderer.CameraRotation.Y += Raylib.GetMouseDelta().X / 500f;
                    renderer.CameraRotation.X -= Raylib.GetMouseDelta().Y / 500f;
                }

                var rotMatrix = renderer.CameraRotationMatrix;
                var xVector = new Vector3(rotMatrix.M11, rotMatrix.M12, rotMatrix.M13);
                var yVector = new Vector3(rotMatrix.M21, rotMatrix.M22, rotMatrix.M23);
                var zVector = new Vector3(rotMatrix.M31, rotMatrix.M32, rotMatrix.M33);

                var flySpeed = 300f;
                if (EditorWindow.IsKeyDown(ImGuiKey.W))
                {
                    renderer.CameraPosition += zVector * flySpeed * Raylib.GetFrameTime();
                }
                
                if (EditorWindow.IsKeyDown(ImGuiKey.S))
                {
                    renderer.CameraPosition -= zVector * flySpeed * Raylib.GetFrameTime();
                }

                if (EditorWindow.IsKeyDown(ImGuiKey.D))
                {
                    renderer.CameraPosition += xVector * flySpeed * Raylib.GetFrameTime();
                }
                
                if (EditorWindow.IsKeyDown(ImGuiKey.A))
                {
                    renderer.CameraPosition -= xVector * flySpeed * Raylib.GetFrameTime();
                }

                if (EditorWindow.IsKeyDown(ImGuiKey.E))
                {
                    renderer.CameraPosition -= yVector * flySpeed * Raylib.GetFrameTime();
                }
                
                if (EditorWindow.IsKeyDown(ImGuiKey.Q))
                {
                    renderer.CameraPosition += yVector * flySpeed * Raylib.GetFrameTime();
                }
            }
        } ImGui.End();
    }
}