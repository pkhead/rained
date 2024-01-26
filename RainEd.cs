using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

namespace RainEd;

class RainEd
{
    private readonly Level level;
    private readonly GeometryEditor geometryEditor;

    public RainEd() {
        level = new();
        geometryEditor = new(level, 1, 1);
    }

    public void Draw()
    {
        Raylib.ClearBackground(Color.DarkGray);

        rlImGui.Begin();

        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }

        var imViewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(imViewport.WorkPos);
        ImGui.SetNextWindowSize(imViewport.WorkSize);

        var fullscreenFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus;
        if (ImGui.Begin("RainEd", fullscreenFlags))
        {
            if (ImGui.BeginTabBar("Editor Tabs", ImGuiTabBarFlags.None))
            {
                if (ImGui.BeginTabItem("Overview"))
                {
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Geometry"))
                {
                    ImGui.Text($"Work Layer: {geometryEditor.WorkLayer}");

                    var regionMax = ImGui.GetWindowContentRegionMax();
                    var regionMin = ImGui.GetCursorPos();

                    geometryEditor.Resize((int)(regionMax.X - regionMin.X), (int)(regionMax.Y - regionMin.Y));
                    geometryEditor.ImguiRender();

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }
        ImGui.End();

        ImGui.ShowDemoWindow();
        rlImGui.End();
    }
}