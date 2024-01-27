using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

namespace RainEd
{
    public class Boot
    {
        static void Main(string[] args)
        {
            Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
            Raylib.InitWindow(1280, 800, "Rained");
            Raylib.SetTargetFPS(144);

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