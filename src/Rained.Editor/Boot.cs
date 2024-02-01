using Raylib_cs;
using rlImGui_cs;

using SFML.Window;
using SFML.Graphics;

namespace RainEd
{
    public class Boot
    {
        static void SplashScreen()
        {
            /*Raylib.InitWindow(480, 360, "Rained");
            var splashScreen = new RlManaged.Texture2D("data/splash-screen.png");
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Gray);
            Raylib.DrawTexture(
                splashScreen,
                (int)((1280f - splashScreen.Width) / 2f), (int)((800 - splashScreen.Height) / 2f),
                Color.White
            );
            Raylib.EndDrawing();*/
            var win = new RenderWindow(new VideoMode(480, 360), "Rained Splash Screen", Styles.None);
            Texture texture = new("data/splash-screen.png");
            var sprite = new Sprite(texture);

            while (win.IsOpen)
            {
                win.DispatchEvents();
                win.Clear();
                win.Draw(sprite);
                win.Display();
            }
        }

        static void Main(string[] args)
        {
            // create splash screen window using sfml
            // to display while editor is loading
            // funny how i have to use two separate window/graphics libraries
            // cus raylib doesn't have multi-window support
            // I was originally going to use only SFML, but it didn't have any
            // good C# ImGui integration libraries. (Raylib did, though)
            var splashScreenWindow = new RenderWindow(new VideoMode(480, 360), "Rained Splash Screen", Styles.None);
            Texture texture = new("data/splash-screen.png");
            var sprite = new Sprite(texture);

            splashScreenWindow.Clear();
            splashScreenWindow.Draw(sprite);
            splashScreenWindow.Display();

            Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.HiddenWindow);
            Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
            Raylib.InitWindow(1200, 800, "Rained");
            Raylib.SetTargetFPS(120);
            Raylib.SetExitKey(KeyboardKey.Null);

            // setup imgui
            rlImGui.Setup(true, true);
            rlImGui.SetIniFilename("data/imgui.ini");

            RainEd app = new(args.Length > 0 ? args[0] : "");
            Raylib.ClearWindowState(ConfigFlags.HiddenWindow);
            
            // for some reason, closing the window bugs
            // out raylib, so i just set it invisible
            // and close it when the program ends
            splashScreenWindow.SetVisible(false);
            
            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                app.Draw(Raylib.GetFrameTime());
                Raylib.EndDrawing();
            }

            rlImGui.Shutdown();
            Raylib.CloseWindow();
            splashScreenWindow.Close();
        }
    }
}