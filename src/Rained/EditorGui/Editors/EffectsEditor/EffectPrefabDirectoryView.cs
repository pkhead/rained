using ImGuiNET;
using Rained.Assets;
using System.Numerics;
using Rained.LevelData;
using RainEd.EditorGui;
namespace Rained.EditorGui.Editors;

class EffectPrefabDirectoryView : DirectoryTreeView
{
    enum ActionPromptRequest
    {
        None,
        DeletePrefab,
        RenamePrefab,
        CreatePrefab,
    };

    public Action<EffectPrefab>? ApplyPrefabCallback;

    private ActionPromptRequest actionPromptRequest = ActionPromptRequest.None;
    private string? actionFilePath = null;
    private List<int> selectedEffects = null!;
    private NamePromptState? namePromptState = null;

    public EffectPrefabDirectoryView() :
        base(Path.Combine(Boot.AppDataPath, "config", "prefabs", "effects"), "*.json", false)
    {
        Directory.CreateDirectory(Cache.BaseDirectory);
        Cache.Refresh();
    }

    protected override void DirectoryContextMenu(string dirPath)
    {
        if (ImGui.Selectable("Create Prefab"))
        {
            actionFilePath = dirPath;
            actionPromptRequest = ActionPromptRequest.CreatePrefab;
        }
    }

    protected override void FileContextMenu(string filePath)
    { }

    protected override void RequestFileDeletion(string filePath)
    {
        actionFilePath = filePath;
        actionPromptRequest = ActionPromptRequest.DeletePrefab;
    }

    protected override void RequestFileRename(string filePath)
    {
        actionFilePath = filePath;
        actionPromptRequest = ActionPromptRequest.RenamePrefab;
    }

    protected override void FileActivated(string filePath)
    {
        var realPath = Cache.ConvertToRealPath(filePath);
        Log.Information("Load prefab {Path}", realPath);

        try
        {
            var prefab = EffectPrefab.ReadFromFile(realPath);
            if (prefab is null)
            {
                Log.UserLogger.Error("Could not load prefab flie \"{File}\"", realPath);
                EditorWindow.ShowNotification("Error occurred while applying prefab");
            }
            else
            {
                ApplyPrefabCallback?.Invoke(prefab);
            }
        }
        catch (Exception e)
        {
            Log.UserLogger.Error(e.ToString());
            EditorWindow.ShowNotification("Error occurred while applying prefab");
        }
    }

    public override void Render(string label, Vector2 size)
    {
        base.Render(label, size);

        if (actionPromptRequest != ActionPromptRequest.None)
        {
            switch (actionPromptRequest)
            {
                case ActionPromptRequest.DeletePrefab:
                    ImGui.OpenPopup("Delete Prefab?");
                    ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
                    break;

                case ActionPromptRequest.CreatePrefab:
                    ImGui.OpenPopup("Create Prefab");
                    ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
                    selectedEffects = [];

                    break;

                case ActionPromptRequest.RenamePrefab:
                    ImGui.OpenPopup("Rename Prefab");
                    ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
                    break;
            }

            actionPromptRequest = ActionPromptRequest.None;
        }

        var flags = ImGuiWindowFlags.AlwaysAutoResize;
        bool isPopupOpen = false;

        if (ImGui.BeginPopupModal("Delete Prefab?", flags))
        {
            isPopupOpen = true;

            ImGui.PushTextWrapPos(ImGui.GetTextLineHeight() * 20.0f);
            ImGui.TextWrapped($"Are you sure you want to delete the file \"{actionFilePath}\"?");

            ImGui.Separator();

            if (StandardPopupButtons.Show(PopupButtonList.YesNo, out int btn))
            {
                if (btn == 0)
                {
                    try
                    {
                        Cache.DeleteFile(actionFilePath!);
                    }
                    catch (Exception e)
                    {
                        Log.UserLogger.Error(e.Message);
                        Log.Error(e.ToString());
                        EditorWindow.ShowNotification("An error occurred trying to delete the prefab");
                    }
                }

                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        if (ImGui.BeginPopupModal("Create Prefab", flags))
        {
            isPopupOpen = true;

            // show effect stack
            var level = RainEd.Instance.Level;

            ImGui.Text("Select effects to add");
            if (ImGui.BeginListBox("##sources", new Vector2(-0.00001f, ImGui.GetFrameHeight() * 12.0f)))
            {
                if (level.Effects.Count == 0)
                {
                    ImGui.TextDisabled("(no effects)");
                }
                else
                {
                    for (int i = 0; i < level.Effects.Count; i++)
                    {
                        ImGui.PushID(i);
                        var effect = level.Effects[i];

                        bool isSelected = selectedEffects.Contains(i);
                        if (ImGui.Selectable(effect.Data.name, isSelected))
                        {
                            if (isSelected)
                                selectedEffects.Remove(i);
                            else
                                selectedEffects.Add(i);
                        }

                        ImGui.PopID();
                    }
                }

                ImGui.EndListBox();
            }

            namePromptState ??= new NamePromptState(actionFilePath!)
            {
                CanOverwrite = true,
                GetFullPath = static (self, x) => DirectoryTreeCache.Join(self.Path, x + ".json")
            };

            switch (PopupNamePrompt("Prefab Name...", namePromptState, selectedEffects.Count > 0))
            {
                case 1: // submit
                    {
                        var prefabPath = DirectoryTreeCache.Join(actionFilePath!, namePromptState.Input + ".json");

                        try
                        {
                            selectedEffects.Sort();
                            var effectPrefab = new EffectPrefab()
                            {
                                Items = [.. selectedEffects.Select(
                                            x => new EffectPrefab.EffectPrefabItem(level.Effects[x])
                                        )]
                            };

                            effectPrefab.WriteToFile(Cache.ConvertToRealPath(prefabPath)!);
                            Cache.RefreshDirectoryList(actionFilePath!, false);
                        }
                        catch (Exception e)
                        {
                            Log.UserLogger.Error(e.Message);
                            Log.Error(e.ToString());
                            EditorWindow.ShowNotification("An error occurred trying to create the prefab");
                        }

                        ImGui.CloseCurrentPopup();
                    }
                    break;

                case 2: // cancel
                    ImGui.CloseCurrentPopup();
                    break;
            }

            ImGui.EndPopup();
        }

        if (ImGui.BeginPopupModal("Rename Prefab", flags))
        {
            isPopupOpen = true;

            namePromptState ??= new NamePromptState(actionFilePath!, PathGetNameWithoutExtension(actionFilePath!))
            {
                PathPermittedToOverride = actionFilePath,
                GetFullPath = static (self, x) => DirectoryTreeCache.Join(self.Path, "..", x + ".json")
            };

            switch (PopupNamePrompt("Prefab Name...", namePromptState))
            {
                case 1: // submit
                    {
                        var newPath = DirectoryTreeCache.Join(actionFilePath!, "..", namePromptState.Input + ".json");
                        newPath = DirectoryTreeCache.NormalizePath(newPath);

                        try
                        {
                            Cache.MoveFile(actionFilePath!, newPath);
                        }
                        catch (Exception e)
                        {
                            Log.UserLogger.Error(e.Message);
                            Log.Error(e.ToString());
                            EditorWindow.ShowNotification("An error occurred trying to rename the prefab");
                        }

                        ImGui.CloseCurrentPopup();
                    }
                    break;

                case 2: // cancel
                    ImGui.CloseCurrentPopup();
                    break;
            }

            ImGui.EndPopup();
        }

        if (!isPopupOpen)
        {
            namePromptState = null;
        }
    }
}