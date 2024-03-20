using ImGuiNET;
using System.Numerics;

namespace RainEd;

static class LevelLoadFailedWindow
{
    public const string WindowName = "Load Failure";
    public static bool IsWindowOpen = false;

    public static LevelLoadResult? LoadResult = null;

    public static void ShowWindow()
    {
        if (!ImGui.IsPopupOpen(WindowName) && IsWindowOpen)
        {
            ImGui.OpenPopup(WindowName);

            // center popup modal
            var viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewport.GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        }

        if (ImGui.BeginPopupModal(WindowName, ref IsWindowOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            ImGui.Text("The level failed to load because of unrecognized assets.");

            // show unknown props
            if (LoadResult!.UnrecognizedProps.Length > 0)
            {
                ImGui.SeparatorText("Unrecognized Props");
                foreach (var name in LoadResult.UnrecognizedProps)
                {
                    ImGui.BulletText(name);
                }
            }

            // show unknown tiles
            if (LoadResult!.UnrecognizedTiles.Length > 0)
            {
                ImGui.SeparatorText("Unrecognized Tiles");
                foreach (var name in LoadResult.UnrecognizedTiles)
                {
                    ImGui.BulletText(name);
                }
            }

            // show unknown materials
            if (LoadResult!.UnrecognizedMaterials.Length > 0)
            {
                ImGui.SeparatorText("Unrecognized Materials");
                foreach (var name in LoadResult.UnrecognizedMaterials)
                {
                    ImGui.BulletText(name);
                }
            }

            // show unknown effects
            if (LoadResult!.UnrecognizedEffects.Length > 0)
            {
                ImGui.SeparatorText("Unrecognized Effects");
                foreach (var name in LoadResult.UnrecognizedEffects)
                {
                    ImGui.BulletText(name);
                }
            }

            if (ImGui.Button("OK") || EditorWindow.IsKeyPressed(ImGuiKey.Escape) || EditorWindow.IsKeyPressed(ImGuiKey.Enter) || EditorWindow.IsKeyPressed(ImGuiKey.Space))
            {
                ImGui.CloseCurrentPopup();
                IsWindowOpen = false;
            }

            ImGui.EndPopup();
        }
        else
        {
            LoadResult = null;
        }
    }
}