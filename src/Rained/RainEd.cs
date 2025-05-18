using Raylib_cs;
using ImGuiNET;
using NLua.Exceptions;
using Rained.Autotiles;
using Rained.EditorGui;
using Rained.Assets;
using Rained.LevelData;
using Rained.Rendering;
using System.Reflection;
using Rained.LuaScripting;
using System.Diagnostics;

namespace Rained;

[Serializable]
public class RainEdStartupException : Exception
{
    public RainEdStartupException() { }
    public RainEdStartupException(string message) : base(message) { }
    public RainEdStartupException(string message, Exception inner) : base(message, inner) { }
}

[Serializable]
public class NoLevelException : Exception
{
    public NoLevelException() { }
    public NoLevelException(string message) : base(message) { }
    public NoLevelException(string message, System.Exception inner) : base(message, inner) { }
}

/// <summary>
/// The main application.
/// </summary>
sealed class RainEd
{
    public static string Version;
    public static RainEd Instance = null!;

    static RainEd()
    {
        var asmVersion = Assembly.GetExecutingAssembly().GetName().Version;
        if (asmVersion is null)
        {
            Version = "v0.0.0";
        }
        else
        {
            Version = $"v{asmVersion.Major}.{asmVersion.Minor}.{asmVersion.Build}";
        }

        #if !FULL_RELEASE
        Version += "-dev";
        #endif
    }

    public bool Running = true; // if false, Boot.cs will close the window

    public static Glib.Window Window => Boot.Window;
    public static Glib.RenderContext RenderContext => Glib.RenderContext.Instance!;

    private LevelWindow? levelView;

    private readonly string prefFilePath;
    public UserPreferences Preferences;

    public string AssetDataPath;
    public readonly AssetGraphicsProvider AssetGraphics;
    public readonly MaterialDatabase MaterialDatabase;
    public readonly TileDatabase TileDatabase;
    public readonly EffectsDatabase EffectsDatabase;
    public readonly LightBrushDatabase LightBrushDatabase;
    public readonly PropDatabase PropDatabase;
    public readonly AutotileCatalog Autotiles;

    public string CurrentFilePath { get => CurrentTab!.FilePath; }
    
    /// <summary>
    /// The path of the emergency save file. Is created when the application
    /// encounters a fatal error.
    /// </summary> 
    public static readonly string EmergencySaveFolder = Path.Combine(Boot.AppDataPath, "emsavs");

    private readonly List<LevelTab> _tabs = [];
    public List<LevelTab> Tabs => _tabs;
    private LevelTab? _currentTab = null;
    public LevelTab? CurrentTab { get => _currentTab; set => SwitchTab(value, false); }

    public Level Level {
        get
        {
            if (CurrentTab is null) throw new NoLevelException();
            return CurrentTab.Level;
        }
    }

    public LevelWindow LevelView {
        get
        {
            if (CurrentTab is null) throw new NoLevelException();
            return levelView!; 
        }
    }

    public ChangeHistory.ChangeHistory ChangeHistory {
        get
        {
            if (CurrentTab is null) throw new NoLevelException();
            return CurrentTab!.ChangeHistory;
        }
    }

    // this is used to set window IsEventDriven to true
    // when the user hasn't interacted with the window in a while
    private float remainingActiveTime = 2f;

    // this is used to make sure window doesn't sleep when
    // any key is held down
    private int keysPressed = 0;
    
    private double lastRopeUpdateTime = 0f;

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

    public struct CommandCreationParameters(string name, Action<int> callback)
    {
        public string Name = name;
        public Action<int> Callback = callback;
        public bool AutoHistory = true;
        public bool RequiresLevel = true;
    }

    public record Command(CommandCreationParameters @params)
    {
        private static int nextID = 0;

        public readonly int ID = nextID++;
        public readonly string Name = @params.Name;
        public readonly Action<int> Callback = @params.Callback;
        public readonly CommandCreationParameters parameters = @params;
    };

    private readonly List<Command> customCommands = [];
    public List<Command> CustomCommands { get => customCommands; }
    
    public RainEd(string? assetData, IEnumerable<string> levelPaths) {
        if (Instance != null)
            throw new Exception("Attempt to create more than one RainEd instance");
        
        Instance = this;

        Log.Information("========================");
        Log.Information("Rained {Version} started", Version);
        
        // display drizzle version
        {
            var drizzleVer = typeof(global::Drizzle.Lingo.Runtime.LingoRuntime).Assembly.GetName().Version;
            if (drizzleVer is not null)
                Log.Information("Drizzle version: " + drizzleVer.ToString());
            else
                Log.Information("Drizzle version: UNKNOWN");
        }

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
                Log.UserLogger.Error("Failed to load user preferences!\n{ErrorMessage}", e);
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
            PlaceholderTexture.GlibTexture.WrapModeUV = Glib.TextureWrapMode.Repeat;
        }

