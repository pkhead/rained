using Raylib_cs;
using rlImGui_cs;

using SFML.Window;
using SFML.Graphics;
using ImGuiNET;

namespace RainEd
{
    class Boot
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

        static void Main(string[] args)
        {
            // parse command arguments
            bool showSplashScreen = true;
            bool showAltSplashScreen = false;
            string levelToLoad = string.Empty;

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
            
            RenderWindow? splashScreenWindow = null;

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

                RainEd app = new(levelToLoad);
                Raylib.ClearWindowState(ConfigFlags.HiddenWindow);
                
                // for some reason, closing the window bugs
                // out raylib, so i just set it invisible
                // and close it when the program ends
                splashScreenWindow?.SetVisible(false);
                
                while (app.Running)
                {
                    Raylib.BeginDrawing();
                    app.Draw(Raylib.GetFrameTime());
                    Raylib.EndDrawing();
                    //Console.WriteLine(stopwatch.Elapsed.TotalMilliseconds);

                    RlManaged.RlObject.UnloadGCQueue();
                }

                RainEd.Logger.Information("Shutting down Rained...");
                app.Shutdown();
                rlImGui.Shutdown();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            RlManaged.RlObject.UnloadGCQueue();
            
            Raylib.CloseWindow();
            splashScreenWindow?.Close();
        }
    }
}