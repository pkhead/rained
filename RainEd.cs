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

    private readonly string[] _viewModes = new string[2] {
        "Overlay", "Stack"
    };

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

                // TOOD: make each tab a separate class
                if (ImGui.BeginTabItem("Geometry"))
                {
                    // work layer
                    var workLayer = geometryEditor.WorkLayer + 1;
                    ImGui.SetNextItemWidth(ImGui.GetTextLineHeightWithSpacing() * 4);
                    ImGui.InputInt("Work Layer", ref workLayer);
                    workLayer = Math.Clamp(workLayer, 1, 3);
                    
                    geometryEditor.WorkLayer = workLayer - 1;

                    // view mode
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(ImGui.GetTextLineHeight() * 8);
                    if (ImGui.BeginCombo("View Mode", _viewModes[(int)geometryEditor.layerViewMode]))
                    {
                        for (int i = 0; i < _viewModes.Count(); i++)
                        {
                            bool isSelected = i == (int)geometryEditor.layerViewMode;
                            if (ImGui.Selectable(_viewModes[i], isSelected))
                            {
                                geometryEditor.layerViewMode = (GeometryEditor.LayerViewMode) i;
                            }

                            if (isSelected) ImGui.SetItemDefaultFocus();
                        }

                        ImGui.EndCombo();
                    }

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