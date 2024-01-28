using Raylib_cs;
using rlImGui_cs;

namespace RainEd
{
    public class Boot
    {
        static void Main(string[] args)
        {
            Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
            Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
            Raylib.InitWindow(1200, 800, "Rained");
            Raylib.SetTargetFPS(144);
            Raylib.SetExitKey(KeyboardKey.Null);

            // show splash screen
            var splashScreen = new RlManaged.Texture2D("data/splash-screen.png");
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Gray);
            Raylib.DrawTexture(
                splashScreen,
                (int)((1280f - splashScreen.Width) / 2f), (int)((800 - splashScreen.Height) / 2f),
                Color.White
            );
            Raylib.EndDrawing();

            // setup imgui
            rlImGui.Setup(true, true);
            rlImGui.SetIniFilename("data/imgui.ini");

            RainEd app = new();

            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                app.Draw();
                Raylib.EndDrawing();
            }

            rlImGui.Shutdown();
            Raylib.CloseWindow();
        }
    }
}