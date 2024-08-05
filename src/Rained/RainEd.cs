using Raylib_cs;
using ImGuiNET;
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
    public const string Version = "v2.0.4";

    public static RainEd Instance = null!;

    public bool Running = true; // if false, Boot.cs will close the window

    public static Glib.Window Window => Boot.Window;
    public static Glib.RenderContext RenderContext => Glib.RenderContext.Instance!;

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
    
    /// <summary>
    /// The path of the emergency save file. Is created when the application
    /// encounters a fatal error.
    /// </summary> 
    public static readonly string EmergencySaveFolder = Path.Combine(Boot.AppDataPath, "emsavs");

    /// <summary>
    /// True if the file for the current level is non-existent or is an emergency save.
    /// </summary>
    public bool IsTemporaryFile => string.IsNullOrEmpty(CurrentFilePath) || Path.GetDirectoryName(CurrentFilePath) == EmergencySaveFolder;

    public Level Level { get => level; }
    public LevelView LevelView { get => levelView; }
    
    public ChangeHistory.ChangeHistory ChangeHistory { get => changeHistory; }

    // this is used to set window IsEventDriven to true
    // when the user hasn't interacted with the window in a while
    private float remainingActiveTime = 2f;
    
    private double lastRopeUpdateTime = 0f;
    private float simTimeLeftOver = 0f;
    public float SimulationTimeRemainder { get => simTimeLeftOver; }

    /// <summary>
    /// This is true whenever Rained is in a temporary state where level editing
    /// should be locked (i.e., when saving, loading, resizing the level, or rendering)
    /// </summary>
    public bool IsLevelLocked { get; set; }

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

        Log.Information("========================");
        Log.Information("Rained {Version} started", Version);
        Log.Information("App data located in {AppDataPath}", Boot.AppDataPath);

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
                Log.Error("Failed to load user preferences!\n{ErrorMessage}", e);
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

        // load shaders
        Shaders.LoadShaders();

        // run the update checker
        var versionCheckTask = UpdateChecker.FetchLatestVersion();

        string initPhase = null!;

        #if !DEBUG
        try
        #endif
        {
            AssetGraphics = new AssetGraphicsProvider();
            
            initPhase = "materials";
            Log.Information("Initializing materials database...");
            MaterialDatabase = new Tiles.MaterialDatabase();

            initPhase = "tiles";
            Log.Information("Initializing tile database...");
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

                Log.Error(displayMsg);
                Boot.DisplayError("Could not start", displayMsg);
                throw new RainEdStartupException();
            }
            
            initPhase = "effects";
            Log.Information("Initializing effects database...");
            EffectsDatabase = new EffectsDatabase();

            initPhase = "light brushes";
            Log.Information("Initializing light brush database...");
            LightBrushDatabase = new Light.LightBrushDatabase();

            initPhase = "props";
            Log.Information("Initializing prop database...");
            PropDatabase = new Props.PropDatabase(TileDatabase);

            Log.Information("----- ASSET INIT DONE! -----");
        }
        #if !DEBUG
        catch (Exception e)
        {
            Log.Error(e.ToString());

            if (e is RainEdStartupException)
                throw;
            
            Boot.DisplayError("Could not start", $"There was an error while loading the {initPhase} Init.txt file:\n\n{e}\n\nThe application will now quit.");
            throw new RainEdStartupException();
        }
        #endif
        
        level = Level.NewDefaultLevel();

        LevelGraphicsTexture = RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath,"assets","level-graphics.png"));

        Log.Information("Initializing change history...");
        changeHistory = new ChangeHistory.ChangeHistory();

        Log.Information("Creating level view...");
        levelView = new LevelView();

        if (Preferences.StaticDrizzleLingoRuntime)
        {
            Log.Information("Initializing Zygote runtime...");
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
        PaletteWindow.IsWindowOpen = Preferences.ShowPaletteWindow;

        // level boot load
        if (levelPath.Length > 0)
        {
            Log.Information("Boot load " + levelPath);
            LoadLevel(levelPath);
        }
        else
        {
            EditorWindow.RequestLoadEmergencySave();
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
                Log.Information("Version check successful!");
                Log.Information(LatestVersionInfo.VersionName);

                if (LatestVersionInfo.VersionName != Version)
                {
                    EditorWindow.ShowNotification("New version available! Check the About window for more info.");
                }
            }
            else
            {
                Log.Information("Version check was disabled");
            }
        }
        else if (versionCheckTask.IsFaulted)
        {
            Log.Error("Version check faulted...");
            Log.Error(versionCheckTask.Exception.ToString());
        }

        Log.Information("Boot successful!");
        lastRopeUpdateTime = Raylib.GetTime();

        Boot.Window.KeyDown += (Glib.Key _, int _) =>
            NeedScreenRefresh();

        Boot.Window.KeyUp += (Glib.Key _, int _) =>
            NeedScreenRefresh();

        Boot.Window.MouseDown += (Glib.MouseButton _) =>
            NeedScreenRefresh();

        Boot.Window.MouseUp += (Glib.MouseButton _) =>
            NeedScreenRefresh();

        Boot.Window.MouseMove += (float x, float y) =>
            NeedScreenRefresh();
    }

    /// <summary>
    /// Request the window to rerender even if the user hasn't sent any inputs.
    /// </summary>
    public void NeedScreenRefresh()
    {
        const float ActivityWaitTime = 2f;
        remainingActiveTime = ActivityWaitTime;
        Boot.Window.IsEventDriven = false;
    }

    public void Shutdown()
    {
        // save user-created autotiles
        Autotiles.SaveConfig();

        // save user preferences
        levelView.SavePreferences(Preferences);
        Preferences.ViewKeyboardShortcuts = ShortcutsWindow.IsWindowOpen;
        Preferences.ShowPaletteWindow = PaletteWindow.IsWindowOpen;

        Preferences.WindowWidth = Raylib.GetScreenWidth();
        Preferences.WindowHeight = Raylib.GetScreenHeight();
        Preferences.WindowMaximized = Raylib.IsWindowMaximized();
        Preferences.DataPath = AssetDataPath;
        
        UserPreferences.SaveToFile(Preferences, prefFilePath);
        levelView.Renderer.Dispose();
    }

    public void ShowPathInSystemBrowser(string path, bool reveal)
    {
        var success = false;

        if (reveal)
        {
            success = Platform.RevealPath(path);
        }
        else
        {
            success = Platform.OpenPath(path);
        }

        if (!success)
        {
            Log.Error("Could not show path '{Path}'", path);
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

        //AssetGraphics.ClearTextureCache();
    }

    public void LoadLevel(string path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            Log.Information("Loading level {Path}...", path);

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

                    Log.Information("Done!");
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

                //AssetGraphics.ClearTextureCache();
            }
            catch (Exception e)
            {
                Log.Error("Error loading level {Path}:\n{ErrorMessage}", path, e);
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
    public async Task SaveLevel(string path)
    {
        Log.Information("Saving level to {Path}...", path);
        IsLevelLocked = true;

        levelView.FlushDirty();

        try
        {
            string oldFilePath = currentFilePath;

            LevelSerialization.SaveLevelTextFile(path);
            await LevelSerialization.SaveLevelLightMapAsync(path);
            await ContinueOnNextFrame();

            currentFilePath = path;
            UpdateTitle();
            changeHistory.MarkUpToDate();
            Log.Information("Done!");
            EditorWindow.ShowNotification("Saved!");
            AddToRecentFiles(currentFilePath);

            // if the old level was an emergency save and the user
            // saved it to a non-emergency save file, delete the
            // old file as it is no longer necessary.
            var oldParentFolder = Path.GetDirectoryName(oldFilePath);
            var newParentFolder = Path.GetDirectoryName(currentFilePath);

            if (oldParentFolder == EmergencySaveFolder && newParentFolder != EmergencySaveFolder)
            {
                File.Delete(oldFilePath);
                File.Delete(Path.Combine(oldParentFolder, Path.GetFileName(oldFilePath)) + ".png");
            }

            IsLevelLocked = false;
        }
        catch (Exception e)
        {
            Log.Error("Could not write level file:\n{ErrorMessage}", e);
            await ContinueOnNextFrame();
            EditorWindow.ShowNotification("Could not write level file");
            IsLevelLocked = false;
            throw;
        }
    }

    /// <summary>
    /// Create an emergency save of the level. Level serialization is quite stable,
    /// so this should work from a caught fatal exception.
    /// </summary>
    public void EmergencySave()
    {
        Directory.CreateDirectory(EmergencySaveFolder);

        var secs = (int)Math.Floor((DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds);
        var id = secs.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var fileName = CurrentFilePath;
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "unnamed";
        }
        else
        {
            fileName = Path.GetFileNameWithoutExtension(fileName);
        }

        var emSavFileName = Path.Combine(EmergencySaveFolder, $"{fileName}-{id}.txt");
        LevelSerialization.SaveLevelTextFile(emSavFileName);
        LevelSerialization.SaveLevelLightMap(emSavFileName);
    }

    public static string[] DetectEmergencySaves()
    {
        if (!Directory.Exists(EmergencySaveFolder)) return [];
        List<string> output = [];
        
        foreach (var file in Directory.EnumerateFiles(EmergencySaveFolder))
        {
            // if this is a .txt file and there is also a .png file of the same name,
            // this is most likely a level file...
            if (Path.GetExtension(file) == ".txt" && File.Exists(Path.Combine(EmergencySaveFolder, Path.GetFileNameWithoutExtension(file) + ".png")))
            {
                output.Add(file);
            }
        }

        return [..output];
    }

    public static void DiscardEmergencySaves()
    {
        if (!Directory.Exists(EmergencySaveFolder)) return;

        foreach (var file in Directory.GetFiles(EmergencySaveFolder))
        {
            if (file is not null)
            {
                if (!Platform.TrashFile(file))
                {
                    Log.Warning("TrashFile is not supported, resorted to permanent deletion.");
                    File.Delete(file);
                }
            }
        }

        Directory.Delete(EmergencySaveFolder);
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

    public async void ResizeLevel(int newWidth, int newHeight, int anchorX, int anchorY)
    {
        if (newWidth == level.Width && newHeight == level.Height) return;
        Log.Information("Resizing level...");
        IsLevelLocked = true;

        levelView.FlushDirty();
        var dstOrigin = await level.ResizeAsync(newWidth, newHeight, anchorX, anchorY);
        await ContinueOnNextFrame();

        levelView.ReloadLevel();
        changeHistory.Clear();
        levelView.Renderer.ReloadLevel();
        levelView.ViewOffset += dstOrigin * Level.TileSize;

        Log.Information("Done!");
        IsLevelLocked = false;
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
        string levelName =
            string.IsNullOrEmpty(currentFilePath) ? "Untitled" :
            Path.GetFileNameWithoutExtension(currentFilePath);
        
        if (currentFilePath is not null && Path.GetDirectoryName(currentFilePath) == EmergencySaveFolder)
        {
            int hyphenIndex = levelName.LastIndexOf('-');
            if (hyphenIndex >= 0)
            {
                levelName = levelName[0..hyphenIndex] + " [EMERGENCY SAVE]";
            }
            else
            {
                levelName += " [EMERGENCY SAVE]";
            }
        }
        
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

    private readonly List<Action> deferredActions = [];

    private readonly Mutex _tcsMutex = new();
    private readonly List<TaskCompletionSource> _tasksToRunOnNextFrame = [];

    /// <summary>
    /// Run an action on the next frame. <br /><br />
    /// Used mainly for deferring the disposal of textures
    /// that were drawn in ImGui on the same frame, as ImGui
    /// will use the texture ID of the disposed texture at the
    /// end of the frame.
    /// </summary>
    /// <param name="action">The action to run on the next frame.</param> 
    public void DeferToNextFrame(Action action)
    {
        deferredActions.Add(action);
    }

    public Task ContinueOnNextFrame()
    {
        var tcs = new TaskCompletionSource();
        _tcsMutex.WaitOne();
        _tasksToRunOnNextFrame.Add(tcs);
        _tcsMutex.ReleaseMutex();

        return tcs.Task;
    }
    
    public void Draw(float dt)
    {
        if (Raylib.WindowShouldClose())
            EditorWindow.PromptUnsavedChanges(() => Running = false);
        
        AssetGraphics.Maintenance();
        
        foreach (var f in deferredActions) f();
        deferredActions.Clear();

        {
            _tcsMutex.WaitOne();
            List<TaskCompletionSource> tasks = [.._tasksToRunOnNextFrame];
            _tasksToRunOnNextFrame.Clear();
            _tcsMutex.ReleaseMutex();

            foreach (var t in tasks) t.SetResult();
        }

        EditorWindow.UpdateMouseState();
        
        Raylib.ClearBackground(Color.DarkGray);
        KeyShortcuts.Update();
        ImGui.DockSpaceOverViewport();

        UpdateRopeSimulation();
        EditorWindow.Render();

        if (ImGui.IsKeyPressed(ImGuiKey.F1))
            DebugWindow.IsWindowOpen = !DebugWindow.IsWindowOpen;
        
        // don't sleep rained if mouse is held down
        // for example, the user may be holding down a +/- imgui input, and i'm not quite sure how to detect that.
        if (ImGui.IsMouseDown(ImGuiMouseButton.Left) || ImGui.IsMouseDown(ImGuiMouseButton.Right) || ImGui.IsMouseDown(ImGuiMouseButton.Middle))
        {
            NeedScreenRefresh();
        }
        
#if DEBUG
        if (ImGui.IsKeyPressed(ImGuiKey.F2))
            throw new Exception("Test Exception");
#endif
        DebugWindow.ShowWindow();

        if (remainingActiveTime > 0f)
        {
            remainingActiveTime -= dt;
        }
        else
        {
            Boot.Window.IsEventDriven = true;
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