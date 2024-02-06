using Raylib_cs;
using rlImGui_cs;

using SFML.Window;
using SFML.Graphics;
using ImGuiNET;

namespace RainEd
{
    public class Boot
    {
        static void Main(string[] args)
        {
            // parse command arguments
            bool showSplashScreen = true;
            string levelToLoad = string.Empty;

            foreach (var str in args)
            {
                if (str[0] == '-')
                {
                    if (str == "--no-splash-screen")
                        showSplashScreen = false;
                }
                else
                {
                    levelToLoad = str;
                }
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
                Texture texture = new("data/splash-screen.png");
                var sprite = new Sprite(texture);

                splashScreenWindow.Clear();
                splashScreenWindow.Draw(sprite);
                splashScreenWindow.Display();
            }

            {
                Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.HiddenWindow);
                Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
                Raylib.InitWindow(1200, 800, "Rained");
                Raylib.SetTargetFPS(120);
                Raylib.SetExitKey(KeyboardKey.Null);

                // setup imgui
                rlImGui.Setup(true, true);
                rlImGui.SetIniFilename("data/imgui.ini");
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
                }

                rlImGui.Shutdown();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Raylib.CloseWindow();
            splashScreenWindow?.Close();
        }
    }
}