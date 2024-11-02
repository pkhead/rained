#if !DEBUG
#define EXCEPTION_CATCHING
#endif

using Raylib_cs;
using ImGuiNET;
using System.Globalization;
using Glib;

namespace Rained
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
        public static Glib.ImGui.ImGuiController? ImGuiController { get; private set; }

        private static BootOptions bootOptions = null!;
        public static BootOptions Options { get => bootOptions; }

        // window scale for dpi
        public static float WindowScale { get; set; } = 1.0f;
        
        // this is just the window scale, floored.
        public static int PixelIconScale { get; set; } = 1;

        public readonly static CultureInfo UserCulture = Thread.CurrentThread.CurrentCulture;

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
                bool logToStdout = bootOptions.ConsoleAttached || bootOptions.LogToStdout;
                #if DEBUG
                logToStdout = true;
                #endif

                Log.Setup(logToStdout);
            }

            RenderContext.Log = (LogLevel logLevel, string msg) =>
            {
                if (logLevel == LogLevel.Debug)
                    Log.Information(msg);
                else if (logLevel == LogLevel.Information)
                    Log.Information("[GL] " + msg);
                else if (logLevel == LogLevel.Error)
                    Log.UserLogger.Error("[GL] " + msg);
            };

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

                    ImGuiExt.SetIniFilename(Path.Combine(AppDataPath, "config", "imgui.ini"));
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
                    Fonts.ReloadFonts();
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
                    UserPreferences? prefs = null;
                    {
                        var prefsFile = Path.Combine(AppDataPath, "config", "preferences.json");
                        if (File.Exists(prefsFile))
                        {
                            prefs = UserPreferences.LoadFromFile(prefsFile);
                        }
                    }

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

                    splashScreenCtx.Begin();
                    splashScreenCtx.Clear(Glib.Color.Black);
                    splashScreenCtx.DrawTexture(texture);
                    splashScreenCtx.End();

                    splashScreenWindow.MakeCurrent();
                    splashScreenWindow.SwapBuffers();

                    texture.Dispose();
                    splashScreenCtx.Dispose();

                    renderContext.MakeCurrent();
                    window.MakeCurrent();
                }

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
                CloseSplashScreenWindow();

                isAppReady = true;
                
#if EXCEPTION_CATCHING
                try
#endif
                {
                    Fonts.SetFont(app.Preferences.Font);

                    // set initial target fps
                    var refreshRate = window.SilkWindow.Monitor?.VideoMode.RefreshRate ?? 60;
                    if (app.Preferences.RefreshRate == 0)
                        app.Preferences.RefreshRate = refreshRate;
                    Raylib.SetTargetFPS(app.Preferences.RefreshRate);

                    while (app.Running)
                    {
                        // update fonts if scale changed
                        var io = ImGui.GetIO();
                        if (WindowScale != curWindowScale)
                        {
                            curWindowScale = WindowScale;
                            Fonts.ReloadFonts();
                            ImGuiController!.RecreateFontDeviceTexture();
                        }

                        Raylib.BeginDrawing();
                        PixelIconScale = (int) curWindowScale;

                        // save style sizes and scale to dpi before rendering
                        // restore it back to normal afterwards
                        // (this is so the style editor works)
                        unsafe
                        {
                            ImGuiStyle styleCopy = *ImGui.GetStyle().NativePtr;
                            ImGui.GetStyle().ScaleAllSizes(curWindowScale);

                            ImGuiController!.Update(Raylib.GetFrameTime());
                            app.Draw(Raylib.GetFrameTime());

                            ImGuiController!.Render();
                            *ImGui.GetStyle().NativePtr = styleCopy;
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
    }
}