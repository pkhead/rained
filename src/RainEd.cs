using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

namespace RainEd;

public class RainEd
{
    private readonly Level level;
    public readonly RlManaged.Texture2D LevelGraphicsTexture;
    public readonly Tiles.Database TileDatabase;

    private readonly EditorWindow editorWindow;

    public Level Level { get => level; }

    public RainEd(string levelPath = "") {
        TileDatabase = new Tiles.Database();
        
        if (levelPath.Length > 0)
        {
            level = LevelSerialization.Load(this, levelPath);
        }
        else
        {
            level = Level.NewDefaultLevel(this);
        }

        LevelGraphicsTexture = new("data/level-graphics.png");
        editorWindow = new EditorWindow(this);
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
                if (ImGui.MenuItem("Editor", null, editorWindow.IsWindowOpen))
                    editorWindow.IsWindowOpen = !editorWindow.IsWindowOpen;
                
                if (ImGui.MenuItem("Grid", null, editorWindow.LevelRenderer.ViewGrid))
                {
                    editorWindow.LevelRenderer.ViewGrid = !editorWindow.LevelRenderer.ViewGrid;
                }
                
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Help"))
            {
                ImGui.MenuItem("About...");
                
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }

        editorWindow.Render();

        ImGui.ShowDemoWindow();
        rlImGui.End();
    }
}