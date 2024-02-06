using Raylib_cs;
using rlImGui_cs;
using System.Numerics;
using ImGuiNET;
using System.Text;
using System.Runtime.InteropServices;

namespace RainEd;

public class RainEd
{
    private Level level;
    public readonly RlManaged.Texture2D LevelGraphicsTexture;
    public readonly Tiles.Database TileDatabase;
    private readonly ChangeHistory changeHistory;
    private readonly EditorWindow editorWindow;

    private string currentFilePath = string.Empty;

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

        UpdateTitle();
        RegisterShortcuts();
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
            currentFilePath = path;
            UpdateTitle();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error loading file " + path);
            Console.WriteLine(e);
            ShowError("Could not load level");
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        editorWindow.ReloadLevel();
        editorWindow.LoadView();
    }

    private void SaveLevel(string path)
    {
        editorWindow.UnloadView();

        try
        {
            LevelSerialization.Save(this, path);
            currentFilePath = path;
            UpdateTitle();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            ShowError("Could not write level file");
        }

        editorWindow.LoadView();
    }

    private void UpdateTitle()
    {
        var levelName =
            string.IsNullOrEmpty(currentFilePath) ? "Untitled" :
            Path.GetFileNameWithoutExtension(currentFilePath);
        
        Raylib.SetWindowTitle($"Rained - {levelName}");
    }

