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
            Raylib.InitWindow(1280, 800, "raylib-Extras-cs [ImGui] example - simple ImGui Demo");
            Raylib.SetTargetFPS(144);

            rlImGui.Setup(true);

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