using Raylib_cs;

using System.Numerics;
using ImGuiNET;
using Serilog;
using System.Runtime.InteropServices;
using Serilog.Core;
using System.Diagnostics;
using NLua.Exceptions;
using RainEd.Autotiles;

namespace RainEd;

[Serializable]
public class RainEdStartupException : Exception
{
    public RainEdStartupException() { }
    public RainEdStartupException(string message) : base(message) { }
    public RainEdStartupException(string message, Exception inner) : base(message, inner) { }
}

sealed class RainEd
{
    public const string Version = "b1.4.4";

    public static RainEd Instance = null!;

    public bool Running = true; // if false, Boot.cs will close the window
    private readonly Logger _logger;
    public static Logger Logger { get => Instance._logger; }

    public static Glib.Window Window => Boot.Window;
    public static Glib.RenderContext RenderContext => Boot.Window.RenderContext!;

    private Level level;
    public readonly RlManaged.Texture2D LevelGraphicsTexture;
    private readonly LevelView levelView;
    private readonly ChangeHistory.ChangeHistory changeHistory;
    private bool ShowDemoWindow = false;

    private readonly string prefFilePath;
    public UserPreferences Preferences;

    public string AssetDataPath;
    public readonly AssetGraphicsProvider AssetGraphics;
    public readonly Tiles.MaterialDatabase MaterialDatabase;
    public readonly Tiles.TileDatabase TileDatabase;
    public readonly EffectsDatabase EffectsDatabase;
    public readonly Light.LightBrushDatabase LightBrushDatabase;
    public readonly Props.PropDatabase PropDatabase;
    public readonly AutotileCatalog Autotiles;

    private string currentFilePath = string.Empty;

    public string CurrentFilePath { get => currentFilePath; }
    public Level Level { get => level; }
    public LevelView LevelView { get => levelView; }
    
    public ChangeHistory.ChangeHistory ChangeHistory { get => changeHistory; }

    private double lastRopeUpdateTime = 0f;
    private float simTimeLeftOver = 0f;
    public float SimulationTimeRemainder { get => simTimeLeftOver; }

    public readonly RlManaged.Texture2D PlaceholderTexture;

    /// <summary>
    /// Information for the latest Rained release fetched
    /// from the GitHub API.
    /// </summary>
    public readonly RainedVersionInfo? LatestVersionInfo = null;

    public struct Command(string name, Action<int> cb)
    {
        private static int nextID = 0;

        public readonly int ID = nextID++;
        public readonly string Name = name;
        public readonly Action<int> Callback = cb;
    };

    private readonly List<Command> customCommands = [];
    public List<Command> CustomCommands { get => customCommands; }
    
