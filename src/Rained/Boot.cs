#if !DEBUG
#define EXCEPTION_CATCHING
#endif

using Raylib_cs;
using ImGuiNET;
using System.Globalization;
using Serilog;
using Glib;

namespace RainEd
{
    static class Boot
    {
        // find the location of the app data folder
#if DATA_ASSEMBLY
        public static string AppDataPath = AppContext.BaseDirectory;
#elif DATA_APPDATA
        public static string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rained");
#else
        public static string AppDataPath = Directory.GetCurrentDirectory();
#endif

        public const int DefaultWindowWidth = 1200;
        public const int DefaultWindowHeight = 800;
        private static bool isAppReady = false;
        private static Glib.Window? splashScreenWindow = null;

        private static Glib.Window window = null!;
        public static Glib.Window Window => window;

        private static BootOptions bootOptions = null!;
        public static BootOptions Options { get => bootOptions; }

        // window scale for dpi
        public static float WindowScale { get; set; } = 1.0f;
        public readonly static CultureInfo UserCulture = Thread.CurrentThread.CurrentCulture;

        private static Serilog.Core.Logger? _logger = null;

        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            
            bootOptions = new BootOptions(args);
            if (!bootOptions.ContinueBoot)
                return;

            if (bootOptions.Render)
                LaunchRenderer();
            else
                LaunchEditor();
        }

        private static void LaunchRenderer()
        {
            if (string.IsNullOrEmpty(bootOptions.LevelToLoad))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("error: ");
                Console.ResetColor();

                Console.WriteLine("The level path was not given");
                Environment.ExitCode = 2;
                return;
            }

            try
            {
                Drizzle.DrizzleRender.Render(bootOptions.LevelToLoad);
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
                Directory.CreateDirectory(Path.Combine(AppDataPath, "logs"));

                bool logToStdout = bootOptions.ConsoleAttached || bootOptions.LogToStdout;
                #if DEBUG
                logToStdout = true;
                #endif

                var logLatest = Path.Combine(AppDataPath, "logs", "latest.log.txt");
                if (File.Exists(logLatest))
                    File.Delete(logLatest);

                var loggerConfig = new LoggerConfiguration()
                #if DEBUG
                .MinimumLevel.Debug()
                #endif
                .WriteTo.File(
                    Path.Combine(AppDataPath, "logs", "log.txt"),
                    rollingInterval: RollingInterval.Hour,
                    retainedFileCountLimit: 10
                )
                .WriteTo.File(logLatest, retainedFileCountLimit: 1);

                if (logToStdout)
                    loggerConfig = loggerConfig.WriteTo.Console();

                _logger = loggerConfig.CreateLogger();
                Serilog.Log.Logger = _logger;
            }
            
            // create splash screen window to display while editor is loading
            if (!bootOptions.NoSplashScreen)
            {
                var winOptions = new Glib.WindowOptions()
                {
                    Width = 523,
                    Height = 307,
                    Border = Glib.WindowBorder.Hidden,
                    Title = "Loading Rained...",
                    VSync = false
                };

                winOptions.API.Version = new Silk.NET.Windowing.APIVersion(3, 3);
                winOptions.API.Profile = Silk.NET.Windowing.ContextProfile.Core;

                if (bootOptions.GlDebug)
                {
                    winOptions.API.Flags |= Silk.NET.Windowing.ContextFlags.Debug;
                    winOptions.SetupGlErrorCallback = true;
                }

                splashScreenWindow = new Glib.Window(winOptions);
                splashScreenWindow.Initialize();

                var rctx = splashScreenWindow.RenderContext!;
                var texture = rctx.LoadTexture(Path.Combine(AppDataPath, "assets",showAltSplashScreen ? "splash-screen-alt.png":"splash-screen.png"));

                splashScreenWindow.BeginRender();

                rctx.Clear(Glib.Color.Black);
                rctx.Draw(texture);
                
                splashScreenWindow.EndRender();
                splashScreenWindow.SwapBuffers();
            }

