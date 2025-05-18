using Raylib_cs;
using System.Numerics;
using ImGuiNET;
using NLua.Exceptions;
using System.Runtime.CompilerServices;
namespace Rained.EditorGui;

static class EditorWindow
{
    // input overrides for lmb because of alt + left click drag
    private static bool isLmbPanning = false;
    private static bool isLmbDown = false;
    private static bool isLmbClicked = false;
    private static bool isLmbReleased = false;
    private static bool isLmbDragging = false;

    public static bool IsPanning { get => isLmbPanning; set => isLmbPanning = value; }

    public static bool IsKeyDown(ImGuiKey key)
    {
        if (ImGui.GetIO().WantTextInput) return false;
        return ImGui.IsKeyDown(key);
    }

    public static bool IsKeyPressed(ImGuiKey key)
    {
        if (ImGui.GetIO().WantTextInput) return false;
        return ImGui.IsKeyPressed(key);
    }

    // need to use Raylib.IsKeyPressed instead of EditorWindow.IsKeyPressed
    // because i specifically disabled the Tab key in ImGui input handling
    public static bool IsTabPressed()
    {
        if (ImGui.GetIO().WantTextInput) return false;
        return Raylib.IsKeyPressed(KeyboardKey.Tab);
    }

    public static bool IsMouseClicked(ImGuiMouseButton button, bool repeat = false)
    {
        if (button == ImGuiMouseButton.Left) return isLmbClicked;
        if (button == ImGuiMouseButton.Right) return KeyShortcuts.Activated(KeyShortcut.RightMouse);
        return ImGui.IsMouseClicked(button, repeat);
    }

    public static bool IsMouseDown(ImGuiMouseButton button)
    {
        if (button == ImGuiMouseButton.Left) return isLmbDown;
        if (button == ImGuiMouseButton.Right) return KeyShortcuts.Active(KeyShortcut.RightMouse);
        return ImGui.IsMouseDown(button);
    }

    public static bool IsMouseDoubleClicked(ImGuiMouseButton button)
    {
        if (isLmbPanning) return false;
        return ImGui.IsMouseDoubleClicked(button);
    }

    public static bool IsMouseReleased(ImGuiMouseButton button)
    {
        if (button == ImGuiMouseButton.Left) return isLmbReleased;
        if (button == ImGuiMouseButton.Right) return KeyShortcuts.Deactivated(KeyShortcut.RightMouse);
        return ImGui.IsMouseReleased(button);
    }

    public static bool IsMouseDragging(ImGuiMouseButton button)
    {
        if (button == ImGuiMouseButton.Left) return isLmbDragging;
        return ImGui.IsMouseDragging(button);
    }

    private static FileBrowser? fileBrowser = null;

    private static string notification = "";
    private static float notificationTime = 0f;
    private static float notifFlash = 0f;
    private static int timerDelay = 10;

    private static bool homeTab = true;
    private static bool switchToHomeTab = false;

    private static DrizzleRenderWindow? drizzleRenderWindow = null;
    private static LevelResizeWindow? levelResizeWin = null;
    public static LevelResizeWindow? LevelResizeWindow { get => levelResizeWin; }

    public static void ShowNotification(string msg)
    {
        if (notification == "" || notificationTime != 3f)
        {
            notification = msg;
        }
        else
        {
            notification += "\n" + msg;
        }
        
        notificationTime = 3f;
        notifFlash = 0f;
    }

    private static bool promptUnsavedChanges;
    private static bool promptUnsavedChangesCancelable;
    private static readonly List<TaskCompletionSource<bool>> _tcsUnsavedChanges = [];

    public static bool PromptUnsavedChanges(LevelTab tab, Action<bool> callback, bool canCancel = true)
    {
        if (tab is null)
        {
            callback(true);
            return false;
        }
        
        var changeHistory = tab.ChangeHistory;
        promptUnsavedChangesCancelable = canCancel;

        async Task CallbackTask()
        {
            callback(await PromptUnsavedChanges(tab, canCancel));
        };

        if (changeHistory.HasChanges || (!canCancel && string.IsNullOrEmpty(tab.FilePath)))
        {
            _ = CallbackTask();
            return true;
        }
        else
        {
            callback(true);
            return false;
        }
    }

