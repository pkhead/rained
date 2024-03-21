/*
* App setup
* This runs when a preferences.json file could not be located on boot, which probably means
* that the user needs to set up their Data folder
*/

using ImGuiNET;
using RainEd;
using Raylib_cs;
using rlImGui_cs;
using System.Numerics;

class AppSetup
{
    public static bool Start(out string? assetDataPath)
    {
        assetDataPath = null;
        string? callbackRes = null;
        float callbackWait = 1f; 
        
        while (true)
        {
            if (Raylib.WindowShouldClose())
            {
                return false;
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            rlImGui.Begin();

            if (!ImGui.IsPopupOpen("Quick Setup"))
            {
                ImGui.OpenPopup("Quick Setup");

                // center popup modal
                var viewport = ImGui.GetMainViewport();
                ImGui.SetNextWindowPos(viewport.GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            }

            void Callback(string? path)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    callbackRes = path;
                }
            }

            bool _u = true;
            if (ImGui.BeginPopupModal("Quick Setup", ref _u, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
            {
                if (callbackRes is not null)
                {
                    ImGui.Text("Launching Rained...");
                }
                else
                {
                    ImGui.Text("Do you want to use Rain World level editor asset data already on your computer?");
                    ImGui.Text("Press \"No\" if you have not installed a Rain World level editor on your computer before.");
                    ImGui.Text("Pressing \"No\" will begin downloading asset data from the Internet.");

                    ImGui.Separator();

                    FileBrowser.Render();

                    if (ImGui.Button("Yes"))
                    {
                        FileBrowser.Open(FileBrowser.OpenMode.Directory, Callback, Boot.AppDataPath);
                    }

                    ImGui.SameLine();
                    ImGui.Button("No");
                }

                ImGui.EndPopup();
            }

            rlImGui.End();

            if (callbackRes is not null)
            {
                // wait a bit so that the Launching Rained... message can appear
                callbackWait -= Raylib.GetFrameTime();
                if (callbackWait <= 0f)
                {
                    assetDataPath = callbackRes;
                    break;
                }
            }

            Raylib.EndDrawing();
        }

        return true;
    }
}