        // load other graphics resources
        Shaders.LoadShaders();
        TextRendering.GenerateOutlineFont();
        GeometryIcons.Init();
        GeometryIcons.CurrentSet = Preferences.GeometryIcons;

        // run the update checker
        var versionCheckTask = Task.Run(UpdateChecker.FetchLatestVersion);

        string initPhase = null!;

        #if !DEBUG
        try
        #endif
        {
            DrizzleCast.Initialize();
            AssetGraphics = new AssetGraphicsProvider();
            
            initPhase = "materials";
            Log.UserLogger.Information("Reading Materials/Init.txt");
            MaterialDatabase = new Assets.MaterialDatabase();

            initPhase = "tiles";
            Log.UserLogger.Information("Reading Graphics/Init.txt...");
            TileDatabase = new Assets.TileDatabase();

            // init autotile catalog
            Autotiles = new AutotileCatalog();
            
            initPhase = "effects";
            Log.Information("Initializing effects database...");
            EffectsDatabase = new EffectsDatabase();

            initPhase = "light brushes";
            Log.UserLogger.Information("Reading light brushes...");
            LightBrushDatabase = new LightBrushDatabase();

            initPhase = "props";
            Log.UserLogger.Information("Reading Props/Init.txt");
            PropDatabase = new PropDatabase(TileDatabase);

            Log.UserLogger.Information("Asset initialization done!");
            Log.Information("----- ASSET INIT DONE! -----");
            
            try
            {
                LuaScripting.LuaInterface.Initialize(new APIGuiHost(), !Boot.Options.NoAutoloads);
            }
            catch (LuaScriptException e)
            {
                Exception actualException = e.IsNetException ? e.InnerException! : e;
                string? stackTrace = actualException.Data["Traceback"] as string;

                var displayMsg = "Rained could not start due to an error in a Lua script:\n\n" + actualException.Message;
                if (stackTrace is not null)
                {
                    displayMsg += "\n" + stackTrace;
                }

                Log.Error(displayMsg);
                Boot.DisplayError("Could not start", displayMsg);
                throw new RainEdStartupException();
            }
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

        if (TileDatabase.HasErrors || PropDatabase.HasErrors)
            InitErrorsWindow.IsWindowOpen = true;

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
        // defer this to next frame, because loading lightmap requires gpu stuff,
        // and at this point the first frame hasn't been set up yet, so opengl state
        // hadn't been initialized, so interfacing with opengl at this point results in
        // undefined behavior.
        var levelsToLoad = levelPaths.ToArray();
        DeferToNextFrame(() =>
        {
            foreach (var path in levelsToLoad)
            {
                Log.Information("Boot load " + path);
                LoadLevel(path);
            }
        });
        
        EditorWindow.RequestLoadEmergencySave();

        // force gc. i just added this to try to collect
        // the now-garbage asset image data, as they have
        // been converted into GPU textures. 
        GC.Collect(2, GCCollectionMode.Aggressive, true, true);
        GC.WaitForPendingFinalizers();

        // check on the update checker
        try
        {
            LatestVersionInfo = versionCheckTask.Result;

            if (LatestVersionInfo is not null)
            {
                Log.Information("Version check successful!");
                Log.Information(LatestVersionInfo.VersionName);
            }
            else
            {
                Log.Information("Version check was disabled");
            }
        }
        catch (Exception e)
        {
            Log.Error("Version check faulted...\n" + e);
        }

        Log.Information("Boot successful!");
        lastRopeUpdateTime = Raylib.GetTime();

        Boot.Window.KeyDown += (Glib.Key _, int _) =>
        {
            NeedScreenRefresh();
            keysPressed++;
        };

        Boot.Window.KeyUp += (Glib.Key _, int _) =>
        {
            NeedScreenRefresh();
            keysPressed--;
        };

        Boot.Window.MouseDown += (Glib.MouseButton _) =>
            NeedScreenRefresh();

        Boot.Window.MouseUp += (Glib.MouseButton _) =>
            NeedScreenRefresh();

        Boot.Window.MouseMove += (float x, float y) =>
            NeedScreenRefresh();
        
        Boot.Window.MouseScroll += (float dx, float dy) =>
            NeedScreenRefresh();
        
        Boot.Window.SilkWindow.FocusChanged += (bool focused) =>
        {
            if (focused)
            {
                NeedScreenRefresh();
            }
        };
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
        levelView?.SavePreferences(Preferences);
        Preferences.ViewKeyboardShortcuts = ShortcutsWindow.IsWindowOpen;
        Preferences.ShowPaletteWindow = PaletteWindow.IsWindowOpen;

        Preferences.WindowWidth = Raylib.GetScreenWidth();
        Preferences.WindowHeight = Raylib.GetScreenHeight();
        Preferences.WindowMaximized = Raylib.IsWindowMaximized();
        Preferences.DataPath = AssetDataPath;
        
        UserPreferences.SaveToFile(Preferences, prefFilePath);
        levelView?.Renderer.Dispose();
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
        var tab = new LevelTab();
        _tabs.Add(tab);

        SwitchTab(tab, true);
    }