            {
                var windowOptions = new Glib.WindowOptions()
                {
                    Width = DefaultWindowWidth,
                    Height = DefaultWindowHeight,
                    Border = Glib.WindowBorder.Resizable,
                    Title = "Rained",
                    Visible = false,
                    VSync = true,
                    SetupImGui = true
                };

                windowOptions.API.Version = new Silk.NET.Windowing.APIVersion(3, 3);
                windowOptions.API.Profile = Silk.NET.Windowing.ContextProfile.Core;

                if (bootOptions.GlDebug)
                {
                    windowOptions.API.Flags |= Silk.NET.Windowing.ContextFlags.Debug;
                    windowOptions.SetupGlErrorCallback = true;
                }

                // get available fonts for imgui
                window = new Glib.Window(windowOptions);
                
                window.ImGuiConfigure += () =>
                {
                    WindowScale = window.ContentScale.Y;

                    ImGuiExt.SetIniFilename(Path.Combine(AppDataPath, "config", "imgui.ini"));
                    ImGui.StyleColorsDark();

                    var io = ImGui.GetIO();
                    io.KeyRepeatDelay = 0.5f;
                    io.KeyRepeatRate = 0.03f;
                    io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
                    io.Fonts.FontBuilderFlags = (uint) (ImGuiFreeTypeBuilderFlags.MonoHinting);

                    // this is the easiest way i figured to access preferences.json before
                    // RainEd initialization, but it does result in preferences.json being
                    // loaded twice 
                    var prefsFile = Path.Combine(AppDataPath, "config", "preferences.json");
                    if (File.Exists(prefsFile))
                    {
                        var prefs = UserPreferences.LoadFromFile(prefsFile);
                        if (prefs.ImGuiMultiViewport)
                        {
                            io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
                        }
                    }

                    Fonts.UpdateAvailableFonts();
                    Fonts.ReloadFonts();
                };

                window.Load += () =>
                {
                    if (bootOptions.GlDebug)
                    {
                        Log.Information("Initialize OpenGL debug context");
                        window.RenderContext!.SetupErrorCallback((string msg, DebugSeverity severity) =>
                        {
                            if (severity != DebugSeverity.Notification)
                            {
                                Log.Error("GL error ({severity}): {Error}", severity, msg);
                            }
                        });
                    }
                };

                window.Initialize();
                float curWindowScale = WindowScale;
                Raylib.InitWindow(window);

                //Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.HiddenWindow | ConfigFlags.VSyncHint);
                //Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
                //Raylib.InitWindow(DefaultWindowWidth, DefaultWindowHeight, "Rained");
                //Raylib.SetTargetFPS(240);
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
                    if (splashScreenWindow is not null) splashScreenWindow.Visible = false;
                    
                    var appSetup = new AppSetup();
                    if (!appSetup.Start(out assetDataPath))
                    {
                        window.Dispose();
                        return;
                    }
                }

                RainEd app;

                try
                {
                    app = new(assetDataPath, bootOptions.LevelToLoad);
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
                if (splashScreenWindow is not null) splashScreenWindow.Visible = false;

                isAppReady = true;
                
#if EXCEPTION_CATCHING
                try
#endif
                {
                    Fonts.SetFont(app.Preferences.Font);

                    while (app.Running)
                    {
                        // update fonts if scale changed
                        var io = ImGui.GetIO();
                        if (WindowScale != curWindowScale)
                        {
                            curWindowScale = WindowScale;
                            Fonts.ReloadFonts();
                            window.ImGuiController!.RecreateFontDeviceTexture();
                        }

                        Raylib.BeginDrawing();
                        window.ImGuiController!.Update(Raylib.GetFrameTime());
                        app.Draw(Raylib.GetFrameTime());

                        // save style sizes and scale to dpi before rendering
                        // restore it back to normal afterwards
                        // (this is so the style editor works)
                        unsafe
                        {
                            ImGuiStyle styleCopy = *ImGui.GetStyle().NativePtr;
                            ImGui.GetStyle().ScaleAllSizes(curWindowScale);
                            window.ImGuiController!.Render();
                            *ImGui.GetStyle().NativePtr = styleCopy;
                        }
                        
                        
                        Raylib.EndDrawing();

                        Glib.GLResource.UnloadGCQueue();
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
                window.Dispose();
                //rlImGui.Shutdown();

                //foreach (var img in windowIcons)
                //    Raylib.UnloadImage(img);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            //RlManaged.RlObject.UnloadGCQueue();
            
            //Raylib.CloseWindow();
            splashScreenWindow?.Close();
            _logger.Dispose();
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
                    if (splashScreenWindow is not null) splashScreenWindow.Visible = false;
                }

                ImGui.StyleColorsDark();

                while (!Raylib.WindowShouldClose())
                {
                    Raylib.BeginDrawing();
                    Raylib.ClearBackground(Raylib_cs.Color.Black);

                    window.ImGuiController!.Update(Raylib.GetFrameTime());

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

                    window.ImGuiController!.Render();
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
            var windowContents = $"A fatal exception has occured:\n{e}\n\nThe application will now quit.";

            DisplayError(windowTitle, windowContents);
        }
    }
}