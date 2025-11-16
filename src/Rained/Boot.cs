#if !DEBUG
#define EXCEPTION_CATCHING
#endif

using Raylib_cs;
using ImGuiNET;
using System.Globalization;
using Glib;
using Rained.LuaScripting;

namespace Rained
{
    static class Boot
    {
        // find the location of the app data folder
#if DATA_ASSEMBLY
        public static string AppDataPath = AppContext.BaseDirectory;
        public static string ConfigPath = Path.Combine(AppDataPath, "config");
#elif DATA_APPDATA
        public static string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rained");
        public static string ConfigPath = Path.Combine(AppDataPath, "config");
#else
        public static string AppDataPath = Directory.GetCurrentDirectory();
        public static string ConfigPath = Path.Combine(AppDataPath, "config");
#endif

        public const int DefaultWindowWidth = 1200;
        public const int DefaultWindowHeight = 800;
        private static bool isAppReady = false;
        private static Glib.Window? splashScreenWindow = null;

        private static Glib.Window window = null!;
        public static Glib.Window Window => window;
        public static Glib.ImGui.ImGuiController? ImGuiController { get; private set; }


        private static BootOptions bootOptions = null!;
        public static BootOptions Options { get => bootOptions; }

        // window scale for dpi
        public static float WindowScale { get; set; } = 1.0f;
        
        // this is just the window scale, floored.
        public static int PixelIconScale { get; set; } = 1;

        private static int _refreshRate = 60;
        public static int RefreshRate {
            get => _refreshRate;
            set
            {
                _refreshRate = int.Max(1, value);
                // Raylib.SetTargetFPS(_refreshRate);
            }
        }

        public static int DefaultRefreshRate => window.SilkWindow.Monitor?.VideoMode.RefreshRate ?? 60;

        public readonly static CultureInfo UserCulture = Thread.CurrentThread.CurrentCulture;

        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            
            bootOptions = new BootOptions(args);
            if (!bootOptions.ContinueBoot)
                return;

            if (bootOptions.EffectExportOutput is not null)
            {
                LaunchDrizzleExport(bootOptions.EffectExportOutput);
            }
            else if (bootOptions.Render || bootOptions.Scripts.Count > 0)
            {
                LaunchBatch();
            }
            else
            {
                LaunchEditor();
            }
        }

        private static void LaunchDrizzleExport(string path)
        {
            DrizzleExport.DrizzleEffectExport.Export(Assets.AssetDataPath.GetPath(), Path.Combine(AppDataPath, "assets", "drizzle-cast"), path);
        }

        private static void LaunchBatch()
        {
            // setup serilog
            {
                bool logToStdout = bootOptions.ConsoleAttached || bootOptions.LogToStdout;
                #if DEBUG
                logToStdout = true;
                #endif

                Log.Setup(
                    logToStdout: false,
                    userLoggerToStdout: false
                );
            }

            if (bootOptions.Scripts.Count > 0 || !bootOptions.NoAutoloads)
            {
                var app = new APIBatchHost();
                LuaInterface.Initialize(app, !bootOptions.NoAutoloads);
                
                var lua = LuaInterface.LuaState;
                foreach (var path in bootOptions.Scripts)
                {
                    LuaHelpers.DoFile(lua, path);
                }
            }
            else
            {
                // this would have been loaded by APIBatchHost ctor
                Assets.DrizzleCast.Initialize();
            }

            if (bootOptions.Render)
                LaunchRenderer();
        }

        private static void LaunchRenderer()
        {
            if (bootOptions.Files.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("error: ");
                Console.ResetColor();

                Console.WriteLine("The level path(s) were not given");
                Environment.ExitCode = 2;
                return;
            }

            try
            {
                // single-file render
                if (bootOptions.Files.Count == 1 && !Directory.Exists(bootOptions.Files[0]))
                {
                    Log.Information("====== Standalone render ======");
                    Drizzle.DrizzleRender.ConsoleRender(bootOptions.Files[0]);
                }

                // mass render
                else
                {
                    Log.Information("====== Standalone mass render ======");
                    Drizzle.DrizzleMassRender.ConsoleRender([..bootOptions.Files], bootOptions.RenderThreads);
                }
            }
            catch (Drizzle.DrizzleRenderException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("error: ");
                Console.ResetColor();
                Console.WriteLine(e.Message);
                Environment.ExitCode = 1;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("error: ");
                Console.ResetColor();
                Console.WriteLine(e);
                Environment.ExitCode = 1;
            }
        }

