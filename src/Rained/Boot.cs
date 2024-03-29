using Raylib_cs;
using rlImGui_cs;
using SFML.Window;
using SFML.Graphics;
using ImGuiNET;
using System.Runtime.InteropServices;
using System.Globalization;

namespace RainEd
{
    partial class Boot
    {
        // find the location of the app data folder
#if DATA_ASSEMBLY
        public static string AppDataPath = AppContext.BaseDirectory;
#elif DATA_APPDATA
        public static string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rained");
#else
        public static string AppDataPath = Directory.GetCurrentDirectory();
#endif

        // import win32 MessageBox function
        [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

        [LibraryImport("kernel32.dll")]
        private static partial int AttachConsole(int dwProcessId);

        public const int DefaultWindowWidth = 1200;
        public const int DefaultWindowHeight = 800;
        private static bool isAppReady = false;
        private static RenderWindow? splashScreenWindow = null;

        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            if (OperatingSystem.IsWindows())
            {
                if (AttachConsole(-1) != 0)
                {
                    var cin = new StreamReader(Console.OpenStandardInput());
                    var cerr = new StreamWriter(Console.OpenStandardError())
                    {
                        AutoFlush = true
                    };
                    var cout = new StreamWriter(Console.OpenStandardOutput())
                    {
                        AutoFlush = true
                    };

                    Console.SetOut(cout);
                    Console.SetIn(cin);
                    Console.SetError(cerr);
                }
            }
            
            // parse command arguments
            bool showSplashScreen = true;
            bool showAltSplashScreen = DateTime.Now.Month == 4 && DateTime.Now.Day == 1; // being a lil silly
            string levelToLoad = string.Empty;

            if (args.Length == 1)
            {
                if (args[0] == "--help" || args[0] == "-h")
                {
                    Console.WriteLine(
                    $"""
                    Rained {RainEd.Version}

                    Usage:
                        Rained [-v | --version]
                        Rained [-h | --help]
                        Rained [--no-splash-screen] [--app-data <path>] [--ogscule] [<level path>]
                        
                    --version -v            Print out version
                    --help                  Show this help menu
                    --no-splash-screen      Do not show the splash screen when starting
                    --app-data <path>       Run with app data directory at <path>
                    --ogscule               the intrusive thoughts defeated me
                    <level path>            The path of the level to load
                    """
                    );

                    return;
                }
                else if (args[0] == "--version" || args[0] == "-v")
                {
                    Console.WriteLine($"Rained {RainEd.Version}");
                    return;
                }
            }

            for (int i = 0; i < args.Length; i++)
            {
                var str = args[i];

                // this is here because it appears SFML uses some
                // OpenGL extensions that RenderDoc doesn't support
                if (str == "--no-splash-screen")
                {
                    showSplashScreen = false;
                    continue;
                }

                // runtime-configurable app data path because why not
                if (str == "--app-data")
                {
                    i++;
                    AppDataPath = args[i];
                    continue;
                }

                // the intrusive thoughts defeated me
                if (str == "--ogscule")
                {
                    Console.WriteLine("ogscule");
                    showAltSplashScreen = true;
                    continue;
                }

                levelToLoad = str;
            }
            
            // create splash screen window using sfml
            // to display while editor is loading
            // funny how i have to use two separate window/graphics libraries
            // cus raylib doesn't have multi-window support
            // I was originally going to use only SFML, but it didn't have any
            // good C# ImGui integration libraries. (Raylib did, though)
            if (showSplashScreen)
            {
                splashScreenWindow = new RenderWindow(new VideoMode(523, 307), "Loading Rained...", Styles.None);
                Texture texture = new(Path.Combine(AppDataPath, "assets",showAltSplashScreen ? "splash-screen-alt.png":"splash-screen.png"));
                var sprite = new Sprite(texture);

                splashScreenWindow.Clear();
                splashScreenWindow.Draw(sprite);
                splashScreenWindow.Display();
            }

            {
                Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.HiddenWindow | ConfigFlags.VSyncHint);
                Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
                Raylib.InitWindow(DefaultWindowWidth, DefaultWindowHeight, "Rained");
                Raylib.SetTargetFPS(240);
                Raylib.SetExitKey(KeyboardKey.Null);

                // setup imgui
                rlImGui.Setup(true, true);
                rlImGui.SetIniFilename(Path.Combine(AppDataPath,"imgui.ini"));
                ImGui.GetIO().KeyRepeatDelay = 0.5f;
                ImGui.GetIO().KeyRepeatRate = 0.03f;

                string? assetDataPath = null;
                if (!File.Exists(Path.Combine(AppDataPath, "preferences.json")))
                {
                    Raylib.ClearWindowState(ConfigFlags.HiddenWindow);
                    splashScreenWindow?.SetVisible(false);

                    var appSetup = new AppSetup();
                    if (!appSetup.Start(out assetDataPath))
                    {
                        rlImGui.Shutdown();
                        Raylib.CloseWindow();
                        splashScreenWindow?.Close();
                        return;
                    }
                }

                RainEd app;

                try
                {
                    app = new(assetDataPath, levelToLoad);
                }
                catch (RainEdStartupException)
                {
                    Environment.ExitCode = 1;
                    return;
                }
#if !DEBUG
                catch (Exception e)
                {
                    NotifyError(e);
                    return;
                }
#endif

                Raylib.ClearWindowState(ConfigFlags.HiddenWindow);
                
                // for some reason, closing the window bugs
                // out raylib, so i just set it invisible
                // and close it when the program ends
                splashScreenWindow?.SetVisible(false);
                isAppReady = true;
                
#if !DEBUG
                try
#endif
                {
                    while (app.Running)
                    {
                        Raylib.BeginDrawing();
                        app.Draw(Raylib.GetFrameTime());
                        
                        Raylib.EndDrawing();

                        RlManaged.RlObject.UnloadGCQueue();
                    }

                    RainEd.Logger.Information("Shutting down Rained...");
                    app.Shutdown();
                }
#if !DEBUG
                catch (Exception e)
                {
                    NotifyError(e);
                }
#endif

                rlImGui.Shutdown();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            RlManaged.RlObject.UnloadGCQueue();
            
            Raylib.CloseWindow();
            splashScreenWindow?.Close();
        }

