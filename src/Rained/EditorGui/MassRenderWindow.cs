namespace Rained.EditorGui;

using System.Data;
using System.Numerics;
using ImGuiNET;
using Rained.Drizzle;

static class MassRenderWindow
{
    public const string WindowName = "Mass Render";
    public static bool IsWindowOpen = false;

    record LevelPath(string Path, string LevelName);
    
    private static readonly List<LevelPath> levelPaths = [];
    private static FileBrowser? fileBrowser;
    private static int parallelismLimit = 1;
    private static bool limitParallelism = false;
    private static int queueItemMode = 0;
    private static readonly string[] queueItemModeNames = ["Files", "Folders"];

    private static MassRenderProcessWindow? massRenderProc = null;

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
            RainEd.Instance.IsLevelLocked = true;

            ImGui.SeparatorText("Queue");
            {
                if (ImGuiExt.ButtonSwitch("ItemQueueSwitch", ["Files", "Folders"], ref queueItemMode))
                {
                    levelPaths.Clear();
                }

                /*{
                    ImGui.SliderInt("##QueueMode", ref queueItemMode, 0, 1, queueItemModeNames[queueItemMode]);

                }*/

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

                    // select files
                    if (queueItemMode == 0)
                    {
                        fileBrowser = new FileBrowser(FileBrowser.OpenMode.MultiRead, FileCallback, null);
                        fileBrowser.AddFilterWithCallback("Level file", levelCheck, ".txt");
                        fileBrowser.PreviewCallback = (string path, bool isRw) =>
                        {
                            if (isRw) return new BrowserLevelPreview(path);
                            return null;
                        };
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
                StartRender();
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel", StandardPopupButtons.ButtonSize))
            {
                IsWindowOpen = false;
                levelPaths.Clear();
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
        string[] files;

        if (queueItemMode == 0)
        {
            files = levelPaths.Select(x => x.Path).ToArray();
        } 
        else if (queueItemMode == 1)
        {
            files = levelPaths.SelectMany(x => Directory.GetFiles(x.Path).Where(x => Path.GetExtension(x) == ".txt"))
                .ToArray();
        }
        else
        {
            throw new Exception("Invalid item mode");
        }

        var massRender = new DrizzleMassRender(
            files,
            limitParallelism ? 0 : parallelismLimit
        );

        massRenderProc = new MassRenderProcessWindow(massRender);
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
            levelPaths.Add(new LevelPath(path, Path.GetFileName(path)));
        }
    }
}