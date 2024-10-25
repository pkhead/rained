namespace Rained.EditorGui;

using System.Numerics;
using ImGuiNET;

static class MassRenderWindow
{
    public const string WindowName = "Mass Render";
    public static bool IsWindowOpen = false;

    record LevelPath(string Path, string LevelName);
    
    private static readonly List<LevelPath> levelPaths = [];
    private static FileBrowser? fileBrowser;
    private static int parallelismLimit = 1;
    private static bool limitParallelism = false;

    public static void OpenWindow()
    {
        levelPaths.Clear();
        fileBrowser = null;

        IsWindowOpen = true;
    }

    public static void ShowWindow()
    {
        if (!ImGui.IsPopupOpen(WindowName) && IsWindowOpen)
        {
            ImGui.OpenPopup(WindowName);

            // center popup modal
            ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        }

        if (ImGui.BeginPopupModal(WindowName, ref IsWindowOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            ImGui.SeparatorText("Queue");
            {
                if (ImGui.BeginListBox("##Levels"))
                {
                    foreach (var path in levelPaths)
                    {
                        ImGui.PushID(path.Path);
                        ImGui.Selectable(path.LevelName);
                        ImGui.PopID();
                    }

                    ImGui.EndListBox();
                }

                if (ImGui.Button("Add", StandardPopupButtons.ButtonSize))
                {
                    static bool levelCheck(string path, bool isRw)
                    {
                        return isRw;
                    }

                    var tab = RainEd.Instance.CurrentTab;
                    fileBrowser = new FileBrowser(FileBrowser.OpenMode.MultiRead, FileCallback, null);
                    fileBrowser.AddFilterWithCallback("Level file", levelCheck, ".txt");
                    fileBrowser.PreviewCallback = (string path, bool isRw) =>
                    {
                        if (isRw) return new BrowserLevelPreview(path);
                        return null;
                    };
                }

                ImGui.SameLine();
                if (ImGui.Button("Clear", StandardPopupButtons.ButtonSize))
                {
                    levelPaths.Clear();
                }
            }

            ImGui.SeparatorText("Options");
            {
                ImGui.Checkbox("Limit Parallelism", ref limitParallelism);

                if (limitParallelism)
                {
                    ImGui.InputInt("##Parallelism", ref parallelismLimit);
                    parallelismLimit = Math.Max(parallelismLimit, 1);
                }
            }

            ImGui.Separator();

            if (ImGui.Button("Render", StandardPopupButtons.ButtonSize))
            {

            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel", StandardPopupButtons.ButtonSize))
            {
                IsWindowOpen = false;
                levelPaths.Clear();
                ImGui.CloseCurrentPopup();
            }
            
            fileBrowser?.Render();

            ImGui.EndPopup();
        }
    }

    private static void FileCallback(string[] paths)
    {
        foreach (var path in paths)
        {
            if (levelPaths.Any(x => x.Path == path)) continue;
            levelPaths.Add(new LevelPath(path, Path.GetFileNameWithoutExtension(path)));
        }
    }
}