    public void OpenLevel(Level level, string filePath = "")
    {
        var tab = new LevelTab(level, filePath);
        _tabs.Add(tab);
        SwitchTab(tab, true);
    }

    public LevelLoadResult LoadLevelThrow(string path, bool showLevelLoadFailPopup = true)
    {
        if (string.IsNullOrEmpty(path)) throw new ArgumentException("Path is empty!", nameof(path));
        Log.UserLogger.Information("Load level {Path}", Path.GetFileName(path));

        try
        {
            var loadRes = LevelSerialization.Load(path);

            if (!loadRes.HadUnrecognizedAssets || !showLevelLoadFailPopup)
            {
                var tab = new LevelTab(loadRes.Level, path);
                _tabs.Add(tab);
                Log.Information("Done!");

                SwitchTab(tab, true);
            }
            else
            {
                // level failed to load due to unrecognized assets
                LevelLoadFailedWindow.LoadResult = loadRes;
                LevelLoadFailedWindow.IsWindowOpen = true;
                LevelLoadFailedWindow.LoadAnywayCallback = () =>
                {
                    var tab = new LevelTab(loadRes.Level, path);
                    _tabs.Add(tab);
                    CurrentTab = tab;
                };
            }

            // i think it may be useful to add it to the list
            // even if the level failed to load due to unrecognized assets
            AddToRecentFiles(path);

            return loadRes;
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    public void LoadLevel(string path)
    {        
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                LoadLevelThrow(path);
            }
            catch (Exception e)
            {
                Log.UserLogger.Error("Error loading level {Path}:\n{ErrorMessage}", path, e);
                EditorWindow.ShowNotification("Error while loading level");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    /// <summary>
    /// Save the level to the given path.
    /// </summary>
    /// <param name="path"></param>
    public void SaveLevel(string path)
    {
        Log.Information("Saving level to {Path}...", path);
        IsLevelLocked = true;

        levelView.FlushDirty();

        try
        {
            string oldFilePath = CurrentTab!.FilePath;

            // store backup of level
            if (Preferences.SaveFileBackups)
            {
                var levelPath = path;
                var pngPath = Path.ChangeExtension(path, "png");

                if (File.Exists(levelPath))
                {
                    var backupFile = levelPath + ".1";
                    File.Delete(backupFile);
                    File.Move(levelPath, backupFile);
                }

                if (File.Exists(pngPath))
                {
                    var backupFile = pngPath + ".1";
                    File.Delete(backupFile);
                    File.Move(pngPath, backupFile);
                }
            }

            LevelSerialization.SaveLevelTextFile(Level, path);
            LevelSerialization.SaveLevelLightMap(Level, path);

            CurrentTab.FilePath = path;
            CurrentTab.Name = Path.GetFileNameWithoutExtension(path);

            UpdateTitle();

            Log.Information("Done!");
            CurrentTab.ChangeHistory.MarkUpToDate();
            EditorWindow.ShowNotification("Saved!");
            AddToRecentFiles(CurrentTab.FilePath);

            // if the old level was an emergency save and the user
            // saved it to a non-emergency save file, delete the
            // old file as it is no longer necessary.
            var oldParentFolder = Path.GetDirectoryName(oldFilePath)!;
            var newParentFolder = Path.GetDirectoryName(CurrentTab.FilePath);

            if (Util.ArePathsEquivalent(oldParentFolder, EmergencySaveFolder) && !Util.ArePathsEquivalent(newParentFolder, EmergencySaveFolder))
            {
                File.Delete(oldFilePath);
                File.Delete(Path.Combine(oldParentFolder, Path.GetFileName(oldFilePath)) + ".png");
            }

            IsLevelLocked = false;
        }
        catch (Exception e)
        {
            Log.Error("Could not write level file:\n{ErrorMessage}", e);
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

        foreach (var tab in Tabs)
        {
            var fileName = tab.FilePath;
            bool doSave = tab.ChangeHistory.HasChanges;

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "unnamed";
                doSave = true;
            }
            else
            {
                fileName = Path.GetFileNameWithoutExtension(fileName);
            }

            if (doSave)
            {
                var emSavFileName = Path.Combine(EmergencySaveFolder, $"{fileName}-{id}.txt");
                LevelSerialization.SaveLevelTextFile(tab.Level, emSavFileName);
                LevelSerialization.SaveLevelLightMap(tab.Level, emSavFileName);
            }
        }
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
                    Log.UserLogger.Warning("File trashing is not supported on this platform, resorted to permanent deletion.");
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

        while ((uint)list.Count > Preferences.MaxRecentFiles)
        {
            list.RemoveAt(0);
        }
    }

    public void ResizeLevel(int newWidth, int newHeight, int anchorX, int anchorY)
    {
        var level = Level;

        if (newWidth == level.Width && newHeight == level.Height) return;
        Log.Information("Resizing level...");
        IsLevelLocked = true;

        levelView!.FlushDirty();
        var dstOrigin = level.Resize(newWidth, newHeight, anchorX, anchorY);

        levelView.ReloadLevel();
        CurrentTab!.ChangeHistory.ForceMarkDirty();
        ChangeHistory.Clear();
        levelView.Renderer.ReloadLevel();
        levelView.ViewOffset += dstOrigin * Level.TileSize;

        Log.Information("Done!");
        IsLevelLocked = false;
    }

    private void SwitchTab(LevelTab? tab, bool newLevel)
    {
        if (tab == _currentTab) return;
        if (tab is not null && !_tabs.Contains(tab))
            throw new ArgumentException("Given LevelTab is not in Tabs list", nameof(tab));
        
        _currentTab = tab;
        if (_currentTab is not null)
        {
            var needInitLevelView = levelView is null;

            if (newLevel)
            {
                levelView ??= new LevelWindow();
                levelView.LevelCreated(_currentTab.Level);
            }

            levelView!.ChangeLevel(_currentTab.Level);
            levelView!.Renderer.ReloadLevel();
            if (needInitLevelView) levelView!.LoadView();
            UpdateTitle();
        }
    }

    public bool CloseTab(LevelTab tab)
    {
        levelView?.LevelClosed(tab.Level);
        tab.Dispose();
        return _tabs.Remove(tab);
    }

    private void UpdateTitle()
    {
        if (CurrentTab is not null)
        {
            string levelName = CurrentTab.Name;
            Raylib.SetWindowTitle($"Rained - {levelName}");
        }
        else
        {
            Raylib.SetWindowTitle("Rained");
        }
    }

    /// <summary>
    /// Register a command invokable by the user.
    /// </summary>
    /// <param name="name">The display name of the command.</param>
    /// <param name="cmd">The action to run on command.</param>
    public int RegisterCommand(CommandCreationParameters @params)
    {
        var cmd = new Command(@params);
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

    public Command GetCommand(int id)
    {
        for (int i = 0; i < customCommands.Count; i++)
        {
            if (customCommands[i].ID == id)
            {
                return customCommands[i];
            }
        }

        throw new ArgumentException("Unrecognized ID", nameof(id));
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

    private async void AsyncCloseWindowRequest()
    {
        if (await EditorWindow.CloseAllTabs())
        {
            Running = false; 
        }
    }
    
    public void Draw(float dt)
    {
        if (Raylib.WindowShouldClose())
            AsyncCloseWindowRequest();
        
        AssetGraphics.Maintenance();
        AssetGraphics.CleanUpTextures();
        
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
        
        Raylib.ClearBackground(new Color(51, 51, 51, 255));
        KeyShortcuts.Update();
        //ImGui.DockSpaceOverViewport();

        if (CurrentTab != null)
            UpdateRopeSimulation();
        
        // update node data
        CurrentTab?.NodeData.Update();
        
        EditorWindow.Render();
        LuaInterface.UIUpdate();
        LuaInterface.Update(dt);

        if (ImGui.IsKeyPressed(ImGuiKey.F1))
            DebugWindow.IsWindowOpen = !DebugWindow.IsWindowOpen;
        
        // don't sleep rained if mouse or key is held down
        // for example, the user may be holding down a +/- imgui input, and i'm not quite sure how to detect that.
        if (ImGui.IsMouseDown(ImGuiMouseButton.Left) || ImGui.IsMouseDown(ImGuiMouseButton.Right) || ImGui.IsMouseDown(ImGuiMouseButton.Middle) ||
            keysPressed > 0)
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
            Log.Debug("enter event driven");
        }
    }

    public void UpdateRopeSimulation()
    {
        var level = CurrentTab!.Level;
        const float TickRate = 30f;

        foreach (var prop in level.Props)
        {
            var rope = prop.Rope;
            if (rope is null) continue;

            rope.SimulationTimeStacker += Raylib.GetFrameTime() * TickRate * rope.SimulationSpeed;
            prop.TickRopeSimulation();
        }
    }
}