    public static bool PromptUnsavedChanges(Action<bool> callback, bool canCancel = true) =>
        PromptUnsavedChanges(RainEd.Instance.CurrentTab!, callback, canCancel);
    
    public static Task<bool> PromptUnsavedChanges(LevelTab tab, bool canCancel = true)
    {
        var changeHistory = tab.ChangeHistory;
        promptUnsavedChangesCancelable = canCancel;
        var tcs = new TaskCompletionSource<bool>();

        if (changeHistory.HasChanges || (!canCancel && string.IsNullOrEmpty(tab.FilePath)))
        {
            promptUnsavedChanges = true;
            _tcsUnsavedChanges.Add(tcs);
            return tcs.Task;
        }
        else
        {
            tcs.SetResult(true);
            return tcs.Task;
        }
    }

    private static void OpenLevelBrowser(FileBrowser.OpenMode openMode, Action<string[]> callback)
    {
        static bool levelCheck(string path, bool isRw)
        {
            return isRw;
        }

        var tab = RainEd.Instance.CurrentTab;
        fileBrowser = new FileBrowser(openMode, callback, (tab is null || tab.IsTemporaryFile) ? null : Path.GetDirectoryName(tab.FilePath));
        fileBrowser.AddFilterWithCallback("Level file", levelCheck, ".txt");
        fileBrowser.PreviewCallback = (string path, bool isRw) =>
        {
            if (isRw) return new BrowserLevelPreview(path);
            return null;
        };
    }