        private static void LaunchEditor()
        {
            bool showAltSplashScreen = DateTime.Now.Month == 4 && DateTime.Now.Day == 1; // being a lil silly
            AppDataPath = Options.AppDataPath;
            
            if (bootOptions.ShowOgscule)
                showAltSplashScreen = true;

            // setup serilog
            {
                bool logToStdout = bootOptions.ConsoleAttached || bootOptions.LogToStdout;
                #if DEBUG
                logToStdout = true;
                #endif

                Log.Setup(
                    logToStdout: logToStdout,
                    userLoggerToStdout: false
                );
            }

            // setup window logger
            Glib.Window.ErrorCallback = (string msg) =>
            {
                Log.Error("[Window] " + msg);
            };

            // setup GL logger
            RenderContext.Log = (LogLevel logLevel, string msg) =>
            {
                if (logLevel == LogLevel.Debug)
                    Log.Information(msg);
                else if (logLevel == LogLevel.Information)
                    Log.Information("[GL] " + msg);
                else if (logLevel == LogLevel.Error)
                    Log.UserLogger.Error("[GL] " + msg);
            };

            // load user preferences (though, will be loaded again in the RainEd class)
            // this is for reading preferences that will be applied in the boot process
            UserPreferences? prefs = null;
            {
                var prefsFile = Path.Combine(AppDataPath, "config", "preferences.json");
                if (File.Exists(prefsFile))
                {
                    try
                    {
                        prefs = UserPreferences.LoadFromFile(prefsFile);
                    }
                    catch
                    {
                        Log.Error("Could not load " + prefsFile);
                    }
                }
            }

            {
                var windowOptions = new Glib.WindowOptions()
                {
                    Width = DefaultWindowWidth,
                    Height = DefaultWindowHeight,
                    Border = Glib.WindowBorder.Resizable,
                    Title = "Rained",
                    Visible = false,
                    IsEventDriven = false,
                    //GlDebugContext = true
                };

                window = new Glib.Window(windowOptions);
                RenderContext renderContext = null!;

                void ImGuiConfigure()
                {
                    WindowScale = window.ContentScale.Y;

                    var iniPath = Path.Combine(ConfigPath, "imgui.ini");
                    
                    // create default ini file if non-existent
                    if (!File.Exists(iniPath))
                        File.WriteAllText(iniPath, ImGuiDefaultIni);

                    ImGuiExt.SetIniFilename(Path.Combine(ConfigPath, "imgui.ini"));
                    ImGui.StyleColorsDark();

                    var io = ImGui.GetIO();
                    io.KeyRepeatDelay = 0.5f;
                    io.KeyRepeatRate = 0.03f;
                    io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
                    
                    try
                    {
                        unsafe
                        {
                            io.Fonts.NativePtr->FontBuilderIO = ImGuiNative.ImGuiFreeType_GetBuilderForFreeType();
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error("FreeType setup failed: {Exception}", e);
                    }
                    
                    Fonts.UpdateAvailableFonts();
                    Fonts.ReloadFonts(prefs?.FontSize ?? 13f);
                };

                window.Load += () =>
                {
                    /*if (bootOptions.GlDebug)
                    {
                        Log.Information("Initialize OpenGL debug context");
                        window.RenderContext!.SetupErrorCallback((string msg, DebugSeverity severity) =>
                        {
                            if (severity != DebugSeverity.Notification)
                            {
                                Log.Error("GL error ({severity}): {Error}", severity, msg);
                            }
                        });
                    }*/
                    renderContext = RenderContext.Init(window);
                    ImGuiController = new Glib.ImGui.ImGuiController(window, ImGuiConfigure);
                };

                window.Initialize();
                float curWindowScale = WindowScale;
                Raylib.InitWindow(window);

                // create splash screen window to display while editor is loading
                if (!bootOptions.NoSplashScreen)
                {
                    var winOptions = new Glib.WindowOptions()
                    {
                        Width = 523,
                        Height = 307,
                        Border = Glib.WindowBorder.Hidden,
                        Title = "Loading Rained..."
                    };

                    splashScreenWindow = new Glib.Window(winOptions);
                    splashScreenWindow.Initialize();

                    var splashScreenCtx = RenderContext.Init(splashScreenWindow);

                    //var rctx = splashScreenWindow.RenderContext!;
                    var texture = Glib.Texture.Load(Path.Combine(AppDataPath, "assets",showAltSplashScreen ? "splash-screen-alt.png":"splash-screen.png"));
                    var colorMask = Glib.Texture.Load(Path.Combine(AppDataPath, "assets","splash-screen-colormask.png"));

                    // get theme filepath
                    var themeName = prefs?.Theme ?? "Dark";
                    var themeFilePath = Path.Combine(AppDataPath, "config", "themes", themeName + ".jsonc");
                    if (!File.Exists(themeFilePath)) themeFilePath = Path.Combine(AppDataPath, "config", "themes", themeName + ".json");

                    // get accent color from theme
                    Glib.Color color;
                    try
                    {
                        var style = SerializableStyle.FromFile(themeFilePath);
                        var colorArr = style!.Colors["Button"];
                        color = new Glib.Color(colorArr[0], colorArr[1], colorArr[2], colorArr[3]);
                    }
                    catch
                    {
                        color = Glib.Color.FromRGBA(41, 74, 122);
                    }

                    // draw splash screen, affected by accent color
                    // accent color is color-mask texture
                    splashScreenCtx.Begin();
                    splashScreenCtx.Clear(Glib.Color.Black);
                    splashScreenCtx.DrawTexture(texture);

                    if (!showAltSplashScreen)
                    {
                        splashScreenCtx.DrawColor = color;
                        splashScreenCtx.DrawTexture(colorMask);
                    }

                    splashScreenCtx.DrawColor = Glib.Color.White;
                    splashScreenCtx.End();

                    splashScreenWindow.MakeCurrent();
                    splashScreenWindow.SwapBuffers();

                    texture.Dispose();
                    colorMask?.Dispose();
                    splashScreenCtx.Dispose();

                    renderContext.MakeCurrent();
                    window.MakeCurrent();
                }

                //Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.HiddenWindow | ConfigFlags.VSyncHint);
                //Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
                //Raylib.InitWindow(DefaultWindowWidth, DefaultWindowHeight, "Rained");
                Raylib.SetTargetFPS(0);
                //Raylib.SetExitKey(KeyboardKey.Null);

                {
                    var windowIcons = new Glib.Image[6];
                    windowIcons[0] = Glib.Image.FromFile(Path.Combine(AppDataPath, "assets", "icon16.png"));
                    windowIcons[1] = Glib.Image.FromFile(Path.Combine(AppDataPath, "assets", "icon24.png"));
                    windowIcons[2] = Glib.Image.FromFile(Path.Combine(AppDataPath, "assets", "icon32.png"));
                    windowIcons[3] = Glib.Image.FromFile(Path.Combine(AppDataPath, "assets", "icon48.png"));
                    windowIcons[4] = Glib.Image.FromFile(Path.Combine(AppDataPath, "assets", "icon128.png"));
                    windowIcons[5] = Glib.Image.FromFile(Path.Combine(AppDataPath, "assets", "icon256.png"));
                    window.SetIcon(windowIcons);

                    for (int i = 0; i < windowIcons.Length; i++)
                    {
                        windowIcons[i].Dispose();
                    }
                }

                string? assetDataPath = null;
                if (!File.Exists(Path.Combine(AppDataPath, "config", "preferences.json")))
                {
                    window.Visible = true;
                    CloseSplashScreenWindow();
                    
                    var appSetup = new AppSetup();
                    if (!appSetup.Start(out assetDataPath))
                    {
                        Raylib.CloseWindow();
                        return;
                    }
                }

                RainEd app;

                try
                {
                    app = new(assetDataPath, bootOptions.Files);
                }
                catch (RainEdStartupException)
                {
                    Environment.ExitCode = 1;
                    return;
                }
#if EXCEPTION_CATCHING
                catch (Exception e)
                {
                    NotifyError(e);
                    return;
                }
#endif
                Window.Visible = true;
                CloseSplashScreenWindow();

                isAppReady = true;
                prefs = null;
                
#if EXCEPTION_CATCHING
                try
#endif
                {
                    Fonts.SetFont(app.Preferences.Font);

                    // setup vsync state
                    Window.VSync = app.Preferences.Vsync;

                    // set initial target fps
                    if (Window.VSync || app.Preferences.RefreshRate == 0)
                    {
                        RefreshRate = DefaultRefreshRate;
                    }
                    else
                    {
                        RefreshRate = app.Preferences.RefreshRate;
                    }

                    while (app.Running)
                    {
                        // for some reason on MacOS, turning on vsync just... doesn't?
                        // I should probably just have framelimiting be able to work with vsync
                        // in place, since it's possible a user's drivers don't listen to vsync on/off requests,
                        // but... whatever.
                        if (Window.VSync && !OperatingSystem.IsMacOS())
                        {
                            Raylib.SetTargetFPS(0);
                        }
                        else
                        {
                            Raylib.SetTargetFPS(_refreshRate);
                        }

                        // update fonts if scale changed or reload was requested
                        var io = ImGui.GetIO();
                        if (WindowScale != curWindowScale || Fonts.FontReloadQueued)
                        {
                            Fonts.FontReloadQueued = false;
                            curWindowScale = WindowScale;
                            Fonts.ReloadFonts(app.Preferences.FontSize);
                            ImGuiController!.RecreateFontDeviceTexture();
                        }

                        Raylib.BeginDrawing();
                        PixelIconScale = (int) curWindowScale;

                        // save style sizes and scale to dpi before rendering
                        // restore it back to normal afterwards
                        // (this is so the style editor works)
                        unsafe
                        {
                            ImGuiExt.StoreStyle();
                            ImGui.GetStyle().ScaleAllSizes(curWindowScale);

                            ImGuiController!.Update(Raylib.GetFrameTime());
                            app.Draw(Raylib.GetFrameTime());

                            ImGuiController!.Render();
                            ImGuiExt.LoadStyle();
                        }
                        
                        Raylib.EndDrawing();
                    }

                    Log.Information("Shutting down Rained...");
                    app.Shutdown();
                }
#if EXCEPTION_CATCHING
                catch (Exception e)
                {
                    try
                    {
                        app.EmergencySave();
                    }
                    catch (Exception saveError)
                    {
                        Log.Error("Failed to make an emergency level save.\n{Message}", saveError);
                    }

                    NotifyError(e);
                }
#endif
                ImGuiController?.Dispose();
                Raylib.CloseWindow();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            Raylib.CloseWindow();
            Log.Close();
        }

        private static void CloseSplashScreenWindow()
        {
            if (splashScreenWindow is not null)
            {
                RenderContext.Instance!.RemoveWindow(splashScreenWindow);
                splashScreenWindow.Dispose();
                splashScreenWindow = null;
            }
        }

        public static void DisplayError(string windowTitle, string windowContents)
        {
            var success = Platform.DisplayError(windowTitle, windowContents);
            
            // user does not have a supported system of showing a dialog error box
            // so just show it in imgui.
            if (!success)
            {
                if (!isAppReady)
                {
                    window.Visible = true;
                    CloseSplashScreenWindow();
                }

                ImGui.StyleColorsDark();

                while (!Raylib.WindowShouldClose())
                {
                    Raylib.BeginDrawing();
                    Raylib.ClearBackground(Raylib_cs.Color.Black);

                    ImGuiController!.Update(Raylib.GetFrameTime());

                    var windowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings;

                    var viewport = ImGui.GetMainViewport();
                    ImGui.SetNextWindowPos(viewport.Pos);
                    ImGui.SetNextWindowSize(viewport.Size);

                    if (ImGui.Begin(windowTitle + "##ErrorWindow", windowFlags))
                    {
                        ImGui.PushTextWrapPos(ImGui.GetContentRegionAvail().X);
                        ImGui.TextWrapped(windowContents);
                        ImGui.PopTextWrapPos();
                    } ImGui.End();

                    ImGuiController!.Render();
                    Raylib.EndDrawing();
                }

                if (!isAppReady)
                {
                    Raylib.CloseWindow();
                    splashScreenWindow?.Close();
                }
            }
        }

        private static void NotifyError(Exception e)
        {
            Log.Fatal("FATAL EXCEPTION.\n{ErrorMessage}", e);

            Environment.ExitCode = 1;

            // show message box
            var windowTitle = "Fatal Exception";
            var windowContents = $"A fatal exception has occured:\n{e}\n\nMore information can be found in the logs. The application will now quit.";

            DisplayError(windowTitle, windowContents);
        }

        private static readonly string ImGuiDefaultIni =
        """
        [Window][Work area]
        Pos=0,22
        Size=1200,778
        Collapsed=0

        [Window][Debug##Default]
        Pos=60,60
        Size=400,400
        Collapsed=0

        [Window][Level]
        Pos=10,59
        Size=618,731
        Collapsed=0
        DockId=0x00000011,0

        [Window][Environment]
        Pos=986,59
        Size=204,731
        Collapsed=0
        DockId=0x00000002,0

        [Window][Shortcuts]
        Pos=20,433
        Size=427,256
        Collapsed=0

        [Window][Build]
        Pos=1054,59
        Size=136,731
        Collapsed=0
        DockId=0x00000004,0

        [Window][Tile Selector]
        Pos=630,59
        Size=560,443
        Collapsed=0
        DockId=0x00000015,0

        [Window][Brush]
        Pos=1618,492
        Size=290,489
        Collapsed=0
        DockId=0x0000000A,0

        [Window][###Light Catalog]
        Pos=1618,59
        Size=290,431
        Collapsed=0
        DockId=0x00000009,0

        [Window][Add Effect]
        Pos=1458,59
        Size=450,454
        Collapsed=0
        DockId=0x0000000D,0

        [Window][Active Effects]
        Pos=1458,515
        Size=450,237
        Collapsed=0
        DockId=0x0000000F,0

        [Window][Effect Options]
        Pos=1458,754
        Size=450,227
        Collapsed=0
        DockId=0x00000010,0

        [Window][Props]
        Pos=1371,59
        Size=537,569
        Collapsed=0
        DockId=0x00000013,0

        [Window][Prop Options]
        Pos=1371,630
        Size=537,351
        Collapsed=0
        DockId=0x00000014,0

        [Window][###TileGfxPreview]
        Pos=630,504
        Size=281,286
        Collapsed=0
        DockId=0x00000017,0

        [Window][###TileSpecPreview]
        Pos=913,504
        Size=277,286
        Collapsed=0
        DockId=0x00000018,0

        [Docking][Data]
        DockSpace               ID=0x172EB66B Window=0xD72479ED Pos=10,59 Size=1180,731 Split=X Selected=0x65657392
        DockNode              ID=0x00000003 Parent=0x172EB66B SizeRef=1758,922 Split=X
            DockNode            ID=0x00000001 Parent=0x00000003 SizeRef=1690,922 Split=X Selected=0x65657392
            DockNode          ID=0x00000005 Parent=0x00000001 SizeRef=1334,922 Split=X Selected=0x65657392
                DockNode        ID=0x00000007 Parent=0x00000005 SizeRef=1604,922 Split=X Selected=0x65657392
                DockNode      ID=0x0000000B Parent=0x00000007 SizeRef=1444,922 Split=X Selected=0x65657392
                    DockNode    ID=0x00000011 Parent=0x0000000B SizeRef=1357,922 CentralNode=1 Selected=0x65657392
                    DockNode    ID=0x00000012 Parent=0x0000000B SizeRef=537,922 Split=Y Selected=0x1A02B0A3
                    DockNode  ID=0x00000013 Parent=0x00000012 SizeRef=203,569 Selected=0x1A02B0A3
                    DockNode  ID=0x00000014 Parent=0x00000012 SizeRef=203,351 Selected=0x7A56252A
                DockNode      ID=0x0000000C Parent=0x00000007 SizeRef=450,922 Split=Y Selected=0xDCDF1A23
                    DockNode    ID=0x0000000D Parent=0x0000000C SizeRef=203,454 Selected=0xDCDF1A23
                    DockNode    ID=0x0000000E Parent=0x0000000C SizeRef=203,466 Split=Y Selected=0xC4692E8D
                    DockNode  ID=0x0000000F Parent=0x0000000E SizeRef=203,237 Selected=0xC4692E8D
                    DockNode  ID=0x00000010 Parent=0x0000000E SizeRef=203,227 Selected=0xECF6175A
                DockNode        ID=0x00000008 Parent=0x00000005 SizeRef=290,922 Split=Y Selected=0x56944108
                DockNode      ID=0x00000009 Parent=0x00000008 SizeRef=283,431 Selected=0x56944108
                DockNode      ID=0x0000000A Parent=0x00000008 SizeRef=283,489 Selected=0xCA3ECDE9
            DockNode          ID=0x00000006 Parent=0x00000001 SizeRef=560,922 Split=Y Selected=0x8B39F883
                DockNode        ID=0x00000015 Parent=0x00000006 SizeRef=560,443 Selected=0x8B39F883
                DockNode        ID=0x00000016 Parent=0x00000006 SizeRef=560,286 Split=X Selected=0x04D00AC7
                DockNode      ID=0x00000017 Parent=0x00000016 SizeRef=281,159 Selected=0x04D00AC7
                DockNode      ID=0x00000018 Parent=0x00000016 SizeRef=277,159 Selected=0x27771CA8
            DockNode            ID=0x00000002 Parent=0x00000003 SizeRef=204,922 Selected=0xD04E21C8
        DockNode              ID=0x00000004 Parent=0x172EB66B SizeRef=136,922 Selected=0x57F56F05
        """;
    }
}