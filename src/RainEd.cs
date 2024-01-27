using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

namespace RainEd;

class RainEd
{
    private readonly Level level;

    private readonly LevelOverview overview;
    private readonly GeometryEditor geometry;

    public RainEd() {
        level = new();
        
        overview = new LevelOverview(level);
        geometry = new GeometryEditor(level);
    }

    public void Draw()
    {
        Raylib.ClearBackground(Color.DarkGray);

        rlImGui.Begin();
        ImGui.DockSpaceOverViewport();

        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                ImGui.MenuItem("New");
                ImGui.MenuItem("Open");
                ImGui.MenuItem("Save");
                ImGui.MenuItem("Save As...");
                ImGui.Separator();
                ImGui.MenuItem("Render");
                ImGui.Separator();
                ImGui.MenuItem("Quit");

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Edit"))
            {
                ImGui.MenuItem("Undo");
                ImGui.MenuItem("Redo");
                ImGui.Separator();
                ImGui.MenuItem("Cut");
                ImGui.MenuItem("Copy");
                ImGui.MenuItem("Paste");
                ImGui.Separator();
                ImGui.MenuItem("Level Properties");

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("View"))
            {
                if (ImGui.MenuItem("Overview", null, overview.IsWindowOpen))
                    overview.IsWindowOpen = !overview.IsWindowOpen;

                if (ImGui.MenuItem("Geometry Editor", null, geometry.IsWindowOpen))
                    geometry.IsWindowOpen = !geometry.IsWindowOpen;
                
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Help"))
            {
                ImGui.MenuItem("About...");
                
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }

        
        if (overview.IsWindowOpen && ImGui.Begin("Overview", ref overview.IsWindowOpen))
        {
            overview.Render();
            ImGui.EndTabItem();
        }

        if (geometry.IsWindowOpen && ImGui.Begin("Geometry Editor", ref geometry.IsWindowOpen))
        {
            geometry.Render();
            ImGui.EndTabItem();
        }

        ImGui.ShowDemoWindow();
        rlImGui.End();
    }
}