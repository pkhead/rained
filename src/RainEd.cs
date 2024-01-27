using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

namespace RainEd;

public partial class RainEd
{
    private readonly Level level;
    public readonly RlManaged.Texture2D LevelGraphicsTexture;

    private readonly LevelOverview overview;
    private readonly GeometryEditor geometry;

    public Level Level { get => level; }

    public RainEd() {
        level = new(this);
        LevelGraphicsTexture = new("data/level-graphics.png");
        
        overview = new LevelOverview(this);
        geometry = new GeometryEditor(this);

        Console.WriteLine("reading tileinit...");
        var parser = new Lingo.TokenParser(new StreamReader("data/tileinit.txt"));
        Console.WriteLine("tileinit done");
    }

    // TODO: show status thing in ImGui
    public void ShowError(string msg)
    {
        Console.WriteLine($"ERROR: {msg}");
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

        overview.Render();
        geometry.Render();

        ImGui.ShowDemoWindow();
        rlImGui.End();
    }
}