#region Shortcuts
    private class KeyShortcut
    {
        public string Name;
        public string ShortcutString;
        public ImGuiKey Key;
        public ImGuiModFlags Mods;
        public bool IsActivated = false;
        public bool AllowRepeat = false;

        public KeyShortcut(string name, ImGuiKey key, ImGuiModFlags mods, bool allowRepeat = false)
        {
            // build shortcut string
            var str = new List<string>();

            if (mods.HasFlag(ImGuiModFlags.Ctrl))
                str.Add("Ctrl");
            
            if (mods.HasFlag(ImGuiModFlags.Shift))
                str.Add("Shift");
            
            if (mods.HasFlag(ImGuiModFlags.Alt))
                str.Add("Alt");
            
            if (mods.HasFlag(ImGuiModFlags.Super))
                str.Add("Super");
            
            str.Add(ImGui.GetKeyName(key));

            ShortcutString = string.Join('+', str);

            // initialize other properties
            Name = name;
            Key = key;
            Mods = mods;
            AllowRepeat = allowRepeat;
        }

        public bool IsKeyPressed()
            =>
                ImGui.IsKeyPressed(Key, AllowRepeat) &&
                (Mods.HasFlag(ImGuiModFlags.Ctrl) == ImGui.IsKeyDown(ImGuiKey.ModCtrl)) &&
                (Mods.HasFlag(ImGuiModFlags.Shift) == ImGui.IsKeyDown(ImGuiKey.ModShift)) &&
                (Mods.HasFlag(ImGuiModFlags.Alt) == ImGui.IsKeyDown(ImGuiKey.ModAlt)) &&
                (Mods.HasFlag(ImGuiModFlags.Super) == ImGui.IsKeyDown(ImGuiKey.ModSuper));
    }
    private readonly Dictionary<string, KeyShortcut> keyShortcuts = new();

    public void RegisterKeyShortcut(string name, ImGuiKey key, ImGuiModFlags mods, bool allowRepeat = false)
    {
        keyShortcuts.Add(name, new KeyShortcut(name, key, mods, allowRepeat));
    }

    public bool IsShortcutActivated(string id)
        => keyShortcuts[id].IsActivated;

    public void ImGuiMenuItemShortcut(string shortcutId, string name)
    {
        var shortcutData = keyShortcuts[shortcutId];
        if (ImGui.MenuItem(name, shortcutData.ShortcutString))
            shortcutData.IsActivated = true;
    }

    private void RegisterShortcuts()
    {
        RegisterKeyShortcut("NavUp", ImGuiKey.W, ImGuiModFlags.None, true);
        RegisterKeyShortcut("NavLeft", ImGuiKey.A, ImGuiModFlags.None, true);
        RegisterKeyShortcut("NavDown", ImGuiKey.S, ImGuiModFlags.None, true);
        RegisterKeyShortcut("NavRight", ImGuiKey.D, ImGuiModFlags.None, true);
        RegisterKeyShortcut("NewObject", ImGuiKey.N, ImGuiModFlags.None, true);

        RegisterKeyShortcut("New", ImGuiKey.N, ImGuiModFlags.Ctrl);
        RegisterKeyShortcut("Open", ImGuiKey.O, ImGuiModFlags.Ctrl);
        RegisterKeyShortcut("Save", ImGuiKey.S, ImGuiModFlags.Ctrl);
        RegisterKeyShortcut("SaveAs", ImGuiKey.S, ImGuiModFlags.Ctrl | ImGuiModFlags.Shift);

        RegisterKeyShortcut("Cut", ImGuiKey.X, ImGuiModFlags.Ctrl);
        RegisterKeyShortcut("Copy", ImGuiKey.C, ImGuiModFlags.Ctrl);
        RegisterKeyShortcut("Paste", ImGuiKey.V, ImGuiModFlags.Ctrl);
        RegisterKeyShortcut("Undo", ImGuiKey.Z, ImGuiModFlags.Ctrl);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            RegisterKeyShortcut("Redo", ImGuiKey.Y, ImGuiModFlags.Ctrl);
        else
            RegisterKeyShortcut("Redo", ImGuiKey.Z, ImGuiModFlags.Ctrl | ImGuiModFlags.Shift);
    }

    private void HandleShortcuts()
    {
        // activate shortcuts on key press
        if (!ImGui.GetIO().WantTextInput)
        {
            foreach (var shortcut in keyShortcuts.Values)
            {
                if (shortcut.IsKeyPressed())
                    shortcut.IsActivated = true;
            }
        }

        if (IsShortcutActivated("New"))
        {
            editorWindow.UnloadView();
            level = Level.NewDefaultLevel(this);
            editorWindow.ReloadLevel();
            changeHistory.Clear();
            editorWindow.LoadView();

            currentFilePath = string.Empty;
            UpdateTitle();
        }

        if (IsShortcutActivated("Open"))
        {
            LevelBrowser.Open(LevelBrowser.OpenMode.Read, LoadLevel, currentFilePath);
        }

        if (IsShortcutActivated("Save"))
        {
            if (string.IsNullOrEmpty(currentFilePath))
                LevelBrowser.Open(LevelBrowser.OpenMode.Write, SaveLevel, currentFilePath);
            else
                SaveLevel(currentFilePath);
        }

        if (IsShortcutActivated("SaveAs"))
        {
            LevelBrowser.Open(LevelBrowser.OpenMode.Write, SaveLevel, currentFilePath);
        }

        if (IsShortcutActivated("Undo"))
        {
            Undo();
        }

        if (IsShortcutActivated("Redo"))
        {
            Redo();
        }
    }
#endregion

    public void Draw(float dt)
    {
        Raylib.ClearBackground(Color.DarkGray);
        
        foreach (var shortcut in keyShortcuts.Values)
            shortcut.IsActivated = false;

        rlImGui.Begin();
        ImGui.DockSpaceOverViewport();

        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                ImGuiMenuItemShortcut("New", "New");
                ImGuiMenuItemShortcut("Open", "Open");
                ImGuiMenuItemShortcut("Save", "Save");
                ImGuiMenuItemShortcut("SaveAs", "Save As...");

                ImGui.Separator();
                ImGui.MenuItem("Render");
                ImGui.Separator();
                ImGui.MenuItem("Quit");

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Edit"))
            {
                ImGuiMenuItemShortcut("Undo", "Undo");
                ImGuiMenuItemShortcut("Redo", "Redo");
                ImGui.Separator();
                ImGuiMenuItemShortcut("Cut", "Cut");
                ImGuiMenuItemShortcut("Copy", "Copy");
                ImGuiMenuItemShortcut("Paste", "Paste");

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

        HandleShortcuts();

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