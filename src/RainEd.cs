using Raylib_cs;
using rlImGui_cs;
using System.Numerics;
using ImGuiNET;

namespace RainEd;

public class RainEd
{
    private readonly Level level;
    public readonly RlManaged.Texture2D LevelGraphicsTexture;
    public readonly Tiles.Database TileDatabase;

    private readonly EditorWindow editorWindow;

    public Level Level { get => level; }

    private string notification = "";
    private float notificationTime = 0f;
    private float notifFlash = 0f;

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

    public void ShowError(string msg)
    {
        notification = msg;
        notificationTime = 3f;
        notifFlash = 0f;
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

        editorWindow.Render(dt);

        ImGui.ShowDemoWindow();

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

#region Change History
    private struct CellChange
    {
        public int X, Y, Layer;
        public LevelCell OldState, NewState;
    };

    private struct ChangeRecord
    {
        public List<CellChange> CellChanges = new();

        public ChangeRecord() {}

        public readonly bool HasChange()
        {
            return CellChanges.Count > 0;
        }
    }

    private Stack<ChangeRecord> undoStack = new();
    private Stack<ChangeRecord> redoStack = new();

    private bool trackingChange = false;
    private LevelCell[,,]? oldLayers = null;

    public void BeginChange()
    {
        if (trackingChange) throw new Exception("BeginChange() already called");
        trackingChange = true;

        oldLayers = (LevelCell[,,]) level.Layers.Clone();
    }

    public void TryEndChange()
    {
        if (!trackingChange || oldLayers is null) return;
        EndChange();
    }

    public void EndChange()
    {
        if (!trackingChange || oldLayers is null) throw new Exception("EndChange() already called");
        trackingChange = false;
        redoStack.Clear();

        // find changes made to layers
        ChangeRecord changes = new();
        for (int l = 0; l < Level.LayerCount; l++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                for (int y = 0; y < level.Height; y++)
                {
                    if (!oldLayers[l,x,y].Equals(level.Layers[l,x,y]))
                    {
                        changes.CellChanges.Add(new CellChange()
                        {
                            X = x, Y = y, Layer = l,
                            OldState = oldLayers[l,x,y],
                            NewState = level.Layers[l,x,y]
                        });
                    }
                }
            }
        }

        // record changes
        if (changes.HasChange())
            undoStack.Push(changes);

        oldLayers = null;
    }

    public void Undo()
    {
        if (undoStack.Count == 0) return;
        var record = undoStack.Pop();
        redoStack.Push(record);

        // apply changes
        foreach (CellChange change in record.CellChanges)
        {
            level.Layers[change.Layer, change.X, change.Y] = change.OldState;
        }
    }

    public void Redo()
    {
        if (redoStack.Count == 0) return;
        var record = redoStack.Pop();
        undoStack.Push(record);

        // apply changes
        foreach (CellChange change in record.CellChanges)
        {
            level.Layers[change.Layer, change.X, change.Y] = change.NewState;
        }
    }
#endregion

}