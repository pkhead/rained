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
                if (ButtonSwitch("ItemQueueSwitch", ["Files", "Folders"], ref queueItemMode))
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

    private static bool ButtonSwitch(string id, ReadOnlySpan<string> options, ref int selected)
    {
        var activeCol = ImGui.GetStyle().Colors[(int)ImGuiCol.Button];
        var activeColHover = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered];
        var activeColActive = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive];

        var inactiveCol = new Vector4(activeCol.X, activeCol.Y, activeCol.Z, activeCol.W / 2f);
        var inactiveColHover = new Vector4(activeColHover.X, activeColHover.Y, activeColHover.Z, activeColHover.W / 2f);
        var inactiveColActive = new Vector4(activeColActive.X, activeColActive.Y, activeColActive.Z, activeColActive.W / 2f);

        var itemSpacing = ImGui.GetStyle().ItemInnerSpacing;
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, itemSpacing);

        ImGui.PushID(id);

        var returnValue = false;
        var itemSize = new Vector2((ImGui.CalcItemWidth() + itemSpacing.X * (1 - options.Length)) / options.Length, 0f);

        for (int i = 0; i < options.Length; i++)
        {
            if (selected == i)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, activeCol);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, activeColHover);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColActive);
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, inactiveCol);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, inactiveColHover);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, inactiveColActive);
            }

            ImGui.PushID(i);
            if (i > 0) ImGui.SameLine();
            if (ImGui.Button(options[i], itemSize))
            {
                if (selected != i) returnValue = true;
                selected = i;
            }
            ImGui.PopID();

            ImGui.PopStyleColor(3);
        }

        ImGui.PopID();
        ImGui.PopStyleVar();

        return returnValue;
    }
}