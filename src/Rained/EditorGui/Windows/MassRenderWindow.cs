namespace Rained.EditorGui;

using System.Data;
using System.Numerics;
using ImGuiNET;
using Rained.Drizzle;

static class MassRenderWindow
{
    public const string WindowName = "Mass Render";
    public static bool IsWindowOpen = false;

    record LevelPath(string Path, string Name);
    
    private static readonly List<LevelPath> levelPaths = [];
    private static readonly List<LevelPath> folderPaths = [];
    private static FileBrowser? fileBrowser;
    private static int parallelismLimit = 4;
    private static int queueItemMode = 0;
    private static readonly string[] queueItemModeNames = ["Files", "Folders"];

    private static MassRenderProcessWindow? massRenderProc = null;

    public static void OpenWindow()
    {
        levelPaths.Clear();
        folderPaths.Clear();
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
            RainEd.Instance.IsLevelLocked = true;

            ImGui.SeparatorText("Queue");
            {
                ImGuiExt.ButtonSwitch("ItemQueueSwitch", ["Files", "Folders"], ref queueItemMode);
                var curModePaths = queueItemMode == 1 ? folderPaths : levelPaths;

                /*{
                    ImGui.SliderInt("##QueueMode", ref queueItemMode, 0, 1, queueItemModeNames[queueItemMode]);

                }*/

                if (ImGui.BeginListBox("##Levels"))
                {
                    int deleteRequest = -1;

                    for (int i = 0; i < curModePaths.Count; i++)
                    {
                        var path = curModePaths[i];
                        
                        ImGui.PushID(path.Path);
                        if (ImGui.Selectable(path.Name))
                            deleteRequest = i;

                        ImGui.PopID();
                    }

                    if (deleteRequest >= 0)
                        curModePaths.RemoveAt(deleteRequest);

                    ImGui.EndListBox();
                }

                if (ImGui.Button("Add", StandardPopupButtons.ButtonSize))
                {
                    static bool levelCheck(string path, bool isRw)
                    {
                        return isRw;
                    }

                    // select files
                    if (queueItemMode == 0)
                    {
                        fileBrowser = new FileBrowser(FileBrowser.OpenMode.MultiRead, FileCallback, null);
                        LevelData.FileFormats.LevelFileFormats.SetUpFileBrowser(fileBrowser);
                    }

                    // select folders
                    else if (queueItemMode == 1)
                    {
                        fileBrowser = new FileBrowser(FileBrowser.OpenMode.MultiDirectory, FolderCallback, null);
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button("Clear", StandardPopupButtons.ButtonSize))
                {
                    curModePaths.Clear();
                }
            }

            ImGui.SeparatorText("Options");
            {
                ImGui.SetNextItemWidth(ImGui.GetFontSize() * 6.0f);
                ImGui.InputInt("Threads", ref parallelismLimit);
                parallelismLimit = Math.Max(parallelismLimit, 1);

                ImGui.SameLine();
                ImGui.TextDisabled("(?)");
                if (ImGui.BeginItemTooltip())
                {
                    ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
                    ImGui.TextWrapped("The maximum amount of concurrent render processes. Higher values make the total render time shorter, but require more CPU cores and RAM.");
                    ImGui.PopTextWrapPos();
                    ImGui.EndTooltip();
                }
            }

            ImGui.Separator();

            if (ImGui.Button("Render", StandardPopupButtons.ButtonSize) &&
                (levelPaths.Count > 0 || folderPaths.Count > 0))
            {
                StartRender();
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel", StandardPopupButtons.ButtonSize))
            {
                IsWindowOpen = false;
                levelPaths.Clear();
                folderPaths.Clear();
                ImGui.CloseCurrentPopup();
                IsWindowOpen = false;
            }
            
            fileBrowser?.Render();

            if (massRenderProc is not null)
            {
                massRenderProc.Render();
                if (massRenderProc.IsDone)
                {
                    massRenderProc = null;
                    IsWindowOpen = false;
                    ImGui.CloseCurrentPopup();
                }
            }

            ImGui.EndPopup();
        }
        
        if (!IsWindowOpen)
        {
            RainEd.Instance.IsLevelLocked = false;
        }
    }

    private static void StartRender()
    {
        List<string> files = [];

        // process folders
        files.AddRange(folderPaths.SelectMany(x => Directory.GetFiles(x.Path).Where(x => Path.GetExtension(x) == ".txt")));

        // process files, not including files that were already added when processing folders
        // obviously quite slow. hashset not viable because I want them to be in order of submission.
        files.AddRange(levelPaths.Where(x => !files.Contains(x.Path)).Select(x => x.Path));

        massRenderProc = new MassRenderProcessWindow([..files], parallelismLimit);
    }

    private static void FileCallback(string[] paths)
    {
        foreach (var path in paths)
        {
            if (levelPaths.Any(x => x.Path == path)) continue;
            levelPaths.Add(new LevelPath(path, Path.GetFileNameWithoutExtension(path)));
        }
    }

    private static void FolderCallback(string[] paths)
    {
        foreach (var path in paths)
        {
            var dirname = Path.GetFileName(Path.GetDirectoryName(path)!);
            folderPaths.Add(new LevelPath(path, dirname));
        }
    }
}