    public RainEd(string? assetData, string levelPath = "") {
        if (Instance != null)
            throw new Exception("Attempt to create more than one RainEd instance");
        
        Instance = this;

        // create serilog logger
        Directory.CreateDirectory(Path.Combine(Boot.AppDataPath, "logs"));

        bool logToStdout = Boot.Options.ConsoleAttached || Boot.Options.LogToStdout;
#if DEBUG
        logToStdout = true;
#endif

        var loggerConfig = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#endif
            .WriteTo.File(Path.Combine(Boot.AppDataPath, "logs", "log.txt"), rollingInterval: RollingInterval.Day);

        if (logToStdout)
            loggerConfig = loggerConfig.WriteTo.Console();

        _logger = loggerConfig.CreateLogger();

        Logger.Information("========================");
        Logger.Information("Rained {Version} started", Version);
        Logger.Information("App data located in {AppDataPath}", Boot.AppDataPath);

        // load user preferences
        KeyShortcuts.InitShortcuts();
        prefFilePath = Path.Combine(Boot.AppDataPath, "config", "preferences.json");

        if (File.Exists(prefFilePath))
        {
            try
            {
                Preferences = UserPreferences.LoadFromFile(prefFilePath);
                Preferences.LoadKeyboardShortcuts();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to load user preferences!\n{ErrorMessage}", e);
                Preferences = new UserPreferences();
                EditorWindow.ShowNotification("Failed to load preferences");
            }
        }
        else
        {
            // first-time
            Preferences = new UserPreferences();
            if (assetData is not null)
            {
                Preferences.DataPath = assetData;
            }
            
            UserPreferences.SaveToFile(Preferences, prefFilePath);
        }

        // if --data was passed into the program arguments,
        // set the data path value to it
        if (Boot.Options.DrizzleDataPath is not null)
        {
            Preferences.DataPath = Boot.Options.DrizzleDataPath;
        }

        Preferences.ApplyTheme();

        // load asset database
        AssetDataPath = Preferences.DataPath;

        // halt if asset data path directory doesn't exist
        if (!Directory.Exists(AssetDataPath))
        {
            Boot.DisplayError("Could not start", "The Data directory is missing!\n\nPlease edit the \"dataPath\" property in preferences.json to point to a valid RWLE data folder.\n\nAlternatively, you may delete preferences.json and re-launch Rained to show the data directory configuration screen.");
            throw new RainEdStartupException();
        }

        // create placeholder for missing texture
        {
            using var img = RlManaged.Image.GenColor(2, 2, Color.Black);
            img.DrawPixel(0, 0, new Color(255, 0, 255, 255));
            img.DrawPixel(1, 1, new Color(255, 0, 255, 255));
            PlaceholderTexture = RlManaged.Texture2D.LoadFromImage(img);
        }

        // run the update checker
        var versionCheckTask = UpdateChecker.FetchLatestVersion();

        string initPhase = null!;

        #if !DEBUG
        try
        #endif
        {
            AssetGraphics = new AssetGraphicsProvider();
            
            initPhase = "materials";
            Logger.Information("Initializing materials database...");
            MaterialDatabase = new Tiles.MaterialDatabase();

            initPhase = "tiles";
            Logger.Information("Initializing tile database...");
            TileDatabase = new Tiles.TileDatabase();

            // init autotile catalog
            Autotiles = new AutotileCatalog();
            
            // run lua scripts after initializing the tiles
            // (trying to get lua error messages to show as soon as possible)
            try
            {
                LuaInterface.Initialize();
            }
            catch (LuaScriptException e)
            {
                Exception actualException = e.IsNetException ? e.InnerException! : e;
                string? stackTrace = actualException.Data["Traceback"] as string;

                var displayMsg = "RainEd could not start due to an error in a Lua script:\n\n" + actualException.Message;
                if (stackTrace is not null)
                {
                    displayMsg += "\n" + stackTrace;
                }

                _logger.Error(displayMsg);
                Boot.DisplayError("Could not start", displayMsg);
                throw new RainEdStartupException();
            }
            
            initPhase = "effects";
            Logger.Information("Initializing effects database...");
            EffectsDatabase = new EffectsDatabase();

            initPhase = "light brushes";
            Logger.Information("Initializing light brush database...");
            LightBrushDatabase = new Light.LightBrushDatabase();

            initPhase = "props";
            Logger.Information("Initializing prop database...");
            PropDatabase = new Props.PropDatabase(TileDatabase);

            Logger.Information("----- ASSET INIT DONE! -----\n\n\n");
        }
        #if !DEBUG
        catch (Exception e)
        {
            _logger.Error(e.ToString());

            if (e is RainEdStartupException)
                throw;
            
            Boot.DisplayError("Could not start", $"There was an error while loading the {initPhase} Init.txt file:\n\n{e}\n\nThe application will now quit.");
            throw new RainEdStartupException();
        }
        #endif
        
        level = Level.NewDefaultLevel();

        LevelGraphicsTexture = RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath,"assets","level-graphics.png"));

        Logger.Information("Initializing change history...");
        changeHistory = new ChangeHistory.ChangeHistory();

        Logger.Information("Creating level view...");
        levelView = new LevelView();

        if (Preferences.StaticDrizzleLingoRuntime)
        {
            Logger.Information("Initializing Zygote runtime...");
            Drizzle.DrizzleRender.InitStaticRuntime();
        }

        UpdateTitle();

        // apply window preferences
        Window.SetSize(Preferences.WindowWidth, Preferences.WindowHeight);
        if (Preferences.WindowMaximized)
        {
            Window.WindowState = Silk.NET.Windowing.WindowState.Maximized;
        }
        ShortcutsWindow.IsWindowOpen = Preferences.ViewKeyboardShortcuts;

        // level boot load
        if (levelPath.Length > 0)
        {
            Logger.Information("Boot load " + levelPath);
            LoadLevel(levelPath);
        }

        // force gc. i just added this to try to collect
        // the now-garbage asset image data, as they have
        // been converted into GPU textures. 
        GC.Collect(2, GCCollectionMode.Aggressive, true, true);
        GC.WaitForPendingFinalizers();

        // check on the update checker
        if (!versionCheckTask.IsCompleted)
            versionCheckTask.Wait();
        
        if (versionCheckTask.IsCompletedSuccessfully)
        {
            LatestVersionInfo = versionCheckTask.Result;

            if (LatestVersionInfo is not null)
            {
                Logger.Information("Version check successful!");
                Logger.Information(LatestVersionInfo.VersionName);

                if (LatestVersionInfo.VersionName != Version)
                {
                    EditorWindow.ShowNotification("New version available! Check the About window for more info.");
                }
            }
            else
            {
                Logger.Information("Version check was disabled");
            }
        }
        else if (versionCheckTask.IsFaulted)
        {
            Logger.Error("Version check faulted...");
            Logger.Error(versionCheckTask.Exception.ToString());
        }

        Logger.Information("Boot successful!");
        lastRopeUpdateTime = Raylib.GetTime();
    }

    public void Shutdown()
    {
        // save user-created autotiles
        Autotiles.SaveConfig();

        // save user preferences
        levelView.SavePreferences(Preferences);
        Preferences.ViewKeyboardShortcuts = ShortcutsWindow.IsWindowOpen;

        Preferences.WindowWidth = Raylib.GetScreenWidth();
        Preferences.WindowHeight = Raylib.GetScreenHeight();
        Preferences.WindowMaximized = Raylib.IsWindowMaximized();
        Preferences.DataPath = AssetDataPath;
        
        UserPreferences.SaveToFile(Preferences, prefFilePath);
    }

    public void ShowPathInSystemBrowser(string path, bool reveal)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (reveal)
                    Process.Start("explorer.exe", "/select," + path);
                else
                    Process.Start("explorer.exe", path);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // I can't test if this actually works as intended
                if (reveal)
                    Process.Start("open", "-R " + path);
                else
                    Process.Start("open", path);
            }
            else // assume Linux
            {
                if (reveal)
                    Process.Start("xdg-open", Path.GetDirectoryName(path)!);
                else
                    Process.Start("xdg-open", path);
            }
        }
        catch (Exception e)
        {
            Logger.Error("Could not show path '{Path}':\n{Error}", e);
            EditorWindow.ShowNotification("Could not open the system file browser");
        }
    }

    public void LoadDefaultLevel()
    {
        levelView.UnloadView();
        level.LightMap.Dispose();
        level = Level.NewDefaultLevel();
        ReloadLevel();
        levelView.LoadView();

        currentFilePath = string.Empty;
        UpdateTitle();
    }

    public void LoadLevel(string path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            Logger.Information("Loading level {Path}...", path);

            levelView.UnloadView();

            try
            {
                var loadRes = LevelSerialization.Load(path);

                if (loadRes.Level is not null)
                {
                    level = loadRes.Level;

                    ReloadLevel();
                    currentFilePath = path;
                    UpdateTitle();

                    Logger.Information("Done!");
                }
                else
                {
                    // level failed to load due to unrecognized assets
                    LevelLoadFailedWindow.LoadResult = loadRes;
                    LevelLoadFailedWindow.IsWindowOpen = true;
                }

                // i think it may be useful to add it to the list
                // even if the level failed to load due to unrecognized assets
                AddToRecentFiles(path);
            }
            catch (Exception e)
            {
                Logger.Error("Error loading level {Path}:\n{ErrorMessage}", path, e);
                EditorWindow.ShowNotification("Error while loading level");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            levelView.ReloadLevel();
            levelView.LoadView();
        }
    }

    /// <summary>
    /// Save the level to the given path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>True if the save was successful, false if not.</returns>
    public bool SaveLevel(string path)
    {
        Logger.Information("Saving level to {Path}...", path);

        levelView.FlushDirty();

        try
        {
            LevelSerialization.Save(path);
            currentFilePath = path;
            UpdateTitle();
            changeHistory.MarkUpToDate();
            Logger.Information("Done!");
            EditorWindow.ShowNotification("Saved!");
            AddToRecentFiles(currentFilePath);

            return true;
        }
        catch (Exception e)
        {
            Logger.Error("Could not write level file:\n{ErrorMessage}", e);
            EditorWindow.ShowNotification("Could not write level file");
        }

        return false;
    }

    private void AddToRecentFiles(string filePath)
    {
        var list = Preferences.RecentFiles;
        list.Remove(filePath);
        list.Add(filePath);

        while (list.Count > 10)
        {
            list.RemoveAt(0);
        }
    }

    public void ResizeLevel(int newWidth, int newHeight, int anchorX, int anchorY)
    {
        if (newWidth == level.Width && newHeight == level.Height) return;
        Logger.Information("Resizing level...");

        levelView.FlushDirty();
        var dstOrigin = level.Resize(newWidth, newHeight, anchorX, anchorY);
        levelView.ReloadLevel();
        changeHistory.Clear();
        levelView.Renderer.ReloadLevel();
        levelView.ViewOffset += dstOrigin * Level.TileSize;

        Logger.Information("Done!");
    }

    private void ReloadLevel()
    {
        levelView.ReloadLevel();
        changeHistory.Clear();
        changeHistory.MarkUpToDate();
        levelView.Renderer.ReloadLevel();
    }

    private void UpdateTitle()
    {
        var levelName =
            string.IsNullOrEmpty(currentFilePath) ? "Untitled" :
            Path.GetFileNameWithoutExtension(currentFilePath);
        
        Raylib.SetWindowTitle($"Rained - {levelName}");
    }

    /// <summary>
    /// Register a command invokable by the user.
    /// </summary>
    /// <param name="name">The display name of the command.</param>
    /// <param name="cmd">The action to run on command.</param>
    public int RegisterCommand(string name, Action<int> callback)
    {
        var cmd = new Command(name, callback);
        customCommands.Add(cmd);
        return cmd.ID;
    }

    /// <summary>
    /// Unregister a command.
    /// </summary>
    /// <param name="cmd">The action of the command to unregister.</param>
    public void UnregisterCommand(int id)
    {
        for (int i = 0; i < customCommands.Count; i++)
        {
            if (customCommands[i].ID == id)
            {
                customCommands.RemoveAt(i);
                break;
            }
        }
    }
    
    public void Draw(float dt)
    {
        if (Raylib.WindowShouldClose())
            EditorWindow.PromptUnsavedChanges(() => Running = false);
        
        EditorWindow.UpdateMouseState();
        
        Raylib.ClearBackground(Color.DarkGray);
        KeyShortcuts.Update();
        ImGui.DockSpaceOverViewport();

        UpdateRopeSimulation();
        EditorWindow.Render();

        if (ImGui.IsKeyPressed(ImGuiKey.F1))
            ShowDemoWindow = !ShowDemoWindow;
        
        // this is how imgui is documented
        // you see what it can do and when i want to know how it does that,
        // i ctrl+f imgui_demo.cpp.
        // I guess it works?
        if (ShowDemoWindow)
        {
            ImGui.ShowDemoWindow(ref ShowDemoWindow);
        }
    }

    public void UpdateRopeSimulation()
    {
        double nowTime = Raylib.GetTime();
        double stepTime = 1.0 / 30.0;

        for (int i = 0; nowTime >= lastRopeUpdateTime + stepTime; i++)
        {
            lastRopeUpdateTime += stepTime;
            
            // tick rope simulation
            foreach (var prop in level.Props)
            {
                prop.TickRopeSimulation();
            }

            // break if too many iterations in one frame
            if (i == 8)
            {
                lastRopeUpdateTime += stepTime * Math.Floor(nowTime - lastRopeUpdateTime);
                break;
            }
        }

        simTimeLeftOver = (float)((nowTime - lastRopeUpdateTime) / stepTime);

        foreach (var prop in level.Props)
        {
            if (prop.Rope is not null && prop.Rope.Simulate)
                prop.Rope.SimulationTimeRemainder = simTimeLeftOver;
        }
    }
}