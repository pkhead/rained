namespace Rained.EditorGui;
using ImGuiNET;

static class MassRenderWindow
{
    public const string WindowName = "Mass Render";
    public static bool IsWindowOpen = false;

    record LevelPath(string Path, string LevelName);
    
    private static List<LevelPath> levelPaths = [];
    private static FileBrowser? fileBrowser;

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

            if (ImGui.Button("Select File(s)"))
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

            if (ImGui.BeginListBox("Levels"))
            {
                foreach (var path in levelPaths)
                {
                    ImGui.PushID(path.Path);
                    ImGui.Selectable(path.LevelName);
                    ImGui.PopID();
                }
            }

            // center popup modal
            ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        }

        if (ImGui.BeginPopupModal(WindowName, ref IsWindowOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
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