    private static void DrawMenuBar()
    {
        if (ImGui.BeginMainMenuBar())
        {
            var fileActive = RainEd.Instance.CurrentTab is not null;

            if (ImGui.BeginMenu("File"))
            {
                KeyShortcuts.ImGuiMenuItem(KeyShortcut.New, "New");
                KeyShortcuts.ImGuiMenuItem(KeyShortcut.Open, "Open");

                if (ImGui.BeginMenu("Open Recent"))
                {
                    RecentLevelsList(10);
                    ImGui.EndMenu();
                }

                KeyShortcuts.ImGuiMenuItem(KeyShortcut.Save, "Save", enabled: fileActive);
                KeyShortcuts.ImGuiMenuItem(KeyShortcut.SaveAs, "Save As...", enabled: fileActive);

                KeyShortcuts.ImGuiMenuItem(KeyShortcut.CloseFile, "Close", enabled: fileActive);
                KeyShortcuts.ImGuiMenuItem(KeyShortcut.CloseAllFiles, "Close All");

                ImGui.Separator();

                KeyShortcuts.ImGuiMenuItem(KeyShortcut.Render, "Render...", enabled: fileActive);
                KeyShortcuts.ImGuiMenuItem(KeyShortcut.ExportGeometry, "Export Geometry...", enabled: fileActive);
                if (ImGui.MenuItem("Mass Render..."))
                {
                    MassRenderWindow.OpenWindow();
                }

                ImGui.Separator();
                if (ImGui.MenuItem("Reload Scripts"))
                {
                    LuaScripting.LuaInterface.Unload();
                    
                    try
                    {
                        LuaScripting.LuaInterface.Initialize(new LuaScripting.APIGuiHost(), true);
                    }
                    catch (LuaScriptException e)
                    {
                        LuaScripting.LuaInterface.HandleException(e);
                    }
                }

                if (ImGui.MenuItem("Preferences"))
                {
                    PreferencesWindow.OpenWindow();
                }

                ImGui.Separator();
                if (ImGui.MenuItem("Quit", "Alt+F4"))
                {
                    PromptUnsavedChanges((bool ok) =>
                    {
                        if (ok) RainEd.Instance.Running = false;
                    });
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Edit"))
            {
                KeyShortcuts.ImGuiMenuItem(KeyShortcut.Undo, "Undo", enabled: fileActive);
                KeyShortcuts.ImGuiMenuItem(KeyShortcut.Redo, "Redo", enabled: fileActive);
                //ImGui.Separator();
                //ImGuiMenuItemShortcut(ShortcutID.Cut, "Cut");
                //ImGuiMenuItemShortcut(ShortcutID.Copy, "Copy");
                //ImGuiMenuItemShortcut(ShortcutID.Paste, "Paste");
                ImGui.Separator();

                if (ImGui.MenuItem("Resize Level...", enabled: fileActive))
                {
                    levelResizeWin = new LevelResizeWindow();
                }

                var customCommands = RainEd.Instance.CustomCommands;
                if (customCommands.Count > 0)
                {
                    ImGui.Separator();

                    if (ImGui.BeginMenu("Commands"))
                    {
                        foreach (RainEd.Command cmd in customCommands)
                        {
                            bool enabled = RainEd.Instance.CurrentTab?.Level is not null || !cmd.parameters.RequiresLevel;
                            if (ImGui.MenuItem(cmd.Name, enabled))
                            {
                                cmd.Callback(cmd.ID);
                            }
                        }

                        ImGui.EndMenu();
                    }
                }

                ImGui.Separator();
                if (fileActive)
                {
                    RainEd.Instance.LevelView.ShowEditMenu();
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("View"))
            {
                var prefs = RainEd.Instance.Preferences;

                KeyShortcuts.ImGuiMenuItem(KeyShortcut.ViewZoomIn, "Zoom In", enabled: fileActive);
                KeyShortcuts.ImGuiMenuItem(KeyShortcut.ViewZoomOut, "Zoom Out", enabled: fileActive);
                if (ImGui.MenuItem("Reset View", enabled: fileActive))
                {
                    RainEd.Instance.LevelView.ResetView();
                }

                ImGui.Separator();

                Rendering.LevelEditRender? renderer = null;
                if (RainEd.Instance.CurrentTab is not null)
                {
                    renderer = RainEd.Instance.LevelView.Renderer;
                }

                KeyShortcuts.ImGuiMenuItem(KeyShortcut.ToggleViewGrid, "Grid", prefs.ViewGrid);
                KeyShortcuts.ImGuiMenuItem(KeyShortcut.ToggleViewTiles, "Tiles", prefs.ViewTiles);
                KeyShortcuts.ImGuiMenuItem(KeyShortcut.ToggleViewProps, "Props", prefs.ViewProps);
                KeyShortcuts.ImGuiMenuItem(KeyShortcut.ToggleViewCameras, "Camera Borders", prefs.ViewCameras);
                KeyShortcuts.ImGuiMenuItem(KeyShortcut.ToggleViewGraphics, "Tile Graphics", prefs.ViewPreviews);

                if (ImGui.BeginMenu("Node Indices"))
                {
                    KeyShortcuts.ImGuiMenuItem(KeyShortcut.ToggleViewNodeIndices, "Show", prefs.ViewNodeIndices);
                    ImGui.Separator();

                    Span<string> flagNames = [
                        "Room Exits",
                        "Creature Dens",
                        "Region Transports",
                        "Side Exits",
                        "Sky Exits",
                        "Sea Exits",
                        "Hives",
                        "Garbage Holes",
                    ];

                    for (int i = 0; i < flagNames.Length; i++)
                    {
                        ref var flag = ref prefs.NodeViewFilter.Flags[i];
                        ImGui.MenuItem(flagNames[i], null, ref flag);
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.MenuItem("Obscured Beams", null, prefs.ViewObscuredBeams))
                {
                    prefs.ViewObscuredBeams = !prefs.ViewObscuredBeams;

                    if (renderer is not null)
                    {
                        renderer.InvalidateGeo(0);
                        renderer.InvalidateGeo(1);
                        renderer.InvalidateGeo(2);
                    }
                }

                if (ImGui.MenuItem("Tile Heads", null, prefs.ViewTileHeads))
                {
                    prefs.ViewTileHeads = !prefs.ViewTileHeads;
                }

                ImGui.Separator();

                if (ImGui.MenuItem("Keyboard Shortcuts", null, ShortcutsWindow.IsWindowOpen))
                {
                    ShortcutsWindow.IsWindowOpen = !ShortcutsWindow.IsWindowOpen;
                }

                if (ImGui.MenuItem("Logs", null, LogsWindow.IsWindowOpen))
                {
                    LogsWindow.IsWindowOpen = !LogsWindow.IsWindowOpen;
                }

                if (ImGui.MenuItem("Palettes", null, PaletteWindow.IsWindowOpen))
                {
                    PaletteWindow.IsWindowOpen = !PaletteWindow.IsWindowOpen;
                }

                if (ImGui.BeginMenu("Tile Preview"))
                {
                    var viewGfx = prefs.ViewTileGraphicPreview;
                    var viewSpecs = prefs.ViewTileSpecPreview;
                    var specsTooltip = prefs.ViewTileSpecsOnTooltip;

                    if (ImGui.MenuItem("Graphics", null, ref viewGfx))
                        prefs.ViewTileGraphicPreview = viewGfx;
                    
                    if (ImGui.MenuItem("Geometry", null, ref viewSpecs))
                        prefs.ViewTileSpecPreview = viewSpecs;
                    
                    if (ImGui.MenuItem("Tooltip Geometry", null, ref specsTooltip))
                        prefs.ViewTileSpecsOnTooltip = specsTooltip;
                    
                    ImGui.EndMenu();
                }

                if (ImGui.MenuItem("Home", !homeTab))
                {
                    homeTab = true;
                    switchToHomeTab = true;
                }
                ImGui.Separator();
                
                if (ImGui.MenuItem("Show Data Folder..."))
                    RainEd.Instance.ShowPathInSystemBrowser(RainEd.Instance.AssetDataPath, false);
                
                if (ImGui.MenuItem("Show Render Folder..."))
                    RainEd.Instance.ShowPathInSystemBrowser(Path.Combine(RainEd.Instance.AssetDataPath, "Levels"), false);
                
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Help"))
            {
                if (ImGui.MenuItem("Readme..."))
                {
                    Platform.OpenURL(Path.Combine(Boot.AppDataPath, "README.txt"));
                }

                if (ImGui.MenuItem("Manual..."))
                {
                    OpenManual();
                }

                if (ImGui.MenuItem("About..."))
                {
                    AboutWindow.IsWindowOpen = true;
                }
                
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }
    }

    private static void OpenLevelPrompt()
    {
        OpenLevelBrowser(FileBrowser.OpenMode.Read, static (paths) =>
        {
            if (paths.Length > 0) RainEd.Instance.LoadLevel(paths[0]);
        });
    }

    public delegate void AsyncSaveCallback(string? path, bool immediate);

    /// <summary>
    /// Attempts to save the current level to the optional overridePath parameter. If not specified,
    /// it will use the already-associated file path for the level. If that doesn't exist either,
    /// it will return false, open the GUI file browser, and run the given callback if the user
    /// submitted to or canceled the prompt.
    /// </summary>
    /// <param name="callback">The optional callback to run if the file browser was opened.</param>
    /// <param name="overridePath">The path to save the level to.</param>
    /// <returns>True if the level was able to be saved immediately, false if not.</returns>
    public static bool AsyncSave(AsyncSaveCallback? callback = null, string? overridePath = null)
    {
        if (RainEd.Instance.CurrentTab!.IsTemporaryFile && string.IsNullOrEmpty(overridePath))
        {
            OpenLevelBrowser(FileBrowser.OpenMode.Write, (paths) =>
            {
                if (paths.Length > 0)
                {
                    SaveLevelCallback(paths[0]);
                    callback?.Invoke(paths[0], false);
                }
                else
                {
                    callback?.Invoke(null, false);
                }
            });
            return false;
        }
        else
        {
            var path = overridePath ?? RainEd.Instance.CurrentFilePath;
            SaveLevelCallback(path);
            callback?.Invoke(path, true);
            return true;
        }
    }

    private static void HandleShortcuts()
    {
        if (RainEd.Instance.IsLevelLocked) return;
        
        var fileActive = RainEd.Instance.CurrentTab is not null;
        var prefs = RainEd.Instance.Preferences;

        if (KeyShortcuts.Activated(KeyShortcut.New))
        {
            NewLevelWindow.OpenWindow();
        }

        if (KeyShortcuts.Activated(KeyShortcut.Open))
        {
            OpenLevelPrompt();
        }

        if (KeyShortcuts.Activated(KeyShortcut.Save) && fileActive)
        {
            AsyncSave();
        }

        if (KeyShortcuts.Activated(KeyShortcut.SaveAs) && fileActive)
        {
            OpenLevelBrowser(FileBrowser.OpenMode.Write, static (paths) =>
            {
                if (paths.Length > 0) SaveLevelCallback(paths[0]);
            });
        }

        if (KeyShortcuts.Activated(KeyShortcut.CloseFile) && fileActive)
        {
            PromptUnsavedChanges((bool ok) =>
            {
                if (ok) RainEd.Instance.CloseTab(RainEd.Instance.CurrentTab!);
            });
        }

        if (KeyShortcuts.Activated(KeyShortcut.CloseAllFiles))
        {
            _ = CloseAllTabs();
        }

        if (KeyShortcuts.Activated(KeyShortcut.Undo) && fileActive)
        {
            RainEd.Instance.CurrentTab?.ChangeHistory.Undo();
        }

        if (KeyShortcuts.Activated(KeyShortcut.Redo) && fileActive)
        {
            RainEd.Instance.CurrentTab?.ChangeHistory.Redo();
        }

        if (KeyShortcuts.Activated(KeyShortcut.Render) && fileActive)
        {
            PromptUnsavedChanges((bool ok) =>
            {
                if (ok)
                {
                    drizzleRenderWindow = new DrizzleRenderWindow(false);
                }
            }, false);
        }

        if (KeyShortcuts.Activated(KeyShortcut.ExportGeometry) && fileActive)
        {
            PromptUnsavedChanges((bool ok) =>
            {
                if (ok) drizzleRenderWindow = new DrizzleRenderWindow(true);
            }, false);
        }

        if (KeyShortcuts.Activated(KeyShortcut.ToggleViewGrid))
        {
            prefs.ViewGrid = !prefs.ViewGrid;
        }

        if (KeyShortcuts.Activated(KeyShortcut.ToggleViewTiles))
        {
            prefs.ViewTiles = !prefs.ViewTiles;
        }

        if (KeyShortcuts.Activated(KeyShortcut.ToggleViewProps))
        {
            prefs.ViewProps = !prefs.ViewProps;
        }

        if (KeyShortcuts.Activated(KeyShortcut.ToggleViewCameras))
        {
            prefs.ViewCameras = !prefs.ViewCameras;
        }

        if (KeyShortcuts.Activated(KeyShortcut.ToggleViewGraphics))
        {
            prefs.ViewPreviews = !prefs.ViewPreviews;
        }

        if (KeyShortcuts.Activated(KeyShortcut.ToggleViewNodeIndices))
        {
            prefs.ViewNodeIndices = !prefs.ViewNodeIndices;
        }
    }

    private static void SaveLevelCallback(string path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            bool success;

            try
            {
                RainEd.Instance.SaveLevel(path);
                success = true;
            }
            catch
            {
                success = false;
            }

            if (success)
            {
                foreach (var t in _tcsUnsavedChanges.ToArray())
                {
                    _tcsUnsavedChanges.Remove(t);
                    t.SetResult(true);
                }
            }
        }
        else
        {
            _tcsUnsavedChanges.Clear();
        }
    }

    /// <summary>
    /// Show miscellaneous windows that may be docked.
    /// </summary>
    static void ShowMiscWindows()
    {
        ShortcutsWindow.ShowWindow();
        PaletteWindow.ShowWindow();
    }

    /// <summary>
    /// Only show miscellaneous windows that cannot be docked since
    /// they are either pop-ups or can be viewable without a level
    /// active.
    /// </summary>
    static void ShowMiscFloatingWindows()
    {
        AboutWindow.ShowWindow();
        LevelLoadFailedWindow.ShowWindow();
        PreferencesWindow.ShowWindow();
        EmergencySaveWindow.ShowWindow();
        NewLevelWindow.ShowWindow();
        MassRenderWindow.ShowWindow();
        InitErrorsWindow.ShowWindow();
        LogsWindow.ShowWindow();
    }

    /// <summary>
    /// Called on startup, and asks the user if
    /// they want to load the emergency save file if it exists.
    /// </summary>
    public static void RequestLoadEmergencySave()
    {
        var list = RainEd.DetectEmergencySaves();
        if (list.Length > 0)
        {
            EmergencySaveWindow.UpdateList(list);
            EmergencySaveWindow.IsWindowOpen = true;
        }
    }
    
    private static bool closeDrizzleRenderWindow = false;

    private static LevelTab? _prevTab = null;
    private static int _prevTabCount = 0;

    public static void Render()
    {
        DrawMenuBar();
        HandleShortcuts();
        LevelTab? tabToDelete = null;

        var workAreaFlags = ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoMove;
        
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().WorkPos);
        ImGui.SetNextWindowSize(ImGui.GetMainViewport().WorkSize);

        if (ImGui.Begin("Work area", workAreaFlags))
        {
            var dockspaceId = ImGui.GetID("Dockspace");
            
            if (ImGui.BeginTabBar("AAA", ImGuiTabBarFlags.DrawSelectedOverline | ImGuiTabBarFlags.Reorderable))
            {
                // if a tab switch was forced by outside code setting TabIndex
                bool tabChanged = _prevTab != RainEd.Instance.CurrentTab;
                var anyTabActive = false;

                // home tab
                if (homeTab)
                {
                    var tabFlags = ImGuiTabItemFlags.None;

                    if (switchToHomeTab)
                    {
                        tabFlags |= ImGuiTabItemFlags.SetSelected;
                        _prevTab = null;
                        switchToHomeTab = false;
                    }
                    
                    if (ImGui.BeginTabItem("Home", ref homeTab, tabFlags))
                    {
                        if (!tabChanged)
                        {
                            RainEd.Instance.CurrentTab = null;
                            _prevTab = null;
                        }

                        HomeTab();
                        ImGui.EndTabItem();
                    }
                }

                var tabIndex = 0;
                foreach (var tab in RainEd.Instance.Tabs.ToArray())
                {
                    var tabId = tab.Name + "###" + tabIndex;
                    var open = true;

                    // use SetSelected on relevant tab if a tab switch was forced
                    var tabFlags = ImGuiTabItemFlags.None;
                    
                    if (tabChanged && RainEd.Instance.CurrentTab == tab)
                    {
                        tabFlags |= ImGuiTabItemFlags.SetSelected;
                        _prevTab = tab;
                    }

                    if (tab.ChangeHistory.HasChanges)
                    {
                        tabFlags |= ImGuiTabItemFlags.UnsavedDocument;
                    }

                    if (ImGui.BeginTabItem(tabId, ref open, tabFlags))
                    {
                        if (!tabChanged)
                            RainEd.Instance.CurrentTab = tab;
                        anyTabActive = true;
                        _prevTab = tab;

                        ImGui.DockSpace(dockspaceId, new Vector2(0f, 0f));

                        //ImGui.PopStyleVar();
                        RainEd.Instance.LevelView.Render();
                        ShowMiscWindows();

                        //ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
                        ImGui.EndTabItem();
                    }

                    if (!open)
                        tabToDelete = tab;
                    
                    tabIndex++;
                }
                
                // imgui doesn't show a tab on the frame that it is deleted 
                // but it's still being processed or something Idk???
                // What the hell. Why.
                // anyway that's why i only set this to null given these conditions.
                if (!tabChanged && !anyTabActive && tabIndex == _prevTabCount)
                    RainEd.Instance.CurrentTab = null;
                _prevTabCount = tabIndex;

                ImGui.EndTabBar();
            }
        } ImGui.End();

        FileBrowser.Render(ref fileBrowser);
        
        // render drizzle render, if in progress
        // disposing of drizzle render window must be done on the next frame
        // otherwise the texture ID given to ImGui for the previee will be invalid
        // and it will spit out an opengl error. it's not a fatal error, it's just...
        // not supposed to happen.
        if (drizzleRenderWindow is not null)
        {
            RainEd.Instance.IsLevelLocked = true;
            RainEd.Instance.NeedScreenRefresh();

            if (closeDrizzleRenderWindow)
            {
                closeDrizzleRenderWindow = false;
                drizzleRenderWindow.Dispose();
                drizzleRenderWindow = null;
                RainEd.Instance.IsLevelLocked = false;

                // the whole render process allocates ~1 gb of memory
                // so, try to free all that
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                GC.WaitForFullGCComplete();
            }
            
            // if this returns true, the render window had closed
            else if (drizzleRenderWindow.DrawWindow())
            {
                closeDrizzleRenderWindow = true;
            }
        }

        // render level resize window
        if (levelResizeWin is not null)
        {
            levelResizeWin.DrawWindow();
            if (!levelResizeWin.IsWindowOpen) levelResizeWin = null;
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

            if (timerDelay == 0)
            {
                var dt = Raylib.GetFrameTime();
                notificationTime -= dt;
                notifFlash += dt;
            }
        }

        ShowMiscFloatingWindows();

        // prompt unsaved changes
        if (promptUnsavedChanges)
        {
            promptUnsavedChanges = false;
            ImGui.OpenPopup("Unsaved Changes");

            // center popup 
            ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        }

        bool unused = true;
        if (ImGui.BeginPopupModal("Unsaved Changes", ref unused, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            if (promptUnsavedChangesCancelable)
            {
                ImGui.Text("Do you want to save your changes before proceeding?");
            }
            else
            {
                ImGui.Text("You must save before proceeding.\nDo you want to save now?");
            }

            ImGui.Separator();

            if (ImGui.Button("Yes", StandardPopupButtons.ButtonSize) || ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.Space))
            {
                ImGui.CloseCurrentPopup();

                // unsaved change callback is run in SaveLevel
                if (string.IsNullOrEmpty(RainEd.Instance.CurrentFilePath))
                    OpenLevelBrowser(FileBrowser.OpenMode.Write, static (paths) =>
                    {
                        if (paths.Length > 0) SaveLevelCallback(paths[0]);
                    });
                else
                    SaveLevelCallback(RainEd.Instance.CurrentFilePath);
            }

            ImGui.SameLine();
            if (ImGui.Button("No", StandardPopupButtons.ButtonSize) || (!promptUnsavedChangesCancelable && ImGui.IsKeyPressed(ImGuiKey.Escape)))
            {
                ImGui.CloseCurrentPopup();

                if (promptUnsavedChangesCancelable)
                {
                    foreach (var t in _tcsUnsavedChanges.ToArray())
                    {
                        _tcsUnsavedChanges.Remove(t);
                        t.SetResult(true);
                    }
                }
                else
                {
                    _tcsUnsavedChanges.Clear();
                }
            }

            if (promptUnsavedChangesCancelable)
            {
                ImGui.SameLine();
                if (ImGui.Button("Cancel", StandardPopupButtons.ButtonSize) || ImGui.IsKeyPressed(ImGuiKey.Escape))
                {
                    ImGui.CloseCurrentPopup();

                    foreach (var t in _tcsUnsavedChanges.ToArray())
                    {
                        _tcsUnsavedChanges.Remove(t);
                        t.SetResult(false);
                    }
                }
            }

            ImGui.EndPopup();
        }

        if (timerDelay > 0)
            timerDelay--;
        
        // deferred tab deletion
        if (tabToDelete is not null)
            RainEd.Instance.DeferToNextFrame(() =>
            {
                var oldTab = RainEd.Instance.CurrentTab;
                
                bool didPrompt = PromptUnsavedChanges(tabToDelete, (bool ok) =>
                {
                    RainEd.Instance.CurrentTab = oldTab;
                    if (ok) RainEd.Instance.CloseTab(tabToDelete);
                });
                
                // i want it to switch to the tab that is being deleted
                // when it shows the prompt, then switch back to the previous tab
                // when done.
                if (didPrompt)
                    RainEd.Instance.CurrentTab = tabToDelete;
            });
    }

    public static void UpdateMouseState()
    {
        bool wasLmbDown = isLmbDown;
        isLmbClicked = false;
        isLmbDown = false;
        isLmbReleased = false;
        isLmbDragging = false;

        if (!isLmbPanning)
        {
            isLmbDown = ImGui.IsMouseDown(ImGuiMouseButton.Left);
            isLmbDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left);

            // manually set Clicked or Released bools based on lmbdown state changes
            // this is so it registers that the mouse was released when ther user alt+tabs out of the window
            if (!wasLmbDown && isLmbDown)
                isLmbClicked = true;

            if (wasLmbDown && !isLmbDown)
                isLmbReleased = true;
        }
    }

    public static async Task<bool> CloseAllTabs()
    {
        LevelTab[] tabs = [..RainEd.Instance.Tabs];

        foreach (var tab in tabs)
        {
            if (tab.ChangeHistory.HasChanges || string.IsNullOrEmpty(tab.FilePath))
                RainEd.Instance.CurrentTab = tab;

            if (await PromptUnsavedChanges(tab))
            {
                RainEd.Instance.CloseTab(tab);
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    public static void HomeTab()
    {
        var childSize = new Vector2(RainedLogo.Width, RainedLogo.Height - 100f + ImGui.GetFrameHeight() * 16f);
        ImGui.SetCursorPos((ImGui.GetWindowSize() - childSize) / 2f);
        ImGui.BeginChild("Contents", childSize);

        var newVersion = RainEd.Instance.LatestVersionInfo is not null && RainEd.Instance.LatestVersionInfo.VersionName != RainEd.Version;

        RainedLogo.Draw();

        ImGui.SetCursorPosY(RainedLogo.Height - 100f);
        var btnSize = new Vector2(-0.00001f, 0f);

        if (ImGui.Button("New Level...", btnSize))
            NewLevelWindow.OpenWindow();
        
        if (ImGui.Button("Open Level...", btnSize))
            OpenLevelPrompt();
        
        if (ImGui.Button("Manual...", btnSize))
            OpenManual();

        // recent levels list
        ImGui.Text("Recent Levels");
        var listBoxSize = ImGui.GetContentRegionAvail();
        // if new version was found, make space for the text
        if (newVersion)
            listBoxSize.Y -= ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemSpacing.Y + 1;
        
        if (ImGui.BeginListBox("##RecentLevels", listBoxSize))
        {
            RecentLevelsList();
            ImGui.EndListBox();
        }

        // show new version
        if (newVersion)
        {
            ImGui.Text("New version available!");
            ImGui.SameLine();
            ImGuiExt.LinkText(RainEd.Instance.LatestVersionInfo!.VersionName, RainEd.Instance.LatestVersionInfo.GitHubReleaseUrl);
        }

        ImGui.EndChild();
    }

    private static void RecentLevelsList(int count = int.MaxValue)
    {
        var recentFiles = RainEd.Instance.Preferences.RecentFiles;

        if (recentFiles.Count == 0)
        {
            ImGui.MenuItem("(no recent files)", false);
        }
        else
        {
            // display 10 entries
            for (int n = 0; n < count; n++)
            {
                var i = recentFiles.Count - (n+1);
                if (i < 0) break;

                var filePath = recentFiles[i];

                if (ImGui.MenuItem(Path.GetFileName(filePath)))
                {
                    if (File.Exists(filePath))
                    {
                        RainEd.Instance.LoadLevel(filePath);
                    }
                    else
                    {
                        ShowNotification("File could not be accessed");
                        recentFiles.RemoveAt(i);
                    }
                }

            }
        }
    }

    private static void OpenManual()
    {
        #if DEBUG
        var docPath = Path.Combine("dist", "docs", "en", "index.html");
        #else
        var docPath = Path.Combine(Boot.AppDataPath, "docs", "en", "index.html");
        #endif

        if (File.Exists(docPath))
        {
            Platform.OpenURL(docPath);
        }
        else
        {
            ShowNotification("Could not open documentation.");
        }
    }
}