        public static void DisplayError(string windowTitle, string windowContents)
        {
            if (OperatingSystem.IsWindows())
            {
                MessageBoxW(new IntPtr(0), windowContents, windowTitle, 0x10);
            }
            else
            {
                if (isAppReady)
                {
                    while (!Raylib.WindowShouldClose())
                    {
                        Raylib.BeginDrawing();
                        Raylib.ClearBackground(new Raylib_cs.Color(0, 0, 255, 255));
                        Raylib.DrawText(windowContents, 20, 20, 20, Raylib_cs.Color.White);
                        Raylib.EndDrawing();
                    }
                }
                else
                {
                    Raylib.ClearWindowState(ConfigFlags.HiddenWindow);
                    splashScreenWindow?.SetVisible(false);

                    while (!Raylib.WindowShouldClose())
                    {
                        Raylib.BeginDrawing();
                        Raylib.ClearBackground(new Raylib_cs.Color(0, 0, 255, 255));
                        Raylib.DrawText(windowContents, 20, 20, 20, Raylib_cs.Color.White);
                        Raylib.EndDrawing();
                    }

                    Raylib.CloseWindow();
                    splashScreenWindow?.Close();
                }
            }
        }

        private static void NotifyError(Exception e)
        {
            if (RainEd.Instance is not null)
            {
                RainEd.Logger.Error("FATAL EXCEPTION.\n{ErrorMessage}", e);
            }

            Environment.ExitCode = 1;

            // show message box
            var windowTitle = "Fatal Exception";
            var windowContents = $"A fatal exception has occured:\n{e}\n\nThe application will now quit.";

            DisplayError(windowTitle, windowContents);
        }
    }
}