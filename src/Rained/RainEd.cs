using Raylib_cs;
using rlImGui_cs;
using System.Numerics;
using ImGuiNET;

namespace RainEd;

public class RainEd
{
    private Level level;
    public readonly RlManaged.Texture2D LevelGraphicsTexture;
    public readonly Tiles.Database TileDatabase;
    private readonly ChangeHistory changeHistory;
    private EditorWindow editorWindow;

    public Level Level { get => level; }
    public EditorWindow Window { get => editorWindow; }

    private string notification = "";
    private float notificationTime = 0f;
    private float notifFlash = 0f;

    public ChangeHistory ChangeHistory { get => changeHistory; }

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

        LevelGraphicsTexture = RlManaged.Texture2D.Load("data/level-graphics.png");
        editorWindow = new EditorWindow(this);
        changeHistory = new ChangeHistory(this);
    }

    public void ShowError(string msg)
    {
        notification = msg;
        notificationTime = 3f;
        notifFlash = 0f;
    }

    private void LoadLevel(string path)
    {
        editorWindow.UnloadView();

        try
        {
            level = LevelSerialization.Load(this, path);
            editorWindow.ReloadLevel();
            changeHistory.Clear();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error loading file " + path);
            Console.WriteLine(e);
            ShowError("Could not load level");
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        editorWindow.LoadView();
    }

    public void Draw(float dt)
    {
        Raylib.ClearBackground(Color.DarkGray);

        rlImGui.Begin();
        ImGui.DockSpaceOverViewport();

        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                ImGui.MenuItem("New");
                if (ImGui.MenuItem("Open"))
                {
                    LevelBrowser.Open(LevelBrowser.OpenMode.Read, LoadLevel);
                }
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
                ImGui.MenuItem("Undo", "Ctrl+Z");
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
                if (ImGui.MenuItem("Grid", null, editorWindow.LevelRenderer.ViewGrid))
                {
                    editorWindow.LevelRenderer.ViewGrid = !editorWindow.LevelRenderer.ViewGrid;
                }

                if (ImGui.MenuItem("Obscured Beams", null, editorWindow.LevelRenderer.ViewObscuredBeams))
                {
                    editorWindow.LevelRenderer.ViewObscuredBeams = !editorWindow.LevelRenderer.ViewObscuredBeams;
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

        editorWindow.Render(dt);

        ImGui.ShowDemoWindow();

        if (LevelBrowser.Singleton is not null)
        {
            LevelBrowser.Singleton.Render();
        }

        // notification window
        if (notificationTime > 0f) {
            ImGuiWindowFlags windowFlags =
                ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoMove;
            
            ImGuiViewportPtr viewport = ImGui.GetMainViewport();
            const float pad = 10f;

            Vector2 windowPos = new(
                viewport.WorkPos.X + pad,
                viewport.WorkPos.Y + viewport.WorkSize.Y - pad
            );
            Vector2 windowPosPivot = new(0f, 1f);
            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always, windowPosPivot);

            var flashValue = (float) (Math.Sin(Math.Min(notifFlash, 0.25f) * 16 * Math.PI) + 1f) / 2f;
            var windowBg = ImGui.GetStyle().Colors[(int) ImGuiCol.WindowBg];

            if (flashValue > 0.5f)
                ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(flashValue, flashValue, flashValue, windowBg.W));
            else
                ImGui.PushStyleColor(ImGuiCol.WindowBg, windowBg);
            
            if (ImGui.Begin("Notification", windowFlags))
                ImGui.TextUnformatted(notification);
            ImGui.End();

            ImGui.PopStyleColor();

            notificationTime -= dt;
            notifFlash += dt;
        }
        rlImGui.End();
    }
    
    public void BeginChange() => changeHistory.BeginChange();
    public void EndChange() => changeHistory.EndChange();
    public void TryEndChange() => changeHistory.TryEndChange();
    public void Undo() => changeHistory.Undo();
    public void Redo() => changeHistory.Redo();
}